using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Én række i character sheetets ability-liste. Lav en prefab med denne
/// komponent og træk den ind på CharacterSheetUI.abilityEntryPrefab.
/// </summary>
public class AbilityEntryUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("Valgfri: viser mana cost og cooldown.")]
    [SerializeField] private TMP_Text statsText;

    public void Bind(AbilitySO ability)
    {
        if (icon != null)
        {
            icon.sprite = ability.icon;
            icon.enabled = ability.icon != null; // skjul tom hvid firkant hvis der mangler et ikon
        }

        if (nameText != null) nameText.text = ability.abilityName;
        if (descriptionText != null) descriptionText.text = ability.description;

        if (statsText != null)
            statsText.text = $"{ability.manaCost} MP  ·  {ability.cooldown:0.#}s CD";
    }
}
