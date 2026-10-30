using System;
using System.Collections.Generic;
using Crookedile.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Whose statuses <see cref="IfHasStatusEffect"/> reads.</summary>
    public enum StatusHolder
    {
        Target, // The card's target (the focused enemy)
        You, // The player
    }

    /// <summary>
    /// Branches on a status: runs Then when the checked side has at least Min Stacks of it, Else
    /// otherwise ("If the target has Guilt: deal 4 more"; "If you have 2+ Glamour: draw 1"). Reads
    /// the stacks when it executes, so a status applied by an earlier effect on the same card counts.
    /// </summary>
    [Serializable]
    public class IfHasStatusEffect : BattleEffect
    {
        [Tooltip("Whose statuses to check: the card's target, or you.")]
        [SerializeField]
        private StatusHolder _check = StatusHolder.Target;

        [Tooltip("The status to look for.")]
        [SerializeReference]
        private StatusBehavior _status;

        [Tooltip("Then runs when the checked side has at least this many stacks.")]
        [MinValue(1)]
        [SerializeField]
        private int _minStacks = 1;

        [Tooltip("Run when the status is there.")]
        [SerializeReference]
        private List<BattleEffect> _then = new List<BattleEffect>();

        [Tooltip("Run when it isn't.")]
        [SerializeReference]
        private List<BattleEffect> _else = new List<BattleEffect>();

        /// <summary>
        /// Single-target when it reads the target, or when either branch is, so the card counts as
        /// singling an enemy out (the crowd's Hostility bump reads this).
        /// </summary>
        public override TargetType Target
        {
            get
            {
                if (_check == StatusHolder.Target)
                    return TargetType.Opponent;
                foreach (var list in new[] { _then, _else })
                    if (list != null)
                        foreach (var effect in list)
                            if (effect != null && effect.Target == TargetType.Opponent)
                                return TargetType.Opponent;
                return TargetType.Self;
            }
        }

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null) =>
            ExecuteAll(Holds(ctx) ? _then : _else, ctx);

        /// <summary>Met when the Then branch would run; an "Unless" (empty Then) has no bonus to show.</summary>
        public override bool IsConditionMet(EffectExecutionContext ctx) =>
            _then != null && _then.Count > 0 && Holds(ctx);

        private bool Holds(EffectExecutionContext ctx)
        {
            if (_status == null)
                return false;
            StatusEffectManager statuses =
                _check == StatusHolder.You ? ctx.PlayerStatusEffects
                : ctx.Target != ctx.PlayerStats ? ctx.TargetStatusEffects
                : null;
            return (statuses?.GetStacks(_status) ?? 0) >= _minStacks;
        }

        public override string GetDescription()
        {
            string name = _status?.DisplayName ?? "(no status)";
            string amount = _minStacks > 1 ? $"{_minStacks}+ {name}" : name;
            string test =
                _check == StatusHolder.You ? $"you have {amount}" : $"the target has {amount}";
            string then = DescribeAll(_then);
            string otherwise = DescribeAll(_else);
            if (otherwise.Length == 0)
                return $"If {test}: {then}";
            if (then.Length == 0)
                return $"Unless {test}: {otherwise}";
            return $"{otherwise}. If {test} instead: {then}";
        }

#if UNITY_EDITOR
        public override IEnumerable<string> GetConfigurationIssues()
        {
            if (_status == null)
                yield return "No status chosen; the branch never runs Then.";
            if ((_then == null || _then.Count == 0) && (_else == null || _else.Count == 0))
                yield return "Both Then and Else are empty; the effect does nothing.";
        }
#endif
    }
}
