using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

/// <summary>
/// Universal "can take damage and die" component. Used by bosses,
/// regular enemies (EnemyBehaviour), and the player alike — anything
/// that deals damage (bullets, melee hits) just needs to call
/// GetComponent&lt;Health&gt;()?.TakeDamage(amount) on the thing it hit.
///
/// Two ways to set maxHealth:
/// - Assign an EnemyStats asset -> maxHealth is pulled from stats.Health at Awake
///   (typical for regular enemies).
/// - Leave stats empty and set maxHealth directly in the inspector
///   (typical for a boss or the player, which don't use EnemyStats).
/// </summary>
[DisallowMultipleComponent]
public class Health : NetworkBehaviour
{
    [Header("Stats source (optional)")]
    [Tooltip("If assigned, maxHealth is pulled from stats.Health at Awake. Leave empty for things (e.g. a boss or the player) that set maxHealth directly instead.")]
    public StatsSO stats;

    [Header("Health")]
    [Tooltip("Used directly if no EnemyStats is assigned.")]
    public int maxHealth = 100;

    // Serveren ejer livet. Klienter får værdien automatisk og kører de samme events.
    private readonly NetworkVariable<int> netHealth = new();
    public int CurrentHealth => netHealth.Value;
    public float HealthPercent => maxHealth > 0 ? (float)CurrentHealth / maxHealth : 0f;
    public bool IsDead { get; private set; }
    public bool IsInvulnerable { get; set; }

    [Header("Events")]
    [Tooltip("Fires every time damage is taken (successfully). Hook up hit-flash/SFX here.")]
    public UnityEvent onDamaged;
    [Tooltip("Fires with the new health percent (0-1) whenever health changes. Bosses can use this to trigger phase transitions; a health bar can bind to this directly.")]
    public UnityEvent<float> onHealthPercentChanged;
    [Tooltip("Fires once, when health reaches 0.")]
    public UnityEvent onDeath;

    private void Awake()
    {
        if (stats != null)
            maxHealth = stats.Health;

        netHealth.Value = maxHealth;
        // kører både på serveren (når den skriver) og på klienter (når de modtager)
        netHealth.OnValueChanged += OnHealthChanged;
    }

    public void TakeDamage(int amount)
    {
        if (IsSpawned && !IsServer) return; // kun hosten giver skade - gælder fireball, pile og melee
        if (IsInvulnerable || IsDead || amount <= 0) return;

        netHealth.Value = Mathf.Max(0, CurrentHealth - amount);
    }

    public void Heal(int amount)
    {
        if (IsSpawned && !IsServer) return;
        if (IsDead || amount <= 0) return;

        netHealth.Value = Mathf.Min(maxHealth, CurrentHealth + amount);
    }

    private void OnHealthChanged(int previous, int current)
    {
        if (current < previous) onDamaged?.Invoke();
        onHealthPercentChanged?.Invoke(HealthPercent);
        if (current <= 0 && !IsDead) Die();
    }

    private void Die()
    {
        IsDead = true;
        onDeath?.Invoke();
    }
}