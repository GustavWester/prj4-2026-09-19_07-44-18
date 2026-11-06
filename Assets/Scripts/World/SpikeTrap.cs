using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Spike-fælde i gulvet: spidserne skyder op og trækker sig ned i en fast takt og giver skade,
/// mens de er oppe. Kræver en Collider2D med Is Trigger.
///
/// Takten regnes ud fra serverens tid, så alle spillere ser spidserne på samme tidspunkt
/// uden at der sendes noget over netværket. Skaden tæller kun hos hosten, fordi
/// ResourceController.TakeDamage kun virker der - ligesom fjendernes kugler.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class SpikeTrap : MonoBehaviour
{
    [Tooltip("0 = nede, 1 = advarsel, 2 = halvt oppe, 3 = helt oppe.")]
    [SerializeField] private Sprite[] frames = new Sprite[4];
    [SerializeField] private float cycle = 3f;      // sekunder for én runde
    [SerializeField] private float upTime = 0.8f;   // hvor længe spidserne er helt oppe
    [Tooltip("Forskyder takten, så fælder ved siden af hinanden kan bølge.")]
    [SerializeField] private float offset;
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitCooldown = 0.75f;

    private const float Warn = 0.3f, Move = 0.1f; // advarsel og op/ned-frame
    private SpriteRenderer sr;
    private readonly Dictionary<ResourceController, float> lastHit = new();

    private bool Up => Frame == 3;

    private int Frame
    {
        get
        {
            float t = Mathf.Repeat(Now + offset, cycle);
            float upStart = cycle - upTime - Move; // spidserne går op mod slutningen af runden
            if (t < upStart - Warn) return 0;
            if (t < upStart) return 1;
            if (t < upStart + Move) return 2;
            if (t < cycle - Move) return 3;
            return 2;
        }
    }

    private static float Now =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening
            ? (float)NetworkManager.Singleton.ServerTime.Time
            : Time.time;

    private void Awake() => sr = GetComponent<SpriteRenderer>();

    private void Update()
    {
        var f = frames[Frame];
        if (f != null && sr.sprite != f) sr.sprite = f;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!Up || !other.CompareTag("Player") || !other.TryGetComponent(out ResourceController hp)) return;
        if (lastHit.TryGetValue(hp, out float t) && Time.time - t < hitCooldown) return;
        lastHit[hp] = Time.time;
        hp.TakeDamage(damage, "Spikes");
    }
}
