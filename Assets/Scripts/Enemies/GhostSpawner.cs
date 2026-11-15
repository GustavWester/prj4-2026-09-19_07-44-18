using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Boss-spøgelset: kalder små spøgelser frem, mens der står en spiller i boss-rummet.
/// Sidder sammen med ResourceController på bossen. Kører kun på hosten (eller offline), ligesom fjendernes AI.
/// Når bossen dør, dør dens spøgelser også.
/// </summary>
[RequireComponent(typeof(ResourceController))]
public class GhostSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject minion; // Ghost.prefab, skal ligge i DefaultNetworkPrefabs
    [SerializeField] private float interval = 6f;
    [SerializeField] private int maxAlive = 4;
    [SerializeField] private float spawnRadius = 2f;

    [Header("Blå fireballs")]
    [Tooltip("Mønstret der skydes. Skal også ligge i EnemyController.attackPatterns på bossen (sendes som nummer over netværket).")]
    [SerializeField] private BulletPatternSO fireballs;
    [SerializeField] private float fireInterval = 3f;

    private readonly List<ResourceController> alive = new();
    private ResourceController health;
    private Collider2D room;
    private bool RunsAI => !IsSpawned || IsServer;

    private void Awake()
    {
        health = GetComponent<ResourceController>();
        health.onDeath.AddListener(KillMinions);
    }

    private void Start()
    {
        room = RoomVisibility.RoomAt(transform.position);
        StartCoroutine(SpawnLoop());
        if (fireballs != null) StartCoroutine(FireLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (!health.IsDead)
        {
            // vent et interval, fjern døde spøgelser fra alive-listen, tjek om vi må spawne et nyt spøgelse
            yield return new WaitForSeconds(interval);
            alive.RemoveAll(m => m == null || m.IsDead);
            if (!RunsAI || health.IsDead || alive.Count >= maxAlive || !PlayerInRoom()) continue;
        // spawn et nyt spøgelse et tilfældigt sted i en cirkel omkring bossen
            Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle.normalized * spawnRadius;
            var ghost = Instantiate(minion, pos, Quaternion.identity);
            if (IsSpawned) ghost.GetComponent<NetworkObject>().Spawn(true); // true = forsvinder med scenen
            alive.Add(ghost.GetComponent<ResourceController>());
        }
    }

    // skyder fireballs mod spilleren, mens der står en spiller i boss-rummet (ved siden af melee og spøgelserne)
    private IEnumerator FireLoop()
    {
        var enemy = GetComponent<EnemyController>();
        while (!health.IsDead)
        {
            yield return new WaitForSeconds(fireInterval);
            if (RunsAI && !health.IsDead && PlayerInRoom()) enemy.FirePattern(fireballs);
        }
    }

    private bool PlayerInRoom()
    {
        foreach (var p in GameObject.FindGameObjectsWithTag("Player"))
            if (!p.GetComponent<ResourceController>().IsDead && (room == null || room.OverlapPoint(p.transform.position))) return true;
        return false;
    }
//draeb alle spoegelser, hvis bossen doer
    private void KillMinions()
    {
        if (!RunsAI) return;
        foreach (var m in alive)
            if (m != null) m.TakeDamage(int.MaxValue);
    }
}
