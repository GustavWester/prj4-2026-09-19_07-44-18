using System.Collections;
using UnityEngine;

/// <summary>
/// Brief global time freeze on impact — the single cheapest way to make
/// a hit feel heavy. Put this on any persistent object (e.g. a GameManager)
/// and call:
///     HitStop.Instance.Stop(0.05f);      // small hit
///     HitStop.Instance.Stop(0.12f);      // big/death hit
/// Uses unscaled time internally so the freeze itself isn't affected by
/// the time scale it's setting.
/// </summary>
public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }

    private Coroutine activeStop;
    private float originalScale = 1f;

    private void Awake()
    {
        Instance = this;
    }

    // ellers peger Instance på et destroyed objekt efter et sceneskift, og Instance?.Stop() fejler
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Stop(float duration)
    {
        // et nyt hit mens tiden står stille må ikke gemme 0 som "originalen", ellers fryser spillet for altid
        if (activeStop != null) StopCoroutine(activeStop);
        else originalScale = Time.timeScale;
        activeStop = StartCoroutine(DoStop(duration));
    }

    private IEnumerator DoStop(float duration)
    {
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = originalScale;
        activeStop = null;
    }
}
