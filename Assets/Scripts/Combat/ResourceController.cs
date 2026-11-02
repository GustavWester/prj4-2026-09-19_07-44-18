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
///
/// Online: hosten ejer livet, ejeren ejer manaen, og alle kopier ser de samme tal.
/// Der skrives aldrig til en NetworkVariable før spawn (giver advarsel) - før spawn
/// og offline bruges de lokale værdier.
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
    public UnityEvent onDamaged = new();
    [Tooltip("Fires with the new health percent (0-1) whenever health changes. A health bar can bind to this directly.")]
    public UnityEvent<float> onHealthPercentChanged = new();
    [Tooltip("Fires once, when health reaches 0.")]
    public UnityEvent onDeath = new();
    [Tooltip("Fires with (current, max) whenever mana changes.")]
    public UnityEvent<int, int> onManaChanged = new();

    // Serveren ejer livet. Klienter får værdien automatisk og kører de samme events.
    // ikke readonly: Awake laver den med startværdien, fordi en skrivning før spawn giver en Netcode-advarsel
    private NetworkVariable<int> netHealth = new();
    public int CurrentHealth => netHealth.Value;
    public int MaxHealth => maxHealth;
    public float HealthPercent => ResourceMath.Percent(CurrentHealth, maxHealth);
    public bool IsDead { get; private set; }
    public bool IsInvulnerable { get; set; }

    // Mana ejes af spilleren selv (owner skriver), men læses af alle, så andre kan se manabaren.
    private readonly NetworkVariable<int> netMana = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);
    private int localMana; // før spawn / offline

    public int CurrentMana => IsSpawned ? netMana.Value : localMana;
    public int MaxMana => maxMana;
    public float ManaPercent => ResourceMath.Percent(CurrentMana, maxMana);

    private readonly RegenAccumulator regen = new(); // samler brøkdele op, så regen virker selvom manaRegenPerSecond * deltaTime < 1
    private bool initialized;

    private void Awake()
    {
        netHealth = new NetworkVariable<int>(maxHealth); // Netcode samler variablerne først ved spawn, efter Awake
        localMana = maxMana;
        initialized = true;
        // kører både på serveren (når den skriver) og på klienter (når de modtager)
        netHealth.OnValueChanged += OnHealthChanged;
        netMana.OnValueChanged += OnManaNetChanged;
    }

    public override void OnNetworkSpawn()
    {
        // Første skrivning sker først nu, hvor NetworkVariable'erne kender deres NetworkBehaviour.
        // Klienterne får værdierne synkroniseret.
        if (IsServer) netHealth.Value = Mathf.Clamp(localHealth, 0, maxHealth);
        if (IsOwner) netMana.Value = Mathf.Clamp(localMana, 0, maxMana);
    }

    private void Update()
    {
        if (!HasManaAuthority) return; // kun ejeren regenererer (ellers ville ikke-ejere forsøge at skrive)
        if (manaRegenPerSecond <= 0f || CurrentMana >= maxMana) return;

        int whole = regen.Tick(manaRegenPerSecond, Time.deltaTime);
        if (whole > 0) RestoreMana(whole);
    }

    // ---------------------------------------------------------------
    // Health
    // ---------------------------------------------------------------
    private bool HasHealthAuthority => !IsSpawned || IsServer;

    /// <summary>Skriver liv. Online går det via NetworkVariable (som fyrer eventet), offline fyrer vi det selv.</summary>
    private void SetHealth(int value)
    {
        if (IsSpawned)
        {
            netHealth.Value = value;
        }
        else
        {
            int previous = localHealth;
            localHealth = value;
            OnHealthChanged(previous, value);
        }
    }

    public void TakeDamage(int amount)
    {
        if (!HasHealthAuthority) return; // kun hosten giver skade - gælder fireball, pile og melee
        if (IsInvulnerable || IsDead || amount <= 0) return;

        SetHealth(ResourceMath.ApplyDamage(CurrentHealth, amount));
    }

    public void Heal(int amount)
    {
        if (!HasHealthAuthority) return;
        if (IsDead || amount <= 0) return;

        SetHealth(ResourceMath.ApplyHeal(CurrentHealth, amount, maxHealth));
    }

    /// <summary>
    /// Sætter nyt max-liv. Nuværende liv flyttes lige så meget, så +10 max også giver +10 liv.
    /// </summary>
    public void SetMaxHealth(int max)
    {
        max = Mathf.Max(1, max);
        int oldMax = maxHealth;
        maxHealth = max;
        if (!initialized) return; // Awake fylder livet op

        if (HasHealthAuthority && !IsDead)
            SetHealth(ResourceMath.ResizeCurrent(CurrentHealth, oldMax, maxHealth, 1));
        onHealthPercentChanged?.Invoke(HealthPercent);
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

    // ---------------------------------------------------------------
    // Mana
    // ---------------------------------------------------------------
    private bool HasManaAuthority => !IsSpawned || IsOwner;

    /// <summary>Skriver mana. Online går det via NetworkVariable (som fyrer eventet), offline fyrer vi det selv.</summary>
    private void SetMana(int value)
    {
        value = Mathf.Clamp(value, 0, maxMana);

        if (IsSpawned)
        {
            if (netMana.Value != value) netMana.Value = value;
        }
        else
        {
            localMana = value;
            onManaChanged?.Invoke(localMana, maxMana);
        }
    }

    private void OnManaNetChanged(int previous, int current) => onManaChanged?.Invoke(current, maxMana);

    /// <summary>Trækker mana hvis der er nok. Returnerer false (og trækker intet) hvis ikke. Kun ejeren kan bruge mana.</summary>
    public bool TrySpendMana(int amount)
    {
        if (!HasManaAuthority) return false;
        if (amount < 0 || CurrentMana < amount) return false;

        SetMana(CurrentMana - amount);
        return true;
    }

    public void RestoreMana(int amount)
    {
        if (!HasManaAuthority) return;
        if (amount <= 0) return;

        SetMana(CurrentMana + amount);
    }

    /// <summary>Sætter ny max-mana. Nuværende mana flyttes lige så meget.</summary>
    public void SetMaxMana(int max)
    {
        max = Mathf.Max(0, max);
        int oldMax = maxMana;
        maxMana = max;
        if (!initialized) return; // Awake fylder manaen op

        if (HasManaAuthority)
            SetMana(ResourceMath.ResizeCurrent(CurrentMana, oldMax, maxMana, 0));
        onManaChanged?.Invoke(CurrentMana, maxMana); // max kan have ændret sig selvom mana ikke gjorde
    }

    public void SetManaRegen(float perSecond) => manaRegenPerSecond = Mathf.Max(0f, perSecond);
}