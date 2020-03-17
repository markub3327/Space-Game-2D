using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;
using UnityEngine.UI;
using System.IO;

public class AgentDDQN : ShipController
{
    private const int num_of_frames = 4;
    private const int num_of_states = Sensors.Radar.num_of_rays * Sensors.Radar.num_of_objs * num_of_frames;
    private const int num_of_actions = 16;

    private List<float> frameBuffer = new List<float>();
    private bool isFirstFrame = true;

    public NeuralNetwork QNet = new NeuralNetwork();
    public NeuralNetwork QTargetNet = new NeuralNetwork();

    private ReplayBuffer replayMemory = new ReplayBuffer();
    private ReplayBufferItem replayBufferItem = null;
    private const int BATCH_SIZE = 32; // size of minibatch

    // Epsilon
    private float epsilon = 1.0f;
    private const float epsilonMin = 0.10f;
    private float epsilon_decay;

    public float fitness { get; set; } = 0;
    public float avgErr { get; set; } = 0;
    public int num_of_episodes { get; set; } = 0;

    public TurretController[] turretControllers;

    // Meno lode
    public Text nameBox;
    public Text levelBox;

    public bool presiel10Epizod { get; set; } = false;
    public bool testMode = false;

    private int planets_old = 0;

    public override void Start()
    {
        base.Start();

        //epsilon_decay = (epsilon - epsilonMin) / 500000f;

        QNet.CreateLayer(NeuronLayerType.INPUT);    // Input layer
        QNet.CreateLayer(NeuronLayerType.HIDDEN);   // 1st hidden
        QNet.CreateLayer(NeuronLayerType.HIDDEN);   // 2nd hidden
        QNet.CreateLayer(NeuronLayerType.OUTPUT);   // Output

        QTargetNet.CreateLayer(NeuronLayerType.INPUT);    // Input layer
        QTargetNet.CreateLayer(NeuronLayerType.HIDDEN);   // 1st hidden
        QTargetNet.CreateLayer(NeuronLayerType.HIDDEN);   // 2nd hidden
        QTargetNet.CreateLayer(NeuronLayerType.OUTPUT);   // Output

        QNet.SetEdge(QNet.neuronLayers[1], QNet.neuronLayers[0]);
        QNet.SetEdge(QNet.neuronLayers[2], QNet.neuronLayers[1]);
        QNet.SetEdge(QNet.neuronLayers[3], QNet.neuronLayers[2]);
        QNet.SetBPGEdge(QNet.neuronLayers[0], QNet.neuronLayers[1]);
        QNet.SetBPGEdge(QNet.neuronLayers[1], QNet.neuronLayers[2]);
        QNet.SetBPGEdge(QNet.neuronLayers[2], QNet.neuronLayers[3]);

        QTargetNet.SetEdge(QTargetNet.neuronLayers[1], QTargetNet.neuronLayers[0]);
        QTargetNet.SetEdge(QTargetNet.neuronLayers[2], QTargetNet.neuronLayers[1]);
        QTargetNet.SetEdge(QTargetNet.neuronLayers[3], QTargetNet.neuronLayers[2]);
        QTargetNet.SetBPGEdge(QTargetNet.neuronLayers[0], QTargetNet.neuronLayers[1]);
        QTargetNet.SetBPGEdge(QTargetNet.neuronLayers[1], QTargetNet.neuronLayers[2]);
        QTargetNet.SetBPGEdge(QTargetNet.neuronLayers[2], QTargetNet.neuronLayers[3]);

        //var num_of_inputs = num_of_states * num_of_frames;
        QNet.neuronLayers[0].CreateNeurons(num_of_states, 32);
        QNet.neuronLayers[1].CreateNeurons(32);
        QNet.neuronLayers[2].CreateNeurons(24);
        QNet.neuronLayers[3].CreateNeurons(num_of_actions);

        QTargetNet.neuronLayers[0].CreateNeurons(num_of_states, 32);
        QTargetNet.neuronLayers[1].CreateNeurons(32);
        QTargetNet.neuronLayers[2].CreateNeurons(24);
        QTargetNet.neuronLayers[3].CreateNeurons(num_of_actions);
    
        // Init Player info panel
        this.nameBox.text = this.Nickname;
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
        this.frameBuffer.AddRange(Sensors.Radar.Scan(this.rigidbody2d.position, this.LookDirection, this));
        this.frameBuffer.AddRange(Sensors.Radar.Scan(this.rigidbody2d.position, this.LookDirection, this));
        this.frameBuffer.AddRange(Sensors.Radar.Scan(this.rigidbody2d.position, this.LookDirection, this));
        this.frameBuffer.AddRange(Sensors.Radar.Scan(this.rigidbody2d.position, this.LookDirection, this));
        this.replayBufferItem = new ReplayBufferItem { State = this.frameBuffer.ToArray() };
        //Debug.Log($"1. frameBuffer.Count = {this.frameBuffer.Count}");
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
                // LIFO
                if (this.frameBuffer.Count >= num_of_states)
                    this.frameBuffer.RemoveRange(0, (Sensors.Radar.num_of_rays * Sensors.Radar.num_of_objs));
                this.frameBuffer.AddRange(Sensors.Radar.Scan(this.rigidbody2d.position, this.LookDirection, this));
                //Debug.Log($"frameBuffer.Count = {this.frameBuffer.Count}");

                // Nacitaj stav hry
                replayBufferItem.Next_state = this.frameBuffer.ToArray();
                
                // Ak uz hrac nema zivoty ani palivo znici sa lod
                if (this.Health <= 0 || this.Fuel <= 0)
                {
                    if (this.Hits > 0)
                        Debug.Log($"Kills[{this.Nickname}]: {(this.Hits / (float)(ShipController.maxHealth*2))}");

                    // Destrukcia lode
                    this.DestroyShip();
                    planets_old = 0;

                    // Terminalny stav - koniec epizody
                    replayBufferItem.Done = true;
                    replayBufferItem.Reward = -1.0f;
                }                
                else if (this.myPlanets.Count > planets_old)
                {
                    if (this.myPlanets.Count > 1)
                    {
                        var strPlanets = string.Empty;
                        this.myPlanets.ForEach(x => strPlanets += x.name + ", ");
                        Debug.Log($"MyPlanets[{this.Nickname}]: {strPlanets}");
                    }

                    // Vytaz hry - ziskal vsetky planety
                    if (this.myPlanets.Count == 4)
                    {
                        this.WinnerShip();
                        planets_old = 0;
                    }

                    // Terminalny stav - koniec epizody
                    replayBufferItem.Done = true;
                    replayBufferItem.Reward = +1.0f;
                    planets_old = this.myPlanets.Count;
                }
                else    // pokracuje v hre
                {
                    // Neterminalny stav - pokracuje v hre
                    replayBufferItem.Done = false;             
                    replayBufferItem.Reward = 0.0f;
                }

                // Vypocet fitness pre Geneticky algoritmus vyberu jedincov
                var score = this.Score;
                if (!presiel10Epizod)
                {
                    this.fitness += score - this.score_old;
                    this.score_old = score;
                }
                // Vypis fitness hraca
                this.levelBox.text = this.fitness.ToString("0.0");

                //Debug.Log($"reward[{this.Nickname}]  = {replayBufferItem.Reward}");
                //Debug.Log($"deltaScore[{this.Nickname}]  = {deltaScore}");
                
                // Uloz udalost do bufferu
                replayMemory.Add(replayBufferItem);    // pridaj do pamate trenovacich dat

                // Uchovaj stav predoslej hry
                this.replayBufferItem = new ReplayBufferItem { State = replayBufferItem.Next_state };
                
                // Prepni na prvy obraz (akcia lode)
                this.isFirstFrame = true;
            }                        
        }
        else
        {
            if (this.presiel10Epizod == false)
            {
                if (num_of_episodes > 0 && (num_of_episodes % 10 == 0))
                {
                    this.presiel10Epizod = true; // po 10000 epizodach vygeneruje 1000 generacii populacie 
      
                    Debug.Log($"epsilon[{this.Nickname}] = {epsilon}");
                    Debug.Log($"episode[{this.Nickname}] = {num_of_episodes}");
                }
                num_of_episodes++;
            }

            // Pretrenuj hraca derivaciami
            this.Training();
            
            // Obnov hraca do hry
            this.RespawnShip();
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Podla oznacenia herneho objektu vykonaj akciu kolizie
        switch (collision.gameObject.tag)
        {
            case "Star":
                // Uberie sa hracovi zivot
                this.ChangeHealth(-1.0f);
                // Prahra clip poskodenia lode
                this.PlaySound(damageClip);
                break;

        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Podla oznacenia herneho objektu vykonaj akciu kolizie
        switch (collision.gameObject.tag)
        {
            case "Star":
                // Uberie sa hracovi zivot
                this.ChangeHealth(-1.0f);
                // Prahra clip poskodenia lode
                this.PlaySound(damageClip);
                break;
            case "Ammo":
                if (this.Ammo < ShipController.maxAmmo)
                {
                    // Pridaj municiu hracovi
                    this.ChangeAmmo(+50.00f);
                    // Prehraj klip
                    this.PlaySound(collectibleClip);
                }
                Destroy(collision.gameObject);
                break;
            case "Health":
                if (this.Health < ShipController.maxHealth)
                {
                    // Pridaj zivot hracovi
                    this.ChangeHealth(+1.0f);
                    // Prehraj klip
                    this.PlaySound(collectibleClip);
                }
                Destroy(collision.gameObject);
                break;
            case "Asteroid":
                // Uberie sa hracovi zivot
                this.ChangeHealth(-1.0f);      
                // Prahra clip poskodenia lode
                this.PlaySound(damageClip);
                break;
            case "Planet":
                {
                    var planet = collision.gameObject.GetComponent<PlanetController>();
                    if (planet.OwnerPlanet == null)
                    {
                        planet.OwnerPlanet = this.gameObject;
                        this.myPlanets.Add(planet);

                        planet.dialogBox.Clear();
                        planet.dialogBox.WriteLine($"Planet: {planet.name}");
                        planet.dialogBox.WriteLine($"Owner: {this.Nickname}");
                    }
                }
                break;
            case "Player":
                // Uberie sa hracovi zivot
                this.ChangeHealth(-1.0f);
                // Prahra clip poskodenia lode
                this.PlaySound(damageClip);
                break;
        }
    }

    private void Training(float gamma=0.95f, float tau=0.01f)
    {        
        // Exploration/Exploitation parameter decay
        if (this.epsilon > epsilonMin)
            this.epsilon *= 0.999f; //this.epsilon_decay;  // od 100% nahody po 1%

        // Ak je v zasobniku dost vzorov k uceniu
        if (replayMemory.Count >= BATCH_SIZE && !testMode)
        {
            var sample = replayMemory.Sample(BATCH_SIZE);
            float target;

            //Debug.Log($"sample.Count = {sample.Count}, memory.Count = {this.replayMemory.Count}");
            this.avgErr = 0f;
            for (int i = 0; i < sample.Count; i++)
            {
                // Non-terminal state     
                if (sample[i].Done == false)
                {
                   // Ziskaj najvyssie Q
                    QNet.Run(sample[i].Next_state);
                    // Ziskaj najvyssie next Q
                    QTargetNet.Run(sample[i].Next_state);

                    // Vyber nasledujuce Q
                    var next_Q = math.min(
                        GetMaxQ(QNet.neuronLayers[3].Neurons),
                        GetMaxQ(QTargetNet.neuronLayers[3].Neurons)
                    );

                    // TD
                    target = sample[i].Reward + (gamma * next_Q);
                }
                // terminal state
                else
                {
                    // TD
                    target = sample[i].Reward;
                    //Debug.Log($"target[{i}] = {targets[sample[i].Action]}");
                }
                
                // Training Q network            
                QNet.Run(sample[i].State);
                QNet.Training(sample[i].State, sample[i].Action, target);

                // Soft update Q Target network
                for (int j = 0; j < this.QNet.neuronLayers.Count; j++)
                {
                    for (int k = 0; k < this.QNet.neuronLayers[j].Weights.Count; k++)
                    {
                        this.QTargetNet.neuronLayers[j].Weights[k] = tau*this.QNet.neuronLayers[j].Weights[k] + (1.0f-tau)*this.QTargetNet.neuronLayers[j].Weights[k];                    
                    }
                }

                if (presiel10Epizod)
                    this.avgErr += math.abs(target - QNet.neuronLayers[3].Neurons[sample[i].Action].output);
            }
        
            if (presiel10Epizod)
            {
                // Priemer chyby NN
                this.avgErr /= (float)sample.Count;
                Debug.Log($"avgErr.QNet[{this.Nickname}] = {avgErr}");
            }            
        }
    }

    private int Act(float[] state, float epsilon=0.20f)
    {
        int action = 0;

        // Vyuzivaj naucenu vedomost
        if (UnityEngine.Random.Range(0.0f, 1.0f) < epsilon || testMode)
        {
            action = UnityEngine.Random.Range(0, num_of_actions);
        }
        else
        {
            QNet.Run(state);
            GetMaxQ(QNet.neuronLayers[3].Neurons, ref action);
        }

        switch (action)
        {
            // Up
            case 0x00:
                this.MoveShip(Vector2.up);
                break;
            // Up-Right
            case 0x01:
                this.MoveShip(ExtendedVector2.up_right);
                break;
            // Right
            case 0x02:
                this.MoveShip(Vector2.right);
                break;
            // Down-Right
            case 0x03:
                this.MoveShip(ExtendedVector2.down_right);
                break;
            // Down
            case 0x04:
                this.MoveShip(Vector2.down);
                break;
            // Down-Left
            case 0x05:
                this.MoveShip(ExtendedVector2.down_left);
                break;
            // Left
            case 0x06:
                this.MoveShip(Vector2.left);
                break;
            // Up-Left
            case 0x07:
                this.MoveShip(ExtendedVector2.up_left);
                break;
            // Up
            case 0x08:
                this.Fire();
                this.MoveShip(Vector2.up);
                break;
            // Up-Right
            case 0x09:
                this.Fire();
                this.MoveShip(ExtendedVector2.up_right);
                break;
            // Right
            case 0x10:
                this.Fire();
                this.MoveShip(Vector2.right);
                break;
            // Down-Right
            case 0x11:
                this.Fire();
                this.MoveShip(ExtendedVector2.down_right);
                break;
            // Down
            case 0x12:
                this.Fire();
                this.MoveShip(Vector2.down);
                break;
            // Down-Left
            case 0x13:
                this.Fire();
                this.MoveShip(ExtendedVector2.down_left);
                break;
            // Left
            case 0x14:
                this.Fire();
                this.MoveShip(Vector2.left);
                break;
            // Up-Left
            case 0x15:
                this.Fire();
                this.MoveShip(ExtendedVector2.up_left);
                break;
        }

        return action;
    }

    private void Fire()
    {
        if (this.Ammo > 0)
        {       
            // Prehra zvuk vystrelu
            this.PlaySound(throwClip);
                     
            foreach (var turret in this.turretControllers)
            {
                // Vystrel
                turret.Fire();
                // Uber z municie hraca jeden naboj
                this.ChangeAmmo(-1.0f);
                // Ak uz hrac nema municiu nemoze pokracovat v strelbe
                if (this.Ammo <= 0)
                    break;
            }
        }
    }

    private float GetMaxQ(List<Neuron> qValues, ref int action)
    {
        action = 0;

        for (int i = 1; i < qValues.Count; i++)
        {
            if (qValues[action].output < qValues[i].output)
                action = i;
        }
        return qValues[action].output;
    }

    private float GetMaxQ(List<Neuron> qValues)
    {
        int action = 0;

        for (int i = 1; i < qValues.Count; i++)
        {
            if (qValues[action].output < qValues[i].output)
                action = i;
        }
        return qValues[action].output;
    }
}

public class ReplayBuffer
{
    private const int max_count = 100000;

    public List<ReplayBufferItem> items = new List<ReplayBufferItem>();

    public void Add(ReplayBufferItem item)
    {
        // LIFO
        if (items.Count >= max_count)    
            items.RemoveAt(0);
        items.Add(item);        
    }

    public List<ReplayBufferItem> Sample(int batch_size)
    {
        List<ReplayBufferItem> buff = new List<ReplayBufferItem>(batch_size);

        for (int i = 0; i < batch_size; i++)
        {
            var idx = UnityEngine.Random.Range(0, this.Count);
            buff.Add(this.items[idx]);
        }

        return buff;
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

