using System;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Behavior-first version of <see cref="ApplyStatusEffect"/> — applies a polymorphic
    /// <see cref="StatusBehavior"/> (picked via the inspector's [SerializeReference] type dropdown)
    /// instead of the legacy StatusEffectType enum. Routes through the transitional bridge
    /// (<c>StatusEffectManager.ApplyStatus</c>) so it works alongside the old system during migration.
    ///
    /// To author: add this effect, then in the "Behavior" field pick a status class (GuiltStatus,
    /// WeakenedStatus, …). The Faith Leader pacify check reads <see cref="StatusBehavior.CountsTowardPacify"/>.
    /// </summary>
    [Serializable]
    public class ApplyStatusBehaviorEffect : BattleEffect
    {
        [Tooltip("Who receives the status.")]
        [SerializeField]
        private TargetType _target = TargetType.Opponent;

        public override TargetType Target => _target;

        [Tooltip("The status to apply — pick a StatusBehavior subclass from the type dropdown.")]
        [SerializeReference]
        private StatusBehavior _behavior;

        [Tooltip("Number of stacks to apply.")]
        [SerializeField]
        private int _stacks = 1;

        [Tooltip("How the status duration is tracked.")]
        [SerializeField]
        private StatusDurationType _duration = StatusDurationType.DecreasePerTurn;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (_behavior == null)
            {
                GameLogger.LogWarning<ApplyStatusBehaviorEffect>("No status behavior assigned — no-op");
                return;
            }

            int stacks = amountOverride ?? _stacks;

            foreach (var (targetStats, statusMgr) in ctx.GetTargets(_target))
            {
                if (statusMgr == null)
                    continue;

                statusMgr.ApplyStatus(_behavior, stacks, _duration);
                GameLogger.LogInfo<ApplyStatusBehaviorEffect>(
                    $"Applied {stacks} {_behavior.DisplayName} ({_duration}) to "
                        + $"{(targetStats == ctx.PlayerStats ? "Player" : "Enemy")}"
                );

                // Notify the UI/passives (event still carries the legacy enum during the bridge period).
                if (StatusBridge.TryToEnum(_behavior, out var legacyType))
                    EventBus.Publish(
                        new StatusEffectAppliedEvent
                        {
                            StatusType = legacyType,
                            Stacks = stacks,
                            IsToPlayer = targetStats == ctx.PlayerStats,
                            EnemyIndex = targetStats.OwnerEnemyIndex,
                        }
                    );

                // Faith Leader pacify-conversion: a pacify status on an enemy may cross the threshold.
                if (stacks > 0 && _behavior.CountsTowardPacify && targetStats != ctx.PlayerStats)
                    ctx.BattleManager?.TryPacifyConvert(targetStats, statusMgr);
            }
        }

        public override string GetDescription() =>
            $"Apply {_stacks} {_behavior?.DisplayName ?? "(none)"} ({_duration}) to {_target}";
    }
}
