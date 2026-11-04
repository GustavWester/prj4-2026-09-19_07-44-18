using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ét slot i inventory-griddet. Lav en prefab med denne komponent og træk den
/// ind på InventoryUI.slotPrefab.
/// </summary>
[RequireComponent(typeof(Button))]
public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;

    [Header("Background sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;

    private int index;
    private Action<int> onClicked;

    public void Init(int slotIndex, Action<int> clicked)
    {
        index = slotIndex;
        onClicked = clicked;
        GetComponent<Button>().onClick.AddListener(() => onClicked?.Invoke(index));
    }

    public void Show(Inventory.Slot slot, bool selected)
    {
        bool hasItem = slot != null && !slot.IsEmpty;

        icon.enabled = hasItem && slot.item.icon != null;
        if (hasItem) icon.sprite = slot.item.icon;

        if (countText != null)
            countText.text = hasItem && slot.count > 1 ? slot.count.ToString() : "";

        if (background != null)
            background.sprite = selected ? selectedSprite : normalSprite;
    }
}
