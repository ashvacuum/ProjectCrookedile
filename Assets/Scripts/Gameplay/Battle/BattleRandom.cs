using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// The battle's random stream. Every rule that rolls (shuffles, random targets, enemy move
    /// picks, chance effects) draws from here, so one seed replays one battle exactly, and
    /// cosmetic draws on <see cref="UnityEngine.Random"/> (audio pitch, VFX) cannot shift it.
    /// <see cref="BattleManager.StartBattle"/> seeds it from <see cref="BattleSetup.seed"/>.
    ///
    /// ponytail: static because one battle runs at a time (as <see cref="BattleManager.Current"/>
    /// already assumes). Make it an instance on BattleManager if battles ever run side by side.
    /// </summary>
    public static class BattleRandom
    {
        private static System.Random _rng;

        // Unseeded use (a test poking one effect) falls back to a seed drawn from Unity's stream.
        private static System.Random Rng => _rng ??= new System.Random(UnitySeed());

        /// <summary>A seed from Unity's stream, which the playtest harness seeds per battle.</summary>
        public static int UnitySeed() => Random.Range(int.MinValue, int.MaxValue);

        /// <summary>Restarts the stream; the same seed gives the same draws.</summary>
        public static void Seed(int seed) => _rng = new System.Random(seed);

        /// <summary>An int in [<paramref name="min"/>, <paramref name="maxExclusive"/>).</summary>
        public static int Range(int min, int maxExclusive) => Rng.Next(min, maxExclusive);

        /// <summary>A float in [0, 1).</summary>
        public static float Value => (float)Rng.NextDouble();

        /// <summary>True with the given probability: 0 never, 1 always.</summary>
        public static bool Chance(float probability) => Value < probability;
    }
}
