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
                this.neurons[i] = new Neuron(num_of_inputs.Value, units, new Swish());
            }
        }
        else if (type == NeuronLayerType.HIDDEN)
        {
            for (int i = 0; i < units; i++)
            {
                this.neurons[i] = new Neuron(this.edge.neurons.Length, units, new Swish());
            }
            this.edge.BPG_egde = this;
        }
        else if (type == NeuronLayerType.OUTPUT)
        {
            for (int i = 0; i < units; i++)
            {
                this.neurons[i] = new Neuron(this.edge.neurons.Length, units, new Linear());
            }
            this.edge.BPG_egde = this;
        }
    }
}