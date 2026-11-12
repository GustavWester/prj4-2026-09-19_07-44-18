using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

/// <summary>
/// Skyder spillerens spell (fireball, beam ...) i piletasternes retning (to piletaster = skråt).
/// En stav kan skifte spell via SetSpell. Pris, cooldown og om man skal holde piletasten står på spell-prefabben.
/// Koster mana (fra ResourceController via PlayerManager).
/// Online: kun ejeren må skyde.
/// </summary>
[RequireComponent(typeof(PlayerMovementController), typeof(PlayerManager))]
public class PlayerAttackController : NetworkBehaviour
{
    [Tooltip("Start-spell. Skiftes når man samler en stav med en spell op.")]
    [SerializeField] private GameObject fireballPrefab;

    private float cooldownTimer;
    private PlayerManager playerManager;
    private PlayerSpell spell;

    private void Awake()
    {
        playerManager = GetComponent<PlayerManager>();
        spell = fireballPrefab.GetComponent<PlayerSpell>();
    }

    public PlayerSpell Spell => spell;

    // kaldes på alle maskiner (fra ItemPickup/ItemDropper), så alle laver den samme spell når der skydes. null = start-fireball
    public void SetSpell(PlayerSpell newSpell) => spell = newSpell != null ? newSpell : fireballPrefab.GetComponent<PlayerSpell>();

    // spell som da levelen startede (DeathScreen / Try again)
    private PlayerSpell savedSpell;
    public void SaveCheckpoint() => savedSpell = spell;
    public void LoadCheckpoint() { if (savedSpell != null) spell = savedSpell; }

    private void Update()
    {
        if (IsSpawned && !IsOwner) return; // kun min egen wizard reagerer på mit tastatur
        if (playerManager.Resources.IsDead) return; // døde skyder ikke
        cooldownTimer -= Time.deltaTime; //tæller ned hver frame

        var kb = Keyboard.current;
        if (kb == null || cooldownTimer > 0f) return; //hvis der ikke er tilsluttet et tastatur stopper vi
        // piletasterne bestemmer retningen, to på én gang = skråt (fx op + højre)
        Vector2 dir = new Vector2(
            (kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.leftArrowKey.isPressed ? 1f : 0f),
            (kb.upArrowKey.isPressed ? 1f : 0f) - (kb.downArrowKey.isPressed ? 1f : 0f)).normalized;
        bool fire = dir != Vector2.zero && (spell.HoldToFire // fx beam: skyder så længe en piletast holdes
            || kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame // ellers ét skud pr. tryk
            || kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame);
        if (!fire) return;

        if (!playerManager.Resources.TrySpendMana(spell.ManaCost)) return; // ikke nok mana -> intet skud

        if (IsSpawned) ShootRpc(transform.position, dir);
        else Shoot(transform.position, dir); // offline
        cooldownTimer = spell.Cooldown; //starter nedtælling
    }

    [Rpc(SendTo.Everyone)]
    private void ShootRpc(Vector2 pos, Vector2 dir) => Shoot(pos, dir);

    private void Shoot(Vector2 pos, Vector2 dir)
    {
        int count = playerManager.ProjectileCount;
        for (int i = 0; i < count; i++)
        {
            Vector2 d = Quaternion.Euler(0, 0, spell.SpreadAngle(i, count)) * dir;
            Instantiate(spell, pos, Quaternion.identity).Launch(d, playerManager); //sender retning og skytte videre, så spellen kan læse spell damage
        }
        GetComponent<Animator>().SetTrigger("Attack"); //afspiller angribsanimationen
    }
}
