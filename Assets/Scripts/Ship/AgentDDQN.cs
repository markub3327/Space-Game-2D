using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;
using UnityEngine.UI;
using System.IO;

public class AgentDDQN : ShipController
{
    // Senzory
    private const int num_of_states = Sensors.Radar.num_of_rays * Sensors.Radar.num_of_objs;
    private const int num_of_actions = 16;

    // Neuronove siete
    public NeuralNetwork QNet = new NeuralNetwork();
    public NeuralNetwork QTargetNet = new NeuralNetwork();
    private ReplayBuffer replayMemory = new ReplayBuffer();
    private ReplayBufferItem replayBufferItem = null;
    private const int BATCH_SIZE = 32; // size of minibatch
    private float epLoss = 0.0f;

    // Epsilon
    private float epsilon = 1.0f;
    private const float epsilonMin = 0.10f;
    private float epsilon_decay = 0.995f;

    // Premenne herneho prostredia
    public int episode { get; private set; } = 0;
    public int step { get; private set; } = 0;
    public bool testMode = false;
    private bool isFirstFrame = true;
    public float score = 0f;

    // Lod
    public Text nameBox;
    public Text levelBox;
    public TurretController[] turretControllers;


    public override void Start()
    {
        base.Start();

        // vytvor Q siet
        QNet.addLayer(128, NeuronLayerType.INPUT, num_of_states);
        QNet.addLayer(128, NeuronLayerType.HIDDEN, edge:QNet.neuronLayers[0]);
        QNet.addLayer(num_of_actions, NeuronLayerType.OUTPUT, edge:QNet.neuronLayers[1]);
        QNet.setBPGEdge(QNet.neuronLayers[1], QNet.neuronLayers[2]);
        QNet.setBPGEdge(QNet.neuronLayers[0], QNet.neuronLayers[1]);

        // vytvor Q_target siet
        QTargetNet.addLayer(128, NeuronLayerType.INPUT, num_of_states);
        QTargetNet.addLayer(128, NeuronLayerType.HIDDEN, edge:QTargetNet.neuronLayers[0]);
        QTargetNet.addLayer(num_of_actions, NeuronLayerType.OUTPUT, edge:QTargetNet.neuronLayers[1]);
        QTargetNet.setBPGEdge(QTargetNet.neuronLayers[1], QTargetNet.neuronLayers[2]);
        QTargetNet.setBPGEdge(QTargetNet.neuronLayers[0], QTargetNet.neuronLayers[1]);
    
        for (int j = 0; j < this.QNet.neuronLayers.Count; j++)
        {
            for (int k = 0; k < this.QNet.neuronLayers[j].neurons.Length; k++)
            {
                this.QNet.neuronLayers[j].neurons[k].soft_update(this.QTargetNet.neuronLayers[j].neurons[k], 1.0f);
            }
        }

        // Init Player info panel
        this.nameBox.text = this.Nickname;
        this.levelBox.text = "0";

        // Nacitaj vahy zo subora ak existuje
        /*var str1 = Application.dataPath + "/DDQN_Weights_QNet.save";
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
        }*/        

        // Nacitaj stav, t=0
        this.replayBufferItem = new ReplayBufferItem { State = Sensors.Radar.Scan(this.rigidbody2d.position, this.LookDirection, this) };
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
                this.replayBufferItem.Next_state = Sensors.Radar.Scan(this.rigidbody2d.position, this.LookDirection, this);
                
                // Ak uz hrac nema zivoty ani palivo znici sa lod
                if (this.Health <= 0 || this.Fuel <= 0)
                {                       
                    if (this.myPlanets.Count > 1)
                    {
                        var strPlanets = string.Empty;
                        this.myPlanets.ForEach(x => strPlanets += x.name + ", ");
                        Debug.Log($"MyPlanets[{this.Nickname}]: {strPlanets}");
                    }

                    if (this.Hits > 0)
                        Debug.Log($"Kills[{this.Nickname}]: {(this.Hits / (float)(ShipController.maxHealth*2))}");

                    Debug.Log($"episode[{this.Nickname}]: {episode}");
                    Debug.Log($"step[{this.Nickname}]: {step}");
                    Debug.Log($"score[{this.Nickname}]: {score}");
                    Debug.Log($"epsilon[{this.Nickname}]: {epsilon}");
                    Debug.Log($"epLoss[{this.Nickname}]: {epLoss}");

                    // Terminalny stav - koniec epizody
                    this.replayBufferItem.Done = true;

                    // Destrukcia lode
                    this.DestroyShip();
                    
                    this.episode += 1;
                    this.step = 0;

                    // Exploration/Exploitation parameter decay
                    if (this.epsilon > epsilonMin)
                        this.epsilon *= this.epsilon_decay;  // od 100% nahody po 1%
                }
                else    // pokracuje v hre
                {
                    // Neterminalny stav - pokracuje v hre
                    this.replayBufferItem.Done = false;             
                }

                //Debug.Log($"done[{this.Nickname}]: {replayBufferItem.Done}");
                //Debug.Log($"reward[{this.reward}]: {reward}");
                //Debug.Log($"action[{this.Nickname}]: {replayBufferItem.Action}");

                this.replayBufferItem.Reward = this.reward;
                this.score += this.reward;
                this.step += 1;
                this.levelBox.text = this.score.ToString("0.0");

                // Uloz udalost do bufferu
                replayMemory.Add(this.replayBufferItem);

                // Pretrenuj hraca derivaciami
                if  (replayMemory.Count >= BATCH_SIZE && this.step % 10 == 0 && !this.testMode)
                {
                    // training loop
                    //for (int k = 0; k < 25; k++)
                    //{
                    epLoss = this.Training();
                    //}
                }

                // Prepni na prvy obraz (akcia lode)
                this.isFirstFrame = true;

                this.replayBufferItem = new ReplayBufferItem { State = this.replayBufferItem.Next_state };
            }                        
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
                this.reward = -0.75f;
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
                this.reward = -0.75f;
                // Prahra clip poskodenia lode
                this.PlaySound(damageClip);
                break;
            case "Ammo":
                if (this.Ammo < ShipController.maxAmmo)
                {
                    // Pridaj municiu hracovi
                    this.ChangeAmmo(+50.00f);
                    this.reward = 0.20f;
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
                    this.reward = 0.20f;
                    // Prehraj klip
                    this.PlaySound(collectibleClip);
                }
                Destroy(collision.gameObject);
                break;
            case "Asteroid":
                // Uberie sa hracovi zivot
                this.ChangeHealth(-1.0f);      
                this.reward = -0.20f;
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
                        this.reward = 1.0f;

                        planet.dialogBox.Clear();
                        planet.dialogBox.WriteLine($"Planet: {planet.name}");
                        planet.dialogBox.WriteLine($"Owner: {this.Nickname}");
                    }
                }
                break;
            case "Player":
                // Uberie sa hracovi zivot
                this.ChangeHealth(-1.0f);
                this.reward = -0.75f;
                // Prahra clip poskodenia lode
                this.PlaySound(damageClip);
                break;
            case "Space":
                this.reward = -1.0f;
                break;
            default:
                Debug.Log($"collision_tag: {collision.gameObject.tag}");
                break;
        }
    }

    private float Training(float gamma=0.95f, float tau=0.01f)
    {        
        float avgErr = 0f;

        // Ak je v zasobniku dost vzorov k uceniu
        var sample = replayMemory.Sample(BATCH_SIZE);
            
        for (int i = 0; i < sample.Count; i++)
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
                
            // Training Q network  - Stochastic gradient descent with momentum           
            QNet.update(sample[i].State, target);

            // Soft update Q Target network
            for (int j = 0; j < this.QNet.neuronLayers.Count; j++)
            {
                for (int k = 0; k < this.QNet.neuronLayers[j].neurons.Length; k++)
                {
                    this.QNet.neuronLayers[j].neurons[k].soft_update(this.QTargetNet.neuronLayers[j].neurons[k], tau);
                }
            }

            avgErr += math.abs(target[sample[i].Action] - QNet.neuronLayers[QNet.neuronLayers.Count - 1].neurons[sample[i].Action].output);
        }
        
        // Priemer chyby NN
        avgErr /= (float)sample.Count;
        //Debug.Log($"avgErr.QNet[{this.Nickname}] = {avgErr}");
 
        return avgErr;
    }

    private int Act(float[] state, float epsilon=0.20f)
    {
        int action;

        // Vyuzivaj naucenu vedomost
        if (UnityEngine.Random.Range(0.0f, 1.0f) < epsilon || testMode)
        {
            action = UnityEngine.Random.Range(0, num_of_actions);
        }
        else
        {
            var q = QNet.predict(state);
            action = System.Array.IndexOf(q, q.Max());
            //Debug.Log($"action = {action}");
        }
        
        // za pohyb lode
        this.reward = -0.015f;

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
                this.reward = -0.030f;
                // Ak uz hrac nema municiu nemoze pokracovat v strelbe
                if (this.Ammo <= 0)
                    break;
            }
        }
    }
}

public class ReplayBuffer
{
    private const int max_count = 1000000;

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

