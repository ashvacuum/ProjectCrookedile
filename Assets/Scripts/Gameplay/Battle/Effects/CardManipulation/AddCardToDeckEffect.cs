using System;
using Crookedile.Data.Cards;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Adds one or more copies of a specific card to the player's draw pile.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class AddCardToDeckEffect : BattleEffect
    {
        [Required]
        [Tooltip("The card to add to the draw pile.")]
        [SerializeField]
        private CardData _card;

        [MinValue(1)]
        [Tooltip("How many copies to add.")]
        [SerializeField]
        private int _amount = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null)
                return;
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
            return _amount == 1
                ? $"Add {name} to your deck"
                : $"Add {_amount}x {name} to your deck";
        }

#if UNITY_EDITOR
        public override System.Collections.Generic.IEnumerable<string> GetConfigurationIssues()
        {
            if (_card == null)
                yield return "No card assigned — effect will do nothing";
        }
#endif
    }
}
