using NUnit.Framework;

namespace Game.Core.Tests
{
    public class ResourceMathTests
    {
        [TestCase(10, 3, 7)]
        [TestCase(10, 10, 0)]
        [TestCase(10, 25, 0)] // aldrig under 0
        [TestCase(10, 0, 10)]
        [TestCase(10, -5, 10)] // negativ skade må ikke heale
        public void ApplyDamage(int current, int amount, int expected)
        {
            Assert.AreEqual(expected, ResourceMath.ApplyDamage(current, amount));
        }

        [TestCase(5, 3, 10, 8)]
        [TestCase(8, 5, 10, 10)] // aldrig over max
        [TestCase(5, 0, 10, 5)]
        [TestCase(5, -2, 10, 5)] // negativ heal må ikke give skade
        public void ApplyHeal(int current, int amount, int max, int expected)
        {
            Assert.AreEqual(expected, ResourceMath.ApplyHeal(current, amount, max));
        }

        [Test]
        public void ResizeCurrent_HigherMax_RaisesCurrentBySameAmount()
        {
            Assert.AreEqual(15, ResourceMath.ResizeCurrent(current: 5, oldMax: 10, newMax: 20, minCurrent: 1));
        }

        [Test]
        public void ResizeCurrent_LowerMax_LowersCurrentBySameAmount()
        {
            Assert.AreEqual(5, ResourceMath.ResizeCurrent(current: 10, oldMax: 20, newMax: 15, minCurrent: 1));
        }

        [Test]
        public void ResizeCurrent_NeverBelowMinimum()
        {
            // Liv: et item der fjernes må aldrig slå spilleren ihjel
            Assert.AreEqual(1, ResourceMath.ResizeCurrent(current: 2, oldMax: 20, newMax: 5, minCurrent: 1));
            // Mana: må gerne ende på 0
            Assert.AreEqual(0, ResourceMath.ResizeCurrent(current: 2, oldMax: 20, newMax: 5, minCurrent: 0));
        }

        [Test]
        public void ResizeCurrent_NeverAboveNewMax()
        {
            Assert.AreEqual(5, ResourceMath.ResizeCurrent(current: 20, oldMax: 10, newMax: 5, minCurrent: 0));
        }

        [Test]
        public void ResizeCurrent_EquipThenUnequip_RestoresOriginalValue()
        {
            int afterEquip = ResourceMath.ResizeCurrent(7, oldMax: 10, newMax: 20, minCurrent: 1);
            int afterUnequip = ResourceMath.ResizeCurrent(afterEquip, oldMax: 20, newMax: 10, minCurrent: 1);

            Assert.AreEqual(7, afterUnequip);
        }

        [TestCase(5, 10, 0.5f)]
        [TestCase(0, 10, 0f)]
        [TestCase(10, 10, 1f)]
        [TestCase(5, 0, 0f)] // ingen division med 0
        [TestCase(5, -1, 0f)]
        public void Percent(int current, int max, float expected)
        {
            Assert.AreEqual(expected, ResourceMath.Percent(current, max), 1e-6f);
        }
    }

    public class RegenAccumulatorTests
    {
        [Test]
        public void Tick_LessThanOnePoint_GivesNothingButKeepsFraction()
        {
            var regen = new RegenAccumulator();

            int gained = regen.Tick(perSecond: 10f, deltaTime: 0.05f); // 0.5 point

            Assert.AreEqual(0, gained);
            Assert.AreEqual(0.5f, regen.Buffer, 1e-5f);
        }

        [Test]
        public void Tick_FractionsAddUpOverFrames()
        {
            var regen = new RegenAccumulator();
            int total = 0;

            // 60 frames á 1/60 sek. med 10 mana/sek. = 10 mana på et sekund
            for (int i = 0; i < 60; i++)
                total += regen.Tick(10f, 1f / 60f);

            Assert.That(total, Is.InRange(9, 10), "Afrunding må højst koste ét point");
            Assert.That(total + regen.Buffer, Is.EqualTo(10f).Within(1e-3f));
        }

        [Test]
        public void Tick_LargeFrame_GivesSeveralPointsAtOnce()
        {
            var regen = new RegenAccumulator();

            Assert.AreEqual(3, regen.Tick(perSecond: 7f, deltaTime: 0.5f)); // 3.5
            Assert.AreEqual(0.5f, regen.Buffer, 1e-5f);
        }

        [TestCase(0f, 1f)]
        [TestCase(-5f, 1f)]
        [TestCase(5f, 0f)]
        [TestCase(5f, -1f)]
        public void Tick_NoRegenOrNoTime_GivesNothing(float perSecond, float deltaTime)
        {
            var regen = new RegenAccumulator();

            Assert.AreEqual(0, regen.Tick(perSecond, deltaTime));
            Assert.AreEqual(0f, regen.Buffer);
        }

        [Test]
        public void Reset_ClearsBuffer()
        {
            var regen = new RegenAccumulator();
            regen.Tick(1f, 0.9f);

            regen.Reset();

            Assert.AreEqual(0f, regen.Buffer);
        }
    }
}
