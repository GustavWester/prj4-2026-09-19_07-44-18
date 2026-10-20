using System.Collections.Generic;

/// <summary>
/// Vinklerne (i grader, 0 = højre, mod uret) som hvert bullet pattern skyder i.
/// Ren C# (ingen UnityEngine), så den kan unit-testes uden Unity. Bruges af EnemyController.
/// </summary>
public static class BulletPatternMath
{
    /// <summary>
    /// Kugler fordelt jævnt over spreadAngle, startende ved rotationOffset.
    /// Bruges af RadialBurst (offset 0) og Spiral (offset = volley * spiralRotationSpeedDeg).
    /// </summary>
    public static List<float> RadialAngles(int bulletCount, float spreadAngle, float rotationOffset)
    {
        var angles = new List<float>();
        if (bulletCount <= 0) return angles;

        float step = spreadAngle / bulletCount;
        for (int i = 0; i < bulletCount; i++)
            angles.Add(rotationOffset + i * step);
        return angles;
    }

    /// <summary>Vinklen for Spiral i en given volley.</summary>
    public static float SpiralOffset(int volley, float rotationSpeedDeg) => volley * rotationSpeedDeg;

    /// <summary>
    /// Vifte centreret om baseAngle (retningen mod spilleren). Første og sidste kugle
    /// ligger præcis i hver sin kant af spreadAngle. Én kugle skydes lige mod spilleren.
    /// </summary>
    public static List<float> AimedSpreadAngles(int bulletCount, float spreadAngle, float baseAngle)
    {
        var angles = new List<float>();
        if (bulletCount <= 0) return angles;

        float step = bulletCount > 1 ? spreadAngle / (bulletCount - 1) : 0f;
        float start = bulletCount > 1 ? baseAngle - spreadAngle / 2f : baseAngle;
        for (int i = 0; i < bulletCount; i++)
            angles.Add(start + i * step);
        return angles;
    }

    /// <summary>Som RadialAngles, men kuglen på gapIndex mangler, så spilleren har et hul at undvige igennem.</summary>
    public static List<float> CurtainAngles(int bulletCount, float spreadAngle, int gapIndex)
    {
        var angles = new List<float>();
        if (bulletCount <= 0) return angles;

        float step = spreadAngle / bulletCount;
        for (int i = 0; i < bulletCount; i++)
        {
            if (i == gapIndex) continue;
            angles.Add(i * step);
        }
        return angles;
    }

    /// <summary>Næste index når patterns køres i rækkefølge (ikke tilfældigt). Starter forfra efter det sidste.</summary>
    public static int NextIndex(int current, int count) => count <= 0 ? 0 : (current + 1) % count;
}
