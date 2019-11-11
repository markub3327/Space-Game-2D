using Unity.Entities;

public enum NeuronState : byte
{
    Calculating,
    IsReady
}

public struct Neuron : IComponentData
{
    public float Output;    // Vystup neuronu
    public NeuronState State;    // Je vystup neuronu pripraveny?
}

// Vstupne polia neuronu
public struct NeuronInput : IBufferElementData
{
    public float input;
}

public struct NeuronWeight : IBufferElementData
{
    public float weight;
}

public struct NeuronEdge : IBufferElementData
{
    public Entity entity;
}
