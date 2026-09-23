using System.Collections;
using UnityEngine;

/// <summary>
/// Simple camera shake. Put this on your Main Camera and call Shake()
/// from anywhere — e.g. hook Health.onDamaged / onDeath to it via the
/// Inspector (UnityEvent), or call it directly from code:
///     CameraShake.Instance.Shake(0.15f, 0.2f);
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Vector3 originalLocalPos;
    private Coroutine activeShake;

    private void Awake()
    {
        Instance = this;
        originalLocalPos = transform.localPosition;
    }

    /// <summary>Call with no args for a small default "hit" shake.</summary>
    public void Shake() => Shake(0.15f, 0.2f);

    public void Shake(float duration, float magnitude)
    {
        if (activeShake != null) StopCoroutine(activeShake);
        activeShake = StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        float t = 0f;
        while (t < duration)
        {
            Vector2 offset = Random.insideUnitCircle * magnitude;
            transform.localPosition = originalLocalPos + (Vector3)offset;
            t += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = originalLocalPos;
    }
}
