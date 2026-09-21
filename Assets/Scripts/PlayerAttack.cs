using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Skyder en fireball i den retning, spilleren vender, når man trykker Enter.
/// </summary>
[RequireComponent(typeof(MovementController))]
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private float cooldown = 0.4f;

    private float cooldownTimer;

    private void Update()
    {
        cooldownTimer -= Time.deltaTime; //tæller ned hver frame

        var kb = Keyboard.current;
        if (kb == null || cooldownTimer > 0f) return; //hvis der ikke er tilsluttet et tastatur stopper vi
        if (!kb.enterKey.wasPressedThisFrame && !kb.numpadEnterKey.wasPressedThisFrame) return; // wasPressedThisFrame er kun true i den frame, hvor tasten trykkes ned.

        GameObject fireball = Instantiate(fireballPrefab, transform.position, Quaternion.identity);
        fireball.GetComponent<Fireball>().Launch(GetComponent<MovementController>().FacingDirection); //sender spillerens retning videre til fireBall scriptet
        GetComponent<Animator>().SetTrigger("Attack"); //afspiller angribsanimationen
        cooldownTimer = cooldown; //starter nedtælling
    }
}
