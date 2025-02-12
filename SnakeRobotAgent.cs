using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

/*
    CubeとPlaneの1辺の長さ(初期値)は
    Cube:1m
    Plane:10m
*/

[System.Serializable]
public class AxleInfo
{
    public WheelCollider firstLeftWheel;
    public WheelCollider firstRightWheel;
    public WheelCollider secondLeftWheel;
    public WheelCollider secondRightWheel;
    public WheelCollider thirdLeftWheel;
    public WheelCollider thirdRightWheel;
}

// SnakeRobotAgent
public class SnakeRobotAgent : Agent
{

    TrailRenderer trailRenderer;

    public GameObject goal; // GoalのGameObject

    public GameObject agentCamera; //ヘビ型ロボットのCamera

    public GameObject Body1; // Body1のGameObject
    public GameObject Body2; // Body2のGameObject
    public GameObject Body3; // Body3のGameObject

    Rigidbody rBody1; // Body1のRigidbody
    Rigidbody rBody2; // Body2のRigidbody
    Rigidbody rBody3; // Body3のRigidbody

    public List<AxleInfo> axleInfos;
    public float maxMotorTorque; // 最大モータートルク

    float stageNumber = 0.0f; // エピソード開始時のステージ番号を取得

    int actionCount = 10; // 行動の回数をカウント

    int goalCountTrain = 0; // ゴール到達回数をカウント（学習用）

    // episodeCount1とepisodeCount2は同じ値にすること
    public int episodeCount1; // 指定したエピソード回数をカウント（評価用）
    public int episodeCount2; // ゴール到達率の分母（評価用）
    int goalCountEval = 0; // ゴール到達回数をカウント（評価用）
    int[] collisionCount = new int[100]; // ゴール到達までに瓦礫に衝突した回数（1エピソードごとに算出）
    int fallCount = 0; // フィールドから落下した回数をカウント（評価用）
    int fallFlg = 0; // フィールドから落下（1なら落下0なら未落下）
    int timeUpFlg = 1; // 時間切れ（1なら時間切れ0なら時間内）

    bool evalFlg = true; // trueは未評価、falseは評価済み
    float episodeReward = 0.0f; // 1エピソードごとの獲得報酬を計測

    //追加1
    public event System.Action OnEndEpisode;
    public event System.Action OnStartEpisode;
    //ここまで

    

    // ゲームオブジェクト生成時に呼ばれる
    public override void Initialize()
    {
        // BodyのRigidBodyの参照の取得
        rBody1 = Body1.GetComponent<Rigidbody>();
        rBody2 = Body2.GetComponent<Rigidbody>();
        rBody3 = Body3.GetComponent<Rigidbody>();
    }

    //目的地の位置をランダムに変更
    // private void SetRandomGoalPosition()
    // {
    //     // ランダムなXとZ座標を生成 (範囲は環境に応じて調整します)
    //     float randomX = Random.Range(-2.5f, 2.5f);
    //     float randomZ = Random.Range(0.0f, 4.775f);

    //     // ゴールのY座標は保持し、それ以外をランダムに変える
    //     goal.transform.position = new Vector3(randomX, goal.transform.position.y, randomZ);
    // }

    
    // エピソード開始時に呼ばれる
    public override void OnEpisodeBegin()
    {
        // 目的地の位置をランダムに変更
        // SetRandomGoalPosition();

        

        if (episodeCount1 == 0 && evalFlg == true)
        {
            evalFlg = false;
            evaluation(goalCountEval, episodeCount2);
            averageCollision(collisionCount, episodeCount2);
        }
        else if (episodeCount1 > 0)
        {
            episodeCount1 -= 1;
        }
        collisionCount[episodeCount2 - (episodeCount1 + 1)] = 0;

        // エピソード開始時のステージ番号を取得
        stageNumber = Academy.Instance.EnvironmentParameters.GetWithDefault("stage_number", 0.0f);

        // 現在のステージの学習が終わったらアプリケーションを終了
        if (SceneManager.GetActiveScene().name == "SnakeRobot-Stage1" && stageNumber > 1.0f)
        {
            Application.Quit();
        }
        if (SceneManager.GetActiveScene().name == "SnakeRobot-Stage2" && stageNumber > 2.0f)
        {
            Application.Quit();
        }
        if (SceneManager.GetActiveScene().name == "SnakeRobot-Stage3" && stageNumber > 3.0f)
        {
            Application.Quit();
        }
        if (SceneManager.GetActiveScene().name == "SnakeRobot-Stage4" && stageNumber > 4.0f)
        {
            Application.Quit();
        }
        if (SceneManager.GetActiveScene().name == "SnakeRobot-Stage5" && stageNumber > 5.0f)
        {
            Application.Quit();
        }
        if (SceneManager.GetActiveScene().name == "SnakeRobot-Stage6" && stageNumber > 6.0f)
        {
            Application.Quit();
        }
        if (SceneManager.GetActiveScene().name == "SnakeRobot-Stage7" && stageNumber > 7.0f)
        {
            Application.Quit();
        }
         if (SceneManager.GetActiveScene().name == "SnakeRobot-Stage8" && stageNumber > 8.0f)
        {
            Application.Quit();
        }
         if (SceneManager.GetActiveScene().name == "SnakeRobot-Stage9" && stageNumber > 9.0f)
        {
            Application.Quit();
        }

        // SnakeRobotAgentの位置と速度をリセット
        rBody1.angularVelocity = Vector3.zero;
        rBody1.velocity = Vector3.zero;
        rBody2.angularVelocity = Vector3.zero;
        rBody3.velocity = Vector3.zero;
        rBody3.angularVelocity = Vector3.zero;
        rBody3.velocity = Vector3.zero;

        Body1.transform.position = new Vector3(0.0f, 0.1f, 0.1f);
        Body1.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        Body2.transform.position = new Vector3(0.0f, 0.1f, 0.1f);
        Body2.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        Body3.transform.position = new Vector3(0.0f, 0.1f, 0.1f);
        Body3.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

        // 評価環境用
        trailRenderer = GetComponent<TrailRenderer>();
        trailRenderer.Clear();


        // 障害物の位置をランダムにリセット(Stage2以降)
        // Random.valueは 0.0 ≦ x < 1.0 の実数
        if (Mathf.Approximately(stageNumber, 2.0f))
        {
            GameObject[] rubbles = GameObject.FindGameObjectsWithTag("Rubble");
            foreach (GameObject rubble in rubbles)
            {
                rubble.transform.position = new Vector3(Random.value * 2.55f - 1.275f, 0.0f, Random.value * 3.0f + 1.0f);
            }
        }
        if (Mathf.Approximately(stageNumber, 3.0f))
        {
            GameObject[] rubbles = GameObject.FindGameObjectsWithTag("Rubble");
            foreach (GameObject rubble in rubbles)
            {
                rubble.transform.position = new Vector3(Random.value * 2.55f - 1.275f, 0.0f, Random.value * 3.0f + 1.0f);
            }
        }
        if (Mathf.Approximately(stageNumber, 4.0f))
        {
            GameObject[] rubbles = GameObject.FindGameObjectsWithTag("Rubble");
            foreach (GameObject rubble in rubbles)
            {
                rubble.transform.position = new Vector3(Random.value * 2.55f - 1.275f, 0.0f, Random.value * 3.0f + 1.0f);
            }
        }
        if (Mathf.Approximately(stageNumber, 5.0f))
        {
            GameObject[] rubbles = GameObject.FindGameObjectsWithTag("Rubble");
            foreach (GameObject rubble in rubbles)
            {
                rubble.transform.position = new Vector3(Random.value * 2.55f - 1.275f, 0.0f, Random.value * 3.0f + 1.0f);
            }
        }
        if (Mathf.Approximately(stageNumber, 6.0f))
        {
            GameObject[] rubbles = GameObject.FindGameObjectsWithTag("Rubble");
            foreach (GameObject rubble in rubbles)
            {
                rubble.transform.position = new Vector3(Random.value * 2.55f - 1.275f, 0.0f, Random.value * 3.0f + 1.0f);
            }
        }
        if (Mathf.Approximately(stageNumber, 7.0f))
        {
            GameObject[] rubbles = GameObject.FindGameObjectsWithTag("Rubble");
            foreach (GameObject rubble in rubbles)
            {
                rubble.transform.position = new Vector3(Random.value * 2.55f - 1.275f, 0.0f, Random.value * 3.0f + 1.0f);
            }
        }
        if (Mathf.Approximately(stageNumber, 8.0f))
        {
            GameObject[] rubbles = GameObject.FindGameObjectsWithTag("Rubble");
            foreach (GameObject rubble in rubbles)
            {
                rubble.transform.position = new Vector3(Random.value * 2.55f - 1.275f, 0.0f, Random.value * 3.0f + 1.0f);
            }
        }
        if (Mathf.Approximately(stageNumber, 9.0f))
        {
            GameObject[] rubbles = GameObject.FindGameObjectsWithTag("Rubble");
            foreach (GameObject rubble in rubbles)
            {
                rubble.transform.position = new Vector3(Random.value * 2.55f - 1.275f, 0.0f, Random.value * 3.0f + 1.0f);
            }
        }

        //Stage2で使った
        // 目的地の位置を変更
        // goal.transform.position = new Vector3(0.0f, 1e-3f, goalCountTrain / 100 * 0.25f + 0.775f);
        //Debug.Log(goalCountTrain);
        //Debug.Log(goalCountTrain / 100 * 0.25f + 0.775f);

        //追加1
        OnStartEpisode?.Invoke();
        //ここまで
    }
 

    //追加1
     public void EndThisEpisode()
    {
        EndEpisode();
        OnEndEpisode?.Invoke();
    }
    //ここまで


    // 観察取得時に呼ばれる
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(goal.transform.position.x); //TargetのX座標
        sensor.AddObservation(goal.transform.position.z); //TargetのZ座標
        sensor.AddObservation(agentCamera.transform.position.x); //SnakeRobotAgentのX座標
        sensor.AddObservation(agentCamera.transform.position.z); //SnakeRobotAgentのZ座標
        sensor.AddObservation(rBody1.velocity.x); //Body1のX速度
        sensor.AddObservation(rBody1.velocity.z); //Body1のZ速度
        sensor.AddObservation(rBody2.velocity.x); //Body2のX速度
        sensor.AddObservation(rBody2.velocity.z); //Body2のZ速度
        sensor.AddObservation(rBody3.velocity.x); //Body3のX速度
        sensor.AddObservation(rBody3.velocity.z); //Body3のZ速度
        sensor.AddObservation(Body1.transform.eulerAngles.y); //Body1のY角度
        sensor.AddObservation(Body2.transform.eulerAngles.y); //Body2のY角度
        sensor.AddObservation(Body3.transform.eulerAngles.y); //Body3のY角度
    }

    // 行動決定時に呼ばれる
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {

        actionCount -= 1;

        // SnakeRobotに力を加える
        Vector3 firstLeftWheelControlSignal = Vector3.zero;
        Vector3 firstRightWheelControlSignal = Vector3.zero;
        Vector3 secondLeftWheelControlSignal = Vector3.zero;
        Vector3 secondRightWheelControlSignal = Vector3.zero;
        Vector3 thirdLeftWheelControlSignal = Vector3.zero;
        Vector3 thirdRightWheelControlSignal = Vector3.zero;

        // ブレーキを解除
        axleInfos[0].firstLeftWheel.brakeTorque = 0f;
        axleInfos[0].firstRightWheel.brakeTorque = 0f;
        axleInfos[0].secondLeftWheel.brakeTorque = 0f;
        axleInfos[0].secondRightWheel.brakeTorque = 0f;
        axleInfos[0].thirdLeftWheel.brakeTorque = 0f;
        axleInfos[0].thirdRightWheel.brakeTorque = 0f;

        // Discreteの場合
        // Agentが選択した行動を取得
        int firstLeftWheelAction = actionBuffers.DiscreteActions[0];
        int firstRightWheelAction = actionBuffers.DiscreteActions[1];
        int secondLeftWheelAction = actionBuffers.DiscreteActions[2];
        int secondRightWheelAction = actionBuffers.DiscreteActions[3];
        int thirdLeftWheelAction = actionBuffers.DiscreteActions[4];
        int thirdRightWheelAction = actionBuffers.DiscreteActions[5];

        // Debug.Log("FL" + firstLeftWheelAction);
        // Debug.Log("FR" + firstRightWheelAction);
        // Debug.Log("SL" + secondLeftWheelAction);
        // Debug.Log("SR" + secondRightWheelAction);
        // Debug.Log("TL" + thirdLeftWheelAction);
        // Debug.Log("TR" + thirdRightWheelAction);

        // z軸が前進(1)後退(-1)
        // firstLeftWheel
        if (firstLeftWheelAction == 0)
        {
            firstLeftWheelControlSignal.z = 1.0f; //前進
        }
        else if (firstLeftWheelAction == 1)
        {
            firstLeftWheelControlSignal.z = -1.0f; //後退
        }
        else
        {
            axleInfos[0].firstLeftWheel.brakeTorque = 100f; //停止
        }

        // firstRightWheel
        if (firstRightWheelAction == 0)
        {
            firstRightWheelControlSignal.z = 1.0f; //前進
        }
        else if (firstRightWheelAction == 1)
        {
            firstRightWheelControlSignal.z = -1.0f; //後退
        }
        else
        {
            axleInfos[0].firstRightWheel.brakeTorque = 100f; //停止
        }

        // secondLeftWheel
        if (secondLeftWheelAction == 0)
        {
            secondLeftWheelControlSignal.z = 1.0f; //前進
        }
        else if (secondLeftWheelAction == 1)
        {
            secondLeftWheelControlSignal.z = -1.0f; //後退
        }
        else
        {
            axleInfos[0].secondLeftWheel.brakeTorque = 100f; //停止
        }

        // secondRightWheel
        if (secondRightWheelAction == 0)
        {
            secondRightWheelControlSignal.z = 1.0f; //前進
        }
        else if (secondRightWheelAction == 1)
        {
            secondRightWheelControlSignal.z = -1.0f; //後退
        }
        else
        {
            axleInfos[0].secondRightWheel.brakeTorque = 100f; //停止
        }

        // thirdLeftWheel
        if (thirdLeftWheelAction == 0)
        {
            thirdLeftWheelControlSignal.z = 1.0f; //前進
        }
        else if (thirdLeftWheelAction == 1)
        {
            thirdLeftWheelControlSignal.z = -1.0f; //後退
        }
        else
        {
            axleInfos[0].thirdLeftWheel.brakeTorque = 100f; //停止
        }

        // thirdRightWheel
        if (thirdRightWheelAction == 0)
        {
            thirdRightWheelControlSignal.z = 1.0f; //前進
        }
        else if (thirdRightWheelAction == 1)
        {
            thirdRightWheelControlSignal.z = -1.0f; //後退
        }
        else
        {
            axleInfos[0].thirdRightWheel.brakeTorque = 100f; //停止
        }

        // トルクをかける方向を決定
        float[] motor = new float[6];
        motor[0] = maxMotorTorque * firstLeftWheelControlSignal.z;
        motor[1] = maxMotorTorque * firstRightWheelControlSignal.z;
        motor[2] = maxMotorTorque * secondLeftWheelControlSignal.z;
        motor[3] = maxMotorTorque * secondRightWheelControlSignal.z;
        motor[4] = maxMotorTorque * thirdLeftWheelControlSignal.z;
        motor[5] = maxMotorTorque * thirdRightWheelControlSignal.z;

        // モーター操作
        axleInfos[0].firstLeftWheel.motorTorque = motor[0];
        axleInfos[0].firstRightWheel.motorTorque = motor[1];
        axleInfos[0].secondLeftWheel.motorTorque = motor[2];
        axleInfos[0].secondRightWheel.motorTorque = motor[3];
        axleInfos[0].thirdLeftWheel.motorTorque = motor[4];
        axleInfos[0].thirdRightWheel.motorTorque = motor[5];

        

        // SnakeRobotAgentがGoalにたどり着いた時
        float distanceToTarget = Vector3.Distance(agentCamera.transform.position, goal.transform.position);
        if (distanceToTarget < 0.125f)
        {
            goalCountTrain += 1;
            goalCountEval += 1;

            if (goalCountTrain < 1600)
            {
                AddReward(0.75f); // 報酬を+0.75点加算
            }
            else if (goalCountTrain >= 1600)
            {
                AddReward(1.0f); // 報酬を+1.0点加算
            }

            EndEpisode();
        }
        // else if(actionCount < 1)
        // {
        //     actionCount = 10;
        //     AddReward(-1e-4f * distanceToTarget); // 災害ロボットの現在地がゴール地点から離れているほど減点
        // }

        // SnakeRobotAgentが道路から落下した時
        if (Body1.transform.position.y < 0 && Body2.transform.position.y < 0 && Body3.transform.position.y < 0)
        {
            AddReward(-0.5f); // 報酬を-0.5点加算
            fallFlg = 1;
            timeUpFlg = 0;
            fallCount += 1; // フィールドから落下した回数をカウント
         

            //追加1
            EndThisEpisode();
            
        }

        // // 車体の角度に応じてマイナスの報酬を与える
        // AddReward(-1e-6f * Mathf.Abs(AngleTranslate(Body1.transform.eulerAngles.y)));
        // AddReward(-1e-6f * Mathf.Abs(AngleTranslate(Body2.transform.eulerAngles.y)));
        // AddReward(-1e-6f * Mathf.Abs(AngleTranslate(Body3.transform.eulerAngles.y)));

    }

    // SnakeRobotAgentが瓦礫に衝突したとき
    public void CollisionChecker(Collision collision)
    {

        if(collision.gameObject.name.Contains("Cube"))
        {
            collisionCount[episodeCount2 - (episodeCount1 + 1)] += 1;
            AddReward(-0.1f); // 報酬を-0.1点加算
        }
        
    }

    // ヒューリスティックモードの行動決定時に呼ばれる
    public override void Heuristic(in ActionBuffers actionBuffers)
    {

        // Discreteの場合
        var actionsOut = actionBuffers.DiscreteActions;

        actionsOut[0] = 0;
        actionsOut[1] = 0;
        actionsOut[2] = 0;
        actionsOut[3] = 0;
        actionsOut[4] = 0;
        actionsOut[5] = 0;

        if (Input.GetKey(KeyCode.Alpha2)) actionsOut[0] = 0;
        if (Input.GetKey(KeyCode.Alpha3)) actionsOut[1] = 0;
        if (Input.GetKey(KeyCode.Alpha4)) actionsOut[2] = 0;
        if (Input.GetKey(KeyCode.Alpha5)) actionsOut[3] = 0;
        if (Input.GetKey(KeyCode.Alpha6)) actionsOut[4] = 0;
        if (Input.GetKey(KeyCode.Alpha7)) actionsOut[5] = 0;

    }

   
    // ゴール到達回数、落下回数、時間切れ回数を算出
    public void evaluation(int goalCountEval, int episodeCount2)
    {
        // float successRate;
        int timeOut = 100 - (goalCountEval + fallCount); // 時間切れ回数を算出

        // successRate = (float)goalCountEval / (float)episodeCount2 * 100.0f;

        // Debug.Log("到達率[％]：" + successRate);
        Debug.Log("到達回数[回]：" + goalCountEval);
        Debug.Log("落下回数[回]：" + fallCount);
        Debug.Log("時間切れ回数[回]：" + timeOut);
    }

    // 瓦礫に衝突した回数（平均）を算出
    public void averageCollision(int[] collisionCount, int episodeCount2){
        float avgCollision = 0.0f;

        for(int i = 0; i < episodeCount2; i++){
            avgCollision += collisionCount[i];
            //Debug.Log("瓦礫に衝突した回数[回]：" + avgCollision);
        }
        // avgCollision = avgCollision / episodeCount2;

        // Debug.Log("瓦礫に衝突した回数[回]（平均）：" + avgCollision);
        Debug.Log("瓦礫に衝突した回数[回]：" + avgCollision);
    }


    // // 角度を-180度〜180度に変換
    // float AngleTranslate(float angle){
    //     if(angle > 180){
    //         angle = angle - 360.0f;
    //     }

    //     return angle;
    // }
}