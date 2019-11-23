using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;


[BurstCompile]
public struct NeuronInTrainingJob : IJobParallelFor
{
    // Vstup z herneho prostredia
    [DeallocateOnJobCompletion, ReadOnly]
    public NativeArray<float> Input;

    // Spatna-vazba od nasledujucej vrstvy
    [DeallocateOnJobCompletion, ReadOnly]
    public NativeArray<Neuron> BPG_egdes;

    // Vahy neuronov od spatnej-vazby
    [DeallocateOnJobCompletion, ReadOnly]
    public NativeArray<float> BPG_weights;

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

        // Spätne sirenie chyby po sieti
        float sum = 0f;
        for (int i = 0; i < BPG_egdes.Length; i++)
        {
            sum += BPG_egdes[i].sigma * BPG_weights[BPG_egdes[i].IndexW + index];
            //Debug.Log($"BPG_sigma[{i}] = {BPG_egdes[i].sigma}, BPG_weights[{BPG_egdes[i].IndexW + index}] = {BPG_weights[BPG_egdes[i].IndexW + index]}");
        }                
        neuron.sigma = sum * NeuronFn.derivELU(neuron.output/*, neuron.alpha*/);
        
        // Adaptuj vahy podla chyby neuronu
        for (int n = 0; n < neuron.num_of_inputs; n++)
        {
            deltaWeights[neuron.IndexW + n] = neuron.learning_rate * neuron.sigma * Input[n] + neuron.momentum * deltaWeights[neuron.IndexW + n];
            Weights[neuron.IndexW + n] += deltaWeights[neuron.IndexW + n];
        }
        
        // Bias
        deltaWeights[neuron.IndexW + neuron.num_of_inputs] = neuron.learning_rate * neuron.sigma + neuron.momentum * deltaWeights[neuron.IndexW + neuron.num_of_inputs];
        Weights[neuron.IndexW + neuron.num_of_inputs] += deltaWeights[neuron.IndexW + neuron.num_of_inputs];
        
        // Copy back
        this.Neurons[index] = neuron;
    }
}