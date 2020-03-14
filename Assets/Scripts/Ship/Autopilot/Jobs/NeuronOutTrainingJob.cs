using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct NeuronOutTrainingJob : IJobParallelFor
{
    // Vstup z hran
    [DeallocateOnJobCompletion, ReadOnly]
    public NativeArray<Neuron> Edges;

    // Spatna-vazba od hry 
    public float Feedback;
    public int idx;

     // Pole neuronov (entit)
    [NativeDisableParallelForRestriction]
    public NativeArray<Neuron> Neurons;

    // Matica vah ulozena do pola        
    [NativeDisableParallelForRestriction]
    public NativeArray<float> Weights;

    // Matica delta vah ulozena do pola        
    [NativeDisableParallelForRestriction]
    public NativeArray<float> deltaWeights;


    public void Execute(int index)
    {
        var neuron = this.Neurons[index];
        float diff;

        // Vypocitaj chybu siete podla vystupu metodou Huber loss function
        if (index == idx)
        {
            diff = Feedback - neuron.output;
        }
        else
        {
            diff = 0f;            
        }
        neuron.sigma = diff * NeuronFn.derivELU(neuron.output);
        //Debug.Log($"sigma = {neuron.sigma}");
        //Debug.Log($"diff = {diff}");

        // Adaptuj vahy podla chyby neuronu
        for (int n = 0; n < neuron.num_of_inputs; n++)
        {
            deltaWeights[neuron.IndexW + n] = neuron.learning_rate * neuron.sigma * Edges[n].output + (neuron.momentum * deltaWeights[neuron.IndexW + n]);
            Weights[neuron.IndexW + n] += deltaWeights[neuron.IndexW + n];
        }

        // Bias
        deltaWeights[neuron.IndexW + neuron.num_of_inputs] = neuron.learning_rate * neuron.sigma + (neuron.momentum * deltaWeights[neuron.IndexW + neuron.num_of_inputs]);
        Weights[neuron.IndexW + neuron.num_of_inputs] += deltaWeights[neuron.IndexW + neuron.num_of_inputs];

        // Copy back
        this.Neurons[index] = neuron;
    }
}