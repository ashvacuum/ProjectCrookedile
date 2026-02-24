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
        private OriginType  _playerOrigin;

        // Player deck
        private DeckManager _playerDeck;

        // Enemies — all active enemies; index 0 = first enemy in room
        private List<EnemyController> _enemies = new List<EnemyController>();
        private int _focusedEnemyIndex = 0;

        // Effect Resolver
        private EffectResolver _effectResolver;

        // Turn tracking
        private int _currentTurn = 0;
        private bool _isPlayerTurn = true;

        // Battle result
        private BattleResult _battleResult;

        #region Properties

        public BattleState CurrentState  => _stateMachine?.CurrentStateType ?? BattleState.Initialize;
        public BattleStats PlayerStats   => _playerStats;
        public DeckManager PlayerDeck    => _playerDeck;
        public OriginType  PlayerOrigin  => _playerOrigin;
        public int         CurrentTurn   => _currentTurn;
        public bool        IsPlayerTurn  => _isPlayerTurn;

        // Multi-enemy
        public IReadOnlyList<EnemyController> Enemies    => _enemies;
        public EnemyController FocusedEnemy              => _enemies.Count > 0 ? _enemies[_focusedEnemyIndex] : null;
        public BattleStats     OpponentStats             => FocusedEnemy?.Stats;
        public EnemyController EnemyController           => FocusedEnemy;   // backward compat
        public int             FocusedEnemyIndex         => _focusedEnemyIndex;

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
            GameLogger.LogInfo<BattleManager>($"Starting battle: {setup.playerOrigin} vs {setup.enemies.Count} enemies");

            // Player stats from origin
            OriginBattleStats playerOriginStats = setup.GetPlayerStats();
            _playerStats  = new BattleStats(playerOriginStats.maxResolve, playerOriginStats.maxActionPoints);
            _playerOrigin = setup.playerOrigin;

            // Build enemy controllers — each owns its own BattleStats + StatusEffectManager
            _enemies.Clear();
            foreach (var enemyData in setup.enemies)
            {
                if (enemyData != null)
                    _enemies.Add(new EnemyController(enemyData));
            }
            _focusedEnemyIndex = 0;

            // Player deck
            _playerDeck = new DeckManager(setup.playerDeck, "Player", 10);

            // Effect resolver — initially targets the first enemy; receives full enemy list for multi-target effects
            _effectResolver = new EffectResolver(_playerStats, FocusedEnemy.Stats, _playerDeck, _enemies);

            // Reset counters
            _currentTurn = 0;
            _isPlayerTurn = true;

            EventBus.Publish(new BattleStartedEvent { Setup = setup });
            _stateMachine.ChangeState(BattleState.Initialize);
        }

        /// <summary>
        /// Sets the player's focused target to the enemy at <paramref name="index"/>.
        /// All subsequent card damage and hostility effects will apply to that enemy.
        /// Silently ignored if the index is out of range or the enemy is already defeated.
        /// </summary>
        public void SetFocusedEnemy(int index)
        {
            if (index < 0 || index >= _enemies.Count || _enemies[index].IsDefeated) return;
            _focusedEnemyIndex = index;
            _effectResolver.SetFocusedOpponent(FocusedEnemy.Stats, FocusedEnemy.StatusEffects);
            GameLogger.LogInfo<BattleManager>($"Focused enemy: [{index}] {FocusedEnemy.EnemyData.EnemyName}");
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
            ApplyPolicyHostilityShifts(card);
            CheckAndAdvanceFocusAfterCardPlay();

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

        /// <summary>
        /// If the played card is a Policy card, shifts EVERY living enemy's hostility
        /// based on how their DemographicValues aligns with the card's PolicyLean.
        ///
        /// Alignment table (PolicyLean × DemographicValues):
        ///   Left   + Progressive  → −1  (agreement — they like it)
        ///   Left   + Moderate     →  0
        ///   Left   + Traditional  → +1  (disagreement — they dislike it)
        ///   Center + Progressive  →  0
        ///   Center + Moderate     → −1  (agreement)
        ///   Center + Traditional  →  0
        ///   Right  + Progressive  → +1  (disagreement)
        ///   Right  + Moderate     →  0
        ///   Right  + Traditional  → −1  (agreement)
        /// </summary>
        private void ApplyPolicyHostilityShifts(CardData card)
        {
            if (card.CardType != CardType.Policy) return;

            for (int i = 0; i < _enemies.Count; i++)
            {
                var enemy = _enemies[i];
                if (enemy.IsDefeated) continue;

                int shift = GetPolicyHostilityShift(card.PolicyLean, enemy.EnemyData.DemographicValues);
                if (shift == 0) continue;

                int old = enemy.Stats.CurrentHostility;
                if (shift > 0) enemy.Stats.GainHostility(shift);
                else           enemy.Stats.ReduceHostility(-shift);

                if (enemy.Stats.CurrentHostility != old)
                    EventBus.Publish(new EnemyHostilityChangedEvent
                    {
                        OldValue   = old,
                        NewValue   = enemy.Stats.CurrentHostility,
                        EnemyIndex = i
                    });
            }
        }

        private static int GetPolicyHostilityShift(PolicyLean lean, DemographicValues values)
        {
            return (lean, values) switch
            {
                (PolicyLean.Left,   DemographicValues.Progressive)  => -1,
                (PolicyLean.Left,   DemographicValues.Traditional)  => +1,
                (PolicyLean.Right,  DemographicValues.Traditional)  => -1,
                (PolicyLean.Right,  DemographicValues.Progressive)  => +1,
                (PolicyLean.Center, DemographicValues.Moderate)     => -1,
                _                                                    =>  0
            };
        }

        /// <summary>
        /// Called after each card resolves. If the focused enemy just died, publishes
        /// EnemyDefeatedEvent and auto-advances focus to the next living enemy.
        /// </summary>
        private void CheckAndAdvanceFocusAfterCardPlay()
        {
            if (FocusedEnemy == null || !FocusedEnemy.IsDefeated) return;

            EventBus.Publish(new EnemyDefeatedEvent
            {
                EnemyIndex = _focusedEnemyIndex,
                EnemyName  = FocusedEnemy.EnemyData.EnemyName
            });

            GameLogger.LogInfo<BattleManager>($"Enemy [{_focusedEnemyIndex}] {FocusedEnemy.EnemyData.EnemyName} defeated — seeking next target");

            for (int i = 0; i < _enemies.Count; i++)
            {
                if (!_enemies[i].IsDefeated)
                {
                    SetFocusedEnemy(i);
                    return;
                }
            }
            // All defeated — TurnEnd victory check will catch it
        }

        #endregion

        #region Victory Conditions

        /// <summary>Checks if the battle has ended and caches the result.</summary>
        public bool CheckVictoryConditions()
        {
            bool playerDefeated    = _playerStats.IsDefeated;
            bool allEnemiesDefeated = _enemies.Count > 0 && _enemies.TrueForAll(e => e.IsDefeated);

            if (playerDefeated || allEnemiesDefeated)
            {
                _battleResult = new BattleResult
                {
                    isVictory             = allEnemiesDefeated,
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

        /// <summary>Runs start-of-turn effects for the current combatant(s).</summary>
        public void StartTurn()
        {
            if (_isPlayerTurn)
            {
                _playerStats.StartTurn();
                _effectResolver.PlayerStatusEffects.OnTurnStart(_playerStats);
            }
            else
            {
                foreach (var enemy in _enemies)
                {
                    if (enemy.IsDefeated) continue;
                    enemy.Stats.StartTurn();
                    enemy.StatusEffects.OnTurnStart(enemy.Stats);
                }
            }
        }

        /// <summary>Runs end-of-turn effects for the current combatant(s).</summary>
        public void EndTurn()
        {
            if (_isPlayerTurn)
            {
                _playerStats.EndTurn();
                _effectResolver.PlayerStatusEffects.OnTurnEnd(_playerStats);
                _playerDeck.EndTurn();
            }
            else
            {
                foreach (var enemy in _enemies)
                {
                    if (enemy.IsDefeated) continue;
                    enemy.Stats.EndTurn();
                    enemy.StatusEffects.OnTurnEnd(enemy.Stats);
                }
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

                    // Every living enemy declares their intent (Slay the Spire timing)
                    for (int i = 0; i < _manager._enemies.Count; i++)
                    {
                        var enemy = _manager._enemies[i];
                        if (enemy.IsDefeated) continue;
                        EnemyMoveData intent = enemy.SelectNextMove();
                        if (intent != null)
                        {
                            EventBus.Publish(new EnemyIntentDeclaredEvent { Move = intent, EnemyIndex = i });
                            GameLogger.LogInfo<BattleManager>($"Enemy [{i}] {enemy.EnemyData.EnemyName} declares: {intent.MoveName}");
                        }
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
                GameLogger.LogInfo<BattleManager>("Enemy turn started — all living enemies act");

                for (int i = 0; i < _manager._enemies.Count; i++)
                {
                    var enemy = _manager._enemies[i];
                    if (enemy.IsDefeated || enemy.CurrentIntent == null) continue;

                    GameLogger.LogInfo<BattleManager>($"Enemy [{i}] {enemy.EnemyData.EnemyName} executes: {enemy.CurrentIntent.MoveName}");

                    // Temporarily point EffectResolver at this enemy as the caster
                    _manager._effectResolver.SetFocusedOpponent(enemy.Stats, enemy.StatusEffects);
                    _manager._effectResolver.ResolveEnemyMoveEffects(enemy.CurrentIntent);
                }

                // Restore resolver to the player's current focused target
                if (_manager.FocusedEnemy != null)
                    _manager._effectResolver.SetFocusedOpponent(
                        _manager.FocusedEnemy.Stats, _manager.FocusedEnemy.StatusEffects);

                // Enemy turn is instant — transition immediately after all effects resolve
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
    /// Player brings a card deck and origin; opponents are one or more scripted enemies.
    /// </summary>
    [Serializable]
    public class BattleSetup
    {
        public OriginType     playerOrigin;
        public OriginStats    originStats;
        public List<CardData> playerDeck = new List<CardData>();

        /// <summary>All enemies present in this room (1–5). Order = display order.</summary>
        public List<EnemyData> enemies = new List<EnemyData>();

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
