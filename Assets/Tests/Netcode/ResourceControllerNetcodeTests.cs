using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Tests.PlayMode;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Netcode
{
    /// <summary>
    /// ResourceController online: hosten ejer livet, ejeren ejer manaen, og alle ser de samme tal.
    /// </summary>
    public class ResourceControllerNetcodeTests : NetcodeTestBase
    {
        private GameObject prefab;

        protected override void OnServerAndClientsCreated()
        {
            prefab = NewPrefab("Resources");
            var r = prefab.AddComponent<ResourceController>();
            TestScene.SetField(r, "maxHealth", 10);
            TestScene.SetField(r, "maxMana", 50);
            TestScene.SetField(r, "manaRegenPerSecond", 0f);
        }

        private NetworkObject obj;

        // spillerens ressourcer ejes af klient 1, ligesom en wizard
        private IEnumerator SpawnOwnedByClient1()
        {
            yield return Spawn(prefab, Client1, o => obj = o);
        }

        private IEnumerator AllCopiesHaveHealth(int health) =>
            WaitFor(() => Copies<ResourceController>(obj).All(r => r.CurrentHealth == health), $"Ikke alle kopier har {health} liv");

        [UnityTest]
        public IEnumerator Spawn_DoesNotLogNetworkVariableWarning()
        {
            yield return SpawnOwnedByClient1();

            // skrivning til en NetworkVariable i Awake (før spawn) giver denne advarsel. Rettet i #50, rullet tilbage i #53.
            NetcodeLogAssert.LogWasNotReceived(LogType.Warning, new Regex("NetworkVariable is written to"));
        }

        [UnityTest]
        public IEnumerator Spawn_AllCopiesStartAtFullHealthAndMana()
        {
            yield return SpawnOwnedByClient1();

            yield return WaitFor(() => Copies<ResourceController>(obj).All(r => r.CurrentHealth == 10 && r.CurrentMana == 50),
                "Ikke alle kopier starter med fuldt liv og mana");
        }

        [UnityTest]
        public IEnumerator TakeDamage_OnHost_ReplicatesToAllClients()
        {
            yield return SpawnOwnedByClient1();
            int damagedOnClient2 = 0;
            On<ResourceController>(Client2, obj).onDamaged.AddListener(() => damagedOnClient2++);

            On<ResourceController>(Host, obj).TakeDamage(3);

            yield return AllCopiesHaveHealth(7);
            Assert.AreEqual(1, damagedOnClient2, "onDamaged skal også køre hos klienterne (hit-flash)");
        }

        [UnityTest]
        public IEnumerator TakeDamage_OnOwnerClient_IsIgnored()
        {
            yield return SpawnOwnedByClient1();

            On<ResourceController>(Client1, obj).TakeDamage(3);
            yield return WaitTicks();

            Assert.That(Copies<ResourceController>(obj).Select(r => r.CurrentHealth), Is.All.EqualTo(10),
                "Kun hosten må give skade, ellers tælles den dobbelt");
        }

        [UnityTest]
        public IEnumerator LethalDamage_OnHost_EveryCopyDiesExactlyOnce()
        {
            yield return SpawnOwnedByClient1();
            var copies = Copies<ResourceController>(obj);
            var deaths = new int[copies.Count];
            for (int i = 0; i < copies.Count; i++)
            {
                int index = i;
                copies[i].onDeath.AddListener(() => deaths[index]++);
            }

            On<ResourceController>(Host, obj).TakeDamage(999);
            On<ResourceController>(Host, obj).TakeDamage(999);

            yield return WaitFor(() => copies.All(r => r.IsDead), "Ikke alle kopier er døde");
            yield return WaitTicks();
            Assert.That(deaths, Is.All.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator TrySpendMana_OnOwner_ReplicatesToOthers()
        {
            yield return SpawnOwnedByClient1();

            bool spent = On<ResourceController>(Client1, obj).TrySpendMana(20);

            Assert.IsTrue(spent);
            yield return WaitFor(() => Copies<ResourceController>(obj).All(r => r.CurrentMana == 30), "Manaen blev ikke synkroniseret");
        }

        [UnityTest]
        public IEnumerator TrySpendMana_OnNonOwnerHost_ReturnsFalseAndChangesNothing()
        {
            yield return SpawnOwnedByClient1();

            bool spent = On<ResourceController>(Host, obj).TrySpendMana(20);
            yield return WaitTicks();

            Assert.IsFalse(spent, "Kun ejeren må bruge sin egen mana");
            Assert.That(Copies<ResourceController>(obj).Select(r => r.CurrentMana), Is.All.EqualTo(50));
        }
    }
}
