using System;
using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Reduces a card's Action Point cost for this battle.
    /// Supports player-choice, random-any, and random-by-type modes.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class ReduceCardCostEffect : BattleEffect
    {
        [MinValue(1)]
        [Tooltip("Amount to reduce the card's AP cost by.")]
        [SerializeField]
        private int _costReduction = 1;

        [Tooltip("How the card to reduce is selected.")]
        [SerializeField]
        private CardSelectionMode _selectionMode = CardSelectionMode.PlayerChoice;

        [ShowIf("@_selectionMode == CardSelectionMode.RandomByType")]
        [Tooltip("Card type to filter for when using Random By Type.")]
        [SerializeField]
        private CardType _filterType = CardType.Pressure;

        [MinValue(0)]
        [Tooltip(
            "The reduction never takes a card's printed cost below this (after earlier "
                + "reductions this battle). 0 = no floor. With a floor, only cards that can still "
                + "get cheaper are picked."
        )]
        [SerializeField]
        private int _minimumCost = 0;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null)
                return;
            if (_selectionMode != CardSelectionMode.ThisCard && ctx.Deck.HandCount == 0)
            {
                GameLogger.LogInfo<ReduceCardCostEffect>("Hand is empty — no-op");
                return;
            }
            int reduction = amountOverride ?? _costReduction;
            var pool = new List<CardData>();
            foreach (var card in ctx.Deck.Hand)
                if (_minimumCost <= 0 || RoomAboveFloor(ctx.Deck, card) > 0)
                    pool.Add(card);
            ResolveCardSelection(
                pool,
                _selectionMode,
                _filterType,
                $"Choose a card — Reduce cost by {reduction}",
                1,
                chosen =>
                {
                    if (chosen.Count == 0)
                        return;
                    int applied =
                        _minimumCost > 0
                            ? Mathf.Min(reduction, RoomAboveFloor(ctx.Deck, chosen[0]))
                            : reduction;
                    if (applied > 0)
                        ctx.Deck.ApplyCostReduction(chosen[0], applied);
                },
                thisCard: ctx.OwnerCard
            );
        }

        /// <summary>How far the card's printed cost, net of this battle's reductions, sits above the floor.</summary>
        private int RoomAboveFloor(DeckManager deck, CardData card)
        {
            int reduced = deck.GetCardCostReduction(card);
            if (reduced == int.MaxValue)
                return 0; // already free
            return card.PrintedCost - reduced - _minimumCost;
        }

        public override string GetDescription()
        {
            string suffix = _selectionMode switch
            {
                CardSelectionMode.RandomAny => "a random card",
                CardSelectionMode.RandomByType => $"a random {_filterType} card",
                CardSelectionMode.ThisCard => "this card",
                _ => "a card",
            };
            string floor = _minimumCost > 0 ? $" (not below {_minimumCost})" : "";
            return $"Reduce {suffix}'s cost by {_costReduction}{floor} this battle";
        }
    }
}
