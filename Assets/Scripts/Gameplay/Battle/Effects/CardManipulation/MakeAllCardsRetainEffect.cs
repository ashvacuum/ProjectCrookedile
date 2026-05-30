using System;
using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Data.Cards;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Marks every card in hand to be retained at end of turn.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class MakeAllCardsRetainEffect : BattleEffect
    {
        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null) return;

            // Snapshot before iterating to be safe against any collection changes
            var snapshot = new List<CardData>(ctx.Deck.Hand);
            int count    = 0;
            foreach (var card in snapshot)
                if (card != null && ctx.Deck.RetainCard(card)) count++;

            GameLogger.LogInfo<MakeAllCardsRetainEffect>($"Retained all {count} cards in hand");
        }

        public override string GetDescription() => "Retain all cards in hand until the battle ends";
    }
}
