using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Holder styr på hvilken wizard der er "min" på denne maskine.
/// Online har alle spillere tagget "Player", så UI (character sheet, inventory)
/// spørger LocalPlayer.Current i stedet og lytter på Changed.
///
/// Online: sættes når ejerens wizard spawner.
/// Offline (ingen NetworkManager): den wizard der ligger i scenen bliver den lokale.
/// </summary>
public class LocalPlayer : NetworkBehaviour
{
    public static GameObject Current { get; private set; }

    /// <summary>Fires med den nye lokale spiller (eller null når den forsvinder).</summary>
    public static event Action<GameObject> Changed;

    // Nulstil statics ved Play, også hvis "Enter Play Mode Options" slår domain reload fra.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Current = null;
        Changed = null;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner) Set(gameObject);
    }

    public override void OnNetworkDespawn()
    {
        if (Current == gameObject) Set(null);
    }

    private void Start()
    {
        // Offline er wizarden aldrig spawnet over netværket, så den er automatisk vores.
        if (!IsSpawned && Current == null) Set(gameObject);
    }

    public override void OnDestroy()
    {
        if (Current == gameObject) Set(null);
        base.OnDestroy();
    }

    private static void Set(GameObject player)
    {
        Current = player;
        Changed?.Invoke(player);
    }
}
