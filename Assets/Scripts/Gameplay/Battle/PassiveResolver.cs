using System;
using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Gameplay;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Fires origin-specific passive abilities at the correct battle moments.
    /// Reads all magnitudes from the OriginPassive ScriptableObject so they are
    /// fully tunable in the Inspector without code changes.
    ///
    /// Phase A passives (implemented):
    ///   Faith Leader — "Opening Prayer": draw 1 extra card at battle start.
    ///   Nepo Baby    — "Lawyers on Speed Dial": gain Composure whenever taking Resolve damage.
    ///
    /// Phase B passives (stubbed — requires CardSelectionPanel UI):
    ///   Actor        — "Improvise": on even player turns, optionally discard and redraw same count.
    /// </summary>
    public class PassiveResolver
    {
        private readonly OriginPassive _passive;
        private int  _playerTurnNumber  = 0;
        private int  _cardsPlayedCount  = 0;
        private bool _improviseAvailable = false;
        private bool _improviseUsed      = false;

        /// <summary>
        /// Fired when Actor's Improvise window opens (player's even turns).
        /// BattleUI subscribes to show/hide the Improvise button (Phase B).
        /// </summary>
        public event Action OnImproviseAvailable;

        public PassiveResolver(OriginPassive passive)
        {
            _passive = passive;
        }

        // ------------------------------------------------------------------ Properties

        /// <summary>True when Improvise is available this turn and hasn't been used yet.</summary>
        public bool ImproviseAvailable => _improviseAvailable && !_improviseUsed;

        // ------------------------------------------------------------------ Fire Points

        /// <summary>
        /// Called once at battle start. No active effects currently use BattleStart trigger.
        /// Kept as a hook for future passives.
        /// </summary>
        public void FireBattleStart(DeckManager deck)
        {
            // No current passive uses BattleStart — Faith Leader fires on TurnStart (playerTurnNumber == 1)
        }

        /// <summary>
        /// Called at the start of each player turn (not enemy turns).
        /// Handles TurnStart passives and Improvise availability.
        /// </summary>
        /// <param name="playerTurnNumber">Cumulative count of player turns this battle (1-based).</param>
        /// <param name="playerStats">Player's BattleStats — used for stat-granting passives (e.g. Faith Leader AP).</param>
        public void FireTurnStart(int playerTurnNumber, BattleStats playerStats)
        {
            _playerTurnNumber = playerTurnNumber;
            _improviseUsed    = false;

            // Faith Leader — "Discipline": +1 AP on first player turn only
            if (_passive?.Origin == OriginType.FaithLeader && playerTurnNumber == 1
                && _passive.EffectType == PassiveEffectType.GainActionPoints)
            {
                playerStats.GainActionPoints(_passive.EffectAmount);
                GameLogger.LogInfo<PassiveResolver>(
                    $"[{_passive.PassiveName}] Gained {_passive.EffectAmount} AP on turn 1");
            }

            // Actor — "Improvise": window opens on turn 1 only (once per battle)
            if (_passive?.Origin == OriginType.Actor)
            {
                _improviseAvailable = (playerTurnNumber == 1);
                if (_improviseAvailable)
                {
                    GameLogger.LogInfo<PassiveResolver>("[Improvise] Available on player turn 1");
                    OnImproviseAvailable?.Invoke();
                }
            }
        }

        /// <summary>
        /// Called when the player takes Resolve damage.
        /// Handles PassiveTrigger.OnDamageTaken effects (e.g. Nepo Baby Composure gain).
        /// </summary>
        /// <param name="playerStats">Player's BattleStats (Composure is added here).</param>
        /// <param name="damageAmount">Actual damage dealt after all modifiers.</param>
        public void FireOnDamageTaken(BattleStats playerStats, int damageAmount)
        {
            if (_passive == null || damageAmount <= 0) return;
            if (_passive.Trigger != PassiveTrigger.OnDamageTaken) return;

            if (_passive.EffectType == PassiveEffectType.GainComposure)
            {
                playerStats.GainComposure(_passive.EffectAmount);
                GameLogger.LogInfo<PassiveResolver>(
                    $"[{_passive.PassiveName}] Gained {_passive.EffectAmount} Composure from taking {damageAmount} damage");
            }
        }

        // ------------------------------------------------------------------ Card Play Counter

        /// <summary>
        /// Called every time the player plays a card.
        /// Nepo Baby — "Nepotism": refunds 1 AP on every 5th card played this battle.
        /// </summary>
        /// <param name="playerStats">Player's BattleStats — AP is refunded here.</param>
        public void FireOnCardPlayed(BattleStats playerStats)
        {
            if (_passive?.Origin != OriginType.NepoBaby) return;
            _cardsPlayedCount++;
            if (_cardsPlayedCount % 5 == 0)
            {
                playerStats.GainActionPoints(_passive.EffectAmount);
                GameLogger.LogInfo<PassiveResolver>(
                    $"[{_passive.PassiveName}] Card #{_cardsPlayedCount} — refunded {_passive.EffectAmount} AP");
            }
        }

        // ------------------------------------------------------------------ Phase B: Improvise

        /// <summary>
        /// Phase B — called by the UI when the player confirms an Improvise selection.
        /// Discards the chosen cards and draws the same number back.
        /// </summary>
        /// <param name="deck">Player's deck manager.</param>
        /// <param name="cardsToDiscard">Cards selected by the player to discard. Empty list = skip.</param>
        /// <returns>True if the Improvise was executed; false if unavailable or no cards given.</returns>
        public bool TryImprovise(DeckManager deck, List<CardData> cardsToDiscard)
        {
            if (!ImproviseAvailable)
            {
                GameLogger.LogWarning<PassiveResolver>("TryImprovise called but Improvise is not available");
                return false;
            }

            // Treat empty/null list as a skip (player chose not to use it)
            if (cardsToDiscard == null || cardsToDiscard.Count == 0)
            {
                _improviseUsed      = true;
                _improviseAvailable = false;
                GameLogger.LogInfo<PassiveResolver>("[Improvise] Skipped — no cards discarded");
                return false;
            }

            int count = cardsToDiscard.Count;
            foreach (var card in cardsToDiscard)
                deck.DiscardCard(card);

            int drawn = deck.DrawCards(count);
            _improviseUsed      = true;
            _improviseAvailable = false;

            GameLogger.LogInfo<PassiveResolver>(
                $"[Improvise] Discarded {count} card(s), drew {drawn} back");
            return true;
        }
    }
}
