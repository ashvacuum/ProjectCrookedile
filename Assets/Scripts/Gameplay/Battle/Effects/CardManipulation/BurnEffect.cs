using System;
using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Burn: the player exhausts card(s) from hand as a cost, then the follow-up effects run.
    /// Policies can never be burned (<see cref="NepoBabyRules.CanBurn"/>). The follow-ups live
    /// inside this effect because the choice resolves after the rest of the card's effects.
    /// Each burned card counts toward <see cref="EffectContextValue.CardsBurnedByThisCard"/>.
    /// </summary>
    [Serializable]
    public class BurnEffect : BattleEffect
    {
        public enum BurnFilter
        {
            AnyCard, // any card the burn rules allow (never a Policy)
            JunkOnly, // Heckles and Scandals only
        }

        [MinValue(1)]
        [Tooltip("How many cards to burn (the most, when Up To is on).")]
        [SerializeField]
        private int _count = 1;

        [Tooltip("Up to: the player may burn fewer, including none.")]
        [SerializeField]
        private bool _upTo = false;

        [Tooltip("Which cards in hand can be burned. Policies never can.")]
        [SerializeField]
        private BurnFilter _filter = BurnFilter.AnyCard;

        [Tooltip(
            "Run once for each burned card whose printed cost is at least Per-Card Minimum Cost."
        )]
        [SerializeReference]
        private List<BattleEffect> _perBurnedCard = new List<BattleEffect>();

        [MinValue(0)]
        [Tooltip("Per-card effects only count burned cards with at least this printed cost.")]
        [SerializeField]
        private int _perCardMinimumCost = 0;

        [Tooltip("Run once after the burn.")]
        [SerializeReference]
        private List<BattleEffect> _afterBurn = new List<BattleEffect>();

        [Tooltip("The after-burn effects run only if at least one card was burned.")]
        [SerializeField]
        private bool _afterRequiresBurn = true;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null)
                return;

            var candidates = new List<CardData>();
            foreach (var card in ctx.Deck.Hand)
                if (
                    NepoBabyRules.CanBurn(card)
                    && (_filter == BurnFilter.AnyCard || card.IsJunk)
                )
                    candidates.Add(card);

            ResolveCardSelection(
                candidates,
                CardSelectionMode.PlayerChoice,
                CardType.Pressure, // ignored by PlayerChoice
                ChoiceTitle(),
                _count,
                chosen =>
                {
                    int burned = 0;
                    foreach (var card in chosen)
                    {
                        if (!ctx.Deck.ExhaustCard(card))
                            continue;
                        burned++;
                        ctx.CardsBurned++;
                        if (card.PrintedCost >= _perCardMinimumCost)
                            ExecuteAll(_perBurnedCard, ctx);
                    }
                    GameLogger.LogInfo<BurnEffect>($"Burned {burned} card(s)");
                    if (burned > 0 || !_afterRequiresBurn)
                        ExecuteAll(_afterBurn, ctx);
                },
                allowFewer: _upTo
            );
        }

        private string ChoiceTitle()
        {
            string what = _filter == BurnFilter.JunkOnly ? "Heckle or Scandal" : "card";
            if (_count == 1)
                return _upTo ? $"Burn a {what}? (optional)" : $"Burn a {what}";
            return _upTo ? $"Burn up to {_count} {what}s" : $"Burn {_count} {what}s";
        }

        public override string GetDescription()
        {
            string what = _filter == BurnFilter.JunkOnly ? "Heckle or Scandal" : "card";
            string head =
                _count == 1
                    ? (_upTo ? $"You may burn a {what}" : $"Burn a {what}")
                    : (_upTo ? $"Burn up to {_count} {what}s" : $"Burn {_count} {what}s");
            var parts = new List<string> { head };
            string per = DescribeAll(_perBurnedCard);
            if (per.Length > 0)
                parts.Add(
                    _perCardMinimumCost > 0
                        ? $"for each one costing {_perCardMinimumCost}+: {per}"
                        : $"for each: {per}"
                );
            string after = DescribeAll(_afterBurn);
            if (after.Length > 0)
                parts.Add(after);
            return string.Join(". ", parts);
        }

#if UNITY_EDITOR
        public override IEnumerable<string> GetConfigurationIssues()
        {
            if ((_perBurnedCard == null || _perBurnedCard.Count == 0)
                && (_afterBurn == null || _afterBurn.Count == 0))
                yield return "Burn has no follow-up effects — the burned card buys nothing.";
        }
#endif
    }
}
