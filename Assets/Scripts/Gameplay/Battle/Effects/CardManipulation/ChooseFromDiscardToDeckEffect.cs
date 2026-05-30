using System;
using Crookedile.Core;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Player chooses cards from their discard pile and shuffles them back into the draw pile.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class ChooseFromDiscardToDeckEffect : BattleEffect
    {
        [MinValue(1)]
        [SerializeField]
        private int _amount = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null || ctx.Deck.DiscardCount == 0)
            {
                GameLogger.LogInfo<ChooseFromDiscardToDeckEffect>("Discard is empty — no-op");
                return;
            }

            int count = Mathf.Min(_amount, ctx.Deck.DiscardCount);
            string title =
                count == 1
                    ? "Choose a card — return to Deck"
                    : $"Choose {count} cards — return to Deck";
            EventBus.Publish(
                new CardChoiceRequestedEvent
                {
                    Title = title,
                    Choices = ctx.Deck.DiscardPile,
                    RequiredCount = count,
                    OnConfirmed = chosen =>
                    {
                        foreach (var c in chosen)
                            ctx.Deck.MoveFromDiscardToDeck(c);
                    },
                }
            );
        }

        public override string GetDescription() =>
            _amount == 1
                ? "Choose 1 card from your discard pile and shuffle it into your deck"
                : $"Choose {_amount} cards from your discard pile and shuffle them into your deck";
    }
}
