using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

/// <summary>
/// Skyder en fireball i den retning, spilleren vender, når man trykker Enter.
/// Online: kun ejeren må skyde.
/// </summary>
[RequireComponent(typeof(PlayerMovementController))]
public class PlayerAttackController : NetworkBehaviour
{
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private float cooldown = 0.4f;

    private float cooldownTimer;

    private void Update()
    {
        if (IsSpawned && !IsOwner) return; // kun min egen wizard reagerer på mit tastatur
        cooldownTimer -= Time.deltaTime; //tæller ned hver frame

        var kb = Keyboard.current;
        if (kb == null || cooldownTimer > 0f) return; //hvis der ikke er tilsluttet et tastatur stopper vi
        if (!kb.enterKey.wasPressedThisFrame && !kb.numpadEnterKey.wasPressedThisFrame) return; // wasPressedThisFrame er kun true i den frame, hvor tasten trykkes ned.

        Vector2 dir = GetComponent<PlayerMovementController>().FacingDirection;
        if (IsSpawned) ShootRpc(transform.position, dir);
        else Shoot(transform.position, dir); // offline
        cooldownTimer = cooldown; //starter nedtælling
    }

    [Rpc(SendTo.Everyone)]
    private void ShootRpc(Vector2 pos, Vector2 dir) => Shoot(pos, dir);

    private void Shoot(Vector2 pos, Vector2 dir)
    {
        GameObject fireball = Instantiate(fireballPrefab, pos, Quaternion.identity);
        fireball.GetComponent<Fireball>().Launch(dir); //sender spillerens retning videre til fireBall scriptet
        GetComponent<Animator>().SetTrigger("Attack"); //afspiller angribsanimationen
    }
}
