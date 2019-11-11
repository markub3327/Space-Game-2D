using Unity.Mathematics;

public static class NeuronMathf
{
    public static float ELU(float x, float a)
    {
        if (x < 0f)
            return a * (math.exp(x) - 1f);
        return x;
    }
}
