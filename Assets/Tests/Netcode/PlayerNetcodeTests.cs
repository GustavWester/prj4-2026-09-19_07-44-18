using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tests.PlayMode;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Netcode
{
    /// <summary>
    /// Spilleren online: hver maskine har sin egen LocalPlayer, navneskiltet synkroniseres
    /// fra ejeren, og en potion heler via hosten.
    /// </summary>
    public class PlayerNetcodeTests : NetcodeTestBase
    {
        private readonly List<GameObject> announced = new();

        protected override IEnumerator OnSetup()
        {
            yield return base.OnSetup();
            announced.Clear();
            LocalPlayer.Changed += OnLocalPlayerChanged; // før spillerne spawner
        }

        protected override IEnumerator OnTearDown()
        {
            LocalPlayer.Changed -= OnLocalPlayerChanged;
            yield return base.OnTearDown();
        }

        private void OnLocalPlayerChanged(GameObject player)
        {
            if (player != null) announced.Add(player);
        }

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<LocalPlayer>();
            m_PlayerPrefab.AddComponent<CharacterProfile>().characterName = "Testtrold";
            m_PlayerPrefab.AddComponent<PlayerIdentity>();
            AddPlayerManager(m_PlayerPrefab, maxHealth: 5);
            m_PlayerPrefab.AddComponent<Inventory>();
            var potion = m_PlayerPrefab.AddComponent<PotionHotkey>();
            TestScene.SetField(potion, "potion", Resources.Load<ItemSO>("Items/HealthPotion"));
        }

        private static string NetName(PlayerIdentity identity) =>
            TestScene.GetField<NetworkVariable<FixedString64Bytes>>(identity, "netName").Value.ToString();

        [UnityTest]
        public IEnumerator Spawn_OnlyOwnerCopiesBecomeLocalPlayer()
        {
            yield return WaitTicks();

            Assert.AreEqual(TotalClients, announced.Count, "Én LocalPlayer pr. maskine");
            Assert.That(announced.Select(p => p.GetComponent<NetworkObject>().IsOwner), Is.All.True);
            Assert.AreEqual(TotalClients, announced.Select(p => p.GetComponent<NetworkObject>().NetworkManager).Distinct().Count());
        }

        [UnityTest]
        public IEnumerator Despawn_OfCurrentPlayer_ClearsCurrent()
        {
            var current = LocalPlayer.Current.GetComponent<NetworkObject>();

            m_PlayerNetworkObjects[Host.LocalClientId][current.OwnerClientId].Despawn();

            yield return WaitFor(() => LocalPlayer.Current == null, "LocalPlayer.Current blev ikke nulstillet");
        }

        [UnityTest]
        public IEnumerator Despawn_OfOtherPlayer_KeepsCurrent()
        {
            var current = LocalPlayer.Current;
            ulong owner = current.GetComponent<NetworkObject>().OwnerClientId;
            ulong other = m_NetworkManagers.First(nm => nm.LocalClientId != owner).LocalClientId;

            m_PlayerNetworkObjects[Host.LocalClientId][other].Despawn();
            yield return WaitTicks();

            Assert.AreSame(current, LocalPlayer.Current);
        }

        [UnityTest]
        public IEnumerator Spawn_OwnerNameReplicatesToAllCopies()
        {
            // netName og ikke characterName: navnet i prefabben ville være "Testtrold" uanset synkronisering
            yield return WaitFor(() => m_NetworkManagers.All(owner => PlayerCopies<PlayerIdentity>(owner).All(i => NetName(i) == "Testtrold")),
                "Navnet blev ikke synkroniseret til alle kopier");
        }

        [UnityTest]
        public IEnumerator NameChange_ByOwner_ShowsOnOtherCopies()
        {
            TestScene.GetField<NetworkVariable<FixedString64Bytes>>(Player<PlayerIdentity>(Client1, Client1), "netName").Value = "Anna";

            yield return WaitFor(() => Player<CharacterProfile>(Host, Client1).characterName == "Anna"
                                    && Player<CharacterProfile>(Client2, Client1).characterName == "Anna",
                "Navneskiltet blev ikke opdateret hos de andre");
        }

        [UnityTest]
        public IEnumerator LateJoin_NewClientSeesExistingName()
        {
            TestScene.GetField<NetworkVariable<FixedString64Bytes>>(Player<PlayerIdentity>(Client1, Client1), "netName").Value = "Anna";
            yield return WaitFor(() => Player<CharacterProfile>(Host, Client1).characterName == "Anna", "Navnet nåede ikke hosten");

            yield return CreateAndStartNewClient();
            var late = m_ClientNetworkManagers.Last();

            yield return WaitFor(() => Player<CharacterProfile>(late, Client1).characterName == "Anna",
                "En spiller der joiner senere ser ikke navnet");
        }

        [UnityTest]
        public IEnumerator HealRpc_FromOwner_HealsOnHostAndReplicates()
        {
            Player<ResourceController>(Host, Client1).TakeDamage(3);
            yield return WaitFor(() => Player<ResourceController>(Client1, Client1).CurrentHealth == 2, "Skaden nåede ikke ejeren");

            TestScene.Call(Player<PotionHotkey>(Client1, Client1), "HealRpc");

            // HealthPotion heler 2
            yield return WaitFor(() => PlayerCopies<ResourceController>(Client1).All(r => r.CurrentHealth == 4),
                "Healingen blev ikke udført af hosten og synkroniseret");
        }
    }
}
