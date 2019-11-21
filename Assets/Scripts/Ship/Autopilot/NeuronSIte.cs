using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;

public class NeuronLayer
{
    private const float learning_rate = 0.1f;

    public List<Neuron> Neurons;

    public List<float> Weights;

    public NeuronLayer BPG_egdes;        // hrana ucenia BPG pre spatne sirenie chyby neuronu

    public NeuronLayer Edge;

    private Unity.Mathematics.Random randGen;
    

    [BurstCompile]
    private struct NeuronsJob : IJobParallelFor
    {        
        // Vstup z herneho prostredia
        [DeallocateOnJobCompletion, ReadOnly]
        public NativeArray<float> Input;

        // Vstup z hran
        [DeallocateOnJobCompletion, ReadOnly]
        public NativeArray<Neuron> Edges;

        // Pole neuronov (entit)
        [NativeDisableParallelForRestriction]
        public NativeArray<Neuron> Neurons;

        // Matica vah ulozena do pola        
        [NativeDisableParallelForRestriction]
        public NativeArray<float> Weights;


        // index = Id neuronu
        public void Execute(int index)
        {
            var neuron = this.Neurons[index];

            // Vazena suma
            neuron.output = 0f;
            if (neuron.type == NeuronType.INPUT)
            {
                for (int n = 0; n < neuron.num_of_inputs; n++)
                {                
                    // Matematicka operacia vyuzivajuca instrukcnu sadu SIMD
                    neuron.output += math.mul(Weights[neuron.IndexW + n], Input[n]);
                    //Debug.Log($"Input[{n}] = {Input[n]}");
                }
            }
            else
            {
                for (int n = 0; n < neuron.num_of_inputs; n++)
                {                
                    // Matematicka operacia vyuzivajuca instrukcnu sadu SIMD
                    neuron.output += math.mul(Weights[neuron.IndexW + n], Edges[n].output);
                    //Debug.Log($"Input[{n}] = {Edges[n].output}");
                }
            }
            neuron.output = NeuronFn.ELU(neuron.output);

            // Copy back
            this.Neurons[index] = neuron;
        }
    }

    [BurstCompile]
    private struct NeuronTrainingJob : IJobParallelFor
    {
        // Vstup z herneho prostredia
        [DeallocateOnJobCompletion, ReadOnly]
        public NativeArray<float> Input;

        // Vstup z hran
        [DeallocateOnJobCompletion, ReadOnly]
        public NativeArray<Neuron> Edges;

        // Spatna-vazba od nasledujucej vrstvy
        [DeallocateOnJobCompletion, ReadOnly]
        public NativeArray<Neuron> BPG_egdes;

        // Vahy neuronov od spatnej-vazby
        [DeallocateOnJobCompletion, ReadOnly]
        public NativeArray<float> BPG_weights;

        // Spatna-vazba od hry 
        [DeallocateOnJobCompletion, ReadOnly]
        public NativeArray<float> Feedback;

        // Pole neuronov (entit)
        [NativeDisableParallelForRestriction]
        public NativeArray<Neuron> Neurons;

        // Matica vah ulozena do pola        
        [NativeDisableParallelForRestriction]
        public NativeArray<float> Weights;


        public void Execute(int index)
        {
            var neuron = this.Neurons[index];

            // Vypocitaj chybu siete podla vystupu
            if (neuron.type == NeuronType.OUTPUT)
            {
                neuron.sigma = (Feedback[index] - neuron.output) * NeuronFn.derivELU(neuron.output);                
            }
            // Spätne sirenie chyby po sieti
            else
            {
                float sum = 0f;
                for (int i = 0; i < BPG_egdes.Length; i++)
                {
                    sum += BPG_egdes[i].sigma * BPG_weights[BPG_egdes[i].IndexW + index];
                    //Debug.Log($"BPG_sigma[{i}] = {BPG_egdes[i].sigma}, BPG_weights[{BPG_egdes[i].IndexW + index}] = {BPG_weights[BPG_egdes[i].IndexW + index]}");
                }                
                neuron.sigma = sum * NeuronFn.derivELU(neuron.output);
            }

            // Adaptuj vahy podla chyby neuronu
            if (neuron.type == NeuronType.INPUT)
            {
                for (int n = 0; n < neuron.num_of_inputs; n++)
                {
                    Weights[neuron.IndexW + n] += learning_rate * neuron.sigma * Input[n];
                }
            }
            else
            {
                for (int n = 0; n < neuron.num_of_inputs; n++)
                {
                    Weights[neuron.IndexW + n] += learning_rate * neuron.sigma * Edges[n].output;
                }
            }

            // Copy back
            this.Neurons[index] = neuron;
        }
    }


    public NeuronLayer(NeuronLayer BPG_egdes = null, NeuronLayer Edge = null)
    {
        this.Neurons = new List<Neuron>();
        this.Weights = new List<float>();
        this.randGen = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);

        this.BPG_egdes = BPG_egdes;
        this.Edge = Edge;
    }

    public void CreateNeuron(int num_of_inputs, NeuronType type)
    {
        this.Neurons.Add(new Neuron {
            output = 0f,
            IndexW = this.Weights.Count,
            type = type,
            num_of_inputs = num_of_inputs
        });

        for (int n = 0; n < num_of_inputs; n++)
        {
            var w = 0.1f * n + 0.1f;//randGen.NextFloat(0f, 1f);
            this.Weights.Add(w);
        }
    }

    public void CreateNeuron(NeuronType type)
    {
        this.Neurons.Add(new Neuron {
            output = 0f,
            IndexW = this.Weights.Count,
            type = type,
            num_of_inputs = Edge.Neurons.Count
        });

        for (int n = 0; n < Edge.Neurons.Count; n++)
        {
            var w = 0.1f * n + 0.1f;//randGen.NextFloat(0f, 1f);
            this.Weights.Add(w);
        }
    }

    public void Run(float[] Input = null)
    {
        var neuronsNative = new NativeArray<Neuron>(Neurons.ToArray(), Allocator.TempJob);
        var weightsNative = new NativeArray<float>(Weights.ToArray(), Allocator.TempJob);
        
        // Rozdelenie prace medzi CPU jadrami
        var batchSize = this.Neurons.Count / SystemInfo.processorCount;
        if (batchSize < 1) batchSize = 1;
        //Debug.Log($"batchSize = {batchSize}, Num. of CPUs = {SystemInfo.processorCount}");

        var job = new NeuronsJob
        {
            Input        =  (Input != null) ? new NativeArray<float>(Input, Allocator.TempJob) : new NativeArray<float>(0, Allocator.TempJob),
            Edges        =  (Edge != null) ? new NativeArray<Neuron>(Edge.Neurons.ToArray(), Allocator.TempJob) : new NativeArray<Neuron>(0, Allocator.TempJob),
            Neurons      =  neuronsNative,
            Weights      =  weightsNative
        };

        // Naplanuj spustenie paralelnej operacie
        job.Schedule(this.Neurons.Count, batchSize).Complete();

        this.Neurons = new List<Neuron>(neuronsNative.ToArray());

        for (int i = 0; i < this.Neurons.Count; i++)
            Debug.Log($"neuron[{i}].out = {this.Neurons[i].output}");

        neuronsNative.Dispose();
        weightsNative.Dispose();
    }

    public void RunTraining(float[] Input = null, float[] o = null)
    {
        var neuronsNative = new NativeArray<Neuron>(Neurons.ToArray(), Allocator.TempJob);
        var weightsNative = new NativeArray<float>(Weights.ToArray(), Allocator.TempJob);

        NativeArray<Neuron> BPG_edgesNative;
        NativeArray<float>  BPG_weightsNative;

        // Rozdelenie prace medzi CPU jadrami
        var batchSize = this.Neurons.Count / SystemInfo.processorCount;
        if (batchSize < 1) batchSize = 1;
        //Debug.Log($"batchSize = {batchSize}, Num. of CPUs = {SystemInfo.processorCount}");

        if (BPG_egdes != null)
        {
            BPG_weightsNative = new NativeArray<float>(BPG_egdes.Weights.ToArray(), Allocator.TempJob);
            BPG_edgesNative = new NativeArray<Neuron>(BPG_egdes.Neurons.ToArray(), Allocator.TempJob);
        }
        else
        {
            BPG_weightsNative = new NativeArray<float>(0, Allocator.TempJob);
            BPG_edgesNative = new NativeArray<Neuron>(0, Allocator.TempJob);
        }

        var jobT = new NeuronTrainingJob
        {
            Input        =  (Input != null) ? new NativeArray<float>(Input, Allocator.TempJob) : new NativeArray<float>(0, Allocator.TempJob),
            Edges        =  (Edge != null) ? new NativeArray<Neuron>(Edge.Neurons.ToArray(), Allocator.TempJob) : new NativeArray<Neuron>(0, Allocator.TempJob),
            Feedback     =  (o != null) ? new NativeArray<float>(o, Allocator.TempJob) : new NativeArray<float>(0, Allocator.TempJob),
            BPG_egdes    =  BPG_edgesNative,
            BPG_weights  =  BPG_weightsNative,
            Neurons      =  neuronsNative,
            Weights      =  weightsNative
        };

        // Naplanuj spustenie paralelnej operacie
        jobT.Schedule(this.Neurons.Count, batchSize).Complete();

        this.Neurons = new List<Neuron>(neuronsNative.ToArray());
        this.Weights = new List<float>(weightsNative.ToArray());

        //for (int i = 0; i < this.Neurons.Count; i++)
        //    Debug.Log($"neuron[{i}].sigma = {this.Neurons[i].sigma}");

        neuronsNative.Dispose();
        weightsNative.Dispose();        
    }
}