using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;
using UnityEngine.UI;
using System.IO;

public class AgentDDQN : ShipController
{
    private const int num_of_states = Sensors.Radar.num_of_rays * Sensors.Radar.num_of_objs;

    private const int num_of_actions = 18;

    private bool isFirstFrame = true;

    public NeuralNetwork QNet = new NeuralNetwork();
    public NeuralNetwork QTargetNet = new NeuralNetwork();

    private ReplayBuffer replayMemory = new ReplayBuffer();
    private ReplayBufferItem replayBufferItem = null;
    private const int BATCH_SIZE = 32; // size of minibatch

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

    public bool presiel10Epizod = false;
    public bool testMode = false;

    public override void Start()
    {
        base.Start();

        epsilon_decay = (epsilon - epsilonMin) / 1000000f;

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
        QNet.neuronLayers[0].CreateNeurons(num_of_states, num_of_states);
        QNet.neuronLayers[1].CreateNeurons(24); // 24, 32, 48, 64(lode sa po 2000 iteraciach skoro nehybu), 128(stal na mieste), 256(letel k okrajom Vesmiru)
        QNet.neuronLayers[2].CreateNeurons(num_of_actions);

        QTargetNet.neuronLayers[0].CreateNeurons(num_of_states, num_of_states);
        QTargetNet.neuronLayers[1].CreateNeurons(24); // 24, 32, 48, 64(lode sa po 2000 iteraciach skoro nehybu), 128(stal na mieste), 256(letel k okrajom Vesmiru)
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
                replayBufferItem.Next_state = Sensors.Radar.Scan(this.rigidbody2d.position, this.LookDirection, this);

                // Ak uz hrac nema zivoty ani palivo znici sa lod
                if (this.Health <= 0 || this.Fuel <= 0)
                {
                    if (this.myPlanets.Count > 0)
                    {
                        var strPlanets = string.Empty;
                        this.myPlanets.ForEach(x => strPlanets += x.name + ", ");
                        Debug.Log($"MyPlanets[{this.name}]: {strPlanets}");
                    }
                    if (this.Hits > 0)
                        Debug.Log($"Hits[{this.name}]: {this.Hits}");

                    // Destrukcia lode
                    DestroyShip();

                    // Terminalny stav - koniec epizody
                    replayBufferItem.Done = true;
                    replayBufferItem.Reward = -1.0f;
                }
                else    // pokracuje v hre
                {
                    var score = this.Score;
                    // Neterminalny stav - pokracuje v hre
                    replayBufferItem.Done = false;                    
                    replayBufferItem.Reward = score - this.scoreOld;
                    this.scoreOld = score;       // odmena za krok v hre je prirastok v skore po akcii hraca
                    this.levelBox.text = score.ToString("0.000");
                }
                
                // Uloz udalost do bufferu
                replayMemory.Add(replayBufferItem);    // pridaj do pamate trenovacich dat

                // Uchovaj stav predoslej hry
                this.replayBufferItem = new ReplayBufferItem { State = replayBufferItem.Next_state };
                
                // Prepni na prvy obraz (akcia lode)
                this.isFirstFrame = true;
            }                        
        }
        else    // Ak je lod znicena = cas na preucenie siete (nove vedomosti)
        {            
            if (this.presiel10Epizod == false)
            {
                if (num_of_episodes > 0 && (num_of_episodes % 10 == 0))
                {
                    this.presiel10Epizod = true; // po 10000 epizodach vygeneruje 1000 generacii populacie 
      
                    Debug.Log($"epsilon[{this.name}] = {epsilon}");
                    Debug.Log($"episode[{this.name}] = {num_of_episodes}");
                }
    
                // Pretrenuj hraca derivaciami
                this.Training();

                num_of_episodes++;
            }
            if (num_of_episodes > 5000) 
            { 
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
            }
            
            // Respawn
            RespawnShip();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Podla oznacenia herneho objektu vykonaj akciu kolizie
        switch (collision.gameObject.tag)
        {
            case "Ammo":
                if (this.Ammo < ShipController.maxAmmo)
                {
                    // Pridaj municiu hracovi
                    this.ChangeAmmo(+100);
                    // Prehraj klip
                    this.PlaySound(collectibleClip);
                }
                Destroy(collision.gameObject);
                break;
            case "Nebula":
                if (this.Fuel < ShipController.maxFuel)
                {
                    // Pridaj palivo hracovi
                    this.ChangeFuel(+1);
                    // Prehraj klip
                    this.PlaySound(collectibleClip);
                }
                break;
            case "Health":
                if (this.Health < ShipController.maxHealth)
                {
                    // Pridaj zivot hracovi
                    this.ChangeHealth(+1);
                    // Prehraj klip
                    this.PlaySound(collectibleClip);
                }
                Destroy(collision.gameObject);
                break;
            case "Asteroid":
                // Uberie sa hracovi zivot
                this.ChangeHealth(-1);      
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
                        planet.dialogBox.WriteLine($"Owner: {this.name}");
                    }
                }
                break;
            case "Star":
                // Prahra clip poskodenia lode
                this.PlaySound(damageClip);
                // Uberie sa hracovi zivot
                this.ChangeHealth(-1);
                break;
            case "Player":
                // Prahra clip poskodenia lode
                this.PlaySound(damageClip);
                // Uberie sa hracovi zivot
                this.ChangeHealth(-1);
                break;
        }
    }

    private void Training(float gamma=0.90f, float tau=0.01f)
    {        
        // Ak je v zasobniku dost vzorov k uceniu
        if (replayMemory.Count >= BATCH_SIZE && !testMode)
        {
            var sample = replayMemory.Sample(BATCH_SIZE);
            float avgErr = 0;

            //Debug.Log($"sample.Count = {sample.Count}, memory.Count = {this.replayMemory.Count}");

            for (int i = 0; i < sample.Count; i++)
            {
                float[] targets = new float[num_of_actions];

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

                avgErr += math.abs(targets[sample[i].Action] - QNet.neuronLayers[2].Neurons[sample[i].Action].output);
            }
            // Priemer chyby NN
            avgErr /= (float)sample.Count;

            if (presiel10Epizod)
            {
                QNet.errorList.Add(avgErr);
                Debug.Log($"avgErr.QNet[{this.name}] = {avgErr}");
            }
            // Vypocet fitness pre Geneticky algoritmus vyberu jedincov
            else
            {
                this.fitness += avgErr;
            }

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

        switch (action)
        {
            // case 0x00 - is stop (do nothing)
            // Up
            case 0x01:
                this.MoveShip(Vector2.up);
                break;
            // Up-Right
            case 0x02:
                this.MoveShip(ExtendedVector2.up_right);
                break;
            // Right
            case 0x03:
                this.MoveShip(Vector2.right);
                break;
            // Down-Right
            case 0x04:
                this.MoveShip(ExtendedVector2.down_right);
                break;
            // Down
            case 0x05:
                this.MoveShip(Vector2.down);
                break;
            // Down-Left
            case 0x06:
                this.MoveShip(ExtendedVector2.down_left);
                break;
            // Left
            case 0x07:
                this.MoveShip(Vector2.left);
                break;
            // Up-Left
            case 0x08:
                this.MoveShip(ExtendedVector2.up_left);
                break;
            // is only firing
            case 0x09:
                this.Fire();
                break;
            // Up
            case 0x10:
                this.Fire();
                this.MoveShip(Vector2.up);
                break;
            // Up-Right
            case 0x11:
                this.Fire();
                this.MoveShip(ExtendedVector2.up_right);
                break;
            // Right
            case 0x12:
                this.Fire();
                this.MoveShip(Vector2.right);
                break;
            // Down-Right
            case 0x13:
                this.Fire();
                this.MoveShip(ExtendedVector2.down_right);
                break;
            // Down
            case 0x14:
                this.Fire();
                this.MoveShip(Vector2.down);
                break;
            // Down-Left
            case 0x15:
                this.Fire();
                this.MoveShip(ExtendedVector2.down_left);
                break;
            // Left
            case 0x16:
                this.Fire();
                this.MoveShip(Vector2.left);
                break;
            // Up-Left
            case 0x17:
                this.Fire();
                this.MoveShip(ExtendedVector2.up_left);
                break;
        }

        // Exploration/Exploitation parameter changed
        if (this.epsilon > epsilonMin)
            this.epsilon -= this.epsilon_decay;  // od 100% nahody po 1%

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
                this.ChangeAmmo(-1);
                // Ak uz hrac nema municiu nemoze pokracovat v strelbe
                if (this.Ammo <= 0)
                    break;
            }
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
}

public class ReplayBuffer
{
    private const int max_count = 100000;

    public LinkedList<ReplayBufferItem> items = new LinkedList<ReplayBufferItem>();

    public System.Random rand = new System.Random();

    public void Add(ReplayBufferItem item)
    {
        // LIFO
        if (items.Count >= max_count)    
            items.RemoveFirst();
        items.AddLast(item);        
    }

    public List<ReplayBufferItem> Sample(int batch_size)
    {
        var buff = new List<ReplayBufferItem>(batch_size);
        //var i = 0;

        foreach (var element in items)
        {
            if (buff.Count < batch_size)
            {
                if ((float)rand.NextDouble() < ((float)batch_size/(float)items.Count))
                {
                    buff.Add(element);              
                    //Debug.Log($"selected element i = {i}/{items.Count}");
                }
            }
            else
                break;  // end of selecting items

            //i++;
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

