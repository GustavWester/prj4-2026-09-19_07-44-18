using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tests.PlayMode;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Netcode
{
    /// <summary>
    /// Host + 2 klienter i samme proces (Netcodes egen NetcodeIntegrationTest).
    /// Prefabs bygges under en inaktiv rod: Awake/Start/Update kører kun på de kopier Netcode
    /// spawner, ikke på skabelonen, og private felter kan sættes med TestScene.SetField før Awake.
    ///
    /// Alt ligger i én scene, så hver spawnet ting findes i én kopi pr. maskine (host + klienter).
    /// Brug On/Copies til at hente en bestemt maskines kopi.
    /// </summary>
    [Category("Netcode")]
    public abstract class NetcodeTestBase : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        protected NetworkManager Host => m_ServerNetworkManager;
        protected NetworkManager Client1 => m_ClientNetworkManagers[0];
        protected NetworkManager Client2 => m_ClientNetworkManagers[1];

        private GameObject templates;

        protected override IEnumerator OnSetup()
        {
            if (templates != null) Object.DestroyImmediate(templates); // fra forrige test
            templates = new GameObject("Templates");
            templates.SetActive(false);
            yield break;
        }

        // kaldes før Netcode tilføjer NetworkObject, så flytningen giver ingen Netcode-fejl
        protected override void OnPlayerPrefabGameObjectCreated() => m_PlayerPrefab.transform.SetParent(templates.transform);

        // Skabelonerne slettes først når Netcode er lukket ned (næste OnSetup / efter sidste test).
        // OnTearDown kører før nedlukningen, og fx GhostSpawner bruger stadig sin minion-prefab der.
        protected override void OnOneTimeTearDown()
        {
            if (templates != null) Object.DestroyImmediate(templates);
            base.OnOneTimeTearDown();
        }

        /// <summary>Inaktiv skabelon der ikke er en netværks-prefab, fx en spell eller kugle.</summary>
        protected GameObject NewTemplate(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(templates.transform);
            return go;
        }

        /// <summary>Netværks-prefab kendt af host og alle klienter. Kald fra OnServerAndClientsCreated.</summary>
        protected GameObject NewPrefab(string name)
        {
            var go = NewTemplate(name);
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(go.AddComponent<NetworkObject>());
            foreach (var nm in m_NetworkManagers) nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = go });
            return go;
        }

        /// <summary>PlayerManager uden mana-regen, så mana kun ændrer sig når testen gør det.</summary>
        protected static PlayerManager AddPlayerManager(GameObject go, int maxHealth = 5)
        {
            var stats = ScriptableObject.CreateInstance<PlayerStatsSO>();
            stats.maxHealth = maxHealth;
            stats.manaRegenPerSecond = 0f;
            var pm = go.AddComponent<PlayerManager>();
            TestScene.SetField(pm, "baseStats", stats);
            return pm;
        }

        /// <summary>Spawner en prefab (af hosten) med owner som ejer og venter til alle maskiner har den.</summary>
        protected IEnumerator Spawn(GameObject prefab, NetworkManager owner, Action<NetworkObject> result)
        {
            var obj = SpawnObject(prefab, owner).GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(obj);
            AssertOnTimeout($"{prefab.name} blev ikke spawnet hos alle");
            result(obj);
        }

        /// <summary>Maskinen nm's kopi af et spawnet objekt.</summary>
        protected static T On<T>(NetworkManager nm, NetworkObject obj) where T : Component =>
            nm.SpawnManager.SpawnedObjects[obj.NetworkObjectId].GetComponent<T>();

        /// <summary>Alle maskiners kopier af et spawnet objekt.</summary>
        protected List<T> Copies<T>(NetworkObject obj) where T : Component =>
            m_NetworkManagers.Select(nm => On<T>(nm, obj)).ToList();

        /// <summary>viewer's kopi af owner's spiller.</summary>
        protected T Player<T>(NetworkManager viewer, NetworkManager owner) where T : Component =>
            m_PlayerNetworkObjects[viewer.LocalClientId][owner.LocalClientId].GetComponent<T>();

        /// <summary>Alle maskiners kopier af owner's spiller.</summary>
        protected List<T> PlayerCopies<T>(NetworkManager owner) where T : Component =>
            m_NetworkManagers.Select(viewer => Player<T>(viewer, owner)).ToList();

        protected IEnumerator WaitFor(Func<bool> condition, string message)
        {
            yield return WaitForConditionOrTimeOut(condition);
            AssertOnTimeout(message);
        }

        /// <summary>Giver netværket tid til at sende, når testen vil vise at noget IKKE sker.</summary>
        protected IEnumerator WaitTicks(int ticks = 5) => WaitForTicks(Host, ticks);

        /// <summary>Kører action, som om koden kørte på en klient (NetworkManager.Singleton er ellers hosten).</summary>
        protected void AsSingleton(NetworkManager nm, Action action)
        {
            nm.SetSingleton();
            try { action(); }
            finally { Host.SetSingleton(); }
        }
    }
}
