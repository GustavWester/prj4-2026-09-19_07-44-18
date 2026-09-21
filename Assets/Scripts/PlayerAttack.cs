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
        cooldownTimer -= Time.deltaTime;

        var kb = Keyboard.current;
        if (kb == null || cooldownTimer > 0f) return;
        if (!kb.enterKey.wasPressedThisFrame && !kb.numpadEnterKey.wasPressedThisFrame) return;

        GameObject fireball = Instantiate(fireballPrefab, transform.position, Quaternion.identity);
        fireball.GetComponent<Fireball>().Launch(GetComponent<MovementController>().FacingDirection);
        GetComponent<Animator>().SetTrigger("Attack");
        cooldownTimer = cooldown;
    }
}
