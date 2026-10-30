using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drikker en potion fra inventory med en tast (som pills i Binding of Isaac).
/// Sæt én pr. potion-type på spiller-prefabben, fx Q = HealthPotion, R = ManaPotion.
/// Liv ejes af serveren, så heal sendes dertil. Mana er lokal og fyldes op her.
/// </summary>
[RequireComponent(typeof(Inventory), typeof(PlayerManager))]
public class PotionHotkey : NetworkBehaviour
{
    [SerializeField] private ItemSO potion;
    [SerializeField] private Key key = Key.Q;

    private void Update()
    {
        if (IsSpawned && !IsOwner) return; // kun min egen wizard reagerer på mit tastatur
        var kb = Keyboard.current;
        if (kb == null || !kb[key].wasPressedThisFrame) return;

        var res = GetComponent<PlayerManager>().Resources;
        bool useful = (potion.healAmount > 0 && res.CurrentHealth < res.MaxHealth)
                   || (potion.manaAmount > 0 && res.CurrentMana < res.MaxMana);
        if (!useful || !GetComponent<Inventory>().TryRemove(potion)) return; // fuldt liv/mana eller ingen potions

        res.RestoreMana(potion.manaAmount);
        if (potion.healAmount > 0)
        {
            if (IsSpawned) HealRpc();
            else res.Heal(potion.healAmount); // offline
        }
    }

    // serveren bruger sin egen healAmount, så en klient ikke selv kan bestemme hvor meget den healer
    [Rpc(SendTo.Server)]
    private void HealRpc() => GetComponent<PlayerManager>().Resources.Heal(potion.healAmount);
}