using UnityEngine;

/// <summary>
/// Minimal top-down bullet: moves in a straight line and despawns
/// after a lifetime or when leaving the arena bounds.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBullet : MonoBehaviour
{
    public float lifetime = 6f;
    private Vector2 direction;
    private float speed;
    private int damage = 1;
    private string source = ""; // hvem der skød, til "Killed by"
    private Rigidbody2D rb;

    public void Init(Vector2 dir, float bulletSpeed, int bulletDamage = 1, string shooter = "")
    {
        damage = bulletDamage;
        source = shooter;
        direction = dir.normalized;
        speed = bulletSpeed;
        transform.right = direction; // pil peger i flyveretningen
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = direction * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) 
        {
            other.GetComponent<ResourceController>()?.TakeDamage(damage, source);
            Destroy(gameObject);
        }
        // alt solidt stopper kuglen (vægge, døre, kister ...), undtagen fjenderne selv, så den ikke dør i skytten.
        // Trigger-zoner (rum, dørens E-område) og andre kugler ignoreres
        else if (!other.isTrigger && other.GetComponentInParent<EnemyController>() == null)
        {
            Destroy(gameObject);
        }
    }
}
