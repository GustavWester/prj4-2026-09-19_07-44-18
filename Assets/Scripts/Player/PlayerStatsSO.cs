using UnityEngine;

/// <summary>
/// Spillerens character stats. Opret via Create > Scriptable Objects > PlayerStats.
/// Assetten er kun udgangspunktet: PlayerManager laver en runtime-kopi og lægger
/// item-modifiers oveni, så assetten selv aldrig ændres under spillet.
/// </summary>
[CreateAssetMenu(fileName = "PlayerStats", menuName = "Scriptable Objects/PlayerStats")]
public class PlayerStatsSO : ScriptableObject
{
    [Header("Resources")]
    [Min(1)] public int maxHealth = 5;
    [Min(0)] public int maxMana = 100;
    [Tooltip("Mana pr. sekund der regenereres. 0 = ingen regen.")]
    [Min(0)] public float manaRegenPerSecond = 10f;

    [Header("Movement")]
    [Min(0)] public float moveSpeed = 5f;

    [Header("Combat")]
    [Tooltip("Lægges oveni hver spells egen base damage.")]
    public int spellDamage = 0;

    /// <summary>Lægger en modifier oveni disse stats.</summary>
    public void Add(StatModifier mod)
    {
        maxHealth += mod.maxHealth;
        maxMana += mod.maxMana;
        manaRegenPerSecond += mod.manaRegenPerSecond;
        moveSpeed += mod.moveSpeed;
        spellDamage += mod.spellDamage;
    }
}

// StatModifier ligger i Core/StatModifier.cs, så den kan unit-testes uden Unity.
