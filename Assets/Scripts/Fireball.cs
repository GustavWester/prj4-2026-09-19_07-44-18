using UnityEngine;

/// <summary>
/// Flyver ligeud i den retning, den bliver skudt afsted i, og skader det første den rammer.
/// </summary>
public class Fireball : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 3f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Sprite peger mod højre, så transform.right er flyveretningen.
        transform.position += transform.right * (speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<MovementController>() != null) return; // ignorer spilleren

        // SendMessage, så enemies bare skal have en TakeDamage(int)-metode. Ingen krav om fælles klasse.
        other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        Destroy(gameObject);
    }
}
