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
    
    public float[] predict(float[] input)
    {
        // feed-forward
        for (int m = 0; m < this.neuronLayers[0].neurons.Length; m++)
        {
            this.neuronLayers[0].neurons[m].predict(input);
        }

        for (int l = 1; l < this.neuronLayers.Count-1; l++)
        {
            for (int m = 0; m < this.neuronLayers[l].neurons.Length; m++)
            {
                this.neuronLayers[l].neurons[m].predict(this.neuronLayers[l].edge);
            }
        }

        var y = new float[this.neuronLayers[this.neuronLayers.Count-1].neurons.Length];
        for (int m = 0; m < this.neuronLayers[this.neuronLayers.Count-1].neurons.Length; m++)
        {
            y[m] = this.neuronLayers[this.neuronLayers.Count-1].neurons[m].predict(this.neuronLayers[this.neuronLayers.Count-1].edge);   
        }
        return y;
    }

    public void train(float[] x, float[] y)
    {
        for (int m = 0; m < this.neuronLayers[this.neuronLayers.Count-1].neurons.Length; m++)
        {
            this.neuronLayers[this.neuronLayers.Count-1].neurons[m].train(this.neuronLayers[this.neuronLayers.Count-1].edge, y[m]);
        }

        for (int l = this.neuronLayers.Count-2; l >= 1; l--)
        {
            for (int m = 0; m < this.neuronLayers[l].neurons.Length; m++)
            {
               this.neuronLayers[l].neurons[m].train(this.neuronLayers[l].edge, this.neuronLayers[l].BPG_egde, m);
            }
        }

        for (int m = 0; m < this.neuronLayers[0].neurons.Length; m++)
        {
            this.neuronLayers[0].neurons[m].train(x, this.neuronLayers[0].BPG_egde, m);
        }
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