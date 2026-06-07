using System;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Nepo Baby's generate verb — "Call in Patronage". Sacrifices (exhausts) one or more chosen
    /// cards from hand and banks Patronage in return. The sacrifice IS the cost (à la burning the
    /// hand you were dealt to fund borrowed power); there is no free baseline generation.
    /// No-op when the hand is empty.
    /// </summary>
    [Serializable]
    public class GeneratePatronageEffect : BattleEffect
    {
        [MinValue(1)]
        [Tooltip("Patronage banked per card sacrificed.")]
        [SerializeField]
        private int _patronagePerCard = 2;

        [MinValue(1)]
        [Tooltip("How many cards to sacrifice from hand.")]
        [SerializeField]
        private int _cardsToSacrifice = 1;

        [Tooltip("How the sacrificed card(s) are selected.")]
        [SerializeField]
        private CardSelectionMode _selectionMode = CardSelectionMode.PlayerChoice;

        [ShowIf("@_selectionMode == CardSelectionMode.RandomByType")]
        [Tooltip("Card type to filter for when using Random By Type.")]
        [SerializeField]
        private CardType _filterType = CardType.Pressure;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null || ctx.Deck.HandCount == 0)
            {
                GameLogger.LogInfo<GeneratePatronageEffect>(
                    "Hand is empty — no Patronage generated"
                );
                return;
            }

            int count = Mathf.Min(_cardsToSacrifice, ctx.Deck.HandCount);
            string title =
                count == 1
                    ? "Sacrifice a card for Patronage"
                    : $"Sacrifice {count} cards for Patronage";

            ResolveCardSelection(
                ctx.Deck.Hand,
                _selectionMode,
                _filterType,
                title,
                count,
                chosen =>
                {
                    int sacrificed = 0;
                    foreach (var card in chosen)
                        if (ctx.Deck.ExhaustCard(card))
                            sacrificed++;

                    int gained = sacrificed * _patronagePerCard;
                    if (gained > 0)
                    {
                        ctx.BattleManager?.GainPatronage(gained);
                        GameLogger.LogInfo<GeneratePatronageEffect>(
                            $"Sacrificed {sacrificed} card(s) → +{gained} Patronage"
                        );
                    }
                }
            );
        }

        public override string GetDescription()
        {
            string what = _cardsToSacrifice == 1 ? "a card" : $"{_cardsToSacrifice} cards";
            return $"Sacrifice {what} to gain {_patronagePerCard} Patronage each";
        }
    }
}
