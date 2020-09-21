using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;

public class NeuralLayer
{
    public Neuron[] neurons;

    public NeuralLayer BPG_egde = null;       // hrana ucenia BPG pre spatne sirenie chyby neuronu

    public NeuralLayer edge = null;

    public NeuronLayerType type;

    public NeuralLayer(int units, int? num_of_inputs, NeuralLayer edge, NeuronLayerType type)
    {
        this.type = type;
        this.edge = edge;
        this.neurons = new Neuron[units];
        
        if (type == NeuronLayerType.INPUT)
        {
            for (int i = 0; i < units; i++)
            {
                this.neurons[i] = new Neuron(num_of_inputs.Value, units, new ReLU());
            }
        }
        else if (type == NeuronLayerType.HIDDEN)
        {
            for (int i = 0; i < units; i++)
            {
                this.neurons[i] = new Neuron(this.edge.neurons.Length, units, new ReLU());
            }
        }
        else if (type == NeuronLayerType.OUTPUT)
        {
            for (int i = 0; i < units; i++)
            {
                this.neurons[i] = new Neuron(this.edge.neurons.Length, units, new Linear());
            }
        }
    }

    public void predict(float[] input=null)
    {
        if (this.edge != null)
        {
            foreach (var n in this.neurons)
            {
                n.predict(this.edge);
            } 
        }
        else
        {
            foreach (var n in this.neurons)
            {
                n.predict(input);
            } 
        }
    }

    public void update(float[] input=null, float[] y=null)
    {
        if (type == NeuronLayerType.INPUT)
        {
            for (int m = 0; m < this.neurons.Length; m++)
            {
                this.neurons[m].update(input, this.BPG_egde, m);
            } 
        }
        else if (type == NeuronLayerType.OUTPUT)
        {
            for (int m = 0; m < this.neurons.Length; m++)
            {
                this.neurons[m].update(this.edge, y[m]);
            }
        }
        else
        {
            for (int m = 0; m < this.neurons.Length; m++)
            {
                this.neurons[m].update(this.edge, this.BPG_egde, m);
            }
        }
    }
}