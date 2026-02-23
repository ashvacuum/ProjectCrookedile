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
        // ─── State ────────────────────────────────────────────────────────────────

        private readonly EnemyData _enemyData;

        /// <summary>Index into the move list for Sequential pattern.</summary>
        private int _moveIndex;

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
            _enemyData   = enemyData;
            _moveIndex   = 0;
            Stats        = new BattleStats(enemyData.MaxResolve, maxActionPoints: 0);
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
        /// <returns>The selected move, or null if no moves are defined.</returns>
        public EnemyMoveData SelectNextMove()
        {
            var moves = _enemyData?.Moves;
            if (moves == null || moves.Count == 0)
            {
                CurrentIntent = null;
                return null;
            }

            switch (_enemyData.MovePattern)
            {
                case EnemyMovePattern.Sequential:
                    CurrentIntent = moves[_moveIndex % moves.Count];
                    _moveIndex++;
                    break;

                case EnemyMovePattern.Random:
                    int randomIndex = Random.Range(0, moves.Count);
                    CurrentIntent = moves[randomIndex];
                    break;

                default:
                    CurrentIntent = moves[0];
                    break;
            }

            return CurrentIntent;
        }
    }
}
