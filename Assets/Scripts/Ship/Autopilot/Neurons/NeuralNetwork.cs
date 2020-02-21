using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class NeuralNetwork
{
    public string Name;
    public List<NeuralLayer> neuronLayers { get; private set; }

    public NeuralNetwork(string name=default(string))
    {
        this.neuronLayers = new List<NeuralLayer>();
        this.Name = name;
    }

    public void CreateLayer(NeuronLayerType type)
    {
        this.neuronLayers.Add(new NeuralLayer(type));
    }
    
    public void SetBPGEdge(NeuralLayer layer, NeuralLayer BPG_egdes)
    {
        layer.BPG_egdes = BPG_egdes;
    }

    public void SetEdge(NeuralLayer layer, NeuralLayer edge)
    {
        layer.Edges = edge;
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

    public float Training(float[] externalInput, float[] feedBack)
    {
        float err = 0f;

        for (int i = neuronLayers.Count - 1; i >= 0; i--)
        {
            if (neuronLayers[i].type == NeuronLayerType.INPUT)
            {
                neuronLayers[i].RunTraining(externalInput);
            }
            else if (neuronLayers[i].type == NeuronLayerType.OUTPUT)
            {
                neuronLayers[i].RunTraining(feedBack, ref err);
            }
            else
            {
                neuronLayers[i].RunTraining();
            }
        }

        return err;
    }

    public override string ToString()
    {
        List<float> weights = new List<float>();

        for (int i = 0; i < this.neuronLayers.Count; i++)
        {
            for (int j = 0; j < this.neuronLayers[i].Weights.Count; j++)
            {
                weights.Add(this.neuronLayers[i].Weights[j]);
            }            
        }

        return JsonUtility.ToJson(new JSON_NET { Weights = weights }, true);
    } 
}

[System.Serializable]
public class JSON_NET
{
    public List<float> Weights;
}