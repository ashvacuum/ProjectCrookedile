using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Randomly discards the given number of cards from the player's hand.</summary>
    [Serializable]
    public class DiscardCardsEffect : BattleEffect
    {
        [MinValue(1)]
        [SerializeField] private int _amount = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null) return;
            int amount      = amountOverride ?? _amount;
            int discarded   = 0;
            for (int i = 0; i < amount && ctx.Deck.HandCount > 0; i++)
            {
                int idx = RandomHelper.Range(0, ctx.Deck.HandCount);
                if (ctx.Deck.DiscardCard(ctx.Deck.Hand[idx])) discarded++;
            }
            GameLogger.LogInfo<DiscardCardsEffect>($"Discarded {discarded} cards");
        }

        public override string GetDescription() =>
            _amount == 1 ? "Discard 1 random card" : $"Discard {_amount} random cards";
    }
}
