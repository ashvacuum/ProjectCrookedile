using System;
using System.Collections.Generic;
using Crookedile.Data;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Branches on a live context value: runs Then when the comparison holds, Else otherwise
    /// ("8 Sway, or 12 if you pulled a card this turn"). Reads the value when it executes, so
    /// accumulators filled by earlier effects on the same card are visible.
    /// </summary>
    [Serializable]
    public class IfContextValueEffect : BattleEffect
    {
        [Tooltip("The value to test.")]
        [SerializeField]
        private EffectContextValue _value = EffectContextValue.CardsPulledThisTurn;

        [Tooltip("How to compare it against the threshold.")]
        [SerializeField]
        private ComparisonType _comparison = ComparisonType.AtLeast;

        [Tooltip("The threshold.")]
        [SerializeField]
        private int _threshold = 1;

        [Tooltip("Run when the comparison holds.")]
        [SerializeReference]
        private List<BattleEffect> _then = new List<BattleEffect>();

        [Tooltip("Run when it doesn't.")]
        [SerializeReference]
        private List<BattleEffect> _else = new List<BattleEffect>();

        /// <summary>
        /// Single-target when either branch is, so a branched attack still counts as singling an
        /// enemy out (the crowd's Hostility bump reads this).
        /// </summary>
        public override TargetType Target
        {
            get
            {
                foreach (var list in new[] { _then, _else })
                    if (list != null)
                        foreach (var effect in list)
                            if (effect != null && effect.Target == TargetType.Opponent)
                                return TargetType.Opponent;
                return TargetType.Self;
            }
        }

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int value = ctx.GetValue(_value);
            bool holds = _comparison switch
            {
                ComparisonType.AtLeast => value >= _threshold,
                ComparisonType.AtMost => value <= _threshold,
                ComparisonType.Equals => value == _threshold,
                _ => false,
            };
            ExecuteAll(holds ? _then : _else, ctx);
        }

        public override string GetDescription()
        {
            string test = _comparison switch
            {
                ComparisonType.AtLeast => $"{_value} is {_threshold}+",
                ComparisonType.AtMost => $"{_value} is {_threshold} or less",
                _ => $"{_value} is {_threshold}",
            };
            string then = DescribeAll(_then);
            string otherwise = DescribeAll(_else);
            if (otherwise.Length == 0)
                return $"If {test}: {then}";
            if (then.Length == 0)
                return $"Unless {test}: {otherwise}";
            return $"{otherwise}. If {test} instead: {then}";
        }
    }
}
