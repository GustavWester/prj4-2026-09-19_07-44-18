using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// "Edit tests" der tjekker spillets data-assets (ScriptableObjects) for værdier
    /// der ville give fejl eller mærkelig opførsel i spillet. Hver asset er sin egen
    /// test case, så Test Runner viser præcis hvilken asset der er galt.
    /// Ingen scene loades, og spillet startes ikke.
    /// </summary>
    [Category("AssetValidation")]
    public class AssetValidationTests
    {
        private static IEnumerable<TestCaseData> AssetsOfType<T>() where T : Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => new TestCaseData(path).SetName($"{typeof(T).Name}: {path}"));
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.IsNotNull(asset, $"Kunne ikke loade {path}");
            return asset;
        }

        private static IEnumerable<TestCaseData> Items() => AssetsOfType<ItemSO>();
        private static IEnumerable<TestCaseData> PlayerStats() => AssetsOfType<PlayerStatsSO>();
        private static IEnumerable<TestCaseData> EnemyStats() => AssetsOfType<EnemyStatsSO>();
        private static IEnumerable<TestCaseData> BulletPatterns() => AssetsOfType<BulletPatternSO>();

        [Test]
        public void Project_HasDataAssetsToValidate()
        {
            // Sikrer at testene nedenfor ikke "består" bare fordi FindAssets ikke fandt noget.
            Assert.IsNotEmpty(Items(), "Ingen ItemSO-assets fundet");
            Assert.IsNotEmpty(PlayerStats(), "Ingen PlayerStatsSO-assets fundet");
            Assert.IsNotEmpty(EnemyStats(), "Ingen EnemyStatsSO-assets fundet");
            Assert.IsNotEmpty(BulletPatterns(), "Ingen BulletPatternSO-assets fundet");
        }

        [TestCaseSource(nameof(Items))]
        public void Item_IsValid(string path)
        {
            var item = Load<ItemSO>(path);

            Assert.IsFalse(string.IsNullOrWhiteSpace(item.itemName), "itemName er tom (vises i inventory)");
            Assert.GreaterOrEqual(item.maxStack, 1, "maxStack skal være mindst 1");
            Assert.IsNotNull(item.icon, "icon mangler (slottet bliver tomt i inventory)");
        }

        [TestCaseSource(nameof(PlayerStats))]
        public void PlayerStats_AreValid(string path)
        {
            var stats = Load<PlayerStatsSO>(path);

            Assert.GreaterOrEqual(stats.maxHealth, 1, "maxHealth");
            Assert.GreaterOrEqual(stats.maxMana, 0, "maxMana");
            Assert.GreaterOrEqual(stats.manaRegenPerSecond, 0f, "manaRegenPerSecond");
            Assert.Greater(stats.moveSpeed, 0f, "moveSpeed 0 betyder at spilleren ikke kan gå");
        }

        [TestCaseSource(nameof(EnemyStats))]
        public void EnemyStats_AreValid(string path)
        {
            var stats = Load<EnemyStatsSO>(path);

            Assert.Greater(stats.Health, 0, "Health");
            Assert.GreaterOrEqual(stats.attackDamage, 0, "attackDamage");
            Assert.GreaterOrEqual(stats.moveSpeed, 0f, "moveSpeed");
            Assert.Greater(stats.attackRange, 0f, "attackRange");
            Assert.Greater(stats.attackCooldown, 0f, "attackCooldown");
            Assert.GreaterOrEqual(stats.detectionRange, stats.attackRange,
                "detectionRange skal være mindst attackRange, ellers angriber fjenden aldrig mens den jager");
        }

        [TestCaseSource(nameof(BulletPatterns))]
        public void BulletPattern_IsValid(string path)
        {
            var pattern = Load<BulletPatternSO>(path);

            Assert.Greater(pattern.bulletCount, 0, "bulletCount");
            Assert.GreaterOrEqual(pattern.volleys, 1, "volleys");
            Assert.Greater(pattern.bulletSpeed, 0f, "bulletSpeed");
            Assert.GreaterOrEqual(pattern.delayBetweenVolleys, 0f, "delayBetweenVolleys");
            Assert.That(pattern.spreadAngle, Is.InRange(0f, 360f), "spreadAngle");
        }

        [TestCaseSource(nameof(BulletPatterns))]
        public void BulletPattern_PrefabIsAWorkingEnemyBullet(string path)
        {
            var pattern = Load<BulletPatternSO>(path);

            Assert.IsNotNull(pattern.bulletPrefab, "bulletPrefab mangler, så der skydes ingenting");
            Assert.IsNotNull(pattern.bulletPrefab.GetComponent<EnemyBullet>(), "bulletPrefab har ingen EnemyBullet");
            Assert.IsNotNull(pattern.bulletPrefab.GetComponent<Rigidbody2D>(), "bulletPrefab har ingen Rigidbody2D");
            var collider = pattern.bulletPrefab.GetComponent<Collider2D>();
            Assert.IsNotNull(collider, "bulletPrefab har ingen Collider2D, så den kan ikke ramme");
            Assert.IsTrue(collider.isTrigger, "EnemyBullet bruger OnTriggerEnter2D, så collideren skal være Is Trigger");
        }
    }
}
