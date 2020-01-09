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
    [DeallocateOnJobCompletion, ReadOnly]
    public NativeArray<float> Feedback;

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

        // Vypocitaj chybu siete podla vystupu metodou Huber loss function
        var diff = Feedback[index] - neuron.output;

        if (math.abs(diff) > 1.0f)
            // MAE (Mean absolute error)
            neuron.sigma = math.sign(diff) * NeuronFn.derivELU(neuron.output);
        else
            // MSE (Mean squared error)
            neuron.sigma = diff * NeuronFn.derivELU(neuron.output);         
        
        //Debug.Log($"sigma = {neuron.sigma}");

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