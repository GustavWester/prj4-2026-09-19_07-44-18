using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Isaac-agtigt minimap i hjørnet med spilleren altid i midten. M skifter til et stort kort.
/// Rummet man står i er lyst, besøgte rum er grå, og naboerne er mørkegrå indtil man går ind.
/// De andre spillere vises som blå prikker (deres position er allerede synket af NetworkTransform).
/// Ikon: RoomVisibility.minimapIcon, ellers en kiste (Resources/MinimapIcons) hvis rummet har en RoomReward.
/// Tegnes som UI, så Light 2D ikke gør det mørkt. Lokalt: online ser hver spiller kun sine egne rum.
/// Sæt på et UI-panel med RectMask2D; Content er et tomt barn, og en prik efter Content markerer spilleren.
/// </summary>
public class MinimapUI : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private float pixelsPerUnit = 4f;

    [Header("Udseende")]
    [Tooltip("Valgfri. Unitys indbyggede 'UISprite' giver runde hjørner. Tom = skarpe firkanter.")]
    [SerializeField] private Sprite roomSprite;
    [SerializeField] private Color roomColor = new(0.55f, 0.55f, 0.55f), currentRoomColor = new(0.9f, 0.9f, 0.9f),
        unexploredRoomColor = new(0.3f, 0.3f, 0.3f), borderColor = Color.black;
    [SerializeField] private float borderSize = 6f, iconSize = 16f, playerDotSize = 6f; // px
    [SerializeField] private Color otherPlayerColor = new(0.3f, 0.6f, 1f);
    [Tooltip("Hvor langt (world-enheder) der må være mellem to rum, før de ikke længere tæller som naboer. Væggene er ca. 1 felt.")]
    [SerializeField] private float neighbourGap = 1.5f;

    [Header("Stort kort")]
    [SerializeField] private Key toggleKey = Key.M;
    [SerializeField] private float bigMargin = 60f; // afstand til skærmkanten når kortet er stort

    private static MinimapUI instance;
    private readonly Dictionary<RoomVisibility, Image> rooms = new();
    private readonly Dictionary<GameObject, Image> otherPlayers = new();
    private Image currentRoom;
    private Transform borders; // første barn af Content, så alle kanter ligger under alle rum og smelter sammen til én ramme
    private Sprite chestIcon;
    private RectTransform rect;
    private Vector2[] small; // hjørne-placeringen fra editoren: anchorMin, anchorMax, offsetMin, offsetMax
    private bool big;

    private void Awake()
    {
        instance = this;
        rect = (RectTransform)transform;
        small = new[] { rect.anchorMin, rect.anchorMax, rect.offsetMin, rect.offsetMax };
        borders = new GameObject("Borders", typeof(RectTransform)).transform;
        borders.SetParent(content, false);
        chestIcon = Resources.Load<Sprite>("MinimapIcons/Chest");
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current[toggleKey].wasPressedThisFrame) return;
        big = !big;
        rect.anchorMin = big ? Vector2.zero : small[0];
        rect.anchorMax = big ? Vector2.one : small[1];
        rect.offsetMin = big ? Vector2.one * bigMargin : small[2];
        rect.offsetMax = big ? -Vector2.one * bigMargin : small[3];
    }

    private void LateUpdate()
    {
        // flyt kortet modsat spilleren, så spilleren står i midten
        if (LocalPlayer.Current != null)
            content.anchoredPosition = -(Vector2)LocalPlayer.Current.transform.position * pixelsPerUnit;

        foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (player == LocalPlayer.Current) continue; // min egen er PlayerDot i midten
            if (!otherPlayers.TryGetValue(player, out var dot))
                dot = otherPlayers[player] = NewImage(content, Vector2.zero, Vector2.one * playerDotSize, otherPlayerColor, null);
            dot.rectTransform.anchoredPosition = (Vector2)player.transform.position * pixelsPerUnit;
            dot.rectTransform.SetAsLastSibling(); // over rum der er tegnet efter prikken
        }
        // spillere der har forladt spillet
        foreach (var gone in otherPlayers.Keys.Where(p => p == null).ToList())
        {
            Destroy(otherPlayers[gone].gameObject);
            otherPlayers.Remove(gone);
        }
    }

    /// <summary>Kaldes af RoomVisibility når min spiller går ind i rummet.</summary>
    public static void Enter(RoomVisibility room)
    {
        if (instance != null) instance.EnterRoom(room); // scenen har måske ingen minimap
    }

    private void EnterRoom(RoomVisibility room)
    {
        // rummet selv og naboerne tegnes første gang de ses (rummet rammer altid sig selv)
        Bounds near = room.GetComponent<Collider2D>().bounds;
        near.Expand(neighbourGap * 2);
        foreach (var other in FindObjectsByType<RoomVisibility>())
            if (!rooms.ContainsKey(other) && near.Intersects(other.GetComponent<Collider2D>().bounds))
                rooms[other] = Draw(other);

        if (currentRoom != null) currentRoom.color = roomColor; // det forrige rum er altid besøgt
        currentRoom = rooms[room];
        currentRoom.color = currentRoomColor;
    }

    private Image Draw(RoomVisibility room)
    {
        var area = room.GetComponent<Collider2D>();
        Vector2 pos = area.bounds.center * pixelsPerUnit, size = area.bounds.size * pixelsPerUnit;
        NewImage(borders, pos, size + Vector2.one * borderSize * 2, borderColor, roomSprite);
        var fill = NewImage(content, pos, size, unexploredRoomColor, roomSprite);
        var icon = room.MinimapIcon != null ? room.MinimapIcon
            : FindObjectsByType<RoomReward>().Any(c => area.OverlapPoint(c.transform.position)) ? chestIcon : null;
        if (icon != null) NewImage(fill.transform, Vector2.zero, Vector2.one * iconSize, Color.white, icon);
        return fill;
    }

    private Image NewImage(Transform parent, Vector2 pos, Vector2 size, Color color, Sprite sprite)
    {
        var image = new GameObject("Room", typeof(Image)).GetComponent<Image>();
        image.rectTransform.SetParent(parent, false);
        image.rectTransform.anchoredPosition = pos;
        image.rectTransform.sizeDelta = size;
        image.color = color;
        image.sprite = sprite;
        image.type = sprite == roomSprite ? Image.Type.Sliced : Image.Type.Simple; // rum: runde hjørner, ikon: normalt
        image.preserveAspect = true; // gælder kun ikonerne
        image.raycastTarget = false;
        return image;
    }
}
