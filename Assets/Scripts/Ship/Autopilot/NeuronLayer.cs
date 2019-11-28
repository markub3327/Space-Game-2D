using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;

public class NeuronLayer
{
    public List<Neuron> Neurons;

    public List<float> Weights;

    public List<float> deltaWeights;

    public NeuronLayer BPG_egdes;        // hrana ucenia BPG pre spatne sirenie chyby neuronu

    public NeuronLayer Edge;

    private Unity.Mathematics.Random randGen;

    public NeuronLayerType type;    

    public NeuronLayer(NeuronLayerType type, NeuronLayer BPG_egdes = null, NeuronLayer Edge = null)
    {
        this.Neurons = new List<Neuron>();
        this.Weights = new List<float>();
        this.deltaWeights = new List<float>();
        this.randGen = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);

        this.BPG_egdes = BPG_egdes;
        this.Edge = Edge;
        this.type = type;
    }

    public void CreateNeuron(int num_of_inputs)
    {
        this.Neurons.Add(new Neuron {
            output = 0f,
            sigma = 0f,
            IndexW = this.Weights.Count,
            num_of_inputs = num_of_inputs,
            momentum = 0.0001f,
            learning_rate = 0.0005f
        });

        for (int n = 0; n < num_of_inputs; n++)
        {
            var w = randGen.NextFloat(-1f, 1f);
            this.Weights.Add(w);
            this.deltaWeights.Add(0f);
        }
    }

    public void CreateNeuron()
    {
        this.Neurons.Add(new Neuron {
            output = 0f,
            sigma = 0f,
            IndexW = this.Weights.Count,
            num_of_inputs = Edge.Neurons.Count,
            momentum = 0.0001f,
            learning_rate = 0.0005f
        });

        for (int n = 0; n <= Edge.Neurons.Count; n++)
        {
            var w = randGen.NextFloat(-1f, 1f);
            this.Weights.Add(w);
            this.deltaWeights.Add(0f);
        }
    }

    public void Run()
    {
        var neuronsNative = new NativeArray<Neuron>(Neurons.ToArray(), Allocator.TempJob);
        var weightsNative = new NativeArray<float>(Weights.ToArray(), Allocator.TempJob);

        // Rozdelenie prace medzi CPU jadrami
        var batchSize = this.Neurons.Count / SystemInfo.processorCount;
        if (batchSize < 1) batchSize = 1;
        //Debug.Log($"batchSize = {batchSize}, Num. of CPUs = {SystemInfo.processorCount}");
        
        var job = new NeuronJob
        {
            Edges = new NativeArray<Neuron>(Edge.Neurons.ToArray(), Allocator.TempJob),
            Neurons      =  neuronsNative,
            Weights      =  weightsNative
        };

        // Naplanuj spustenie paralelnej operacie
        job.Schedule(this.Neurons.Count, batchSize).Complete();
        
        this.Neurons = new List<Neuron>(neuronsNative.ToArray());

        //for (int i = 0; i < this.Neurons.Count; i++)
        //{
            //Debug.Log($"neuron[{i}].out = {this.Neurons[i].output}");
            //Debug.Log($"neuron[{i}].alpha = {this.Neurons[i].alpha}");
        //}

        neuronsNative.Dispose();
        weightsNative.Dispose();
    }

    public void Run(float[] Input)
    {
        var neuronsNative = new NativeArray<Neuron>(Neurons.ToArray(), Allocator.TempJob);
        var weightsNative = new NativeArray<float>(Weights.ToArray(), Allocator.TempJob);
        
        // Rozdelenie prace medzi CPU jadrami
        var batchSize = this.Neurons.Count / SystemInfo.processorCount;
        if (batchSize < 1) batchSize = 1;
        //Debug.Log($"batchSize = {batchSize}, Num. of CPUs = {SystemInfo.processorCount}");
    
        var job = new NeuronInJob
        {
            Input        =  new NativeArray<float>(Input, Allocator.TempJob),
            Neurons      =  neuronsNative,
            Weights      =  weightsNative
        };

        // Naplanuj spustenie paralelnej operacie
        job.Schedule(this.Neurons.Count, batchSize).Complete();
        
        this.Neurons = new List<Neuron>(neuronsNative.ToArray());

        //for (int i = 0; i < this.Neurons.Count; i++)
        //{
        //    Debug.Log($"neuron[{i}].out = {this.Neurons[i].output}");
            //Debug.Log($"neuron[{i}].alpha = {this.Neurons[i].alpha}");
        //}

        neuronsNative.Dispose();
        weightsNative.Dispose();
    }

    public void RunTraining()
    {
        var neuronsNative = new NativeArray<Neuron>(Neurons.ToArray(), Allocator.TempJob);
        var weightsNative = new NativeArray<float>(Weights.ToArray(), Allocator.TempJob);
        var deltaWeightsNative = new NativeArray<float>(deltaWeights.ToArray(), Allocator.TempJob);

        // Rozdelenie prace medzi CPU jadrami
        var batchSize = this.Neurons.Count / SystemInfo.processorCount;
        if (batchSize < 1) batchSize = 1;
        //Debug.Log($"batchSize = {batchSize}, Num. of CPUs = {SystemInfo.processorCount}");
        
        var job = new NeuronTrainingJob
        {
            Edges        =  new NativeArray<Neuron>(Edge.Neurons.ToArray(), Allocator.TempJob),
            BPG_egdes    =  new NativeArray<Neuron>(BPG_egdes.Neurons.ToArray(), Allocator.TempJob),
            BPG_weights  =  new NativeArray<float>(BPG_egdes.Weights.ToArray(), Allocator.TempJob),
            Neurons      =  neuronsNative,
            Weights      =  weightsNative,
            deltaWeights =  deltaWeightsNative
        };

        // Naplanuj spustenie paralelnej operacie
        job.Schedule(this.Neurons.Count, batchSize).Complete();
        
        this.Neurons = new List<Neuron>(neuronsNative.ToArray());
        this.Weights = new List<float>(weightsNative.ToArray());
        this.deltaWeights = new List<float>(deltaWeightsNative.ToArray());

        //for (int i = 0; i < this.Neurons.Count; i++)
        //    Debug.Log($"neuron[{i}].sigma = {this.Neurons[i].sigma}");

        neuronsNative.Dispose();
        weightsNative.Dispose();
        deltaWeightsNative.Dispose();
    }

    public void RunTraining(float[] Input)
    {
        var neuronsNative = new NativeArray<Neuron>(Neurons.ToArray(), Allocator.TempJob);
        var weightsNative = new NativeArray<float>(Weights.ToArray(), Allocator.TempJob);
        var deltaWeightsNative = new NativeArray<float>(deltaWeights.ToArray(), Allocator.TempJob);

        // Rozdelenie prace medzi CPU jadrami
        var batchSize = this.Neurons.Count / SystemInfo.processorCount;
        if (batchSize < 1) batchSize = 1;
        //Debug.Log($"batchSize = {batchSize}, Num. of CPUs = {SystemInfo.processorCount}");

        var job = new NeuronInTrainingJob
        {
            Input        =  new NativeArray<float>(Input, Allocator.TempJob),
            BPG_egdes    =  new NativeArray<Neuron>(BPG_egdes.Neurons.ToArray(), Allocator.TempJob),
            BPG_weights  =  new NativeArray<float>(BPG_egdes.Weights.ToArray(), Allocator.TempJob),
            Neurons      =  neuronsNative,
            Weights      =  weightsNative,
            deltaWeights =  deltaWeightsNative
        };

        // Naplanuj spustenie paralelnej operacie
        job.Schedule(this.Neurons.Count, batchSize).Complete();

        this.Neurons = new List<Neuron>(neuronsNative.ToArray());
        this.Weights = new List<float>(weightsNative.ToArray());
        this.deltaWeights = new List<float>(deltaWeightsNative.ToArray());

        //for (int i = 0; i < this.Neurons.Count; i++)
        //    Debug.Log($"neuron[{i}].sigma = {this.Neurons[i].sigma}");

        neuronsNative.Dispose();
        weightsNative.Dispose();    
        deltaWeightsNative.Dispose();
    }

    public void RunTraining(float[] o, ref float err)
    {
        var neuronsNative = new NativeArray<Neuron>(Neurons.ToArray(), Allocator.TempJob);
        var weightsNative = new NativeArray<float>(Weights.ToArray(), Allocator.TempJob);
        var deltaWeightsNative = new NativeArray<float>(deltaWeights.ToArray(), Allocator.TempJob);

        // Rozdelenie prace medzi CPU jadrami
        var batchSize = this.Neurons.Count / SystemInfo.processorCount;
        if (batchSize < 1) batchSize = 1;
        //Debug.Log($"batchSize = {batchSize}, Num. of CPUs = {SystemInfo.processorCount}");

        var job = new NeuronOutTrainingJob
        {
            Edges    =  new NativeArray<Neuron>(Edge.Neurons.ToArray(), Allocator.TempJob),
            Feedback =  new NativeArray<float>(o, Allocator.TempJob),
            Neurons  =  neuronsNative,
            Weights  =  weightsNative,
            deltaWeights =  deltaWeightsNative
        };

        // Naplanuj spustenie paralelnej operacie
        job.Schedule(this.Neurons.Count, batchSize).Complete();
        
        this.Neurons = new List<Neuron>(neuronsNative.ToArray());
        this.Weights = new List<float>(weightsNative.ToArray());
        this.deltaWeights = new List<float>(deltaWeightsNative.ToArray());

        //for (int i = 0; i < this.Neurons.Count; i++)
        //    Debug.Log($"neuron[{i}].sigma = {this.Neurons[i].sigma}");

        neuronsNative.Dispose();
        weightsNative.Dispose();
        deltaWeightsNative.Dispose();
    }
}