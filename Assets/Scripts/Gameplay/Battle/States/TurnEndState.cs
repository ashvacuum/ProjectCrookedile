using Crookedile.Core;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Turn End State — cleanup effects, Judgment turn-limit check, victory check, advance.</summary>
    internal class TurnEndState : BattleStateBase
    {
        public TurnEndState(BattleManager manager)
            : base(manager) { }

        public override void OnEnter()
        {
            _manager.EndTurn();
            GameLogger.LogInfo<BattleManager>("Ending turn");

            EventBus.Publish(
                new TurnEndedEvent
                {
                    TurnNumber = _manager.CurrentTurn,
                    WasPlayerTurn = _manager.IsPlayerTurn,
                }
            );

            // Track player turns and check the Judgment turn limit.
            if (_manager.IsPlayerTurn)
            {
                _manager.IncrementPlayerTurnsElapsed();

                int remaining =
                    _manager.MaxTurns > 0
                        ? Mathf.Max(0, _manager.MaxTurns - _manager.PlayerTurnsElapsed)
                        : 0;

                EventBus.Publish(
                    new TurnLimitUpdatedEvent
                    {
                        PlayerTurnsElapsed = _manager.PlayerTurnsElapsed,
                        MaxTurns = _manager.MaxTurns,
                        TurnsRemaining = remaining,
                    }
                );

                if (_manager.MaxTurns > 0 && _manager.PlayerTurnsElapsed >= _manager.MaxTurns)
                {
                    // Judgment — outcome decided by majority opinion
                    var ledger = _manager.Opinion;
                    int threshold = ledger.MaxOpinion / 2;
                    bool isVictory = ledger.CurrentOpinion >= threshold;

                    _manager.SetBattleResult(
                        new BattleResult
                        {
                            isVictory = isVictory,
                            turnsToWin = _manager.CurrentTurn,
                            finalPlayerSupport = ledger.CurrentSupport,
                            finalPlayerHostility = _manager.PlayerStats.CurrentHostility,
                            finalOpinion = ledger.CurrentOpinion,
                            wasJudgmentVictory = isVictory,
                        }
                    );

                    EventBus.Publish(
                        new JudgmentEvent
                        {
                            FinalOpinion = ledger.CurrentOpinion,
                            Threshold = threshold,
                            IsVictory = isVictory,
                        }
                    );

                    GameLogger.LogInfo<BattleManager>(
                        $"Judgment! Opinion {ledger.CurrentOpinion}/{ledger.MaxOpinion} — {(isVictory ? "VICTORY" : "DEFEAT")}"
                    );

                    _manager.TransitionToState(BattleState.BattleEnd);
                    return;
                }
            }

            if (_manager.CheckVictoryConditions())
                _manager.TransitionToState(BattleState.BattleEnd);
            else
                _manager.TransitionToState(BattleState.TurnStart);
        }
    }
}
