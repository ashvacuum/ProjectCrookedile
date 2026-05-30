using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Discards the entire hand. Optionally lets the player reclaim a number of
    /// just-discarded cards back into their hand via the CardChoicePanel.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class DiscardHandEffect : BattleEffect
    {
        [MinValue(0)]
        [Tooltip("Number of cards to reclaim from the discarded hand. 0 = no reclaim.")]
        [SerializeField] private int _reclaimAmount = 0;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null) return;

            var discarded = ctx.Deck.DiscardHand();
            GameLogger.LogInfo<DiscardHandEffect>($"Discarded entire hand ({discarded.Count} cards)");

            int reclaim = _reclaimAmount;
            if (reclaim <= 0 || discarded.Count == 0) return;

            int   count = Mathf.Min(reclaim, discarded.Count);
            string title = count == 1 ? "Reclaim 1 card" : $"Reclaim {count} cards";
            EventBus.Publish(new CardChoiceRequestedEvent
            {
                Title         = title,
                Choices       = discarded,
                RequiredCount = count,
                OnConfirmed   = chosen =>
                {
                    foreach (var card in chosen)
                        ctx.Deck.MoveFromDiscardToHand(card);
                },
            });
        }

        public override string GetDescription()
        {
            string desc = "Discard your entire hand";
            if (_reclaimAmount > 0)
                desc += $", then reclaim {_reclaimAmount}";
            return desc;
        }
    }
}
