using System.Linq;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class BulletPatternMathTests
    {
        private const float Eps = 1e-4f;

        [Test]
        public void RadialAngles_FullCircle_IsEvenlySpaced()
        {
            var angles = BulletPatternMath.RadialAngles(bulletCount: 4, spreadAngle: 360f, rotationOffset: 0f);

            CollectionAssert.AreEqual(new[] { 0f, 90f, 180f, 270f }, angles);
        }

        [Test]
        public void RadialAngles_FullCircle_DoesNotDoubleUpAt360()
        {
            var angles = BulletPatternMath.RadialAngles(16, 360f, 0f);

            Assert.AreEqual(16, angles.Count);
            Assert.Less(angles.Last(), 360f, "Sidste kugle må ikke ligge oven i den første");
        }

        [Test]
        public void RadialAngles_WithOffset_RotatesEveryBullet()
        {
            var angles = BulletPatternMath.RadialAngles(4, 360f, rotationOffset: 10f);

            CollectionAssert.AreEqual(new[] { 10f, 100f, 190f, 280f }, angles);
        }

        [TestCase(0)]
        [TestCase(-2)]
        public void RadialAngles_NoBullets_ReturnsEmpty(int count)
        {
            Assert.IsEmpty(BulletPatternMath.RadialAngles(count, 360f, 0f));
        }

        [Test]
        public void SpiralOffset_GrowsWithEachVolley()
        {
            Assert.AreEqual(0f, BulletPatternMath.SpiralOffset(0, 20f));
            Assert.AreEqual(20f, BulletPatternMath.SpiralOffset(1, 20f));
            Assert.AreEqual(60f, BulletPatternMath.SpiralOffset(3, 20f));
        }

        [Test]
        public void AimedSpread_IsCenteredOnTarget()
        {
            var angles = BulletPatternMath.AimedSpreadAngles(bulletCount: 3, spreadAngle: 30f, baseAngle: 90f);

            Assert.AreEqual(75f, angles[0], Eps);
            Assert.AreEqual(90f, angles[1], Eps);
            Assert.AreEqual(105f, angles[2], Eps);
        }

        [Test]
        public void AimedSpread_EdgesMatchSpreadAngle()
        {
            var angles = BulletPatternMath.AimedSpreadAngles(5, 60f, baseAngle: 0f);

            Assert.AreEqual(-30f, angles.First(), Eps);
            Assert.AreEqual(30f, angles.Last(), Eps);
            Assert.AreEqual(60f, angles.Last() - angles.First(), Eps);
        }

        [Test]
        public void AimedSpread_SingleBullet_GoesStraightAtTarget()
        {
            var angles = BulletPatternMath.AimedSpreadAngles(1, 45f, baseAngle: 135f);

            Assert.AreEqual(1, angles.Count);
            Assert.AreEqual(135f, angles[0], Eps);
        }

        [Test]
        public void Curtain_LeavesOneGap()
        {
            var angles = BulletPatternMath.CurtainAngles(bulletCount: 4, spreadAngle: 360f, gapIndex: 2);

            CollectionAssert.AreEqual(new[] { 0f, 90f, 270f }, angles);
        }

        [Test]
        public void Curtain_GapOutsideRange_FiresEveryBullet()
        {
            var angles = BulletPatternMath.CurtainAngles(4, 360f, gapIndex: 99);

            Assert.AreEqual(4, angles.Count);
        }

        [TestCase(0, 3, 1)]
        [TestCase(1, 3, 2)]
        [TestCase(2, 3, 0)] // starter forfra
        [TestCase(0, 1, 0)]
        [TestCase(5, 0, 0)] // ingen patterns
        public void NextIndex_CyclesThroughPatterns(int current, int count, int expected)
        {
            Assert.AreEqual(expected, BulletPatternMath.NextIndex(current, count));
        }
    }
}
