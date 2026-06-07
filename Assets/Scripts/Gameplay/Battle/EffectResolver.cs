using System.Collections;
using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Resolves card and enemy-move effects during battle using the polymorphic
    /// <see cref="BattleEffect"/> hierarchy. Creates <see cref="EffectExecutionContext"/>
    /// from the current combatant state and delegates execution to each effect.
    /// </summary>
    [Debuggable("EffectResolver", LogLevel.Info)]
    public class EffectResolver
    {
        private BattleStats _playerStats;
        private BattleStats _opponentStats;
        private DeckManager _playerDeck;
        private IReadOnlyList<EnemyController> _allEnemies;
        private StatusEffectManager _playerStatusEffects;
        private StatusEffectManager _opponentStatusEffects;
        private string _attackerName = "Player";
        private int _attackerEnemyIndex = -1;

        private BattleManager _battleManager;

        /// <summary>
        /// Delay (in seconds) yielded between each effect in a multi-effect enemy move,
        /// so that floating damage texts appear sequentially rather than all at once.
        /// </summary>
        public float EffectStepDelay = 0.15f;

        public EffectResolver(
            BattleStats playerStats,
            BattleStats opponentStats,
            DeckManager playerDeck,
            IReadOnlyList<EnemyController> allEnemies = null,
            BattleManager battleManager = null
        )
        {
            _playerStats = playerStats;
            _opponentStats = opponentStats;
            _playerDeck = playerDeck;
            _allEnemies = allEnemies;
            _battleManager = battleManager;
            _playerStatusEffects = new StatusEffectManager("Player");
            _opponentStatusEffects = new StatusEffectManager("Opponent");
        }

        public StatusEffectManager PlayerStatusEffects => _playerStatusEffects;
        public StatusEffectManager OpponentStatusEffects => _opponentStatusEffects;

        /// <summary>
        /// Retargets the resolver to a different enemy.
        /// Call this before resolving any effect that should apply to a specific enemy.
        /// </summary>
        public void SetFocusedOpponent(
            BattleStats stats,
            StatusEffectManager statusEffects,
            int enemyIndex = -1,
            string enemyName = "Opponent"
        )
        {
            _opponentStats = stats;
            _opponentStatusEffects = statusEffects;
            _attackerEnemyIndex = enemyIndex;
            _attackerName = enemyName;
        }

        /// <summary>
        /// Creates an <see cref="EffectExecutionContext"/> from the resolver's current state.
        /// </summary>
        public EffectExecutionContext CreateContext(bool isPlayerCard)
        {
            BattleStats caster = isPlayerCard ? _playerStats : _opponentStats;
            BattleStats target = isPlayerCard ? _opponentStats : _playerStats;
            // The player's deck is the only deck in the game, so it is always the target of card
            // manipulation — including enemy moves that add/remove/discard the player's cards.
            DeckManager deck = _playerDeck;
            StatusEffectManager casterStatus = isPlayerCard
                ? _playerStatusEffects
                : _opponentStatusEffects;
            StatusEffectManager targetStatus = isPlayerCard
                ? _opponentStatusEffects
                : _playerStatusEffects;

            return new EffectExecutionContext(
                caster: caster,
                target: target,
                playerStats: _playerStats,
                isPlayerCard: isPlayerCard,
                deck: deck,
                allEnemies: _allEnemies,
                casterStatusEffects: casterStatus,
                targetStatusEffects: targetStatus,
                playerStatusEffects: _playerStatusEffects,
                battleManager: _battleManager,
                attackerName: isPlayerCard ? "Player" : _attackerName,
                attackerEnemyIndex: isPlayerCard ? -1 : _attackerEnemyIndex
            );
        }

        /// <summary>
        /// Resolves a card's <see cref="BattleEffect"/> list using polymorphic dispatch.
        /// </summary>
        public EffectExecutionContext ResolveCardEffects(
            CardData card,
            bool isPlayerCard,
            int[] amountOverrides = null
        )
        {
            GameLogger.LogInfo<EffectResolver>(
                $"Resolving effects for: {card.CardName} (Player: {isPlayerCard})"
            );

            var execCtx = CreateContext(isPlayerCard);

            // Honour the card's upgrade state — GetNewEffects returns the upgraded effect list when
            // the card is upgraded (and one is authored), else the base list. Identical to .Effects
            // for non-upgraded cards, so this is a no-op for them.
            var effects = card.GetNewEffects();
            if (effects == null)
                return execCtx;
            for (int j = 0; j < effects.Count; j++)
            {
                if (effects[j] == null)
                    continue;
                int? overrideAmount =
                    (amountOverrides != null && j < amountOverrides.Length)
                        ? (int?)amountOverrides[j]
                        : null;
                effects[j].Execute(execCtx, overrideAmount);
            }

            return execCtx;
        }

        /// <summary>
        /// Resolves an enemy move's <see cref="BattleEffect"/> list with a delay between effects.
        /// </summary>
        public IEnumerator ResolveEnemyMoveEffects(EnemyMoveData move)
        {
            if (move == null)
                yield break;

            GameLogger.LogInfo<EffectResolver>($"Resolving enemy move: {move.MoveName}");

            var execCtx = CreateContext(isPlayerCard: false);
            var effects = move.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                effects[i]?.Execute(execCtx);
                if (i < effects.Count - 1)
                    yield return new WaitForSeconds(EffectStepDelay);
            }
        }
    }
}
