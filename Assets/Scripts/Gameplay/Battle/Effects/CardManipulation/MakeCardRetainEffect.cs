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
    /// Marks a card in hand so it stays in hand at the end of the turn rather than
    /// being discarded. Supports player-choice, random-any, and random-by-type modes.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class MakeCardRetainEffect : BattleEffect
    {
        [Tooltip("How the card to retain is selected.")]
        [SerializeField]
        private CardSelectionMode _selectionMode = CardSelectionMode.PlayerChoice;

        [ShowIf("@_selectionMode == CardSelectionMode.RandomByType")]
        [Tooltip("Card type to filter for when using Random By Type.")]
        [SerializeField]
        private CardType _filterType = CardType.Pressure;

        [Tooltip(
            "If true the retain persists every turn until the card is played or the battle "
                + "ends. If false (default) it lasts this turn only."
        )]
        [SerializeField]
        private bool _untilEndOfBattle = false;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null || ctx.Deck.HandCount == 0)
            {
                GameLogger.LogInfo<MakeCardRetainEffect>("Hand is empty — no-op");
                return;
            }
            ResolveCardSelection(
                ctx.Deck.Hand,
                _selectionMode,
                _filterType,
                "Choose a card to Retain",
                1,
                chosen =>
                {
                    if (chosen.Count > 0)
                        ctx.Deck.RetainCard(chosen[0], _untilEndOfBattle);
                }
            );
        }

        public override string GetDescription()
        {
            string suffix = _selectionMode switch
            {
                CardSelectionMode.RandomAny => "a random card",
                CardSelectionMode.RandomByType => $"a random {_filterType} card",
                _ => "a card",
            };
            return _untilEndOfBattle
                ? $"Retain {suffix} for the rest of the battle"
                : $"Retain {suffix} this turn (it stays in hand at end of turn)";
        }
    }
}
