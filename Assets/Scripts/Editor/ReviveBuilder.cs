using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sætter revive op på Wizard2: Revive-komponenten, "Press E to revive" (kopi af Nameplate) og en bar (kopi af
/// fjendernes Healthbar-prefab) under wizarden. Kør via Tools > Setup Revive. Kan rettes i Unity bagefter,
/// men et nyt kør bygger prompt og bar forfra.
/// </summary>
public static class ReviveBuilder
{
    const string WizardPath = "Assets/World/Characters/wizard/Wizard2.prefab";
    const string HealthbarPath = "Assets/UI/Prefabs/Healthbar.prefab";
    static readonly Color BarColor = new(0.35f, 0.85f, 0.35f);
    const float EnemyScale = 5f; // fjenderne er skaleret x5, så baren får samme størrelse i verden som på dem
    const float BelowWizard = -0.65f; // world-enheder, samme sted som fjendernes healthbar

    [MenuItem("Tools/Setup Revive")]
    static void Setup()
    {
        var wizard = PrefabUtility.LoadPrefabContents(WizardPath);
        float scale = wizard.transform.localScale.y;

        // slås op via navn, så editor-assembly'en ikke skal kende NetworkBehaviour (Netcode)
        Component revive = wizard.GetComponent("Revive");
        if (revive == null) revive = wizard.AddComponent(System.Type.GetType("Revive, Game"));

        foreach (var old in new[] { "RevivePrompt", "ReviveBar" })
            if (wizard.transform.Find(old) is { } t) Object.DestroyImmediate(t.gameObject);

        // prompt: samme world-space tekst som navneskiltet, bare under wizarden
        var prompt = Object.Instantiate(wizard.transform.Find("Nameplate").gameObject, wizard.transform);
        prompt.name = "RevivePrompt";
        prompt.GetComponent<TMP_Text>().text = "Press E to revive";
        ((RectTransform)prompt.transform).anchoredPosition = new Vector2(0, BelowWizard / scale);
        prompt.SetActive(false);

        // bar: fjendernes healthbar uden HealthBar-scriptet, i grøn
        var bar = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HealthbarPath), wizard.transform);
        PrefabUtility.UnpackPrefabInstance(bar, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        bar.name = "ReviveBar";
        Object.DestroyImmediate(bar.GetComponent<HealthBar>());
        var rect = (RectTransform)bar.transform;
        rect.localScale *= EnemyScale / scale;
        rect.anchoredPosition = new Vector2(0, BelowWizard / scale);
        var fill = bar.transform.Find("BG/Fill").GetComponent<Image>();
        fill.color = BarColor;
        bar.SetActive(false);

        var so = new SerializedObject(revive);
        so.FindProperty("prompt").objectReferenceValue = prompt;
        so.FindProperty("bar").objectReferenceValue = bar;
        so.FindProperty("fill").objectReferenceValue = fill;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(wizard, WizardPath);
        PrefabUtility.UnloadPrefabContents(wizard);
        AssetDatabase.SaveAssets();
        Debug.Log($"Revive sat op på {WizardPath}");
    }
}
