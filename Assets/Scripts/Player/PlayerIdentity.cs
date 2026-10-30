using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Viser spillerens username fra PlayerSession i et navneskilt over hovedet.
///
/// Online: ejeren skriver navnet i en NetworkVariable, så alle maskiner ser det.
/// Uden login (fx Play direkte i DungeonScene) bruges CharacterProfile.characterName.
///
/// Setup: læg scriptet på spiller-prefabben og træk et child med TextMeshPro
/// (world-space, ikke UGUI) ind i 'nameplate'.
/// </summary>
[RequireComponent(typeof(CharacterProfile))]
public class PlayerIdentity : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameplate;

    // Kun ejeren må skrive. Alle andre læser.
    private readonly NetworkVariable<FixedString64Bytes> netName = new(writePerm: NetworkVariableWritePermission.Owner);

    private CharacterProfile profile;

    private void Awake()
    {
        profile = GetComponent<CharacterProfile>();
    }

    // Offline er spilleren aldrig spawnet, så navnet sættes lokalt.
    private void Start()
    {
        if (!IsSpawned) ApplyName(SessionName());
    }

    public override void OnNetworkSpawn()
    {
        netName.OnValueChanged += OnNetNameChanged;

        if (IsOwner)
        {
            ApplyName(SessionName());
            netName.Value = new FixedString64Bytes(SessionName());
        }
        else if (!netName.Value.IsEmpty)
        {
            ApplyName(netName.Value.ToString());
        }
    }

    public override void OnNetworkDespawn()
    {
        netName.OnValueChanged -= OnNetNameChanged;
    }

    private string SessionName() => PlayerSession.Instance?.Username ?? profile.characterName;

    private void OnNetNameChanged(FixedString64Bytes _, FixedString64Bytes newValue) => ApplyName(newValue.ToString());

    private void ApplyName(string newName)
    {
        profile.characterName = newName;
        if (nameplate != null) nameplate.text = newName;
    }
}
