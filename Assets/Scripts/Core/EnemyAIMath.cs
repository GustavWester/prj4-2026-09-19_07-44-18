using System;

/// <summary>
/// Beslutningerne bag fjendernes bevægelse og angrebs-timing.
/// Ren C# (ingen UnityEngine), så den kan unit-testes uden Unity. Bruges af EnemyController.
/// </summary>
public static class EnemyAIMath
{
    /// <summary>Melee- og KeepDistance-fjender stiller sig lidt inden for attackRange.</summary>
    public const float PreferredRangeFactor = 0.8f;

    /// <summary>Korteste ventetid mellem to angreb, uanset hvor negativ jitter er.</summary>
    public const float MinAttackInterval = 0.05f;

    public static float PreferredDistance(float attackRange) => attackRange * PreferredRangeFactor;

    /// <summary>
    /// KeepDistance: 1 = gå mod spilleren, -1 = gå væk, 0 = bliv stående (inden for tolerance).
    /// </summary>
    public static int KeepDistanceStep(float distance, float attackRange, float tolerance)
    {
        float preferred = PreferredDistance(attackRange);
        if (Math.Abs(distance - preferred) <= tolerance) return 0;
        return distance > preferred ? 1 : -1;
    }

    /// <summary>ChasePlayer: om fjenden skal bevæge sig mod spilleren.</summary>
    public static bool ShouldChase(float distance, float detectionRange, bool isMelee, float attackRange)
    {
        if (distance > detectionRange) return false;
        // melee stopper lidt inden for attackRange i stedet for at skubbe ind i spilleren
        if (isMelee && distance <= PreferredDistance(attackRange)) return false;
        return true;
    }

    /// <summary>Ventetid før næste angreb: cooldown + jitter, men aldrig under MinAttackInterval.</summary>
    public static float AttackInterval(float cooldown, float jitter) =>
        Math.Max(MinAttackInterval, cooldown + jitter);
}
