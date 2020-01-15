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

        // Vazena suma
        //neuron.output = 0f;
        //for (int n = 0; n < neuron.num_of_inputs; n++)
        //{                
            // Matematicka operacia vyuzivajuca instrukcnu sadu SIMD
        neuron.output = math.mul(Weights[neuron.IndexW + 0], Input[index]);
            //Debug.Log($"Input[{n}] = {Edges[n].output}");
        //}
        neuron.output += Weights[neuron.IndexW + neuron.num_of_inputs];     // bias
        neuron.output = NeuronFn.ELU(neuron.output);

        // Copy back
        this.Neurons[index] = neuron;
    }
}