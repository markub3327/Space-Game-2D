using System.Collections.Generic;
using UnityEngine;

public class NeuralNetwork
{
    public List<NeuralLayer> neuronLayers { get; private set; }

    public NeuralNetwork()
    {
        this.neuronLayers = new List<NeuralLayer>();
    }

    public void addLayer(int units, NeuronLayerType type, int? num_of_inputs=null, NeuralLayer edge=null)
    {
        this.neuronLayers.Add(new NeuralLayer(units, num_of_inputs, edge, type));
    }
    
    public void setBPGEdge(NeuralLayer layer, NeuralLayer edge)
    {
        layer.BPG_egde = edge;
    }

    public float[] predict(float[] input)
    {
        neuronLayers[0].predict(input);

        for (int i = 1; i < neuronLayers.Count; i++)
        {
            neuronLayers[i].predict();
        }

        float[] output = new float[neuronLayers[neuronLayers.Count - 1].neurons.Length];
        for (int n = 0; n < output.Length; n++)
        {
            output[n] = neuronLayers[neuronLayers.Count - 1].neurons[n].output;
        }

        return output;
    }

    public void update(float[] input, float[] y)
    {
        neuronLayers[neuronLayers.Count - 1].update(y: y);

        for (int i = neuronLayers.Count - 2; i >= 1; i--)
        {
            neuronLayers[i].update();
        }

        neuronLayers[0].update(input: input);
    }

    public override string ToString()
    {
        List<float> weights = new List<float>();

        for (int i = 0; i < this.neuronLayers.Count; i++)
        {
            for (int j = 0; j < this.neuronLayers[i].neurons.Length; j++)
            {
                weights.AddRange(this.neuronLayers[i].neurons[j].weights);
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