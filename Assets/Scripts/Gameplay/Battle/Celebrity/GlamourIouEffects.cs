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

        [Tooltip("Where to read the amount from at runtime (e.g. CurrentGlamour doubles it; CurrentSupport with x0.5 halves Composure).")]
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
            int amount = ResolveScaledAmount(ctx, amountOverride, _amount, _amountSource, _perXSource, _multiplier);
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

        [Tooltip("Debt owed next turn. Keep early Borrow cards at 2+ so Line of Credit does not make them free.")]
        [SerializeField]
        private int _debt = 2;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            var state = ctx.BattleManager?.Celebrity;
            if (state == null)
                return;
            int mult = 1 + state.BorrowBonusMultiplesThisTurn;
            state.BorrowsPlayedThisTurn++;
            ctx.Caster.GainActionPoints(_energy * mult);
            state.GainDebt(_debt * mult);
        }

        public override string GetDescription() => $"Borrow: gain {_energy} energy, owe {_debt} Debt";
    }

    /// <summary>Cancels Debt (all, or up to a cap) for Composure per Debt cancelled.</summary>
    [Serializable]
    public class ForgiveDebtEffect : BattleEffect
    {
        [Tooltip("Most Debt to cancel. 0 = all of it.")]
        [SerializeField]
        private int _max;

        [Tooltip("Composure (Support) gained per Debt cancelled.")]
        [SerializeField]
        private int _supportPerDebt = 2;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int forgiven = ctx.BattleManager?.Celebrity.Forgive(_max) ?? 0;
            if (forgiven > 0 && _supportPerDebt > 0)
                ApplyGainSupport(forgiven * _supportPerDebt, ctx);
        }

        public override string GetDescription() =>
            $"Cancel {(_max <= 0 ? "all" : $"up to {_max}")} Debt, gain {_supportPerDebt} Composure per Debt";
    }

    public enum DebtRule
    {
        LineOfCredit, // battle: first Debt gain each turn is N less
        OpenTab, // battle: first N Borrow cards (tag "borrow") each turn cost 0
        Bailout, // battle: unpaid Debt damage adds that many Soundbites to hand (max N per turn)
        TooBigToFail, // the next N unpaid Debt damage instances this fight deal none
        RainCheck, // delay Debt settlement by N turns
        Overdraft, // this turn Borrow cards give (N+1)x energy and Debt
    }

    /// <summary>
    /// The Debt-policy rules. Battle-lifetime ones are installed by playing the Policy and reset
    /// next battle. Every rule has an Amount, so an upgrade is the same rule with a bigger number;
    /// playing a second copy stacks onto the first.
    /// </summary>
    [Serializable]
    public class DebtRuleEffect : BattleEffect
    {
        [SerializeField]
        private DebtRule _rule;

        [Tooltip(
            "Line of Credit: Debt shaved off. Open Tab: free Borrows per turn. Bailout: Soundbite cap "
                + "per turn. Too Big to Fail: hits negated. Rain Check: turns delayed. "
                + "Overdraft: extra multiples (1 = double, 2 = triple)."
        )]
        [MinValue(1)]
        [SerializeField]
        private int _amount = 1;

        [Tooltip("Bailout only: the Soundbite card to add.")]
        [ShowIf("@_rule == DebtRule.Bailout")]
        [SerializeField]
        private CardData _soundbite;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            var state = ctx.BattleManager?.Celebrity;
            if (state == null)
                return;
            int n = amountOverride ?? _amount;
            switch (_rule)
            {
                case DebtRule.LineOfCredit: state.LineOfCreditReduction += n; break;
                case DebtRule.OpenTab: state.FreeBorrowsPerTurn += n; break;
                case DebtRule.Bailout:
                    state.BailoutCard = _soundbite;
                    state.BailoutCapPerTurn += n;
                    break;
                case DebtRule.TooBigToFail: state.DebtWaivers += n; break;
                case DebtRule.RainCheck: state.SettlementsDelayed += n; break;
                case DebtRule.Overdraft: state.BorrowBonusMultiplesThisTurn += n; break;
            }
        }

        public override string GetDescription() =>
            _rule switch
            {
                DebtRule.LineOfCredit => $"The first time each turn you would gain Debt, gain {_amount} less",
                DebtRule.OpenTab => _amount == 1
                    ? "The first Borrow card each turn costs 0"
                    : $"The first {_amount} Borrow cards each turn cost 0",
                DebtRule.Bailout => $"Whenever unpaid Debt deals Opinion damage, add that many Soundbites to your hand (max {_amount} a turn)",
                DebtRule.TooBigToFail => _amount == 1
                    ? "The first time this fight unpaid Debt would deal Opinion damage, it deals none"
                    : $"The first {_amount} times this fight unpaid Debt would deal Opinion damage, it deals none",
                DebtRule.RainCheck => _amount == 1
                    ? "Delay Debt settlement by one turn"
                    : $"Delay Debt settlement by {_amount} turns",
                _ => _amount switch
                {
                    1 => "This turn Borrow cards give double energy and double Debt",
                    2 => "This turn Borrow cards give triple energy and triple Debt",
                    _ => $"This turn Borrow cards give {_amount + 1}x energy and Debt",
                },
            };

#if UNITY_EDITOR
        public override System.Collections.Generic.IEnumerable<string> GetConfigurationIssues()
        {
            if (_rule == DebtRule.Bailout && _soundbite == null)
                yield return "Bailout needs a Soundbite card assigned";
        }
#endif
    }
}
