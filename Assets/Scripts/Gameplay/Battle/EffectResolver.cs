using System.Collections.Generic;
using System.Threading;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Utilities;
using Cysharp.Threading.Tasks;
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
            if (allEnemies != null)
            {
                for (int i = 0; i < allEnemies.Count; i++)
                    if (allEnemies[i].Stats == opponentStats)
                    {
                        SetFocusedOpponent(
                            opponentStats,
                            allEnemies[i].StatusEffects,
                            i,
                            allEnemies[i].EnemyData.EnemyName
                        );
                        break;
                    }
            }
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
        public EffectExecutionContext CreateContext(bool isPlayerCard) =>
            isPlayerCard
                ? BuildContext(
                    _playerStats,
                    _playerStatusEffects,
                    _opponentStats,
                    _opponentStatusEffects,
                    isPlayerCard: true,
                    "Player"
                )
                : BuildContext(
                    _opponentStats,
                    _opponentStatusEffects,
                    _playerStats,
                    _playerStatusEffects,
                    isPlayerCard: false,
                    _attackerName,
                    _attackerEnemyIndex
                );

        /// <summary>
        /// Builds a context for an enemy-owned passive reacting to its own event — caster is
        /// that enemy, target is the player. Built directly from stored state rather than
        /// <see cref="CreateContext"/>/<see cref="SetFocusedOpponent"/> so it never disturbs
        /// whichever enemy the resolver is currently focused on for turn resolution.
        /// </summary>
        public EffectExecutionContext CreateEnemyPassiveContext(EnemyController owner) =>
            BuildContext(
                owner.Stats,
                owner.StatusEffects,
                _playerStats,
                _playerStatusEffects,
                isPlayerCard: false,
                owner.EnemyData != null ? owner.EnemyData.EnemyName : "Enemy",
                owner.Stats.OwnerEnemyIndex
            );

        /// <summary>
        /// Builds a context for a player-side effect aimed at one named enemy (a status payoff,
        /// say) without retargeting the resolver's focused opponent.
        /// </summary>
        public EffectExecutionContext CreatePlayerContextAgainst(
            EnemyController enemy,
            string attackerName
        ) =>
            BuildContext(
                _playerStats,
                _playerStatusEffects,
                enemy.Stats,
                enemy.StatusEffects,
                isPlayerCard: true,
                attackerName,
                enemy.Stats.OwnerEnemyIndex
            );

        /// <summary>
        /// The one place a context is constructed. Fills in the services every context shares —
        /// the player, the player's deck (the only deck in the game, so enemy moves act on it
        /// too), the enemy row and the battle — so callers say only who acts on whom.
        /// </summary>
        private EffectExecutionContext BuildContext(
            BattleStats caster,
            StatusEffectManager casterStatusEffects,
            BattleStats target,
            StatusEffectManager targetStatusEffects,
            bool isPlayerCard,
            string attackerName,
            int attackerEnemyIndex = -1,
            IReadOnlyDictionary<TargetType, int> fixedAudienceTargets = null,
            bool isBossMove = false
        ) =>
            new EffectExecutionContext(
                caster,
                target,
                _playerStats,
                isPlayerCard,
                _playerDeck,
                _allEnemies,
                casterStatusEffects,
                targetStatusEffects,
                _playerStatusEffects,
                _battleManager,
                attackerName,
                attackerEnemyIndex,
                fixedAudienceTargets,
                isBossMove
            );

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
            // Effects know the card they're printed on (CardSelectionMode.ThisCard,
            // MoveOwnerCardEffect). Passive dispatches set this separately in PassiveResolver.
            execCtx.OwnerCard = card;
            // An armed Sway multiplier (Overpromise) waits for the next card that deals Sway.
            int armedSway = 1;
            if (isPlayerCard && _battleManager != null)
                execCtx.SwayMultiplier = armedSway = _battleManager.Celebrity.NextSwayMultiplier;

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

            // Spent once this card dealt Sway with it, unless the card armed a new one.
            if (
                armedSway > 1
                && execCtx.LastDamageDealt > 0
                && _battleManager.Celebrity.NextSwayMultiplier == armedSway
            )
                _battleManager.Celebrity.NextSwayMultiplier = 1;

            return execCtx;
        }

        /// <summary>
        /// Resolves an enemy move's <see cref="BattleEffect"/> list with a delay between effects.
        /// </summary>
        public async UniTask ResolveEnemyMoveEffects(EnemyMoveData move, CancellationToken ct)
        {
            if (move == null)
                return;

            GameLogger.LogInfo<EffectResolver>($"Resolving enemy move: {move.MoveName}");

            var execCtx = CreateContext(isPlayerCard: false);
            var effects = move.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                effects[i]?.Execute(execCtx);
                if (i < effects.Count - 1)
                    await UniTask.WaitForSeconds(EffectStepDelay, cancellationToken: ct);
            }
        }

        public EffectExecutionContext CreateBossContext(BossController boss, int intentIndex = -1) =>
            BuildContext(
                boss.Stats,
                boss.StatusEffects,
                _playerStats,
                _playerStatusEffects,
                isPlayerCard: false,
                boss.Data.DisplayName,
                fixedAudienceTargets: boss.GetAudienceTargets(intentIndex),
                isBossMove: true
            );

        public async UniTask ResolveBossMoveEffects(
            BossController boss,
            EnemyMoveData move,
            CancellationToken ct
        )
        {
            var context = CreateBossContext(boss, boss.ResolvedIntentCount - 1);
            foreach (var effect in move.Effects)
            {
                ct.ThrowIfCancellationRequested();
                effect?.Execute(context);
                if (
                    _battleManager.CurrentState == BattleState.BattleEnd
                    || _battleManager.CheckAndEndBattleIfOver()
                )
                    return;

                if (EffectStepDelay > 0)
                    await UniTask.WaitForSeconds(EffectStepDelay, cancellationToken: ct);
            }
        }
    }
}
