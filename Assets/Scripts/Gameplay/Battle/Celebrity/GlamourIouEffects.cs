using System;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Gains Glamour (player-side stacking status). Read it with EffectContextValue.CurrentGlamour.</summary>
    [Serializable]
    public class GainGlamourEffect : BattleEffect
    {
        [Tooltip("Base Glamour to gain. Ignored when Amount Source is not Fixed.")]
        [ShowIf("@_amountSource == EffectContextValue.FixedAmount")]
        [MinValue(1)]
        [SerializeField]
        private int _amount = 1;

        [Tooltip(
            "Where to read the amount from at runtime (e.g. CurrentGlamour doubles it; CurrentSupport with x0.5 halves Support)."
        )]
        [SerializeField]
        private EffectContextValue _amountSource = EffectContextValue.FixedAmount;

        [Tooltip("Optional scaling: multiply the amount by this context value. None = no scaling.")]
        [SerializeField]
        private EffectContextValue _perXSource = EffectContextValue.None;

        [Tooltip("Optional flat multiplier applied last. Values <= 0 are treated as 1.")]
        [SerializeField]
        private float _multiplier = 1f;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (!ctx.IsPlayerCard || ctx.PlayerStatusEffects == null)
                return;
            int amount = ResolveScaledAmount(
                ctx,
                amountOverride,
                _amount,
                _amountSource,
                _perXSource,
                _multiplier
            );
            if (amount > 0)
                ctx.PlayerStatusEffects.ApplyStatus(
                    StatusRegistry.Get<GlamourStatus>(),
                    amount,
                    StatusDurationType.Permanent
                );
        }

        public override string GetDescription() =>
            $"Gain {DescribeScaledAmount(_amount, _amountSource, _perXSource, _multiplier)} Glamour";
    }

    /// <summary>Draws extra cards only while Glamour is at or above a threshold (Fan Mail).</summary>
    [Serializable]
    public class DrawIfGlamourEffect : BattleEffect
    {
        [SerializeField]
        private int _threshold = 5;

        [MinValue(1)]
        [SerializeField]
        private int _amount = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck != null && (ctx.BattleManager?.CurrentGlamour ?? 0) >= _threshold)
                ctx.Deck.DrawCards(amountOverride ?? _amount);
        }

        public override string GetDescription() =>
            $"If Glamour is {_threshold}+, draw {_amount} more";
    }

    /// <summary>Borrow: energy now, Debt settled at the start of your next turn. Tag the card "borrow".</summary>
    [Serializable]
    public class BorrowEffect : BattleEffect
    {
        [SerializeField]
        private int _energy = 2;

        [Tooltip("Debt owed next turn, after any armed Line of Credit reduction.")]
        [SerializeField]
        private int _debt = 2;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            var state = ctx.BattleManager?.Celebrity;
            if (state == null)
                return;
            ctx.Caster.GainActionPoints(_energy);
            state.GainDebt(_debt, ctx.Deck);
        }

        public override string GetDescription() =>
            $"Borrow: gain {_energy} energy, owe {_debt} Debt";
    }

    /// <summary>Cancels Debt, optionally granting Support per Debt cancelled.</summary>
    [Serializable]
    public class ForgiveDebtEffect : BattleEffect
    {
        [Tooltip("Most Debt to cancel. 0 = all of it.")]
        [SerializeField]
        private int _max;

        [Tooltip("Support gained per Debt cancelled.")]
        [SerializeField]
        private int _supportPerDebt = 2;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int forgiven = ctx.BattleManager?.Celebrity.Forgive(_max) ?? 0;
            if (forgiven > 0 && _supportPerDebt > 0)
                ApplyGainSupport(forgiven * _supportPerDebt, ctx);
        }

        public override string GetDescription() =>
            $"Cancel {(_max <= 0 ? "all" : $"up to {_max}")} Debt"
            + (_supportPerDebt > 0 ? $", gain {_supportPerDebt} Support per Debt" : "");
    }
}
