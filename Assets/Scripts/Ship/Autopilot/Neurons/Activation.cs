using Unity.Mathematics;

public abstract class Activation
{
    public abstract float run(float x);
    public abstract float deriv(float x);
}

public class ReLU : Activation
{
    public override float run(float x)
    {
        return math.max(x, 0.0f);
    }

    public override float deriv(float x)
    {
        if (x < 0.0f)     return 0.0f;
        else              return 1.0f;
    }
}

public class Linear : Activation
{
    public override float run(float x)
    {
        return x;
    }

    public override float deriv(float x)
    {
        return 1.0f;
    }
}