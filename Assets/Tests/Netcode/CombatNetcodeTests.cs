using System.Collections;
using System.Linq;
using NUnit.Framework;
using Tests.PlayMode;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Netcode
{
    /// <summary>
    /// Angreb online: et skud laves én gang på hver maskine, men kun hostens spell giver skade.
    /// </summary>
    public class CombatNetcodeTests : NetcodeTestBase
    {
        private GameObject targetPrefab;
        private GameObject cameraObject;

        protected override IEnumerator OnSetup()
        {
            yield return base.OnSetup();
            // PlayerMovementController sætter kameraet til at følge ejerens wizard ved spawn
            cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<CameraController>();
        }

        protected override IEnumerator OnTearDown()
        {
            // spells er ikke netværksobjekter, så Netcode rydder dem ikke op
            foreach (var spell in Object.FindObjectsByType<TestSpell>()) Object.DestroyImmediate(spell.gameObject);
            TestSpell.Launched.Clear();
            Object.DestroyImmediate(cameraObject);
            yield return base.OnTearDown();
        }

        protected override void OnCreatePlayerPrefab()
        {
            AddPlayerManager(m_PlayerPrefab);
            m_PlayerPrefab.AddComponent<Animator>(); // Shoot afspiller "Attack"
            m_PlayerPrefab.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            m_PlayerPrefab.AddComponent<PlayerMovementController>().enabled = false; // ingen input i testene
            var attack = m_PlayerPrefab.AddComponent<PlayerAttackController>();
            TestScene.SetField(attack, "fireballPrefab", NewTemplate("Spell").AddComponent<TestSpell>().gameObject);
        }

        protected override void OnServerAndClientsCreated()
        {
            targetPrefab = NewPrefab("Target");
            TestScene.SetField(targetPrefab.AddComponent<ResourceController>(), "maxHealth", 10);
            targetPrefab.AddComponent<BoxCollider2D>();
        }

        // en spell som den ville ligge på en bestemt maskine (base damage 1, ingen skytte)
        private static TestSpell NewSpell() => new GameObject("Spell").AddComponent<TestSpell>();

        [UnityTest]
        public IEnumerator ShootRpc_FromClient_SpellCreatedOncePerMachine()
        {
            TestScene.Call(Player<PlayerAttackController>(Client1, Client1), "ShootRpc", Vector2.zero, Vector2.right);

            yield return WaitFor(() => TestSpell.Launched.Count == TotalClients, "Skuddet blev ikke lavet hos alle");
            yield return WaitTicks();
            Assert.AreEqual(TotalClients, TestSpell.Launched.Count, "Én spell pr. maskine, ikke flere");
            Assert.AreEqual(TotalClients, TestSpell.Launched.Select(s => s.Shooter.GetComponent<NetworkObject>().NetworkManager).Distinct().Count());
            Assert.That(TestSpell.Launched.Select(s => s.Shooter.GetComponent<NetworkObject>().OwnerClientId), Is.All.EqualTo(Client1.LocalClientId),
                "Det er klient 1's wizard der skyder på alle maskiner");
        }

        [UnityTest]
        public IEnumerator DealDamage_SpellOnClient_DealsNoDamage()
        {
            NetworkObject target = null;
            yield return Spawn(targetPrefab, Host, o => target = o);
            Assert.AreSame(Host, NetworkManager.Singleton, "I testene er Singleton hosten");

            var spell = NewSpell();
            AsSingleton(Client1, () => spell.Hit(On<BoxCollider2D>(Host, target)));
            yield return WaitTicks();

            Assert.That(Copies<ResourceController>(target).Select(r => r.CurrentHealth), Is.All.EqualTo(10),
                "Hver maskine har sin egen spell, så kun hostens må give skade");
        }

        [UnityTest]
        public IEnumerator DealDamage_SpellOnHost_DamageReplicates()
        {
            NetworkObject target = null;
            yield return Spawn(targetPrefab, Host, o => target = o);

            NewSpell().Hit(On<BoxCollider2D>(Host, target));

            yield return WaitFor(() => Copies<ResourceController>(target).All(r => r.CurrentHealth == 9), "Skaden blev ikke synkroniseret");
        }
    }
}
