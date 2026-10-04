using System;
using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Data.Cards;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Debt is charged only after a selected card is successfully recovered or booked.</summary>
    [Serializable]
    public sealed class BorrowCardSelectionEffect : BattleEffect
    {
        [Tooltip("Book from the draw pile for next turn instead of recovering from discard now.")]
        [SerializeField]
        private bool _nextTurn;

        [Tooltip("Debt charged when a card is selected successfully.")]
        [Min(1)]
        [SerializeField]
        private int _debt = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (
                !ctx.IsPlayerCard
                || ctx.Deck == null
                || ctx.BattleManager == null
                || ctx.Deck.IsHandFull && !_nextTurn
            )
                return;

            var choices = new List<CardData>();
            foreach (var card in _nextTurn ? ctx.Deck.DrawPile : ctx.Deck.DiscardPile)
            {
                if (!card.GetNewEffects().Exists(e => e is ExhaustThisCardEffect))
                    choices.Add(card);
            }
            if (choices.Count == 0)
                return;

            bool resolved = false;
            EventBus.Publish(
                new CardChoiceRequestedEvent
                {
                    Title = _nextTurn ? "Book a card for next turn" : "Return a card to hand",
                    Choices = choices,
                    RequiredCount = 1,
                    OnConfirmed = selected =>
                    {
                        if (
                            resolved
                            || selected == null
                            || selected.Count != 1
                            || !choices.Contains(selected[0])
                            || ctx.BattleManager.CurrentState == BattleState.BattleEnd
                        )
                            return;

                        resolved = true;
                        bool moved = _nextTurn
                            ? ctx.Deck.BookForNextTurn(selected[0])
                            : ctx.Deck.MoveFromDiscardToHand(selected[0]);
                        if (moved)
                            ctx.BattleManager.Celebrity.GainDebt(_debt, ctx.Deck);
                    },
                }
            );
        }

        public override string GetDescription() =>
            _nextTurn
                ? $"Choose a non-Exhaust card from your draw pile to receive next turn. Take {_debt} Debt"
                : $"Return a non-Exhaust card from discard to hand. Take {_debt} Debt";
    }
}
