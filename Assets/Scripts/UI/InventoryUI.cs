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
    [Tooltip("Inventory der vises. Er den tom, følges LocalPlayer (min egen wizard, også online).")]
    [SerializeField] private Inventory inventory;

    [SerializeField] private Transform slotGrid;
    [SerializeField] private InventorySlotUI slotPrefab;

    [Header("Details")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;

    private readonly List<InventorySlotUI> slotViews = new();
    private int selectedIndex = -1;
    private bool followLocalPlayer;

    private void Start()
    {
        // Online spawner wizarden først efter scenen er startet, så vi lytter efter den.
        followLocalPlayer = inventory == null;
        if (followLocalPlayer)
        {
            LocalPlayer.Changed += OnLocalPlayerChanged;
            OnLocalPlayerChanged(LocalPlayer.Current);
        }
        else
        {
            SetInventory(inventory);
        }
    }

    private void OnDestroy()
    {
        if (followLocalPlayer) LocalPlayer.Changed -= OnLocalPlayerChanged;
        if (inventory != null) inventory.onChanged.RemoveListener(Refresh);
    }

    private void OnLocalPlayerChanged(GameObject player)
    {
        SetInventory(player != null ? player.GetComponent<Inventory>() : null);
    }

    public void SetInventory(Inventory newInventory)
    {
        if (inventory != null) inventory.onChanged.RemoveListener(Refresh);

        inventory = newInventory;
        selectedIndex = -1;
        BuildSlots();

        if (inventory != null) inventory.onChanged.AddListener(Refresh);
        Refresh();
    }

    private void BuildSlots()
    {
        slotViews.Clear();
        for (int i = slotGrid.childCount - 1; i >= 0; i--)
            Destroy(slotGrid.GetChild(i).gameObject);

        if (inventory == null) return;

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
        var slots = inventory != null ? inventory.Slots : null;
        for (int i = 0; i < slotViews.Count; i++)
            slotViews[i].Show(slots[i], i == selectedIndex);

        var selected = slots != null && selectedIndex >= 0 ? slots[selectedIndex] : null;
        bool hasItem = selected != null && !selected.IsEmpty;

        if (itemNameText != null)
            itemNameText.text = hasItem ? selected.item.itemName : "";
        if (itemDescriptionText != null)
            itemDescriptionText.text = hasItem ? selected.item.description : "";
    }
}
