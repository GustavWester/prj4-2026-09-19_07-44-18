using UnityEngine;

/// <summary>
/// Data for én slags item. Opret via Create > Scriptable Objects > Item.
/// </summary>
[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class ItemSO : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    [TextArea(2, 4)] public string description;

    [Tooltip("Hvor mange der kan ligge i ét slot. 1 = kan ikke stackes.")]
    [Min(1)] public int maxStack = 1;
}
