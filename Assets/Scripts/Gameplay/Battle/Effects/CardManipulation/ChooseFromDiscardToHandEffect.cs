using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Core;
using Crookedile.Utilities;
namespace Crookedile.Gameplay.Battle
{
    /// <summary>Player chooses cards from their discard pile and returns them to hand.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class ChooseFromDiscardToHandEffect : BattleEffect
    {
        [MinValue(1)]
        [SerializeField] private int _amount = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null || ctx.Deck.DiscardCount == 0)
            {
                GameLogger.LogInfo<ChooseFromDiscardToHandEffect>("Discard is empty — no-op");
                return;
            }

            int    count = Mathf.Min(_amount, ctx.Deck.DiscardCount);
            string title = count == 1 ? "Choose a card from Discard" : $"Choose {count} cards from Discard";
            EventBus.Publish(new CardChoiceRequestedEvent
            {
                Title         = title,
                Choices       = ctx.Deck.DiscardPile,
                RequiredCount = count,
                OnConfirmed   = chosen => { foreach (var c in chosen) ctx.Deck.MoveFromDiscardToHand(c); },
            });
        }

        public override string GetDescription() =>
            _amount == 1 ? "Choose 1 card from your discard pile and put it in your hand"
                         : $"Choose {_amount} cards from your discard pile and put them in your hand";
    }
}
