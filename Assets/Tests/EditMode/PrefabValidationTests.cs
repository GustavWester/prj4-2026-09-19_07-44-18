using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// "Edit tests" der tjekker at prefabs er sat korrekt op: ingen manglende scripts,
    /// og at de felter koden forudsætter (fx PlayerAttackController.fireballPrefab) er udfyldt.
    /// Prefabs loades kun som assets. Ingen scene loades, og spillet startes ikke.
    /// </summary>
    [Category("AssetValidation")]
    public class PrefabValidationTests
    {
        private static IEnumerable<TestCaseData> PrefabsWith<T>() where T : Component
        {
            return AllPrefabPaths()
                .Where(path => AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentInChildren<T>(true) != null)
                .Select(path => new TestCaseData(path).SetName($"{typeof(T).Name}: {path}"));
        }

        private static IEnumerable<string> AllPrefabPaths() =>
            AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath);

        private static IEnumerable<TestCaseData> AllPrefabs() =>
            AllPrefabPaths().Select(path => new TestCaseData(path).SetName($"Prefab: {path}"));

        private static IEnumerable<TestCaseData> Enemies() => PrefabsWith<EnemyController>();
        private static IEnumerable<TestCaseData> Players() => PrefabsWith<PlayerManager>();
        private static IEnumerable<TestCaseData> PlayerBullets() => PrefabsWith<PlayerBullet>();

        private static GameObject Load(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, $"Kunne ikke loade {path}");
            return prefab;
        }

        private static T Field<T>(Object target, string fieldName) where T : Object
        {
            var prop = new SerializedObject(target).FindProperty(fieldName);
            Assert.IsNotNull(prop, $"{target.GetType().Name} har intet serialiseret felt '{fieldName}'");
            return prop.objectReferenceValue as T;
        }

        [Test]
        public void Project_HasPrefabsToValidate()
        {
            Assert.IsNotEmpty(Enemies(), "Ingen fjende-prefabs fundet");
            Assert.IsNotEmpty(Players(), "Ingen spiller-prefab fundet");
            Assert.IsNotEmpty(PlayerBullets(), "Ingen PlayerBullet-prefab fundet");
        }

        [TestCaseSource(nameof(AllPrefabs))]
        public void Prefab_HasNoMissingScripts(string path)
        {
            var prefab = Load(path);

            var broken = prefab.GetComponentsInChildren<Transform>(true)
                .Where(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
                .Select(t => t.name)
                .ToList();

            Assert.IsEmpty(broken, $"GameObjects med 'Missing Script': {string.Join(", ", broken)}");
        }

        [TestCaseSource(nameof(Enemies))]
        public void Enemy_IsSetUpCorrectly(string path)
        {
            var enemy = Load(path).GetComponentInChildren<EnemyController>(true);

            Assert.IsNotNull(enemy.stats, "EnemyController.stats (EnemyStatsSO) mangler");
            Assert.IsNotNull(enemy.GetComponent<ResourceController>(), "Fjenden har ingen ResourceController, så den kan ikke tage skade");
            Assert.IsNotNull(enemy.GetComponent<Rigidbody2D>(), "Rigidbody2D mangler");
            Assert.IsNotNull(enemy.GetComponent<Collider2D>(), "Collider2D mangler, så kugler kan ikke ramme fjenden");
        }

        [TestCaseSource(nameof(Enemies))]
        public void RangedEnemy_CanShoot(string path)
        {
            var enemy = Load(path).GetComponentInChildren<EnemyController>(true);
            if (enemy.stats == null || enemy.stats.classSo != ClassSO.Ranged)
                Assert.Ignore("Ikke en ranged fjende");

            Assert.IsNotNull(enemy.attackPatterns, "attackPatterns mangler");
            Assert.IsNotEmpty(enemy.attackPatterns, "Ranged fjende uden attackPatterns skyder aldrig");
            Assert.IsFalse(enemy.attackPatterns.Any(p => p == null), "attackPatterns indeholder et tomt felt");
            Assert.IsNotNull(enemy.firePoint, "firePoint mangler, så kuglerne har intet sted at spawne");
        }

        [TestCaseSource(nameof(Players))]
        public void Player_IsSetUpCorrectly(string path)
        {
            var player = Load(path);
            var manager = player.GetComponent<PlayerManager>();

            Assert.IsNotNull(Field<PlayerStatsSO>(manager, "baseStats"), "PlayerManager.baseStats mangler (der bruges så default-stats)");
            Assert.IsNotNull(player.GetComponent<ResourceController>(), "ResourceController mangler");
            Assert.IsNotNull(player.GetComponent<Inventory>(), "Inventory mangler");
            Assert.IsNotNull(player.GetComponent<PlayerMovementController>(), "PlayerMovementController mangler");
            Assert.IsNotNull(player.GetComponent<Animator>(), "Animator mangler (bruges af movement og attack)");
            Assert.IsNotNull(player.GetComponent<SpriteRenderer>(), "SpriteRenderer mangler");
            Assert.IsTrue(player.CompareTag("Player"), "Spilleren skal have tagget 'Player' (bruges af fjender, kugler og minimap)");
        }

        [TestCaseSource(nameof(Players))]
        public void Player_FireballPrefabIsAWorkingBullet(string path)
        {
            var attack = Load(path).GetComponent<PlayerAttackController>();
            Assert.IsNotNull(attack, "PlayerAttackController mangler");

            var fireball = Field<GameObject>(attack, "fireballPrefab");
            Assert.IsNotNull(fireball, "fireballPrefab mangler, så PlayerAttackController.Awake fejler");
            Assert.IsNotNull(fireball.GetComponent<PlayerBullet>(), "fireballPrefab har ingen PlayerBullet");
        }

        [TestCaseSource(nameof(PlayerBullets))]
        public void PlayerBullet_IsSetUpCorrectly(string path)
        {
            var bullet = Load(path).GetComponent<PlayerBullet>();
            Assert.IsNotNull(bullet, "PlayerBullet skal sidde på roden af prefabben");

            Assert.IsNotNull(bullet.GetComponent<Animator>(), "Animator mangler (PlayerBullet.Launch sætter DirX/DirY)");
            Assert.IsNotNull(bullet.GetComponent<SpriteRenderer>(), "SpriteRenderer mangler (PlayerBullet.Launch sætter flipX)");
            Assert.IsNotNull(bullet.GetComponent<Collider2D>(), "Collider2D mangler, så kuglen kan ikke ramme noget");
            Assert.GreaterOrEqual(bullet.ManaCost, 0, "ManaCost");
        }

        [Test]
        public void BuildSettings_ScenesExist()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.IsNotEmpty(scenes, "Ingen scener i Build Settings");

            foreach (var scene in scenes)
                Assert.IsTrue(File.Exists(scene.path), $"Scenen '{scene.path}' i Build Settings findes ikke");

            Assert.IsTrue(scenes.Any(s => s.enabled), "Alle scener i Build Settings er slået fra");
        }
    }
}
