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
    /// Fjender online: AI og død kører kun på hosten, klienterne ser resultatet.
    /// </summary>
    public class EnemyNetcodeTests : NetcodeTestBase
    {
        private GameObject enemyPrefab;
        private GameObject bossPrefab;

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.tag = "Player"; // GhostSpawner spawner kun når der er en spiller
        }

        protected override void OnServerAndClientsCreated()
        {
            enemyPrefab = NewPrefab("Enemy");
            // kinematisk: kopierne fra alle maskiner ligger oven i hinanden i samme fysik-scene
            enemyPrefab.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            TestScene.SetField(enemyPrefab.AddComponent<ResourceController>(), "maxHealth", 10);
            enemyPrefab.AddComponent<SpriteRenderer>();
            var enemy = enemyPrefab.AddComponent<EnemyController>();
            enemy.movementPattern = EnemyController.MovementPattern.Stationary;
            enemy.firePoint = enemyPrefab.transform;
            var pattern = ScriptableObject.CreateInstance<BulletPatternSO>();
            pattern.bulletPrefab = NewTemplate("Bullet");
            pattern.bulletPrefab.AddComponent<TestMarker>();
            enemy.attackPatterns = new List<BulletPatternSO> { pattern };

            var minionPrefab = NewPrefab("Minion");
            TestScene.SetField(minionPrefab.AddComponent<ResourceController>(), "maxHealth", 1);
            minionPrefab.AddComponent<TestMarker>();

            bossPrefab = NewPrefab("Boss");
            TestScene.SetField(bossPrefab.AddComponent<ResourceController>(), "maxHealth", 10);
            var spawner = bossPrefab.AddComponent<GhostSpawner>();
            TestScene.SetField(spawner, "minion", minionPrefab);
            TestScene.SetField(spawner, "interval", 0.1f);
            TestScene.SetField(spawner, "maxAlive", 2);
        }

        protected override IEnumerator OnTearDown()
        {
            // kugler er ikke netværksobjekter, så Netcode rydder dem ikke op
            foreach (var marker in Object.FindObjectsByType<TestMarker>())
                if (marker.GetComponent<NetworkObject>() == null) Object.DestroyImmediate(marker.gameObject);
            yield return base.OnTearDown();
        }

        private static List<ResourceController> Minions(NetworkManager nm) =>
            nm.SpawnManager.SpawnedObjects.Values.Where(o => o.GetComponent<TestMarker>() != null)
                .Select(o => o.GetComponent<ResourceController>()).ToList();

        [UnityTest]
        public IEnumerator Death_OnHost_DespawnsOnAllClients()
        {
            NetworkObject enemy = null;
            yield return Spawn(enemyPrefab, Host, o => enemy = o);

            On<ResourceController>(Host, enemy).TakeDamage(999);

            yield return WaitForDespawnedOnAllOrTimeOut(new List<NetworkObject> { enemy });
            AssertOnTimeout("Den døde fjende forsvandt ikke hos alle");
        }

        [UnityTest]
        public IEnumerator MovingLeft_FlipXReplicatesToClients()
        {
            var template = enemyPrefab.GetComponent<EnemyController>();
            template.movementPattern = EnemyController.MovementPattern.Linear;
            template.linearDirection = Vector2.left;
            enemyPrefab.transform.position = new Vector3(100f, 0f); // langt fra spillerne, så den kigger i gå-retningen

            NetworkObject enemy = null;
            yield return Spawn(enemyPrefab, Host, o => enemy = o);

            yield return WaitFor(() => m_ClientNetworkManagers.All(c => On<SpriteRenderer>(c, enemy).flipX),
                "Klienterne ser ikke fjenden vende mod venstre");
        }

        [UnityTest]
        public IEnumerator SpawnBulletRpc_OnHost_BulletCreatedOncePerMachine()
        {
            NetworkObject enemy = null;
            yield return Spawn(enemyPrefab, Host, o => enemy = o);

            TestScene.Call(On<EnemyController>(Host, enemy), "SpawnBulletRpc", 0, Vector2.zero, Vector2.right);

            yield return WaitFor(() => Object.FindObjectsByType<TestMarker>().Count(m => m.GetComponent<NetworkObject>() == null) == TotalClients,
                "Kuglen blev ikke lavet hos alle");
            yield return WaitTicks();
            Assert.AreEqual(TotalClients, Object.FindObjectsByType<TestMarker>().Count(m => m.GetComponent<NetworkObject>() == null),
                "Én kugle pr. maskine, ikke flere");
        }

        [UnityTest]
        public IEnumerator GhostSpawner_OnHost_MinionsVisibleToAllAndNoLocalGhosts()
        {
            yield return Spawn(bossPrefab, Host, _ => { });

            yield return WaitFor(() => m_NetworkManagers.All(nm => Minions(nm).Count > 0), "Minions blev ikke spawnet hos alle");
            Assert.That(Object.FindObjectsByType<TestMarker>().Select(m => m.GetComponent<NetworkObject>().IsSpawned), Is.All.True,
                "Klienternes kopi af bossen må ikke lave sine egne minions");
        }

        [UnityTest]
        public IEnumerator BossDeath_KillsMinionsForEveryone()
        {
            NetworkObject boss = null;
            yield return Spawn(bossPrefab, Host, o => boss = o);
            yield return WaitFor(() => m_NetworkManagers.All(nm => Minions(nm).Count == 2), "Bossen nåede ikke at spawne 2 minions");

            On<ResourceController>(Host, boss).TakeDamage(999);

            yield return WaitFor(() => m_NetworkManagers.All(nm => Minions(nm).All(m => m.IsDead)), "Minions døde ikke hos alle");
        }
    }
}
