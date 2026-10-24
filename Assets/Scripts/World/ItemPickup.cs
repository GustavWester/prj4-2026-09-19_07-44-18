using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Item der ligger på banen. Står min spiller ved det og trykker E, lægges det i mit Inventory.
/// Powerups (ItemSO.isPowerup) giver deres stats med det samme på alle maskiner, da hosten afgør skade og liv.
/// Én prefab til alle items: navnet synkes, og klienterne henter ItemSO'en fra Resources/Items.
/// Kræver NetworkObject og en Collider2D med Is Trigger.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ItemPickup : NetworkBehaviour
{
    [SerializeField] private ItemSO item; // sæt i inspectoren for items der ligger i scenen, ellers sætter LootDrop det
    [SerializeField, Min(1)] private int amount = 1;

    private readonly NetworkVariable<FixedString64Bytes> itemName = new();
    private GameObject nearPlayer; // min egen wizard, når den står ved itemet
    private bool taken;

    public void SetItem(ItemSO newItem)
    {
        item = newItem;
        GetComponent<SpriteRenderer>().sprite = item.icon;
    }

    // viser itemets ikon i editoren, så man kan se forskel på pickups i scenen
    private void OnValidate()
    {
        if (item != null) GetComponent<SpriteRenderer>().sprite = item.icon;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) itemName.Value = item.name;
        else item = Resources.Load<ItemSO>("Items/" + itemName.Value.ToString());
        SetItem(item);
    }

    private void Start()
    {
        if (!IsSpawned) SetItem(item); // offline
    }

    private void Update()
    {
        if (taken || nearPlayer == null || !Keyboard.current.eKey.wasPressedThisFrame)return;

        if (IsSpawned) TakeRpc(nearPlayer.GetComponent<NetworkObject>());
        else { taken = true; Give(nearPlayer); Destroy(gameObject); } // offline
    }

    // serveren afgør hvem der fik itemet, så to spillere ikke begge får det
    [Rpc(SendTo.Server)]
    private void TakeRpc(NetworkObjectReference player)
    {
        if (taken || !player.TryGet(out NetworkObject p)) return;
        taken = true;
        Give(p.gameObject);
        GiveRpc(player);
        NetworkObject.Despawn();
    }

    [Rpc(SendTo.NotServer)]
    private void GiveRpc(NetworkObjectReference player)
    {
        if (player.TryGet(out NetworkObject p)) Give(p.gameObject);
    }

    // spillere der joiner sent får ikke de andres powerups; syncmodifier-listen hvis det bliver et problem
    private void Give(GameObject player)
    {
        // Inventory er lokalt (ikke synket), så kun ejerens egen kopi får itemet
        if (!IsSpawned || player.GetComponent<NetworkObject>().IsOwner)
            player.GetComponent<Inventory>().Add(item, amount);

        // stats skal ændres på alle maskiner, fordi hosten regner skade og liv ud
        if (item.isPowerup)
            for (int i = 0; i < amount; i++)
                player.GetComponent<PlayerManager>().Equip(item);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out PlayerMovementController p) && p.HasControl)nearPlayer = other.gameObject;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject == nearPlayer) nearPlayer = null;
    }
}