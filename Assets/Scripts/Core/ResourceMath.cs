using System;

/// <summary>
/// Regnestykkerne bag liv og mana (skade, heal, nyt max, regen).
/// Ren C# (ingen UnityEngine), så den kan unit-testes uden Unity. Bruges af ResourceController.
/// </summary>
public static class ResourceMath
{
    /// <summary>Nyt liv efter skade. Går aldrig under 0, og 0 eller negativ skade gør intet.</summary>
    public static int ApplyDamage(int current, int amount) =>
        amount <= 0 ? current : Math.Max(0, current - amount);

    /// <summary>Nyt liv efter heal. Går aldrig over max, og 0 eller negativ heal gør intet.</summary>
    public static int ApplyHeal(int current, int amount, int max) =>
        amount <= 0 ? current : Math.Min(max, current + amount);

    /// <summary>
    /// Nuværende værdi når max ændres: den flyttes lige så meget som max (så +10 max også giver +10),
    /// og holdes inden for [minCurrent, newMax].
    /// </summary>
    public static int ResizeCurrent(int current, int oldMax, int newMax, int minCurrent)
    {
        int value = current + (newMax - oldMax);
        return Math.Max(minCurrent, Math.Min(newMax, value));
    }

    /// <summary>Andel 0-1. Giver 0 hvis max er 0 eller mindre.</summary>
    public static float Percent(int current, int max) => max > 0 ? (float)current / max : 0f;
}

/// <summary>
/// Samler brøkdele af regen op over flere frames, så fx 10 mana/sek. virker selvom
/// 10 * deltaTime er under 1 i hver frame.
/// </summary>
public class RegenAccumulator
{
    private float buffer;

    public float Buffer => buffer;

    /// <summary>Tilføjer perSecond * deltaTime og returnerer de hele point, der er klar til at blive givet.</summary>
    public int Tick(float perSecond, float deltaTime)
    {
        if (perSecond <= 0f || deltaTime <= 0f) return 0;

        buffer += perSecond * deltaTime;
        if (buffer < 1f) return 0;

        int whole = (int)Math.Floor(buffer);
        buffer -= whole;
        return whole;
    }

    public void Reset() => buffer = 0f;
}
