using System.Collections.Generic;

/// <summary>Ét slot der kan holde en stak af én slags item. Implementeres af Inventory.Slot.</summary>
public interface IStackSlot<T> where T : class
{
    T Item { get; set; }
    int Count { get; set; }
}

/// <summary>
/// Reglerne for hvordan items lægges i og tages ud af en række slots.
/// Ren C# (ingen UnityEngine), så den kan unit-testes uden Unity.
/// </summary>
public static class InventoryStacking
{
    public static bool IsEmpty<T>(IStackSlot<T> slot) where T : class =>
        slot.Item == null || slot.Count <= 0;

    /// <summary>
    /// Lægger 'amount' af 'item' i slots. Fylder først eksisterende stakke op, derefter tomme slots.
    /// Returnerer hvor mange der IKKE var plads til.
    /// </summary>
    public static int Add<T>(IReadOnlyList<IStackSlot<T>> slots, T item, int amount, int maxStack) where T : class
    {
        if (item == null || amount <= 0) return amount;
        if (maxStack < 1) maxStack = 1;

        foreach (var slot in slots)
        {
            if (amount == 0) break;
            if (!ReferenceEquals(slot.Item, item) || slot.Count >= maxStack) continue;

            int moved = System.Math.Min(amount, maxStack - slot.Count);
            slot.Count += moved;
            amount -= moved;
        }

        foreach (var slot in slots)
        {
            if (amount == 0) break;
            if (!IsEmpty(slot)) continue;

            int moved = System.Math.Min(amount, maxStack);
            slot.Item = item;
            slot.Count = moved;
            amount -= moved;
        }

        return amount;
    }

    /// <summary>Fjerner op til 'amount' fra slot nr. 'index'. Returnerer true hvis noget blev fjernet.</summary>
    public static bool RemoveAt<T>(IReadOnlyList<IStackSlot<T>> slots, int index, int amount) where T : class
    {
        if (index < 0 || index >= slots.Count || IsEmpty(slots[index]) || amount <= 0) return false;

        var slot = slots[index];
        slot.Count -= amount;
        if (slot.Count <= 0)
        {
            slot.Item = null;
            slot.Count = 0;
        }
        return true;
    }

    /// <summary>Hvor mange af 'item' der ligger i alt på tværs af slots.</summary>
    public static int CountOf<T>(IReadOnlyList<IStackSlot<T>> slots, T item) where T : class
    {
        int total = 0;
        foreach (var slot in slots)
            if (!IsEmpty(slot) && ReferenceEquals(slot.Item, item))
                total += slot.Count;
        return total;
    }
}
