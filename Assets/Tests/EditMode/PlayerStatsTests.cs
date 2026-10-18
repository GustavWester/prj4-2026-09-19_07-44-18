using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>Unit tests af PlayerStatsSO. Kører i edit mode uden scene.</summary>
    public class PlayerStatsTests
    {
        private PlayerStatsSO stats;

        [SetUp]
        public void SetUp() => stats = ScriptableObject.CreateInstance<PlayerStatsSO>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(stats);

        [Test]
        public void Add_AppliesEveryField()
        {
            stats.maxHealth = 5;
            stats.maxMana = 100;
            stats.manaRegenPerSecond = 10f;
            stats.moveSpeed = 5f;
            stats.spellDamage = 0;

            stats.Add(new StatModifier { maxHealth = 2, maxMana = 20, manaRegenPerSecond = 1.5f, moveSpeed = 0.5f, spellDamage = 3 });

            Assert.AreEqual(7, stats.maxHealth);
            Assert.AreEqual(120, stats.maxMana);
            Assert.AreEqual(11.5f, stats.manaRegenPerSecond, 1e-5f);
            Assert.AreEqual(5.5f, stats.moveSpeed, 1e-5f);
            Assert.AreEqual(3, stats.spellDamage);
        }

        [Test]
        public void Add_EmptyModifier_ChangesNothing()
        {
            var before = JsonUtility.ToJson(stats);

            stats.Add(default);

            Assert.AreEqual(before, JsonUtility.ToJson(stats));
        }

        [Test]
        public void ItemStatModifier_SurvivesSerialization()
        {
            // StatModifier ligger i Game.Core-assemblyen. Tjekker at Unity stadig serialiserer den på ItemSO.
            var item = ScriptableObject.CreateInstance<ItemSO>();
            item.statModifier = new StatModifier { maxHealth = 4, spellDamage = 2 };

            var copy = ScriptableObject.CreateInstance<ItemSO>();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(item), copy);

            Assert.AreEqual(item.statModifier, copy.statModifier);
            Object.DestroyImmediate(item);
            Object.DestroyImmediate(copy);
        }
    }
}
