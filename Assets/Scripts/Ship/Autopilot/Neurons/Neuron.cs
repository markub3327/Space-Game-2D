using Unity.Mathematics;
using UnityEngine;

// Trieda neuronu
public class Neuron
{
    // Parametre neuronu - vahy
    public float[] weights;
    public float[] deltaWeights;

    // Aktivacna funkcia
    public Activation activation;

    // Vystup neuronu
    public float output;
    public float z;

    // Chyba neuronu
    public float sigma;

    // Pocet vstupov
    public int num_of_inputs;

    // Hyper-parametre
    public float momentum;
    public float learning_rate;

    public Neuron(int num_of_inputs, int num_of_neurons, Activation activation)
    {
        this.output = 0.0f;
        this.z = 0f;
        this.sigma = 0.0f;
        this.num_of_inputs = num_of_inputs;
        this.learning_rate = 0.001f;
        this.momentum = 0.9f;
        this.activation = activation;

        this.weights = new float[num_of_inputs + 1];        // +bias
        this.deltaWeights = new float[num_of_inputs + 1];   // +bias

        float limit;

        // inicializacia vah podla he uniform
        if (activation.GetType() == typeof(ReLU) || activation.GetType() == typeof(Swish))
        {
            limit = Unity.Mathematics.math.sqrt(6.0f / (num_of_inputs));
        }
        // inicializacia vah podla glorot uniform
        else
        {
            limit = Unity.Mathematics.math.sqrt(6.0f / (num_of_inputs + num_of_neurons));
        }

        for (int n = 0; n < this.num_of_inputs; n++)
        {
            this.weights[n] = UnityEngine.Random.Range(-limit, limit);
        }
        this.weights[this.num_of_inputs] = 0.0f;    // zero initializer
    }

    public float predict(NeuralLayer edge)
    {
        // Vazena suma
        this.z = this.weights[this.num_of_inputs];
        for (int n = 0; n < this.num_of_inputs; n++)
        {                
            this.z += math.mul(this.weights[n], edge.neurons[n].output);
            //Debug.Log($"Input[{n}] = {Edges[n].output}");
        }        
        this.output = this.activation.run(this.z);

        return this.output;
    }

    public float predict(float[] input)
    {
        // Vazena suma
        this.z = this.weights[this.num_of_inputs];
        for (int n = 0; n < this.num_of_inputs; n++)
        {                
            this.z += math.mul(this.weights[n], input[n]);
            //Debug.Log($"Input[{n}] = {Edges[n].output}");
        }        
        this.output = this.activation.run(this.z);

        return this.output;
    }

    public void train(NeuralLayer edge, NeuralLayer BPG_edge, int idx)
    {
        float sum = 0.0f;
        for (int k = 0; k < BPG_edge.neurons.Length; k++)
        {
            sum += BPG_edge.neurons[k].sigma * BPG_edge.neurons[k].weights[idx];
        }
        this.sigma = this.activation.deriv(this) * sum;
        
        for (int n = 0; n < this.num_of_inputs; n++)
        {
            this.deltaWeights[n] = this.learning_rate * this.sigma * edge.neurons[n].output + this.momentum * this.deltaWeights[n];
            this.weights[n] += this.deltaWeights[n];
        }
        this.deltaWeights[this.num_of_inputs] = this.learning_rate * this.sigma + this.momentum * this.deltaWeights[this.num_of_inputs];
        this.weights[this.num_of_inputs] += this.deltaWeights[this.num_of_inputs];
    }

    public void train(float[] input, NeuralLayer BPG_edge, int idx)
    {
        float sum = 0.0f;
        for (int k = 0; k < BPG_edge.neurons.Length; k++)
        {
            sum += BPG_edge.neurons[k].sigma * BPG_edge.neurons[k].weights[idx];
        }
        this.sigma = this.activation.deriv(this) * sum;
        
        for (int n = 0; n < this.num_of_inputs; n++)
        {
            this.deltaWeights[n] = this.learning_rate * this.sigma * input[n] + this.momentum * this.deltaWeights[n];
            this.weights[n] += this.deltaWeights[n];
        }
        this.deltaWeights[this.num_of_inputs] = this.learning_rate * this.sigma + this.momentum * this.deltaWeights[this.num_of_inputs];
        this.weights[this.num_of_inputs] += this.deltaWeights[this.num_of_inputs];
    }

    public void train(NeuralLayer edge, float y)
    {
        this.sigma = y - this.output;
        this.sigma *= this.activation.deriv(this);

        for (int n = 0; n < this.num_of_inputs; n++)
        {
            this.deltaWeights[n] = this.learning_rate * this.sigma * edge.neurons[n].output + this.momentum * this.deltaWeights[n];
            this.weights[n] += this.deltaWeights[n];
        }
        this.deltaWeights[this.num_of_inputs] = this.learning_rate * this.sigma + this.momentum * this.deltaWeights[this.num_of_inputs];
        this.weights[this.num_of_inputs] += this.deltaWeights[this.num_of_inputs];
    }

    public void soft_update(Neuron neuron, float tau)
    {
        for (int k = 0; k < this.weights.Length; k++)
            this.weights[k] = tau*neuron.weights[k] + (1.0f-tau)*this.weights[k];
    }
}