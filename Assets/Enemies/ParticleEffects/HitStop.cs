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

    private void Awake()
    {
        Instance = this;
    }

    public void Stop(float duration)
    {
        if (activeStop != null) StopCoroutine(activeStop);
        activeStop = StartCoroutine(DoStop(duration));
    }

    private IEnumerator DoStop(float duration)
    {
        float originalScale = Time.timeScale;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = originalScale;
    }
}
