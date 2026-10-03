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
    /// The Return lane: brings a card back to hand from the discard (or draw) pile. The lane's
    /// brake applies by default: the returned card costs
    /// <see cref="NepoBabyConfig.ReturnInTurnCostIncrease"/> more for the rest of the turn,
    /// stacking with each return.
    /// </summary>
    [Serializable]
    public class ReturnToHandEffect : BattleEffect
    {
        public enum ReturnSource
        {
            ThisCard, // the card this effect is printed on (Heirloom, Family Seat)
            TriggeringCard, // the card a passive's event names (Hand-Me-Downs)
            LastRhetoricPlayed, // the latest Rhetoric card to resolve (I Know a Guy)
            ChooseFromDiscard, // the player picks from the discard, filtered by type
        }

        [Tooltip("Which card comes back.")]
        [SerializeField]
        private ReturnSource _source = ReturnSource.ThisCard;

        [ShowIf("_source", ReturnSource.ChooseFromDiscard)]
        [Tooltip("Only cards of this type can be chosen.")]
        [SerializeField]
        private CardType _cardType = CardType.Rhetoric;

        [Tooltip("The returned card costs more for the rest of the turn (the Return lane's brake).")]
        [SerializeField]
        private bool _applyReturnBrake = true;

        [Tooltip(
            "Run only when a card actually came back (it may have been exhausted, or the hand "
                + "was full)."
        )]
        [SerializeReference]
        private List<BattleEffect> _onReturned = new List<BattleEffect>();

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null)
                return;

            switch (_source)
            {
                case ReturnSource.ThisCard:
                    Return(ctx.OwnerCard, ctx);
                    break;
                case ReturnSource.TriggeringCard:
                    Return(ctx.TriggeringCard, ctx);
                    break;
                case ReturnSource.LastRhetoricPlayed:
                    Return(ctx.BattleManager?.CardPlay?.LastRhetoricPlayed, ctx);
                    break;
                case ReturnSource.ChooseFromDiscard:
                    var candidates = new List<CardData>();
                    foreach (var card in ctx.Deck.DiscardPile)
                        if (card != null && card.CardType == _cardType)
                            candidates.Add(card);
                    ResolveCardSelection(
                        candidates,
                        CardSelectionMode.PlayerChoice,
                        _cardType,
                        $"Return a {_cardType} card to hand",
                        1,
                        chosen =>
                        {
                            if (chosen.Count > 0)
                                Return(chosen[0], ctx);
                        }
                    );
                    break;
            }
        }

        private void Return(CardData card, EffectExecutionContext ctx)
        {
            if (card == null)
                return;
            if (!ctx.Deck.RepositionCard(card, DeckManager.CardDestination.Hand))
                return;
            if (_applyReturnBrake)
                ctx.Deck.IncreaseCostThisTurn(card, NepoBabyConfig.Current.ReturnInTurnCostIncrease);
            GameLogger.LogInfo<ReturnToHandEffect>($"Returned {card.CardName} to hand");
            ExecuteAll(_onReturned, ctx);
        }

        public override string GetDescription()
        {
            string what = _source switch
            {
                ReturnSource.ThisCard => "Return this card to hand",
                ReturnSource.TriggeringCard => "Return that card to hand",
                ReturnSource.LastRhetoricPlayed => "Return the last Rhetoric card you played to hand",
                _ => $"Return a {_cardType} card of your choice from your discard to hand",
            };
            if (_applyReturnBrake)
                what += ". It costs more for the rest of the turn";
            string after = DescribeAll(_onReturned);
            return after.Length > 0 ? $"{what}. If it does: {after}" : what;
        }
    }
}
