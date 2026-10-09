using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

/// <summary>
/// Holder styr på de ressourcer der ændrer sig under spillet: nuværende liv og mana,
/// skade, heal, mana-forbrug, regen og død. Bruges af både spilleren og fjender —
/// alt der giver skade (kugler, melee) kalder bare
/// GetComponent&lt;ResourceController&gt;()?.TakeDamage(amount) på det, den rammer.
///
/// Max-værdierne kommer udefra: PlayerManager (fra PlayerStatsSO) eller
/// EnemyController (fra EnemyStatsSO) kalder SetMaxHealth/SetMaxMana.
/// Uden det bruges værdierne sat i inspectoren.
/// </summary>
[DisallowMultipleComponent]
public class ResourceController : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    [Header("Mana")]
    [SerializeField] private int maxMana = 0;
    [Tooltip("Mana pr. sekund der regenereres. 0 = ingen regen.")]
    [SerializeField] private float manaRegenPerSecond = 0f;

    [Header("Events")]
    [Tooltip("Fires every time damage is taken (successfully). Hook up hit-flash/SFX here.")]
    public UnityEvent onDamaged;
    [Tooltip("Fires with the new health percent (0-1) whenever health changes. A health bar can bind to this directly.")]
    public UnityEvent<float> onHealthPercentChanged;
    [Tooltip("Fires once, when health reaches 0.")]
    public UnityEvent onDeath;
    [Tooltip("Fires with (current, max) whenever mana changes.")]
    public UnityEvent<int, int> onManaChanged;

    // Serveren ejer livet. Klienter får værdien automatisk og kører de samme events.
    private readonly NetworkVariable<int> netHealth = new();
    public int CurrentHealth => netHealth.Value;
    public int MaxHealth => maxHealth;
    public float HealthPercent => maxHealth > 0 ? (float)CurrentHealth / maxHealth : 0f;
    public bool IsDead { get; private set; }
    public bool IsInvulnerable { get; set; }

    // Mana er lokal: kun ejeren bruger den (til at skyde), så den synkes ikke.
    public int CurrentMana { get; private set; }
    public int MaxMana => maxMana;
    public float ManaPercent => maxMana > 0 ? (float)CurrentMana / maxMana : 0f;

    private float regenBuffer; // samler brøkdele op, så regen virker selvom manaRegenPerSecond * deltaTime < 1
    private bool initialized;

    private void Awake()
    {
        netHealth.Value = maxHealth;
        CurrentMana = maxMana;
        initialized = true;
        // kører både på serveren (når den skriver) og på klienter (når de modtager)
        netHealth.OnValueChanged += OnHealthChanged;
    }

    private void Update()
    {
        if (manaRegenPerSecond <= 0f || CurrentMana >= maxMana) return;

        regenBuffer += manaRegenPerSecond * Time.deltaTime;
        if (regenBuffer >= 1f)
        {
            int whole = Mathf.FloorToInt(regenBuffer);
            regenBuffer -= whole;
            RestoreMana(whole);
        }
    }

    // ---------------------------------------------------------------
    // Health
    // ---------------------------------------------------------------
    public void TakeDamage(int amount)
    {
        if (!HasHealthAuthority) return; // kun hosten giver skade - gælder fireball, pile og melee
        if (IsInvulnerable || IsDead || amount <= 0) return;

        netHealth.Value = Mathf.Max(0, CurrentHealth - amount);
    }

    public void Heal(int amount)
    {
        if (!HasHealthAuthority) return;
        if (IsDead || amount <= 0) return;

        netHealth.Value = Mathf.Min(maxHealth, CurrentHealth + amount);
    }

    /// <summary>
    /// Sætter nyt max-liv. Nuværende liv flyttes lige så meget, så +10 max også giver +10 liv.
    /// </summary>
    public void SetMaxHealth(int max)
    {
        max = Mathf.Max(1, max);
        int delta = max - maxHealth;
        maxHealth = max;
        if (!initialized) return; // Awake fylder livet op

        if (HasHealthAuthority && !IsDead)
            netHealth.Value = Mathf.Clamp(CurrentHealth + delta, 1, maxHealth);
        onHealthPercentChanged?.Invoke(HealthPercent);
    }

    private bool HasHealthAuthority => !IsSpawned || IsServer;

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

    // ---------------------------------------------------------------
    // Mana
    // ---------------------------------------------------------------
    /// <summary>Trækker mana hvis der er nok. Returnerer false (og trækker intet) hvis ikke.</summary>
    public bool TrySpendMana(int amount)
    {
        if (amount < 0 || CurrentMana < amount) return false;

        CurrentMana -= amount;
        onManaChanged?.Invoke(CurrentMana, maxMana);
        return true;
    }

    public void RestoreMana(int amount)
    {
        if (amount <= 0) return;

        CurrentMana = Mathf.Min(maxMana, CurrentMana + amount);
        onManaChanged?.Invoke(CurrentMana, maxMana);
    }

    /// <summary>Sætter ny max-mana. Nuværende mana flyttes lige så meget.</summary>
    public void SetMaxMana(int max)
    {
        max = Mathf.Max(0, max);
        int delta = max - maxMana;
        maxMana = max;
        if (!initialized) return; // Awake fylder manaen op

        CurrentMana = Mathf.Clamp(CurrentMana + delta, 0, maxMana);
        onManaChanged?.Invoke(CurrentMana, maxMana);
    }

    public void SetManaRegen(float perSecond) => manaRegenPerSecond = Mathf.Max(0f, perSecond);
}
