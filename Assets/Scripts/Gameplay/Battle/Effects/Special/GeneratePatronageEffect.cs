using System;
using Crookedile.Data;
using Crookedile.Data.Cards;
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
                    int gained = 0;
                    foreach (var card in chosen)
                        if (ctx.Deck.ExhaustCard(card))
                        {
                            sacrificed++;
                            gained += PatronageValue(card);
                        }

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
            return $"Sacrifice {what} to gain Patronage equal to its cost (+1 if Rare, +1 if Upgraded; 0-cost and junk cards give 1)";
        }

        /// <summary>
        /// Patronage banked when <paramref name="card"/> is sacrificed.
        /// Base = the card's energy cost, +1 if Rare, +1 if Upgraded. A 0-cost card — and any
        /// junk card (Status / Scandal) — is always worth a flat 1, regardless of rarity/upgrade.
        /// </summary>
        public static int PatronageValue(CardData card)
        {
            if (card == null)
                return 0;

            // Junk / unplayable filler is worth a flat 1.
            if (card.CardType == CardType.Status || card.CardType == CardType.Scandal)
                return 1;

            int cost = BaseEnergyCost(card);
            if (cost <= 0)
                return 1; // 0-cost cards always give 1 (rarity/upgrade do not apply).

            int value = cost;
            if (card.Rarity == CardRarity.Rare)
                value += 1;
            if (card.IsUpgraded)
                value += 1;
            return value;
        }

        /// <summary>The card's printed energy (ActionPoints) cost, or 0 if it has none.</summary>
        private static int BaseEnergyCost(CardData card)
        {
            foreach (var c in card.GetCosts())
                if (c.CostType == CostType.ActionPoints)
                    return c.BaseAmount;
            return 0;
        }
    }
}
