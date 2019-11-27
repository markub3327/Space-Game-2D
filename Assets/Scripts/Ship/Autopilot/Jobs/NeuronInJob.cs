using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct NeuronInJob : IJobParallelFor
{        
    // Vstup z herneho prostredia
    [DeallocateOnJobCompletion, ReadOnly]
    public NativeArray<float> Input;

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

        // Matematicka operacia vyuzivajuca instrukcnu sadu SIMD
        neuron.output = math.mul(Weights[neuron.IndexW], Input[index]);
        //Debug.Log($"Input[{n}] = {Input[n]}");
        neuron.output = NeuronFn.ELU(neuron.output);

        // Copy back
        this.Neurons[index] = neuron;
    }
}