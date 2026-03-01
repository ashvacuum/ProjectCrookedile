using System;
using System.Collections;
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
        [SerializeField] private int   _startingHandSize    = 5;
        [SerializeField] private int   _cardsPerTurn        = 1;
        [Tooltip("Seconds to wait after the opponent turn starts before enemies attack.")]
        [SerializeField] private float _opponentTurnDelay      = 1.0f;
        [Tooltip("Seconds to wait between each individual enemy's attack.")]
        [SerializeField] private float _perEnemyAttackDelay    = 0.5f;

        [Header("Origin Passives")]
        [Tooltip("Assign all three OriginPassive assets here (FaithLeader, NepoBaby, Actor).")]
        [SerializeField] private OriginPassive[] _originPassives;

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

        // Confused status — maps hand index → randomized effect amounts (one per effect on the card)
        private readonly Dictionary<int, int[]> _confusedOverrides = new Dictionary<int, int[]>();

        // Passive ability resolver — one per battle, origin-specific
        private PassiveResolver _passiveResolver;

        // Turn tracking
        private int _currentTurn = 0;
        private int _playerTurnNumber = 0;   // counts only the player's turns (for Actor Improvise)
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

        // Phase B — Improvise (Actor passive)
        /// <summary>True when the Actor's Improvise window is open this turn.</summary>
        public bool IsImproviseAvailable => _passiveResolver?.ImproviseAvailable ?? false;

        /// <summary>Phase B: UI calls this after the player selects cards to discard.</summary>
        public bool TryPlayerImprovise(List<CardData> cardsToDiscard)
            => _passiveResolver?.TryImprovise(_playerDeck, cardsToDiscard) ?? false;

        // Multi-enemy
        public IReadOnlyList<EnemyController> Enemies    => _enemies;
        public EnemyController FocusedEnemy              => _enemies.Count > 0 ? _enemies[_focusedEnemyIndex] : null;
        public BattleStats     OpponentStats             => FocusedEnemy?.Stats;
        public EnemyController EnemyController           => FocusedEnemy;   // backward compat
        public int             FocusedEnemyIndex         => _focusedEnemyIndex;

        /// <summary>The player's status effect manager. Used by HandPanel to compute effective card costs.</summary>
        public StatusEffectManager PlayerStatusEffects => _effectResolver?.PlayerStatusEffects;

        /// <summary>Maps hand index to randomized effect amounts while the player has Confused. Empty when not confused.</summary>
        public IReadOnlyDictionary<int, int[]> ConfusedOverrides => _confusedOverrides;

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
            _passiveResolver?.Dispose();
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

            // Passive resolver — find the asset matching this run's origin (null-safe: no passive if asset missing)
            var passive = _originPassives != null
                ? System.Array.Find(_originPassives, p => p != null && p.Origin == _playerOrigin)
                : null;
            _passiveResolver = new PassiveResolver(passive, _playerStats);
            // Event subscriptions are managed internally by PassiveResolver via EventBus

            // Reset counters
            _currentTurn = 0;
            _playerTurnNumber = 0;
            // Start as false so the first NextTurn() call (in TurnStartState) flips it to
            // true, making turn 1 the player's turn. Starting as true would give the opponent
            // the first move.
            _isPlayerTurn = false;

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
            _effectResolver.SetFocusedOpponent(
                FocusedEnemy.Stats, FocusedEnemy.StatusEffects,
                index, FocusedEnemy.EnemyData.EnemyName);
            GameLogger.LogInfo<BattleManager>($"Focused enemy: [{index}] {FocusedEnemy.EnemyData.EnemyName}");
        }

        /// <summary>Transitions to the next state in the battle flow.</summary>
        public void TransitionToState(BattleState newState)
        {
            _stateMachine.ChangeState(newState);
        }

        /// <summary>
        /// Adds up to <paramref name="count"/> copies of <paramref name="data"/> to the enemy list
        /// (capped so total enemies never exceed 5). Called by OpponentTurnState when a SummonMinion
        /// move resolves. Each new enemy immediately declares intent for the next turn.
        /// </summary>
        public void SummonMinions(EnemyData data, int count)
        {
            if (data == null || count <= 0) return;

            int available = Mathf.Max(0, 5 - _enemies.Count);
            if (available == 0)
            {
                GameLogger.LogWarning<BattleManager>("SummonMinion: enemy cap (5) already reached.");
                return;
            }

            count = Mathf.Min(count, available);

            for (int i = 0; i < count; i++)
            {
                var controller = new EnemyController(data);
                int newIndex   = _enemies.Count;
                _enemies.Add(controller);

                EventBus.Publish(new EnemySummonedEvent { EnemyData = data, EnemyIndex = newIndex });

                // Pre-declare intent so the player sees the threat on the next player turn
                var intent = controller.SelectNextMove();
                if (intent != null)
                    EventBus.Publish(new EnemyIntentDeclaredEvent { Move = intent, EnemyIndex = newIndex });

                GameLogger.LogInfo<BattleManager>($"Summoned enemy [{newIndex}]: {data.EnemyName}");
            }
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

            // Capture Confused overrides before the card leaves the hand (indices are stable here)
            _confusedOverrides.TryGetValue(handIndex, out int[] amountOverrides);

            if (!_playerDeck.PlayCardAtIndex(handIndex))
            {
                GameLogger.LogError<BattleManager>("Failed to play card from hand");
                return;
            }

            // Shift Confused override indices — the played card is gone, so subsequent indices move down
            ShiftConfusedOverridesAfterPlay(handIndex);

            EventBus.Publish(new CardPlayedEvent { Card = card, IsPlayer = true });
            _effectResolver.ResolveCardEffects(card, isPlayerCard: true, amountOverrides: amountOverrides);
            // PassiveResolver listens to CardPlayedEvent via EventBus — no direct call needed
            ApplyPolicyHostilityShifts(card);
            CheckAndAdvanceFocusAfterCardPlay();
            TriggerMomentum();

            // Echo — replay the card a second time; consume the stack BEFORE the replay to
            // prevent a second Echo stack (if any) from triggering an infinite chain.
            int echoStacks = _effectResolver.PlayerStatusEffects.GetStacks(StatusEffectType.Echo);
            if (echoStacks > 0)
            {
                _effectResolver.PlayerStatusEffects.RemoveStacks(StatusEffectType.Echo, 1);
                GameLogger.LogInfo<BattleManager>($"Echo triggered — replaying {card.CardName}");
                _effectResolver.ResolveCardEffects(card, isPlayerCard: true, amountOverrides: null);
                CheckAndAdvanceFocusAfterCardPlay();
            }

            GameLogger.LogInfo<BattleManager>($"Player played: {card.CardName}");
        }

        private bool CanPlayCard(CardData card, BattleStats stats)
        {
            foreach (var cost in card.Costs)
            {
                if (cost.CostType == CostType.ActionPoints)
                    if (stats.CurrentActionPoints < GetEffectiveCardCost(card))
                        return false;
            }
            return true;
        }

        private void PayCardCosts(CardData card, BattleStats stats)
        {
            foreach (var cost in card.Costs)
            {
                if (cost.CostType == CostType.ActionPoints)
                {
                    int effective = GetEffectiveCardCost(card);
                    stats.SpendActionPoints(effective);
                    GameLogger.LogInfo<BattleManager>($"Paid {effective} AP for {card.CardName}");
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

        /// <summary>
        /// Single source of truth for the effective AP cost of a card this battle.
        /// Applies (in order): status effect modifiers (Focus, Energized, Entangled),
        /// then per-card battle overrides (ReduceCardCost / MakeCardFree effects).
        /// Result is floored at 0.
        /// </summary>
        public int GetEffectiveCardCost(CardData card)
        {
            if (card?.Costs == null || card.Costs.Count == 0) return 0;
            var cost = card.Costs[0];
            if (cost.CostType != CostType.ActionPoints) return 0;

            StatusEffectManager statusMgr = _effectResolver?.PlayerStatusEffects;
            int baseCost = statusMgr != null
                ? statusMgr.ModifyCardCost(cost.CurrentAmount)
                : cost.CurrentAmount;

            // Per-card battle override (ReduceCardCost / MakeCardFree)
            int reduction = _playerDeck?.GetCardCostReduction(card) ?? 0;
            if (reduction == int.MaxValue) return 0;   // MakeCardFree sentinel
            return Mathf.Max(0, baseCost - reduction);
        }

        /// <summary>
        /// Randomizes the displayed/resolved amounts for each card currently in the player's hand.
        /// Called at turn start while the player has the Confused status. Values are [0, 3] inclusive.
        /// </summary>
        private void ApplyConfusedOverrides()
        {
            _confusedOverrides.Clear();
            var hand = _playerDeck.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                var effects = hand[i].Effects;
                if (effects == null || effects.Count == 0) continue;
                var overrides = new int[effects.Count];
                for (int j = 0; j < effects.Count; j++)
                    overrides[j] = UnityEngine.Random.Range(0, 4);  // [0, 3] inclusive
                _confusedOverrides[i] = overrides;
            }
            GameLogger.LogInfo<BattleManager>($"Confused: randomized amounts for {_confusedOverrides.Count} cards in hand");
        }

        /// <summary>
        /// If the player has Momentum stacks, deals stacks damage to a random living enemy.
        /// Called once per card play (before Echo replay).
        /// </summary>
        private void TriggerMomentum()
        {
            int stacks = _effectResolver?.PlayerStatusEffects?.GetStacks(StatusEffectType.Momentum) ?? 0;
            if (stacks <= 0) return;

            var living = new List<EnemyController>();
            foreach (var e in _enemies)
                if (!e.IsDefeated) living.Add(e);
            if (living.Count == 0) return;

            var target = living[UnityEngine.Random.Range(0, living.Count)];
            target.Stats.DamageResolve(stacks);
            GameLogger.LogInfo<BattleManager>($"Momentum dealt {stacks} damage to {target.EnemyData.EnemyName}");
        }

        /// <summary>
        /// After a card is removed from the hand, all hand indices above the played index shift
        /// down by 1. This keeps _confusedOverrides aligned with the updated hand layout.
        /// </summary>
        private void ShiftConfusedOverridesAfterPlay(int playedIndex)
        {
            if (_confusedOverrides.Count == 0) return;
            var shifted = new Dictionary<int, int[]>(_confusedOverrides.Count);
            foreach (var kvp in _confusedOverrides)
            {
                if (kvp.Key == playedIndex) continue;   // this entry is now gone
                int newKey = kvp.Key > playedIndex ? kvp.Key - 1 : kvp.Key;
                shifted[newKey] = kvp.Value;
            }
            _confusedOverrides.Clear();
            foreach (var kvp in shifted)
                _confusedOverrides[kvp.Key] = kvp.Value;
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
                // Composure decays at the start of each turn before anything else fires.
                // Ritual (OnTurnStart: gain Composure) then grants fresh Composure on top of the cleared slate.
                // A future relic could check a flag here and skip ConsumeAllComposure().
                _playerStats.ConsumeAllComposure();
                _playerStats.StartTurn();
                _effectResolver.PlayerStatusEffects.OnTurnStart(_playerStats);

                // Confused: randomize card effect amounts for this turn
                if (_effectResolver.PlayerStatusEffects.HasEffect(StatusEffectType.Confused))
                    ApplyConfusedOverrides();
                else
                    _confusedOverrides.Clear();
            }
            else
            {
                foreach (var enemy in _enemies)
                {
                    if (enemy.IsDefeated) continue;
                    enemy.Stats.ConsumeAllComposure();
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

                // Fire battle-start passive AFTER the opening hand is dealt
                // (e.g. Faith Leader's Opening Prayer draws 1 extra card on top of the base hand)
                _manager._passiveResolver?.FireBattleStart(_manager._playerDeck);

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
                    // Track the player's personal turn count and fire per-player-turn passives
                    _manager._playerTurnNumber++;
                    _manager._passiveResolver?.FireTurnStart(_manager._playerTurnNumber);

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
        /// Opponent Turn State — waits <c>_opponentTurnDelay</c> seconds, then resolves all
        /// living enemies' declared moves and transitions to TurnEnd.
        /// The delay gives the player a visible pause before damage lands.
        /// </summary>
        private class OpponentTurnState : State
        {
            private readonly BattleManager _manager;
            public OpponentTurnState(BattleManager manager) { _manager = manager; }

            public override void OnEnter()
            {
                _manager.StartCoroutine(ExecuteAfterDelay());
            }

            private IEnumerator ExecuteAfterDelay()
            {
                yield return new WaitForSeconds(_manager._opponentTurnDelay);

                GameLogger.LogInfo<BattleManager>("Enemy turn started — all living enemies act");

                // Capture count before the loop so summoned enemies act next turn, not this one.
                int enemyCount = _manager._enemies.Count;

                for (int i = 0; i < enemyCount; i++)
                {
                    var enemy = _manager._enemies[i];
                    if (enemy.IsDefeated || enemy.CurrentIntent == null) continue;

                    // Signal the UI: this enemy is about to act (shake + highlight intent panel)
                    EventBus.Publish(new EnemyActingEvent { EnemyIndex = i });

                    // Brief pause so the player sees the signal before damage lands
                    yield return new WaitForSeconds(_manager._perEnemyAttackDelay);

                    GameLogger.LogInfo<BattleManager>(
                        $"Enemy [{i}] {enemy.EnemyData.EnemyName} executes: {enemy.CurrentIntent.MoveName}");

                    // Temporarily point EffectResolver at this enemy as the caster
                    _manager._effectResolver.SetFocusedOpponent(
                        enemy.Stats, enemy.StatusEffects, i, enemy.EnemyData.EnemyName);
                    _manager._effectResolver.ResolveEnemyMoveEffects(enemy.CurrentIntent);

                    // Handle SummonMinion moves after normal effects resolve
                    if (enemy.CurrentIntent.MoveType == EnemyMoveType.SummonMinion &&
                        enemy.CurrentIntent.MinionToSummon != null)
                    {
                        _manager.SummonMinions(enemy.CurrentIntent.MinionToSummon,
                                               enemy.CurrentIntent.MinionCount);
                    }
                }

                // Restore resolver to the player's current focused target
                if (_manager.FocusedEnemy != null)
                    _manager._effectResolver.SetFocusedOpponent(
                        _manager.FocusedEnemy.Stats, _manager.FocusedEnemy.StatusEffects,
                        _manager.FocusedEnemyIndex, _manager.FocusedEnemy.EnemyData.EnemyName);

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
