using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top-down bullet-hell boss with a 3-stage fight.
/// Stage is driven purely by HP thresholds, each stage has its own
/// list of BulletPatternSO's to pick from, and stage 2+ unlocks
/// a telegraphed charge attack.
///
/// Setup:
/// - Add Rigidbody2D (Dynamic, gravity scale 0) + Collider2D (isTrigger) + this script.
/// - Assign waypoints (empty GameObjects) for roaming.
/// - Assign a firePoint (empty child transform, usually boss center).
/// - Fill in stage1/2/3 pattern lists and a bullet prefab per pattern.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BossController : MonoBehaviour
{
    public enum BossState { Roaming, Attacking, Telegraphing, Charging, Staggered, PhaseTransition, Dead }
    public enum Phase { Stage1 = 1, Stage2 = 2, Stage3 = 3 }

    [Header("Health")]
    public float maxHP = 300f;
    private float currentHP;
    [Range(0, 1f)] public float stage2Threshold = 0.66f;
    [Range(0, 1f)] public float stage3Threshold = 0.33f;

    [Header("References")]
    public Transform player;
    public Transform firePoint;
    public Transform[] roamWaypoints;

    [Header("Movement")]
    public float roamSpeed = 2.5f;
    public float chargeSpeed = 14f;
    public float chargeDuration = 0.5f;
    public float waypointReachedDistance = 0.3f;

    [Header("Timing")]
    public float telegraphDuration = 0.5f;
    public float staggerDuration = 1.2f;
    public float phaseTransitionDuration = 1.5f;
    public float timeBetweenActions = 1f;

    [Header("Attack patterns per stage")]
    public List<BulletPatternSO> stage1Patterns;
    public List<BulletPatternSO> stage2Patterns;
    public List<BulletPatternSO> stage3Patterns;
    [Tooltip("Chance (0-1) that stage 2/3 picks a Charge instead of a bullet pattern.")]
    [Range(0, 1f)] public float chargeChance = 0.35f;

    [Header("Events (hook up VFX/SFX/animator here)")]
    public UnityEngine.Events.UnityEvent onTelegraph;
    public UnityEngine.Events.UnityEvent onPhaseTransitionStart;
    public UnityEngine.Events.UnityEvent onPhaseTransitionEnd;
    public UnityEngine.Events.UnityEvent onDeath;

    private Rigidbody2D rb;
    private BossState currentState;
    private Phase currentPhase = Phase.Stage1;
    private bool isInvulnerable;
    private int currentWaypointIndex;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHP = maxHP;
    }

    private void Start()
    {
        StartCoroutine(BossLoop());
    }

    // ---------------------------------------------------------------
    // Main loop: alternates between roaming and picking an action,
    // for as long as the boss is alive.
    // ---------------------------------------------------------------
    private IEnumerator BossLoop()
    {
        while (currentState != BossState.Dead)
        {
            yield return StartCoroutine(RoamForDuration(timeBetweenActions));

            if (currentState == BossState.Dead) yield break;

            yield return StartCoroutine(PickAndRunAction());
        }
    }

    private IEnumerator RoamForDuration(float duration)
    {
        currentState = BossState.Roaming;
        float t = 0f;
        while (t < duration)
        {
            MoveTowardsNextWaypoint();
            t += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = Vector2.zero;
    }

    private void MoveTowardsNextWaypoint()
    {
        if (roamWaypoints == null || roamWaypoints.Length == 0) return;

        Transform target = roamWaypoints[currentWaypointIndex];
        Vector2 dir = (target.position - transform.position);

        if (dir.magnitude <= waypointReachedDistance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % roamWaypoints.Length;
            return;
        }

        rb.linearVelocity = dir.normalized * roamSpeed;
    }

    // ---------------------------------------------------------------
    // Decide what to do next based on current phase.
    // ---------------------------------------------------------------
    private IEnumerator PickAndRunAction()
    {
        bool canCharge = currentPhase != Phase.Stage1;

        if (canCharge && Random.value < chargeChance)
        {
            yield return StartCoroutine(ChargeAttack());
        }
        else
        {
            BulletPatternSO pattern = PickRandomPattern();
            if (pattern != null)
                yield return StartCoroutine(RunBulletPattern(pattern));
        }
    }

    private BulletPatternSO PickRandomPattern()
    {
        List<BulletPatternSO> pool = currentPhase switch
        {
            Phase.Stage1 => stage1Patterns,
            Phase.Stage2 => stage2Patterns,
            Phase.Stage3 => stage3Patterns,
            _ => stage1Patterns
        };

        if (pool == null || pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }

    // ---------------------------------------------------------------
    // Charge attack: telegraph -> lock in direction -> dash -> stagger.
    // ---------------------------------------------------------------
    private IEnumerator ChargeAttack()
    {
        currentState = BossState.Telegraphing;
        onTelegraph?.Invoke();

        Vector2 chargeDir = ((Vector2)player.position - (Vector2)transform.position).normalized;

        yield return new WaitForSeconds(telegraphDuration);

        if (currentState == BossState.Dead) yield break;

        currentState = BossState.Charging;
        float t = 0f;
        while (t < chargeDuration)
        {
            rb.linearVelocity = chargeDir * chargeSpeed;
            t += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = Vector2.zero;

        currentState = BossState.Staggered;
        // Vulnerability window: hook up e.g. 2x damage multiplier here via a flag.
        yield return new WaitForSeconds(staggerDuration);
    }

    // ---------------------------------------------------------------
    // Bullet pattern execution.
    // ---------------------------------------------------------------
    private IEnumerator RunBulletPattern(BulletPatternSO pattern)
    {
        currentState = BossState.Attacking;

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
        float startAngle = extraRotationOffset;

        for (int i = 0; i < pattern.bulletCount; i++)
        {
            float angle = startAngle + i * angleStep;
            Vector2 dir = DirFromAngle(angle);
            SpawnBullet(pattern.bulletPrefab, dir, pattern.bulletSpeed);
        }
    }

    private void FireAimedSpread(BulletPatternSO pattern)
    {
        Vector2 baseDir = ((Vector2)player.position - (Vector2)firePoint.position).normalized;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
        float angleStep = pattern.bulletCount > 1 ? pattern.spreadAngle / (pattern.bulletCount - 1) : 0f;
        float startAngle = baseAngle - pattern.spreadAngle / 2f;

        for (int i = 0; i < pattern.bulletCount; i++)
        {
            float angle = startAngle + i * angleStep;
            Vector2 dir = DirFromAngle(angle);
            SpawnBullet(pattern.bulletPrefab, dir, pattern.bulletSpeed);
        }
    }

    private void FireCurtain(BulletPatternSO pattern)
    {
        // Screen-filling wall of bullets with one safe gap, to force positioning
        // rather than pure dodging. gapIndex picked randomly each call.
        int gapIndex = Random.Range(0, pattern.bulletCount);
        float angleStep = pattern.spreadAngle / pattern.bulletCount;

        for (int i = 0; i < pattern.bulletCount; i++)
        {
            if (i == gapIndex) continue;
            float angle = i * angleStep;
            Vector2 dir = DirFromAngle(angle);
            SpawnBullet(pattern.bulletPrefab, dir, pattern.bulletSpeed);
        }
    }

    private Vector2 DirFromAngle(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    private void SpawnBullet(GameObject prefab, Vector2 dir, float speed)
    {
        if (prefab == null || firePoint == null) return;
        GameObject go = Instantiate(prefab, firePoint.position, Quaternion.identity);
        Bullet b = go.GetComponent<Bullet>();
        if (b != null) b.Init(dir, speed);
    }

    // ---------------------------------------------------------------
    // Damage + phase transitions.
    // ---------------------------------------------------------------
    public void TakeDamage(float amount)
    {
        if (isInvulnerable || currentState == BossState.Dead) return;

        currentHP -= amount;

        if (currentHP <= 0f)
        {
            StopAllCoroutines();
            currentState = BossState.Dead;
            rb.linearVelocity = Vector2.zero;
            onDeath?.Invoke();
            return;
        }

        float hpPercent = currentHP / maxHP;

        if (currentPhase == Phase.Stage1 && hpPercent <= stage2Threshold)
        {
            StopAllCoroutines();
            StartCoroutine(TransitionToPhase(Phase.Stage2));
        }
        else if (currentPhase == Phase.Stage2 && hpPercent <= stage3Threshold)
        {
            StopAllCoroutines();
            StartCoroutine(TransitionToPhase(Phase.Stage3));
        }
    }

    private IEnumerator TransitionToPhase(Phase next)
    {
        currentState = BossState.PhaseTransition;
        isInvulnerable = true;
        rb.linearVelocity = Vector2.zero;

        onPhaseTransitionStart?.Invoke();
        yield return new WaitForSeconds(phaseTransitionDuration);

        currentPhase = next;
        isInvulnerable = false;
        onPhaseTransitionEnd?.Invoke();

        StartCoroutine(BossLoop());
    }
}
