
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Smider items på jorden. Kiste: RoomReward kalder Drop() når den åbner.
/// Fjende: kobl ResourceController.onDeath -> LootDrop.Drop i inspectoren.
/// Kører kun på hosten (eller offline), ligesom fjendernes AI.
/// </summary>
public class LootDrop : MonoBehaviour
{
    [SerializeField] private ItemPickup pickupPrefab;
    [Tooltip("Der trækkes tilfældigt herfra. Læg et item ind flere gange for at gøre det mere almindeligt.")]
    [SerializeField] private ItemSO[] items;
    [SerializeField, Range(0f, 1f)] private float chance = 1f;
    [SerializeField, Min(1)] private int count = 1;
    [Tooltip("Bosser: ét item pr. spiller online, så ingen går tomhændet.")]
    [SerializeField] private bool onePerPlayer;
    [SerializeField] private float spread = 0.5f; // så items ikke ligger oven i hinanden

    public void Drop()
    {
        var nm = NetworkManager.Singleton;
        bool online = nm != null && nm.IsListening;
        if (online && !nm.IsServer) return; // onDeath kører hos alle klienter, kun hosten spawner
        if (items.Length == 0 || Random.value > chance) return;

        int n = onePerPlayer &entsIds.Count : count;
        for (int i = 0; i < n; i++)                                                        {
            Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle *  spread;
            var pickup = Instantiate(pickupPrefab, pos, Quaternion.identity);                  pickup.SetItem(iteength)]);
            if (online) pickup.NetworkObject.Spawn(true); // true = forsvinder med scenen
        }
    }
}