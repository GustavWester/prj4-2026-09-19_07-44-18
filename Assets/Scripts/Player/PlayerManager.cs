using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ejer spillerens tilstand. Holder en runtime-kopi af PlayerStatsSO og er det
/// eneste sted stats ændres (items, upgrades). Andre scripts (movement, bullets,
/// UI) læser stats herfra i stedet for at have deres egne tal.
///
/// Kører før andre scripts (DefaultExecutionOrder), så stats er klar i deres Awake.
/// Online: modifiers skal anvendes på alle maskiner (fx via RPC), da hosten afgør skade.
/// </summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(ResourceController))]
public class PlayerManager : MonoBehaviour
{
    [SerializeField] private PlayerStatsSO baseStats;
    [Tooltip("Max mana kan aldrig komme under dette, så man altid kan skyde (selv med mange minus-mana items).")]
    [SerializeField, Min(0)] private int minMaxMana = 30;

    /// <summary>Fires når stats er genberegnet (item equip/unequip, upgrade).</summary>
    public event Action StatsChanged;

    private PlayerStatsSO stats; // runtime-kopi = baseStats + modifiers
    private readonly List<StatModifier> modifiers = new();
    private readonly HashSet<ItemSO> equippedUnique = new(); // unique items der allerede giver stats

    public ResourceController Resources { get; private set; }

    public int MaxHealth => stats.maxHealth;
    public int MaxMana => stats.maxMana;
    public float ManaRegenPerSecond => stats.manaRegenPerSecond;
    public float MoveSpeed => stats.moveSpeed;
    public int SpellDamage => stats.spellDamage;
    public int ProjectileCount => Mathf.Max(1, stats.projectileCount);

    private void Awake()
    {
        Resources = GetComponent<ResourceController>();

        if (baseStats == null)
        {
            Debug.LogWarning($"{name}: no PlayerStats assigned — using defaults.", this);
            baseStats = ScriptableObject.CreateInstance<PlayerStatsSO>();
        }

        Recalculate();
    }

    private void OnDestroy()
    {
        if (stats != null) Destroy(stats);
    }

    public void AddModifier(StatModifier mod)
    {
        modifiers.Add(mod);
        Recalculate();
    }

    public void RemoveModifier(StatModifier mod)
    {
        if (modifiers.Remove(mod)) Recalculate();
    }

    // powerups som da levelen startede (DeathScreen / Try again). StatModifier er en struct, så listen er en rigtig kopi
    private List<StatModifier> savedModifiers = new();
    private HashSet<ItemSO> savedUnique = new();

    public void SaveCheckpoint()
    {
        savedModifiers = new List<StatModifier>(modifiers);
        savedUnique = new HashSet<ItemSO>(equippedUnique);
    }

    public void LoadCheckpoint()
    {
        modifiers.Clear();
        modifiers.AddRange(savedModifiers);
        equippedUnique.Clear();
        equippedUnique.UnionWith(savedUnique);
        Recalculate();
    }

    public void Equip(ItemSO item)
    {
        if (item == null) return;
        if (item.unique && !equippedUnique.Add(item)) return; // har den allerede -> ingen ekstra stats
        AddModifier(item.statModifier);
    }

    public void Unequip(ItemSO item)
    {
        if (item == null) return;
        if (item.unique && !equippedUnique.Remove(item)) return;
        RemoveModifier(item.statModifier);
    }

    // Bygger stats forfra fra baseStats, så equipment kan tages af igen uden at tallene driver.
    private void Recalculate()
    {
        if (stats != null) Destroy(stats);
        stats = Instantiate(baseStats);
        foreach (var mod in modifiers)
            stats.Add(mod);
        stats.maxMana = Mathf.Max(minMaxMana, stats.maxMana);

        Resources.SetMaxHealth(stats.maxHealth);
        Resources.SetMaxMana(stats.maxMana);
        Resources.SetManaRegen(stats.manaRegenPerSecond);

        StatsChanged?.Invoke();
    }
}
