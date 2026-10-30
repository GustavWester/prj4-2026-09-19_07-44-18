using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Viser en værdi som en række ikoner ved siden af hinanden (hel / halv / tom).
/// Genbruges til både hjerter (unitsPerIcon = 2) og mana (unitsPerIcon = 10).
/// Læg den på et tomt GameObject under dit Canvas – den sørger selv for
/// HorizontalLayoutGroup og opretter/fjerner ikoner efter behov.
/// </summary>
public class HUDBar : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite fullSprite;
    [SerializeField] private Sprite halfSprite;
    [SerializeField] private Sprite emptySprite;

    [Header("Opsætning")]
    [Tooltip("Hvor mange point et helt ikon er værd. Hjerter: 2. Mana: 10.")]
    [SerializeField] private int unitsPerIcon = 2;
    [SerializeField] private Vector2 iconSize = new Vector2(32, 32);
    [SerializeField] private float spacing = 4f;

    private readonly List<Image> icons = new();
    private int lastCurrent = int.MinValue;
    private int lastMax = int.MinValue;
    
    private HorizontalLayoutGroup layout;
    
    private void Awake() => EnsureLayout();

    /// <summary>Opdaterer baren. Kald den hver gang værdien eller max ændrer sig.</summary>
    public void SetValue(int current, int max)
    {
        if (current == lastCurrent && max == lastMax) return;
        lastCurrent = current;
        lastMax = max;

        int unit = Mathf.Max(1, unitsPerIcon);
        int count = max <= 0 ? 0 : Mathf.CeilToInt(max / (float)unit);
        EnsureIconCount(count);

        for (int i = 0; i < count; i++)
        {
            int filled = current - i * unit;
            icons[i].sprite = filled >= unit        ? fullSprite
                : filled * 2 >= unit    ? halfSprite   // mindst halvdelen
                : emptySprite;
        }
    }

    private void EnsureIconCount(int count)
    {
        while (icons.Count < count)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(transform, false);

            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = iconSize.x;
            le.preferredHeight = iconSize.y;

            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            icons.Add(img);
        }

        // Skjul overskydende ikoner (hvis max falder) i stedet for at destruere dem
        for (int i = 0; i < icons.Count; i++)
            icons[i].gameObject.SetActive(i < count);
    }
    
    private void EnsureLayout()
    {
        if (layout != null) return;

        layout = GetComponent<HorizontalLayoutGroup>();
        if (layout == null) layout = gameObject.AddComponent<HorizontalLayoutGroup>();

        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // Rækken bliver præcis så bred som dens ikoner
        var fitter = GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    /// <summary>Sætter fælles størrelse/afstand, så to barer flugter.</summary>
    public void Configure(Vector2 size, float space)
    {
        iconSize = size;
        spacing = space;
        EnsureLayout();
        layout.spacing = space;

        foreach (var img in icons)
        {
            var le = img.GetComponent<LayoutElement>();
            le.preferredWidth = size.x;
            le.preferredHeight = size.y;
        }
    }
}