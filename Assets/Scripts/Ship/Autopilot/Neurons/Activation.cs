using Unity.Mathematics;

public abstract class Activation
{
    public abstract float run(float x);
    public abstract float deriv(Neuron n);
}

public class Swish : Activation
{
    public override float run(float x)
    {
        return x / (1f + math.exp(-x));
    }

    public override float deriv(Neuron n)
    {
        return n.output + ((1f - n.output) / (1f + math.exp(-n.z)));
    }
}

public class ReLU : Activation
{
    public override float run(float x)
    {
        return math.max(x, 0.0f);
    }

    public override float deriv(Neuron n)
    {
        if (n.z < 0.0f)     return 0.0f;
        else                return 1.0f;
    }
}

public class TanH : Activation
{
    public override float run(float x)
    {
        return math.tanh(x);
    }

    public override float deriv(Neuron n)
    {
        return (1f - math.mul(n.output, n.output));
    }
}

public class Linear : Activation
{
    public override float run(float x)
    {
        return x;
    }

    public override float deriv(Neuron n)
    {
        return 1.0f;
    }
}