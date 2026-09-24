using UnityEngine;
using UnityEngine.Events;

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
public class Health : MonoBehaviour
{
    [Header("Stats source (optional)")]
    [Tooltip("If assigned, maxHealth is pulled from stats.Health at Awake. Leave empty for things (e.g. a boss or the player) that set maxHealth directly instead.")]
    public EnemyStats stats;

    [Header("Health")]
    [Tooltip("Used directly if no EnemyStats is assigned.")]
    public int maxHealth = 100;

    public int CurrentHealth { get; private set; }
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

        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsInvulnerable || IsDead || amount <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        onDamaged?.Invoke();
        onHealthPercentChanged?.Invoke(HealthPercent);

        if (CurrentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        onHealthPercentChanged?.Invoke(HealthPercent);
    }

    private void Die()
    {
        IsDead = true;
        onDeath?.Invoke();
    }
}