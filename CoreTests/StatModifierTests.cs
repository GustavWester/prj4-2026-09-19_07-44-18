using NUnit.Framework;

namespace Game.Core.Tests
{
    public class StatModifierTests
    {
        [Test]
        public void Plus_AddsEveryField()
        {
            var a = new StatModifier { maxHealth = 1, maxMana = 10, manaRegenPerSecond = 0.5f, moveSpeed = 1f, spellDamage = 2 };
            var b = new StatModifier { maxHealth = 2, maxMana = 5, manaRegenPerSecond = 1.5f, moveSpeed = -0.5f, spellDamage = 3 };

            var sum = a + b;

            Assert.AreEqual(3, sum.maxHealth);
            Assert.AreEqual(15, sum.maxMana);
            Assert.AreEqual(2f, sum.manaRegenPerSecond, 1e-6f);
            Assert.AreEqual(0.5f, sum.moveSpeed, 1e-6f);
            Assert.AreEqual(5, sum.spellDamage);
        }

        [Test]
        public void Default_IsNeutral()
        {
            var a = new StatModifier { maxHealth = 4, moveSpeed = 2f };

            Assert.AreEqual(a, a + default(StatModifier));
        }

        [Test]
        public void Plus_IsCommutative()
        {
            var a = new StatModifier { maxHealth = 4, spellDamage = 1 };
            var b = new StatModifier { maxMana = 20, moveSpeed = 1.5f };

            Assert.AreEqual(a + b, b + a);
        }

        [Test]
        public void EqualModifiers_AreEqual()
        {
            // PlayerManager.RemoveModifier bruger List.Remove, som afhænger af værdi-lighed
            var a = new StatModifier { maxHealth = 2, spellDamage = 1 };
            var b = new StatModifier { maxHealth = 2, spellDamage = 1 };

            Assert.AreEqual(a, b);
        }
    }
}
