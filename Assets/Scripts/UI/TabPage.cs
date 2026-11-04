using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sættes på en tab (Toggle). Viser 'page' når tabben er valgt og skjuler den ellers.
/// Toggles i samme ToggleGroup sørger selv for, at kun én tab er valgt ad gangen.
/// </summary>
[RequireComponent(typeof(Toggle))]
public class TabPage : MonoBehaviour
{
    [SerializeField] private GameObject page;

    private Toggle toggle;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnToggled);
        OnToggled(toggle.isOn); // sæt den rigtige side fra start
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnToggled);
    }

    private void OnToggled(bool isOn)
    {
        if (page != null)
            page.SetActive(isOn);
    }
}
