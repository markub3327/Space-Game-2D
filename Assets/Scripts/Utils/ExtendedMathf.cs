public static class ExtendedMathf
{
    public static float Line(float x)
    {
        if (x > 1f)
            return 1f;
        if (x < -1f)
            return -1f;

        return x;
    }
}
