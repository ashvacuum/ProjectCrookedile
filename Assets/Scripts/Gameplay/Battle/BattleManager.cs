using System;
using System.Collections.Generic;
using UnityEngine;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Manages the flow of card battles using a state machine.
    /// Orchestrates combat between player and opponent, handling turns, card plays, and victory conditions.
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
        private OriginType _opponentOrigin;

        // Deck Managers
        private DeckManager _playerDeck;
        private DeckManager _opponentDeck;

        // Effect Resolver
        private EffectResolver _effectResolver;

        // Turn tracking
        private int _currentTurn = 0;
        private bool _isPlayerTurn = true;

        // Battle result
        private BattleResult _battleResult;

        #region Properties

        public BattleState CurrentState => _stateMachine?.CurrentStateType ?? BattleState.Initialize;
        public BattleStats PlayerStats => _playerStats;
        public BattleStats OpponentStats => _opponentStats;
        public DeckManager PlayerDeck => _playerDeck;
        public DeckManager OpponentDeck => _opponentDeck;
        public int CurrentTurn => _currentTurn;
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

            // Register battle states
            _stateMachine.RegisterState(BattleState.Initialize, new InitializeState(this));
            _stateMachine.RegisterState(BattleState.TurnStart, new TurnStartState(this));
            _stateMachine.RegisterState(BattleState.PlayerTurn, new PlayerTurnState(this));
            _stateMachine.RegisterState(BattleState.OpponentTurn, new OpponentTurnState(this));
            _stateMachine.RegisterState(BattleState.TurnEnd, new TurnEndState(this));
            _stateMachine.RegisterState(BattleState.BattleEnd, new BattleEndState(this));
        }

        #endregion

        #region Battle Control

        /// <summary>
        /// Starts a new battle with specified combatants.
        /// </summary>
        public void StartBattle(BattleSetup setup)
        {
            GameLogger.LogInfo<BattleManager>($"Starting battle: {setup.playerOrigin} vs {setup.opponentOrigin}");

            // Get origin-specific stats
            OriginBattleStats playerStats = setup.GetPlayerStats();
            OriginBattleStats opponentStats = setup.GetOpponentStats();

            // Initialize combatant stats
            _playerStats = new BattleStats(playerStats.maxResolve, playerStats.maxActionPoints);
            _opponentStats = new BattleStats(opponentStats.maxResolve, opponentStats.maxActionPoints);
            _playerOrigin = setup.playerOrigin;
            _opponentOrigin = setup.opponentOrigin;

            // Initialize deck managers
            _playerDeck = new DeckManager(setup.playerDeck, "Player", 10);
            _opponentDeck = new DeckManager(setup.opponentDeck, "Opponent", 10);

            // Initialize effect resolver
            _effectResolver = new EffectResolver(_playerStats, _opponentStats, _playerDeck, _opponentDeck);

            // Reset turn counter
            _currentTurn = 0;
            _isPlayerTurn = true;

            // Publish battle started event
            EventBus.Publish(new BattleStartedEvent { Setup = setup });

            // Start state machine
            _stateMachine.ChangeState(BattleState.Initialize);
        }

        /// <summary>
        /// Transitions to the next state in the battle flow.
        /// </summary>
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
                GameLogger.LogWarning<BattleManager>("Cannot end player turn - not player's turn!");
                return;
            }

            _stateMachine.ChangeState(BattleState.TurnEnd);
        }

        private void OnPlayCardRequested(PlayCardRequestedEvent evt)
        {
            if (!_isPlayerTurn || CurrentState != BattleState.PlayerTurn)
            {
                GameLogger.LogWarning<BattleManager>("Cannot play card - not player's turn!");
                return;
            }

            PlayCard(evt.Card, evt.HandIndex, true);
        }

        #endregion

        #region Card Playing

        /// <summary>
        /// Plays a card from hand.
        /// </summary>
        private void PlayCard(CardData card, int handIndex, bool isPlayer)
        {
            DeckManager deck = isPlayer ? _playerDeck : _opponentDeck;
            BattleStats stats = isPlayer ? _playerStats : _opponentStats;

            // Check if card can be played (costs, etc.)
            if (!CanPlayCard(card, stats))
            {
                GameLogger.LogWarning<BattleManager>($"Cannot play card: {card.CardName}");
                return;
            }

            // Pay costs
            PayCardCosts(card, stats);

            // Play card (move to discard)
            if (!deck.PlayCardAtIndex(handIndex))
            {
                GameLogger.LogError<BattleManager>("Failed to play card from hand");
                return;
            }

            // Publish card played event
            EventBus.Publish(new CardPlayedEvent { Card = card, IsPlayer = isPlayer });

            // Resolve effects
            _effectResolver.ResolveCardEffects(card, isPlayer);

            GameLogger.LogInfo<BattleManager>($"{(isPlayer ? "Player" : "Opponent")} played: {card.CardName}");
        }

        private bool CanPlayCard(CardData card, BattleStats stats)
        {
            // Check costs
            foreach (var cost in card.Costs)
            {
                if (cost.CostType == CostType.ActionPoints)
                {
                    if (!cost.CanAfford(stats.CurrentActionPoints))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void PayCardCosts(CardData card, BattleStats stats)
        {
            foreach (var cost in card.Costs)
            {
                if (cost.CostType == CostType.ActionPoints)
                {
                    int actualCost = cost.GetActualCost(stats.CurrentActionPoints);
                    stats.SpendActionPoints(actualCost);
                }
            }
        }

        #endregion

        #region Victory Conditions

        /// <summary>
        /// Checks if battle has ended and determines winner.
        /// </summary>
        public bool CheckVictoryConditions()
        {
            bool playerDefeated = _playerStats.IsDefeated;
            bool opponentDefeated = _opponentStats.IsDefeated;

            if (playerDefeated || opponentDefeated)
            {
                _battleResult = new BattleResult
                {
                    isVictory = opponentDefeated,
                    turnsToWin = _currentTurn,
                    finalPlayerResolve = _playerStats.CurrentResolve,
                    finalPlayerComposure = _playerStats.CurrentComposure,
                    finalPlayerHostility = _playerStats.CurrentHostility
                };

                GameLogger.LogInfo("BattleManager", $"Battle ended: {(_battleResult.isVictory ? "Victory" : "Defeat")} in {_currentTurn} turns");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the battle result.
        /// </summary>
        public BattleResult GetBattleResult()
        {
            return _battleResult;
        }

        #endregion

        #region Turn Management

        /// <summary>
        /// Advances to the next turn.
        /// </summary>
        public void NextTurn()
        {
            _currentTurn++;
            _isPlayerTurn = !_isPlayerTurn;
            GameLogger.LogInfo<BattleManager>($"Turn {_currentTurn} - {(_isPlayerTurn ? "Player" : "Opponent")}");
        }

        /// <summary>
        /// Starts a new turn for the current combatant.
        /// </summary>
        public void StartTurn()
        {
            if (_isPlayerTurn)
            {
                _playerStats.StartTurn();
            }
            else
            {
                _opponentStats.StartTurn();
            }
        }

        /// <summary>
        /// Ends the current turn.
        /// </summary>
        public void EndTurn()
        {
            if (_isPlayerTurn)
            {
                _playerStats.EndTurn();
            }
            else
            {
                _opponentStats.EndTurn();
            }
        }

        #endregion

        private void Update()
        {
            _stateMachine?.Update();
        }

        #region Battle States

        /// <summary>
        /// Initialize State - Sets up the battle.
        /// </summary>
        private class InitializeState : State
        {
            private BattleManager _manager;

            public InitializeState(BattleManager manager)
            {
                _manager = manager;
            }

            public override void OnEnter()
            {
                GameLogger.LogInfo<BattleManager>("Initializing battle...");

                // Draw initial hands
                _manager._playerDeck.StartBattle(_manager._startingHandSize);
                _manager._opponentDeck.StartBattle(_manager._startingHandSize);

                // Move to turn start
                _manager.TransitionToState(BattleState.TurnStart);
            }
        }

        /// <summary>
        /// Turn Start State - Begins a new turn.
        /// </summary>
        private class TurnStartState : State
        {
            private BattleManager _manager;

            public TurnStartState(BattleManager manager)
            {
                _manager = manager;
            }

            public override void OnEnter()
            {
                _manager.NextTurn();
                _manager.StartTurn();

                GameLogger.LogInfo<BattleManager>($"Starting turn {_manager.CurrentTurn}");

                // Draw cards
                if (_manager.IsPlayerTurn)
                {
                    _manager._playerDeck.StartTurn(_manager._cardsPerTurn);
                }
                else
                {
                    _manager._opponentDeck.StartTurn(_manager._cardsPerTurn);
                }

                // Publish turn start event
                EventBus.Publish(new TurnStartedEvent
                {
                    TurnNumber = _manager.CurrentTurn,
                    IsPlayerTurn = _manager.IsPlayerTurn
                });

                // Transition to appropriate turn state
                if (_manager.IsPlayerTurn)
                {
                    _manager.TransitionToState(BattleState.PlayerTurn);
                }
                else
                {
                    _manager.TransitionToState(BattleState.OpponentTurn);
                }
            }
        }

        /// <summary>
        /// Player Turn State - Player is choosing and playing cards.
        /// </summary>
        private class PlayerTurnState : State
        {
            private BattleManager _manager;

            public PlayerTurnState(BattleManager manager)
            {
                _manager = manager;
            }

            public override void OnEnter()
            {
                GameLogger.LogInfo<BattleManager>("Player's turn started");
                // Player controls are handled through UI via events
            }

            public override void OnUpdate()
            {
                // Player controls are handled through UI
                // This state waits for player to publish EndTurnRequestedEvent
            }

            public override void OnExit()
            {
                GameLogger.LogInfo<BattleManager>("Player's turn ended");
            }
        }

        /// <summary>
        /// Opponent Turn State - AI is making decisions.
        /// </summary>
        private class OpponentTurnState : State
        {
            private BattleManager _manager;

            public OpponentTurnState(BattleManager manager)
            {
                _manager = manager;
            }

            public override void OnEnter()
            {
                GameLogger.LogInfo<BattleManager>("Opponent's turn started");
                // TODO: AI decision making
                // TODO: Play opponent cards
            }

            public override void OnUpdate()
            {
                // TODO: Process AI actions
                // For now, immediately end turn
                _manager.TransitionToState(BattleState.TurnEnd);
            }
        }

        /// <summary>
        /// Turn End State - Cleanup turn effects.
        /// </summary>
        private class TurnEndState : State
        {
            private BattleManager _manager;

            public TurnEndState(BattleManager manager)
            {
                _manager = manager;
            }

            public override void OnEnter()
            {
                _manager.EndTurn();
                GameLogger.LogInfo<BattleManager>("Ending turn");

                // Publish turn end event
                EventBus.Publish(new TurnEndedEvent
                {
                    TurnNumber = _manager.CurrentTurn,
                    WasPlayerTurn = _manager.IsPlayerTurn
                });

                // Check victory conditions
                if (_manager.CheckVictoryConditions())
                {
                    _manager.TransitionToState(BattleState.BattleEnd);
                }
                else
                {
                    _manager.TransitionToState(BattleState.TurnStart);
                }
            }
        }

        /// <summary>
        /// Battle End State - Battle is over.
        /// </summary>
        private class BattleEndState : State
        {
            private BattleManager _manager;

            public BattleEndState(BattleManager manager)
            {
                _manager = manager;
            }

            public override void OnEnter()
            {
                BattleResult result = _manager.GetBattleResult();
                GameLogger.LogInfo<BattleManager>($"Battle ended - {(result.isVictory ? "VICTORY" : "DEFEAT")}");

                // Publish battle ended event
                EventBus.Publish(new BattleEndedEvent { Result = result });
            }
        }

        #endregion
    }

    /// <summary>
    /// Setup data for initializing a battle.
    /// </summary>
    [Serializable]
    public class BattleSetup
    {
        public OriginType playerOrigin;
        public OriginType opponentOrigin;

        public OriginStats originStats;

        public List<CardData> playerDeck = new List<CardData>();
        public List<CardData> opponentDeck = new List<CardData>();

        /// <summary>
        /// Gets the player's battle stats based on their origin.
        /// </summary>
        public OriginBattleStats GetPlayerStats()
        {
            return originStats != null
                ? originStats.GetStatsForOrigin(playerOrigin)
                : new OriginBattleStats { maxResolve = 20, maxActionPoints = 3 };
        }

        /// <summary>
        /// Gets the opponent's battle stats based on their origin.
        /// </summary>
        public OriginBattleStats GetOpponentStats()
        {
            return originStats != null
                ? originStats.GetStatsForOrigin(opponentOrigin)
                : new OriginBattleStats { maxResolve = 20, maxActionPoints = 3 };
        }
    }

    /// <summary>
    /// Result data from a completed battle.
    /// </summary>
    [Serializable]
    public class BattleResult
    {
        public bool isVictory;
        public int turnsToWin;
        public int finalPlayerResolve;
        public int finalPlayerComposure;
        public int finalPlayerHostility;

        // TODO: Add rewards when reward system exists
    }
}
