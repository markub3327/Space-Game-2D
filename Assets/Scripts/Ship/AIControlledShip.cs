using UnityEngine;
using System.Collections.Generic;

public class AIControlledShip : ShipController
{
    public NeuralNetwork net1 = new NeuralNetwork();
    public NeuralNetwork net2 = new NeuralNetwork();

    private bool waitingOnNextFrame = false;

    private int healthOld;

    private int ammoOld;

    private int fuelOld;

    public int planetsOld;

    // Geneticky algoritmus vyberu najsilnejsieho jedinca v hre
    private float fitnessCounter;
    public float fitness { get; set; }       // ohodnotenie jedinca v ramci hre

    private float[] stateOld = null;

    private int actionOld;

    public TurretController[] turretControllers;

    private Unity.Mathematics.Random randomGen = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);

    public override void Start()
    {
        base.Start();

        this.healthOld = this.Health;
        this.ammoOld = this.Ammo;
        this.fuelOld = this.Fuel;
        this.planetsOld = this.NumOfPlanets;
        this.fitnessCounter = 0f;
        this.fitness = 0f;

        net1.CreateLayer(NeuronLayerType.INPUT);
        net1.CreateLayer(NeuronLayerType.HIDDEN);
        net1.CreateLayer(NeuronLayerType.HIDDEN);
        net1.CreateLayer(NeuronLayerType.OUTPUT);

        net2.CreateLayer(NeuronLayerType.INPUT);
        net2.CreateLayer(NeuronLayerType.HIDDEN);
        net2.CreateLayer(NeuronLayerType.HIDDEN);
        net2.CreateLayer(NeuronLayerType.OUTPUT);

        net1.neuronLayers[1].Edge = net1.neuronLayers[0];
        net1.neuronLayers[2].Edge = net1.neuronLayers[1];
        net1.neuronLayers[3].Edge = net1.neuronLayers[2];
        net1.neuronLayers[0].BPG_egdes = net1.neuronLayers[1];
        net1.neuronLayers[1].BPG_egdes = net1.neuronLayers[2];
        net1.neuronLayers[2].BPG_egdes = net1.neuronLayers[3];

        net2.neuronLayers[1].Edge = net2.neuronLayers[0];
        net2.neuronLayers[2].Edge = net2.neuronLayers[1];
        net2.neuronLayers[3].Edge = net2.neuronLayers[2];
        net2.neuronLayers[0].BPG_egdes = net2.neuronLayers[1];
        net2.neuronLayers[1].BPG_egdes = net2.neuronLayers[2];
        net2.neuronLayers[2].BPG_egdes = net2.neuronLayers[3];

        // Vstupna vrstva
        for (int i = 0; i < 34; i++)
        {
            net1.neuronLayers[0].CreateNeuron(34);
            net2.neuronLayers[0].CreateNeuron(34);

            // Skopiruj parametre sieti
            net2.neuronLayers[0].deltaWeights = net1.neuronLayers[0].deltaWeights;
            net2.neuronLayers[0].Weights      = net1.neuronLayers[0].Weights;
        }

        // Skryta vrstva #1
        for (int i = 0; i < 68; i++)
        {
            net1.neuronLayers[1].CreateNeuron();
            net2.neuronLayers[1].CreateNeuron();

            // Skopiruj parametre sieti
            net2.neuronLayers[1].deltaWeights = net1.neuronLayers[1].deltaWeights;
            net2.neuronLayers[1].Weights      = net1.neuronLayers[1].Weights;
        }

        // Skryta vrstva #2
        for (int i = 0; i < 36; i++)
        {
            net1.neuronLayers[2].CreateNeuron();
            net2.neuronLayers[2].CreateNeuron();

            // Skopiruj parametre sieti
            net2.neuronLayers[2].deltaWeights = net1.neuronLayers[2].deltaWeights;
            net2.neuronLayers[2].Weights      = net1.neuronLayers[2].Weights;
        }

        // Vystupna vrstva
        for (int i = 0; i < 5; i++)
        {
            net1.neuronLayers[3].CreateNeuron();
            net2.neuronLayers[3].CreateNeuron();

            // Skopiruj parametre sieti
            net2.neuronLayers[3].deltaWeights = net1.neuronLayers[3].deltaWeights;
            net2.neuronLayers[3].Weights      = net1.neuronLayers[3].Weights;
        }
    }

    public override void Update()
    {        
        base.Update();
        
        if (!this.IsDestroyed)
        {
            int action;
            var radarResult = Sensors.Radar.Scan(this.transform.position, this.LookDirection, this.transform);
            var state = new float[34];
            
            // Poloha lode v ramci herneho sveta
            state[0] = this.rigidbody2d.position.x / 20f;
            state[1] = this.rigidbody2d.position.y / 20f;
            //Debug.Log($"state[0] = {state[0]}, state[1] = {state[1]}");

            for (int i = 2, j = 0; i < state.Length; i+=2, j++)
            {
                if (radarResult[j] != null)
                {
                    state[i] = (float)radarResult[j].Value.transform.tag.GetHashCode() / (float)int.MaxValue;
                    state[i+1] = radarResult[j].Value.distance / 16f;
                }
                else
                {
                    state[i] = 0x00;
                    state[i+1] = 0x00;
                }
                //Debug.Log($"state[{i}] = {state[i]}, state[{i+1}] = {state[i+1]}");
            }

            if (!waitingOnNextFrame)
            {
                // Spusti hlavnu siet
                net1.Run(state);

                // Exploration/Exploitation problem
                if (randomGen.NextFloat() <= 0.25f) // 20% nahoda, 80% podla vedomosti
                {
                    action = randomGen.NextInt(0,5);
                }
                else
                {
                    // Vyber akciu
                    action = GetMaxQ(net1.neuronLayers[3].Neurons);
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
                net2.Run(state);

                // Vyber akciu
                action = GetMaxQ(net2.neuronLayers[3].Neurons);

                // Ziskaj spatnu vazbu z hry
                var reward = GetReward();
                this.fitnessCounter += reward;
                var o = new float[5] 
                { 
                    0.0f,
                    0.0f,
                    0.0f,
                    0.0f,
                    0.0f
                };
                // priprav ocakavany vystup
                o[actionOld] = (reward + 0.9f * net2.neuronLayers[3].Neurons[action].output);

                Debug.Log($"err = {o[actionOld] - net1.neuronLayers[3].Neurons[actionOld].output}");

                // Vykonaj akciu
                DoAction(action);

                // Pretrenuj hlavnu siet
                net1.Training(stateOld, o);

                // Skopiruj parametre sieti
                for (int i = 0; i < 4; i++)
                {
                    net2.neuronLayers[i].deltaWeights = net1.neuronLayers[i].deltaWeights;
                    net2.neuronLayers[i].Weights      = net1.neuronLayers[i].Weights;
                }

                this.waitingOnNextFrame = false;
            }

            //Debug.Log($"Action = {action}");    
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
        if (this.Health < this.healthOld)
        {
            if (this.Health <= 0)
                reward += (-1.0f);
            else
                reward += (-0.16f);            
        }
        else if (this.Health > this.healthOld)
        {
            reward += (+0.16f);
        }

        // Ziskaj skore z ukazatela poctu municie
        if (this.Ammo < this.ammoOld)
        {
            reward += (-0.01f);
        }
        else if (this.Ammo > this.ammoOld)
        {
            reward += (+0.01f);            
        }

        // Ziskaj skore z ukazatela poctu paliva
        if (this.Fuel < this.fuelOld)
        {
            reward += (-0.04f);            
        }
        else if (this.Fuel > this.fuelOld)
        {
            reward += (+0.04f);            
        }
        
        if (this.NumOfPlanets < this.planetsOld)
        {
            reward += (-0.08f);
        }
        else if (this.NumOfPlanets > this.planetsOld)
        {
            if (this.NumOfPlanets >= 5)
                reward += (+1.0f);
            else
                reward += (+0.08f);
        }

        reward = Mathf.Clamp(reward, -1f, 1f);

        Debug.Log($"reward = {reward}");
        Debug.Log($"Num of planets = {this.NumOfPlanets}");
        Debug.Log($"Health = {this.Health}");
        Debug.Log($"Ammo = {this.Ammo}");
        Debug.Log($"Fuel = {this.Fuel}");

        return reward;
    }

    public override void ChangeHealth(int amount)
    {
        this.healthOld = this.Health;

        if (!this.IsDestroyed)
        {
            base.ChangeHealth(amount);

            if (this.Health <= 0f)
            {
                this.fitness = this.fitnessCounter;
                this.fitnessCounter = 0f;
            }
        }
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
