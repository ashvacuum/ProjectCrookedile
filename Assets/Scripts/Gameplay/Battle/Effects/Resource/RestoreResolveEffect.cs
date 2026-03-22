using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Restores Resolve (HP) to the caster. Does not affect the Opinion Meter.
    /// Use this for effects that literally keep the player alive — recovery cards,
    /// Regeneration-style passives, or lifesteal chains off LastDamageDealt.
    /// For audience-facing opinion recovery, use <see cref="HealResolveEffect"/> instead.
    /// </summary>
    [Serializable]
    public class RestoreResolveEffect : BattleEffect
    {
        [Tooltip("Base Resolve to restore. Ignored when Amount Source is not Fixed.")]
        [ShowIf("@_amountSource == EffectContextValue.FixedAmount")]
        [MinValue(1)]
        [SerializeField] private int _amount = 5;

        [Tooltip("Where to read the heal amount from at runtime. FixedAmount uses the authored Amount field.")]
        [SerializeField] private EffectContextValue _amountSource = EffectContextValue.FixedAmount;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount = amountOverride
                ?? (_amountSource == EffectContextValue.FixedAmount ? _amount : ctx.GetValue(_amountSource));

            int actual = ctx.Caster.RestoreResolve(amount);
            GameLogger.LogInfo<RestoreResolveEffect>($"Restored {actual} Resolve");

            if (actual > 0)
                EventBus.Publish(new HealingAppliedEvent
                {
                    Amount     = actual,
                    IsToPlayer = ctx.Caster == ctx.PlayerStats,
                });

            ctx.LastHealAmount += actual;
        }

        public override string GetDescription()
        {
            string amountStr = _amountSource == EffectContextValue.FixedAmount
                ? _amount.ToString()
                : _amountSource.ToString();
            return $"Restore {amountStr} Resolve";
        }
    }
}
