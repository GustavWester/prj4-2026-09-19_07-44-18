using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Spillerens inventory: et fast antal slots, hvor hvert slot holder én slags item (evt. stacket).
/// Sættes på spiller-prefabben. UI (InventoryUI) lytter på onChanged.
/// </summary>
[DisallowMultipleComponent]
public class Inventory : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public ItemSO item;
        public int count;

        public bool IsEmpty => item == null || count <= 0;
    }

    [SerializeField, Min(1)] private int capacity = 24;

    [Tooltip("Items spilleren starter med. Praktisk til at teste UI'et.")]
    [SerializeField] private List<Slot> startingItems = new();

    [Tooltip("Fires whenever the contents change.")]
    public UnityEvent onChanged;

    private Slot[] slots;

    public int Capacity => capacity;
    public IReadOnlyList<Slot> Slots => slots;

    private void Awake()
    {
        slots = new Slot[capacity];
        for (int i = 0; i < capacity; i++)
            slots[i] = new Slot();

        foreach (var start in startingItems)
            if (start.item != null)
                Add(start.item, start.count);
    }

    /// <summary>Lægger items i inventory. Returnerer hvor mange der IKKE var plads til.</summary>
    public int Add(ItemSO item, int amount = 1)
    {
        if (item == null || amount <= 0) return amount;

        // Fyld først eksisterende stacks op, derefter tomme slots.
        foreach (var slot in slots)
        {
            if (amount == 0) break;
            if (slot.item != item || slot.count >= item.maxStack) continue;

            int moved = Mathf.Min(amount, item.maxStack - slot.count);
            slot.count += moved;
            amount -= moved;
        }

        foreach (var slot in slots)
        {
            if (amount == 0) break;
            if (!slot.IsEmpty) continue;

            int moved = Mathf.Min(amount, item.maxStack);
            slot.item = item;
            slot.count = moved;
            amount -= moved;
        }

        onChanged?.Invoke();
        return amount;
    }

    /// <summary>Fjerner op til 'amount' fra et bestemt slot.</summary>
    public void RemoveAt(int index, int amount = 1)
    {
        if (index < 0 || index >= slots.Length || slots[index].IsEmpty || amount <= 0) return;

        var slot = slots[index];
        slot.count -= amount;
        if (slot.count <= 0)
        {
            slot.item = null;
            slot.count = 0;
        }

        onChanged?.Invoke();
    }
}
