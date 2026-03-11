using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Applies a status effect (buff or debuff) to one or more targets.
    /// Stacks and duration are configured per instance.
    /// </summary>
    [Serializable]
    public class ApplyStatusEffect : BattleEffect
    {
        [Tooltip("Who receives the status effect.")]
        [SerializeField] private TargetType _target = TargetType.Opponent;

        [Tooltip("Which status effect to apply.")]
        [SerializeField] private StatusEffectType _statusType = StatusEffectType.Weakened;

        [Tooltip("Number of stacks to apply. Negative values reduce the stat (supported for Strength and Dexterity).")]
        [SerializeField] private int _stacks = 1;

        [Tooltip("How the status duration is tracked.")]
        [SerializeField] private StatusDurationType _duration = StatusDurationType.DecreasePerTurn;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int stacks = amountOverride ?? _stacks;

            foreach (var (targetStats, statusMgr) in ctx.GetTargets(_target))
            {
                statusMgr?.ApplyStatusEffect(_statusType, stacks, _duration);
                GameLogger.LogInfo<ApplyStatusEffect>(
                    $"Applied {stacks} {_statusType} ({_duration}) to {(targetStats == ctx.PlayerStats ? "Player" : "Enemy")}");

                EventBus.Publish(new StatusEffectAppliedEvent
                {
                    StatusType = _statusType,
                    Stacks     = stacks,
                    IsToPlayer = targetStats == ctx.PlayerStats,
                });
            }
        }

        public override string GetDescription() =>
            $"Apply {_stacks} {_statusType} ({_duration}) to {_target}";
    }
}
