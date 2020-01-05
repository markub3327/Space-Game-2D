using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NeuralLayer
{
    public List<Neuron> Neurons = new List<Neuron>();

    public List<float> Weights = new List<float>();
    
    public List<float> deltaWeights = new List<float>(); 

    public NeuralLayer BPG_egdes = null;       // hrana ucenia BPG pre spatne sirenie chyby neuronu

    public NeuralLayer Edges = null;

    public NeuronLayerType type;

    public NeuralLayer(NeuronLayerType type)
    {
        this.type = type;
    }

    public void CreateNeurons(int num_of_inputs, int num_of_neurons)
    {
        var k = Unity.Mathematics.math.sqrt(2f/num_of_inputs);
        
        for (int m = 0; m < num_of_neurons; m++)
        {
            this.Neurons.Add(new Neuron {
                output = 0f,
                sigma = 0f,
                IndexW = this.Weights.Count,
                num_of_inputs = num_of_inputs,
                momentum = 0.9f,
                learning_rate = 0.001f
            });
            for (int n = 0; n <= num_of_inputs; n++)
            {
                var w = Random.Range(-1.0f, 1.0f) * k;
                this.Weights.Add(w);
                this.deltaWeights.Add(0.0f);
            }
        }
    }

    public void CreateNeurons(int num_of_neurons)
    {
        var num_of_inputs = Edges.Neurons.Count;        
        var k = Unity.Mathematics.math.sqrt(2f/num_of_inputs);

        for (int m = 0; m < num_of_neurons; m++)
        {
            this.Neurons.Add(new Neuron {
                output = 0f,
                sigma = 0f,
                IndexW = this.Weights.Count,
                num_of_inputs = num_of_inputs,
                momentum = 0.9f,
                learning_rate = 0.001f
            });
            for (int n = 0; n <= num_of_inputs; n++)
            {
                var w = Random.Range(-1.0f, 1.0f) * k;
                this.Weights.Add(w);
                this.deltaWeights.Add(0.0f);
            }
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
            Edges        =  new NativeArray<Neuron>(Edges.Neurons.ToArray(), Allocator.TempJob),
            Neurons      =  neuronsNative,
            Weights      =  weightsNative
        };

        // Naplanuj spustenie paralelnej operacie
        job.Schedule(this.Neurons.Count, batchSize).Complete();
        
        this.Neurons = new List<Neuron>(neuronsNative.ToArray());

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
            Edges        =  new NativeArray<Neuron>(Edges.Neurons.ToArray(), Allocator.TempJob),
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
            Edges    =  new NativeArray<Neuron>(Edges.Neurons.ToArray(), Allocator.TempJob),
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