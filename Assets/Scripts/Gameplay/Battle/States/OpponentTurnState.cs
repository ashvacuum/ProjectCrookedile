using Crookedile.Core;
using Crookedile.Data.Enemy;
using Crookedile.Utilities;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Opponent Turn State — waits the opponent-turn delay, then resolves all
    /// living enemies' declared moves and transitions to TurnEnd.
    /// The delay gives the player a visible pause before damage lands.
    /// </summary>
    internal class OpponentTurnState : BattleStateBase
    {
        public OpponentTurnState(BattleManager manager)
            : base(manager) { }

        public override void OnEnter()
        {
            ExecuteAfterDelay(_manager.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid ExecuteAfterDelay(System.Threading.CancellationToken ct)
        {
            await UniTask.WaitForSeconds(_manager.OpponentTurnDelay, cancellationToken: ct);

            GameLogger.LogInfo<BattleManager>("Enemy turn started — all living enemies act");

            // Capture count before the loop so summoned enemies act next turn, not this one.
            int enemyCount = _manager.Enemies.Count;

            // Two-pass resolution. Pass 1: modifier intents (e.g. RileOthers) resolve first so
            // their board changes — amplifying allies' hostility, summoning bodies — land before
            // the direct hits. Pass 2: direct intents (attacks, shields) resolve left to right.
            for (int i = 0; i < enemyCount; i++)
            {
                var intent = _manager.Enemies[i].CurrentIntent;
                if (intent != null && IsModifierIntent(intent.MoveType))
                {
                    await ResolveSingleEnemyAction(i, ct);
                    if (_manager.CurrentState == BattleState.BattleEnd)
                        return;
                }
            }

            for (int i = 0; i < enemyCount; i++)
            {
                var intent = _manager.Enemies[i].CurrentIntent;
                if (intent != null && !IsModifierIntent(intent.MoveType))
                {
                    await ResolveSingleEnemyAction(i, ct);
                    if (_manager.CurrentState == BattleState.BattleEnd)
                        return;
                }
            }

            // Restore resolver to the player's current focused target
            if (_manager.FocusedEnemy != null)
                _manager.Resolver.SetFocusedOpponent(
                    _manager.FocusedEnemy.Stats,
                    _manager.FocusedEnemy.StatusEffects,
                    _manager.FocusedEnemyIndex,
                    _manager.FocusedEnemy.EnemyData.EnemyName
                );

            _manager.TransitionToState(BattleState.TurnEnd);
        }

        /// <summary>
        /// Resolves one enemy's declared action: stun / receptive-skip checks, the acting
        /// signal + pause, effect resolution, and SummonMinion handling. Ends the battle early
        /// (transitioning to BattleEnd) if the player is defeated mid-action — callers should
        /// stop once <see cref="BattleManager.CurrentState"/> is BattleEnd.
        /// </summary>
        private async UniTask ResolveSingleEnemyAction(
            int i,
            System.Threading.CancellationToken ct
        )
        {
            var enemy = _manager.Enemies[i];
            if (enemy.IsDefeated || enemy.CurrentIntent == null)
                return;

            // Stunned or Silenced enemies skip their entire action for this turn.
            // (Silence is the Faith Leader's "shut them up" — also how a Hardened enemy is handled
            // when pacify-conversion can't convert it.)
            if (
                enemy.StatusEffects.HasStatus<StunnedStatus>()
                || enemy.StatusEffects.HasStatus<SilencedStatus>()
            )
            {
                GameLogger.LogInfo<BattleManager>(
                    $"Enemy [{i}] {enemy.EnemyData.EnemyName} is silenced/stunned — skipping action"
                );
                return;
            }

            // Doubt (pacify): a doubting enemy may hold back its action (soft skip, 25% per stack).
            int doubt = enemy.StatusEffects.GetStacks<DoubtStatus>();
            if (doubt > 0)
            {
                float doubtSkip = Mathf.Clamp01(doubt * 0.25f);
                if (Random.value < doubtSkip)
                {
                    GameLogger.LogInfo<BattleManager>(
                        $"Enemy [{i}] {enemy.EnemyData.EnemyName} hesitates (Doubt skip {doubtSkip:P0})"
                    );
                    EventBus.Publish(
                        new EnemySkippedTurnEvent
                        {
                            EnemyIndex = i,
                            EnemyName = enemy.EnemyData.EnemyName,
                        }
                    );
                    return;
                }
            }

            // Receptive enemies have a chance to hold back (20% per negative hostility stack).
            if (enemy.Stats.IsReceptive)
            {
                float skipChance = Mathf.Clamp01(Mathf.Abs(enemy.Stats.CurrentHostility) * 0.20f);
                if (Random.value < skipChance)
                {
                    GameLogger.LogInfo<BattleManager>(
                        $"Enemy [{i}] {enemy.EnemyData.EnemyName} is Receptive — held back "
                            + $"(skip chance {skipChance:P0})"
                    );
                    EventBus.Publish(
                        new EnemySkippedTurnEvent
                        {
                            EnemyIndex = i,
                            EnemyName = enemy.EnemyData.EnemyName,
                        }
                    );
                    return;
                }
            }

            // Signal the UI: this enemy is about to act (shake + highlight intent panel)
            EventBus.Publish(new EnemyActingEvent { EnemyIndex = i, Move = enemy.CurrentIntent });

            // Brief pause so the player sees the signal before damage lands
            await UniTask.WaitForSeconds(_manager.PerEnemyAttackDelay, cancellationToken: ct);

            GameLogger.LogInfo<BattleManager>(
                $"Enemy [{i}] {enemy.EnemyData.EnemyName} executes: {enemy.CurrentIntent.MoveName}"
            );

            // Temporarily point EffectResolver at this enemy as the caster
            _manager.Resolver.SetFocusedOpponent(
                enemy.Stats,
                enemy.StatusEffects,
                i,
                enemy.EnemyData.EnemyName
            );
            var move = enemy.CurrentIntent;
            await _manager.Resolver.ResolveEnemyMoveEffects(move, ct);

            // If the player was defeated by this move, end the battle before any further actions.
            if (_manager.CheckAndEndBattleIfOver())
                return;

            // Handle SummonMinion moves after normal effects resolve.
            if (move.MoveType == EnemyMoveType.SummonMinion && move.MinionToSummon != null)
                _manager.SummonMinions(move.MinionToSummon, move.MinionCount);
        }

        /// <summary>
        /// Modifier intents reshape the board (other enemies / new bodies) and resolve before
        /// direct intents so their effects apply this turn. Add future board-modifiers (e.g. Sway)
        /// here.
        /// </summary>
        private static bool IsModifierIntent(EnemyMoveType type) =>
            type == EnemyMoveType.RileOthers || type == EnemyMoveType.SummonMinion;
    }
}
