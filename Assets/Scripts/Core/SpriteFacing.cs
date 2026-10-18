using System;

/// <summary>
/// Hvilken af de tre animationer (Right / Front / Behind) en retning skal vise,
/// og om spriten skal spejles (venstre = Right spejlet).
/// Ren C# (ingen UnityEngine), så den kan unit-testes uden Unity. Bruges af PlayerBullet.
/// </summary>
public readonly struct SpriteFacing
{
    /// <summary>Værdi til Animatorens DirX: 1 = vandret, 0 = lodret.</summary>
    public readonly float DirX;
    /// <summary>Værdi til Animatorens DirY: 1 = Behind (op), -1 = Front (ned), 0 = vandret.</summary>
    public readonly float DirY;
    public readonly bool FlipX;

    public SpriteFacing(float dirX, float dirY, bool flipX)
    {
        DirX = dirX;
        DirY = dirY;
        FlipX = flipX;
    }

    /// <summary>Snapper en retning til nærmeste af de tre animationer. Ved præcis diagonal vinder vandret.</summary>
    public static SpriteFacing From(float x, float y)
    {
        bool horizontal = Math.Abs(x) >= Math.Abs(y);
        return new SpriteFacing(
            horizontal ? 1f : 0f,
            horizontal ? 0f : Math.Sign(y),
            horizontal && x < 0f);
    }
}
