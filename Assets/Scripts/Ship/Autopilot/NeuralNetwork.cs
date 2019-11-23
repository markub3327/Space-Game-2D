using System.Collections.Generic;


public class NeuralNetwork
{
    public List<NeuronLayer> neuronLayers;

    public NeuralNetwork()
    {
        this.neuronLayers = new List<NeuronLayer>();
    }

    public void CreateLayer(NeuronLayerType type, NeuronLayer BPG_egdes = null, NeuronLayer Edge = null)
    {
        this.neuronLayers.Add(new NeuronLayer(type, BPG_egdes, Edge));
    }
    
    public void SetEdge(NeuronLayer layer, NeuronLayer BPG_egdes, NeuronLayer Edge)
    {
        layer.BPG_egdes = BPG_egdes;
        layer.Edge = Edge;
    }

    public void Run(float[] externalInput)
    {
        for (int i = 0; i < neuronLayers.Count; i++)
        {
            if (neuronLayers[i].type == NeuronLayerType.INPUT)
            {
                neuronLayers[i].Run(externalInput);
            }
            else
            {
                neuronLayers[i].Run();
            }
        }
    }

    public void Training(float[] externalInput, float[] o)
    {
        float e = 0f;

        for (int i = neuronLayers.Count - 1; i >= 0; i--)
        {
            if (neuronLayers[i].type == NeuronLayerType.INPUT)
            {
                neuronLayers[i].RunTraining(externalInput);
            }
            else if (neuronLayers[i].type == NeuronLayerType.OUTPUT)
            {
                neuronLayers[i].RunTraining(o, ref e);
            }
            else
            {
                neuronLayers[i].RunTraining();
            }
        }
    }
}