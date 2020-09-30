using Unity.Mathematics;
using UnityEngine;

// Trieda neuronu
public class Neuron
{
    // Parametre neuronu - vahy
    public float[] weights;
    public float[] g_t;         // gradient
    public float[] m_t;         // first moment
    public float[] v_t;         // second moment

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
    public float learning_rate;
    public float beta_1;
    public float beta_2;
    public float epsilon;
    public ulong _time;

    public Neuron(int num_of_inputs, int num_of_neurons, Activation activation)
    {
        this.output = 0.0f;
        this.z = 0f;
        this.sigma = 0.0f;
        this.num_of_inputs = num_of_inputs;
        this.activation = activation;

        // hyper-pararmeters
        this.learning_rate = 0.001f;
        this.beta_1 = 0.9f;
        this.beta_2 = 0.999f;
        this.epsilon = 0.0000001f; // 1e-07;
        this._time = 0;

        this.weights = new float[num_of_inputs + 1];        // +bias
        this.g_t = new float[num_of_inputs + 1];            // +bias
        this.m_t = new float[num_of_inputs + 1];            // +bias
        this.v_t = new float[num_of_inputs + 1];            // +bias

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
            this.g_t[n] = 0f;
            this.m_t[n] = 0f;
            this.v_t[n] = 0f;
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
        
        // get gradients
        for (int n = 0; n < this.num_of_inputs; n++)
        {
            g_t[n] = this.sigma * edge.neurons[n].output;
        }
        g_t[this.num_of_inputs] = this.sigma;

        this.Adam();
    }

    public void train(float[] input, NeuralLayer BPG_edge, int idx)
    {
        float sum = 0.0f;
        for (int k = 0; k < BPG_edge.neurons.Length; k++)
        {
            sum += BPG_edge.neurons[k].sigma * BPG_edge.neurons[k].weights[idx];
        }
        this.sigma = this.activation.deriv(this) * sum;
        
        // get gradients
        for (int n = 0; n < this.num_of_inputs; n++)
        {
            g_t[n] = this.sigma * input[n];
        }
        g_t[this.num_of_inputs] = this.sigma;

        this.Adam();
    }

    public void train(NeuralLayer edge, float y)
    {
        this.sigma = y - this.output;
        this.sigma *= this.activation.deriv(this);

        // get gradients
        for (int n = 0; n < this.num_of_inputs; n++)
        {
            g_t[n] = this.sigma * edge.neurons[n].output;
        }
        g_t[this.num_of_inputs] = this.sigma;

        this.Adam();
    }

    private void Adam()
    {
        this._time = this._time + 1;

        for (int n = 0; n < this.weights.Length; n++)
        {
            m_t[n] = beta_1*m_t[n] + (1f-beta_1)*g_t[n];	            // updates the moving averages of the gradient
	        v_t[n] = beta_2*v_t[n] + (1f-beta_2)*(g_t[n]*g_t[n]);	    // updates the moving averages of the squared gradient
	        
            float m_cap = m_t[n]/(1f-math.pow(beta_1, this._time));		        // calculates the bias-corrected estimates
	        float v_cap = v_t[n]/(1f-math.pow(beta_2, this._time));		        // calculates the bias-corrected estimates
        
            // update weights
	        this.weights[n] += (this.learning_rate*m_cap)/(math.sqrt(v_cap)+this.epsilon);
	    }
    }

    public void soft_update(Neuron neuron, float tau)
    {
        for (int k = 0; k < this.weights.Length; k++)
            this.weights[k] = tau*neuron.weights[k] + (1.0f-tau)*this.weights[k];
    }
}