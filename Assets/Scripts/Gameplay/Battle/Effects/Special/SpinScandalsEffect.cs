using System;
using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Celebrity "spin" / cash-out — the Scandal line's detonator. Clears every Scandal clogging the
    /// hand (exhausting them) and converts the pile into a burst of Opinion, scaling with how many
    /// were carried. Lets the player detonate at the peak before the hand chokes. No-op with no Scandals.
    /// </summary>
    [Serializable]
    public class SpinScandalsEffect : BattleEffect
    {
        [MinValue(1)]
        [Tooltip("Opinion raised per Scandal cleared from hand.")]
        [SerializeField]
        private int _opinionPerScandal = 3;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null || ctx.Deck.HandCount == 0)
                return;

            // Snapshot first — exhausting mutates the hand we're scanning.
            var scandals = new List<CardData>();
            foreach (var card in ctx.Deck.Hand)
                if (card != null && card.CardType == CardType.Scandal)
                    scandals.Add(card);

            int cleared = 0;
            foreach (var scandal in scandals)
                if (ctx.Deck.ExhaustCard(scandal))
                    cleared++;

            if (cleared <= 0)
                return;

            int burst = cleared * _opinionPerScandal;
            ctx.BattleManager?.RaiseOpinion(burst);
            ctx.LastHealAmount += burst;
            GameLogger.LogInfo<SpinScandalsEffect>(
                $"Spin: cleared {cleared} Scandal(s) → +{burst} Opinion"
            );
        }

        public override string GetDescription() =>
            $"Clear all Scandals from your hand; raise Opinion by {_opinionPerScandal} per Scandal cleared";
    }
}
