using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;
using UnityEngine.UI;
using System.IO;

public class AgentDDQN : ShipController
{
    private const int num_of_states = 768;

    private const int num_of_actions = 16;

    private bool isFirstFrame = true;

    public NeuralNetwork QNet = new NeuralNetwork();
    public NeuralNetwork QTargetNet = new NeuralNetwork();

    private ReplayBuffer replayMemory = new ReplayBuffer();
    private ReplayBufferItem replayBufferItem = null;
    private const int BATCH_SIZE = 48; // size of minibatch

    // Frame buffer
    //private const int num_of_frames = 4;
    //private List<float> frameBuffer = new List<float>(num_of_frames * num_of_states);

    // Epsilon
    private float epsilon = 1.0f;
    private const float epsilonMin = 0.01f;
    private float epsilon_decay;

    public float fitness = 0;
    public int num_of_episodes = 0;

    public TurretController[] turretControllers;

    // Meno lode
    public Text nameBox;
    public Text levelBox;

    public bool presiel50Epizod = false;
    public bool testMode = false;

    private float score = 0;
    private float score_old;

    public override void Start()
    {
        base.Start();

        epsilon_decay = (epsilon - epsilonMin) / 10000f;

        QNet.CreateLayer(NeuronLayerType.INPUT);    // Input layer
        QNet.CreateLayer(NeuronLayerType.HIDDEN);   // 1st hidden
        QNet.CreateLayer(NeuronLayerType.OUTPUT);   // Output

        QTargetNet.CreateLayer(NeuronLayerType.INPUT);    // Input layer
        QTargetNet.CreateLayer(NeuronLayerType.HIDDEN);   // 1st hidden
        QTargetNet.CreateLayer(NeuronLayerType.OUTPUT);   // Output

        QNet.SetEdge(QNet.neuronLayers[1], QNet.neuronLayers[0]);
        QNet.SetEdge(QNet.neuronLayers[2], QNet.neuronLayers[1]);
        QNet.SetBPGEdge(QNet.neuronLayers[0], QNet.neuronLayers[1]);
        QNet.SetBPGEdge(QNet.neuronLayers[1], QNet.neuronLayers[2]);

        QTargetNet.SetEdge(QTargetNet.neuronLayers[1], QTargetNet.neuronLayers[0]);
        QTargetNet.SetEdge(QTargetNet.neuronLayers[2], QTargetNet.neuronLayers[1]);
        QTargetNet.SetBPGEdge(QTargetNet.neuronLayers[0], QTargetNet.neuronLayers[1]);
        QTargetNet.SetBPGEdge(QTargetNet.neuronLayers[1], QTargetNet.neuronLayers[2]);

        //var num_of_inputs = num_of_states * num_of_frames;
        QNet.neuronLayers[0].CreateNeurons(num_of_states, 48);
        QNet.neuronLayers[1].CreateNeurons(48); // 24, 32, 48, 64(lode sa po 2000 iteraciach skoro nehybu), 128(stal na mieste), 256(letel k okrajom Vesmiru)
        QNet.neuronLayers[2].CreateNeurons(num_of_actions);

        QTargetNet.neuronLayers[0].CreateNeurons(num_of_states, 48);
        QTargetNet.neuronLayers[1].CreateNeurons(48); // 24, 32, 48, 64(lode sa po 2000 iteraciach skoro nehybu), 128(stal na mieste), 256(letel k okrajom Vesmiru)
        QTargetNet.neuronLayers[2].CreateNeurons(num_of_actions);
        
        // Init Player info panel
        this.nameBox.text = this.name;
        this.levelBox.text = ((int)this.fitness).ToString();

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

        // Nacitaj stav, t=0
        this.replayBufferItem = new ReplayBufferItem { State = this.GetState() };
        this.score_old = GetScore();
    }

    public void Update()
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
                // Nacitaj stav hry
                replayBufferItem.Next_state = this.GetState();

                // Ak uz hrac nema zivoty ani palivo znici sa lod
                if (this.Health <= 0 || this.Fuel <= 0)
                {
                    if (this.myPlanets.Count > 0)
                    {
                        var strPlanets = string.Empty;
                        this.myPlanets.ForEach(x => strPlanets += x.name + ", ");
                        Debug.Log($"MyPlanets[{this.name}]: {strPlanets}");
                    }

                    // Destrukcia lode
                    DestroyShip();
                    this.score_old = GetScore();

                    // Terminalny stav - koniec epizody
                    replayBufferItem.Done = true;
                    replayBufferItem.Reward = 0.0f;     // smrtou straca vsetky ziskane body
                }
                else    // pokracuje v hre
                {
                    // Neterminalny stav - pokracuje v hre
                    replayBufferItem.Done = false;
                    replayBufferItem.Reward = GetReward();
                    this.levelBox.text = this.score.ToString("0.000");
                }
                
                // Vypocet fitness pre Geneticky algoritmus vyberu jedincov
                if (!this.presiel50Epizod)
                {
                    this.fitness += replayBufferItem.Reward;
                }

                // Uloz udalost do bufferu
                this.replayMemory.Add(replayBufferItem);    // pridaj do pamate trenovacich dat

                // Uchovaj stav predoslej hry
                this.replayBufferItem = new ReplayBufferItem { State = replayBufferItem.Next_state };
                
                // Prepni na prvy obraz (akcia lode)
                this.isFirstFrame = true;
            }                        
        }
        else    // Ak je lod znicena = cas na preucenie siete (nove vedomosti)
        {            
            if (this.presiel50Epizod == false)
            {
                if (num_of_episodes > 0 && (num_of_episodes % 50 == 0))
                {
                    this.presiel50Epizod = true; // po 10000 epizodach vygeneruje 1000 generacii populacie 
      
                    Debug.Log($"epsilon[{this.name}] = {epsilon}");
                    Debug.Log($"episode[{this.name}] = {num_of_episodes}");
                }

                // Exploration/Exploitation parameter changed
                if (this.epsilon > epsilonMin)
                    this.epsilon -= this.epsilon_decay;  // od 100% nahody po 1%

                num_of_episodes++;
            }
            if (num_of_episodes > 20000) 
            { 
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
            }

            // Pretrenuj hraca derivaciami
            this.Training();
            
            // Respawn
            RespawnShip();
        }
    }

    private int Act(float[] state, float epsilon=0.20f)
    {
        int action;

        // Vyuzivaj naucenu vedomost
        if (UnityEngine.Random.Range(0.0f, 1.0f) > epsilon || testMode)
        {
            QNet.Run(state);
            GetMaxQ(QNet.neuronLayers[2].Neurons, out action);
        }
        else    // Skumaj prostredie
        {
            action = UnityEngine.Random.Range(0, num_of_actions);
        }
        
        // Vykonaj akciu => koordinacia pohybu lode
        switch (action)
        {
            case 0:         // Up
                this.MoveShip(Vector2.up);
                break;
            case 1:         // Up-Right
                this.MoveShip(new Vector2(1f, 1f));
                break;
            case 2:         // Right
                this.MoveShip(Vector2.right);
                break;
            case 3:         // Down-Right
                this.MoveShip(new Vector2(1f, -1f));
                break;
            case 4:         // Down
                this.MoveShip(Vector2.down);
                break;
            case 5:         // Down-Left
                this.MoveShip(new Vector2(-1f, -1f));
                break;
            case 6:         // Left
                this.MoveShip(Vector2.left);
                break;
            case 7:         // Left-Up
                this.MoveShip(new Vector2(-1f, 1f));
                break;            
            case 8:         // Up
                this.MoveShip(Vector2.up);
                this.Fire();
                break;
            case 9:         // Up-Right
                this.MoveShip(new Vector2(1f, 1f));
                this.Fire();
                break;
            case 10:         // Right
                this.MoveShip(Vector2.right);
                this.Fire();
                break;
            case 11:         // Down-Right
                this.MoveShip(new Vector2(1f, -1f));
                this.Fire();
                break;
            case 12:         // Down
                this.MoveShip(Vector2.down);
                this.Fire();
                break;
            case 13:         // Down-Left
                this.MoveShip(new Vector2(-1f, -1f));
                this.Fire();
                break;
            case 14:         // Left
                this.MoveShip(Vector2.left);
                this.Fire();
                break;
            case 15:         // Left-Up
                this.MoveShip(new Vector2(-1f, 1f));
                this.Fire();
                break;
        }

        return action;
    }

    private void Fire()
    {
        foreach (var turret in turretControllers)
        {
            if (turret != null)                    
            turret.Fire();
        }
    }

    private float GetMaxQ(List<Neuron> qValues, out int action)
    {
        action = 0;

        for (int i = 1; i < qValues.Count; i++)
        {
            if (qValues[action].output < qValues[i].output)
                action = i;
        }
        return qValues[action].output;
    }

    private void Training(float gamma=0.90f, float tau=0.001f)
    {        
        // Ak je v zasobniku dost vzorov k uceniu
        if (this.replayMemory.Count >= BATCH_SIZE && !testMode)
        {
            var sample = replayMemory.Sample(BATCH_SIZE);
            float avgErr = 0;
        
            for (int i = 0; i < BATCH_SIZE; i++)
            {
                float[] targets = new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

                // Non-terminal state     
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
                // terminal state
                else
                {
                    // TD
                    targets[sample[i].Action] = sample[i].Reward;
                }

                // Training Q network            
                QNet.Run(sample[i].State);
                QNet.Training(sample[i].State, targets);

                // Soft update Q Target network
                for (int j = 0; j < this.QNet.neuronLayers.Count; j++)
                {
                    for (int k = 0; k < this.QNet.neuronLayers[j].Weights.Count; k++)
                    {
                        this.QTargetNet.neuronLayers[j].Weights[k] = tau*this.QNet.neuronLayers[j].Weights[k] + (1.0f-tau)*this.QTargetNet.neuronLayers[j].Weights[k];                    
                    }
                }
            
                if (presiel50Epizod)
                    avgErr += math.abs(targets[sample[i].Action] - QNet.neuronLayers[2].Neurons[sample[i].Action].output);
            }
                 
            if (presiel50Epizod)
            {
                // Kvadraticky priemer chyby NN
                avgErr /= (float)BATCH_SIZE;
                QNet.errorList.Add(avgErr);
                Debug.Log($"avgErr.QNet[{this.name}] = {avgErr}");
            }
        }
    }

    private float[] GetState()
    {
        float[] state = new float[num_of_states];       // array of zeros
        var radarResult = Sensors.Radar.Scan(this.rigidbody2d.position, this.LookDirection, this.transform);
        
        // 736 = 23x32
        // Udaje o objektoch v okoli lodi
        for (int i = 0, idx = 0; i < 32; i++, idx+=24)
        {
            // ak luc narazil na objekt hry
            if (radarResult[i] != null)
            {
                // TAG = VSTUP do NN
                switch (radarResult[i].Value.transform.tag)
                {
                    case "Asteroid":
                        state[idx + 23] = GetDistance(radarResult[i].Value.distance);
                        break;
                    case "Planet": // staticke orientacne body
                        var planet = radarResult[i].Value.transform.GetComponent<PlanetController>();                        
                        switch (planet.name)
                        {
                            case "Saturn":
                                if (this.myPlanets.Contains(planet))     
                                    // moja planeta
                                    state[idx + 22] = GetDistance(radarResult[i].Value.distance); 
                                else if (planet.OwnerPlanet != null)
                                    // planeta uz vlastnena
                                    state[idx + 21] = GetDistance(radarResult[i].Value.distance); 
                                else
                                    // planeta bez vlastnika
                                    state[idx + 20] = GetDistance(radarResult[i].Value.distance); 
                                break;
                            case "Earth":
                                if (this.myPlanets.Contains(planet))     
                                    // moja planeta
                                    state[idx + 19] = GetDistance(radarResult[i].Value.distance); 
                                else if (planet.OwnerPlanet != null)
                                    // planeta uz vlastnena
                                    state[idx + 18] = GetDistance(radarResult[i].Value.distance); 
                                else
                                    // planeta bez vlastnika
                                    state[idx + 17] = GetDistance(radarResult[i].Value.distance); 
                                break;
                            case "Mars":
                                if (this.myPlanets.Contains(planet))     
                                    // moja planeta
                                    state[idx + 16] = GetDistance(radarResult[i].Value.distance); 
                                else if (planet.OwnerPlanet != null)
                                    // planeta uz vlastnena
                                    state[idx + 15] = GetDistance(radarResult[i].Value.distance); 
                                else
                                    // planeta bez vlastnika
                                    state[idx + 14] = GetDistance(radarResult[i].Value.distance); 
                                break;
                            case "Jupiter":
                                if (this.myPlanets.Contains(planet))     
                                    // moja planeta
                                    state[idx + 13] = GetDistance(radarResult[i].Value.distance); 
                                else if (planet.OwnerPlanet != null)
                                    // planeta uz vlastnena
                                    state[idx + 12] = GetDistance(radarResult[i].Value.distance); 
                                else
                                    // planeta bez vlastnika
                                    state[idx + 11] = GetDistance(radarResult[i].Value.distance); 
                                break;
                            default:
                                Debug.Log($"obj.tag = {planet.name}, dist = {radarResult[i].Value.distance}");
                                break;
                        }
                        break;
                    case "Moon":
                        state[idx + 10] = GetDistance(radarResult[i].Value.distance);
                        break;
                    case "Star":
                        state[idx + 9] = GetDistance(radarResult[i].Value.distance); 
                        break;
                     case "Nebula": // staticke orientacne body
                        switch (radarResult[i].Value.transform.name)
                        {
                            case "Nebula-Red":
                                state[idx + 8] = GetDistance(radarResult[i].Value.distance); 
                                break;
                            case "Nebula-Blue":
                                state[idx + 7] = GetDistance(radarResult[i].Value.distance); 
                                break;
                            case "Nebula-Silver":
                                state[idx + 6] = GetDistance(radarResult[i].Value.distance); 
                                break;
                            default:
                                Debug.Log($"obj.tag = {radarResult[i].Value.transform.name}, dist = {radarResult[i].Value.distance}");
                                break;
                        }                        
                        break;
                    case "Health":
                        state[idx + 5] = GetDistance(radarResult[i].Value.distance); 
                        break;
                    case "Ammo":
                        state[idx + 4] = GetDistance(radarResult[i].Value.distance); 
                        break;
                    case "Projectile":
                        var projectile = radarResult[i].Value.transform.GetComponent<Projectile>();
                        if (projectile.firingShip == this)
                            // projektil patri mne (neutoci)
                            state[idx + 3] = GetDistance(radarResult[i].Value.distance);
                        else
                            // projektil patri inej lodi (utoci)
                            state[idx + 2] = GetDistance(radarResult[i].Value.distance);  
                        break;
                    case "Player":
                        state[idx + 1] = GetDistance(radarResult[i].Value.distance); 
                        break;    
                    case "Space":
                        state[idx] = GetDistance(radarResult[i].Value.distance); 
                        break;    
                    default:
                        Debug.Log($"obj.tag = {radarResult[i].Value.transform.tag}, dist = {radarResult[i].Value.distance}");
                        break;
                }
            }
        }

        return state;
    }

    private float GetDistance(float d)
    {
        // Normalizovany vstup
        return (Sensors.Radar.max_distance - d) / Sensors.Radar.max_distance;
    }

    private float GetReward()
    {
        float reward;

        this.score = GetScore();
        reward = score - this.score_old;
        this.score_old = this.score;

        return reward;
    }

    private float GetScore()
    {
        float avg;

        // Vypocitaj skore hraca
        avg  = (float)this.Health          * 0.15f;
        avg += (float)this.Fuel            * 0.15f;
        avg += (float)this.Ammo            * 0.10f;
        avg += (float)this.myPlanets.Count * 0.60f;

        return avg;
    }
}

public class ReplayBuffer
{
    private const int max_count = 50000;

    private Unity.Mathematics.Random randGen = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);

    public List<ReplayBufferItem> items;

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
            var idx = randGen.NextInt(0,this.Count);
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

