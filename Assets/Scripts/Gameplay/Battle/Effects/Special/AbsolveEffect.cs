using System;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Absolution — the Faith Leader's harvest. Consumes ALL of the target's Guilt and converts it
    /// into a burst of opinion pressure that scales with the Guilt eaten, so seeding Guilt over
    /// several turns pays off in one swing. Targets without Guilt are skipped.
    ///
    /// Guilt is consumed before the pressure lands, so its own "+pressure per stack" persuasion
    /// modifier does not also amplify the absolution (the scaling IS the payoff — no double-dip).
    /// </summary>
    [Serializable]
    public class AbsolveEffect : BattleEffect
    {
        [MinValue(1)]
        [Tooltip("Opinion pressure dealt per Guilt stack consumed.")]
        [SerializeField]
        private int _pressurePerGuilt = 2;

        [Tooltip(
            "Whose Guilt to absolve. Opponent = the focused enemy; AllHostile / AllReceptive / "
                + "Adjacent absolve a group of guilty enemies at once."
        )]
        [SerializeField]
        private TargetType _target = TargetType.Opponent;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            foreach (var (stats, statusMgr) in ctx.GetTargets(_target))
            {
                int guilt = statusMgr?.GetStacks(StatusEffectType.Guilt) ?? 0;
                if (guilt <= 0)
                    continue;

                // Eat the Guilt first (so it doesn't also amplify), then cash it in as pressure.
                statusMgr.RemoveStatusEffect(StatusEffectType.Guilt);
                EventBus.Publish(
                    new StatusEffectAppliedEvent
                    {
                        StatusType = StatusEffectType.Guilt,
                        Stacks = -guilt,
                        IsToPlayer = stats == ctx.PlayerStats,
                        EnemyIndex = stats.OwnerEnemyIndex,
                    }
                );

                int pressure = guilt * _pressurePerGuilt;
                ApplyPressure(stats, ctx.Caster, pressure, ctx);
                GameLogger.LogInfo<AbsolveEffect>(
                    $"Absolved {guilt} Guilt → {pressure} opinion pressure"
                );
            }
        }

        public override string GetDescription() =>
            $"Absolve: consume the target's Guilt, applying {_pressurePerGuilt} opinion pressure per stack consumed";
    }
}
