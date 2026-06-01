using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Runtime context for a single card play or enemy move.
    /// Passed to every <see cref="BattleEffect.Execute"/> call so effects can resolve
    /// targets, read battle state, and accumulate results for triggered effects.
    ///
    /// Created once per card/move by <see cref="EffectResolver.CreateContext"/>.
    /// Replaces the old split between the resolver's injected fields and <see cref="EffectContext"/>.
    /// </summary>
    public class EffectExecutionContext
    {
        #region Identities
        /// <summary>
        /// The effect caster — player's BattleStats for player cards;
        /// focused enemy's BattleStats for enemy moves.
        /// </summary>
        public BattleStats Caster { get; }

        /// <summary>
        /// The primary target — focused enemy for player cards; player for enemy moves.
        /// </summary>
        public BattleStats Target { get; }

        /// <summary>
        /// Always the player's BattleStats regardless of card direction.
        /// Used for multi-target resolution and <c>IsToPlayer</c> checks in events.
        /// </summary>
        public BattleStats PlayerStats { get; }

        /// <summary>True for player cards; false for enemy moves.</summary>
        public bool IsPlayerCard { get; }

        #endregion

        #region Services / dependencies
        /// <summary>
        /// The player's deck. <b>Null for enemy moves</b> — card manipulation effects
        /// must guard against null and return early.
        /// </summary>
        public DeckManager Deck { get; }

        /// <summary>All battle enemies — used for multi-target resolution.</summary>
        public IReadOnlyList<EnemyController> AllEnemies { get; }

        /// <summary>
        /// The active BattleManager — used by effects that operate on session-level stats
        /// (Support, Denial, Opinion) rather than per-combatant stats.
        /// </summary>
        public BattleManager BattleManager { get; }

        /// <summary>Status manager for the caster.</summary>
        public StatusEffectManager CasterStatusEffects { get; }

        /// <summary>Status manager for the primary target.</summary>
        public StatusEffectManager TargetStatusEffects { get; }

        /// <summary>Always the player's StatusEffectManager (direction-independent).</summary>
        public StatusEffectManager PlayerStatusEffects { get; }

        #endregion

        #region Attacker metadata
        /// <summary>Display name of the attacker — "Player" or an enemy name.</summary>
        internal string AttackerName { get; }

        /// <summary>
        /// Enemy index of the attacker, or −1 if the player is attacking.
        /// Used by <see cref="DamageDealtEvent"/> for floating-text positioning.
        /// </summary>
        internal int AttackerEnemyIndex { get; }

        #endregion

        #region Accumulated results (mutable during resolution)
        /// <summary>Total opinion-meter pressure applied by this card's effects.</summary>
        public int LastDamageDealt { get; set; }

        /// <summary>Total Opinion raised directly by this card's effects.</summary>
        public int LastHealAmount { get; set; }

        /// <summary>Total Support gained by this card's effects.</summary>
        public int LastSupportGained { get; set; }

        /// <summary>Total Support lost by this card's effects.</summary>
        public int LastSupportLost { get; set; }

        /// <summary>True if any target was defeated during this card's resolution.</summary>
        public bool LastTargetDied { get; set; }

        /// <summary>
        /// Set by <see cref="ExhaustThisCardEffect"/> so <c>BattleManager</c>
        /// can move the card from the discard pile to the exhaust pile after all effects resolve.
        /// </summary>
        public bool ShouldExhaust { get; set; }

        #endregion

        #region Constructor
        public EffectExecutionContext(
            BattleStats caster,
            BattleStats target,
            BattleStats playerStats,
            bool isPlayerCard,
            DeckManager deck,
            IReadOnlyList<EnemyController> allEnemies,
            StatusEffectManager casterStatusEffects,
            StatusEffectManager targetStatusEffects,
            StatusEffectManager playerStatusEffects,
            BattleManager battleManager = null,
            string attackerName = "Player",
            int attackerEnemyIndex = -1
        )
        {
            Caster = caster;
            Target = target;
            PlayerStats = playerStats;
            IsPlayerCard = isPlayerCard;
            Deck = deck;
            AllEnemies = allEnemies;
            CasterStatusEffects = casterStatusEffects;
            TargetStatusEffects = targetStatusEffects;
            PlayerStatusEffects = playerStatusEffects;
            BattleManager = battleManager;
            AttackerName = attackerName;
            AttackerEnemyIndex = attackerEnemyIndex;
        }

        #endregion

        #region Target resolution
        /// <summary>
        /// Resolves a <see cref="TargetType"/> into a list of (BattleStats, StatusEffectManager) pairs.
        /// Single-target types return 1 element; multi-target types return N (one per living combatant).
        /// Absorbs the logic previously in <c>EffectResolver.ResolveTargetPairs</c>.
        /// </summary>
        public List<(BattleStats stats, StatusEffectManager statusMgr)> GetTargets(
            TargetType targetType
        )
        {
            var pairs = new List<(BattleStats, StatusEffectManager)>();

            switch (targetType)
            {
                case TargetType.Self:
                    pairs.Add((Caster, CasterStatusEffects));
                    break;

                case TargetType.Opponent:
                    pairs.Add((Target, TargetStatusEffects));
                    break;

                case TargetType.Random:
                    if (IsPlayerCard)
                    {
                        // Player card: pick a random living enemy
                        if (AllEnemies != null)
                        {
                            var living = new List<(BattleStats, StatusEffectManager)>();
                            foreach (var e in AllEnemies)
                                if (!e.IsDefeated)
                                    living.Add((e.Stats, e.StatusEffects));
                            if (living.Count > 0)
                                pairs.Add(living[Random.Range(0, living.Count)]);
                        }
                    }
                    else
                    {
                        // Enemy card: always targets the player
                        pairs.Add((Target, TargetStatusEffects));
                    }
                    break;

                case TargetType.All:
                    // Player + every living enemy
                    pairs.Add((PlayerStats, PlayerStatusEffects));
                    if (AllEnemies != null)
                        foreach (var e in AllEnemies)
                            if (!e.IsDefeated)
                                pairs.Add((e.Stats, e.StatusEffects));
                    break;

                case TargetType.AllOpponents:
                    if (IsPlayerCard)
                    {
                        // Hit all living enemies
                        if (AllEnemies != null)
                            foreach (var e in AllEnemies)
                                if (!e.IsDefeated)
                                    pairs.Add((e.Stats, e.StatusEffects));
                    }
                    else
                    {
                        // Enemy card — only one player to target
                        pairs.Add((PlayerStats, PlayerStatusEffects));
                    }
                    break;

                case TargetType.AllAllies:
                    if (!IsPlayerCard)
                    {
                        // Buff all living enemies
                        if (AllEnemies != null)
                            foreach (var e in AllEnemies)
                                if (!e.IsDefeated)
                                    pairs.Add((e.Stats, e.StatusEffects));
                    }
                    else
                    {
                        // Player has no other allies — same as Self
                        pairs.Add((PlayerStats, PlayerStatusEffects));
                    }
                    break;

                default:
                    GameLogger.LogWarning<EffectExecutionContext>(
                        $"Unhandled TargetType {targetType} — falling back to Opponent"
                    );
                    pairs.Add((Target, TargetStatusEffects));
                    break;
            }

            return pairs;
        }

        /// <summary>
        /// Looks up the <see cref="StatusEffectManager"/> for any <see cref="BattleStats"/>
        /// in the current battle — needed by multi-target effects to get the right manager
        /// per target, not just for the focused opponent.
        /// </summary>
        public StatusEffectManager GetStatusEffectManager(BattleStats stats)
        {
            if (stats == PlayerStats)
                return PlayerStatusEffects;

            if (AllEnemies != null)
                foreach (var enemy in AllEnemies)
                    if (enemy.Stats == stats)
                        return enemy.StatusEffects;

            GameLogger.LogWarning<EffectExecutionContext>(
                "GetStatusEffectManager: unknown BattleStats — returning null"
            );
            return null;
        }

        /// <summary>
        /// Retrieves the integer value indicated by <paramref name="source"/>.
        /// Returns 0 for <see cref="EffectContextValue.FixedAmount"/> — the caller
        /// should use the authored amount on the effect in that case.
        /// </summary>
        public int GetValue(EffectContextValue source) =>
            source switch
            {
                EffectContextValue.LastDamageDealt => LastDamageDealt,
                EffectContextValue.LastHealAmount => LastHealAmount,
                EffectContextValue.LastShieldGained => LastSupportGained,
                EffectContextValue.LastShieldLost => LastSupportLost,
                EffectContextValue.CurrentShield => BattleManager?.CurrentSupport ?? 0,
                EffectContextValue.CurrentHostility => Target?.CurrentHostility ?? 0,
                EffectContextValue.HostileEnemyCount => CountLivingEnemies(e => e.Stats.IsHostile),
                EffectContextValue.ReceptiveEnemyCount => CountLivingEnemies(e =>
                    e.Stats.IsReceptive
                ),
                _ => 0, // FixedAmount / None — use authored value
            };

        /// <summary>
        /// Counts living enemies matching <paramref name="predicate"/>.
        /// Returns 0 if <see cref="AllEnemies"/> is null (e.g. enemy move context).
        /// </summary>
        private int CountLivingEnemies(System.Func<EnemyController, bool> predicate)
        {
            if (AllEnemies == null)
                return 0;
            int count = 0;
            foreach (var enemy in AllEnemies)
                if (!enemy.IsDefeated && predicate(enemy))
                    count++;
            return count;
        }
    }
}
        #endregion
