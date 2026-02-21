using System;
using System.Collections.Generic;
using UnityEngine;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Manages the flow of card battles using a state machine.
    /// The player uses a card deck; the opponent is a scripted enemy with preset moves.
    /// Orchestrates turns, card plays, enemy move execution, and victory conditions.
    /// Instantiated per-battle, not a singleton.
    /// </summary>
    [Debuggable("BattleManager", LogLevel.Info)]
    public class BattleManager : MonoBehaviour
    {
        [Header("Battle Settings")]
        [SerializeField] private int _startingHandSize = 5;
        [SerializeField] private int _cardsPerTurn = 1;

        // State Machine
        private StateMachine<BattleState> _stateMachine;

        // Combatants
        private BattleStats _playerStats;
        private BattleStats _opponentStats;
        private OriginType _playerOrigin;

        // Player deck
        private DeckManager _playerDeck;

        // Enemy (replaces opponent deck + origin)
        private EnemyController _enemyController;

        // Effect Resolver
        private EffectResolver _effectResolver;

        // Turn tracking
        private int _currentTurn = 0;
        private bool _isPlayerTurn = true;

        // Battle result
        private BattleResult _battleResult;

        #region Properties

        public BattleState CurrentState => _stateMachine?.CurrentStateType ?? BattleState.Initialize;
        public BattleStats PlayerStats   => _playerStats;
        public BattleStats OpponentStats => _opponentStats;
        public DeckManager PlayerDeck    => _playerDeck;
        public EnemyController EnemyController => _enemyController;
        public int  CurrentTurn  => _currentTurn;
        public bool IsPlayerTurn => _isPlayerTurn;

        #endregion

        #region Initialization

        private void Awake()
        {
            InitializeStateMachine();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<EndTurnRequestedEvent>(OnEndTurnRequested);
            EventBus.Subscribe<PlayCardRequestedEvent>(OnPlayCardRequested);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<EndTurnRequestedEvent>(OnEndTurnRequested);
            EventBus.Unsubscribe<PlayCardRequestedEvent>(OnPlayCardRequested);
        }

        private void InitializeStateMachine()
        {
            _stateMachine = new StateMachine<BattleState>();

            _stateMachine.RegisterState(BattleState.Initialize,   new InitializeState(this));
            _stateMachine.RegisterState(BattleState.TurnStart,     new TurnStartState(this));
            _stateMachine.RegisterState(BattleState.PlayerTurn,    new PlayerTurnState(this));
            _stateMachine.RegisterState(BattleState.OpponentTurn,  new OpponentTurnState(this));
            _stateMachine.RegisterState(BattleState.TurnEnd,       new TurnEndState(this));
            _stateMachine.RegisterState(BattleState.BattleEnd,     new BattleEndState(this));
        }

        #endregion

        #region Battle Control

        /// <summary>
        /// Starts a new battle. The player brings a card deck; the enemy is defined by EnemyData.
        /// </summary>
        public void StartBattle(BattleSetup setup)
        {
            GameLogger.LogInfo<BattleManager>($"Starting battle: {setup.playerOrigin} vs {setup.enemyData?.EnemyName ?? "Unknown Enemy"}");

            // Player stats from origin
            OriginBattleStats playerOriginStats = setup.GetPlayerStats();
            _playerStats  = new BattleStats(playerOriginStats.maxResolve, playerOriginStats.maxActionPoints);
            _playerOrigin = setup.playerOrigin;

            // Enemy stats come directly from EnemyData (no OriginStats lookup)
            // Enemies have 0 AP — they don't spend action points to play cards
            _opponentStats = new BattleStats(setup.enemyData.MaxResolve, maxActionPoints: 0);

            // Player deck
            _playerDeck = new DeckManager(setup.playerDeck, "Player", 10);

            // Enemy controller (handles move selection, intent tracking)
            _enemyController = new EnemyController(setup.enemyData);

            // Effect resolver — player deck only; enemies have no deck
            _effectResolver = new EffectResolver(_playerStats, _opponentStats, _playerDeck);

            // Reset counters
            _currentTurn = 0;
            _isPlayerTurn = true;

            EventBus.Publish(new BattleStartedEvent { Setup = setup });
            _stateMachine.ChangeState(BattleState.Initialize);
        }

        /// <summary>Transitions to the next state in the battle flow.</summary>
        public void TransitionToState(BattleState newState)
        {
            _stateMachine.ChangeState(newState);
        }

        #endregion

        #region Event Handlers

        private void OnEndTurnRequested(EndTurnRequestedEvent evt)
        {
            if (!_isPlayerTurn || CurrentState != BattleState.PlayerTurn)
            {
                GameLogger.LogWarning<BattleManager>("Cannot end player turn — not player's turn!");
                return;
            }
            _stateMachine.ChangeState(BattleState.TurnEnd);
        }

        private void OnPlayCardRequested(PlayCardRequestedEvent evt)
        {
            if (!_isPlayerTurn || CurrentState != BattleState.PlayerTurn)
            {
                GameLogger.LogWarning<BattleManager>("Cannot play card — not player's turn!");
                return;
            }
            PlayCard(evt.Card, evt.HandIndex, isPlayer: true);
        }

        #endregion

        #region Card Playing

        /// <summary>Plays a card from the player's hand.</summary>
        private void PlayCard(CardData card, int handIndex, bool isPlayer)
        {
            // Only the player plays cards; enemies use scripted moves via EnemyController
            BattleStats stats = _playerStats;

            if (!CanPlayCard(card, stats))
            {
                GameLogger.LogWarning<BattleManager>($"Cannot play card: {card.CardName}");
                return;
            }

            PayCardCosts(card, stats);

            if (!_playerDeck.PlayCardAtIndex(handIndex))
            {
                GameLogger.LogError<BattleManager>("Failed to play card from hand");
                return;
            }

            EventBus.Publish(new CardPlayedEvent { Card = card, IsPlayer = true });
            _effectResolver.ResolveCardEffects(card, isPlayerCard: true);

            GameLogger.LogInfo<BattleManager>($"Player played: {card.CardName}");
        }

        private bool CanPlayCard(CardData card, BattleStats stats)
        {
            StatusEffectManager statusMgr = _effectResolver.PlayerStatusEffects;

            foreach (var cost in card.Costs)
            {
                if (cost.CostType == CostType.ActionPoints)
                {
                    int modifiedCost = statusMgr.ModifyCardCost(cost.CurrentAmount);
                    if (stats.CurrentActionPoints < modifiedCost)
                        return false;
                }
            }
            return true;
        }

        private void PayCardCosts(CardData card, BattleStats stats)
        {
            StatusEffectManager statusMgr = _effectResolver.PlayerStatusEffects;

            foreach (var cost in card.Costs)
            {
                if (cost.CostType == CostType.ActionPoints)
                {
                    int baseCost     = cost.GetActualCost(stats.CurrentActionPoints);
                    int modifiedCost = statusMgr.ModifyCardCost(baseCost);
                    stats.SpendActionPoints(modifiedCost);
                    GameLogger.LogInfo<BattleManager>($"Paid {modifiedCost} AP (base: {baseCost})");
                }
            }
        }

        #endregion

        #region Victory Conditions

        /// <summary>Checks if the battle has ended and caches the result.</summary>
        public bool CheckVictoryConditions()
        {
            bool playerDefeated   = _playerStats.IsDefeated;
            bool opponentDefeated = _opponentStats.IsDefeated;

            if (playerDefeated || opponentDefeated)
            {
                _battleResult = new BattleResult
                {
                    isVictory             = opponentDefeated,
                    turnsToWin            = _currentTurn,
                    finalPlayerResolve    = _playerStats.CurrentResolve,
                    finalPlayerComposure  = _playerStats.CurrentComposure,
                    finalPlayerHostility  = _playerStats.CurrentHostility
                };

                GameLogger.LogInfo("BattleManager", $"Battle ended: {(_battleResult.isVictory ? "Victory" : "Defeat")} in {_currentTurn} turns");
                return true;
            }
            return false;
        }

        /// <summary>Returns the cached battle result.</summary>
        public BattleResult GetBattleResult() => _battleResult;

        #endregion

        #region Turn Management

        /// <summary>Advances the turn counter and toggles whose turn it is.</summary>
        public void NextTurn()
        {
            _currentTurn++;
            _isPlayerTurn = !_isPlayerTurn;
            GameLogger.LogInfo<BattleManager>($"Turn {_currentTurn} — {(_isPlayerTurn ? "Player" : "Enemy")}");
        }

        /// <summary>Runs start-of-turn effects for the current combatant.</summary>
        public void StartTurn()
        {
            if (_isPlayerTurn)
            {
                _playerStats.StartTurn();
                _effectResolver.PlayerStatusEffects.OnTurnStart(_playerStats);
            }
            else
            {
                _opponentStats.StartTurn();
                _effectResolver.OpponentStatusEffects.OnTurnStart(_opponentStats);
            }
        }

        /// <summary>Runs end-of-turn effects for the current combatant.</summary>
        public void EndTurn()
        {
            if (_isPlayerTurn)
            {
                _playerStats.EndTurn();
                _effectResolver.PlayerStatusEffects.OnTurnEnd(_playerStats);
            }
            else
            {
                _opponentStats.EndTurn();
                _effectResolver.OpponentStatusEffects.OnTurnEnd(_opponentStats);
            }
        }

        #endregion

        private void Update()
        {
            _stateMachine?.Update();
        }

        #region Battle States

        /// <summary>Initialize State — draws the player's opening hand.</summary>
        private class InitializeState : State
        {
            private BattleManager _manager;
            public InitializeState(BattleManager manager) { _manager = manager; }

            public override void OnEnter()
            {
                GameLogger.LogInfo<BattleManager>("Initializing battle...");

                // Draw player's opening hand; enemies have no deck
                _manager._playerDeck.StartBattle(_manager._startingHandSize);

                _manager.TransitionToState(BattleState.TurnStart);
            }
        }

        /// <summary>
        /// Turn Start State — draws cards for the player and, on player turns,
        /// has the enemy declare their intent (Slay the Spire timing: player sees
        /// the threat BEFORE deciding which cards to play).
        /// </summary>
        private class TurnStartState : State
        {
            private BattleManager _manager;
            public TurnStartState(BattleManager manager) { _manager = manager; }

            public override void OnEnter()
            {
                _manager.NextTurn();
                _manager.StartTurn();

                GameLogger.LogInfo<BattleManager>($"Starting turn {_manager.CurrentTurn}");

                if (_manager.IsPlayerTurn)
                {
                    // Draw cards for the player
                    _manager._playerDeck.StartTurn(_manager._cardsPerTurn);

                    // Enemy declares their intent now so the player can plan around it
                    EnemyMoveData intent = _manager._enemyController.SelectNextMove();
                    if (intent != null)
                    {
                        EventBus.Publish(new EnemyIntentDeclaredEvent { Move = intent });
                        GameLogger.LogInfo<BattleManager>($"Enemy declares intent: {intent.MoveName}");
                    }
                }
                // Enemy turn: no card draw; intent was already declared during the previous player turn

                EventBus.Publish(new TurnStartedEvent
                {
                    TurnNumber   = _manager.CurrentTurn,
                    IsPlayerTurn = _manager.IsPlayerTurn
                });

                _manager.TransitionToState(_manager.IsPlayerTurn
                    ? BattleState.PlayerTurn
                    : BattleState.OpponentTurn);
            }
        }

        /// <summary>Player Turn State — waits for EndTurnRequestedEvent from the UI.</summary>
        private class PlayerTurnState : State
        {
            private BattleManager _manager;
            public PlayerTurnState(BattleManager manager) { _manager = manager; }

            public override void OnEnter() => GameLogger.LogInfo<BattleManager>("Player's turn started");
            public override void OnExit()  => GameLogger.LogInfo<BattleManager>("Player's turn ended");

            public override void OnUpdate()
            {
                // Waits for player to publish EndTurnRequestedEvent via the UI
            }
        }

        /// <summary>
        /// Opponent Turn State — executes the enemy's declared move then immediately ends.
        /// Enemy turns are instant (no waiting for input).
        /// </summary>
        private class OpponentTurnState : State
        {
            private BattleManager _manager;
            public OpponentTurnState(BattleManager manager) { _manager = manager; }

            public override void OnEnter()
            {
                GameLogger.LogInfo<BattleManager>("Enemy's turn started");

                EnemyMoveData move = _manager._enemyController.CurrentIntent;
                if (move != null)
                {
                    GameLogger.LogInfo<BattleManager>($"Enemy executes: {move.MoveName}");
                    _manager._effectResolver.ResolveEnemyMoveEffects(move);
                }
                else
                {
                    GameLogger.LogWarning<BattleManager>("Enemy has no intent — skipping move");
                }

                // Enemy turn is instant — transition immediately after effects resolve
                _manager.TransitionToState(BattleState.TurnEnd);
            }
        }

        /// <summary>Turn End State — cleanup effects, check victory, advance.</summary>
        private class TurnEndState : State
        {
            private BattleManager _manager;
            public TurnEndState(BattleManager manager) { _manager = manager; }

            public override void OnEnter()
            {
                _manager.EndTurn();
                GameLogger.LogInfo<BattleManager>("Ending turn");

                EventBus.Publish(new TurnEndedEvent
                {
                    TurnNumber     = _manager.CurrentTurn,
                    WasPlayerTurn  = _manager.IsPlayerTurn
                });

                if (_manager.CheckVictoryConditions())
                    _manager.TransitionToState(BattleState.BattleEnd);
                else
                    _manager.TransitionToState(BattleState.TurnStart);
            }
        }

        /// <summary>Battle End State — publishes the result event.</summary>
        private class BattleEndState : State
        {
            private BattleManager _manager;
            public BattleEndState(BattleManager manager) { _manager = manager; }

            public override void OnEnter()
            {
                BattleResult result = _manager.GetBattleResult();
                GameLogger.LogInfo<BattleManager>($"Battle ended — {(result.isVictory ? "VICTORY" : "DEFEAT")}");
                EventBus.Publish(new BattleEndedEvent { Result = result });
            }
        }

        #endregion
    }

    /// <summary>
    /// Setup data for initializing a battle.
    /// Player brings a card deck and origin; opponent is defined by EnemyData.
    /// </summary>
    [Serializable]
    public class BattleSetup
    {
        public OriginType playerOrigin;
        public OriginStats originStats;
        public List<CardData> playerDeck = new List<CardData>();

        /// <summary>The scripted enemy the player will fight.</summary>
        public EnemyData enemyData;

        /// <summary>Gets the player's battle stats based on their origin.</summary>
        public OriginBattleStats GetPlayerStats()
        {
            return originStats != null
                ? originStats.GetStatsForOrigin(playerOrigin)
                : new OriginBattleStats { maxResolve = 20, maxActionPoints = 3 };
        }
    }

    /// <summary>Result data from a completed battle.</summary>
    [Serializable]
    public class BattleResult
    {
        public bool isVictory;
        public int  turnsToWin;
        public int  finalPlayerResolve;
        public int  finalPlayerComposure;
        public int  finalPlayerHostility;

        // TODO: Add rewards when reward system exists
    }
}
