using Unity.Mathematics;

public static class NeuronFn
{
    public static float ELU(float x)
    {
        if (x <= 0f)
        {
            return 0f;
        }
        else if (x >= 1f)
        {
            return 1f;
        }
        else
        {
            return x;
        }
    }

    public static float derivELU(float y)
    {
        if (y <= 0f) 
        {
            return 0f;
        }
        else if (y >= 1f)
        {
            return 0f;
        }
        else
        {
            return 1f;
        }
    }
}