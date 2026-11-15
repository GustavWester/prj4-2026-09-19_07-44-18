using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bygger game over-sedlen (UI/Prefabs/DeathScreenUI) i samme stil som HUD'en og sætter den på Wizard2's DeathScreen.
/// Kør via Tools > Create Death Screen UI. Prefab'en kan rettes i Unity bagefter, men et nyt kør bygger den forfra.
/// </summary>
public static class DeathScreenBuilder
{
    const string Frames = "Assets/Artwork/User_Interface/Frames/";
    const string PrefabPath = "Assets/UI/Prefabs/DeathScreenUI.prefab";
    const string WizardPath = "Assets/World/Characters/wizard/Wizard2.prefab";

    static readonly Color Ink = new(0.247f, 0.102f, 0f);         // samme brune tekst som HUD'en
    static readonly Color Parchment = new(0.87f, 0.83f, 0.69f);  // lys tekst på de mørke knapper

    [MenuItem("Tools/Create Death Screen UI")]
    static void Create()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Resources/Fonts/m5x7.asset");
        var paper = AssetDatabase.LoadAssetAtPath<Sprite>(Frames + "frame_4_beige.png");
        var buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Frames + "frame_1_dark.png");

        var root = new GameObject("DeathScreenUI", typeof(RectTransform));
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        canvas.sortingOrder = 100; // over HUD'en
        var scaler = root.AddComponent<CanvasScaler>(); // samme som HUD-canvas, så pixels er lige store
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(640, 360);
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 16;
        root.AddComponent<GraphicRaycaster>();
        var ui = root.AddComponent<DeathScreenUI>();

        // mørk baggrund over hele skærmen, blokerer også klik på HUD'en
        var dim = Child("Dim", root.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var dimRect = (RectTransform)dim.transform;
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dim.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        // sedlen
        var note = Child("Note", root.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 210));
        var noteImage = note.AddComponent<Image>();
        noteImage.sprite = paper;
        noteImage.type = Image.Type.Sliced;

        ui.titleText = Text(note.transform, "Title", "GAME OVER", 32, Ink, new Vector2(0, -30), new Vector2(260, 36), font);
        ui.messageText = Text(note.transform, "Message", "Try a little harder next time, pal.", 16, Ink, new Vector2(0, -62), new Vector2(250, 36), font);
        ui.killedByText = Text(note.transform, "KilledBy", "Killed by: Goblin", 16, Ink, new Vector2(0, -98), new Vector2(250, 24), font); // teksten sættes af DeathScreen

        ui.tryAgainButton = MakeButton(note.transform, "TryAgain", "Try again", -66, buttonSprite, font);
        ui.waitForReviveButton = MakeButton(note.transform, "WaitForRevive", "Wait for revive", 66, buttonSprite, font);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        // sæt prefab'en på wizardens DeathScreen
        var wizard = PrefabUtility.LoadPrefabContents(WizardPath);
        // slås op via navn, så editor-assembly'en ikke skal kende NetworkBehaviour (Netcode)
        var so = new SerializedObject(wizard.GetComponent("DeathScreen"));
        so.FindProperty("uiPrefab").objectReferenceValue = prefab.GetComponent<DeathScreenUI>();
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(wizard, WizardPath);
        PrefabUtility.UnloadPrefabContents(wizard);

        AssetDatabase.SaveAssets();
        Debug.Log($"Death screen oprettet: {PrefabPath} (sat på {WizardPath})");
    }

    static GameObject Child(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    // tekst forankret i toppen af sedlen
    static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color, Vector2 pos, Vector2 box, TMP_FontAsset font) =>
        Label(Child(name, parent, new Vector2(0.5f, 1f), pos, box), text, size, color, font);

    static TextMeshProUGUI Label(GameObject go, string text, float size, Color color, TMP_FontAsset font)
    {
        var t = go.AddComponent<TextMeshProUGUI>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    // knap forankret i bunden af sedlen
    static Button MakeButton(Transform parent, string name, string text, float x, Sprite sprite, TMP_FontAsset font)
    {
        var go = Child(name, parent, new Vector2(0.5f, 0f), new Vector2(x, 32), new Vector2(120, 28));
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 2; // halv kant, ellers fylder rammen hele knappen
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        var label = Child("Label", go.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 28));
        Label(label, text, 16, Parchment, font);
        return button;
    }
}
