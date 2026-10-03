using System;
using Crookedile.Data.Cards;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Replays a card: its effects resolve again without it being played (Encore, Dynasty).
    /// A replay is not a play for "first card each turn" effects, adds no single-target
    /// Hostility, and never applies to Policies. See <see cref="CardPlayController.ReplayCard"/>.
    /// </summary>
    [Serializable]
    public class ReplayCardEffect : BattleEffect
    {
        public enum ReplaySource
        {
            LastNonPolicyPlayed, // the latest non-Policy card to resolve before this one (Encore)
            TriggeringCard, // the card a passive's event names (Dynasty's double-played card)
        }

        [Tooltip("Which card replays.")]
        [SerializeField]
        private ReplaySource _source = ReplaySource.LastNonPolicyPlayed;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            var cardPlay = ctx.BattleManager?.CardPlay;
            if (cardPlay == null)
                return;
            CardData card =
                _source == ReplaySource.TriggeringCard
                    ? ctx.TriggeringCard
                    : cardPlay.LastNonPolicyPlayed;
            cardPlay.ReplayCard(card);
        }

        public override string GetDescription() =>
            _source == ReplaySource.TriggeringCard
                ? "Play that card again"
                : "Replay the last non-Policy card you played";
    }
}
