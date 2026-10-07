using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Viser spillerens Inventory som et grid af slots. Klik på et slot for at se
/// itemets navn og beskrivelse nedenunder.
///
/// Setup: sæt scriptet på InventoryPage. 'slotGrid' skal have en Grid Layout Group.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Tooltip("Spilleren hvis inventory vises. Er den tom, findes objektet med tagget 'Player'.")]
    [SerializeField] private Inventory inventory;

    [SerializeField] private Transform slotGrid;
    [SerializeField] private InventorySlotUI slotPrefab;

    [Header("Details")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;

    private readonly List<InventorySlotUI> slotViews = new();
    private int selectedIndex = -1;

    private void Awake()
    {
        if (inventory == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) inventory = player.GetComponent<Inventory>();
        }
    }

    private void Start()
    {
        if (inventory == null)
        {
            Debug.LogWarning("InventoryUI: no Inventory found on the player.", this);
            return;
        }

        BuildSlots();
        inventory.onChanged.AddListener(Refresh);
        Refresh();
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.onChanged.RemoveListener(Refresh);
    }

    private void BuildSlots()
    {
        for (int i = slotGrid.childCount - 1; i >= 0; i--)
            Destroy(slotGrid.GetChild(i).gameObject);

        for (int i = 0; i < inventory.Capacity; i++)
        {
            var view = Instantiate(slotPrefab, slotGrid);
            view.Init(i, Select);
            slotViews.Add(view);
        }
    }

    private void Select(int index)
    {
        // Klik på det valgte slot igen fjerner markeringen.
        selectedIndex = index == selectedIndex ? -1 : index;
        Refresh();
    }

    private void Refresh()
    {
        var slots = inventory.Slots;
        for (int i = 0; i < slotViews.Count; i++)
            slotViews[i].Show(slots[i], i == selectedIndex);

        var selected = selectedIndex >= 0 ? slots[selectedIndex] : null;
        bool hasItem = selected != null && !selected.IsEmpty;

        if (itemNameText != null)
            itemNameText.text = hasItem ? selected.item.itemName : "";
        if (itemDescriptionText != null)
            itemDescriptionText.text = hasItem ? selected.item.description : "";
    }
}
