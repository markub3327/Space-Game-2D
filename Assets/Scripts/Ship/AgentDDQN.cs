using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;
using UnityEngine.UI;
using System.IO;

public class AgentDDQN : ShipController
{
    private const int num_of_states = 176;

    private const int num_of_actions = 16;

    private bool isFirstFrame = true;

    public NeuralNetwork QNet = new NeuralNetwork();
    public NeuralNetwork QTargetNet = new NeuralNetwork();

    private ReplayBuffer replayMemory = new ReplayBuffer();
    private ReplayBufferItem replayBufferItem = null;
    private const int BATCH_SIZE = 32; // size of minibatch

    // Frame buffer
    //private const int num_of_frames = 4;
    //private List<float> frameBuffer = new List<float>(num_of_frames * num_of_states);

    // Epsilon
    private float epsilon = 1.0f;
    private float epsilonMin = 0.01f;

    public float fitness = 0;
    public int num_of_episodes = 0;

    public TurretController[] turretControllers;

    // Meno lode
    public Text nameBox;
    public Text levelBox;

    public bool presiel10Epizod = false;
    public bool testMode = false;

    public GeneticsAlgorithm myGA;

    public override void Start()
    {
        base.Start();

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
        QNet.neuronLayers[0].CreateNeurons(num_of_states, 32);
        QNet.neuronLayers[1].CreateNeurons(32); // 24, 32, 48, 64(lode sa po 2000 iteraciach skoro nehybu), 128(stal na mieste), 256(letel k okrajom Vesmiru)
        QNet.neuronLayers[2].CreateNeurons(num_of_actions);

        QTargetNet.neuronLayers[0].CreateNeurons(num_of_states, 32);
        QTargetNet.neuronLayers[1].CreateNeurons(32); // 24, 32, 48, 64(lode sa po 2000 iteraciach skoro nehybu), 128(stal na mieste), 256(letel k okrajom Vesmiru)
        QTargetNet.neuronLayers[2].CreateNeurons(num_of_actions);
        
        // Init Player info panel
        this.nameBox.text = this.name;
        this.levelBox.text = ((int)this.fitness).ToString();

        // Nacitaj stav, t=0
        //this.GetState();
        //this.GetState();
        //this.GetState();
        this.replayBufferItem = new ReplayBufferItem { State = this.GetState() };

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
                // Ak uz hrac nema zivoty znici sa lod
                if (this.Health <= 0 || this.Fuel <= 0)
                {
                    this.hasNewPlanet = false;

                    if (num_of_episodes > 0 && num_of_episodes % 10 == 0)
                    {
                        this.presiel10Epizod = true; // po 10000 epizodach vygeneruje 1000 generacii populacie
                        
                        Debug.Log($"epsilon[{this.name}] = {epsilon}");
                        Debug.Log($"episode[{this.name}] = {num_of_episodes}");                        
                    }
                    if (num_of_episodes > 20000) 
                    { 
                        #if UNITY_EDITOR
                            UnityEditor.EditorApplication.isPlaying = false;
                        #else
                            Application.Quit();
                        #endif
                    }
                    num_of_episodes++;
                    
                    // Destrukcia lode
                    DestroyShip();

                    // Terminalny stav - koniec epizody
                    replayBufferItem.Done = true;
                    replayBufferItem.Reward = 0.0f;     // smrtou straca vsetky ziskane body
                }
                else if (this.hasNewPlanet)
                {
                    this.hasNewPlanet = false;
                    
                    // Vitaz musi ziskat vsetky planety
                    if (this.myPlanets.Count == 4)
                        WinnerShip();

                    var strPlanets = string.Empty;
                    this.myPlanets.ForEach(x => strPlanets += x.name + ", ");
                    Debug.Log($"MyPlanets[{this.name}]: {strPlanets}");

                    replayBufferItem.Done = false;
                    replayBufferItem.Reward = +1.0f;
                } 
                else
                {
                    // Neterminalny stav - hlada cestu
                    replayBufferItem.Done = false;
                    // Vazeny priemer - hodnotenie stavu hraca podla poctu planet, zivotov, municie a paliva
                    replayBufferItem.Reward = ((float)this.Health / (float)ShipController.maxHealth) * 0.16f;
                    replayBufferItem.Reward += ((float)this.Fuel / (float)ShipController.maxFuel) * 0.16f;
                    replayBufferItem.Reward += ((float)this.Ammo / (float)ShipController.maxAmmo) * 0.04f;
                }

                // Kym nepresiel 10 epizod scitavaj odmeny do celkoveho skore
                if (this.presiel10Epizod == false)
                {
                    this.fitness += replayBufferItem.Reward;
                    this.levelBox.text = ((int)this.fitness).ToString();
                }

                //Debug.Log($"frame.count = {frameBuffer.Count}");

                // Uloz udalost do bufferu
                replayBufferItem.Next_state = this.GetState();
                this.replayMemory.Add(replayBufferItem);    // pridaj do pamate trenovacich dat

                // Training
                if (myGA.bestAgent == this)
                    this.Training();

                // Uchovaj stav predoslej hry
                this.replayBufferItem = new ReplayBufferItem { State = replayBufferItem.Next_state };

                // Exploration/Exploitation parameter changed
                this.epsilon = math.max(epsilonMin, (epsilon * 0.999995f));  // od 100% nahody po 1%
                
                // Prepni na prvy obraz (akcia lode)
                this.isFirstFrame = true;
            }                        
        }
        else    // Ak je lod znicena = cas na preucenie siete (nove vedomosti)
        {
            // Zabite lode pretrenuj
            this.Training();
            
            // Respawn
            RespawnShip();
        }
    }

    /*float[][] X = new float[][] { new float[]{0,0}, new float[]{1,0}, new float[]{0,1}, new float[]{1,1} };
    float[][] targets = new float[][] { new float[]{0}, new float[]{1}, new float[]{1}, new float[]{0} };

    public void Update()
    {
        float avgErr = 0;

        for (int v = 0; v < 4; v++)
        {
            // Training Q network            
            QNet.Run(X[v]);
            QNet.Training(X[v], targets[v]);

            if (num_of_episodes % 10 == 0)
            {        
                Debug.Log($"X[{v}][0] = {X[v][0]}, X[{v}][1] = {X[v][1]}, y = {QNet.neuronLayers[2].Neurons[0].output}");
            }
            avgErr += math.abs(targets[v][0] - QNet.neuronLayers[2].Neurons[0].output);
        }
        // Kvadraticky priemer chyby NN
        avgErr /= (float)BATCH_SIZE;

        if (num_of_episodes > 0 && num_of_episodes % 10 == 0)
        {
            this.presiel10Epizod = true; // po 10000 epizodach vygeneruje 1000 generacii populacie
            QNet.errorList.Add(avgErr);
            Debug.Log($"avgErr.QNet[{this.name}] = {avgErr}");
        }

        // Kym nepresiel 10 epizod scitavaj odmeny do celkoveho skore
        if (this.presiel10Epizod == false)
        {
            this.fitness += avgErr;//replayBufferItem.Reward;
            this.levelBox.text = ((int)this.fitness).ToString();
        }

        num_of_episodes++;
    }*/

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

    private void Training(float gamma=0.90f, float tau=0.01f)
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
            
                if (this.presiel10Epizod && this.IsDestroyed)
                {        
                    avgErr += math.abs(targets[sample[i].Action] - QNet.neuronLayers[2].Neurons[sample[i].Action].output);
                }
            }

            if (this.presiel10Epizod && this.IsDestroyed)
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
        var radarResult = Sensors.Radar.Scan(this.rigidbody2d.position, this.LookDirection, this.transform);
        float[] state = new float[num_of_states];       // array of zeros
        int idx = 0;
        
        // 192 = 12x16
        // Udaje o objektoch v okoli lodi
        for (int i = 0; i < 16; i++, idx+=11)
        {
            // ak luc narazil na objekt hry
            if (radarResult[i] != null)
            {
                // TAG = VSTUP do NN
                switch (radarResult[i].Value.transform.tag)
                {
                    case "Planet":
                        var planet = radarResult[i].Value.transform.GetComponent<PlanetController>();
                        if (this.myPlanets.Contains(planet))                
                            // moja planeta
                            state[idx + 10] = 0x01; 
                        else if (planet.OwnerPlanet != null)
                            // planeta uz vlastnena
                            state[idx + 9] = 0x01; 
                        else
                            // planeta bez vlastnika
                            state[idx + 8] = 0x01; 
                        break;
                    case "Moon":
                        state[idx + 7] = 0x01;
                        break;
                    case "Star":
                        state[idx + 6] = 0x01; 
                        break;
                     case "Nebula":
                        state[idx + 5] = 0x01;
                        break;
                    case "Health":
                        state[idx + 4] = 0x01; 
                        break;
                    case "Ammo":
                        state[idx + 3] = 0x01; 
                        break;
                    case "Projectile":
                        state[idx + 2] = 0x01;  
                        break;
                    case "Player":
                        state[idx + 1] = 0x01; 
                        break;    
                    case "Space":
                        state[idx] = 0x01; 
                        break;    
                    default:
                        Debug.Log($"obj.tag = {radarResult[i].Value.transform.tag}, dist = {radarResult[i].Value.distance}");
                        break;
                }
            }            
        }

        // LIFO
        //if (frameBuffer.Count >= (num_of_states * num_of_frames))
        //    frameBuffer.RemoveRange(0, num_of_states);
        // pridaj stav do frameBufferu        
        //frameBuffer.AddRange(state);

        //return frameBuffer.ToArray();
        return state;
    }
}

public class ReplayBuffer
{
    private const int max_count = 20000;

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

