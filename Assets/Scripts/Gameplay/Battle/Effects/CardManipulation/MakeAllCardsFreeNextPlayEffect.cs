using System;
using System.Collections.Generic;
using UnityEngine;
using Crookedile.Core;
using Crookedile.Data.Cards;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Makes every card currently in hand cost 0 AP, but only for the NEXT card played.
    /// All temporary free-cost overrides revert after a single player card play.
    ///
    /// Implementation:
    ///  1. Snapshots the current cost-reduction state so pre-existing permanent reductions survive.
    ///  2. Calls <see cref="DeckManager.MakeCardFreeThisBattle"/> on each hand card.
    ///  3. Subscribes a one-shot <see cref="CardPlayedEvent"/> handler that restores the snapshot.
    ///
    /// Because the snapshot is restored and the hand is re-initialised by the normal
    /// post-play rearrange pass, no extra UI refresh logic is required.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class MakeAllCardsFreeNextPlayEffect : BattleEffect
    {
        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null || ctx.Deck.HandCount == 0)
            {
                GameLogger.LogInfo<MakeAllCardsFreeNextPlayEffect>("Hand is empty — no-op");
                return;
            }

            // Snapshot existing reductions so permanent ones survive the revert.
            ctx.Deck.SnapshotCostReductions();

            // Make every card in hand free.
            // Snapshot the hand contents first — the list is a live view.
            var hand = new List<CardData>(ctx.Deck.Hand);
            foreach (var card in hand)
                ctx.Deck.MakeCardFreeThisBattle(card);

            GameLogger.LogInfo<MakeAllCardsFreeNextPlayEffect>(
                $"Made {hand.Count} card(s) free — will revert after the next card played");

            // Register a one-shot listener on CardPlayedEvent to restore the snapshot.
            // The lambda captures the DeckManager reference; the EventBus subscription is
            // removed immediately after it fires to prevent any subsequent plays from triggering it.
            DeckManager deckRef = ctx.Deck;
            void OnNextCardPlayed(CardPlayedEvent e)
            {
                if (!e.IsPlayer) return;                            // ignore enemy plays
                EventBus.Unsubscribe<CardPlayedEvent>(OnNextCardPlayed);
                deckRef.RestoreCostReductionSnapshot();
                GameLogger.LogInfo<MakeAllCardsFreeNextPlayEffect>(
                    "Next-play revert fired — cost reductions restored to pre-effect state");
            }
            EventBus.Subscribe<CardPlayedEvent>(OnNextCardPlayed);
        }

        public override string GetDescription() =>
            "All cards in hand cost 0 AP. Reverts after the next card you play.";
    }
}
