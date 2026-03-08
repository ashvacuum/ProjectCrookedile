using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Draws cards from the player's draw pile into hand.</summary>
    [Serializable]
    public class DrawCardsEffect : BattleEffect
    {
        [MinValue(1)]
        [SerializeField] private int _amount = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null) return; // enemies have no deck
            int amount = amountOverride ?? _amount;
            int drawn  = ctx.Deck.DrawCards(amount);
            GameLogger.LogInfo<DrawCardsEffect>($"Drew {drawn} cards");
        }

        public override string GetDescription() =>
            _amount == 1 ? "Draw 1 card" : $"Draw {_amount} cards";
    }
}
