using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tests.PlayMode;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Netcode
{
    /// <summary>
    /// Loot online: kun hosten spawner items, klienterne finder itemet ud fra navnet,
    /// og når en spiller samler op, får kun den spiller det i sit inventory.
    /// </summary>
    public class LootNetcodeTests : NetcodeTestBase
    {
        private GameObject pickupPrefab;
        private LootDrop drop;
        private ItemSO potion;

        protected override void OnCreatePlayerPrefab()
        {
            AddPlayerManager(m_PlayerPrefab, maxHealth: 5);
            m_PlayerPrefab.AddComponent<Inventory>();
        }

        protected override void OnServerAndClientsCreated()
        {
            potion = Resources.Load<ItemSO>("Items/HealthPotion");
            // item er tomt i prefabben, så klienterne kun kan få det via navnet fra hosten
            pickupPrefab = NewPrefab("Pickup");
            pickupPrefab.AddComponent<ItemPickup>();
        }

        protected override IEnumerator OnTearDown()
        {
            if (drop != null) Object.DestroyImmediate(drop.gameObject);
            yield return base.OnTearDown();
        }

        // LootDrop er ikke et netværksobjekt, den ligger på en fjende hos hosten
        private LootDrop NewDrop(bool onePerPlayer = false)
        {
            drop = new GameObject("LootDrop").AddComponent<LootDrop>();
            TestScene.SetField(drop, "pickupPrefab", pickupPrefab.GetComponent<ItemPickup>());
            TestScene.SetField(drop, "items", new[] { potion });
            TestScene.SetField(drop, "chance", 1f);
            TestScene.SetField(drop, "onePerPlayer", onePerPlayer);
            return drop;
        }

        private static List<ItemPickup> Pickups(NetworkManager nm) =>
            nm.SpawnManager.SpawnedObjects.Values.Select(o => o.GetComponent<ItemPickup>()).Where(p => p != null).ToList();

        private IEnumerator SpawnPickup(ItemSO item, System.Action<NetworkObject> result)
        {
            TestScene.SetField(pickupPrefab.GetComponent<ItemPickup>(), "item", item);
            yield return Spawn(pickupPrefab, Host, result);
        }

        private static void Take(ItemPickup pickup, NetworkObject player) =>
            TestScene.Call(pickup, "TakeRpc", new NetworkObjectReference(player));

        [UnityTest]
        public IEnumerator Drop_OnHost_PickupSpawnsForAll_ClientsLoadItemByName()
        {
            NewDrop().Drop();

            yield return WaitFor(() => m_NetworkManagers.All(nm => Pickups(nm).Count == 1), "Pickuppen blev ikke spawnet hos alle");
            foreach (var client in m_ClientNetworkManagers)
                Assert.AreSame(potion, TestScene.GetField<ItemSO>(Pickups(client)[0], "item"), "Klienten fandt ikke itemet ud fra navnet");
        }

        [UnityTest]
        public IEnumerator Drop_OnePerPlayer_OneItemPerConnectedClient()
        {
            NewDrop(onePerPlayer: true).Drop();

            yield return WaitFor(() => m_NetworkManagers.All(nm => Pickups(nm).Count == TotalClients), "Ikke ét item pr. spiller");
        }

        [UnityTest]
        public IEnumerator Drop_AsClient_SpawnsNothing()
        {
            var d = NewDrop();

            // onDeath kører hos alle, så Drop kaldes også på klienterne
            AsSingleton(Client1, d.Drop);
            yield return WaitTicks();

            Assert.That(m_NetworkManagers.Select(nm => Pickups(nm).Count), Is.All.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator TakeRpc_FromClient_OnlyOwnersInventoryGetsItem()
        {
            NetworkObject pickup = null;
            yield return SpawnPickup(potion, o => pickup = o);

            Take(On<ItemPickup>(Client1, pickup), Player<NetworkObject>(Client1, Client1));

            yield return WaitFor(() => Player<Inventory>(Client1, Client1).CountOf(potion) == 1, "Spilleren fik ikke itemet");
            yield return WaitTicks();
            Assert.AreEqual(0, Player<Inventory>(Host, Client1).CountOf(potion), "Hostens kopi af spilleren må ikke få det");
            Assert.AreEqual(0, Player<Inventory>(Client2, Client1).CountOf(potion), "Klient 2's kopi af spilleren må ikke få det");
        }

        [UnityTest]
        public IEnumerator TakeRpc_DespawnsPickupForEveryone()
        {
            NetworkObject pickup = null;
            yield return SpawnPickup(potion, o => pickup = o);

            Take(On<ItemPickup>(Client1, pickup), Player<NetworkObject>(Client1, Client1));

            yield return WaitForDespawnedOnAllOrTimeOut(new List<NetworkObject> { pickup });
            AssertOnTimeout("Pickuppen forsvandt ikke hos alle");
        }

        [UnityTest]
        public IEnumerator TakeRpc_TwoClientsAtOnce_OnlyOneGetsItem()
        {
            NetworkObject pickup = null;
            yield return SpawnPickup(potion, o => pickup = o);

            Take(On<ItemPickup>(Client1, pickup), Player<NetworkObject>(Client1, Client1));
            Take(On<ItemPickup>(Client2, pickup), Player<NetworkObject>(Client2, Client2));

            yield return WaitForDespawnedOnAllOrTimeOut(new List<NetworkObject> { pickup });
            yield return WaitTicks();
            int total = Player<Inventory>(Client1, Client1).CountOf(potion) + Player<Inventory>(Client2, Client2).CountOf(potion);
            Assert.AreEqual(1, total, "Samme item må ikke kunne samles op af to");
        }

        [UnityTest]
        public IEnumerator TakeRpc_Powerup_StatsUpdatedOnAllCopies()
        {
            var mushroom = Resources.Load<ItemSO>("Items/MagicMushroom"); // +1 max liv
            NetworkObject pickup = null;
            yield return SpawnPickup(mushroom, o => pickup = o);

            Take(On<ItemPickup>(Client1, pickup), Player<NetworkObject>(Client1, Client1));

            // hosten afgør skade ud fra sine egne stats, så powerups skal også gælde på hostens kopi
            yield return WaitFor(() => PlayerCopies<PlayerManager>(Client1).All(p => p.MaxHealth == 6), "Powerup'en gælder ikke på alle kopier");
        }
    }
}
