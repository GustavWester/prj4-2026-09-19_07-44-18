using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using Unity.Collections;

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
    private readonly NetworkVariable<int> netHealth = new();
    private int localHealth; // før spawn / offline
    public int CurrentHealth => IsSpawned ? netHealth.Value : localHealth;
    public int MaxHealth => maxHealth;
    public float HealthPercent => ResourceMath.Percent(CurrentHealth, maxHealth);
    public bool IsDead { get; private set; }
    public bool IsInvulnerable { get; set; }

    // Hvad der gav det dræbende slag (fx "Goblin"), vises på game over-sedlen. Serveren skriver, alle læser.
    private readonly NetworkVariable<FixedString64Bytes> netKilledBy = new();
    private string localKilledBy = ""; // offline
    public string KilledBy => IsSpawned ? netKilledBy.Value.ToString() : localKilledBy;

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
        localHealth = maxHealth;
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

    public override void OnNetworkDespawn()
    {
        // Efter despawn læser CurrentHealth/CurrentMana de lokale værdier, så de skal passe.
        // Ellers ser en død scene-placeret boss (kun despawnet, ikke slettet) fuld ud, og stigen åbner aldrig.
        localHealth = netHealth.Value;
        localMana = netMana.Value;
        base.OnNetworkDespawn();
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

    /// <param name="source">Navnet på det der giver skaden. Vises på game over-sedlen, hvis det dræber.</param>
    public void TakeDamage(int amount, string source = "")
    {
        if (!HasHealthAuthority) return; // kun hosten giver skade - gælder fireball, pile og melee
        if (IsInvulnerable || IsDead || amount <= 0) return;

        int health = ResourceMath.ApplyDamage(CurrentHealth, amount);
        if (health <= 0) // skrives før livet, så klienterne har navnet når de ser døden
        {
            source ??= "";
            if (source.Length > 30) source = source[..30]; // FixedString64Bytes kaster ved over 61 bytes (æøå fylder 2)
            if (IsSpawned) netKilledBy.Value = new FixedString64Bytes(source);
            else localKilledBy = source;
        }
        SetHealth(health);
    }

    public void Heal(int amount)
    {
        if (!HasHealthAuthority) return;
        if (IsDead || amount <= 0) return;

        SetHealth(ResourceMath.ApplyHeal(CurrentHealth, amount, maxHealth));
    }

    /// <summary>Liv igen (percent af max), også hvis man er død. Try again (DeathScreen) giver fuldt, Revive halvt. Kun hosten.</summary>
    public void ResetHealth(float percent = 1f)
    {
        if (!HasHealthAuthority) return;
        SetHealth(Mathf.Max(1, Mathf.RoundToInt(maxHealth * percent)));
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

    private static readonly Color HealColor = new(0.4f, 1f, 0.4f);
    private static readonly Color PlayerHurtColor = new(1f, 0.3f, 0.3f);
    private static readonly Color EnemyHurtColor = new(1f, 0.95f, 0.6f);

    private void OnHealthChanged(int previous, int current)
    {
        // tal ved højre hjørne af figuren. Kører på alle maskiner, så alle ser heal og skade
        int delta = current - previous;
        if (delta != 0)
            FloatingText.Spawn(PopupPosition, delta > 0 ? $"+{delta}" : delta.ToString(),
                delta > 0 ? HealColor : CompareTag("Player") ? PlayerHurtColor : EnemyHurtColor);

        if (current < previous) onDamaged?.Invoke();
        onHealthPercentChanged?.Invoke(HealthPercent);
        if (current > 0) IsDead = false; // ResetHealth - kører også hos klienterne
        else if (!IsDead) Die();
    }

    /// <summary>Figurens øverste højre hjørne (sprite), hvor FloatingText popper op.</summary>
    public Vector3 PopupPosition => TryGetComponent(out Renderer r) ? r.bounds.max : transform.position;

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