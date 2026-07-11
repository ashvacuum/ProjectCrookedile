using System;
using Crookedile.Data;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Moves the card this passive LIVES ON to a new deck position — the payoff of default
    /// (ambient) card passives: the card repositions itself in reaction to battle events
    /// without ever being played (e.g. "when an enemy converts, this card sneaks to the top
    /// of the draw pile").
    ///
    /// Only meaningful inside a card's passive effect list — played-card effects and
    /// origin/relic passives have no owner card and no-op with a log.
    /// Cards currently in hand or exhausted stay put (see DeckManager.RepositionCard).
    /// </summary>
    [Serializable]
    public class MoveOwnerCardEffect : BattleEffect
    {
        [Tooltip("Where this card moves when the passive fires.")]
        [SerializeField]
        private DeckManager.CardDestination _destination =
            DeckManager.CardDestination.TopOfDrawPile;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.OwnerCard == null)
            {
                GameLogger.LogWarning<MoveOwnerCardEffect>(
                    "No owner card — this effect only works inside a card's passive list"
                );
                return;
            }
            if (ctx.Deck == null)
                return;

            ctx.Deck.RepositionCard(ctx.OwnerCard, _destination);
        }

        public override string GetDescription()
        {
            string dest = _destination switch
            {
                DeckManager.CardDestination.TopOfDrawPile => "the top of the draw pile",
                DeckManager.CardDestination.BottomOfDrawPile => "the bottom of the draw pile",
                DeckManager.CardDestination.ShuffledIntoDrawPile => "a random spot in the draw pile",
                DeckManager.CardDestination.Hand => "your hand",
                DeckManager.CardDestination.Discard => "the discard pile",
                _ => _destination.ToString(),
            };
            return $"This card moves itself to {dest}.";
        }
    }
}
