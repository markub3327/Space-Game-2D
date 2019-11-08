using UnityEngine;

// Rozsirujuca trieda pre triedu UnityEngine.Vector2
public static class ExtendedVector2
{
    public static readonly Vector2 down_right = new Vector2(1f, -1f);

    public static readonly Vector2 down_left = new Vector2(-1f, -1f);

    public static readonly Vector2 up_left = new Vector2(-1f, 1f);

    public static readonly Vector2 up_right = new Vector2(1f, 1f);

    /// <summary>
    /// Zrotuj vektor podla vektora shift
    /// </summary>
    /// <param name="orig">Povodny vektor</param>
    /// <param name="shift">Vektor posunu</param>
    /// <returns></returns>
    public static Vector2 Shift(this Vector2 orig, Vector2 shift)
    {
        Vector2 vector = new Vector2(orig.x * shift.x - orig.y * shift.y, orig.y * shift.x + orig.x * shift.y);

        return vector;
    }

    /// <summary>
    /// Zrotuj vektor podla uhlu
    /// </summary>
    /// <param name="orig">Povodny vektor</param>
    /// <param name="angle">Uhol posunu</param>
    /// <returns></returns>
    public static Vector2 Shift(this Vector2 orig, float angle)
    {
        var angleRad = angle * Mathf.Deg2Rad;

        Vector2 shift = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
        Vector2 vector = new Vector2(orig.x * shift.x - orig.y * shift.y, orig.y * shift.x + orig.x * shift.y);

        return vector;
    }
} 