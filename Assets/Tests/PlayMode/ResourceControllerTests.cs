using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    /// <summary>
    /// ResourceController offline (ingen NetworkManager): liv, skade, død og mana.
    /// Offline har den selv autoritet, præcis som når spillet køres uden netværk.
    /// </summary>
    public class ResourceControllerTests
    {
        private TestScene scene;

        [SetUp]
        public void SetUp() => scene = new TestScene();

        [TearDown]
        public void TearDown() => scene.Cleanup();

        private ResourceController NewResources(int maxHealth = 10, int maxMana = 0, float regen = 0f)
        {
            var go = scene.Inactive("Resources");
            var resources = go.AddComponent<ResourceController>();
            TestScene.SetField(resources, "maxHealth", maxHealth);
            TestScene.SetField(resources, "maxMana", maxMana);
            TestScene.SetField(resources, "manaRegenPerSecond", regen);
            go.SetActive(true);
            return resources;
        }

        [Test]
        public void Awake_StartsAtFullHealthAndMana()
        {
            var r = NewResources(maxHealth: 10, maxMana: 50);

            Assert.AreEqual(10, r.CurrentHealth);
            Assert.AreEqual(50, r.CurrentMana);
            Assert.AreEqual(1f, r.HealthPercent);
            Assert.IsFalse(r.IsDead);
        }

        [Test]
        public void TakeDamage_LowersHealthAndFiresEvents()
        {
            var r = NewResources(maxHealth: 10);
            int damaged = 0;
            float lastPercent = -1f;
            r.onDamaged.AddListener(() => damaged++);
            r.onHealthPercentChanged.AddListener(p => lastPercent = p);

            r.TakeDamage(3);

            Assert.AreEqual(7, r.CurrentHealth);
            Assert.AreEqual(1, damaged);
            Assert.AreEqual(0.7f, lastPercent, 1e-5f);
        }

        [Test]
        public void TakeDamage_Lethal_DiesExactlyOnce()
        {
            var r = NewResources(maxHealth: 5);
            int deaths = 0;
            r.onDeath.AddListener(() => deaths++);

            r.TakeDamage(3);
            r.TakeDamage(10);
            r.TakeDamage(10);

            Assert.AreEqual(0, r.CurrentHealth);
            Assert.IsTrue(r.IsDead);
            Assert.AreEqual(1, deaths);
        }

        [Test]
        public void Heal_DoesNotGoAboveMax()
        {
            var r = NewResources(maxHealth: 10);
            r.TakeDamage(4);

            r.Heal(100);

            Assert.AreEqual(10, r.CurrentHealth);
        }

        [Test]
        public void Heal_WhenDead_DoesNothing()
        {
            var r = NewResources(maxHealth: 5);
            r.TakeDamage(5);

            r.Heal(5);

            Assert.AreEqual(0, r.CurrentHealth);
            Assert.IsTrue(r.IsDead);
        }

        [Test]
        public void Invulnerable_IgnoresDamage()
        {
            var r = NewResources(maxHealth: 10);
            r.IsInvulnerable = true;

            r.TakeDamage(5);

            Assert.AreEqual(10, r.CurrentHealth);
        }

        [Test]
        public void SetMaxHealth_AfterAwake_MovesCurrentHealthBySameAmount()
        {
            var r = NewResources(maxHealth: 10);
            r.TakeDamage(4); // 6/10

            r.SetMaxHealth(20);
            Assert.AreEqual(16, r.CurrentHealth);

            r.SetMaxHealth(10);
            Assert.AreEqual(6, r.CurrentHealth);
        }

        [Test]
        public void SetMaxHealth_Lower_NeverKills()
        {
            var r = NewResources(maxHealth: 20);
            r.TakeDamage(18); // 2/20

            r.SetMaxHealth(5);

            Assert.AreEqual(1, r.CurrentHealth);
            Assert.IsFalse(r.IsDead);
        }

        [Test]
        public void TrySpendMana_NotEnough_SpendsNothing()
        {
            var r = NewResources(maxMana: 10);

            Assert.IsFalse(r.TrySpendMana(11));
            Assert.AreEqual(10, r.CurrentMana);
        }

        [Test]
        public void TrySpendMana_Enough_SpendsAndFiresEvent()
        {
            var r = NewResources(maxMana: 10);
            var events = new List<(int, int)>();
            r.onManaChanged.AddListener((cur, max) => events.Add((cur, max)));

            Assert.IsTrue(r.TrySpendMana(4));

            Assert.AreEqual(6, r.CurrentMana);
            CollectionAssert.AreEqual(new[] { (6, 10) }, events);
        }

        [Test]
        public void TrySpendMana_Negative_IsRejected()
        {
            var r = NewResources(maxMana: 10);
            r.TrySpendMana(5);

            Assert.IsFalse(r.TrySpendMana(-5), "Negativ pris må ikke give mana");
            Assert.AreEqual(5, r.CurrentMana);
        }

        [UnityTest]
        public IEnumerator ManaRegen_RefillsOverTime_ButNotAboveMax()
        {
            var r = NewResources(maxMana: 20, regen: 100f);
            r.TrySpendMana(20);

            yield return new WaitForSeconds(0.05f);
            Assert.Greater(r.CurrentMana, 0, "Mana skal regenerere i Update");

            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(20, r.CurrentMana);
        }

        [UnityTest]
        public IEnumerator ManaRegen_Zero_DoesNotRegenerate()
        {
            var r = NewResources(maxMana: 20, regen: 0f);
            r.TrySpendMana(10);

            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(10, r.CurrentMana);
        }
    }
}
