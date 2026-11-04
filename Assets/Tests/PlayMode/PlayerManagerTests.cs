using NUnit.Framework;
using UnityEngine;

namespace Tests.PlayMode
{
    /// <summary>PlayerManager: stats fra PlayerStatsSO, equip/unequip og at assetten aldrig ændres.</summary>
    public class PlayerManagerTests
    {
        private TestScene scene;
        private PlayerStatsSO baseStats;

        [SetUp]
        public void SetUp()
        {
            scene = new TestScene();
            baseStats = scene.Track(ScriptableObject.CreateInstance<PlayerStatsSO>());
            baseStats.maxHealth = 5;
            baseStats.maxMana = 100;
            baseStats.manaRegenPerSecond = 10f;
            baseStats.moveSpeed = 5f;
            baseStats.spellDamage = 1;
        }

        [TearDown]
        public void TearDown() => scene.Cleanup();

        private PlayerManager NewPlayer()
        {
            var go = scene.Inactive("Player");
            var manager = go.AddComponent<PlayerManager>(); // RequireComponent tilføjer ResourceController
            TestScene.SetField(manager, "baseStats", baseStats);
            go.SetActive(true);
            return manager;
        }

        private ItemSO NewItem(StatModifier mod)
        {
            var item = scene.Track(ScriptableObject.CreateInstance<ItemSO>());
            item.statModifier = mod;
            return item;
        }

        [Test]
        public void Awake_PushesStatsToResourceController()
        {
            var player = NewPlayer();

            Assert.AreEqual(5, player.MaxHealth);
            Assert.AreEqual(5, player.Resources.MaxHealth);
            Assert.AreEqual(5, player.Resources.CurrentHealth);
            Assert.AreEqual(100, player.Resources.MaxMana);
            Assert.AreEqual(100, player.Resources.CurrentMana);
            Assert.AreEqual(5f, player.MoveSpeed);
            Assert.AreEqual(1, player.SpellDamage);
        }

        [Test]
        public void Equip_AddsItemBonus()
        {
            var player = NewPlayer();
            var ring = NewItem(new StatModifier { maxHealth = 3, spellDamage = 2, moveSpeed = 1f });

            player.Equip(ring);

            Assert.AreEqual(8, player.MaxHealth);
            Assert.AreEqual(8, player.Resources.MaxHealth);
            Assert.AreEqual(8, player.Resources.CurrentHealth, "+3 max liv giver også +3 nuværende liv");
            Assert.AreEqual(3, player.SpellDamage);
            Assert.AreEqual(6f, player.MoveSpeed);
        }

        [Test]
        public void Unequip_RestoresOriginalStats()
        {
            var player = NewPlayer();
            var ring = NewItem(new StatModifier { maxHealth = 3, maxMana = 20, spellDamage = 2 });

            player.Equip(ring);
            player.Unequip(ring);

            Assert.AreEqual(5, player.MaxHealth);
            Assert.AreEqual(5, player.Resources.CurrentHealth);
            Assert.AreEqual(100, player.Resources.MaxMana);
            Assert.AreEqual(1, player.SpellDamage);
        }

        [Test]
        public void Unequip_ItemThatWasNeverEquipped_ChangesNothing()
        {
            var player = NewPlayer();
            int changes = 0;
            player.StatsChanged += () => changes++;

            player.Unequip(NewItem(new StatModifier { maxHealth = 3 }));

            Assert.AreEqual(5, player.MaxHealth);
            Assert.AreEqual(0, changes);
        }

        [Test]
        public void TwoOfSameItem_StackAndRemoveOneAtATime()
        {
            var player = NewPlayer();
            var ring = NewItem(new StatModifier { spellDamage = 2 });

            player.Equip(ring);
            player.Equip(ring);
            Assert.AreEqual(5, player.SpellDamage);

            player.Unequip(ring);
            Assert.AreEqual(3, player.SpellDamage);
        }

        [Test]
        public void Equip_FiresStatsChanged()
        {
            var player = NewPlayer();
            int changes = 0;
            player.StatsChanged += () => changes++;

            player.Equip(NewItem(new StatModifier { moveSpeed = 1f }));

            Assert.AreEqual(1, changes);
        }

        [Test]
        public void Equip_DoesNotModifyTheStatsAsset()
        {
            var player = NewPlayer();

            player.Equip(NewItem(new StatModifier { maxHealth = 10, spellDamage = 10 }));

            Assert.AreEqual(5, baseStats.maxHealth, "PlayerStatsSO-assetten må aldrig ændres under spillet");
            Assert.AreEqual(1, baseStats.spellDamage);
        }

        [Test]
        public void Equip_Null_IsIgnored()
        {
            var player = NewPlayer();

            Assert.DoesNotThrow(() => player.Equip(null));
            Assert.DoesNotThrow(() => player.Unequip(null));
            Assert.AreEqual(5, player.MaxHealth);
        }

        [Test]
        public void Damaged_ThenEquipHealthItem_KeepsDamage()
        {
            var player = NewPlayer();
            player.Resources.TakeDamage(2); // 3/5

            player.Equip(NewItem(new StatModifier { maxHealth = 5 }));

            Assert.AreEqual(8, player.Resources.CurrentHealth);
            Assert.AreEqual(10, player.Resources.MaxHealth);
        }
    }
}
