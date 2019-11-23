using UnityEngine;
using System.Collections.Generic;

public class AIControlledShip : ShipController
{
    NeuralNetwork net1 = new NeuralNetwork();
    NeuralNetwork net2 = new NeuralNetwork();

    private bool waitingOnNextFrame = false;

    private int healthOld;

    private int ammoOld;

    private int fuelOld;

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

        net1.CreateLayer(NeuronLayerType.INPUT);
        net1.CreateLayer(NeuronLayerType.HIDDEN);
        net1.CreateLayer(NeuronLayerType.OUTPUT);

        net2.CreateLayer(NeuronLayerType.INPUT);
        net2.CreateLayer(NeuronLayerType.HIDDEN);
        net2.CreateLayer(NeuronLayerType.OUTPUT);

        net1.neuronLayers[1].Edge = net1.neuronLayers[0];
        net1.neuronLayers[2].Edge = net1.neuronLayers[1];
        net1.neuronLayers[0].BPG_egdes = net1.neuronLayers[1];
        net1.neuronLayers[1].BPG_egdes = net1.neuronLayers[2];

        net2.neuronLayers[1].Edge = net2.neuronLayers[0];
        net2.neuronLayers[2].Edge = net2.neuronLayers[1];
        net2.neuronLayers[0].BPG_egdes = net2.neuronLayers[1];
        net2.neuronLayers[1].BPG_egdes = net2.neuronLayers[2];

        for (int i = 0; i < 24; i++)
        {
            net1.neuronLayers[0].CreateNeuron(34);
            net2.neuronLayers[0].CreateNeuron(34);

            // Skopiruj parametre sieti
            net2.neuronLayers[0].deltaWeights = net1.neuronLayers[0].deltaWeights;
            net2.neuronLayers[0].Weights      = net1.neuronLayers[0].Weights;
        }

        for (int i = 0; i < 14; i++)
        {
            net1.neuronLayers[1].CreateNeuron();
            net2.neuronLayers[1].CreateNeuron();

            // Skopiruj parametre sieti
            net2.neuronLayers[1].deltaWeights = net1.neuronLayers[1].deltaWeights;
            net2.neuronLayers[1].Weights      = net1.neuronLayers[1].Weights;
        }

        for (int i = 0; i < 5; i++)
        {
            net1.neuronLayers[2].CreateNeuron();
            net2.neuronLayers[2].CreateNeuron();

            // Skopiruj parametre sieti
            net2.neuronLayers[2].deltaWeights = net1.neuronLayers[2].deltaWeights;
            net2.neuronLayers[2].Weights      = net1.neuronLayers[2].Weights;
        }
    }

    public override void Update()
    {        
        base.Update();
        
        if (!this.IsDestroyed)
        {
            int action;
            var radarResult = Sensors.Radar.Scan(this.transform.position, this.LookDirection, this.transform);
            var state = new float[(radarResult.Length * 2) + 2];
            
            // Poloha lode v ramci herneho sveta
            state[0] = this.rigidbody2d.position.x / 20f;
            state[1] = this.rigidbody2d.position.y / 20f;
            //Debug.Log($"state[0] = {state[0]}, state[1] = {state[1]}");

            for (int i = 2, j = 0; i < state.Length; i+=2, j++)
            {
                if (radarResult[j] != null)
                {
                    state[i] = (float)radarResult[j].Value.transform.tag.GetHashCode() / (float)int.MaxValue;
                    state[i+1] = radarResult[j].Value.distance / 10f;
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
                if (randomGen.NextFloat() <= 0.2f)
                {
                    action = randomGen.NextInt(0,5);
                }
                else
                {
                    // Vyber akciu
                    action = GetMaxQ(net1.neuronLayers[2].Neurons);
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

                // Exploration/Exploitation problem
                if (randomGen.NextFloat() <= 0.2f)
                {
                    action = randomGen.NextInt(0,5);
                }
                else
                {
                    // Vyber akciu
                    action = GetMaxQ(net2.neuronLayers[2].Neurons);
                }

                var reward = GetReward();
                var o = new float[5] 
                { 
                    net1.neuronLayers[2].Neurons[0].output, 
                    net1.neuronLayers[2].Neurons[1].output, 
                    net1.neuronLayers[2].Neurons[2].output, 
                    net1.neuronLayers[2].Neurons[3].output, 
                    net1.neuronLayers[2].Neurons[4].output, 
                };
                o[actionOld] = reward + 0.6f * net2.neuronLayers[2].Neurons[action].output;

                // Vykonaj akciu
                DoAction(action);

                // Pretrenuj hlavnu siet
                net1.Training(stateOld, o);

                // Skopiruj parametre sieti
                net2.neuronLayers[2].deltaWeights = net1.neuronLayers[2].deltaWeights;
                net2.neuronLayers[2].Weights      = net1.neuronLayers[2].Weights;

                this.waitingOnNextFrame = false;
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
                    //turret.SetPosition();
                    turret.Fire();
                }
                break;
            }
        }
    }

    private float GetReward()
    {
        float reward = 0f;

        if (this.Health < this.healthOld)
        {
            if (this.Health <= 0)
                reward += -100f;
            else
                reward += -1f;            
        }
        else if (this.Health > this.healthOld)
        {
            reward += +1f;            
        }

        if (this.Ammo < this.ammoOld)
        {
            reward += -0.04f;            
        }
        else if (this.Ammo > this.ammoOld)
        {
            reward += +0.04f;            
        }

        if (this.Fuel < this.fuelOld)
        {
            reward += -0.04f;            
        }
        else if (this.Fuel > this.fuelOld)
        {
            reward += +0.04f;            
        }
        
        // Nenastala zmena ... hladna politika ... kroky, ktore nevedu k vyvoju su zbytocne
        if (reward == 0f)
            reward = -0.01f;

        //Debug.Log($"reward = {reward}");
        
        return reward;
    }

    public override void ChangeHealth(int amount)
    {
        this.healthOld = this.Health;

        base.ChangeHealth(amount);
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
