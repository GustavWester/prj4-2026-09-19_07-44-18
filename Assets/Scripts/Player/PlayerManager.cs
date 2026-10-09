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

    /// <summary>Fires når stats er genberegnet (item equip/unequip, upgrade).</summary>
    public event Action StatsChanged;

    private PlayerStatsSO stats; // runtime-kopi = baseStats + modifiers
    private readonly List<StatModifier> modifiers = new();

    public ResourceController Resources { get; private set; }

    public int MaxHealth => stats.maxHealth;
    public int MaxMana => stats.maxMana;
    public float ManaRegenPerSecond => stats.manaRegenPerSecond;
    public float MoveSpeed => stats.moveSpeed;
    public int SpellDamage => stats.spellDamage;

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

    public void Equip(ItemSO item)
    {
        if (item != null) AddModifier(item.statModifier);
    }

    public void Unequip(ItemSO item)
    {
        if (item != null) RemoveModifier(item.statModifier);
    }

    // Bygger stats forfra fra baseStats, så equipment kan tages af igen uden at tallene driver.
    private void Recalculate()
    {
        if (stats != null) Destroy(stats);
        stats = Instantiate(baseStats);
        foreach (var mod in modifiers)
            stats.Add(mod);

        Resources.SetMaxHealth(stats.maxHealth);
        Resources.SetMaxMana(stats.maxMana);
        Resources.SetManaRegen(stats.manaRegenPerSecond);

        StatsChanged?.Invoke();
    }
}
