using System;
using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Explicitly attempts the Faith Leader pacify conversion on the chosen targets —
    /// the authored replacement for the old hidden auto-convert that fired whenever a
    /// pacify status landed. Author this on a card (e.g. a Policy "Cast Out"): if the
    /// target's Guilt+Shame+Doubt stacks meet the threshold (3 + its Jaded stacks), the
    /// stacks are consumed and the enemy converts (Fanatic burst → reverts to neutral,
    /// permanent Jaded +1; Hardened enemies are Silenced instead). Below the threshold
    /// the play is a no-op on that target.
    /// </summary>
    [Serializable]
    public class ConvertPacifiedEffect : BattleEffect
    {
        [Tooltip("Which enemies to attempt the conversion on.")]
        [SerializeField]
        private TargetType _target = TargetType.Opponent;

        public override TargetType Target => _target;

        [Tooltip(
            "Which statuses fuel this conversion (counted toward the threshold AND consumed, "
                + "in this order). Leave EMPTY for the default Guilt/Shame/Doubt trio."
        )]
        [SerializeReference]
        private List<StatusBehavior> _statusesToConvert = new List<StatusBehavior>();

        [Min(0)]
        [Tooltip(
            "Max stacks consumed per target. 0 = consume everything. The threshold "
                + "(3 + award-status stacks) is still checked against the target's FULL total — "
                + "the cap only limits what is spent, so leftovers stay and the burst scales "
                + "with what was eaten."
        )]
        [SerializeField]
        private int _maxStacksToConsume = 0;

        [Tooltip(
            "Permanent status stamped on the enemy after a successful conversion — the "
                + "escalator that also raises this card's threshold by its current stacks. "
                + "Leave EMPTY for the default Jaded."
        )]
        [SerializeReference]
        private StatusBehavior _awardStatus;

        [Min(0)]
        [Tooltip("Stacks of the award status applied per conversion. 0 = award nothing (no escalation).")]
        [SerializeField]
        private int _awardStacks = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.BattleManager == null)
            {
                GameLogger.LogWarning<ConvertPacifiedEffect>("No BattleManager in context — no-op");
                return;
            }

            foreach (var (targetStats, statusMgr) in ctx.GetTargets(_target))
            {
                if (targetStats == null || statusMgr == null || targetStats == ctx.PlayerStats)
                    continue;

                ctx.BattleManager.TryPacifyConvert(
                    targetStats,
                    statusMgr,
                    _statusesToConvert,
                    _maxStacksToConsume,
                    _awardStatus,
                    _awardStacks
                );
            }
        }

        public override string GetDescription()
        {
            string targetStr = _target == TargetType.Opponent ? "an enemy" : $"{_target}";
            string fuel =
                _statusesToConvert == null || _statusesToConvert.Count == 0
                    ? "Guilt/Shame/Doubt"
                    : DescribeStatusList();
            string cap = _maxStacksToConsume > 0 ? $" (max {_maxStacksToConsume} stacks)" : "";
            string award = _awardStatus?.DisplayName ?? "Jaded";
            return $"Convert {targetStr} with enough {fuel} (3 + {award}) — "
                + $"consume the stacks{cap} for an opinion burst.";
        }

        private string DescribeStatusList()
        {
            var names = new List<string>();
            foreach (var status in _statusesToConvert)
                if (status != null)
                    names.Add(status.DisplayName);
            return names.Count > 0 ? string.Join("/", names) : "Guilt/Shame/Doubt";
        }
    }
}
