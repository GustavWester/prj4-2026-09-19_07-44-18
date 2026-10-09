using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Viser spillerens character sheet: navn, portræt, HP, MP og abilities.
///
/// Setup:
/// - Sæt scriptet på et objekt der altid er aktivt (fx Canvas), og træk
///   CharacterWindow ind i 'window'. Så kan vinduet slås til/fra uden at
///   scriptet selv bliver deaktiveret.
/// - HP/MP-bar: et Image med Image Type = Filled (Horizontal).
/// - Spilleren skal have CharacterProfile og ResourceController.
/// </summary>
public class CharacterSheetUI : MonoBehaviour
{
    [Header("Window")]
    [SerializeField] private GameObject window;
    [SerializeField] private Key toggleKey = Key.C;
    [SerializeField] private bool startOpen = false;

    [Header("Target")]
    [Tooltip("Spilleren der vises. Er den tom, følges LocalPlayer (min egen wizard, også online).")]
    [SerializeField] private GameObject target;

    [Header("Identity")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image portraitImage;

    [Header("Health")]
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text healthText;

    [Header("Mana")]
    [SerializeField] private Image manaFill;
    [SerializeField] private TMP_Text manaText;

    [Header("Abilities")]
    [Tooltip("Forælder til ability-rækkerne, fx et objekt med Vertical Layout Group.")]
    [SerializeField] private Transform abilityListRoot;
    [SerializeField] private AbilityEntryUI abilityEntryPrefab;

    private CharacterProfile profile;
    private ResourceController resources;

    public bool IsOpen => window != null && window.activeSelf;

    private bool followLocalPlayer;

    private void Start()
    {
        // Online spawner wizarden først efter scenen er startet, så vi lytter efter den.
        followLocalPlayer = target == null;
        if (followLocalPlayer)
        {
            LocalPlayer.Changed += SetTarget;
            target = LocalPlayer.Current;
        }

        SetTarget(target);
        SetOpen(startOpen);
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame)
            Toggle();
    }

    private void OnDestroy()
    {
        if (followLocalPlayer) LocalPlayer.Changed -= SetTarget;
        Unsubscribe();
    }

    public void SetTarget(GameObject newTarget)
    {
        Unsubscribe();

        target = newTarget;
        profile = target != null ? target.GetComponent<CharacterProfile>() : null;
        resources = target != null ? target.GetComponent<ResourceController>() : null;

        if (resources != null)
        {
            resources.onHealthPercentChanged.AddListener(OnHealthChanged);
            resources.onManaChanged.AddListener(OnManaChanged);
        }

        RefreshAll();
    }

    // Kobles på CloseButton's OnClick i inspectoren.
    public void Close() => SetOpen(false);
    public void Toggle() => SetOpen(!IsOpen);

    public void SetOpen(bool open)
    {
        if (window == null) return;

        window.SetActive(open);
        if (open) RefreshAll(); // værdier kan have ændret sig mens vinduet var lukket
    }

    private void Unsubscribe()
    {
        if (resources == null) return;
        resources.onHealthPercentChanged.RemoveListener(OnHealthChanged);
        resources.onManaChanged.RemoveListener(OnManaChanged);
    }

    private void RefreshAll()
    {
        RefreshIdentity();
        RefreshHealth();
        RefreshMana();
        RebuildAbilities();
    }

    private void RefreshIdentity()
    {
        if (nameText != null)
            nameText.text = profile != null ? profile.characterName : "-";

        if (portraitImage != null)
        {
            portraitImage.sprite = profile != null ? profile.portrait : null;
            portraitImage.enabled = portraitImage.sprite != null;
        }
    }

    private void OnHealthChanged(float _) => RefreshHealth();
    private void OnManaChanged(int current, int max) => RefreshMana();

    private void RefreshHealth()
    {
        int current = resources != null ? resources.CurrentHealth : 0;
        int max = resources != null ? resources.MaxHealth : 0;
        SetBar(healthFill, healthText, current, max);
    }

    private void RefreshMana()
    {
        int current = resources != null ? resources.CurrentMana : 0;
        int max = resources != null ? resources.MaxMana : 0;
        SetBar(manaFill, manaText, current, max);
    }

    private static void SetBar(Image fill, TMP_Text label, int current, int max)
    {
        if (fill != null) fill.fillAmount = max > 0 ? (float)current / max : 0f;
        if (label != null) label.text = $"{current} / {max}";
    }

    private void RebuildAbilities()
    {
        if (abilityListRoot == null || abilityEntryPrefab == null) return;

        for (int i = abilityListRoot.childCount - 1; i >= 0; i--)
            Destroy(abilityListRoot.GetChild(i).gameObject);

        if (profile == null) return;

        foreach (var ability in profile.abilities)
        {
            if (ability == null) continue;
            Instantiate(abilityEntryPrefab, abilityListRoot).Bind(ability);
        }
    }
}
