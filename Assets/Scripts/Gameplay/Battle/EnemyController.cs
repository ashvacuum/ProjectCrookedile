using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Crookedile.Data.Enemy;
using Crookedile.Gameplay;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Runtime state for a single enemy encounter.
    /// Owns move-selection logic (Sequential cycling or Random) and tracks
    /// which move was declared as intent for the current turn.
    ///
    /// This is a plain C# class — not a MonoBehaviour.
    /// BattleManager creates one instance per battle via new EnemyController(enemyData).
    /// </summary>
    public class EnemyController
    {
        // ─── Condition evaluators registry ───────────────────────────────────────
        // Add an entry here when introducing a new EnemyMoveCondition value —
        // no changes to IsMoveEligible() or any other method are needed.

        private static readonly Dictionary<EnemyMoveCondition, IMoveConditionEvaluator> ConditionEvaluators
            = new Dictionary<EnemyMoveCondition, IMoveConditionEvaluator>
        {
            [EnemyMoveCondition.None]                 = new NoConditionEvaluator(),
            [EnemyMoveCondition.OnlyIfNoMinionsAlive] = new NoMinionsAliveEvaluator(),
        };

        // ─── State ────────────────────────────────────────────────────────────────

        private readonly EnemyData            _enemyData;
        private readonly IMovePatternSelector _moveSelector;

        /// <summary>
        /// The move the enemy intends to execute this turn.
        /// Set by SelectNextMove() at the start of the player's turn
        /// and consumed by OpponentTurnState when the enemy acts.
        /// </summary>
        public EnemyMoveData CurrentIntent { get; private set; }

        /// <summary>The enemy definition this controller operates on.</summary>
        public EnemyData EnemyData => _enemyData;

        /// <summary>Battle stats for this enemy (Resolve, Composure, Hostility).</summary>
        public BattleStats Stats { get; }

        /// <summary>Status effect manager for this enemy.</summary>
        public StatusEffectManager StatusEffects { get; }

        /// <summary>True when this enemy's Resolve has reached zero.</summary>
        public bool IsDefeated => Stats.IsDefeated;

        // ─── Constructor ──────────────────────────────────────────────────────────

        public EnemyController(EnemyData enemyData)
        {
            _enemyData = enemyData;

            // Factory switch — acceptable here: construction happens once per enemy instance.
            // To add a new pattern, add an enum value + a new IMovePatternSelector class.
            _moveSelector = enemyData.MovePattern switch
            {
                EnemyMovePattern.Sequential       => new SequentialMoveSelector(),
                EnemyMovePattern.Random           => new RandomMoveSelector(),
                EnemyMovePattern.RandomSequential => new RandomSequentialMoveSelector(),
                _                                 => new SequentialMoveSelector(),
            };

            Stats = new BattleStats(enemyData.MaxResolve, maxActionPoints: 0, isPlayer: false);
            Stats.SetHostilityLimits(enemyData.MinHostility, enemyData.MaxHostility);
            Stats.SetHostility(enemyData.StartingHostility);
            StatusEffects = new StatusEffectManager(enemyData.EnemyName);

            foreach (var effect in enemyData.StartingEffects)
                StatusEffects.ApplyStatusEffect(effect.Type, effect.Stacks, effect.DurationType);
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Selects and stores the next move according to the enemy's move pattern.
        /// Call this at the START of the player's turn (Slay the Spire timing) so the
        /// player can see the threat before deciding which cards to play.
        /// </summary>
        /// <param name="allEnemies">
        /// All living and dead enemies in the current battle. Used to evaluate per-move
        /// conditions (e.g. <see cref="EnemyMoveCondition.OnlyIfNoMinionsAlive"/>).
        /// Pass <c>null</c> to skip condition filtering (all moves treated as eligible).
        /// </param>
        /// <returns>The selected move, or null if no moves are defined or all are blocked.</returns>
        public EnemyMoveData SelectNextMove(IReadOnlyList<EnemyController> allEnemies = null)
        {
            var moves = _enemyData?.Moves;
            if (moves == null || moves.Count == 0)
            {
                CurrentIntent = null;
                return null;
            }

            // Filter out moves whose conditions aren't met right now.
            // Condition evaluation is fully encapsulated in the ConditionEvaluators registry.
            var eligible = moves
                .Where(m => IsMoveEligible(m, allEnemies))
                .ToList();

            if (eligible.Count == 0)
            {
                CurrentIntent = null;
                return null;
            }

            // When receptive (negative hostility), prefer non-offensive moves.
            if (Stats.IsReceptive)
            {
                var nonOffensiveMoves = eligible
                    .Where(m => m.MoveType != EnemyMoveType.Attack
                             && m.MoveType != EnemyMoveType.OffensiveBuff
                             && m.MoveType != EnemyMoveType.DebuffAttack)
                    .ToList();

                if (nonOffensiveMoves.Count > 0)
                {
                    CurrentIntent = nonOffensiveMoves[Random.Range(0, nonOffensiveMoves.Count)];
                    return CurrentIntent;
                }
                // No non-offensive moves available — fall through to normal selection.
            }

            CurrentIntent = _moveSelector.SelectMove(eligible);
            return CurrentIntent;
        }

        // ─── Private Helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if <paramref name="move"/> should be included in the selection pool
        /// this turn. Delegates to the <see cref="ConditionEvaluators"/> registry —
        /// add new conditions there, not here.
        /// </summary>
        private bool IsMoveEligible(EnemyMoveData move, IReadOnlyList<EnemyController> allEnemies)
        {
            if (!ConditionEvaluators.TryGetValue(move.Condition, out var evaluator))
                return true; // Unknown condition — default to eligible.

            return evaluator.IsMet(move, allEnemies, this);
        }
    }
}
