using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    /// <summary>
    /// Kuglerne med rigtig 2D-fysik i en tom testscene: bevægelse, levetid og hvad de rammer.
    /// </summary>
    public class BulletTests
    {
        private TestScene scene;

        [SetUp]
        public void SetUp() => scene = new TestScene();

        [TearDown]
        public void TearDown() => scene.Cleanup();

        private static IEnumerator PhysicsSteps(int steps = 3)
        {
            for (int i = 0; i < steps; i++)
                yield return new WaitForFixedUpdate();
        }

        private EnemyBullet NewEnemyBullet(Vector2 position, float lifetime = 6f)
        {
            var go = scene.Inactive("EnemyBullet");
            go.transform.position = position;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            go.AddComponent<CircleCollider2D>().isTrigger = true;
            var bullet = go.AddComponent<EnemyBullet>();
            bullet.lifetime = lifetime;
            go.SetActive(true);
            return bullet;
        }

        private ResourceController NewTarget(Vector2 position, string tag = "Untagged", int maxHealth = 10)
        {
            var go = scene.Inactive("Target");
            go.tag = tag;
            go.transform.position = position;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            go.AddComponent<BoxCollider2D>();
            var resources = go.AddComponent<ResourceController>();
            TestScene.SetField(resources, "maxHealth", maxHealth);
            go.SetActive(true);
            return resources;
        }

        private GameObject NewWall(Vector2 position)
        {
            var go = scene.Active("Wall");
            go.transform.position = position;
            go.AddComponent<BoxCollider2D>();
            return go;
        }

        // ---------------------------------------------------------------
        // EnemyBullet
        // ---------------------------------------------------------------
        [UnityTest]
        public IEnumerator EnemyBullet_FliesInGivenDirection()
        {
            var bullet = NewEnemyBullet(Vector2.zero);
            bullet.Init(new Vector2(0f, 2f), bulletSpeed: 5f); // retningen normaliseres

            yield return PhysicsSteps(5);

            Vector2 pos = bullet.transform.position;
            Assert.Greater(pos.y, 0.1f);
            Assert.AreEqual(0f, pos.x, 1e-3f);
            Assert.AreEqual(5f, bullet.GetComponent<Rigidbody2D>().linearVelocity.magnitude, 1e-3f);
        }

        [Test]
        public void EnemyBullet_Init_PointsSpriteInFlightDirection()
        {
            var bullet = NewEnemyBullet(Vector2.zero);

            bullet.Init(Vector2.up, 5f);

            Assert.AreEqual(Vector3.up.y, bullet.transform.right.y, 1e-4f);
        }

        [UnityTest]
        public IEnumerator EnemyBullet_HittingPlayer_DealsDamageAndDisappears()
        {
            var player = NewTarget(Vector2.zero, tag: "Player", maxHealth: 5);
            var bullet = NewEnemyBullet(Vector2.zero);
            bullet.Init(Vector2.right, 0f);

            yield return PhysicsSteps();

            Assert.AreEqual(4, player.CurrentHealth);
            Assert.IsTrue(bullet == null, "Kuglen skal forsvinde når den rammer spilleren");
        }

        [UnityTest]
        public IEnumerator EnemyBullet_HittingWall_Disappears()
        {
            NewWall(Vector2.zero);
            var bullet = NewEnemyBullet(Vector2.zero);
            bullet.Init(Vector2.right, 0f);

            yield return PhysicsSteps();

            Assert.IsTrue(bullet == null);
        }

        [UnityTest]
        public IEnumerator EnemyBullet_InsideTriggerZone_KeepsFlying()
        {
            // rum og dørens E-område er triggers og må ikke stoppe kuglen
            var zone = scene.Active("RoomTrigger");
            zone.AddComponent<BoxCollider2D>().isTrigger = true;
            var bullet = NewEnemyBullet(Vector2.zero);
            bullet.Init(Vector2.right, 0f);

            yield return PhysicsSteps();

            Assert.IsTrue(bullet != null);
        }

        [UnityTest]
        public IEnumerator EnemyBullet_InsideShootingEnemy_KeepsFlying()
        {
            var enemyGo = scene.Inactive("Enemy");
            enemyGo.AddComponent<Rigidbody2D>().gravityScale = 0f;
            enemyGo.AddComponent<BoxCollider2D>();
            var enemy = enemyGo.AddComponent<EnemyController>();
            enemy.movementPattern = EnemyController.MovementPattern.Stationary;
            enemyGo.SetActive(true);

            var bullet = NewEnemyBullet(Vector2.zero);
            bullet.Init(Vector2.right, 0f);

            yield return PhysicsSteps();

            Assert.IsTrue(bullet != null, "Kuglen må ikke dø inde i den fjende der skød den");
        }

        [UnityTest]
        public IEnumerator EnemyBullet_DespawnsAfterLifetime()
        {
            var bullet = NewEnemyBullet(new Vector2(100f, 100f), lifetime: 0.1f);
            bullet.Init(Vector2.right, 1f);

            yield return new WaitForSeconds(0.3f);

            Assert.IsTrue(bullet == null);
        }

        // ---------------------------------------------------------------
        // PlayerBullet (fireball)
        // ---------------------------------------------------------------
        private PlayerBullet NewFireball(Vector2 position)
        {
            var go = scene.Inactive("Fireball");
            go.transform.position = position;
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<Animator>();
            go.AddComponent<CircleCollider2D>().isTrigger = true;
            var bullet = go.AddComponent<PlayerBullet>();
            go.SetActive(true);
            return bullet;
        }

        private PlayerManager NewShooter(int spellDamage)
        {
            var stats = scene.Track(ScriptableObject.CreateInstance<PlayerStatsSO>());
            stats.spellDamage = spellDamage;
            var go = scene.Inactive("Shooter");
            var manager = go.AddComponent<PlayerManager>();
            TestScene.SetField(manager, "baseStats", stats);
            go.SetActive(true);
            return manager;
        }

        [TestCase(1f, 0f, false)]
        [TestCase(-1f, 0f, true)]
        [TestCase(0f, 1f, false)]
        public void Fireball_Launch_FlipsSpriteOnlyWhenFlyingLeft(float x, float y, bool expectFlip)
        {
            var fireball = NewFireball(new Vector2(100f, 100f));

            fireball.Launch(new Vector2(x, y), null);

            Assert.AreEqual(expectFlip, fireball.GetComponent<SpriteRenderer>().flipX);
        }

        [UnityTest]
        public IEnumerator Fireball_MovesInLaunchDirection()
        {
            var fireball = NewFireball(new Vector2(100f, 100f));
            fireball.Launch(Vector2.left, null);

            yield return new WaitForSeconds(0.1f);

            Assert.Less(fireball.transform.position.x, 100f);
            Assert.AreEqual(100f, fireball.transform.position.y, 1e-3f);
        }

        [UnityTest]
        public IEnumerator Fireball_DamageIsBaseDamagePlusShootersSpellDamage()
        {
            var shooter = NewShooter(spellDamage: 3);
            var enemy = NewTarget(new Vector2(0f, -50f), maxHealth: 20);
            var fireball = NewFireball(new Vector2(0f, -50f));
            fireball.Launch(Vector2.right, shooter);

            yield return PhysicsSteps();

            Assert.AreEqual(20 - (1 + 3), enemy.CurrentHealth, "baseDamage (1) + spellDamage (3)");
            Assert.IsTrue(fireball == null, "Fireballen skal forsvinde når den rammer");
        }

        [UnityTest]
        public IEnumerator Fireball_IgnoresPlayers()
        {
            var otherPlayer = NewTarget(new Vector2(0f, -80f), tag: "Player", maxHealth: 5);
            var fireball = NewFireball(new Vector2(0f, -80f));
            fireball.Launch(Vector2.right, null);

            yield return PhysicsSteps();

            Assert.AreEqual(5, otherPlayer.CurrentHealth);
            Assert.IsTrue(fireball != null);
        }
    }
}
