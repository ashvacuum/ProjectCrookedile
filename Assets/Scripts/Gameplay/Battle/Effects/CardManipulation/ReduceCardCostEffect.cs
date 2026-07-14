using System;
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
            ResolveCardSelection(
                ctx.Deck.Hand,
                _selectionMode,
                _filterType,
                $"Choose a card — Reduce cost by {reduction}",
                1,
                chosen =>
                {
                    if (chosen.Count > 0)
                        ctx.Deck.ApplyCostReduction(chosen[0], reduction);
                },
                thisCard: ctx.OwnerCard
            );
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
            return $"Reduce {suffix}'s cost by {_costReduction} this battle";
        }
    }
}
