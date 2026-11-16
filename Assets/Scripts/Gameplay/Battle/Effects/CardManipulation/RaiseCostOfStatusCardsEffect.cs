using System;
using System.Collections.Generic;
using Crookedile.Data.Cards;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Every card in your deck that applies the chosen status costs more for the rest of the
    /// battle. Meant for a battle-start passive (a costly ally's downside). A card "applies" the
    /// status when one of its top-level effects is an <see cref="ApplyStatusBehaviorEffect"/> of
    /// it. ponytail: top-level effects only, and only cards in the deck when it fires; walk nested
    /// effects and hook card creation if a generated or wrapped Guilt card should pay too.
    /// </summary>
    [Serializable]
    public class RaiseCostOfStatusCardsEffect : BattleEffect
    {
        [Tooltip("Cards that apply this status cost more.")]
        [SerializeReference]
        private StatusBehavior _status;

        [Tooltip("Extra Action Points those cards cost this battle.")]
        [Min(1)]
        [SerializeField]
        private int _increase = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null || _status == null)
                return;
            int increase = amountOverride ?? _increase;
            // Cost overrides are keyed per card object, so a shared asset pays once, not per copy.
            var seen = new HashSet<CardData>();
            foreach (var card in ctx.Deck.AllCards)
                if (card != null && seen.Add(card) && Applies(card))
                    ctx.Deck.ApplyCostReduction(card, -increase);
        }

        private bool Applies(CardData card)
        {
            var effects = card.GetNewEffects();
            if (effects == null)
                return false;
            foreach (var effect in effects)
                if (effect is ApplyStatusBehaviorEffect apply && apply.Behavior?.Id == _status.Id)
                    return true;
            return false;
        }

        public override IEnumerable<string> GetConfigurationIssues()
        {
            if (_status == null)
                yield return "No status picked — no card will cost more.";
        }

        public override string GetDescription() =>
            $"Your {_status?.DisplayName ?? "(no status)"} cards cost +{_increase} this battle";
    }
}
