using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Universal behaviour for small enemies: movement + attacking only.
/// No HP/stats here on purpose — pair this with a separate stats/health
/// component. Movement is picked via a dropdown enum so you can reuse
/// this one script across many enemy types just by changing values
/// in the inspector (or on prefab variants).
///
/// Attack patterns reuse the same BulletPatternSO assets and firing
/// logic as the boss, via BulletPatternFirer.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBehaviour : MonoBehaviour
{
    public enum MovementPattern
    {
        Stationary,
        Linear,       // flies in one fixed direction, e.g. straight down the screen
        Waypoints,    // patrols a list of points, looping
        Orbit,        // circles around a center point
        Sine,         // moves along a direction while weaving side to side
        ChasePlayer,  // moves straight at the player
        KeepDistance  // approaches/retreats to sit at a preferred range
    }

    [Header("Targeting")]
    public Transform player;
    public Transform firePoint;

    [Header("Movement pattern")]
    public MovementPattern movementPattern = MovementPattern.Linear;
    public float moveSpeed = 3f;

    [Header("Linear settings")]
    public Vector2 linearDirection = Vector2.down;

    [Header("Waypoints settings")]
    public Transform[] waypoints;
    public float waypointReachedDistance = 0.2f;
    private int waypointIndex;

    [Header("Orbit settings")]
    public Transform orbitCenter;
    public float orbitRadius = 3f;
    public float orbitAngularSpeedDeg = 60f;
    private float orbitAngleDeg;

    [Header("Sine / weave settings")]
    public Vector2 sineForwardDirection = Vector2.down;
    public float sineAmplitude = 1.5f;
    public float sineFrequency = 2f;
    private float sineTimer;
    private Vector3 sineOrigin;

    [Header("Chase / keep-distance settings")]
    public float preferredDistance = 4f;
    public float distanceTolerance = 0.5f;

    [Header("Attack")]
    public List<BulletPatternSO> attackPatterns;
    [Tooltip("Seconds between the start of one attack and the next.")]
    public float attackInterval = 2f;
    public bool randomizePatternOrder = true;
    [Tooltip("Small random +/- added to attackInterval each time, so enemies of the same type don't all fire in sync.")]
    public float attackIntervalJitter = 0.3f;

    private Rigidbody2D rb;
    private int patternIndex;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        sineOrigin = transform.position;
        orbitAngleDeg = Random.Range(0f, 360f); // stagger multiple orbiters
    }

    private void Start()
    {
        if (attackPatterns != null && attackPatterns.Count > 0 && firePoint != null)
            StartCoroutine(AttackLoop());
    }

    private void FixedUpdate()
    {
        switch (movementPattern)
        {
            case MovementPattern.Stationary:
                rb.linearVelocity = Vector2.zero;
                break;
            case MovementPattern.Linear:
                MoveLinear();
                break;
            case MovementPattern.Waypoints:
                MoveWaypoints();
                break;
            case MovementPattern.Orbit:
                MoveOrbit();
                break;
            case MovementPattern.Sine:
                MoveSine();
                break;
            case MovementPattern.ChasePlayer:
                MoveChasePlayer();
                break;
            case MovementPattern.KeepDistance:
                MoveKeepDistance();
                break;
        }
    }

    // ---------------------------------------------------------------
    // Movement patterns
    // ---------------------------------------------------------------
    private void MoveLinear()
    {
        rb.linearVelocity = linearDirection.normalized * moveSpeed;
    }

    private void MoveWaypoints()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Transform target = waypoints[waypointIndex];
        Vector2 dir = (Vector2)target.position - rb.position;

        if (dir.magnitude <= waypointReachedDistance)
        {
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            return;
        }

        rb.linearVelocity = dir.normalized * moveSpeed;
    }

    private void MoveOrbit()
    {
        Vector2 center = orbitCenter != null ? (Vector2)orbitCenter.position : Vector2.zero;

        orbitAngleDeg += orbitAngularSpeedDeg * Time.fixedDeltaTime;
        float rad = orbitAngleDeg * Mathf.Deg2Rad;
        Vector2 targetPos = center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadius;

        // MovePosition gives a clean circular path without fighting physics.
        rb.MovePosition(targetPos);
    }

    private void MoveSine()
    {
        sineTimer += Time.fixedDeltaTime;

        Vector2 forward = sineForwardDirection.normalized;
        Vector2 perpendicular = new Vector2(-forward.y, forward.x);

        Vector3 forwardOffset = (Vector3)(forward * moveSpeed * sineTimer);
        float weave = Mathf.Sin(sineTimer * sineFrequency) * sineAmplitude;
        Vector3 sideOffset = (Vector3)(perpendicular * weave);

        rb.MovePosition(sineOrigin + forwardOffset + sideOffset);
    }

    private void MoveChasePlayer()
    {
        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 dir = (Vector2)player.position - rb.position;
        rb.linearVelocity = dir.normalized * moveSpeed;
    }

    private void MoveKeepDistance()
    {
        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float distance = toPlayer.magnitude;

        if (Mathf.Abs(distance - preferredDistance) <= distanceTolerance)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Too far -> move closer. Too close -> back away.
        float direction = distance > preferredDistance ? 1f : -1f;
        rb.linearVelocity = toPlayer.normalized * moveSpeed * direction;
    }

    // ---------------------------------------------------------------
    // Attacking (reuses the boss's bullet-pattern assets and firing logic)
    // ---------------------------------------------------------------
    private IEnumerator AttackLoop()
    {
        while (true)
        {
            float jitter = Random.Range(-attackIntervalJitter, attackIntervalJitter);
            yield return new WaitForSeconds(Mathf.Max(0.05f, attackInterval + jitter));

            BulletPatternSO pattern = PickPattern();
            if (pattern != null)
                yield return StartCoroutine(RunPattern(pattern));
        }
    }

    private BulletPatternSO PickPattern()
    {
        if (attackPatterns == null || attackPatterns.Count == 0) return null;

        if (randomizePatternOrder)
            return attackPatterns[Random.Range(0, attackPatterns.Count)];

        BulletPatternSO next = attackPatterns[patternIndex];
        patternIndex = (patternIndex + 1) % attackPatterns.Count;
        return next;
    }

    // ---------------------------------------------------------------
    // Firing logic (same patterns/behaviour as BossController, kept
    // local here so this script has no dependency on other files
    // beyond BulletPatternSO and Bullet).
    // ---------------------------------------------------------------
    private IEnumerator RunPattern(BulletPatternSO pattern)
    {
        for (int volley = 0; volley < pattern.volleys; volley++)
        {
            switch (pattern.type)
            {
                case BulletPatternSO.PatternType.RadialBurst:
                    FireRadialBurst(pattern, extraRotationOffset: 0f);
                    break;

                case BulletPatternSO.PatternType.Spiral:
                    FireRadialBurst(pattern, extraRotationOffset: volley * pattern.spiralRotationSpeedDeg);
                    break;

                case BulletPatternSO.PatternType.AimedSpread:
                    FireAimedSpread(pattern);
                    break;

                case BulletPatternSO.PatternType.Curtain:
                    FireCurtain(pattern);
                    break;
            }

            yield return new WaitForSeconds(pattern.delayBetweenVolleys);
        }
    }

    private void FireRadialBurst(BulletPatternSO pattern, float extraRotationOffset)
    {
        float angleStep = pattern.spreadAngle / pattern.bulletCount;

        for (int i = 0; i < pattern.bulletCount; i++)
        {
            float angle = extraRotationOffset + i * angleStep;
            SpawnBullet(pattern, DirFromAngle(angle));
        }
    }

    private void FireAimedSpread(BulletPatternSO pattern)
    {
        if (player == null) return;

        Vector2 baseDir = ((Vector2)player.position - (Vector2)firePoint.position).normalized;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
        float angleStep = pattern.bulletCount > 1 ? pattern.spreadAngle / (pattern.bulletCount - 1) : 0f;
        float startAngle = baseAngle - pattern.spreadAngle / 2f;

        for (int i = 0; i < pattern.bulletCount; i++)
        {
            float angle = startAngle + i * angleStep;
            SpawnBullet(pattern, DirFromAngle(angle));
        }
    }

    private void FireCurtain(BulletPatternSO pattern)
    {
        int gapIndex = Random.Range(0, pattern.bulletCount);
        float angleStep = pattern.spreadAngle / pattern.bulletCount;

        for (int i = 0; i < pattern.bulletCount; i++)
        {
            if (i == gapIndex) continue;
            SpawnBullet(pattern, DirFromAngle(i * angleStep));
        }
    }

    private Vector2 DirFromAngle(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    private void SpawnBullet(BulletPatternSO pattern, Vector2 dir)
    {
        if (pattern.bulletPrefab == null || firePoint == null) return;
        GameObject go = Instantiate(pattern.bulletPrefab, firePoint.position, Quaternion.identity);
        Bullet b = go.GetComponent<Bullet>();
        if (b != null) b.Init(dir, pattern.bulletSpeed);
    }
}