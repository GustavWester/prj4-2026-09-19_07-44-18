using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal; // Light2D

/// <summary>
/// Fades a Light2D's intensity down to zero over a short duration instead
/// of letting it hold steady until the GameObject is destroyed. Use this
/// on short impact/muzzle-flash lights so they die gracefully rather than
/// popping off abruptly when the parent effect is cleaned up.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class ImpactLightFlash : MonoBehaviour
{
    [Tooltip("How long the light stays at full intensity before it starts fading.")]
    [SerializeField] private float holdDuration = 0.03f;
    [Tooltip("How long the fade-out itself takes.")]
    [SerializeField] private float fadeDuration = 0.2f;

    private Light2D light2D;
    private float startIntensity;

    private void Awake()
    {
        light2D = GetComponent<Light2D>();
        startIntensity = light2D.intensity;
    }

    private void Start()
    {
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(holdDuration);

        float t = 0f;
        while (t < fadeDuration)
        {
            light2D.intensity = Mathf.Lerp(startIntensity, 0f, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }

        light2D.intensity = 0f;
    }
}