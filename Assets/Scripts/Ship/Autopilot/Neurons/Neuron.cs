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
        this.sigma = 0.0f;
        this.num_of_inputs = num_of_inputs;
        this.momentum = 0.90f;
        this.learning_rate = 0.0001f;
        this.activation = activation;

        this.weights = new float[num_of_inputs + 1];        // +bias
        this.deltaWeights = new float[num_of_inputs + 1];   // +bias

        float limit;

        // inicializacia vah podla he uniform
        if (activation.GetType() == typeof(ReLU))
        {
            limit = Unity.Mathematics.math.sqrt(6.0f / (num_of_inputs));
        }
        // inicializacia vah podla glorot uniform
        else
        {
            limit = Unity.Mathematics.math.sqrt(6.0f / (num_of_inputs + num_of_neurons));
        }

        for (int n = 0; n < num_of_inputs; n++)
        {
            this.weights[n] = UnityEngine.Random.Range(-limit, limit);
        }
        this.weights[this.num_of_inputs] = 0.0f;    // zero initializer
    }

    public void predict(NeuralLayer edge)
    {
        // Vazena suma
        this.output = 0.0f;
        for (int n = 0; n < this.num_of_inputs; n++)
        {                
            this.output += math.mul(this.weights[n], edge.neurons[n].output);
            //Debug.Log($"Input[{n}] = {Edges[n].output}");
        }        
        this.output += this.weights[this.num_of_inputs];     // bias
        this.output = this.activation.run(this.output);
    }

    public void predict(float[] input)
    {
        // Vazena suma
        this.output = 0.0f;
        for (int n = 0; n < this.num_of_inputs; n++)
        {                
            this.output += math.mul(this.weights[n], input[n]);
            //Debug.Log($"Input[{n}] = {Edges[n].output}");
        }        
        this.output += this.weights[this.num_of_inputs];     // bias
        this.output = this.activation.run(this.output);
    }

    public virtual void update(NeuralLayer edge, float y)
    {
        this.sigma = (y - this.output) * this.activation.deriv(this.output);
        
        for (int n = 0; n < this.num_of_inputs; n++)
        {                
            this.deltaWeights[n] = this.learning_rate * this.sigma * edge.neurons[n].output + (this.momentum * this.deltaWeights[n]);
            this.weights[n] += deltaWeights[n];
        }
        // bias
        this.deltaWeights[this.num_of_inputs] = this.learning_rate * this.sigma + (this.momentum * this.deltaWeights[this.num_of_inputs]);
        this.weights[this.num_of_inputs] += deltaWeights[this.num_of_inputs];
    }

    public virtual void update(NeuralLayer edge, NeuralLayer BPG_egde, int index)
    {
        // Spätne sirenie chyby po sieti
        float sum = 0.0f;
        for (int k = 0; k < BPG_egde.neurons.Length; k++)
        {
            sum += BPG_egde.neurons[k].sigma * BPG_egde.neurons[k].weights[index];
            //Debug.Log($"BPG_sigma[{i}] = {BPG_egdes[i].sigma}, BPG_weights[{BPG_egdes[i].IndexW + index}] = {BPG_weights[BPG_egdes[i].IndexW + index]}");
        }                
        this.sigma = sum * this.activation.deriv(this.output);
                               
        // Adaptuj vahy podla chyby neuronu
        for (int n = 0; n < this.num_of_inputs; n++)
        {                
            this.deltaWeights[n] = this.learning_rate * this.sigma * edge.neurons[n].output + (this.momentum * this.deltaWeights[n]);
            this.weights[n] += deltaWeights[n];
        }
        // bias
        this.deltaWeights[this.num_of_inputs] = this.learning_rate * this.sigma + (this.momentum * this.deltaWeights[this.num_of_inputs]);
        this.weights[this.num_of_inputs] += deltaWeights[this.num_of_inputs];
    }

    public void soft_update(Neuron target_neuron, float tau)
    {
        for (int k = 0; k < this.num_of_inputs; k++)
            target_neuron.weights[k] = tau*this.weights[k] + (1.0f-tau)*target_neuron.weights[k];
    }

    public virtual void update(float[] input, NeuralLayer BPG_egde, int index)
    {
        // Spätne sirenie chyby po sieti
        float sum = 0.0f;
        for (int k = 0; k < BPG_egde.neurons.Length; k++)
        {
            sum += BPG_egde.neurons[k].sigma * BPG_egde.neurons[k].weights[index];
            //Debug.Log($"BPG_sigma[{i}] = {BPG_egdes[i].sigma}, BPG_weights[{BPG_egdes[i].IndexW + index}] = {BPG_weights[BPG_egdes[i].IndexW + index]}");
        }                
        this.sigma = sum * this.activation.deriv(this.output);
                               
        // Adaptuj vahy podla chyby neuronu
        for (int n = 0; n < this.num_of_inputs; n++)
        {                
            this.deltaWeights[n] = this.learning_rate * this.sigma * input[n] + (this.momentum * this.deltaWeights[n]);
            this.weights[n] += deltaWeights[n];
        }
        // bias
        this.deltaWeights[this.num_of_inputs] = this.learning_rate * this.sigma + (this.momentum * this.deltaWeights[this.num_of_inputs]);
        this.weights[this.num_of_inputs] += deltaWeights[this.num_of_inputs];
    }
}