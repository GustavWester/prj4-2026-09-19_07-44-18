using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

/// <summary>
/// Skyder spillerens spell (fireball, beam ...) i den retning, spilleren vender, når man trykker Enter.
/// En stav kan skifte spell via SetSpell. Pris, cooldown og om man skal holde Enter står på spell-prefabben.
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

    // kaldes på alle maskiner (fra ItemPickup), så alle laver den samme spell når der skydes
    public void SetSpell(PlayerSpell newSpell) => spell = newSpell;

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
        bool fire = spell.HoldToFire
            ? kb.enterKey.isPressed || kb.numpadEnterKey.isPressed // fx beam: skyder så længe tasten holdes
            : kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame; // wasPressedThisFrame er kun true i den frame, hvor tasten trykkes ned.
        if (!fire) return;

        if (!playerManager.Resources.TrySpendMana(spell.ManaCost)) return; // ikke nok mana -> intet skud

        Vector2 dir = GetComponent<PlayerMovementController>().FacingDirection;
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
