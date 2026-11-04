using System;

/// <summary>
/// Bonus til spillerens stats, fx fra et item eller en upgrade (+2 speed, +10 health ...).
/// Felter der står på 0 har ingen effekt.
/// </summary>
[Serializable]
public struct StatModifier
{
    public int maxHealth;
    public int maxMana;
    public float manaRegenPerSecond;
    public float moveSpeed;
    public int spellDamage;
    public int projectileCount; // ekstra fireballs pr. skud

    public static StatModifier operator +(StatModifier a, StatModifier b) => new StatModifier
    {
        maxHealth = a.maxHealth + b.maxHealth,
        maxMana = a.maxMana + b.maxMana,
        manaRegenPerSecond = a.manaRegenPerSecond + b.manaRegenPerSecond,
        moveSpeed = a.moveSpeed + b.moveSpeed,
        spellDamage = a.spellDamage + b.spellDamage,
        projectileCount = a.projectileCount + b.projectileCount,
    };
}
