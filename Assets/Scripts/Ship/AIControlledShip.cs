using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

public class AIControlledShip : ShipController
{
    public NeuralNetwork Qnet = new NeuralNetwork();
    public NeuralNetwork QTargetNet = new NeuralNetwork();

    private bool waitingOnNextFrame = false;

    private int healthOld;

    private int ammoOld;

    private int fuelOld;

    public int planetsOld;

    // Geneticky algoritmus vyberu najsilnejsieho jedinca v hre
    private float fitnessCounter;
    public float fitness { get; set; }       // ohodnotenie jedinca v ramci hre

    private float[] stateOld = null;

    private int actionOld = 0;

    public TurretController[] turretControllers;

    public bool testingMode = false;

    public override void Start()
    {
        base.Start();

        this.healthOld = this.Health;
        this.ammoOld = this.Ammo;
        this.fuelOld = this.Fuel;
        this.planetsOld = this.NumOfPlanets;
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

        // Vstupna vrstva
        for (int i = 0; i < 18; i++)
        {
            Qnet.neuronLayers[0].CreateNeuron(18);
            QTargetNet.neuronLayers[0].CreateNeuron(18);

            // Skopiruj parametre sieti
            QTargetNet.neuronLayers[0].deltaWeights = Qnet.neuronLayers[0].deltaWeights;
            QTargetNet.neuronLayers[0].Weights      = Qnet.neuronLayers[0].Weights;
        }

        // Skryta vrstva #1
        for (int i = 0; i < 36; i++)
        {
            Qnet.neuronLayers[1].CreateNeuron();
            QTargetNet.neuronLayers[1].CreateNeuron();
        }

        // Skryta vrstva #2
        for (int i = 0; i < 20; i++)
        {
            Qnet.neuronLayers[2].CreateNeuron();
            QTargetNet.neuronLayers[2].CreateNeuron();
        }

        // Vystupna vrstva
        for (int i = 0; i < 5; i++)
        {
            Qnet.neuronLayers[3].CreateNeuron();
            QTargetNet.neuronLayers[3].CreateNeuron();
        }
    }

    public override void Update()
    {        
        if (!this.IsDestroyed)
        {
            // Ak uz hrac nema zivoty znici sa lod
            if (this.Health <= 0)
            {
                DestroyShip();
            }

            int action;
            var radarResult = Sensors.Radar.Scan(this.transform.position, this.LookDirection, this.transform);
            var state = new float[18];
            
            // Poloha lode v ramci herneho sveta
            state[0] = this.rigidbody2d.position.x / 20f;
            state[1] = this.rigidbody2d.position.y / 20f;
            //Debug.Log($"state[0](X) = {state[0]}, state[1](Y) = {state[1]}");

            for (int i = 2, j = 0; i < state.Length; i++, j++)
            {
                if (radarResult[j] != null)
                {
                    state[i] = (float)radarResult[j].Value.transform.tag.GetHashCode() / (float)int.MaxValue;
                }
                else
                {
                    state[i] = 0x00;
                    //state[i+1] = 0x00;
                }
                //Debug.Log($"state[{i}](Radar) = {state[i]}, tag = {radarResult[j].Value.transform.tag}");
            }

            if (!waitingOnNextFrame)
            {
                // Spusti hlavnu siet
                Qnet.Run(state);

                // Exploration/Exploitation problem                
                if (randomGen.NextFloat() <= 0.25f && !testingMode) // 20% nahoda, 80% podla vedomosti
                {
                    action = randomGen.NextInt(0,5);
                }
                else
                {
                    // Vyber akciu
                    action = GetMaxQ(Qnet.neuronLayers[3].Neurons);
                }

                // Vykonaj akciu
                DoAction(action);
                
                // Uchovaj si minuly stav a akciu
                this.stateOld = state;
                this.actionOld = action;

                this.waitingOnNextFrame = true;
            }
            else
            {
                // Spusti sekundarnu siet
                QTargetNet.Run(state);

                // Vyber akciu
                action = GetMaxQ(QTargetNet.neuronLayers[3].Neurons);

                // Ziskaj spatnu vazbu z hry
                var reward = GetReward();
                this.fitnessCounter += reward;
                var o = new float[5] 
                { 
                    0f,//Qnet.neuronLayers[3].Neurons[0].output,
                    0f,//Qnet.neuronLayers[3].Neurons[1].output,
                    0f,//Qnet.neuronLayers[3].Neurons[2].output,
                    0f,//Qnet.neuronLayers[3].Neurons[3].output,
                    0f//Qnet.neuronLayers[3].Neurons[4].output
                };
                // priprav ocakavany vystup
                o[actionOld] = (reward + 0.5f * QTargetNet.neuronLayers[3].Neurons[action].output);
                //Debug.Log($"net.error = {o[actionOld] - Qnet.neuronLayers[3].Neurons[actionOld].output}");
               
                // Vykonaj akciu
                DoAction(action);

                // Pretrenuj hlavnu siet
                Qnet.Training(stateOld, o);

                // Skopiruj parametre sieti
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < QTargetNet.neuronLayers[i].Weights.Count; j++)
                    {
                        QTargetNet.neuronLayers[i].deltaWeights[j] = Qnet.neuronLayers[i].deltaWeights[j]; //0.01f * Qnet.neuronLayers[i].deltaWeights[j] + (1f - 0.01f) * QTargetNet.neuronLayers[i].deltaWeights[j];
                        QTargetNet.neuronLayers[i].Weights[j]      = Qnet.neuronLayers[i].Weights[j]; //0.01f * Qnet.neuronLayers[i].Weights[j]      + (1f - 0.01f) * QTargetNet.neuronLayers[i].Weights[j];
                    }
                }

                this.waitingOnNextFrame = false;
            }

            //Debug.Log($"Action = {action}");    
        }
        else
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer < 0.0f)
            {                
                RespawnShip();
                animator.SetTrigger("Respawn");

                this.healthOld      =  this.Health;
                this.ammoOld        =  this.Ammo;
                this.fuelOld        =  this.Fuel;
                this.planetsOld     =  this.NumOfPlanets;
                this.fitnessCounter =  0f;
            }
        }
    }

    private int GetMaxQ(List<Neuron> neurons)
    {
        var index = 0;
        for (int i = 1; i < neurons.Count; i++)
        {
            if (neurons[index].output < neurons[i].output)
                index = i;
        }
        return index;
    }

    private void DoAction(int action)
    {
        // Vykonaj akciu
        switch (action)
        {
            case 0x00:  // Sipka hore
                this.MoveShip(Vector2.up);
                break;
            case 0x01:  // Sipka dole
                this.MoveShip(Vector2.down);
                break;
            case 0x02:  // Sipka vlavo
                this.MoveShip(Vector2.left);
                break;
            case 0x03:  // Sipka vpravo
                this.MoveShip(Vector2.right);
                break;
            case 0x04:  // Fire
            {
                foreach (var turret in turretControllers)
                {
                    turret.Fire();
                }
                break;
            }
        }
    }

    private float GetReward()
    {
        float reward = 0f;

        // Ziskaj skore z ukazatela poctu zivotov
        if (this.IsDestroyed)
        {
            reward = (-1.0f);

            Debug.Log("Lod bola znicena!");
        }        
        else if (this.Health > this.healthOld || this.Ammo > this.ammoOld || this.Fuel > this.fuelOld)
        {
            reward = (+0.1f);
            Debug.Log("Lod ziskala jednu z Collectible itemov!");            
        }
        else if (this.Health < this.healthOld || this.Ammo < this.ammoOld || this.Fuel < this.fuelOld)
        {
            reward = (-0.1f);
            Debug.Log("Lod stratila zivot/municiu/palivo!");
        }
        else if (this.NumOfPlanets > this.planetsOld)
        {
            if (this.NumOfPlanets >= 5)
            {
                reward = (+1.0f);
                Debug.Log("Lod vyhrala ... ziskala vsetky planety");
            }
            else
            {
                reward = (+0.1f);   
                Debug.Log("Lod ziskala planetu!");
            }
        }
        else
        {
            reward = (-0.01f);
            Debug.Log("Zapocitana mala zmena -0.01 = symbol minania paliva.");
        }

        //Debug.Log($"reward = {reward}");

        return reward;
    }

    public override void ChangeHealth(int amount)
    {
        this.healthOld = this.Health;

        base.ChangeHealth(amount);

        if (this.Health <= 0.0f)
            this.fitness = fitnessCounter;
    }

    public override void ChangeAmmo(int amount)
    {
        this.ammoOld = this.Ammo;

        base.ChangeAmmo(amount);
    }

    public override void ChangeFuel(int amount)
    {
        this.fuelOld = this.Fuel;

        base.ChangeFuel(amount);
    }
}
