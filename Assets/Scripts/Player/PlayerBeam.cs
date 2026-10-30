using UnityEngine;

// Stråle: rammer straks det første i retningen (op til range) og tegnes med en LineRenderer.
// Lever kun ét cooldown. Sæt Hold To Fire på prefabben, så PlayerAttackController skyder en ny
// hver cooldown mens Enter holdes - det ligner en sammenhængende stråle, der skader flere gange i sekundet.
[RequireComponent(typeof(LineRenderer))]
public class PlayerBeam : PlayerSpell
{
    [SerializeField] private float range = 8f;

    public override void Launch(Vector2 dir, PlayerManager shooter)
    {
        owner = shooter;
        Vector2 start = transform.position;
        Vector2 end = start + dir.normalized * range;

        // RaycastAll er sorteret efter afstand, så det første der ikke springes over er det nærmeste
        foreach (var hit in Physics2D.RaycastAll(start, dir, range))
        {
            var other = hit.collider;
            if (other.isTrigger || other.CompareTag("Player") || other.CompareTag("Bullet")) continue; // samme regler som PlayerBullet
            DealDamage(other);
            end = hit.point;
            break;
        }

        var line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        Destroy(gameObject, Cooldown);
    }
}
