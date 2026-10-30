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
    public class Slot : IStackSlot<ItemSO>
    {
        public ItemSO item;
        public int count;

        public bool IsEmpty => item == null || count <= 0;

        ItemSO IStackSlot<ItemSO>.Item { get => item; set => item = value; }
        int IStackSlot<ItemSO>.Count { get => count; set => count = value; }
    }

    [SerializeField, Min(1)] private int capacity = 24;

    [Tooltip("Items spilleren starter med. Praktisk til at teste UI'et.")]
    [SerializeField] private List<Slot> startingItems = new();

    [Tooltip("Fires whenever the contents change.")]
    public UnityEvent onChanged = new();

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

        // Fyld først eksisterende stacks op, derefter tomme slots (reglerne ligger i InventoryStacking).
        int leftover = InventoryStacking.Add(slots, item, amount, item.maxStack);

        onChanged?.Invoke();
        return leftover;
    }

    /// <summary>Hvor mange af 'item' der ligger i alt på tværs af alle slots.</summary>
    public int CountOf(ItemSO item) => InventoryStacking.CountOf(slots, item);

    /// <summary>Fjerner op til 'amount' fra et bestemt slot.</summary>
    public void RemoveAt(int index, int amount = 1)
    {
        if (!InventoryStacking.RemoveAt(slots, index, amount)) return;

        onChanged?.Invoke();
    }

    /// <summary>Fjerner én af 'item' fra det første slot der har den. False hvis der ingen er.</summary>
    public bool TryRemove(ItemSO item)
    {
        for (int i = 0; i < slots.Length; i++)
            if (!slots[i].IsEmpty && slots[i].item == item) { RemoveAt(i); return true; }
        return false;
    }
}
