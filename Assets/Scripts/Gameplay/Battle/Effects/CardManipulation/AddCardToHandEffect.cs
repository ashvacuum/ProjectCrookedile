using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Data.Cards;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Adds one or more copies of a specific card directly to the player's hand.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class AddCardToHandEffect : BattleEffect
    {
        [Required]
        [Tooltip("The card to add to the hand.")]
        [SerializeField] private CardData _card;

        [MinValue(1)]
        [Tooltip("How many copies to add.")]
        [SerializeField] private int _amount = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null) return;
            if (_card == null)
            {
                GameLogger.LogWarning<AddCardToHandEffect>("No card specified — no-op");
                return;
            }
            int added = ctx.Deck.AddCardsToHand(_card, _amount);
            GameLogger.LogInfo<AddCardToHandEffect>($"Added {added}/{_amount}x {_card.CardName} to hand");
        }

        public override string GetDescription()
        {
            string name = _card != null ? _card.CardName : "???";
            return _amount == 1 ? $"Add {name} to your hand"
                                : $"Add {_amount}x {name} to your hand";
        }
    }
}
