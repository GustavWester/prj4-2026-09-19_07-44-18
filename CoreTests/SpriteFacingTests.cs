using NUnit.Framework;

namespace Game.Core.Tests
{
    public class SpriteFacingTests
    {
        [TestCase(1f, 0f, 1f, 0f, false)]       // højre
        [TestCase(-1f, 0f, 1f, 0f, true)]       // venstre = Right spejlet
        [TestCase(0f, 1f, 0f, 1f, false)]       // op = Behind
        [TestCase(0f, -1f, 0f, -1f, false)]     // ned = Front
        [TestCase(0.9f, 0.3f, 1f, 0f, false)]   // mest vandret -> højre
        [TestCase(-0.3f, -0.9f, 0f, -1f, false)] // mest lodret -> Front, ikke spejlet
        [TestCase(0.7f, 0.7f, 1f, 0f, false)]   // præcis diagonal -> vandret vinder
        [TestCase(-0.7f, 0.7f, 1f, 0f, true)]
        public void From_SnapsToNearestAnimation(float x, float y, float dirX, float dirY, bool flipX)
        {
            var facing = SpriteFacing.From(x, y);

            Assert.AreEqual(dirX, facing.DirX, "DirX");
            Assert.AreEqual(dirY, facing.DirY, "DirY");
            Assert.AreEqual(flipX, facing.FlipX, "FlipX");
        }

        [Test]
        public void From_ZeroVector_FacesRight()
        {
            var facing = SpriteFacing.From(0f, 0f);

            Assert.AreEqual(1f, facing.DirX);
            Assert.AreEqual(0f, facing.DirY);
            Assert.IsFalse(facing.FlipX);
        }
    }
}
