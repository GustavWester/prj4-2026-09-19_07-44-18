using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Knight-bossen i level 3. Sidder ved siden af EnemyController (som klarer jagt og sværdslag)
/// og lægger tre faser oven på, styret af bossens liv:
///   Fase 1 (100-66 %): sværdslag + udfald (lader op, blinker og stormer frem mod spilleren).
///   Fase 2 (66-33 %):  + skrå "slash"-kugler og en ring af kugler efter hvert udfald. Orange.
///   Fase 3 (under 33 %): raseri - rød, hurtigere, kortere cooldown, dobbelt udfald, spiral og ring.
/// Ved hvert faseskift står bossen uskadelig et øjeblik og kalder knights.
///
/// AI'en kører kun på hosten (eller offline), ligesom GhostSpawner. Farven regnes ud fra livet,
/// som alle maskiner kender, så den kræver intet ekstra netværk. Kun blinket sendes som RPC.
/// </summary>
[RequireComponent(typeof(EnemyController), typeof(ResourceController))]
public class KnightBossPhases : NetworkBehaviour
{
    [Header("Faser (liv i procent)")]
    [SerializeField, Range(0f, 1f)] private float phase2At = 0.66f;
    [SerializeField, Range(0f, 1f)] private float phase3At = 0.33f;

    [Header("Udfald")]
    [SerializeField] private float lungeInterval = 4f;
    [SerializeField] private float lungeIntervalEnraged = 2.5f;
    [SerializeField] private float lungeWindup = 0.4f;
    [SerializeField] private float lungeDuration = 0.35f;
    [SerializeField] private float lungeSpeed = 9f;
    [SerializeField] private int lungeDamage = 2;
    [SerializeField] private float lungeHitRadius = 1.2f;

    [Header("Kugler (skal også ligge i EnemyController.attackPatterns)")]
    [SerializeField] private BulletPatternSO slash;
    [SerializeField] private BulletPatternSO ring;
    [SerializeField] private BulletPatternSO spiral;
    [SerializeField] private float slashInterval = 3f;
    [SerializeField] private float enragedPatternInterval = 2.5f;

    [Header("Faseskift")]
    [SerializeField] private float transitionTime = 1.5f;
    [SerializeField] private GameObject minion; // Knight.prefab, skal ligge i DefaultNetworkPrefabs
    [SerializeField] private int minionsPerTransition = 2;
    [SerializeField] private float spawnRadius = 3f;

    [Header("Raseri (fase 3)")]
    [SerializeField] private float enragedSpeed = 1.4f;
    [SerializeField] private float enragedCooldown = 0.6f;

    [Header("Farver")]
    [SerializeField] private Color phase2Tint = new(1f, 0.75f, 0.45f);
    [SerializeField] private Color phase3Tint = new(1f, 0.4f, 0.4f);

    private EnemyController enemy;
    private ResourceController health;
    private Rigidbody2D rb;
    private Animator animator;
    private NetworkAnimator netAnimator;
    private SpriteRenderer sr;
    private readonly List<ResourceController> minions = new();
    private float blinkUntil;

    private bool RunsAI => !IsSpawned || IsServer;
    private int Phase => health.HealthPercent > phase2At ? 1 : health.HealthPercent > phase3At ? 2 : 3;

    private void Awake()
    {
        enemy = GetComponent<EnemyController>();
        health = GetComponent<ResourceController>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        netAnimator = GetComponent<NetworkAnimator>();
        sr = GetComponent<SpriteRenderer>();
        health.onDeath.AddListener(OnDeath);
    }

    // kører på alle maskiner, men gør kun noget hos hosten (scene-placerede objekter kan starte før de er spawnet)
    private void Start() => StartCoroutine(BossLoop());

    // ---------------------------------------------------------------
    // Hosten: fasernes angreb
    // ---------------------------------------------------------------
    private IEnumerator BossLoop()
    {
        int reached = 1;
        float lungeTimer = lungeInterval, patternTimer = slashInterval;
        bool spiralNext = true;

        while (!health.IsDead)
        {
            if (!RunsAI || enemy.Target == null) { yield return null; continue; } // ikke hosten, eller ingen spiller i boss-rummet endnu

            if (Phase > reached)
            {
                reached = Phase;
                yield return Transition(reached);
                continue;
            }

            lungeTimer -= Time.deltaTime;
            patternTimer -= Time.deltaTime;

            if (lungeTimer <= 0f)
            {
                yield return Lunge();
                if (reached == 3) yield return Lunge(); // raseri: to i træk
                if (reached >= 2) enemy.FirePattern(ring);
                lungeTimer = reached == 3 ? lungeIntervalEnraged : lungeInterval;
            }
            else if (reached >= 2 && patternTimer <= 0f)
            {
                if (reached == 2) enemy.FirePattern(slash);
                else { enemy.FirePattern(spiralNext ? spiral : ring); spiralNext = !spiralNext; }
                patternTimer = reached == 3 ? enragedPatternInterval : slashInterval;
            }
            yield return null;
        }
    }

    private IEnumerator Lunge()
    {
        Transform target = enemy.Target;
        if (target == null || health.IsDead) yield break;

        // stå stille og lad op, så spillerne kan nå at se det komme
        enemy.enabled = false; // ellers skriver EnemyController velocity hver physics-frame
        rb.linearVelocity = Vector2.zero;
        SetTrigger("Stab");
        Blink(lungeWindup);
        yield return new WaitForSeconds(lungeWindup);

        Vector2 dir = ((Vector2)target.position - rb.position).normalized;
        var hit = new HashSet<ResourceController>(); // hver spiller rammes højst én gang pr. udfald
        float end = Time.time + lungeDuration;
        while (Time.time < end && !health.IsDead)
        {
            rb.linearVelocity = dir * lungeSpeed;
            foreach (var c in Physics2D.OverlapCircleAll(rb.position, lungeHitRadius))
                if (c.CompareTag("Player") && c.TryGetComponent(out ResourceController p) && hit.Add(p))
                    p.TakeDamage(lungeDamage, enemy.DisplayName);
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;
        if (!health.IsDead) enemy.enabled = true;
    }

    private IEnumerator Transition(int phase)
    {
        health.IsInvulnerable = true;
        enemy.enabled = false;
        rb.linearVelocity = Vector2.zero;
        Blink(transitionTime);

        yield return new WaitForSeconds(transitionTime * 0.5f);
        SpawnMinions();
        if (phase == 3)
        {
            enemy.speedMultiplier = enragedSpeed;
            enemy.cooldownMultiplier = enragedCooldown;
        }
        yield return new WaitForSeconds(transitionTime * 0.5f);

        health.IsInvulnerable = false;
        if (!health.IsDead) enemy.enabled = true;
    }

    private void SpawnMinions()
    {
        if (minion == null) return;
        minions.RemoveAll(m => m == null || m.IsDead);
        for (int i = 0; i < minionsPerTransition; i++)
        {
            Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle.normalized * spawnRadius;
            var knight = Instantiate(minion, pos, Quaternion.identity);
            if (IsSpawned) knight.GetComponent<NetworkObject>().Spawn(true); // true = forsvinder med scenen
            minions.Add(knight.GetComponent<ResourceController>());
        }
    }

    private void OnDeath()
    {
        if (!RunsAI) return;
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
        foreach (var m in minions)
            if (m != null) m.TakeDamage(int.MaxValue);
    }

    private void SetTrigger(string trigger)
    {
        if (IsSpawned) netAnimator.SetTrigger(trigger);
        else if (animator != null) animator.SetTrigger(trigger);
    }

    // ---------------------------------------------------------------
    // Alle maskiner: farve efter fase og blink
    // ---------------------------------------------------------------
    private void Blink(float duration)
    {
        if (IsSpawned) BlinkRpc(duration);
        else blinkUntil = Time.time + duration; // offline
    }

    [Rpc(SendTo.Everyone)]
    private void BlinkRpc(float duration) => blinkUntil = Time.time + duration;

    private void LateUpdate()
    {
        if (health.IsDead) return;
        int phase = Phase;
        Color tint = phase == 1 ? Color.white : phase == 2 ? phase2Tint : phase3Tint;
        bool blinkOff = Time.time < blinkUntil && Mathf.Repeat(Time.time * 12f, 1f) < 0.5f;
        sr.color = blinkOff ? new Color(tint.r, tint.g, tint.b, 0.35f) : tint;
    }
}
