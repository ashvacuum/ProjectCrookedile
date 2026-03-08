using System.Collections.Generic;
using Crookedile.Data;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Snapshot of all runtime state needed to evaluate a <see cref="PassiveConditionBase"/>.
    /// Created once per <see cref="PassiveRunner.DispatchEvent"/> call and shared across
    /// all passives processing that same event — except <see cref="TriggerFireCount"/> which is
    /// set per passive before condition evaluation.
    /// </summary>
    public class PassiveEvaluationContext
    {
        /// <summary>Player's live battle statistics.</summary>
        public BattleStats PlayerStats { get; }

        /// <summary>Player's deck manager. May be null for enemy-sourced dispatches.</summary>
        public DeckManager Deck { get; }

        /// <summary>All currently active enemies in the encounter.</summary>
        public IReadOnlyList<EnemyController> Enemies { get; }

        /// <summary>Player's status effect manager.</summary>
        public StatusEffectManager PlayerStatusEffects { get; }

        /// <summary>Number of player turns elapsed (set by PassiveResolver.FireTurnStart).</summary>
        public int PlayerTurnNumber { get; }

        /// <summary>
        /// How many times the owning <see cref="BattlePassive"/>'s trigger has fired this battle.
        /// Set by <see cref="BattlePassive.TryFire"/> before calling condition evaluation.
        /// Used by <see cref="Crookedile.Gameplay.Battle.Conditions.NthEventCondition"/>.
        /// </summary>
        public int TriggerFireCount { get; set; }

        /// <summary>The event that caused this dispatch.</summary>
        public PassiveEventContext EventCtx { get; }

        public PassiveEvaluationContext(
            BattleStats            playerStats,
            DeckManager            deck,
            IReadOnlyList<EnemyController> enemies,
            StatusEffectManager    playerStatusEffects,
            int                    playerTurnNumber,
            PassiveEventContext    eventCtx)
        {
            PlayerStats         = playerStats;
            Deck                = deck;
            Enemies             = enemies;
            PlayerStatusEffects = playerStatusEffects;
            PlayerTurnNumber    = playerTurnNumber;
            EventCtx            = eventCtx;
        }
    }
}
