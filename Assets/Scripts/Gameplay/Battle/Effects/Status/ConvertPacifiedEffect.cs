using System;
using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Explicitly attempts the Faith Leader pacify conversion on the chosen targets —
    /// the authored replacement for the old hidden auto-convert. The whole recipe lives here:
    ///
    ///   FUEL: the statuses that count toward the threshold and are consumed (empty = the
    ///   Guilt/Shame/Doubt trio). Threshold = 3 + the target's Buffer Status stacks.
    ///
    ///   AWARDS: statuses stamped on the enemy after a successful conversion, e.g. Jaded
    ///   (the tax) + Fanatic (the converted state). With Stacks Per Award = N, each award's
    ///   stacks are multiplied by consumed/N — big conversions mint more.
    ///
    /// The engine consumes fuel, fires the opinion burst (3 x consumed), and reverts the
    /// enemy to neutral; Hardened targets are Silenced instead (fuel spent, no awards).
    /// </summary>
    [Serializable]
    public class ConvertPacifiedEffect : BattleEffect
    {
        /// <summary>One status stamped on the enemy after a successful conversion.</summary>
        [Serializable]
        public class AwardEntry
        {
            [Tooltip("The status to award.")]
            [SerializeReference]
            public StatusBehavior Behavior;

            [Min(1)]
            [Tooltip("Base stacks (multiplied by consumed/StacksPerAward when that is set).")]
            public int Stacks = 1;

            [Tooltip("Duration of the awarded status (Jaded wants Permanent; Fanatic a decay).")]
            public StatusDurationType Duration = StatusDurationType.Permanent;
        }

        [Tooltip("Which enemies to attempt the conversion on.")]
        [SerializeField]
        private TargetType _target = TargetType.Opponent;

        public override TargetType Target => _target;

        [Tooltip(
            "FUEL: statuses that count toward the threshold and are consumed (in this order). "
                + "Leave EMPTY for the default Guilt/Shame/Doubt trio."
        )]
        [SerializeReference]
        private List<StatusBehavior> _statusesToConvert = new List<StatusBehavior>();

        [Tooltip(
            "BUFFER: the escalator status — the conversion threshold is 3 + the target's "
                + "current stacks of it. Leave EMPTY for the default Jaded. Award it below or "
                + "conversions never get more expensive."
        )]
        [SerializeReference]
        private StatusBehavior _bufferStatus;

        [Min(0)]
        [Tooltip(
            "Award rate: every this-many consumed fuel stacks multiplies the award stacks by 1 "
                + "(e.g. 3 → a 6-stack conversion doubles every award). 0 = flat: each award "
                + "applied once at its authored stacks regardless of fuel size."
        )]
        [SerializeField]
        private int _stacksPerAward = 0;

        [Tooltip(
            "AWARDS: statuses stamped on the enemy after a successful conversion. Leave EMPTY "
                + "for the default (1 permanent stack of the Buffer Status, i.e. Jaded)."
        )]
        [SerializeField]
        private List<AwardEntry> _awards = new List<AwardEntry>();

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

                var outcome = ctx.BattleManager.TryPacifyConvert(
                    targetStats,
                    statusMgr,
                    _statusesToConvert,
                    _bufferStatus,
                    out int consumed
                );

                if (outcome == PacifyConversionEngine.Outcome.Converted)
                    ApplyAwards(statusMgr, consumed);
            }
        }

        private void ApplyAwards(StatusEffectManager mgr, int consumed)
        {
            // Big conversions mint more: consumed/rate multiplies every award (min 1 so a
            // threshold-exact conversion always awards the base amounts).
            int multiplier =
                _stacksPerAward > 0 ? Mathf.Max(1, consumed / _stacksPerAward) : 1;

            if (_awards == null || _awards.Count == 0)
            {
                // Default award: 1 permanent stack of the buffer status (Jaded) per multiple.
                var buffer = _bufferStatus ?? StatusRegistry.Get<JadedStatus>();
                mgr.ApplyStatus(buffer, multiplier, StatusDurationType.Permanent);
                return;
            }

            foreach (var award in _awards)
            {
                if (award?.Behavior == null)
                    continue;
                mgr.ApplyStatus(award.Behavior, award.Stacks * multiplier, award.Duration);
            }
        }

        public override string GetDescription()
        {
            string targetStr = _target == TargetType.Opponent ? "an enemy" : $"{_target}";
            string fuel =
                _statusesToConvert == null || _statusesToConvert.Count == 0
                    ? "Guilt/Shame/Doubt"
                    : JoinNames(_statusesToConvert);
            string buffer = _bufferStatus?.DisplayName ?? "Jaded";
            string awards = DescribeAwards();
            return $"Convert {targetStr} with enough {fuel} (3 + {buffer}): consume the stacks "
                + $"for an opinion burst and apply {awards}.";
        }

        private string DescribeAwards()
        {
            if (_awards == null || _awards.Count == 0)
                return $"1 {_bufferStatus?.DisplayName ?? "Jaded"}";
            var parts = new List<string>();
            foreach (var award in _awards)
                if (award?.Behavior != null)
                    parts.Add($"{award.Stacks} {award.Behavior.DisplayName}");
            string text = string.Join(" + ", parts);
            if (_stacksPerAward > 0)
                text += $" (per {_stacksPerAward} consumed)";
            return text;
        }

        private static string JoinNames(List<StatusBehavior> statuses)
        {
            var names = new List<string>();
            foreach (var status in statuses)
                if (status != null)
                    names.Add(status.DisplayName);
            return names.Count > 0 ? string.Join("/", names) : "Guilt/Shame/Doubt";
        }
    }
}
