using System;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// DEPRECATED — applies a status by the legacy <see cref="StatusEffectType"/> enum. Use
    /// <see cref="ApplyStatusBehaviorEffect"/> (polymorphic <see cref="StatusBehavior"/>) instead.
    /// Kept so existing assets keep working during the status-system migration; re-author onto the
    /// behavior version and this class will be removed once nothing uses it (see Content Hub →
    /// "Status migration").
    /// </summary>
    [Serializable]
    [Obsolete(
        "Use ApplyStatusBehaviorEffect (polymorphic StatusBehavior). Kept for existing assets during migration."
    )]
    [InfoBox(
        "DEPRECATED — use ApplyStatusBehaviorEffect instead (pick a StatusBehavior).",
        InfoMessageType.Warning
    )]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class ApplyStatusEffect : BattleEffect
    {
        [Tooltip("Who receives the status effect.")]
        [SerializeField]
        private TargetType _target = TargetType.Opponent;

        public override TargetType Target => _target;

        [Tooltip("Which status effect to apply.")]
        [SerializeField]
        private StatusEffectType _statusType = StatusEffectType.Weakened;

        [Tooltip(
            "Number of stacks to apply. Negative values reduce the stat (supported for Strength and Dexterity)."
        )]
        [SerializeField]
        private int _stacks = 1;

        [Tooltip("How the status duration is tracked.")]
        [SerializeField]
        private StatusDurationType _duration = StatusDurationType.DecreasePerTurn;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int stacks = amountOverride ?? _stacks;

            foreach (var (targetStats, statusMgr) in ctx.GetTargets(_target))
            {
                statusMgr?.ApplyStatusEffect(_statusType, stacks, _duration);
                GameLogger.LogInfo<ApplyStatusEffect>(
                    $"Applied {stacks} {_statusType} ({_duration}) to {(targetStats == ctx.PlayerStats ? "Player" : "Enemy")}"
                );

                EventBus.Publish(
                    new StatusEffectAppliedEvent
                    {
                        StatusType = _statusType,
                        Stacks = stacks,
                        IsToPlayer = targetStats == ctx.PlayerStats,
                        EnemyIndex = targetStats.OwnerEnemyIndex,
                    }
                );

                // Faith Leader pacify-conversion: stacking Guilt/Shame/Doubt on an enemy may push it
                // over the threshold (3 + Jaded), converting it. No-op for non-pacify statuses,
                // the player target, or sub-threshold totals.
                if (
                    stacks > 0
                    && BattleManager.IsPacifyStatus(_statusType)
                    && targetStats != ctx.PlayerStats
                )
                {
                    ctx.BattleManager?.TryPacifyConvert(targetStats, statusMgr);
                }
            }
        }

        public override string GetDescription() =>
            $"Apply {_stacks} {_statusType} ({_duration}) to {_target}";
    }
}
