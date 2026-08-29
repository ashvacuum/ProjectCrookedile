using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Which side of its NeutralZone an enemy sits on.</summary>
    public enum HostilityBand
    {
        Hostile,
        Receptive,
        Neutral,
    }

    /// <summary>How the counted enemies are measured against the board.</summary>
    public enum StanceCountMode
    {
        /// <summary>At least Count enemies are in the band.</summary>
        AtLeast,

        /// <summary>No more than Count enemies are in the band.</summary>
        AtMost,

        /// <summary>Every living enemy is in the band — the echo chamber's trip wire.</summary>
        All,

        /// <summary>More than half of living enemies are in the band.</summary>
        Majority,
    }

    /// <summary>
    /// Passes on the composition of the whole board rather than one enemy: "every living enemy is
    /// Receptive", "Hostile are the majority", "two or more are Neutral".
    ///
    /// Effects can already <em>scale</em> by board composition via a Per X Source of
    /// HostileEnemyCount / ReceptiveEnemyCount. This is the missing half — branching on it, so a
    /// passive can fire only once the board tips rather than paying out proportionally the whole
    /// way there.
    ///
    /// All and Majority are false on an empty board: with nobody left there is no composition to
    /// speak of, and a passive that fires when the fight is over reads as a bug.
    /// </summary>
    [Serializable]
    public class EnemyStanceCountCondition : PassiveConditionBase
    {
        [Tooltip("Which side of the NeutralZone to count.")]
        [EnumToggleButtons]
        [SerializeField]
        private HostilityBand _band = HostilityBand.Receptive;

        [Tooltip("How the count is measured against the living enemies.")]
        [EnumToggleButtons]
        [SerializeField]
        private StanceCountMode _mode = StanceCountMode.All;

        [Tooltip("The threshold, for At Least and At Most.")]
        [MinValue(0)]
        [ShowIf(
            "@_mode == StanceCountMode.AtLeast || _mode == StanceCountMode.AtMost"
        )]
        [SerializeField]
        private int _count = 1;

        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            if (ctx.Enemies == null)
                return false;

            int living = 0;
            int matching = 0;
            foreach (var enemy in ctx.Enemies)
            {
                if (enemy == null || enemy.IsDefeated || enemy.Stats == null)
                    continue;
                living++;
                if (InBand(enemy))
                    matching++;
            }

            return _mode switch
            {
                StanceCountMode.AtLeast => matching >= _count,
                StanceCountMode.AtMost => matching <= _count,
                StanceCountMode.All => living > 0 && matching == living,
                StanceCountMode.Majority => living > 0 && matching * 2 > living,
                _ => false,
            };
        }

        private bool InBand(EnemyController enemy) =>
            _band switch
            {
                HostilityBand.Hostile => enemy.Stats.IsHostile,
                HostilityBand.Receptive => enemy.Stats.IsReceptive,
                _ => !enemy.Stats.IsHostile && !enemy.Stats.IsReceptive,
            };

        public override string ConditionLabel =>
            _mode switch
            {
                StanceCountMode.All => $"every enemy is {_band}",
                StanceCountMode.Majority => $"most enemies are {_band}",
                StanceCountMode.AtMost => $"at most {_count} enemies are {_band}",
                _ => $"at least {_count} enemies are {_band}",
            };

#if UNITY_EDITOR
        /// <summary>
        /// Self-check for the counting rules, which are the only part with edge cases worth
        /// getting wrong. Run from Crookedile → Debug → Test Stance Count Condition.
        /// </summary>
        [UnityEditor.MenuItem("Crookedile/Debug/Test Stance Count Condition")]
        private static void SelfTest()
        {
            // (matching, living) → expected, per mode. Counting is pure arithmetic once the
            // band filter has run, so the cases that matter are the boundaries.
            Check(StanceCountMode.All, 0, 0, false, "all: empty board");
            Check(StanceCountMode.All, 2, 2, true, "all: everyone matches");
            Check(StanceCountMode.All, 1, 2, false, "all: one holdout");
            Check(StanceCountMode.Majority, 0, 0, false, "majority: empty board");
            Check(StanceCountMode.Majority, 2, 4, false, "majority: exactly half is not a majority");
            Check(StanceCountMode.Majority, 3, 4, true, "majority: three of four");
            Check(StanceCountMode.Majority, 1, 1, true, "majority: sole survivor");
            Debug.Log("EnemyStanceCountCondition self-test passed.");
        }

        private static void Check(
            StanceCountMode mode,
            int matching,
            int living,
            bool expected,
            string label
        )
        {
            bool actual = mode switch
            {
                StanceCountMode.All => living > 0 && matching == living,
                StanceCountMode.Majority => living > 0 && matching * 2 > living,
                _ => false,
            };
            if (actual != expected)
                throw new Exception($"EnemyStanceCountCondition self-test failed — {label}");
        }
#endif
    }
}
