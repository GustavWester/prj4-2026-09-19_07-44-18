using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Fælles for spillerens spells (PlayerBullet, PlayerBeam). PlayerAttackController skyder den
/// spell der er sat, og en stav (ItemSO.spell) kan skifte den ud.
/// Skade = baseDamage + skyttens spell damage (læses gennem PlayerManager).
/// </summary>
public abstract class PlayerSpell : MonoBehaviour
{
    [Tooltip("Spellens egen skade. Spillerens spell damage fra PlayerManager lægges oveni.")]
    [FormerlySerializedAs("damage")]
    [SerializeField] private int baseDamage = 1;
    [Tooltip("Mana det koster at skyde ét skud. Trækkes af PlayerAttackController.")]
    [SerializeField, Min(0)] private int manaCost = 5;
    [Tooltip("Sekunder mellem skud.")]
    [SerializeField, Min(0f)] private float cooldown = 0.1f;
    [Tooltip("Skyd så længe Enter holdes nede (fx beam). Ellers ét skud pr. tryk.")]
    [SerializeField] private bool holdToFire;
    [Tooltip("Vinkel mellem skud når man har flere (projectileCount).")]
    [SerializeField] private float spreadDeg = 12f;

    public int ManaCost => manaCost;
    public float Cooldown => cooldown;
    public bool HoldToFire => holdToFire;

    protected PlayerManager owner;

    protected int Damage => baseDamage + (owner != null ? owner.SpellDamage : 0);

    public abstract void Launch(Vector2 dir, PlayerManager shooter); //kaldes af den der skyder

    /// <summary>Vinkel for skud nr. i ud af count, fx -12, 0, 12 ved 3.</summary>
    public virtual float SpreadAngle(int i, int count) => (i - (count - 1) / 2f) * spreadDeg;

    protected void DealDamage(Collider2D other)
    {
        // Hver klient har sin egen spell, så kun hosten må give skade (ellers tælles den dobbelt)
        var nm = Unity.Netcode.NetworkManager.Singleton;
        // direkte kald i stedet for SendMessage, som ikke kan se TakeDamage's valgfrie source-parameter
        if ((nm == null || !nm.IsListening || nm.IsServer) && other.TryGetComponent(out ResourceController target))
            target.TakeDamage(Damage);
    }
}
