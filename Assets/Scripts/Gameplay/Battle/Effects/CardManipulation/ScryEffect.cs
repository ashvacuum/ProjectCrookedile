using System;
using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Scry N: look at the top N cards of the draw pile and discard any of them, then run the
    /// follow-ups (e.g. draw). Never reshuffles the discard to find cards.
    /// ponytail: the cards kept stay in their order — the card-choice panel can't reorder; add a
    /// reorder step when the UI can.
    /// </summary>
    [Serializable]
    public class ScryEffect : BattleEffect
    {
        [MinValue(1)]
        [Tooltip("How many cards from the top to look at.")]
        [SerializeField]
        private int _count = 3;

        [Tooltip("Run once the player has chosen what to discard (e.g. draw a card).")]
        [SerializeReference]
        private List<BattleEffect> _afterScry = new List<BattleEffect>();

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null)
                return;
            int count = amountOverride ?? _count;
            var top = ctx.Deck.PeekTop(count);
            if (top.Count == 0)
            {
                ExecuteAll(_afterScry, ctx);
                return;
            }

            ResolveCardSelection(
                top,
                CardSelectionMode.PlayerChoice,
                CardType.Pressure, // ignored by PlayerChoice
                $"Scry {count}: discard any",
                top.Count,
                chosen =>
                {
                    foreach (var card in chosen)
                        ctx.Deck.DiscardFromDrawPile(card);
                    GameLogger.LogInfo<ScryEffect>(
                        $"Scry {count}: discarded {chosen.Count} of {top.Count}"
                    );
                    ExecuteAll(_afterScry, ctx);
                },
                allowFewer: true
            );
        }

        public override string GetDescription()
        {
            string after = DescribeAll(_afterScry);
            return after.Length > 0 ? $"Scry {_count}, then {after}" : $"Scry {_count}";
        }
    }
}
