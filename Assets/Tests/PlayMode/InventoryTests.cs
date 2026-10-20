using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tests.PlayMode
{
    /// <summary>Inventory-komponenten i en tom testscene: Awake, events og starting items.</summary>
    public class InventoryTests
    {
        private TestScene scene;
        private ItemSO potion;
        private ItemSO sword;

        [SetUp]
        public void SetUp()
        {
            scene = new TestScene();
            potion = scene.Track(ScriptableObject.CreateInstance<ItemSO>());
            potion.itemName = "Potion";
            potion.maxStack = 5;
            sword = scene.Track(ScriptableObject.CreateInstance<ItemSO>());
            sword.itemName = "Sword";
            sword.maxStack = 1;
        }

        [TearDown]
        public void TearDown() => scene.Cleanup();

        private Inventory NewInventory(int capacity = 4, List<Inventory.Slot> startingItems = null)
        {
            var go = scene.Inactive("Inventory");
            var inventory = go.AddComponent<Inventory>();
            TestScene.SetField(inventory, "capacity", capacity);
            if (startingItems != null) TestScene.SetField(inventory, "startingItems", startingItems);
            go.SetActive(true);
            return inventory;
        }

        [Test]
        public void Awake_CreatesEmptySlotsMatchingCapacity()
        {
            var inventory = NewInventory(capacity: 6);

            Assert.AreEqual(6, inventory.Slots.Count);
            foreach (var slot in inventory.Slots)
                Assert.IsTrue(slot.IsEmpty);
        }

        [Test]
        public void Awake_AddsStartingItems()
        {
            var inventory = NewInventory(startingItems: new List<Inventory.Slot>
            {
                new Inventory.Slot { item = potion, count = 7 },
                new Inventory.Slot { item = null, count = 3 }, // tomme rækker i inspectoren springes over
                new Inventory.Slot { item = sword, count = 1 },
            });

            Assert.AreEqual(7, inventory.CountOf(potion));
            Assert.AreEqual(1, inventory.CountOf(sword));
            Assert.AreEqual(5, inventory.Slots[0].count);
            Assert.AreEqual(2, inventory.Slots[1].count);
            Assert.AreSame(sword, inventory.Slots[2].item);
        }

        [Test]
        public void Add_UsesItemMaxStack()
        {
            var inventory = NewInventory();

            int leftover = inventory.Add(potion, 6);

            Assert.AreEqual(0, leftover);
            Assert.AreEqual(5, inventory.Slots[0].count);
            Assert.AreEqual(1, inventory.Slots[1].count);
        }

        [Test]
        public void Add_WhenFull_ReturnsLeftover()
        {
            var inventory = NewInventory(capacity: 2);

            Assert.AreEqual(1, inventory.Add(sword, 3));
        }

        [Test]
        public void Add_FiresOnChanged()
        {
            var inventory = NewInventory();
            int calls = 0;
            inventory.onChanged.AddListener(() => calls++);

            inventory.Add(potion);

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Add_InvalidInput_DoesNotFireOnChanged()
        {
            var inventory = NewInventory();
            int calls = 0;
            inventory.onChanged.AddListener(() => calls++);

            inventory.Add(null);
            inventory.Add(potion, 0);

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void RemoveAt_RemovesAndFiresOnChanged()
        {
            var inventory = NewInventory();
            inventory.Add(potion, 3);
            int calls = 0;
            inventory.onChanged.AddListener(() => calls++);

            inventory.RemoveAt(0, 3);

            Assert.IsTrue(inventory.Slots[0].IsEmpty);
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void RemoveAt_EmptySlot_DoesNotFireOnChanged()
        {
            var inventory = NewInventory();
            int calls = 0;
            inventory.onChanged.AddListener(() => calls++);

            inventory.RemoveAt(2);
            inventory.RemoveAt(99);

            Assert.AreEqual(0, calls);
        }
    }
}
