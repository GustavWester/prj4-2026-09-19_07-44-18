using System.Linq;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class InventoryStackingTests
    {
        // Simpelt item og slot, så reglerne kan testes uden ItemSO/ScriptableObject.
        private class Item { public string Name; public override string ToString() => Name; }

        private class Slot : IStackSlot<Item>
        {
            public Item Item { get; set; }
            public int Count { get; set; }
        }

        private static Slot[] Slots(int capacity) => Enumerable.Range(0, capacity).Select(_ => new Slot()).ToArray();

        private readonly Item potion = new Item { Name = "Potion" };
        private readonly Item sword = new Item { Name = "Sword" };

        [Test]
        public void Add_IntoEmptyInventory_PutsItemInFirstSlot()
        {
            var slots = Slots(3);

            int leftover = InventoryStacking.Add(slots, potion, 1, maxStack: 5);

            Assert.AreEqual(0, leftover);
            Assert.AreSame(potion, slots[0].Item);
            Assert.AreEqual(1, slots[0].Count);
            Assert.IsTrue(InventoryStacking.IsEmpty(slots[1]));
        }

        [Test]
        public void Add_SameItem_FillsExistingStackBeforeNewSlot()
        {
            var slots = Slots(3);
            InventoryStacking.Add(slots, potion, 3, maxStack: 5);

            InventoryStacking.Add(slots, potion, 2, maxStack: 5);

            Assert.AreEqual(5, slots[0].Count);
            Assert.IsTrue(InventoryStacking.IsEmpty(slots[1]));
        }

        [Test]
        public void Add_MoreThanMaxStack_SpillsIntoNextEmptySlot()
        {
            var slots = Slots(3);

            InventoryStacking.Add(slots, potion, 7, maxStack: 5);

            Assert.AreEqual(5, slots[0].Count);
            Assert.AreSame(potion, slots[1].Item);
            Assert.AreEqual(2, slots[1].Count);
        }

        [Test]
        public void Add_NonStackableItems_UseOneSlotEach()
        {
            var slots = Slots(3);

            InventoryStacking.Add(slots, sword, 2, maxStack: 1);

            Assert.AreEqual(1, slots[0].Count);
            Assert.AreEqual(1, slots[1].Count);
            Assert.AreSame(sword, slots[1].Item);
        }

        [Test]
        public void Add_WhenFull_ReturnsLeftover()
        {
            var slots = Slots(2);

            int leftover = InventoryStacking.Add(slots, potion, 13, maxStack: 5);

            Assert.AreEqual(3, leftover);
            Assert.AreEqual(10, InventoryStacking.CountOf(slots, potion));
        }

        [Test]
        public void Add_DifferentItems_DoNotShareStacks()
        {
            var slots = Slots(3);
            InventoryStacking.Add(slots, potion, 1, maxStack: 5);

            InventoryStacking.Add(slots, sword, 1, maxStack: 5);

            Assert.AreSame(potion, slots[0].Item);
            Assert.AreSame(sword, slots[1].Item);
            Assert.AreEqual(1, slots[0].Count);
        }

        [Test]
        public void Add_FillsGapLeftByRemovedItem()
        {
            var slots = Slots(3);
            InventoryStacking.Add(slots, potion, 1, maxStack: 1);
            InventoryStacking.Add(slots, sword, 1, maxStack: 1);
            InventoryStacking.RemoveAt(slots, 0, 1);

            InventoryStacking.Add(slots, sword, 1, maxStack: 1);

            Assert.AreSame(sword, slots[0].Item, "Det tomme hul forrest skal bruges først");
        }

        [Test]
        public void Add_PartialStackLaterInList_IsToppedUpBeforeEarlierEmptySlot()
        {
            var slots = Slots(3);
            slots[2].Item = potion;
            slots[2].Count = 4;

            InventoryStacking.Add(slots, potion, 1, maxStack: 5);

            Assert.AreEqual(5, slots[2].Count);
            Assert.IsTrue(InventoryStacking.IsEmpty(slots[0]));
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void Add_NonPositiveAmount_ChangesNothingAndReturnsAmount(int amount)
        {
            var slots = Slots(2);

            int leftover = InventoryStacking.Add(slots, potion, amount, maxStack: 5);

            Assert.AreEqual(amount, leftover);
            Assert.IsTrue(slots.All(s => InventoryStacking.IsEmpty(s)));
        }

        [Test]
        public void Add_NullItem_ChangesNothing()
        {
            var slots = Slots(2);

            int leftover = InventoryStacking.Add(slots, null, 4, maxStack: 5);

            Assert.AreEqual(4, leftover);
            Assert.IsTrue(slots.All(s => InventoryStacking.IsEmpty(s)));
        }

        [Test]
        public void Add_MaxStackBelowOne_IsTreatedAsOne()
        {
            var slots = Slots(2);

            int leftover = InventoryStacking.Add(slots, potion, 2, maxStack: 0);

            Assert.AreEqual(0, leftover);
            Assert.AreEqual(1, slots[0].Count);
            Assert.AreEqual(1, slots[1].Count);
        }

        [Test]
        public void RemoveAt_PartOfStack_LeavesRest()
        {
            var slots = Slots(2);
            InventoryStacking.Add(slots, potion, 4, maxStack: 5);

            bool removed = InventoryStacking.RemoveAt(slots, 0, 3);

            Assert.IsTrue(removed);
            Assert.AreEqual(1, slots[0].Count);
            Assert.AreSame(potion, slots[0].Item);
        }

        [Test]
        public void RemoveAt_WholeStackOrMore_ClearsSlot()
        {
            var slots = Slots(2);
            InventoryStacking.Add(slots, potion, 2, maxStack: 5);

            InventoryStacking.RemoveAt(slots, 0, 10);

            Assert.IsNull(slots[0].Item);
            Assert.AreEqual(0, slots[0].Count);
        }

        [TestCase(-1)]
        [TestCase(2)]
        public void RemoveAt_IndexOutOfRange_ReturnsFalse(int index)
        {
            var slots = Slots(2);
            InventoryStacking.Add(slots, potion, 1, maxStack: 5);

            Assert.IsFalse(InventoryStacking.RemoveAt(slots, index, 1));
            Assert.AreEqual(1, slots[0].Count);
        }

        [Test]
        public void RemoveAt_EmptySlot_ReturnsFalse()
        {
            var slots = Slots(2);

            Assert.IsFalse(InventoryStacking.RemoveAt(slots, 1, 1));
        }

        [Test]
        public void RemoveAt_NonPositiveAmount_ReturnsFalse()
        {
            var slots = Slots(2);
            InventoryStacking.Add(slots, potion, 3, maxStack: 5);

            Assert.IsFalse(InventoryStacking.RemoveAt(slots, 0, 0));
            Assert.AreEqual(3, slots[0].Count);
        }

        [Test]
        public void CountOf_SumsAcrossSlots()
        {
            var slots = Slots(4);
            InventoryStacking.Add(slots, potion, 7, maxStack: 3);
            InventoryStacking.Add(slots, sword, 1, maxStack: 1);

            Assert.AreEqual(7, InventoryStacking.CountOf(slots, potion));
            Assert.AreEqual(0, InventoryStacking.CountOf(slots, new Item { Name = "Unknown" }));
        }

        [Test]
        public void IsEmpty_SlotWithItemButZeroCount_IsEmpty()
        {
            var slot = new Slot { Item = potion, Count = 0 };

            Assert.IsTrue(InventoryStacking.IsEmpty(slot));
        }
    }
}
