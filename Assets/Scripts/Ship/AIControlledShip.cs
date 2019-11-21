using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;

public class AIControlledShip : ShipController
{
    NeuronLayer layer1 = new NeuronLayer();
    NeuronLayer layer2 = new NeuronLayer();
    NeuronLayer layer3 = new NeuronLayer();

    private readonly float[][] inputTestVector = new float[][] 
    {
        new float[] { 0f, 0f },
        new float[] { 1f, 0f },
        new float[] { 0f, 1f },
        new float[] { 1f, 1f }
    };

    private readonly float[][] outputTestVector = new float[][] 
    {
        new float[] { /*0f, 0f,*/ 0f },
        new float[] { /*1f, 0f,*/ 1f },
        new float[] { /*1f, 0f,*/ 1f },
        new float[] { /*1f, 1f,*/ 0f }
    };

    private int v_idx = 0;

    private float err = 0f;

    private int timeTraining = 0;

    public override void Start()
    {
        base.Start();
        
        layer1.BPG_egdes = layer2;
        layer2.BPG_egdes = layer3;

        layer2.Edge = layer1;
        layer3.Edge = layer2;

        layer1.CreateNeuron(2, NeuronType.INPUT);
        layer1.CreateNeuron(2, NeuronType.INPUT);

        layer2.CreateNeuron(NeuronType.HIDDEN);
        layer2.CreateNeuron(NeuronType.HIDDEN);

        layer3.CreateNeuron(NeuronType.OUTPUT);
        //layer3.CreateNeuron(NeuronType.OUTPUT);
        //layer3.CreateNeuron(NeuronType.OUTPUT);
    }

    public override void Update()
    {
        base.Update();
        
        //var v = (int)UnityEngine.Time.time % 4;
        if (timeTraining < 10000)
        {
            if (v_idx < 4)
            {
                Debug.Log($"Tested on vector {v_idx}: ");
                Debug.Log($"Layer 1: ");
                layer1.Run(inputTestVector[v_idx]);
                Debug.Log($"Layer 2: ");
                layer2.Run();
                Debug.Log($"Layer 3: ");
                layer3.Run();

                err += math.pow((outputTestVector[v_idx][0] - layer3.Neurons[0].output), 2);

                layer3.RunTraining(null, outputTestVector[v_idx]);
                layer2.RunTraining();
                layer1.RunTraining(inputTestVector[v_idx]);

                v_idx++;
            }
            else
            {
                v_idx = 0;            

                err = math.sqrt(err / 4f);
                Debug.Log($"avgErr = {err}, timeTraining = {timeTraining}");
                err = 0f;
            }

            timeTraining++;
        }
    }   
}
