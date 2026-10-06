using System;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Puts chosen cards from hand on top of the draw pile (Stacked Deck). The first card chosen
    /// ends up on top.
    /// </summary>
    [Serializable]
    public class PutHandCardsOnTopEffect : BattleEffect
    {
        [MinValue(1)]
        [Tooltip("The most cards the player can put back.")]
        [SerializeField]
        private int _count = 2;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null || ctx.Deck.HandCount == 0)
                return;
            ResolveCardSelection(
                ctx.Deck.Hand,
                CardSelectionMode.PlayerChoice,
                CardType.Pressure, // ignored by PlayerChoice
                $"Rehearse up to {_count}: choose cards to put on top of your draw pile",
                _count,
                chosen =>
                {
                    // Insert in reverse so the first card chosen ends up on top.
                    for (int i = chosen.Count - 1; i >= 0; i--)
                        ctx.Deck.MoveFromHandToTopOfDrawPile(chosen[i]);
                    GameLogger.LogInfo<PutHandCardsOnTopEffect>(
                        $"Put {chosen.Count} card(s) on top of the draw pile"
                    );
                },
                allowFewer: true
            );
        }

        public override string GetDescription() =>
            $"Rehearse {_count}";
    }
}
