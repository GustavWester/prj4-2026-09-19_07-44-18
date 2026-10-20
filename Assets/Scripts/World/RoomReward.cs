using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Sidder på kisten (Chest.prefab). Åbner den når alle fjender i det rum, kisten står i, er døde.
/// Rummet er det RoomVisibility-område (Box Collider 2D), kisten står i, så der skal ikke sættes noget op.
/// ResourceController.onDeath kører hos alle klienter, og despawnede fjender (null) tæller som døde, så det virker også online.
/// </summary>
[RequireComponent(typeof(Animator))]
public class RoomReward : MonoBehaviour
{
    private List<ResourceController> enemies;

    private void Start()
    {
        var room = RoomVisibility.RoomAt(transform.position);
        if (room == null)
        {
            Debug.LogWarning($"{name} står ikke i et rum (RoomVisibility), så den åbner aldrig", this);
            enabled = false;
            return;
        }
        // fjenderne der står i rummet når banen starter
        enemies = FindObjectsByType<EnemyController>()
            .Where(e => room.OverlapPoint(e.transform.position))
            .Select(e => e.GetComponent<ResourceController>())
            .ToList();
    }

    private void Update()
    {
        if (!enemies.All(e => e == null || e.IsDead)) return;
        GetComponent<Animator>().SetTrigger("Open");
        enabled = false; // kun én gang
    }
}
