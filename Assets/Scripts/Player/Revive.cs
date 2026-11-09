using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Revive af en død medspiller. Står min levende wizard ved en død wizard, vises "Press E to revive".
/// Holdes E nede i reviveTime sekunder, fyldes en bar, og hosten giver den døde liv igen.
/// DeathScreen ruller selve døden tilbage (Idle, movement, attack, Rigidbody), når livet bliver over 0.
/// Sættes på wizard-prefabben sammen med prompt og bar: Tools > Setup Revive.
/// </summary>
[RequireComponent(typeof(ResourceController))]
public class Revive : NetworkBehaviour
{
    [SerializeField] private float reviveTime = 5f;
    [Tooltip("Hvor tæt (world-enheder) man skal stå på den døde wizard.")]
    [SerializeField] private float range = 1.5f;
    [Tooltip("Liv efter revive, i procent af max.")]
    [SerializeField, Range(0.01f, 1f)] private float reviveHealth = 0.5f;

    [Header("Sættes af Tools > Setup Revive")]
    [SerializeField] private GameObject prompt; // "Press E to revive"
    [SerializeField] private GameObject bar;
    [Tooltip("Image uden sprite, anchors stretch. Baren fyldes via anchorMax.x, som fjendernes healthbar.")]
    [SerializeField] private Image fill;

    [Header("Revive-animation")]
    [SerializeField] private Color flashColor = new(0.6f, 1f, 0.6f);
    [SerializeField] private float animTime = 0.4f;

    private ResourceController health;
    private float progress;

    private void Awake() => health = GetComponent<ResourceController>();

    private void Update()
    {
        bool canRevive = IsSpawned && health.IsDead && MeAliveNear();
        bool holding = canRevive && Keyboard.current != null && Keyboard.current.eKey.isPressed;
        progress = holding ? progress + Time.deltaTime / reviveTime : 0f; // slip E eller gå væk: forfra

        if (prompt.activeSelf != (canRevive && !holding)) prompt.SetActive(canRevive && !holding);
        if (bar.activeSelf != holding) bar.SetActive(holding);
        fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(progress), 1);

        if (progress < 1f) return;
        progress = 0f;
        ReviveRpc();
    }

    /// <summary>
    /// Står min levende wizard ved en død medspiller? Så er E til revive, og stige, døre og items ignorerer E.
    /// Kaldes kun når E trykkes (søger alle wizards).
    /// </summary>
    public static bool AnyNearby()
    {
        foreach (var r in FindObjectsByType<Revive>())
            if (r.IsSpawned && r.health.IsDead && r.MeAliveNear()) return true;
        return false;
    }

    // kun min egen wizard kan revive, og kun hvis den selv lever
    private bool MeAliveNear()
    {
        GameObject me = LocalPlayer.Current;
        return me != null && me != gameObject && !me.GetComponent<ResourceController>().IsDead
               && Vector2.Distance(me.transform.position, transform.position) <= range;
    }

    // serveren tjekker igen og har det sidste ord. +1 i range, så lag ikke afviser et ærligt revive
    [Rpc(SendTo.Server)]
    private void ReviveRpc(RpcParams rpc = default)
    {
        if (!health.IsDead) return; // en anden nåede det først
        var reviver = NetworkManager.ConnectedClients.TryGetValue(rpc.Receive.SenderClientId, out var client) ? client.PlayerObject : null;
        if (reviver == null || reviver.GetComponent<ResourceController>().IsDead
            || Vector2.Distance(reviver.transform.position, transform.position) > range + 1f) return;

        health.ResetHealth(reviveHealth);
        ReviveAnimRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void ReviveAnimRpc() => StartCoroutine(ReviveAnim());

    // lille revive-animation: wizarden popper op fra lille og blinker fra grøn til normal
    private IEnumerator ReviveAnim()
    {
        var sprite = GetComponent<SpriteRenderer>();
        Vector3 scale = transform.localScale;
        Color normal = sprite.color;
        for (float t = 0; t < animTime; t += Time.deltaTime)
        {
            float k = t / animTime;
            transform.localScale = scale * Mathf.Lerp(0.6f, 1f, 1 - (1 - k) * (1 - k)); // ease out
            sprite.color = Color.Lerp(flashColor, normal, k);
            yield return null;
        }
        transform.localScale = scale;
        sprite.color = normal;
    }
}
