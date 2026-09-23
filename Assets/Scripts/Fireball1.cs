using UnityEngine;
using UnityEngine.Rendering.Universal; // Light2D

// Flyver ligeud i den retning, den bliver skudt afsted i, og skader det første den rammer.
// Animationen vælges efter retning (højre/foran/bagved) via blend tree i Animator.
public class Fireball1 : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 3f;

    [Header("Juice - light")]
    [Tooltip("Optional: a Light2D on this fireball (or a child of it) for the glow/bloom effect while it flies.")]
    [SerializeField] private Light2D glowLight;
    [SerializeField] private float baseLightIntensity = 1.2f;
    [SerializeField] private float lightPulseSpeed = 10f;
    [SerializeField] private float lightPulseAmount = 0.3f;

    [Header("Juice - impact")]
    [Tooltip("Particle system prefab spawned on hit. Set its 'Stop Action' to Destroy so it cleans itself up.")]
    [SerializeField] private ParticleSystem impactEffectPrefab;
    [SerializeField] private float cameraShakeDuration = 0.08f;
    [SerializeField] private float cameraShakeMagnitude = 0.12f;
    [SerializeField] private float hitStopDuration = 0.02f;

    private Vector2 direction = Vector2.right;

    public void Launch(Vector2 dir) //kaldes af den der skyder ildkuglen
    {
        direction = dir.normalized;

        // Snap til nærmeste af de tre animationer: vandret -> Right, lodret -> Front/Behind.
        bool horizontal = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y);
        var animator = GetComponent<Animator>();
        animator.SetFloat("DirX", horizontal ? 1f : 0f);
        animator.SetFloat("DirY", horizontal ? 0f : Mathf.Sign(direction.y));

        GetComponent<SpriteRenderer>().flipX = horizontal && direction.x < 0f; // venstre spejles
    }

    private void Start()
    {
        Destroy(gameObject, lifetime); //sletter kuglen igen efter 3 sekunder, hvis den ikke har ramt noget

        if (glowLight != null)
            glowLight.intensity = baseLightIntensity;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * (speed * Time.deltaTime)); //farten er uafhængigt af vores framerate

        // Let pulsering af lyset, så den ikke bare gløder statisk - giver et "levende" flamme-look.
        if (glowLight != null)
            glowLight.intensity = baseLightIntensity + Mathf.Sin(Time.time * lightPulseSpeed) * lightPulseAmount;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<MovementController>() != null) return; // ignorer spilleren

        // SendMessage, så enemies bare skal have en TakeDamage(int)-metode. Ingen krav om fælles klasse.
        other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

        PlayImpactJuice();
        Destroy(gameObject);
    }

    private void PlayImpactJuice()
    {
        if (impactEffectPrefab != null)
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);

        CameraShake.Instance?.Shake(cameraShakeDuration, cameraShakeMagnitude);
        HitStop.Instance?.Stop(hitStopDuration);
    }
}
