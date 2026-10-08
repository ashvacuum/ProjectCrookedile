using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Data.VFX;
using Crookedile.Utilities;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
        [SerializeField]
        private int _startingHandSize = 5;

        [SerializeField]
        private int _cardsPerTurn = 1;

        [Tooltip("Seconds to wait after the opponent turn starts before enemies attack.")]
        [SerializeField]
        private float _opponentTurnDelay = 1.0f;

        [Tooltip("Seconds to wait between each individual enemy's attack.")]
        [SerializeField]
        private float _perEnemyAttackDelay = 0.5f;

        [Tooltip(
            "Opinion lost at the end of each player turn while the room is an echo chamber "
                + "(every enemy receptive). 0 disables decay."
        )]
        [SerializeField]
        private int _echoChamberDecayPerTurn = 5;

        [Tooltip(
            "Support gained at the start of the player turn per receptive enemy (reading a friendly "
                + "room — the mirror to hostile enemies granting bonus draws). 0 disables."
        )]
        [SerializeField]
        private int _supportPerReceptiveEnemy = 1;

        [Tooltip(
            "Support gained the moment an enemy turns receptive — the mirror to an enemy turning "
                + "hostile granting a draw. 0 disables."
        )]
        [SerializeField]
        private int _supportOnBecomingReceptive = 2;

        [Header("Turncoat (receptive → hostile betrayal)")]
        [Tooltip(
            "Turncoat stacks applied to a betrayer. Each stack adds bonus Opinion shift and fades 1/turn."
        )]
        [SerializeField]
        private int _turncoatStacks = 2;

        [Tooltip(
            "Opinion lost when an enemy turns coat (the crowd notices the betrayal). 0 disables."
        )]
        [SerializeField]
        private int _turncoatOpinionHit = 3;

        [Tooltip(
            "Hostility added to each immediate neighbour when an enemy turns coat. 0 disables contagion."
        )]
        [SerializeField]
        private int _turncoatAdjacentNudge = 1;

        [Header("Origin Passives")]
        [Tooltip("Assign all three OriginPassive assets here (FaithLeader, NepoBaby, Actor).")]
        [SerializeField]
        private OriginPassive[] _originPassives;

        // State Machine
        private StateMachine<BattleState> _stateMachine;

        // Combatants
        private BattleStats _playerStats;
        private OriginType _playerOrigin;

        // Player deck
        private DeckManager _playerDeck;

        // Enemies — all active enemies; index 0 = first enemy in room
        private List<EnemyController> _enemies = new List<EnemyController>();
        private int _focusedEnemyIndex = 0;

        // Effect Resolver
        private EffectResolver _effectResolver;

        // Card-play pipeline — costs, VFX handshake, Confused/Momentum/Echo side-effects.
        private CardPlayController _cards;

        // Faith Leader conversion engine — pacify-threshold checks and the convert burst.
        private PacifyConversionEngine _pacify;

        // Passive ability resolver — one per battle, origin-specific
        private PassiveResolver _passiveResolver;

        // Turn tracking
        private int _currentTurn = 0;
        private int _playerTurnNumber = 0; // counts only the player's turns (for Actor Improvise)
        private bool _isPlayerTurn = true;

        // Opinion Meter + session shields — single owner of the shared battle resources.
        private OpinionLedger _opinion;
        private int _maxTurns; // 0 = no limit
        private int _playerTurnsElapsed;

        // Crowd dynamics — hostility shifts from cards, the Echo Chamber rule, the Turncoat cascade.
        private CrowdReactions _crowd;

        // Battle result
        private BattleResult _battleResult;

        #region Properties

        public BattleState CurrentState =>
            _stateMachine?.CurrentStateType ?? BattleState.Initialize;
        public BattleStats PlayerStats => _playerStats;
        public DeckManager PlayerDeck => _playerDeck;
        public OriginType PlayerOrigin => _playerOrigin;

        /// <summary>
        /// The player class's modifiers on what its cards produce (Sway, Support). Identity
        /// for a class without any.
        /// </summary>
        public ClassModifiers PlayerClassModifiers =>
            _playerOrigin == OriginType.NepoBaby
                ? NepoBabyConfig.Current.Modifiers
                : ClassModifiers.Identity;

        /// <summary>The player's card-play pipeline: replays, extra plays and turn-scoped price breaks.</summary>
        public CardPlayController CardPlay => _cards;
        public int CurrentTurn => _currentTurn;
        public bool IsPlayerTurn => _isPlayerTurn;

        // Multi-enemy
        public IReadOnlyList<EnemyController> Enemies => _enemies;

        public BossController Boss { get; private set; }
        internal BossBrain BossBrain { get; private set; }

        /// <summary>Enemies still in the fight. (Currently always all of them — there is no HP/death.)</summary>
        private IEnumerable<EnemyController> LivingEnemies => _enemies.Where(e => !e.IsDefeated);

        public EnemyController FocusedEnemy =>
            _enemies.Count > 0 ? _enemies[_focusedEnemyIndex] : null;
        public BattleStats OpponentStats => FocusedEnemy?.Stats;
        public EnemyController EnemyController => FocusedEnemy; // backward compat
        public int FocusedEnemyIndex => _focusedEnemyIndex;

        /// <summary>The player's status effect manager. Used by HandPanel to compute effective card costs.</summary>
        public StatusEffectManager PlayerStatusEffects => _effectResolver?.PlayerStatusEffects;

        /// <summary>Maps hand index to randomized effect amounts while the player has Confused. Empty when not confused.</summary>
        public IReadOnlyDictionary<int, int[]> ConfusedOverrides =>
            _cards?.ConfusedOverrides ?? _emptyConfusedOverrides;

        private static readonly Dictionary<int, int[]> _emptyConfusedOverrides =
            new Dictionary<int, int[]>();

        // Internal collaborator accessors — for the owned battle subsystems (CardPlayController
        // et al.), not for the UI layer. UI goes through the public facade methods.
        internal EffectResolver Resolver => _effectResolver;
        internal CrowdReactions Crowd => _crowd;
        internal PassiveResolver Passives => _passiveResolver;

        // Opinion Meter + session shields — owned by OpinionLedger; these expose it read-only.
        /// <summary>The shared opinion/Support/Denial ledger. Effects call this directly for opinion pressure.</summary>
        public OpinionLedger Opinion => _opinion;
        public int CurrentOpinion => _opinion?.CurrentOpinion ?? 0;
        public int MaxOpinion => _opinion?.MaxOpinion ?? 0;
        public int MaxTurns => _maxTurns;
        public int PlayerTurnsElapsed => _playerTurnsElapsed;
        public float OpinionPercentage => _opinion?.OpinionPercentage ?? 0f;
        public int CurrentSupport => _opinion?.CurrentSupport ?? 0;
        public int CurrentDenial => _opinion?.CurrentDenial ?? 0;

        /// <summary>True while every enemy is receptive — gains are halved and the meter decays.</summary>
        public bool IsEchoChamber => _crowd != null && _crowd.IsEchoChamber();

        #endregion

        #region Initialization

        private void Awake()
        {
            InitializeStateMachine();
            EventBus.Subscribe<HostilityChangedEvent>(OnStarstruckHostilityChanged);
            // Player input (end turn, play card) arrives as direct method calls
            // (RequestEndTurn / RequestPlayCard); VFX sequencing is a direct callback
            // handshake (ICardPlayFeedback). The bus is notification-only here.
            // EnemyTurncoatEvent is owned by CrowdReactions (subscribed in its constructor).
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<HostilityChangedEvent>(OnStarstruckHostilityChanged);
            BossBrain?.StopPlanning();
            _passiveResolver?.Dispose();
            _crowd?.Dispose();
        }

        /// <summary>
        /// UI-layer VFX implementation for card plays. Registered by
        /// <c>BattleFeedbackController.OnEnable</c>. Null = effects resolve instantly
        /// with no animation (headless / tests / unwired scene).
        /// </summary>
        public ICardPlayFeedback CardPlayFeedback { get; set; }

        private void InitializeStateMachine()
        {
            _stateMachine = new StateMachine<BattleState>();
            _stateMachine.StateEntered += (previous, current) =>
                EventBus.Publish(
                    new BattleStateChangedEvent { Previous = previous, Current = current }
                );

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
        /// Starts a new battle. The player brings a card deck; the enemy is defined by EnemyData.
        /// </summary>
        public void StartBattle(BattleSetup setup)
        {
            if (BossBrain != null)
            {
                BossBrain.StopPlanning();
                BossBrain.gameObject.SetActive(false);
                Destroy(BossBrain.gameObject);
            }
            BossBrain = null;
            Boss = setup.boss != null ? new BossController(setup.boss) : null;
            GameLogger.LogInfo<BattleManager>(
                $"Starting battle: {setup.playerOrigin} vs {setup.enemies.Count} enemies"
            );

            _playerStats = new BattleStats(setup.GetPlayerMaxActionPoints(), isPlayer: true);
            _playerOrigin = setup.playerOrigin;

            // Build enemy controllers — each owns its own BattleStats + StatusEffectManager
            _enemies.Clear();
            foreach (var enemyData in setup.enemies)
            {
                if (enemyData != null)
                    _enemies.Add(
                        new EnemyController(
                            enemyData,
                            () => _playerTurnNumber,
                            () => OpinionPercentage
                        )
                    );
            }
            // Register each enemy's roster index so its BattleStats can stamp hostility events.
            for (int i = 0; i < _enemies.Count; i++)
                _enemies[i].Stats.SetOwnerEnemyIndex(i);
            _focusedEnemyIndex = 0;

            // Campaign choices can send the room in angrier (NextBattleHostilityOutcome).
            int bankedHostility = RunState.Current?.ConsumeNextBattleHostility() ?? 0;
            if (bankedHostility != 0)
            {
                foreach (var enemy in _enemies)
                    enemy.Stats.SetHostility(enemy.Stats.CurrentHostility + bankedHostility);
                GameLogger.LogInfo<BattleManager>(
                    $"Campaign choice: every enemy starts {bankedHostility:+#;-#} hostility."
                );
            }

            if (_enemies.Count == 0)
            {
                GameLogger.LogError<BattleManager>(
                    "StartBattle: setup contains no valid enemies — battle cannot proceed."
                );
                return;
            }

            // Player deck
            _playerDeck = new DeckManager(setup.playerDeck, "Player", 10);

            // Effect resolver — initially targets the first enemy; receives full enemy list for multi-target effects
            _effectResolver = new EffectResolver(
                _playerStats,
                FocusedEnemy.Stats,
                _playerDeck,
                _enemies,
                this
            );

            // Card-play pipeline — owns cost payment, VFX handshake, Confused/Momentum/Echo.
            _cards = new CardPlayController(this);

            // Passive resolver — find the asset matching this run's origin (null-safe: no passive if asset missing)
            var passive =
                _originPassives != null
                    ? System.Array.Find(
                        _originPassives,
                        p => p != null && p.Origin == _playerOrigin
                    )
                    : null;
            // Ally passives — run-level, pulled from the active run (null when battles are
            // started standalone, e.g. BattleTestStarter without a run).
            List<BattlePassive> allyPassives = null;
            var runAllies = RunState.Current?.Allies;
            if (runAllies != null && runAllies.Count > 0)
            {
                allyPassives = new List<BattlePassive>();
                foreach (var ally in runAllies)
                {
                    if (ally?.Passives == null)
                        continue;
                    foreach (var bp in ally.Passives)
                        if (bp != null)
                            allyPassives.Add(bp);
                }
                GameLogger.LogInfo<BattleManager>(
                    $"Registering {allyPassives.Count} ally passive(s) from {runAllies.Count} allies."
                );
            }

            // Dispose the previous run's resolver first, for the same reason _crowd is disposed
            // below: StartBattle is public and re-entrant. Called twice without a scene reload
            // in between, every fight would leave its passive subscriptions on the bus and
            // effects would fire once per battle ever started.
            _passiveResolver?.Dispose();
            _passiveResolver = new PassiveResolver(
                passive,
                _playerStats,
                _effectResolver,
                _enemies,
                _effectResolver.PlayerStatusEffects,
                () => OpinionPercentage,
                this,
                allyPassives
            );
            // Event subscriptions are managed internally by PassiveResolver via EventBus

            if (_instantResolution)
                _effectResolver.EffectStepDelay = 0f;

            // Reset counters
            _currentTurn = 0;
            _playerTurnNumber = 0;
            _playerTurnsElapsed = 0;
            // Start as false so the first NextTurn() call (in TurnStartState) flips it to
            // true, making turn 1 the player's turn. Starting as true would give the opponent
            // the first move.
            _isPlayerTurn = false;

            // Crowd dynamics — created before the ledger because the ledger asks it whether the
            // room is an echo chamber. (Dispose any prior instance if a battle is restarted.)
            _crowd?.Dispose();
            _crowd = new CrowdReactions(
                _enemies,
                _echoChamberDecayPerTurn,
                _turncoatStacks,
                _turncoatOpinionHit,
                _turncoatAdjacentNudge,
                _supportOnBecomingReceptive
            );

            // Opinion Meter + session shields
            int maxOpinion = setup.maxOpinion ?? 100;
            _maxTurns = setup.maxTurns ?? 0;
            _opinion = new OpinionLedger(
                maxOpinion,
                setup.startingOpinion ?? maxOpinion / 2,
                onOpinionMaxed: () => CheckAndEndBattleIfOver(),
                isEchoChamber: _crowd.IsEchoChamber,
                onOpinionZeroed: () => CheckAndEndBattleIfOver()
            );
            _crowd.AttachLedger(_opinion);
            if (Boss != null)
            {
                var brainObject = new GameObject("Boss planning");
                brainObject.SetActive(false);
                brainObject.transform.SetParent(transform, false);
                BossBrain = brainObject.AddComponent<BossBrain>();
                BossBrain.Bind(this, Boss);
                brainObject.SetActive(true);
            }
            _opinion.OnPlayerLeak = _ =>
                PlayerStatusEffects?.RemoveStacks<GlamourStatus>(
                    CelebrityRules.ScrutinyStripPerHit
                );

            // Faith Leader conversion engine — needs the ledger for the convert burst.
            _pacify = new PacifyConversionEngine(_opinion, _playerStats);

            EventBus.Publish(new BattleStartedEvent { Setup = setup });
            TransitionToState(BattleState.Initialize);
        }

        /// <summary>
        /// Sets the player's focused target to the enemy at <paramref name="index"/>.
        /// All subsequent card damage and hostility effects will apply to that enemy.
        /// Silently ignored if the index is out of range or the enemy is already defeated.
        /// </summary>
        public void SetFocusedEnemy(int index)
        {
            if (index < 0 || index >= _enemies.Count)
            {
                GameLogger.LogWarning<BattleManager>(
                    $"SetFocusedEnemy: index {index} out of range (count: {_enemies.Count})"
                );
                return;
            }
            GameLogger.LogInfo<BattleManager>(
                $"SetFocusedEnemy: [{index}] of {_enemies.Count}, defeated={_enemies[index].IsDefeated}"
            );
            if (_enemies[index].IsDefeated)
                return;
            _focusedEnemyIndex = index;
            _effectResolver.SetFocusedOpponent(
                FocusedEnemy.Stats,
                FocusedEnemy.StatusEffects,
                index,
                FocusedEnemy.EnemyData.EnemyName
            );
            GameLogger.LogInfo<BattleManager>(
                $"Focused enemy: [{index}] {FocusedEnemy.EnemyData.EnemyName}"
            );
        }

        /// <summary>Transitions to the next state in the battle flow and notifies all listeners.</summary>
        public void TransitionToState(BattleState newState)
        {
            // The BattleStateChangedEvent is published by the state machine's StateEntered hook
            // (wired in setup) the moment the state becomes current — see ChangeState. Publishing
            // here instead would fire AFTER any nested transition inside the new state's OnEnter,
            // delivering a transient state (e.g. TurnStart) last and clobbering PlayerTurn.
            _stateMachine.ChangeState(newState);
        }

        /// <summary>
        /// Adds up to <paramref name="count"/> copies of <paramref name="data"/> to the enemy row
        /// (capped so total enemies never exceed 5). Used by enemy SummonMinion moves and
        /// SummonBodyEffect. Each new body immediately declares intent for next turn.
        /// </summary>
        /// <param name="initialHostility">
        /// Overrides the body's starting hostility when set — negative spawns it receptive, positive
        /// hostile. Null uses the EnemyData default.
        /// </param>
        public void SummonMinions(EnemyData data, int count, int? initialHostility = null)
        {
            if (data == null || count <= 0)
                return;

            int available = Mathf.Max(0, 5 - _enemies.Count);
            if (available == 0)
            {
                GameLogger.LogWarning<BattleManager>(
                    "SummonMinion: enemy cap (5) already reached."
                );
                return;
            }

            count = Mathf.Min(count, available);

            for (int i = 0; i < count; i++)
            {
                var controller = new EnemyController(
                    data,
                    () => _playerTurnNumber,
                    () => OpinionPercentage
                );
                int newIndex = _enemies.Count;
                _enemies.Add(controller);
                controller.Stats.SetOwnerEnemyIndex(newIndex);

                // Player summons override the body's mood (receptive ally / hostile Plant).
                if (initialHostility.HasValue)
                    controller.Stats.SetHostility(initialHostility.Value);

                EventBus.Publish(
                    new EnemySummonedEvent { EnemyData = data, EnemyIndex = newIndex }
                );

                // Pre-declare intent so the player sees the threat on the next player turn
                var intent = controller.SelectNextMove(_enemies);
                if (intent != null)
                    EventBus.Publish(
                        new EnemyIntentDeclaredEvent { Move = intent, EnemyIndex = newIndex }
                    );

                GameLogger.LogInfo<BattleManager>($"Summoned enemy [{newIndex}]: {data.EnemyName}");
            }
        }

        #endregion

        #region Player Input (direct calls from BattleUI)

        /// <summary>
        /// Ends the player's turn. Called directly by the UI's End Turn button.
        /// Ignored while a card play is still resolving or outside the player turn.
        /// </summary>
        /// <summary>
        /// Headless setup for tools that play battles without a scene (the playtest bot): every
        /// pacing delay goes to zero and the origin passives normally wired in the scene are
        /// supplied directly. Call before <see cref="StartBattle"/>.
        /// </summary>
        public void ConfigureForSimulation(OriginPassive[] originPassives)
        {
            _originPassives = originPassives;
            _opponentTurnDelay = 0f;
            _perEnemyAttackDelay = 0f;
            _instantResolution = true;
        }

        private bool _instantResolution;

        /// <summary>True while a played card is still resolving; input is ignored until it clears.</summary>
        public bool IsCardResolving => _cards != null && _cards.IsResolving;

        /// <summary>
        /// Whether the player could play <paramref name="card"/> right now: costs and
        /// unplayability from the rules, plus Silenced blocking Rhetoric.
        /// </summary>
        // ponytail: Silenced is enforced here and in HandPanel, not in CardPlayController, so
        // RequestPlayCard still accepts a silenced Rhetoric card. Move the check into
        // CardPlayController.CanPlayCard if anything but the UI and the bot ever plays cards.
        public bool CanPlayCard(CardData card)
        {
            if (card == null || _cards == null || _playerStats == null)
                return false;
            if (
                card.CardType == CardType.Rhetoric
                && (PlayerStatusEffects?.HasStatus<SilencedStatus>() ?? false)
            )
                return false;
            return _cards.CanPlayCard(card, _playerStats);
        }

        public void RequestEndTurn()
        {
            if (_cards == null || _cards.IsResolving)
                return;
            if (!_isPlayerTurn || CurrentState != BattleState.PlayerTurn)
            {
                GameLogger.LogWarning<BattleManager>("Cannot end player turn — not player's turn!");
                return;
            }
            TransitionToState(BattleState.TurnEnd);
        }

        /// <summary>
        /// Plays a card from the player's hand. Called directly by the UI when a card is
        /// played. Validates turn state and rejects input while a previous play resolves.
        /// </summary>
        public void RequestPlayCard(CardData card, int handIndex)
        {
            GameLogger.LogInfo<BattleManager>(
                $"RequestPlayCard: '{card?.CardName}'  resolving={_cards?.IsResolving}  IsPlayerTurn={_isPlayerTurn}  State={CurrentState}"
            );
            if (_cards == null || _cards.IsResolving)
            {
                GameLogger.LogWarning<BattleManager>(
                    $"Card play blocked: no active battle or VFX still in flight"
                );
                return;
            }
            if (!_isPlayerTurn || CurrentState != BattleState.PlayerTurn)
            {
                GameLogger.LogWarning<BattleManager>(
                    $"Card play blocked: IsPlayerTurn={_isPlayerTurn}  State={CurrentState}"
                );
                return;
            }
            _cards.PlayCard(card, handIndex);
        }

        #endregion

        #region Opinion Meter / Support / Denial (delegates to OpinionLedger)

        /// <summary>Raises the Opinion Meter directly (bypassing Denial). Used by heal/rally effects.</summary>
        public void RaiseOpinion(int amount) => _opinion?.RaiseDirect(amount);

        /// <summary>Grants session Support (absorbs future opinion drops).</summary>
        public void GainSupport(int amount) => _opinion?.GainSupport(amount);

        /// <summary>Grants session Denial (absorbs future opinion rises).</summary>
        public void GainDenial(int amount) => _opinion?.GainDenial(amount);

        /// <summary>Drains session Support (used by "lose Support" effects). Returns amount removed.</summary>
        public int SpendSupport(int amount) => _opinion?.SpendSupport(amount) ?? 0;

        #endregion

        #region Celebrity — Glamour / Debt

        /// <summary>Celebrity's per-battle Debt and Debt-policy state.</summary>
        public CelebrityState Celebrity { get; } = new CelebrityState();

        /// <summary>Nepo Baby's per-battle escalation counters.</summary>
        public NepoBabyState NepoBaby { get; } = new NepoBabyState();

        public int CurrentGlamour => PlayerStatusEffects?.GetStacks<GlamourStatus>() ?? 0;

        public bool CanUseMediaTraining =>
            _cards != null
            && _playerDeck != null
            && Celebrity.MediaTrainingCost > 0
            && Celebrity.MediaTrainingDraw > 0
            && CurrentState == BattleState.PlayerTurn
            && Celebrity.MediaTrainingAvailable
            && !_cards.IsResolving
            && CurrentGlamour >= Celebrity.MediaTrainingCost
            && !_playerDeck.IsHandFull
            && _playerDeck.DeckCount + _playerDeck.DiscardCount > 0;

        /// <summary>The draw window closes on the first card play or when the turn ends.</summary>
        public bool TryUseMediaTraining()
        {
            if (!CanUseMediaTraining)
                return false;

            Celebrity.MediaTrainingAvailable = false;
            PlayerStatusEffects.RemoveStacksNotify<GlamourStatus>(Celebrity.MediaTrainingCost);
            _playerDeck.DrawCards(Celebrity.MediaTrainingDraw);
            return true;
        }

        private void OnStarstruckHostilityChanged(HostilityChangedEvent evt)
        {
            if (
                evt.IsPlayer
                || _effectResolver == null
                || CurrentState == BattleState.BattleEnd
                || evt.EnemyIndex < 0
                || evt.EnemyIndex >= _enemies.Count
            )
                return;

            var enemy = _enemies[evt.EnemyIndex];
            if (
                !enemy.IsDefeated
                && enemy.Stats.IsHostile
                && enemy.StatusEffects.GetStacks<StarstruckStatus>() > 0
            )
            {
                var context = new EffectExecutionContext(
                    _playerStats,
                    enemy.Stats,
                    _playerStats,
                    true,
                    _playerDeck,
                    _enemies,
                    PlayerStatusEffects,
                    enemy.StatusEffects,
                    PlayerStatusEffects,
                    this,
                    attackerName: "Starstruck",
                    attackerEnemyIndex: evt.EnemyIndex
                );
                StarstruckStatus.ResolvePayoff(context);
            }
        }

        /// <summary>After the enemy turn: Glamour pushes Opinion up by its stacks, then decays.</summary>
        public void TickGlamour()
        {
            int glamour = CurrentGlamour;
            if (glamour <= 0)
                return;
            _opinion.RaiseDirect(glamour);
            PlayerStatusEffects.RemoveStacks<GlamourStatus>(CelebrityRules.GlamourDecayPerTick);
        }

        #endregion

        #region Faith Leader — Pacify Conversion (delegates to PacifyConversionEngine)

        /// <summary>Faith Leader pacify conversions made this player turn (Sermon harvest scaling).</summary>
        public int ConversionsThisTurn => _pacify?.ConversionsThisTurn ?? 0;

        /// <summary>The pacify statuses that count toward (and are consumed by) a conversion.</summary>
        public static bool IsPacifyStatus(StatusBehavior behavior) =>
            PacifyConversionEngine.IsPacifyStatus(behavior);

        /// <summary>
        /// Faith Leader conversion check — see <see cref="PacifyConversionEngine.TryConvert"/>.
        /// Kept as a facade because effects reach it via <c>ctx.BattleManager</c>.
        /// </summary>
        public PacifyConversionEngine.Outcome TryPacifyConvert(
            BattleStats enemyStats,
            StatusEffectManager mgr,
            System.Collections.Generic.IReadOnlyList<StatusBehavior> statuses,
            StatusBehavior bufferStatus,
            out int consumed
        )
        {
            consumed = 0;
            if (_pacify == null)
                return PacifyConversionEngine.Outcome.NotReady;
            return _pacify.TryConvert(enemyStats, mgr, statuses, bufferStatus, out consumed);
        }

        #endregion

        #region Card Playing (pipeline owned by CardPlayController)

        /// <summary>
        /// Called after each card resolves. If the focused enemy just died, publishes
        /// EnemyDefeatedEvent and auto-advances focus to the next living enemy.
        /// </summary>
        internal void CheckAndAdvanceFocusAfterCardPlay()
        {
            if (FocusedEnemy == null || !FocusedEnemy.IsDefeated)
                return;

            // Snapshot before publishing — a passive triggered by the defeat event
            // could change _focusedEnemyIndex, so we capture the reference first.
            var defeatedEnemy = FocusedEnemy;

            EventBus.Publish(
                new EnemyDefeatedEvent
                {
                    EnemyIndex = _focusedEnemyIndex,
                    EnemyName = defeatedEnemy.EnemyData.EnemyName,
                }
            );

            // Purge status effects now that all defeat-event passives have had a
            // chance to read them. Prevents stale buffs/debuffs living in memory
            // on a dead enemy and affecting any future queries.
            defeatedEnemy.StatusEffects.ClearAll();

            GameLogger.LogInfo<BattleManager>(
                $"Enemy [{_focusedEnemyIndex}] {defeatedEnemy.EnemyData.EnemyName} defeated — seeking next target"
            );

            for (int i = 0; i < _enemies.Count; i++)
            {
                if (!_enemies[i].IsDefeated)
                {
                    SetFocusedEnemy(i);
                    return;
                }
            }
            // All defeated — CheckAndEndBattleIfOver will catch it after ApplyCardEffects returns
        }

        /// <summary>
        /// Effective AP cost of a card this battle — facade over
        /// <see cref="CardPlayController.GetEffectiveCardCost"/> for the UI layer
        /// (CardButton / HandPanel read it through the manager).
        /// </summary>
        public int GetEffectiveCardCost(CardData card) => _cards?.GetEffectiveCardCost(card) ?? 0;

        /// <summary>
        /// True if the player played at least one card of <paramref name="cardType"/> this turn.
        /// Read by OpponentTurnState to resolve Counter intents.
        /// </summary>
        public bool WasCardTypePlayedThisTurn(CardType cardType) =>
            _cards?.WasTypePlayedThisTurn(cardType) ?? false;

        /// <summary>How many cards of this type were played this turn (payoff scaling).</summary>
        public int CountPlayedThisTurn(CardType cardType) =>
            _cards?.CountPlayedThisTurn(cardType) ?? 0;

        /// <summary>Total cards played this turn, including the one currently resolving.</summary>
        public int CardsPlayedThisTurn => _cards?.CardsPlayedThisTurn ?? 0;

        /// <summary>Total enemy hostility gained this turn, any source (CrowdReactions tally).</summary>
        public int HostilityGainedThisTurn => _crowd?.HostilityGainedThisTurn ?? 0;

        /// <summary>Total enemy hostility lost this turn, any source (CrowdReactions tally).</summary>
        public int HostilityLostThisTurn => _crowd?.HostilityLostThisTurn ?? 0;

        #endregion

        #region Victory Conditions

        /// <summary>Checks if the battle has ended and caches the result.</summary>
        public bool CheckVictoryConditions()
        {
            bool opinionCollapsed = _opinion.CurrentOpinion <= 0;
            bool opinionMaxed = _opinion.CurrentOpinion >= _opinion.MaxOpinion;

            if (!opinionCollapsed && !opinionMaxed)
                return false;

            _battleResult = new BattleResult
            {
                isVictory = opinionMaxed,
                turnsToWin = _currentTurn,
                finalPlayerSupport = _opinion.CurrentSupport,
                finalPlayerHostility = _playerStats.CurrentHostility,
                finalOpinion = _opinion.CurrentOpinion,
                wasJudgmentVictory = false,
            };

            GameLogger.LogInfo(
                "BattleManager",
                $"Battle ended: {(_battleResult.isVictory ? "Victory" : "Defeat")} "
                    + $"(opinion={_opinion.CurrentOpinion}) in {_currentTurn} turns"
            );
            return true;
        }

        /// <summary>
        /// Convenience wrapper: checks victory conditions and, if the battle is over,
        /// immediately transitions to BattleEnd — bypassing TurnEnd cleanup.
        /// Returns true if the battle ended so callers can exit early.
        /// </summary>
        internal bool CheckAndEndBattleIfOver()
        {
            if (!CheckVictoryConditions())
                return false;
            TransitionToState(BattleState.BattleEnd);
            return true;
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
            GameLogger.LogInfo<BattleManager>(
                $"Turn {_currentTurn} — {(_isPlayerTurn ? "Player" : "Enemy")}"
            );
        }

        /// <summary>Runs start-of-turn effects for the current combatant(s).</summary>
        public void StartTurn()
        {
            // The current side's stale Support/Denial decays (Support at player turn start, Denial
            // at enemy turn start) so each protects through one full opposing turn.
            _opinion.DecaySupportAndDenial(_isPlayerTurn);

            if (_isPlayerTurn)
            {
                // Fresh tallies each player turn (conversion harvest + hostility-shift payoffs).
                _pacify?.ResetTurnTally();
                _crowd?.ResetTurnTallies();

                _playerStats.StartTurn();
                _effectResolver.PlayerStatusEffects.OnTurnStart(_playerStats);

                // Debt comes due after energy refreshes, before the player sees the turn.
                Celebrity.ResetTurn();
                Celebrity.Settle(
                    _opinion,
                    _playerStats,
                    (c, n) => _playerDeck.AddCardsToHand(c, n)
                );

                // Ritual grants Support each turn.
                int ritual = _effectResolver.PlayerStatusEffects.GetStacks<RitualStatus>();
                if (ritual > 0)
                    GainSupport(ritual);

                // Receptive crowd grants Support — the mirror to hostile enemies granting bonus
                // draws. A friendly room cushions your standing (but a fully-receptive room still
                // bleeds via the echo chamber, since decay bypasses Support).
                if (_supportPerReceptiveEnemy > 0)
                {
                    int receptive = LivingEnemies.Count(e => e.Stats.IsReceptive);
                    if (receptive > 0)
                        GainSupport(receptive * _supportPerReceptiveEnemy);
                }

                // Remove player-turn-start duration effects from all living enemies (e.g. Stunned).
                foreach (var enemy in LivingEnemies)
                    enemy.StatusEffects.OnPlayerTurnStart();

                // Confused: randomize card effect amounts for this turn (owned by CardPlayController)
                _cards.OnPlayerTurnStart();

                // "Next turn: X" cards (DelayedEffect) fire now.
                ResolveDueDelayedEffects();

                // "Until your next turn" passives have seen the enemy turn through; they end here.
                _passiveResolver?.ClearUntilNextTurnPassives();
            }
            else
            {
                foreach (var enemy in LivingEnemies)
                {
                    enemy.Stats.StartTurn();
                    var starstruck = StarstruckStatus.Find(enemy.StatusEffects);
                    if (starstruck != null)
                        enemy.Stats.GainHostility(starstruck.HostilityPerTurn);
                    enemy.StatusEffects.OnTurnStart(enemy.Stats);

                    // Enemy Ritual grants Denial.
                    int ritual = enemy.StatusEffects.GetStacks<RitualStatus>();
                    if (ritual > 0)
                        GainDenial(ritual);
                }
            }

            // Surface the echo-chamber state at the top of each turn (enemy moves may have
            // changed the room; halving/decay always use the live IsEchoChamber() check).
            _crowd.RefreshEchoChamberState();
        }

        /// <summary>Runs end-of-turn effects for the current combatant(s).</summary>
        public void EndTurn()
        {
            if (_isPlayerTurn)
            {
                Celebrity.MediaTrainingAvailable = false;
                Celebrity.SettlePromises(
                    _opinion,
                    _playerStats,
                    (c, n) => _playerDeck.AddCardsToHand(c, n)
                );
                _playerStats.EndTurn();
                // Apply opinion-affecting statuses (read at current stacks) before OnTurnEnd decrements.
                ApplyTurnEndOpinionStatuses(_effectResolver.PlayerStatusEffects);
                _effectResolver.PlayerStatusEffects.OnTurnEnd(_playerStats);

                // Echo-chamber decay — bleeds sentiment while the whole room is receptive.
                // Checked after the player's full turn, so breaking the chamber this turn avoids it.
                _crowd.ApplyTurnEndDecay();

                // "This turn" passives (GrantTurnPassiveEffect) expire now.
                _passiveResolver?.ClearTemporaryPassives();

                _playerDeck.EndTurn();
            }
            else
            {
                TickGlamour();
                foreach (var enemy in LivingEnemies)
                {
                    enemy.Stats.EndTurn();
                    ApplyTurnEndOpinionStatuses(enemy.StatusEffects);
                    enemy.StatusEffects.OnTurnEnd(enemy.Stats);
                }
            }
        }

        /// <summary>
        /// Applies turn-end statuses that move the Opinion Meter, routed through the ledger directly
        /// (no EventBus round-trip). Mirrors the Ritual pattern in <see cref="StartTurn"/>:
        /// BattleManager owns opinion, so it reads the stacks and applies the change itself.
        /// Called before the manager's OnTurnEnd decrements the stacks.
        /// </summary>
        private void ApplyTurnEndOpinionStatuses(StatusEffectManager mgr)
        {
            int smear = mgr.GetStacks<SmearStatus>();
            if (smear > 0)
                _opinion.ApplyOpinionShift(
                    smear,
                    toPlayer: true,
                    attackerName: "Smear",
                    sourceEnemyIndex: -1,
                    targetEnemyIndex: -1
                );

            int regen = mgr.GetStacks<RegenerationStatus>();
            if (regen > 0)
                _opinion.RaiseDirect(regen);
        }

        #endregion

        private void Update()
        {
            _stateMachine?.Update();
        }

        #region Internal state-machine surface

        // The FSM states live in Battle/States/ as internal classes. They drive the battle
        // through the public API plus this narrow internal surface — keep it minimal; states
        // should not reach arbitrary private fields.

        /// <summary>Opening hand size (InitializeState).</summary>
        internal int StartingHandSize => _startingHandSize;

        /// <summary>Base card draw per player turn, before hostile-crowd bonus draws (TurnStartState).</summary>
        internal int CardsPerTurn => _cardsPerTurn;

        /// <summary>The player's personal turn count (1 on the first player turn). Set by
        /// <see cref="FirePlayerTurnStartPassives"/>; read by TurnStartState to skip the
        /// turn-1 draw so the opening hand isn't doubled.</summary>
        internal int PlayerTurnNumber => _playerTurnNumber;

        /// <summary>Pause before the enemy side acts (OpponentTurnState).</summary>
        internal float OpponentTurnDelay => _opponentTurnDelay;

        /// <summary>Pause between individual enemy actions (OpponentTurnState).</summary>
        internal float PerEnemyAttackDelay => _perEnemyAttackDelay;

        /// <summary>Resets per-battle session state: class state and the card-play pipeline (InitializeState).</summary>
        internal void ResetBattleSessionState()
        {
            Celebrity.ResetBattle();
            _cards.ResetForBattle();
            NepoBaby.Reset();
            _delayedEffects.Clear();
        }

        #region Delayed effects (DelayedEffect wrapper)

        private sealed class DelayedEntry
        {
            public int TurnsRemaining;
            public List<BattleEffect> Effects;
        }

        private readonly List<DelayedEntry> _delayedEffects = new List<DelayedEntry>();

        /// <summary>
        /// Queues effects to resolve at the start of a future player turn (1 = next turn).
        /// Called by <see cref="DelayedEffect"/>.
        /// </summary>
        public void QueueDelayedEffects(List<BattleEffect> effects, int turnsDelay)
        {
            _delayedEffects.Add(
                new DelayedEntry { TurnsRemaining = Mathf.Max(1, turnsDelay), Effects = effects }
            );
            GameLogger.LogInfo<BattleManager>(
                $"Queued {effects.Count} delayed effect(s), due in {turnsDelay} turn(s)"
            );
        }

        /// <summary>
        /// Ticks the delayed-effect queue at player turn start: due entries resolve with a
        /// fresh context (single-target effects hit the currently focused enemy).
        /// </summary>
        private void ResolveDueDelayedEffects()
        {
            for (int i = _delayedEffects.Count - 1; i >= 0; i--)
            {
                var entry = _delayedEffects[i];
                entry.TurnsRemaining--;
                if (entry.TurnsRemaining > 0)
                    continue;

                _delayedEffects.RemoveAt(i);
                var ctx = _effectResolver.CreateContext(isPlayerCard: true);
                foreach (var effect in entry.Effects)
                    effect?.Execute(ctx);
                GameLogger.LogInfo<BattleManager>(
                    $"Resolved {entry.Effects.Count} delayed effect(s)"
                );
            }
        }

        #endregion

        /// <summary>Advances the player-turn counter and fires per-player-turn passives (TurnStartState).</summary>
        internal void FirePlayerTurnStartPassives()
        {
            _playerTurnNumber++;
            _passiveResolver?.FireTurnStart(_playerTurnNumber);
        }

        /// <summary>Bumps the Judgment turn-limit counter (TurnEndState).</summary>
        internal void IncrementPlayerTurnsElapsed() => _playerTurnsElapsed++;

        /// <summary>Caches the battle result (TurnEndState's Judgment path).</summary>
        internal void SetBattleResult(BattleResult result) => _battleResult = result;

        #endregion
    }
}
