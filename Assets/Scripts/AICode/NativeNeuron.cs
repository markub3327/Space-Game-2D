using Unity.Collections;

public struct NativeNeuron
{
    // Pole vstupov z okoliteho sveta
    public NativeArray<float> Inputs;

    // Vahy neuronu (regulujuce vplyv vstupu na celkovy vystup neuronu)
    public NativeArray<float> Weights;

    // Vystup neuronu
    public float Output;


    public Neuron(int num_of_inputs)
    {
        Inputs = new NativeArray<float>(num_of_inputs, Allocator.TempJob);
        Weights = new NativeArray<float>(num_of_inputs, Allocator.TempJob);
        Output = 0f;
    }

    public void Run()
    {
        for (int i = 0; i < Weights.Length; i++)
        {
            Output += Inputs[i] * Weights[i];
        }

        Output = AICode.MathF.ELU(0.5f, Output);
    }

    public void Dispose()
    {
        Inputs.Dispose();
        Weights.Dispose();
    }
}

