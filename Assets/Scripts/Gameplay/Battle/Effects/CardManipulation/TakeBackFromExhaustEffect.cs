using System;
using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// The player picks a card from the exhaust pile and puts it back in hand. Heckles and
    /// Scandals are not offered. Each card taken back raises Hostility on all enemies.
    /// </summary>
    [Serializable]
    public class TakeBackFromExhaustEffect : BattleEffect
    {
        [Min(1)]
        [Tooltip("How many cards to take back.")]
        [SerializeField]
        private int _count = 1;

        [Min(0)]
        [Tooltip("Hostility raised on every enemy per card taken back. 0 = free.")]
        [SerializeField]
        private int _hostilityPerCard = 2;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null)
                return;

            var candidates = new List<CardData>();
            foreach (var card in ctx.Deck.ExhaustPile)
                if (card != null && !card.IsJunk)
                    candidates.Add(card);

            ResolveCardSelection(
                candidates,
                CardSelectionMode.PlayerChoice,
                CardType.Pressure, // ignored by PlayerChoice
                _count == 1 ? "Take back a card" : $"Take back {_count} cards",
                _count,
                chosen =>
                {
                    int taken = 0;
                    foreach (var card in chosen)
                    {
                        if (!ctx.Deck.MoveFromExhaustToHand(card))
                            continue;
                        taken++;
                        if (_hostilityPerCard > 0)
                            foreach (var (stats, _) in ctx.GetTargets(TargetType.AllOpponents))
                                ctx.LastHostilityGained += stats.GainHostility(_hostilityPerCard);
                    }
                    GameLogger.LogInfo<TakeBackFromExhaustEffect>($"Took back {taken} card(s)");
                }
            );
        }

        public override string GetDescription()
        {
            string what = _count == 1 ? "a card" : $"{_count} cards";
            string text = $"Take {what} from your exhaust pile back into your hand";
            if (_hostilityPerCard > 0)
                text += $". Each one: Aggravate {_hostilityPerCard} (all enemies)";
            return text;
        }
    }
}
