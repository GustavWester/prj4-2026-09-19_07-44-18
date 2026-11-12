using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Smider ét item fra et inventory-slot på jorden ved spilleren (InventoryUI kalder Drop, når man trykker G).
/// Sæt på spiller-prefabben og træk ItemPickup-prefabben ind i 'pickupPrefab'.
///
/// Online: inventory er kun ejerens, så ejeren fjerner itemet og fortæller alle maskiner det.
/// Powerups tages af igen på alle maskiner (de blev også givet på alle i ItemPickup), og kun hosten spawner pickuppen.
/// Droppes den aktive stav, vælger ejeren den nye spell (en anden stav i inventory, ellers fireball) og sender den med.
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
        inventory.RemoveAt(slotIndex);
        // unique giver kun stats én gang, så de tages kun af når den sidste er væk
        bool unequip = item.isPowerup && (!item.unique || inventory.CountOf(item) == 0);

        // kun inventory'ets ejer ved hvilke andre stave der er, så det afgøres her
        bool swap = item.spell != null && item.spell == GetComponent<PlayerAttackController>().Spell && inventory.CountOf(item) == 0;
        ItemSO next = swap ? inventory.Slots.FirstOrDefault(s => !s.IsEmpty && s.item.spell != null)?.item : null;

        if (IsSpawned) DropRpc(item.name, unequip, swap, next != null ? next.name : "");
        else DoDrop(item, unequip, swap, next); // offline
    }

    [Rpc(SendTo.Everyone)]
    private void DropRpc(FixedString64Bytes itemName, bool unequip, bool swap, FixedString64Bytes nextStaff) =>
        DoDrop(Resources.Load<ItemSO>("Items/" + itemName), unequip, swap,
            nextStaff.IsEmpty ? null : Resources.Load<ItemSO>("Items/" + nextStaff));

    private void DoDrop(ItemSO item, bool unequip, bool swap, ItemSO next)
    {
        if (item == null) return; // ligger ikke i Resources/Items
        if (unequip) GetComponent<PlayerManager>().Unequip(item);
        if (swap) GetComponent<PlayerAttackController>().SetSpell(next != null ? next.spell : null); // null = fireball
        if (IsSpawned && !IsServer) return; // kun hosten spawner

        var pickup = Instantiate(pickupPrefab, transform.position, Quaternion.identity);
        pickup.SetItem(item);
        if (IsSpawned) pickup.NetworkObject.Spawn(true); // true = forsvinder med scenen
    }
}
