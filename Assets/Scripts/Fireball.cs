using UnityEngine;

/// <summary>
/// Flyver ligeud i den retning, den bliver skudt afsted i, og skader det første den rammer.
/// Animationen vælges efter retning (højre/foran/bagved) via blend tree i Animator.
/// </summary>
public class Fireball : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 3f;

    private Vector2 direction = Vector2.right;

    public void Launch(Vector2 dir)
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
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * (speed * Time.deltaTime));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<MovementController>() != null) return; // ignorer spilleren

        // SendMessage, så enemies bare skal have en TakeDamage(int)-metode. Ingen krav om fælles klasse.
        other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        Destroy(gameObject);
    }
}
