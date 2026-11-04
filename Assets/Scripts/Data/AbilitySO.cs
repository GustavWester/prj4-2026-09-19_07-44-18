using UnityEngine;

/// <summary>
/// Data for én ability, som vises i character sheet. Opret via
/// Create > Scriptable Objects > Ability.
/// </summary>
[CreateAssetMenu(fileName = "Ability", menuName = "Scriptable Objects/Ability")]
public class AbilitySO : ScriptableObject
{
    public string abilityName;
    public Sprite icon;
    [TextArea(2, 4)] public string description;

    [Header("Stats (vises i UI)")]
    public int manaCost;
    public float cooldown;
}
