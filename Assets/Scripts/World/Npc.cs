using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// NPC man kan snakke med: stå ved den og tryk E, E igen = næste side. Går man væk, lukker samtalen.
/// Har den en buff (ItemSO med statModifier), får man den efter sidste side, én gang pr. spiller.
///
/// Online: samtalen er lokal, så hver spiller læser i sit eget tempo. Buffen sættes på alle maskiner,
/// for hosten regner skade og liv ud (samme som powerups i ItemPickup).
/// Dialogboksen bygges i kode første gang, så der skal ikke laves UI i scenen.
/// Kræver NetworkObject, en solid Collider2D og en større Collider2D med Is Trigger.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Npc : NetworkBehaviour
{
    [SerializeField] private string npcName = "Brother Ansgar";
    [SerializeField] private Sprite portrait;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField, TextArea(2, 4)] private string[] pages;
    [Tooltip("Valgfri: stats man får efter sidste side (én gang pr. spiller). Tom = ingen buff.")]
    [SerializeField] private ItemSO buff;

    [Header("Dialogboks")]
    [SerializeField] private Sprite box;
    [SerializeField] private Sprite nextIcon;

    [Header("Taleboble over hovedet")]
    [Tooltip("Vises når man ikke snakker med NPC'en. Tom = ingen boble.")]
    [SerializeField] private string greeting = "Talk to me, tarnished";
    [Tooltip("Hvor højt over NPC'ens midte boblen sidder (world units).")]
    [SerializeField] private float bubbleHeight = 1.4f;
    [SerializeField] private Sprite bubble; // 9-slice, så den passer til teksten
    [SerializeField] private Sprite bubbleTail;

    private const float FrameTime = 0.5f;
    private const float Px = 0.07f; // én spil-pixel i world units (skala 7 / 100 PPU), boble-sprites har PPU 14.29 så de passer
    private static readonly Color TextColor = new(0.2f, 0.15f, 0.1f);
    private GameObject bubbleObj;
    private SpriteRenderer sr;
    private bool playerNear;
    private bool blessed; // kun på denne maskine; en ny scene (Try again) nulstiller den, og LoadCheckpoint fjerner buffen
    private int page = -1; // -1 = lukket
    private GameObject ui;
    private TMP_Text text;

    private void Awake() => sr = GetComponent<SpriteRenderer>();

    private void Start()
    {
        if (!string.IsNullOrEmpty(greeting) && bubble != null) BuildBubble();
    }

    private void Update()
    {
        if (idleFrames.Length > 0) sr.sprite = idleFrames[(int)(Time.time / FrameTime) % idleFrames.Length];

        if (!playerNear || Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame || Revive.AnyNearby()) return;
        page++;
        if (page < pages.Length) { Show(); return; }

        Close();
        if (buff != null && !blessed) Bless();
    }

    private void Bless()
    {
        blessed = true;
        GameObject me = LocalPlayer.Current;
        if (IsSpawned) BlessRpc(me.GetComponent<NetworkObject>());
        else me.GetComponent<PlayerManager>().Equip(buff); // offline
        FloatingText.Spawn(me.transform.position, buff.description, new Color(1f, 0.85f, 0.3f));
    }

    [Rpc(SendTo.Everyone)]
    private void BlessRpc(NetworkObjectReference player)
    {
        if (player.TryGet(out NetworkObject p, NetworkManager)) p.GetComponent<PlayerManager>().Equip(buff);
    }

    private void Show()
    {
        if (ui == null) BuildUI();
        ui.SetActive(true);
        if (bubbleObj != null) bubbleObj.SetActive(false); // boblen skjules mens man snakker
        text.text = $"<color=#7a3b12>{npcName}</color>\n{pages[page]}";
    }

    private void Close()
    {
        page = -1;
        if (ui != null) ui.SetActive(false);
        if (bubbleObj != null) bubbleObj.SetActive(true);
    }

    // boblen bygges i world units (NPC'en er skaleret op), med samme font og størrelse som FloatingText/navneskiltet
    private void BuildBubble()
    {
        bubbleObj = new GameObject("Bubble");
        bubbleObj.transform.SetParent(transform, false);
        bubbleObj.transform.localScale = Vector3.one / transform.lossyScale.x;
        bubbleObj.transform.position = transform.position + Vector3.up * bubbleHeight;

        var tmp = new GameObject("Text").AddComponent<TextMeshPro>();
        tmp.transform.SetParent(bubbleObj.transform, false);
        var font = Resources.Load<TMP_FontAsset>("Fonts/m5x7");
        if (font != null) tmp.font = font;
        tmp.fontSize = 4;
        tmp.color = TextColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.sortingOrder = 91;
        tmp.text = greeting;
        Vector2 textSize = tmp.GetPreferredValues(greeting);
        tmp.rectTransform.sizeDelta = textSize;

        // rundet op til hele spil-pixels, så kanten er skarp
        Vector2 size = new(Mathf.Ceil((textSize.x + 6 * Px) / Px) * Px, Mathf.Ceil((textSize.y + 4 * Px) / Px) * Px);
        var boxSr = AddSprite(bubble, Vector3.zero);
        boxSr.drawMode = SpriteDrawMode.Sliced;
        boxSr.size = size;
        if (bubbleTail != null) AddSprite(bubbleTail, new Vector3(0, -size.y / 2 - Px, 0)); // øverste række dækker boblens kant
    }

    private SpriteRenderer AddSprite(Sprite sprite, Vector3 localPos)
    {
        var s = new GameObject(sprite.name).AddComponent<SpriteRenderer>();
        s.transform.SetParent(bubbleObj.transform, false);
        s.transform.localPosition = localPos;
        s.sprite = sprite;
        s.sortingOrder = 90;
        return s;
    }

    private void OnTriggerEnter2D(Collider2D other) { if (IsMe(other)) playerNear = true; }
    private void OnTriggerExit2D(Collider2D other) { if (IsMe(other)) { playerNear = false; Close(); } }

    // kun min egen wizard (samme som DoorController)
    private static bool IsMe(Collider2D other) =>
        other.TryGetComponent(out PlayerMovementController p) && p.HasControl;

    // 640x360 skaleres op til skærmen. Boksen (224x32 px) vises 2x og har et felt til portrættet i venstre side
    private void BuildUI()
    {
        ui = new GameObject("NpcDialog", typeof(Canvas), typeof(CanvasScaler));
        var canvas = ui.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = ui.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(640, 360);
        scaler.matchWidthOrHeight = 0.5f;

        Image panel = AddImage(ui.transform, box, new Vector2(448, 64), new Vector2(0.5f, 0f), new Vector2(0, 40));
        AddImage(panel.transform, portrait, new Vector2(40, 40), new Vector2(0f, 0.5f), new Vector2(30, 0));
        AddImage(panel.transform, nextIcon, new Vector2(12, 12), new Vector2(1f, 0f), new Vector2(-14, 12));

        var tmp = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        tmp.transform.SetParent(panel.transform, false);
        tmp.rectTransform.anchorMin = Vector2.zero;
        tmp.rectTransform.anchorMax = Vector2.one;
        tmp.rectTransform.offsetMin = new Vector2(66, 8);
        tmp.rectTransform.offsetMax = new Vector2(-12, -6);
        var font = Resources.Load<TMP_FontAsset>("Fonts/m5x7");
        if (font != null) tmp.font = font;
        tmp.enableAutoSizing = true; // lange sider bliver mindre i stedet for at løbe ud af boksen
        tmp.fontSizeMin = 8;
        tmp.fontSizeMax = 16;
        tmp.color = TextColor;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        text = tmp;
    }

    private static Image AddImage(Transform parent, Sprite sprite, Vector2 size, Vector2 anchor, Vector2 pos)
    {
        var img = new GameObject(sprite != null ? sprite.name : "Image").AddComponent<Image>();
        img.transform.SetParent(parent, false);
        img.sprite = sprite;
        img.rectTransform.anchorMin = img.rectTransform.anchorMax = anchor;
        img.rectTransform.sizeDelta = size;
        img.rectTransform.anchoredPosition = pos;
        return img;
    }
}
