using UnityEngine;
using Unity.Mathematics;
using System.Linq;
using System.IO;

public class AgentDDQN : ShipController
{
    // Senzory
    private const int num_of_states = Sensors.Radar.num_of_rays * Sensors.Radar.num_of_objs;
    private const int num_of_actions = 18;

    // Neuronove siete
    public NeuralNetwork QNet = new NeuralNetwork();
    public NeuralNetwork QTargetNet = new NeuralNetwork();
    private ReplayBufferItem replayBufferItem = null;
    private const int BATCH_SIZE = 32; // size of minibatch
    public float epLoss = 0.0f;

    // Epsilon
    private float epsilon = 1.0f;
    private const float epsilonMin = 0.01f;
    private float epsilon_decay = 0.995f;

    // Premenne herneho prostredia
    public bool testMode = false;

    public override void Start()
    {
        base.Start();

        // vytvor Q siet
        QNet.addLayer(100, NeuronLayerType.INPUT, num_of_states);
        QNet.addLayer(100, NeuronLayerType.HIDDEN, edge:QNet.neuronLayers[0]);
        QNet.addLayer(num_of_actions, NeuronLayerType.OUTPUT, edge:QNet.neuronLayers[1]);

        // vytvor Q_target siet
        QTargetNet.addLayer(100, NeuronLayerType.INPUT, num_of_states);
        QTargetNet.addLayer(100, NeuronLayerType.HIDDEN, edge:QTargetNet.neuronLayers[0]);
        QTargetNet.addLayer(num_of_actions, NeuronLayerType.OUTPUT, edge:QTargetNet.neuronLayers[1]);
    
        /*var lines = File.ReadAllLines("dataset.csv");
        var out_log = File.CreateText("log.csv");
        var num_of_patterns = lines.Length / 16;

        var x_train = new float[num_of_patterns][];
        var y_train = new float[num_of_patterns][][];
        for (int i = 0, k = 0; i < num_of_patterns; i++)
        {
            x_train[i] = new float[16];
            y_train[i] = new float[16][];

            for (int j = 0; j < 16; j++, k++)
            {
                y_train[i][j] = new float[2];

                var line = lines[k].Split(';');
                x_train[i][j] = float.Parse(line[0]);
                y_train[i][j][0] = float.Parse(line[1]);
                y_train[i][j][1] = float.Parse(line[2]);
                Debug.Log($"x: {x_train[i][j]}, y_0: {y_train[i][j][0]}, y_1: {y_train[i][j][1]}");
            }
        }        
        Debug.Log($"pocet_vzorov: {lines.Length}");

        for (int t = 0; t < 1000; t++)
        {
            for (int v = 0; v < num_of_patterns; v++)
            {
                float avgErr = 0f;

                for (int k = 0; k < 16; k++)
                {
                    var target = QNet.predict(new float[] { x_train[v][k] });
                
                    // Vypocet gradientu
                    QNet.gradient(new float[] { x_train[v][k] }, y_train[v][k]);

                    // Mean absolute error
                    var delta_0 = y_train[v][k][0] - target[0];
                    var delta_1 = y_train[v][k][1] - target[1];
                    float mean = (math.mul(delta_0, delta_0) + math.mul(delta_1, delta_1)) / 2f;
                    avgErr += mean;

                    if (t == 999)
                    {
                        out_log.WriteLine($"x:{x_train[v][k]};y_0:{target[0]};y_0_true:{y_train[v][k][0]};y_1:{target[1]};y_1_true:{y_train[v][k][1]}");
                        //Debug.Log($"y: {target[0]}, y_true: {y_train[v][k]}");
                    }
                }

                // Priemer chyby NN
                avgErr /= 16f;
                Debug.Log($"avgErr.QNet[{this.Nickname}][{v}] = {avgErr}");

                // pretrenuj siet na mini-balicek
                QNet.train();
            }
        }
        out_log.Close();
        UnityEditor.EditorApplication.isPlaying = false;*/

        // Nacitaj vahy zo subora ak existuje
        var str1 = Application.dataPath + "/DDQN_Weights_QNet.save";
        if (File.Exists(str1))
        {
            var json = JsonUtility.FromJson<JSON_NET>(File.ReadAllText(str1));
            for (int l = 0, k = 0; l < QNet.neuronLayers.Count; l++)
            {
                for (int m = 0; m < QNet.neuronLayers[l].neurons.Length; m++)
                {
                    for (int n = 0; n < QNet.neuronLayers[l].neurons[m].weights.Length; n++, k++)
                    {
                        QNet.neuronLayers[l].neurons[m].weights[n] = json.Weights[k];
                    }
                }
            }
            Debug.Log("QNet loaded from file.");
        }

        // init Q_target net
        for (int j = 0; j < this.QNet.neuronLayers.Count; j++)
        {
            for (int k = 0; k < this.QNet.neuronLayers[j].neurons.Length; k++)
            {
                this.QTargetNet.neuronLayers[j].neurons[k].soft_update(this.QNet.neuronLayers[j].neurons[k], 1.0f);
            }
        }

        // Nacitaj stav, t=0
        this.replayBufferItem = new ReplayBufferItem { State = Sensors.Radar.Scan(this.rigidbody2d.position, this.LookDirection, this) };
        this.reward = -0.015f;    // najkratsia cesta k planetam
        this.done = false;
    }

    public void Update()
    {
        // Ak nie je lod znicena = hra hru
        if (!IsDestroyed)
        {
            this.replayBufferItem.Action = this.Act(this.replayBufferItem.State, this.epsilon);
                
            // Nacitaj stav hry
            this.replayBufferItem.Next_state = Sensors.Radar.Scan(this.rigidbody2d.position, this.LookDirection, this);
            
            this.replayBufferItem.Reward = this.reward;
            this.score += this.replayBufferItem.Reward;
            this.replayBufferItem.Done = this.done;
            this.step += 1;
            this.levelBox.text = this.score.ToString("0.00");

            // Ak uz hrac nema zivoty ani palivo znici sa lod
            if (this.Health <= 0 || this.Fuel <= 0)
            {
                show_stat();

                // Destrukcia lode
                this.DestroyShip();
                
                this.epLoss /= (float)this.step;

                // Exploration/Exploitation parameter decay
                if (this.epsilon > epsilonMin)
                    this.epsilon *= this.epsilon_decay;  // od 100% nahody po 1%
            }
            // hrac obsadil vsetky planety
            else if (this.myPlanets.Count >= 4)
            {
                show_stat();

                Debug.Log("Vytaz!!!");
                WinnerShip();

                this.epLoss /= (float)this.step;
            }

            // Uloz udalost do bufferu
            ReplayBuffer.Add(this.replayBufferItem);

            // Pretrenuj hraca derivaciami
            if  (testMode == false && ReplayBuffer.Count >= BATCH_SIZE)
                epLoss += this.Training();

            this.replayBufferItem = new ReplayBufferItem { State = this.replayBufferItem.Next_state };
            this.reward = -0.015f;    // najkratsia cesta k planetam
            this.done = false;
        }
    }

    private float Training(float gamma=0.95f, float tau=0.01f)
    {
        float avgErr = 0f;

        // Ak je v zasobniku dost vzorov k uceniu
        var sample = ReplayBuffer.Sample(BATCH_SIZE);

        for (int i = 0; i < BATCH_SIZE; i++)
        {
            var target = QNet.predict(sample[i].State);
                
            // Non-terminal state
            if (sample[i].Done == false)
            {
                // Ziskaj next Q
                var q_next = QTargetNet.predict(sample[i].Next_state);
                // TD
                target[sample[i].Action] = sample[i].Reward + (gamma * q_next.Max());
            }
            // terminal state
            else
            {
                // TD
                target[sample[i].Action] = sample[i].Reward;
            }

            // get gradient
            QNet.gradient(sample[i].State, target);

            // Mean absolute error
            var delta = target[sample[i].Action] - QNet.neuronLayers[QNet.neuronLayers.Count - 1].neurons[sample[i].Action].output;
            avgErr += math.mul(delta, delta) / (float)num_of_actions;
        }

        // Training Q network - Mini-batch gradient descent with momentum           
        QNet.train();

        // Soft update Q Target network
        for (int j = 0; j < this.QTargetNet.neuronLayers.Count; j++)
        {
            for (int k = 0; k < this.QTargetNet.neuronLayers[j].neurons.Length; k++)
            {
                this.QTargetNet.neuronLayers[j].neurons[k].soft_update(this.QNet.neuronLayers[j].neurons[k], tau);
            }
        }

        // Priemer chyby NN
        avgErr /= (float)BATCH_SIZE;
        //Debug.Log($"avgErr.QNet[{this.Nickname}] = {avgErr}");

        return avgErr;
    }

    private int Act(float[] state, float epsilon)
    {
        int action;

        // Vyuzivaj naucenu vedomost
        if (testMode == false && UnityEngine.Random.Range(0.0f, 1.0f) < epsilon)
        {
            action = UnityEngine.Random.Range(0, num_of_actions);
        }
        else
        {
            var q = QNet.predict(state);
            action = System.Array.IndexOf(q, q.Max());
            //Debug.Log($"action = {action}");
        }
        
        switch (action)
        {
            // Up
            case 0:
                this.MoveShip(Vector2.up);
                break;
            // Up-Right
            case 1:
                this.MoveShip(ExtendedVector2.up_right);
                break;
            // Right
            case 2:
                this.MoveShip(Vector2.right);
                break;
            // Down-Right
            case 3:
                this.MoveShip(ExtendedVector2.down_right);
                break;
            // Down
            case 4:
                this.MoveShip(Vector2.down);
                break;
            // Down-Left
            case 5:
                this.MoveShip(ExtendedVector2.down_left);
                break;
            // Left
            case 6:
                this.MoveShip(Vector2.left);
                break;
            // Up-Left
            case 7:
                this.MoveShip(ExtendedVector2.up_left);
                break;
            // Up
            case 8:
                this.Fire();
                this.MoveShip(Vector2.up);
                break;
            // Up-Right
            case 9:
                this.Fire();
                this.MoveShip(ExtendedVector2.up_right);
                break;
            // Right
            case 10:
                this.Fire();
                this.MoveShip(Vector2.right);
                break;
            // Down-Right
            case 11:
                this.Fire();
                this.MoveShip(ExtendedVector2.down_right);
                break;
            // Down
            case 12:
                this.Fire();
                this.MoveShip(Vector2.down);
                break;
            // Down-Left
            case 13:
                this.Fire();
                this.MoveShip(ExtendedVector2.down_left);
                break;
            // Left
            case 14:
                this.Fire();
                this.MoveShip(Vector2.left);
                break;
            // Up-Left
            case 15:
                this.Fire();
                this.MoveShip(ExtendedVector2.up_left);
                break;
            case 16:
                // ... do nothing (no action)
                // Debug.Log($"nerobi nic");
                break;
            case 17:
                // ... only firing
                this.Fire();
                // Debug.Log($"iba striela");
                break;
        }

        return action;
    }

    private void show_stat()
    {
        if (this.myPlanets.Count > 1)
        {
            var strPlanets = string.Empty;
            this.myPlanets.ForEach(x => strPlanets += x.name + ", ");
            Debug.Log($"MyPlanets[{this.Nickname}]: {strPlanets}");
        }

        Debug.Log($"episode[{this.Nickname}]: {episode}");
        Debug.Log($"step[{this.Nickname}]: {step}");
        Debug.Log($"score[{this.Nickname}]: {score}");
        Debug.Log($"epsilon[{this.Nickname}]: {epsilon}");
        Debug.Log($"epLoss[{this.Nickname}]: {epLoss}");
        Debug.Log($"replay_buffer[{this.Nickname}]: {ReplayBuffer.Count}");
    }
}

