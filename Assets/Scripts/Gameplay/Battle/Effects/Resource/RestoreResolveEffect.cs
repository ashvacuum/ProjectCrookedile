using System;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Restores Resolve (HP) to the caster. Does not affect the Opinion Meter.
    /// Use this for effects that literally keep the player alive — recovery cards,
    /// Regeneration-style passives, or lifesteal chains off LastDamageDealt.
    /// For audience-facing opinion recovery, use <see cref="HealResolveEffect"/> instead.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class RestoreResolveEffect : BattleEffect
    {
        [Tooltip("Base Resolve to restore. Ignored when Amount Source is not Fixed.")]
        [ShowIf("@_amountSource == EffectContextValue.FixedAmount")]
        [MinValue(1)]
        [SerializeField]
        private int _amount = 5;

        [Tooltip(
            "Where to read the heal amount from at runtime. FixedAmount uses the authored Amount field."
        )]
        [SerializeField]
        private EffectContextValue _amountSource = EffectContextValue.FixedAmount;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount =
                amountOverride
                ?? (
                    _amountSource == EffectContextValue.FixedAmount
                        ? _amount
                        : ctx.GetValue(_amountSource)
                );

            if (amount <= 0) return;

            EventBus.Publish(new OpinionRaisedDirectlyEvent { Amount = amount });
            GameLogger.LogInfo<RestoreResolveEffect>($"Raised Opinion by {amount}");
            ctx.LastHealAmount += amount;
        }

        public override string GetDescription()
        {
            string amountStr =
                _amountSource == EffectContextValue.FixedAmount
                    ? _amount.ToString()
                    : _amountSource.ToString();
            return $"Raise Opinion by {amountStr}";
        }
    }
}
