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
    /// Makes a card cost 0 Action Points for the rest of this battle.
    /// Supports player-choice, random-any, and random-by-type modes.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class MakeCardFreeEffect : BattleEffect
    {
        [Tooltip("How the card to make free is selected.")]
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
                GameLogger.LogInfo<MakeCardFreeEffect>("Hand is empty — no-op");
                return;
            }
            ResolveCardSelection(
                ctx.Deck.Hand,
                _selectionMode,
                _filterType,
                "Choose a card — Make it Free",
                1,
                chosen =>
                {
                    if (chosen.Count > 0)
                        ctx.Deck.MakeCardFreeThisBattle(chosen[0]);
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
            return $"Make {suffix} cost 0 this battle";
        }
    }
}
