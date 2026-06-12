using Crookedile.Core;
using Crookedile.Data.Enemy;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Turn Start State — draws cards for the player and, on player turns,
    /// has the enemy declare their intent (Slay the Spire timing: player sees
    /// the threat BEFORE deciding which cards to play).
    /// </summary>
    internal class TurnStartState : BattleStateBase
    {
        public TurnStartState(BattleManager manager)
            : base(manager) { }

        public override void OnEnter()
        {
            _manager.NextTurn();
            _manager.StartTurn();

            GameLogger.LogInfo<BattleManager>($"Starting turn {_manager.CurrentTurn}");

            if (_manager.IsPlayerTurn)
            {
                // Track the player's personal turn count and fire per-player-turn passives
                _manager.FirePlayerTurnStartPassives();

                // Count bonus draws BEFORE snapshotting (BecameHostileThisTurn reflects last turn's escalations)
                int bonusDraws = 0;
                foreach (var enemy in _manager.Enemies)
                {
                    if (
                        !enemy.IsDefeated
                        && (enemy.Stats.IsHostile || enemy.BecameHostileThisTurn)
                    )
                        bonusDraws++;
                }

                // Snapshot hostility for the new turn (resets BecameHostileThisTurn on all enemies)
                foreach (var enemy in _manager.Enemies)
                    enemy.SnapshotHostilityForTurn();

                // Draw cards — base draw plus one per hostile/newly-hostile enemy
                int totalDraw = _manager.CardsPerTurn + bonusDraws;
                _manager.PlayerDeck.StartTurn(totalDraw);

                if (bonusDraws > 0)
                    GameLogger.LogInfo<BattleManager>(
                        $"Hostile crowd: drawing +{bonusDraws} extra card(s) ({totalDraw} total)"
                    );

                // Every living enemy declares their intent (Slay the Spire timing)
                for (int i = 0; i < _manager.Enemies.Count; i++)
                {
                    var enemy = _manager.Enemies[i];
                    if (enemy.IsDefeated)
                        continue;
                    EnemyMoveData intent = enemy.SelectNextMove(_manager.Enemies);
                    if (intent != null)
                    {
                        EventBus.Publish(
                            new EnemyIntentDeclaredEvent { Move = intent, EnemyIndex = i }
                        );
                        GameLogger.LogInfo<BattleManager>(
                            $"Enemy [{i}] {enemy.EnemyData.EnemyName} declares: {intent.MoveName}"
                        );
                    }
                }
            }
            // Enemy turn: no card draw; intent was already declared during the previous player turn

            EventBus.Publish(
                new TurnStartedEvent
                {
                    TurnNumber = _manager.CurrentTurn,
                    IsPlayerTurn = _manager.IsPlayerTurn,
                }
            );

            _manager.TransitionToState(
                _manager.IsPlayerTurn ? BattleState.PlayerTurn : BattleState.OpponentTurn
            );
        }
    }
}
