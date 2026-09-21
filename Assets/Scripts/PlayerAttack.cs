using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Skyder en fireball mod musen, når man klikker med venstre museknap.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private float cooldown = 0.4f;

    private float cooldownTimer;

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame || cooldownTimer > 0f) return;

        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 direction = (mouseWorld - (Vector2)transform.position).normalized;

        // Rotation gør, at fireballens transform.right peger mod musen.
        Quaternion rotation = Quaternion.FromToRotation(Vector3.right, direction);
        Instantiate(fireballPrefab, transform.position, rotation);
        cooldownTimer = cooldown;
    }
}
