using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Data.Cards;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Adds one or more copies of a specific card to the player's draw pile.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class AddCardToDeckEffect : BattleEffect
    {
        [Required]
        [Tooltip("The card to add to the draw pile.")]
        [SerializeField] private CardData _card;

        [MinValue(1)]
        [Tooltip("How many copies to add.")]
        [SerializeField] private int _amount = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null) return;
            if (_card == null)
            {
                GameLogger.LogWarning<AddCardToDeckEffect>("No card specified — no-op");
                return;
            }
            ctx.Deck.AddCardsToDeck(_card, _amount);
            GameLogger.LogInfo<AddCardToDeckEffect>($"Added {_amount}x {_card.CardName} to deck");
        }

        public override string GetDescription()
        {
            string name = _card != null ? _card.CardName : "???";
            return _amount == 1 ? $"Add {name} to your deck"
                                : $"Add {_amount}x {name} to your deck";
        }
    }
}
