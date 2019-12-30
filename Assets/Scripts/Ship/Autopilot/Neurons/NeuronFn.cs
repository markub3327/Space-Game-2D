using Unity.Mathematics;

public static class NeuronFn
{
    public static float ELU(float x)
    {
        if (x < 0f)
        {
            return (math.exp(x) - 1f);
        }        
        else
        {
            return x;
        }
    }

    public static float derivELU(float y)
    {
        if (y < 0f)
        {
            return y + 1f;
        }        
        else
        {
            return 1f;
        }
    }
}