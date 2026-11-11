using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Smider ét item fra et inventory-slot på jorden ved spilleren (InventoryUI kalder Drop, når man trykker G).
/// Sæt på spiller-prefabben og træk ItemPickup-prefabben ind i 'pickupPrefab'.
///
/// Online: inventory er kun ejerens, så ejeren fjerner itemet og fortæller alle maskiner det.
/// Powerups tages af igen på alle maskiner (de blev også givet på alle i ItemPickup), og kun hosten spawner pickuppen.
/// Stave kan ikke droppes, for det tidligere spell kendes ikke.
/// </summary>
[RequireComponent(typeof(Inventory), typeof(PlayerManager))]
public class ItemDropper : NetworkBehaviour
{
    [SerializeField] private ItemPickup pickupPrefab;

    public void Drop(int slotIndex)
    {
        if (IsSpawned && !IsOwner) return;
        var inventory = GetComponent<Inventory>();
        var slot = inventory.Slots[slotIndex];
        if (slot.IsEmpty) return;

        ItemSO item = slot.item;
        if (item.spell != null)
        {
            FloatingText.Spawn(transform.position, "Can't be dropped", Color.yellow);
            return;
        }

        inventory.RemoveAt(slotIndex);
        // unique giver kun stats én gang, så de tages kun af når den sidste er væk
        bool unequip = item.isPowerup && (!item.unique || inventory.CountOf(item) == 0);

        if (IsSpawned) DropRpc(item.name, unequip);
        else DoDrop(item, unequip); // offline
    }

    [Rpc(SendTo.Everyone)]
    private void DropRpc(FixedString64Bytes itemName, bool unequip) =>
        DoDrop(Resources.Load<ItemSO>("Items/" + itemName), unequip);

    private void DoDrop(ItemSO item, bool unequip)
    {
        if (item == null) return; // ligger ikke i Resources/Items
        if (unequip) GetComponent<PlayerManager>().Unequip(item);
        if (IsSpawned && !IsServer) return; // kun hosten spawner

        var pickup = Instantiate(pickupPrefab, transform.position, Quaternion.identity);
        pickup.SetItem(item);
        if (IsSpawned) pickup.NetworkObject.Spawn(true); // true = forsvinder med scenen
    }
}
