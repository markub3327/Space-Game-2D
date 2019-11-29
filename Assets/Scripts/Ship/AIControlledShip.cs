using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

public class AIControlledShip : ShipController
{
    public NeuralNetwork Qnet = new NeuralNetwork();
    public NeuralNetwork QTargetNet = new NeuralNetwork();

    private bool waitingOnNextFrame = false;

    // Geneticky algoritmus vyberu najsilnejsieho jedinca v hre
    private float fitnessCounter;
    public float fitness { get; set; }       // ohodnotenie jedinca v ramci hre

    public TurretController[] turretControllers;

    // Balicek dat pripravenych k uceniu
    private Batch myBatch;

    private float epsCounter = 1f;
    public float eps { get { var x = Mathf.Clamp(epsCounter, 0.01f, 1f); /*Debug.Log($"eps({this.name}) = {x}");*/ return x; } }

    public override void Start()
    {
        base.Start();

        this.myPlanets = new List<PlanetController>();
        this.fitnessCounter = 0f;
        this.fitness = 0f;

        Qnet.CreateLayer(NeuronLayerType.INPUT);
        Qnet.CreateLayer(NeuronLayerType.HIDDEN);
        Qnet.CreateLayer(NeuronLayerType.HIDDEN);
        Qnet.CreateLayer(NeuronLayerType.OUTPUT);

        QTargetNet.CreateLayer(NeuronLayerType.INPUT);
        QTargetNet.CreateLayer(NeuronLayerType.HIDDEN);
        QTargetNet.CreateLayer(NeuronLayerType.HIDDEN);
        QTargetNet.CreateLayer(NeuronLayerType.OUTPUT);

        Qnet.neuronLayers[1].Edge = Qnet.neuronLayers[0];
        Qnet.neuronLayers[2].Edge = Qnet.neuronLayers[1];
        Qnet.neuronLayers[3].Edge = Qnet.neuronLayers[2];
        Qnet.neuronLayers[0].BPG_egdes = Qnet.neuronLayers[1];
        Qnet.neuronLayers[1].BPG_egdes = Qnet.neuronLayers[2];
        Qnet.neuronLayers[2].BPG_egdes = Qnet.neuronLayers[3];

        QTargetNet.neuronLayers[1].Edge = QTargetNet.neuronLayers[0];
        QTargetNet.neuronLayers[2].Edge = QTargetNet.neuronLayers[1];
        QTargetNet.neuronLayers[3].Edge = QTargetNet.neuronLayers[2];
        QTargetNet.neuronLayers[0].BPG_egdes = QTargetNet.neuronLayers[1];
        QTargetNet.neuronLayers[1].BPG_egdes = QTargetNet.neuronLayers[2];
        QTargetNet.neuronLayers[2].BPG_egdes = QTargetNet.neuronLayers[3];

        // Vstupna vrstva = pocet neuronov je rovny poctu vstupov
        for (int i = 0; i < 19; i++)
        {
            Qnet.neuronLayers[0].CreateNeuron(1);
            QTargetNet.neuronLayers[0].CreateNeuron(1);

            // Skopiruj parametre sieti
            QTargetNet.neuronLayers[0].deltaWeights = Qnet.neuronLayers[0].deltaWeights;
            QTargetNet.neuronLayers[0].Weights      = Qnet.neuronLayers[0].Weights;
        }

        // Skryta vrstva #1
        for (int i = 0; i < 256; i++)
        {
            Qnet.neuronLayers[1].CreateNeuron();
            QTargetNet.neuronLayers[1].CreateNeuron();
        }

        // Skryta vrstva #2
        for (int i = 0; i < 128; i++)
        {
            Qnet.neuronLayers[2].CreateNeuron();
            QTargetNet.neuronLayers[2].CreateNeuron();
        }

        // Vystupna vrstva
        for (int i = 0; i < 4; i++)
        {
            Qnet.neuronLayers[3].CreateNeuron();
            QTargetNet.neuronLayers[3].CreateNeuron();
        }

        // Zrovnaj vahy siet
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < Qnet.neuronLayers[i].Weights.Count; j++)
            {
                QTargetNet.neuronLayers[i].Weights[j] = Qnet.neuronLayers[i].Weights[j];
            }
        }

        // inicializuj pociatocny stav hry
        this.myBatch = new Batch 
        {
            state = GetState(),
            done = false
        };
    }
    
    public override void Update()
    {        
        if (!this.IsDestroyed)
        {
            // Ak uz hrac nema zivoty znici sa lod
            if ((this.Health <= 0) || (this.Fuel <= 0f))
            {
                this.DestroyShip();
            }

            // Ak necaka na vykonanie akcie lode
            if (!this.waitingOnNextFrame)
            {
                Qnet.Run(this.myBatch.state);
                this.myBatch.action = this.DoAction(this.eps);
                this.waitingOnNextFrame = true;         // bude sa cakat na dalsi obraz kvoli vykonaniu akcie a uceniu sieti                
            }
            // ak lod uz vykonala akciu ziska sa novy stav a nauci sa siet
            else
            {
                this.myBatch.next_state = this.GetState();      // ziskaj novy stav z hry
                this.myBatch.reward = this.GetReward();         // Odmena za akciu vykonana v minulom obraze
                this.myBatch.done = this.IsDestroyed;
                //Debug.Log($"reward({this.name}) = {this.myBatch.reward}");

                this.Learn(this.myBatch);                       // Pretrenuj Qnet podla noveho balicku dat z hry
                this.fitnessCounter += this.myBatch.reward;     // suma odmien (skore) za hernu epizodu lode
                if (IsDestroyed) this.fitness += this.fitnessCounter;
                this.myBatch.state = this.myBatch.next_state;   // prejdi na dalsi stav a vykonaj akciu
                this.waitingOnNextFrame = false;
            }

            epsCounter *= 0.9999f;
        }
        else
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer < 0.0f)
            {                
                RespawnShip();
                animator.SetTrigger("Respawn");
        
                this.fitnessCounter = 0f;
                //this.fitness = 0f;

                // inicializuj pociatocny stav hry
                this.myBatch = new Batch 
                {
                    state = GetState(),
                    done = false
                };
            }
        }
    }

    private int GetMaxAction(List<Neuron> neurons)
    {
        var index = 0;
        for (int i = 1; i < neurons.Count; i++)
        {
            if (neurons[index].output < neurons[i].output)
                index = i;
        }
        return index;
    }

    private float[] GetState()
    {
        var radarResult = Sensors.Radar.Scan(this.rigidbody2d.position, this.LookDirection, this.transform);
        var state = new float[19];
            
        // Poloha lode v ramci herneho sveta
        state[0] = this.rigidbody2d.position.x / 20f;
        state[1] = this.rigidbody2d.position.y / 20f;
        //Debug.Log($"state({this.name})[X] = {state[0]}, state({this.name})[Y] = {state[1]}");
        
        // Vstup z pamate o vlastnictve planet
        string hash = string.Empty;
        foreach (var p in myPlanets)
        {
            //Debug.Log($"planet({this.name}) = {p.name}");
            hash += p.name + "+";
        }
        if (hash.Length > 0)
            state[2] = hash.GetHashCode() / (float)int.MaxValue;
        else
            state[2] = 0x00;    // kod pre prazdnu pamet planet
        //Debug.Log($"state({this.name})[PlanetMemmory] = {state[2]}, hash = {hash}");

        // Radar lode
        for (int i = 3, j = 0; i < state.Length; i++, j++)
        {
            if (radarResult[j] != null)
            {
                state[i] = (float)radarResult[j].Value.transform.tag.GetHashCode() / (float)int.MaxValue;
    
                //Debug.Log($"state({this.name})[Radar{j}] = {state[i]}, tag = {radarResult[j].Value.transform.tag}, name = {radarResult[j].Value.transform.name}");
            }
            else
            {
                state[i] = 0x00;
                //Debug.Log($"state({this.name})[Radar{j}] = {state[i]}");
            }
        }

        return state;
    }

    private int DoAction(float epsilon=0f)
    {
        int action;

        // Epsilon-greedy action selection
        if (randomGen.NextFloat() > epsilon)
        {
            action = GetMaxAction(Qnet.neuronLayers[3].Neurons);
        }
        else
        {
            action = randomGen.NextInt(0,4);            
        }
        //Debug.Log($"action({this.name}) = {action}, epsilon = {epsilon}");

        // Vykonaj akciu
        switch (action)
        {
            case 0x00:  // Sipka hore
                this.MoveShip(Vector2.up);
                break;
            //case 0x01:  // Sipka dole
            //    this.MoveShip(Vector2.down);
            //    break;
            case 0x01:  // Sipka vlavo
                this.MoveShip(Vector2.left);
                break;
            case 0x02:  // Sipka vpravo
                this.MoveShip(Vector2.right);
                break;
            case 0x03:  // Fire
            {
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

    private void Learn(Batch batch, float gamma=0.99f, float tau=0.01f)
    {
        QTargetNet.Run(batch.next_state);

        var next_action = this.GetMaxAction(QTargetNet.neuronLayers[3].Neurons);
        
        // Get max predicted Q values (for next states) from target model
        var Q_targets_next = QTargetNet.neuronLayers[3].Neurons[next_action].output;

        // Compute Q targets for current states
        float Q_target;
        if (!batch.done)
        { 
            Q_target = batch.reward + (gamma * Q_targets_next);
        }
        else
        {
            //Debug.Log("Koniec epizody");
            Q_target = batch.reward;
        }

        // Compute loss
        var network_feedback = new float[] { 0f, 0f, 0f, 0f, 0f };
        network_feedback[batch.action] = Q_target;
        // Minimize the loss
        Qnet.Training(batch.state, network_feedback);
        //Debug.Log($"error({this.name}) = {Q_target - Qnet.neuronLayers[3].Neurons[batch.action].output}");

        // ------------------- update target network -------------------
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < Qnet.neuronLayers[i].Weights.Count; j++)
            {
                QTargetNet.neuronLayers[i].Weights[j] = tau*Qnet.neuronLayers[i].Weights[j] + (1.0f-tau)*QTargetNet.neuronLayers[i].Weights[j];
            }
        }
    }

    private float GetReward()
    {
        float avg = 0f;
        
        // Vazeny priemer
        avg += (this.Health / this.maxHealth) * 0.50f;   // vaha zivotov
        avg += (this.Ammo / this.maxAmmo) * 0.05f;       // vaha municie
        avg += (this.Fuel / this.maxFuel) * 0.15f;       // vaha paliva
        avg += (this.NumOfPlanets / 5f) * 0.30f;         // vaha poctu ziskanych planet
        
        if (myPlanets.Count >= 4)
            Debug.Log($"health({this.name}) = {this.Health}, ammo = {this.Ammo}, fuel = {this.Fuel}, planets = {this.NumOfPlanets}");

        return (avg / 4f);
    }
}

// Varka udajov k nauceniu pre Q algoritmus
public class Batch
{
    // stary stav, ktory preucame
    public float[] state { get; set; }
    // stara akcia, ktoru sme vykonali
    public int action { get; set; }
    // odmena za (stav, akciu)
    public float reward { get; set; }
    // novy stav kam nas dostala akcia
    public float[] next_state { get; set; }
    // je koniec hernej epizody?
    public bool done { get; set; }
}

