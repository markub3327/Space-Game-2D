using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;
using UnityEngine.UI;
using System.IO;

public class AgentDDQN : ShipController
{
    private const int num_of_frames = 4;

    private const int num_of_states = 34;

    private const int num_of_actions = 8;   // 4 + 4

    private bool isFirstFrame = true;

    public NeuralNetwork QNet = new NeuralNetwork();
    public NeuralNetwork QTargetNet = new NeuralNetwork();

    private ReplayBuffer replayMemory = new ReplayBuffer();
    private List<float> framesBuffer = new List<float>(num_of_frames * num_of_states);
    private ReplayBufferItem replayBufferItem = null;
    private const int BATCH_SIZE = 48; // size of minibatch

    // Epsilon
    private float epsilon = 0.5f;
    private float epsilonMin = 0.05f;
    private float epsilonMax = 1.0f;

    public float fitness = 0;

    public int num_of_episodes = 0;

    public TurretController[] turretControllers;

    // Meno lode
    public Text nameBox;
    public Text levelBox;

    // Stavy t-1 (pre ziskanie odmeny za tah v hre)
    private int Health_old;
    private float Fuel_old;
    private int Ammo_old;
    private int num_of_planets_old;

    public bool presiel100Epizod = false;

    public bool testMode = false;

    public override void Start()
    {
        base.Start();

        QNet.CreateLayer(NeuronLayerType.INPUT);    // 1st hidden
        QNet.CreateLayer(NeuronLayerType.HIDDEN);   // 2nd hidden
        QNet.CreateLayer(NeuronLayerType.OUTPUT);   // Output

        QTargetNet.CreateLayer(NeuronLayerType.INPUT);    // 1st hidden
        QTargetNet.CreateLayer(NeuronLayerType.HIDDEN);   // 2nd hidden
        QTargetNet.CreateLayer(NeuronLayerType.OUTPUT);   // Output

        QNet.SetEdge(QNet.neuronLayers[1], QNet.neuronLayers[0]);
        QNet.SetEdge(QNet.neuronLayers[2], QNet.neuronLayers[1]);
        QNet.SetBPGEdge(QNet.neuronLayers[0], QNet.neuronLayers[1]);
        QNet.SetBPGEdge(QNet.neuronLayers[1], QNet.neuronLayers[2]);

        QTargetNet.SetEdge(QTargetNet.neuronLayers[1], QTargetNet.neuronLayers[0]);
        QTargetNet.SetEdge(QTargetNet.neuronLayers[2], QTargetNet.neuronLayers[1]);
        QTargetNet.SetBPGEdge(QTargetNet.neuronLayers[0], QTargetNet.neuronLayers[1]);
        QTargetNet.SetBPGEdge(QTargetNet.neuronLayers[1], QTargetNet.neuronLayers[2]);

        QNet.neuronLayers[0].CreateNeurons((num_of_frames * num_of_states), (num_of_frames * num_of_states));
        QNet.neuronLayers[1].CreateNeurons(48); // 16, 24
        QNet.neuronLayers[2].CreateNeurons(num_of_actions);

        QTargetNet.neuronLayers[0].CreateNeurons((num_of_frames * num_of_states), (num_of_frames * num_of_states));
        QTargetNet.neuronLayers[1].CreateNeurons(48);   // 16, 24
        QTargetNet.neuronLayers[2].CreateNeurons(num_of_actions);

        this.nameBox.text = this.name;
        this.levelBox.text = ((int)this.fitness).ToString();

        // Nacitaj stav, t=0
        for (int i = 0; i < num_of_frames; i++)
        {
            this.GetState();
        }
        this.replayBufferItem = new ReplayBufferItem { State = this.framesBuffer.ToArray() };

        // inicializuj pocitadlo pre t-1
        this.Health_old = this.Health;
        this.Ammo_old = this.Ammo;
        this.Fuel_old  = this.Fuel;
        this.num_of_planets_old = 0;

        // Nacitaj vahy zo subora ak existuje
        var str1 = Application.dataPath + "/DDQN_Weights_QNet.save";
        var str2 = Application.dataPath + "/DDQN_Weights_QTargetNet.save";
        if (File.Exists(str1))
        {
            var json = JsonUtility.FromJson<JSON_NET>(File.ReadAllText(str1));
            for (int i = 0, k = 0, l = 0; i < this.QNet.neuronLayers.Count; i++)
            {
                for (int j = 0; j < this.QNet.neuronLayers[i].Weights.Count; j++, k++)
                {
                    this.QNet.neuronLayers[i].Weights[j] = json.Weights[k];
                }
                for (int j = 0; j < this.QNet.neuronLayers[i].Neurons.Count; j++, l++)
                {
                    var neuron = this.QNet.neuronLayers[i].Neurons[j];
                    this.QNet.neuronLayers[i].Neurons[j] = neuron;
                }
            }
            Debug.Log("QNet loaded from file.");
        }
        if (File.Exists(str2))
        {
            var json = JsonUtility.FromJson<JSON_NET>(File.ReadAllText(str2));
            for (int i = 0, k = 0, l = 0; i < this.QTargetNet.neuronLayers.Count; i++)
            {
                for (int j = 0; j < this.QTargetNet.neuronLayers[i].Weights.Count; j++, k++)
                {
                    this.QTargetNet.neuronLayers[i].Weights[j] = json.Weights[k];
                }
                for (int j = 0; j < this.QTargetNet.neuronLayers[i].Neurons.Count; j++, l++)
                {
                    var neuron = this.QTargetNet.neuronLayers[i].Neurons[j];
                    this.QTargetNet.neuronLayers[i].Neurons[j] = neuron;
                }
            }
            Debug.Log("QTargetNet loaded from file.");
        }
    }

    public override void Update()
    {
        // Ak nie je lod znicena = hra hru
        if (!IsDestroyed)
        {
            // Ide o prvy obraz? => konaj akciu
            if (isFirstFrame)
            {
                this.replayBufferItem.Action = this.Act(this.replayBufferItem.State, epsilon);        
                                
                isFirstFrame = false;
            }
            else    // Ide o druhy obraz? => ziskaj reakciu za akciu a uloz ju
            {
                // Ak uz hrac nema zivoty znici sa lod
                if (this.Health <= 0 || this.Fuel <= 0)
                {
                    DestroyShip();
                    if (num_of_episodes > 0 && num_of_episodes % 100 == 0) this.presiel100Epizod = true; // po 10000 epizodach vygeneruje 1000 generacii populacie
                    if (num_of_episodes > 100000) 
                    { 
                        #if UNITY_EDITOR
                            UnityEditor.EditorApplication.isPlaying = false;
                        #else
                            Application.Quit();
                        #endif
                    }
                    num_of_episodes++;
                }
                else if (this.myPlanets.Count > this.num_of_planets_old)
                {
                    WinnerShip();             
                }

                replayBufferItem.Next_state = this.GetState();
                replayBufferItem.Done = this.IsDestroyed;
                replayBufferItem.Reward = this.GetReward();
                this.replayMemory.Add(replayBufferItem);    // pridaj do pamate trenovacich dat

                this.Health_old = this.Health;
                this.Ammo_old = this.Ammo;
                this.Fuel_old  = this.Fuel;
                this.num_of_planets_old = this.myPlanets.Count;
                this.replayBufferItem = new ReplayBufferItem { State = replayBufferItem.Next_state };
                
                isFirstFrame = true;                
            }            
        }
        else    // Ak je lod znicena = cas na preucenie siete (nove vedomosti)
        {            
            if (respawnTimer <= 0.0f) 
            {
                RespawnShip();

                // inicializuj pocitadlo pre t-1
                this.Health_old = this.Health;
                this.Ammo_old = this.Ammo;
                this.Fuel_old  = this.Fuel;
                this.num_of_planets_old = this.myPlanets.Count;
            }
            else if (respawnTimer == timeRespawn)
            {
                // Ak je v zasobniku dost vzorov k uceniu
                if (this.replayMemory.Count >= BATCH_SIZE && !testMode)
                {
                    // Vyber vzorku a natrenuj sa
                    this.Training();
                }
            }
                        
            respawnTimer -= Time.deltaTime;
        }

        // Exploration/Exploitation parameter changed
        //this.epsilon = Mathf.Clamp((epsilon * 0.999999f), 0.01f, 1.0f);  // od 100% nahody po 1%
        this.epsilon = math.max(epsilonMin, epsilonMax - (epsilonMax-epsilonMin) * this.num_of_episodes/100000f); // Decaying policy with more steps
    }

    private int Act(float[] state, float epsilon=0.20f)
    {
        int action;

        // Vyuzivaj naucenu vedomost
        if (UnityEngine.Random.Range(0.0f, 1.0f) > epsilon || testMode)
        {
            QNet.Run(state);
            var q = GetMaxQ(QNet.neuronLayers[2].Neurons, out action);
            if (num_of_episodes % 100 == 0)
                Debug.Log($"Qval = {q}, {action}");
        }
        else    // Skumaj prostredie
        {
            action = UnityEngine.Random.Range(0, num_of_actions);
        }
        
        //Debug.Log($"eps = {epsilon}");

        // Vykonaj akciu => koordinacia pohybu lode
        switch (action)
        {
            case 0:
                this.MoveShip(Vector2.up);
                break;
            case 1:
                this.MoveShip(Vector2.left);
                break;
            case 2:
                this.MoveShip(Vector2.down);
                break;
            case 3:
                this.MoveShip(Vector2.right);
                break;
            case 4:
                {
                    this.MoveShip(Vector2.up);
                    foreach (var turret in turretControllers)
                    {
                        if (turret != null)                    
                            turret.Fire();
                    }
                    break;
                }
            case 5:
                {
                    this.MoveShip(Vector2.left);
                    foreach (var turret in turretControllers)
                    {
                        if (turret != null)                    
                            turret.Fire();
                    }
                    break;
                }
            case 6:
                {
                    this.MoveShip(Vector2.down);
                    foreach (var turret in turretControllers)
                    {
                        if (turret != null)                    
                            turret.Fire();
                    }
                    break;
                }
            case 7:
                {
                    this.MoveShip(Vector2.right);
                    foreach (var turret in turretControllers)
                    {
                        if (turret != null)                    
                            turret.Fire();
                    }
                    break;
                }
        }

        return action;
    }

    private float GetMaxQ(List<Neuron> qValues, out int action)
    {
        action = 0;

        for (int i = 0; i < qValues.Count; i++)
        {
            if (qValues[action].output < qValues[i].output)
                action = i;
        }
        return qValues[action].output;
    }

    private void Training(float gamma=0.990f)   
    {        
        var sample = replayMemory.Sample(BATCH_SIZE);
        float avgErr1 = 0;
        float avgErr2 = 0;

        // clean old deltaW
        for (int i = 0; i < this.QNet.neuronLayers.Count; i++)
        {
            for (int j = 0; j < this.QNet.neuronLayers[i].Weights.Count; j++)
            {
                this.QNet.neuronLayers[i].deltaWeights[j]       = 0f; 
                this.QTargetNet.neuronLayers[i].deltaWeights[j] = 0f;
            }
        }

        for (int i = 0; i < BATCH_SIZE; i++)
        {
            float[] targets = new float[] { 0, 0, 0, 0, 0, 0, 0, 0 };
                        
            if (sample[i].Done == false)
            {
                // Ziskaj najvyssie Q
                QNet.Run(sample[i].Next_state);
                // Ziskaj najvyssie next Q
                QTargetNet.Run(sample[i].Next_state);

                // Vyber nasledujuce Q 
                var next_Q = math.min(
                    GetMaxQ(QNet.neuronLayers[2].Neurons, out int a1),
                    GetMaxQ(QTargetNet.neuronLayers[2].Neurons, out int a2)
                );

                // TD
                targets[sample[i].Action] = sample[i].Reward + (gamma * next_Q);
            }
            else
            {
                // TD
                targets[sample[i].Action] = sample[i].Reward;
            }
            
            QNet.Run(sample[i].State);
            QTargetNet.Run(sample[i].State);

            QNet.Training(sample[i].State, targets);
            QTargetNet.Training(sample[i].State, targets);
            
            if (num_of_episodes % 100 == 0)
            {        
                var e1 = (targets[sample[i].Action] - QNet.neuronLayers[2].Neurons[sample[i].Action].output);
                avgErr1 += e1 * e1;

                var e2 = (targets[sample[i].Action] - QTargetNet.neuronLayers[2].Neurons[sample[i].Action].output);
                avgErr2 += e2 * e2;
            }
        }

        if (num_of_episodes % 100 == 0)
        {        
            // Kvadraticky priemer chyby NN
            avgErr1 /= (float)BATCH_SIZE;
            avgErr1 = math.sqrt(avgErr1);
            QNet.errorList.Add(avgErr1);

            avgErr2 /= (float)BATCH_SIZE;
            avgErr2 = math.sqrt(avgErr2);
            QTargetNet.errorList.Add(avgErr2);
        
            Debug.Log($"avgErr.QNet[{this.name}] = {avgErr1}");
            Debug.Log($"avgErr.QTargetNet[{this.name}] = {avgErr2}");
            Debug.Log($"epsilon[{this.name}] = {epsilon}");
            Debug.Log($"episode[{this.name}] = {num_of_episodes}");
        }
    }

    private float[] GetState()
    {
        var radarResult = Sensors.Radar.Scan(this.rigidbody2d.position, this.LookDirection, this.transform);
        float[] state = new float[num_of_states];  // 2 + 32 = 34

        // Udaje o polohe
        state[0] = this.rigidbody2d.position.x / 20.0f;
        state[1] = this.rigidbody2d.position.y / 20.0f;

        // Udaje o objektoch v okoli lodi
        for (int i = 0, j = 2; i < 16; i++, j+=2)
        {
            if (radarResult[i] != null)
            {
                state[j] = (float)radarResult[i].Value.transform.tag.GetHashCode()/(float)int.MaxValue;
                state[j+1] = radarResult[i].Value.distance / Sensors.Radar.max_distance;
            }
            else
            {
                state[j] = 0f;
                state[j+1] = 0f;
            }
        }

        // LIFO
        if (this.framesBuffer.Count >= (num_of_frames * num_of_states))
            this.framesBuffer.RemoveRange(0, num_of_states);
        this.framesBuffer.AddRange(state);

        return this.framesBuffer.ToArray();
    }

    private float GetReward()
    {
        float reward = 0.0f;

        // odmeny za profil hraca (zivoty, municia, palivo, pocet vlastnenych planet)
        reward += ((float)(this.Health - this.Health_old) / (float)ShipController.maxHealth);
        reward += ((this.Fuel - this.Fuel_old) / (float)ShipController.maxFuel);
        reward += ((float)(this.Ammo - this.Ammo_old) / (float)ShipController.maxAmmo);
        reward += ((float)(this.myPlanets.Count - this.num_of_planets_old) / 1f);
        reward /= 4.0f;

        this.fitness += reward;
        this.levelBox.text = this.fitness.ToString("0.000");

        if (this.replayBufferItem.Done && (num_of_episodes % 100 == 0))
        {
            Debug.Log($"health[{this.name}] = {this.Health}");
            Debug.Log($"fuel[{this.name}] = {this.Fuel}");
            Debug.Log($"ammo[{this.name}] = {this.Ammo}");
            Debug.Log($"reward[{this.name}] = {reward}");
            Debug.Log($"done[{this.name}] = {this.replayBufferItem.Done}");
            Debug.Log($"fitness = {this.fitness}");

            var strPlanets = string.Empty;
            foreach (var p in this.myPlanets)
            {
                strPlanets += p.name + ", ";
            }
            Debug.Log($"MyPlanets: {strPlanets}");           
        }

        return reward;
    }
}

public class ReplayBuffer
{
    private Unity.Mathematics.Random randGen = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);

    public List<ReplayBufferItem> items;

    private const int max_count = 20000;

    public ReplayBuffer()
    {
        this.items = new List<ReplayBufferItem>(max_count); // Ako ma standardny smart TV :D
    }

    public void Add(ReplayBufferItem item)
    {
        // LIFO
        if (this.Count >= max_count)    
            this.items.RemoveAt(0);
        this.items.Add(item);        
    }

    public ReplayBufferItem[] Sample(int batch_size)
    {
        List<ReplayBufferItem> buff = new List<ReplayBufferItem>(batch_size);

        for (int i = 0; i < batch_size; i++)
        {
            int idx;
            do {
                idx = randGen.NextInt(0,this.Count);
            } while (buff.Contains(this[idx]));
            buff.Add(this[idx]);           
        }

        return buff.ToArray();
    }

    public ReplayBufferItem this[int index]
    {
        get { return items[index]; }
        set { items[index] = value; }
    }
    
    public int Count { get { return items.Count; } }
}

public class ReplayBufferItem
{
    public float[]       State;
    public int          Action;
    public float        Reward;
    public float[]  Next_state;
    public bool           Done;
}

