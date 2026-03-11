using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Data.Cards;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Adds one or more copies of a specific card directly to the player's discard pile.
    /// Typically used for status or curse cards granted by enemy effects.</summary>
    [Serializable]
    public class AddCardToDiscardEffect : BattleEffect
    {
        [Required]
        [Tooltip("The card to add to the discard pile.")]
        [SerializeField] private CardData _card;

        [MinValue(1)]
        [Tooltip("How many copies to add.")]
        [SerializeField] private int _amount = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null) return;
            if (_card == null)
            {
                GameLogger.LogWarning<AddCardToDiscardEffect>("No card specified — no-op");
                return;
            }
            ctx.Deck.AddCardsToDiscard(_card, amountOverride ?? _amount);
            GameLogger.LogInfo<AddCardToDiscardEffect>($"Added {amountOverride ?? _amount}x {_card.CardName} to discard");
        }

        public override string GetDescription()
        {
            string name = _card != null ? _card.CardName : "???";
            return _amount == 1 ? $"Add {name} to your discard"
                                : $"Add {_amount}x {name} to your discard";
        }
    }
}
