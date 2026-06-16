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
        [UnityEngine.Tooltip(
            "If true the retain persists every turn until each card is played or the battle "
                + "ends. If false (default) it lasts this turn only."
        )]
        [UnityEngine.SerializeField]
        private bool _untilEndOfBattle = false;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null)
                return;

            // Snapshot before iterating to be safe against any collection changes
            var snapshot = new List<CardData>(ctx.Deck.Hand);
            int count = 0;
            foreach (var card in snapshot)
                if (card != null && ctx.Deck.RetainCard(card, _untilEndOfBattle))
                    count++;

            GameLogger.LogInfo<MakeAllCardsRetainEffect>($"Retained all {count} cards in hand");
        }

        public override string GetDescription() =>
            _untilEndOfBattle
                ? "Retain your hand for the rest of the battle"
                : "Retain your hand this turn (cards stay in hand at end of turn)";
    }
}
