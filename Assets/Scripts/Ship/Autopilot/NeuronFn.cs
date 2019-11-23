using Unity.Mathematics;

public static class NeuronFn
{
    public static float ELU(float x/*, float a*/)
    {
        if (x < 0f)
        {
            return (math.exp(x) - 1f);  /*a * */
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

    public static float derivELU(float y/*, float a*/)
    {
        if (y <= 0f) 
        {
            return y + 1f; //a;
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