using NUnit.Framework;

namespace Game.Core.Tests
{
    public class EnemyAIMathTests
    {
        [Test]
        public void PreferredDistance_IsJustInsideAttackRange()
        {
            Assert.AreEqual(4f, EnemyAIMath.PreferredDistance(5f), 1e-6f);
        }

        // attackRange 5 -> foretrukken afstand 4, tolerance 0.5
        [TestCase(10f, 1)]   // for langt væk -> gå tættere på
        [TestCase(4.6f, 1)]
        [TestCase(4.5f, 0)]  // præcis på kanten af tolerancen -> bliv
        [TestCase(4f, 0)]
        [TestCase(3.5f, 0)]
        [TestCase(3.4f, -1)]
        [TestCase(1f, -1)]   // for tæt på -> træk dig tilbage
        public void KeepDistanceStep(float distance, int expected)
        {
            Assert.AreEqual(expected, EnemyAIMath.KeepDistanceStep(distance, attackRange: 5f, tolerance: 0.5f));
        }

        [Test]
        public void ShouldChase_OutsideDetectionRange_IsFalse()
        {
            Assert.IsFalse(EnemyAIMath.ShouldChase(distance: 7f, detectionRange: 6f, isMelee: false, attackRange: 1.5f));
        }

        [Test]
        public void ShouldChase_RangedInsideDetection_KeepsChasingEvenWhenClose()
        {
            Assert.IsTrue(EnemyAIMath.ShouldChase(distance: 0.5f, detectionRange: 6f, isMelee: false, attackRange: 1.5f));
        }

        [Test]
        public void ShouldChase_MeleeStopsJustInsideAttackRange()
        {
            Assert.IsTrue(EnemyAIMath.ShouldChase(distance: 1.3f, detectionRange: 6f, isMelee: true, attackRange: 1.5f));
            Assert.IsFalse(EnemyAIMath.ShouldChase(distance: 1.2f, detectionRange: 6f, isMelee: true, attackRange: 1.5f));
            Assert.IsFalse(EnemyAIMath.ShouldChase(distance: 0.1f, detectionRange: 6f, isMelee: true, attackRange: 1.5f));
        }

        [Test]
        public void ShouldChase_ExactlyAtDetectionRange_IsTrue()
        {
            Assert.IsTrue(EnemyAIMath.ShouldChase(6f, 6f, isMelee: false, attackRange: 1.5f));
        }

        [TestCase(1f, 0.3f, 1.3f)]
        [TestCase(1f, -0.3f, 0.7f)]
        [TestCase(0.1f, -0.3f, EnemyAIMath.MinAttackInterval)] // jitter må aldrig give 0 eller negativ ventetid
        [TestCase(0f, 0f, EnemyAIMath.MinAttackInterval)]
        public void AttackInterval(float cooldown, float jitter, float expected)
        {
            Assert.AreEqual(expected, EnemyAIMath.AttackInterval(cooldown, jitter), 1e-5f);
        }

        // '#' = væg/dekoration, alt andet er gangbart. Række i = y, tegn j = x
        private static bool[,] Grid(params string[] rows)
        {
            var g = new bool[rows[0].Length, rows.Length];
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    g[x, y] = rows[y][x] != '#';
            return g;
        }

        // spilleren står over øverste række, så kun y == 0 har udsyn
        private static bool TopRowHasLos(int x, int y) => y == 0;

        [Test]
        public void NextStepTowardLos_StartHasLos_ReturnsStart()
        {
            var grid = Grid("....", "....");
            Assert.AreEqual((2, 0), EnemyAIMath.NextStepTowardLos(grid, 2, 0, TopRowHasLos));
        }

        [Test]
        public void NextStepTowardLos_BehindWall_StepsAroundIt()
        {
            var grid = Grid(
                "....",
                "###.",  // eneste vej op er i højre side
                "....");
            // fjenden står nederst til venstre og må ikke gå op i væggen, men mod højre
            Assert.AreEqual((1, 2), EnemyAIMath.NextStepTowardLos(grid, 0, 2, TopRowHasLos));
        }

        [Test]
        public void NextStepTowardLos_NoReachableCellWithLos_ReturnsNull()
        {
            var grid = Grid(
                "....",
                "####",
                "....");
            Assert.IsNull(EnemyAIMath.NextStepTowardLos(grid, 0, 2, TopRowHasLos));
        }

        [Test]
        public void NextStepTowardLos_StartOutsideGrid_ReturnsNull()
        {
            Assert.IsNull(EnemyAIMath.NextStepTowardLos(Grid("...."), 5, 0, TopRowHasLos));
        }
    }
}
