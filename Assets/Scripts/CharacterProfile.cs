using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sættes på spiller-prefabben. Holder det, character sheet viser ud over
/// Health og Mana: navn, portræt og abilities.
/// </summary>
[DisallowMultipleComponent]
public class CharacterProfile : MonoBehaviour
{
    public string characterName = "Wizard";
    public Sprite portrait;
    public List<AbilitySO> abilities = new();
}
