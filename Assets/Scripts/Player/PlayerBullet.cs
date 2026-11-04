using UnityEngine;
using UnityEngine.Rendering.Universal; // Light2D

// Spillerens projektil (fx fireball). Flyver ligeud i den retning, den bliver skudt afsted i, og skader det første den rammer.
// Varianter (sættes på prefabben): bounces = hopper af vægge, pierce = flyver igennem fjender,
// orbitRadius > 0 = cirkler om spilleren i stedet for at flyve ligeud.
// Animationen vælges efter retning (højre/foran/bagved) via blend tree i Animator.
public class PlayerBullet : PlayerSpell
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 3f;

    [Header("Varianter (0 = almindelig fireball)")]
    [Tooltip("Antal gange den hopper af en væg før den forsvinder.")]
    [SerializeField, Min(0)] private int bounces;
    [Tooltip("Antal fjender den flyver igennem før den stopper.")]
    [SerializeField, Min(0)] private int pierce;
    [Tooltip("> 0: cirkler om spilleren i denne afstand og ignorerer vægge.")]
    [SerializeField, Min(0f)] private float orbitRadius;
    [Tooltip("Grader pr. sekund når den cirkler.")]
    [SerializeField] private float orbitSpeedDeg = 270f;

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
    private Vector2 lastPos; // positionen sidste frame, bruges til at finde vægnormalen ved bounce
    private float orbitAngle;

    private bool Orbits => orbitRadius > 0f;

    // i en kreds fordeles kuglerne jævnt hele vejen rundt
    public override float SpreadAngle(int i, int count) => Orbits ? i * 360f / count : base.SpreadAngle(i, count);

    public override void Launch(Vector2 dir, PlayerManager shooter)
    {
        direction = dir.normalized;
        owner = shooter;
        orbitAngle = Vector2.SignedAngle(Vector2.right, direction);
        UpdateFacing();
    }

    // Snap til nærmeste af de tre animationer: vandret -> Right, lodret -> Front/Behind.
    private void UpdateFacing()
    {
        var facing = SpriteFacing.From(direction.x, direction.y);
        var animator = GetComponent<Animator>();
        animator.SetFloat("DirX", facing.DirX);
        animator.SetFloat("DirY", facing.DirY);

        GetComponent<SpriteRenderer>().flipX = facing.FlipX; // venstre spejles
    }

    private void Start()
    {
        Destroy(gameObject, lifetime); //sletter kuglen igen efter lifetime sekunder, hvis den ikke har ramt noget
        lastPos = transform.position;

        if (glowLight != null)
            glowLight.intensity = baseLightIntensity;
    }

    private void Update()
    {
        lastPos = transform.position;

        if (Orbits)
        {
            if (owner == null) { Destroy(gameObject); return; } // spilleren er væk
            orbitAngle += orbitSpeedDeg * Time.deltaTime;
            Vector2 offset = Quaternion.Euler(0, 0, orbitAngle) * Vector2.right;
            transform.position = (Vector2)owner.transform.position + offset * orbitRadius;
        }
        else
            transform.position += (Vector3)(direction * (speed * Time.deltaTime)); //farten er uafhængigt af vores framerate

        // Let pulsering af lyset, så den ikke bare gløder statisk - giver et "levende" flamme-look.
        if (glowLight != null)
            glowLight.intensity = baseLightIntensity + Mathf.Sin(Time.time * lightPulseSpeed) * lightPulseAmount;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return; // zoner (rum, dørens E-område) er ikke noget man kan ramme
        if (other.CompareTag("Player")) return; // ignorer spilleren
        if (other.CompareTag("Bullet")) return;

        bool enemy = other.GetComponent<ResourceController>() != null; // alt andet regnes som væg
        if (enemy)
        {
            DealDamage(other);
            if (pierce-- > 0) { PlayImpactJuice(); return; } // flyver videre
        }
        else
        {
            if (Orbits) return; // kredsen går igennem vægge
            if (bounces-- > 0) { Bounce(other); return; }
        }

        PlayImpactJuice();
        Destroy(gameObject);
    }

    // ponytail: normalen findes ud fra nærmeste punkt, passer til lige tilemap-vægge. Skrå/runde vægge -> Physics2D.Raycast
    private void Bounce(Collider2D wall)
    {
        Vector2 normal = (lastPos - wall.ClosestPoint(lastPos)).normalized;
        if (normal == Vector2.zero) normal = -direction;
        direction = Vector2.Reflect(direction, normal);
        transform.position = lastPos; // ud af væggen igen
        UpdateFacing();
    }

    private void PlayImpactJuice()
    {
        if (impactEffectPrefab != null)
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);

        // Kuglen findes hos alle spillere. Kun skytten må ryste og fryse, ellers hakker de andres skærm ved hvert hit
        if (owner == null || !owner.GetComponent<PlayerMovementController>().HasControl) return;
        CameraShake.Instance?.Shake(cameraShakeDuration, cameraShakeMagnitude);
        HitStop.Instance?.Stop(hitStopDuration);
    }
}
