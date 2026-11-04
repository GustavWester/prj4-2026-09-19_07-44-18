using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Får fakkelens Light2D til at blafre som en flamme.
/// Perlin noise i stedet for Random, så lyset svinger blødt i stedet for at blinke hakket.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class TorchFlicker : MonoBehaviour
{
    [SerializeField] private float amount = 0.25f; // hvor meget intensiteten svinger op og ned
    [SerializeField] private float speed = 6f;

    private Light2D glow;
    private float baseIntensity;
    private float seed; // forskellig pr. fakkel, så de ikke blafrer i takt

    private void Awake()
    {
        glow = GetComponent<Light2D>();
        baseIntensity = glow.intensity;
        seed = Random.value * 100f;
    }

    private void Update()
    {
        glow.intensity = baseIntensity + (Mathf.PerlinNoise(seed, Time.time * speed) - 0.5f) * 2f * amount;
    }
}
