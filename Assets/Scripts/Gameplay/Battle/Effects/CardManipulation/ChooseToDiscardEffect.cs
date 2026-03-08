using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Lets the player choose cards from their hand to discard.
    /// Supports player-choice (CardChoicePanel), random-any, and random-by-type modes.
    /// </summary>
    [Serializable]
    public class ChooseToDiscardEffect : BattleEffect
    {
        [MinValue(1)]
        [Tooltip("Number of cards to discard.")]
        [SerializeField] private int _amount = 1;

        [Tooltip("How the card(s) to discard are selected.")]
        [SerializeField] private CardSelectionMode _selectionMode = CardSelectionMode.PlayerChoice;

        [ShowIf("@_selectionMode == CardSelectionMode.RandomByType")]
        [Tooltip("Card type to filter for when using Random By Type.")]
        [SerializeField] private CardType _filterType = CardType.Pressure;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null || ctx.Deck.HandCount == 0)
            {
                GameLogger.LogInfo<ChooseToDiscardEffect>("Hand is empty — no-op");
                return;
            }

            int    count = Mathf.Min(_amount, ctx.Deck.HandCount);
            string title = count == 1 ? "Choose a card to Discard" : $"Choose {count} cards to Discard";
            ResolveCardSelection(ctx.Deck.Hand, _selectionMode, _filterType, title, count,
                chosen => { foreach (var card in chosen) ctx.Deck.DiscardCard(card); });
        }

        public override string GetDescription()
        {
            if (_selectionMode == CardSelectionMode.PlayerChoice)
                return _amount == 1 ? "Choose a card to discard" : $"Choose {_amount} cards to discard";

            string suffix = _selectionMode == CardSelectionMode.RandomAny
                ? "a random card"
                : $"a random {_filterType} card";
            return $"Discard {suffix}";
        }
    }
}
