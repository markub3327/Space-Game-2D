using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;
using UnityEngine.UI;
using System.IO;

public class AgentDDQN : ShipController
{
    private const int num_of_states = Sensors.Radar.num_of_rays * Sensors.Radar.num_of_objs;

    private const int num_of_actions = 16;

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

        epsilon_decay = (epsilon - epsilonMin) / 1500000f;

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
        QNet.neuronLayers[0].CreateNeurons(num_of_states, 64);
        QNet.neuronLayers[1].CreateNeurons(64); // 24, 32, 48, 64(lode sa po 2000 iteraciach skoro nehybu), 128(stal na mieste), 256(letel k okrajom Vesmiru)
        QNet.neuronLayers[2].CreateNeurons(num_of_actions);

        QTargetNet.neuronLayers[0].CreateNeurons(num_of_states, 64);
        QTargetNet.neuronLayers[1].CreateNeurons(64); // 24, 32, 48, 64(lode sa po 2000 iteraciach skoro nehybu), 128(stal na mieste), 256(letel k okrajom Vesmiru)
        QTargetNet.neuronLayers[2].CreateNeurons(num_of_actions);
    
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
                    planets_old = 0;
                    if (this.Hits > 0)
                        Debug.Log($"Hits[{this.name}]: {this.Hits}");

                    // Destrukcia lode
                    DestroyShip();

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
                        Debug.Log($"MyPlanets[{this.name}]: {strPlanets}");
                    }

                    // Terminalny stav - koniec epizody
                    replayBufferItem.Done = true;
                    replayBufferItem.Reward = +1.0f;
                    planets_old = this.myPlanets.Count;
                }
                else    // pokracuje v hre
                {
                    var score = this.Score;
                    // Neterminalny stav - pokracuje v hre
                    replayBufferItem.Done = false;             
                    replayBufferItem.Reward = score - this.scoreOld;
                    this.levelBox.text = score.ToString("0.00");
                    this.scoreOld = score;       // odmena za krok v hre je prirastok v skore po akcii hraca
                }
    
                // Vypocet fitness pre Geneticky algoritmus vyberu jedincov
                if (!presiel10Epizod)
                {
                    this.fitness += replayBufferItem.Reward;
                }

                // Uloz udalost do bufferu
                if (replayBufferItem.Reward != 0f)
                {
                    replayMemory.Add(replayBufferItem);    // pridaj do pamate trenovacich dat
                    //if (replayBufferItem.Reward > 0f)
                    //Debug.Log($"Reward[{this.Nickname}] = {replayBufferItem.Reward}");
                }

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
    
                num_of_episodes++;
            }
            if (num_of_episodes > 1000) 
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

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Podla oznacenia herneho objektu vykonaj akciu kolizie
        switch (collision.gameObject.tag)
        {
            case "Star":
                // Prahra clip poskodenia lode
                this.PlaySound(damageClip);
                // Uberie sa hracovi zivot
                this.ChangeHealth(-0.10f);
                break;
            case "Nebula":
                if (this.Fuel < ShipController.maxFuel)
                {
                    // Pridaj palivo hracovi
                    this.ChangeFuel(+0.006f);
                    // Prehraj klip
                    this.PlaySound(collectibleClip);
                }   
                break;

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
                    this.ChangeAmmo(+0.50f);
                    // Prehraj klip
                    this.PlaySound(collectibleClip);
                }
                Destroy(collision.gameObject);
                break;
            case "Health":
                if (this.Health < ShipController.maxHealth)
                {
                    // Pridaj zivot hracovi
                    this.ChangeHealth(+0.10f);
                    // Prehraj klip
                    this.PlaySound(collectibleClip);
                }
                Destroy(collision.gameObject);
                break;
            case "Asteroid":
                // Uberie sa hracovi zivot
                this.ChangeHealth(-0.10f);      
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
                // Prahra clip poskodenia lode
                this.PlaySound(damageClip);
                // Uberie sa hracovi zivot
                this.ChangeHealth(-0.10f);
                break;
        }
    }

    private void Training(float gamma=0.95f, float tau=0.01f)
    {        
        // Ak je v zasobniku dost vzorov k uceniu
        if (replayMemory.Count >= BATCH_SIZE && !testMode)
        {
            var sample = replayMemory.Sample(BATCH_SIZE);

            //Debug.Log($"sample.Count = {sample.Count}, memory.Count = {this.replayMemory.Count}");

            for (int i = 0; i < BATCH_SIZE; i++)
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
                        GetMaxQ(QNet.neuronLayers[2].Neurons),
                        GetMaxQ(QTargetNet.neuronLayers[2].Neurons)
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

                sample[i].error = math.abs(targets[sample[i].Action] - QNet.neuronLayers[2].Neurons[sample[i].Action].output);
                if (presiel10Epizod)
                    this.avgErr += sample[i].error;
            }
        
            if (presiel10Epizod)
            {
                // Priemer chyby NN
                this.avgErr /= (float)sample.Count;
                Debug.Log($"avgErr.QNet[{this.name}] = {avgErr}");
            }            
        }
    }

    private int Act(float[] state, float epsilon=0.20f)
    {
        int action = 0;

        // Vyuzivaj naucenu vedomost
        if (UnityEngine.Random.Range(0.0f, 1.0f) > epsilon || testMode)
        {
            QNet.Run(state);
            GetMaxQ(QNet.neuronLayers[2].Neurons, ref action);
        }
        else    // Skumaj prostredie
        {
            action = UnityEngine.Random.Range(0, num_of_actions);
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
                this.ChangeAmmo(-0.01f);
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
    private const int max_count = 50000;

    public LinkedList<ReplayBufferItem> items = new LinkedList<ReplayBufferItem>();

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
		float[] probability = new float[this.items.Count];
		float prob_sum = 0.0f;
        int i = 0;

		// Softmax function
		/*********************************************************/
        foreach (var x in this.items)
		{
			prob_sum += Unity.Mathematics.math.exp(x.error);
		}        
        foreach (var x in this.items)
        {
            probability[i++] = Unity.Mathematics.math.exp(x.error) / prob_sum;
        }
		/*********************************************************/

		// Pseudorandom selection
		/*********************************************************/
        for (; buff.Count < batch_size;)
        {
            i = 0;
            foreach (var x in this.items)
            {
                if (UnityEngine.Random.Range(0f, 1f) < probability[i])                        
                {                
    			    //Debug.Log($"i = {i}, prob = {probability[i]}, error = {x.error}");
                    buff.Add(x);
                    break;
                }
                i++;
            }
        }
        //Debug.Log($"count = {this.items.Count}, batch_size = {buff.Count}");

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

    public float         error;
}

