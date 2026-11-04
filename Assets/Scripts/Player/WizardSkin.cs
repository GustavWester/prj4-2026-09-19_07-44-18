using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Giver hver spiller sin egen wizard-farve. Serveren vælger den første farve ingen anden spiller har.
/// Animatoren spiller de grå sprites. I LateUpdate byttes hver sprite til den med samme navn i min farve.
/// Farverne ligger i Assets/Artwork/Wizard_Characters/Resources/WizardSkins/&lt;farve&gt;/ og er slicet som den grå wizard.
/// Offline sker der intet (grå wizard).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class WizardSkin : NetworkBehaviour
{
    [Tooltip("Mapper under Resources/WizardSkins. Index 0 får hosten.")]
    [SerializeField] private string[] skins = { "Grey Wizard", "Red Wizard", "Purple Wizard", "Green Wizard", "Brown Wizard", "White Wizard" };

    // de ark Wizard2.controller's klip bruger
    private static readonly string[] Sheets = { "idle_with_staff", "walk_with_staff", "cast_spell_with_staff", "death_with_staff", "hurt_with_staff" };

    private readonly NetworkVariable<int> netSkin = new(-1); // serveren skriver
    private SpriteRenderer sr;
    private Dictionary<string, Sprite> map;

    private void Awake() => sr = GetComponent<SpriteRenderer>();

    public override void OnNetworkSpawn()
    {
        netSkin.OnValueChanged += OnSkinChanged;
        if (IsServer) netSkin.Value = FirstFreeSkin();
        Load(netSkin.Value);
    }

    public override void OnNetworkDespawn() => netSkin.OnValueChanged -= OnSkinChanged;

    private void OnSkinChanged(int _, int skin) => Load(skin);

    private int FirstFreeSkin()
    {
        var taken = FindObjectsByType<WizardSkin>().Where(w => w != this).Select(w => w.netSkin.Value).ToHashSet();
        for (int i = 0; i < skins.Length; i++)
            if (!taken.Contains(i)) return i;
        return (int)(OwnerClientId % (ulong)skins.Length); // flere spillere end farver: genbrug
    }

    private void Load(int skin)
    {
        if (skin < 0) return;
        map = Sheets.SelectMany(sheet => Resources.LoadAll<Sprite>($"WizardSkins/{skins[skin % skins.Length]}/{sheet}"))
                    .ToDictionary(s => s.name);
    }

    // efter animatoren har sat den grå sprite denne frame
    private void LateUpdate()
    {
        if (map != null && sr.sprite != null && map.TryGetValue(sr.sprite.name, out var s)) sr.sprite = s;
    }
}
