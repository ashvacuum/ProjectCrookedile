using System.Collections.Generic;
using Crookedile.Gameplay.Battle;
using NUnit.Framework;

namespace Crookedile.Tests
{
    /// <summary>The battle's random stream replays from its seed.</summary>
    public class BattleRandomTests
    {
        private static List<int> Draw(int seed)
        {
            BattleRandom.Seed(seed);
            var draws = new List<int>();
            for (int i = 0; i < 20; i++)
                draws.Add(BattleRandom.Range(0, 1000));
            return draws;
        }

        [Test]
        public void SameSeedGivesTheSameDraws() => CollectionAssert.AreEqual(Draw(42), Draw(42));

        [Test]
        public void DifferentSeedsDiverge() => CollectionAssert.AreNotEqual(Draw(42), Draw(43));

        [Test]
        public void ChanceIsNeverAtZeroAndAlwaysAtOne()
        {
            BattleRandom.Seed(1);
            for (int i = 0; i < 200; i++)
            {
                Assert.IsFalse(BattleRandom.Chance(0f));
                Assert.IsTrue(BattleRandom.Chance(1f));
            }
        }
    }
}
