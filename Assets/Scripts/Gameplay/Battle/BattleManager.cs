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

        [Header("Turncoat (receptive → hostile betrayal)")]
        [Tooltip("Turncoat stacks applied to a betrayer. Each stack adds bonus pressure and fades 1/turn.")]
        [SerializeField]
        private int _turncoatStacks = 2;

        [Tooltip("Opinion lost when an enemy turns coat (the crowd notices the betrayal). 0 disables.")]
        [SerializeField]
        private int _turncoatOpinionHit = 3;

        [Tooltip("Hostility added to each immediate neighbour when an enemy turns coat. 0 disables contagion.")]
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

        // Celebrity passive: the first card played each battle is played upgraded. Reset at battle start.
        private bool _firstCardPlayedThisBattle;

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

        // VFX — true while a card's VFX animation is in flight; blocks card plays and End Turn
        private bool _vfxInFlight;

        #region Properties

        public BattleState CurrentState =>
            _stateMachine?.CurrentStateType ?? BattleState.Initialize;
        public BattleStats PlayerStats => _playerStats;
        public DeckManager PlayerDeck => _playerDeck;
        public OriginType PlayerOrigin => _playerOrigin;
        public int CurrentTurn => _currentTurn;
        public bool IsPlayerTurn => _isPlayerTurn;

        // Multi-enemy
        public IReadOnlyList<EnemyController> Enemies => _enemies;

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
        public IReadOnlyDictionary<int, int[]> ConfusedOverrides => _confusedOverrides;

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
            _crowd?.Dispose();
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<EndTurnRequestedEvent>(OnEndTurnRequested);
            EventBus.Subscribe<PlayCardRequestedEvent>(OnPlayCardRequested);
            EventBus.Subscribe<CardVFXApplyEffectsEvent>(OnCardVFXApplyEffects);
            EventBus.Subscribe<CardVFXCompleteEvent>(OnCardVFXComplete);
            // EnemyTurncoatEvent is owned by CrowdReactions (subscribed in its constructor).
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<EndTurnRequestedEvent>(OnEndTurnRequested);
            EventBus.Unsubscribe<PlayCardRequestedEvent>(OnPlayCardRequested);
            EventBus.Unsubscribe<CardVFXApplyEffectsEvent>(OnCardVFXApplyEffects);
            EventBus.Unsubscribe<CardVFXCompleteEvent>(OnCardVFXComplete);
        }

        private void InitializeStateMachine()
        {
            _stateMachine = new StateMachine<BattleState>();

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
            GameLogger.LogInfo<BattleManager>(
                $"Starting battle: {setup.playerOrigin} vs {setup.enemies.Count} enemies"
            );

            OriginBattleStats playerOriginStats = setup.GetPlayerStats();
            _playerStats = new BattleStats(playerOriginStats.maxActionPoints, isPlayer: true);
            _playerOrigin = setup.playerOrigin;

            // Build enemy controllers — each owns its own BattleStats + StatusEffectManager
            _enemies.Clear();
            foreach (var enemyData in setup.enemies)
            {
                if (enemyData != null)
                    _enemies.Add(new EnemyController(enemyData, () => CurrentTurn));
            }
            // Register each enemy's roster index so its BattleStats can stamp hostility events.
            for (int i = 0; i < _enemies.Count; i++)
                _enemies[i].Stats.SetOwnerEnemyIndex(i);
            _focusedEnemyIndex = 0;

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

            // Passive resolver — find the asset matching this run's origin (null-safe: no passive if asset missing)
            var passive =
                _originPassives != null
                    ? System.Array.Find(
                        _originPassives,
                        p => p != null && p.Origin == _playerOrigin
                    )
                    : null;
            _passiveResolver = new PassiveResolver(
                passive,
                _playerStats,
                _effectResolver,
                _enemies,
                _effectResolver.PlayerStatusEffects,
                () => OpinionPercentage,
                this
            );
            // Event subscriptions are managed internally by PassiveResolver via EventBus

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
                _turncoatAdjacentNudge
            );

            // Opinion Meter + session shields
            int maxOpinion = setup.maxOpinion ?? 100;
            _maxTurns = setup.maxTurns ?? 0;
            _opinion = new OpinionLedger(
                maxOpinion,
                setup.startingOpinion ?? maxOpinion / 2,
                onOpinionMaxed: () => CheckAndEndBattleIfOver(),
                isEchoChamber: _crowd.IsEchoChamber
            );
            _crowd.AttachLedger(_opinion);

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
            var previous = CurrentState;
            _stateMachine.ChangeState(newState);
            EventBus.Publish(
                new BattleStateChangedEvent { Previous = previous, Current = newState }
            );
        }

        /// <summary>
        /// Adds up to <paramref name="count"/> copies of <paramref name="data"/> to the enemy row
        /// (capped so total enemies never exceed 5). Used both by enemy SummonMinion moves and by the
        /// player's summon cards (Nepo Baby). Each new body immediately declares intent for next turn.
        /// </summary>
        /// <param name="initialHostility">
        /// Overrides the body's starting hostility when set — e.g. a Nepo Baby "Call a Favor" ally
        /// spawns receptive (negative), a "Plant" spawns hostile (positive). Null uses the EnemyData default.
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
                var controller = new EnemyController(data, () => CurrentTurn);
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

        #region Event Handlers

        private void OnEndTurnRequested(EndTurnRequestedEvent evt)
        {
            if (_vfxInFlight)
                return;
            if (!_isPlayerTurn || CurrentState != BattleState.PlayerTurn)
            {
                GameLogger.LogWarning<BattleManager>("Cannot end player turn — not player's turn!");
                return;
            }
            TransitionToState(BattleState.TurnEnd);
        }

        private void OnPlayCardRequested(PlayCardRequestedEvent evt)
        {
            GameLogger.LogInfo<BattleManager>(
                $"PlayCardRequested received: '{evt.Card?.CardName}'  _vfxInFlight={_vfxInFlight}  IsPlayerTurn={_isPlayerTurn}  State={CurrentState}"
            );
            if (_vfxInFlight)
            {
                GameLogger.LogWarning<BattleManager>(
                    $"Card play blocked: VFX animation still in flight"
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
            PlayCard(evt.Card, evt.HandIndex);
        }

        private void OnCardVFXApplyEffects(CardVFXApplyEffectsEvent evt)
        {
            GameLogger.LogInfo<BattleManager>($"VFX ApplyEffects fired for '{evt.Card?.CardName}'");
            ApplyCardEffects(evt.Card, evt.AmountOverrides);
        }

        private void OnCardVFXComplete(CardVFXCompleteEvent evt)
        {
            if (_vfxInFlight)
            {
                GameLogger.LogInfo<BattleManager>(
                    $"VFX complete for '{evt.Card?.CardName}' — unblocking input"
                );
                _vfxInFlight = false;
            }
        }

        #endregion

        #region Opinion Meter / Session Shields (delegates to OpinionLedger)

        /// <summary>Raises the Opinion Meter directly (bypassing Denial). Used by heal/rally effects.</summary>
        public void RaiseOpinion(int amount) => _opinion?.RaiseDirect(amount);

        /// <summary>Grants session Support (absorbs future opinion drops).</summary>
        public void GainSupport(int amount) => _opinion?.GainSupport(amount);

        /// <summary>Grants session Denial (absorbs future opinion rises).</summary>
        public void GainDenial(int amount) => _opinion?.GainDenial(amount);

        /// <summary>Drains session Support (used by "lose Support" effects). Returns amount removed.</summary>
        public int SpendSupport(int amount) => _opinion?.SpendSupport(amount) ?? 0;

        #endregion

        #region Nepo Baby — Patronage (banked currency)

        // Player's banked Patronage. Unlike AP it persists across turns; spent on summons/installations,
        // generated only by sacrificing cards (GeneratePatronageEffect). Reset to 0 at battle start.
        private int _patronage;

        /// <summary>Current banked Patronage (Nepo Baby's spend currency).</summary>
        public int CurrentPatronage => _patronage;

        /// <summary>Banks Patronage (e.g. from a sacrifice). No-op for non-positive amounts.</summary>
        public void GainPatronage(int amount)
        {
            if (amount <= 0)
                return;
            SetPatronage(_patronage + amount);
        }

        /// <summary>Spends Patronage if affordable. Returns false (and spends nothing) if short.</summary>
        public bool SpendPatronage(int amount)
        {
            if (amount <= 0)
                return true;
            if (_patronage < amount)
                return false;
            SetPatronage(_patronage - amount);
            return true;
        }

        private void SetPatronage(int value)
        {
            int old = _patronage;
            _patronage = Mathf.Max(0, value);
            if (_patronage != old)
                EventBus.Publish(
                    new PatronageChangedEvent { OldValue = old, NewValue = _patronage }
                );
        }

        #endregion

        #region Celebrity — Attention (banked spotlight)

        // Player's banked Attention — courted/provoked, then spent as a big opinion-meter hit.
        // Banks across turns; reset to 0 at battle start.
        private int _attention;

        /// <summary>Current banked Attention (Celebrity's build-and-spend spotlight resource).</summary>
        public int CurrentAttention => _attention;

        /// <summary>Banks Attention. No-op for non-positive amounts.</summary>
        public void GainAttention(int amount)
        {
            if (amount <= 0)
                return;
            SetAttention(_attention + amount);
        }

        /// <summary>Spends Attention if affordable. Returns false (and spends nothing) if short.</summary>
        public bool SpendAttention(int amount)
        {
            if (amount <= 0)
                return true;
            if (_attention < amount)
                return false;
            SetAttention(_attention - amount);
            return true;
        }

        private void SetAttention(int value)
        {
            int old = _attention;
            _attention = Mathf.Max(0, value);
            if (_attention != old)
                EventBus.Publish(
                    new AttentionChangedEvent { OldValue = old, NewValue = _attention }
                );
        }

        #endregion

        #region Faith Leader — Pacify Conversion

        // Base pacify threshold before Jaded; each Jaded stack on the enemy raises it by 1.
        private const int PacifyBaseThreshold = 3;

        // Opinion pumped into the meter per pacify stack consumed on conversion (generous by design;
        // over-stacking past the threshold yields a proportionally bigger burst).
        private const int ConvertBurstPerStack = 3;

        // Pacify conversions made during the current player turn — read by Sermon-style harvest cards
        // via EffectContextValue.ConversionsThisTurn. Reset at the start of each player turn.
        private int _conversionsThisTurn;

        /// <summary>Faith Leader pacify conversions made this player turn (Sermon harvest scaling).</summary>
        public int ConversionsThisTurn => _conversionsThisTurn;

        /// <summary>The pacify statuses that count toward (and are consumed by) a conversion.</summary>
        public static bool IsPacifyStatus(StatusBehavior behavior) =>
            behavior != null && behavior.CountsTowardPacify;

        /// <summary>
        /// Faith Leader conversion engine. Call after a pacify status (Guilt/Shame/Doubt) lands on an
        /// enemy. When the enemy's total pacify stacks reach <c>3 + its Jaded stacks</c>, the pacify
        /// statuses are consumed and the enemy converts:
        /// <list type="bullet">
        ///   <item>Hardened enemy — can't be converted; silenced instead (no burst, no Jaded).</item>
        ///   <item>Any other enemy — a one-turn Fanatic burst pumps the meter, then it reverts to
        ///   neutral and gains a permanent Jaded stack (raising its next conversion cost).</item>
        /// </list>
        /// No-op when the threshold isn't met. Player target is ignored.
        /// </summary>
        public void TryPacifyConvert(BattleStats enemyStats, StatusEffectManager mgr)
        {
            if (enemyStats == null || mgr == null || enemyStats == _playerStats)
                return;

            int guilt = mgr.GetStacks<GuiltStatus>();
            int shame = mgr.GetStacks<ShameStatus>();
            int doubt = mgr.GetStacks<DoubtStatus>();
            int total = guilt + shame + doubt;

            int threshold = PacifyBaseThreshold + mgr.GetStacks<JadedStatus>();
            if (total < threshold)
                return;

            int idx = enemyStats.OwnerEnemyIndex;

            // Consume the pacify statuses — spent whether we convert or (Hardened) silence.
            ConsumePacifyStatus(mgr, StatusRegistry.Get<GuiltStatus>(), guilt, idx);
            ConsumePacifyStatus(mgr, StatusRegistry.Get<ShameStatus>(), shame, idx);
            ConsumePacifyStatus(mgr, StatusRegistry.Get<DoubtStatus>(), doubt, idx);

            // A true non-believer can't be converted — shut them up instead.
            if (enemyStats.IsHardened)
            {
                mgr.ApplyStatus(
                    StatusRegistry.Get<SilencedStatus>(),
                    1,
                    StatusDurationType.DecreasePerTurn
                );
                GameLogger.LogInfo<BattleManager>(
                    $"Enemy [{idx}] is Hardened — pacify failed, silenced instead"
                );
                EventBus.Publish(
                    new EnemyConvertedEvent
                    {
                        EnemyIndex = idx,
                        OpinionBurst = 0,
                        WasSilenced = true,
                    }
                );
                return;
            }

            // Convert: one-turn Fanatic burst pumping the meter (generous, scales with stacks eaten).
            int burst = total * ConvertBurstPerStack;
            _opinion.RaiseDirect(burst);

            // Revert to neutral and gain a permanent Jaded stack (raises the next conversion cost).
            enemyStats.SetHostility(0);
            mgr.ApplyStatus(StatusRegistry.Get<JadedStatus>(), 1, StatusDurationType.Permanent);

            _conversionsThisTurn++;

            GameLogger.LogInfo<BattleManager>(
                $"Enemy [{idx}] converted — {total} pacify stacks → {burst} opinion burst "
                    + $"(now Jaded {mgr.GetStacks<JadedStatus>()})"
            );
            EventBus.Publish(
                new EnemyConvertedEvent
                {
                    EnemyIndex = idx,
                    OpinionBurst = burst,
                    WasSilenced = false,
                }
            );
        }

        /// <summary>Removes a pacify status and notifies the UI (negative-stack StatusEffectAppliedEvent).</summary>
        private static void ConsumePacifyStatus(
            StatusEffectManager mgr,
            StatusBehavior behavior,
            int stacks,
            int enemyIndex
        )
        {
            if (stacks <= 0)
                return;
            mgr.RemoveStatus(behavior);
            EventBus.Publish(
                new StatusEffectAppliedEvent
                {
                    Behavior = behavior,
                    Stacks = -stacks,
                    IsToPlayer = false,
                    EnemyIndex = enemyIndex,
                }
            );
        }

        #endregion

        #region Card Playing

        /// <summary>Plays a card from the player's hand.</summary>
        private void PlayCard(CardData card, int handIndex)
        {
            BattleStats stats = _playerStats;

            if (!CanPlayCard(card, stats))
            {
                GameLogger.LogWarning<BattleManager>($"Cannot play card: {card.CardName}");
                return;
            }

            // Celebrity passive ("mastering his craft"): the first card played each battle is played
            // as its upgraded version. Swap to the upgraded instance before paying costs so the
            // upgraded cost AND effects apply. One-shot — consumed on the first play of the battle.
            if (!_firstCardPlayedThisBattle)
            {
                _firstCardPlayedThisBattle = true;
                if (_playerOrigin == OriginType.Actor && !card.IsUpgraded && card.CanUpgrade)
                {
                    var upgraded = card.CreateUpgradedInstance();
                    if (_playerDeck.SwapCardInHand(card, upgraded))
                        card = upgraded;
                }
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
            // PassiveResolver listens to CardPlayedEvent via EventBus — no direct call needed

            GameLogger.LogInfo<BattleManager>(
                $"Player played: {card.CardName}  hasVFX={(card.CardVFX != null)}"
            );

            if (card.CardVFX != null)
            {
                // VFX path: ask the UI layer to play the animation.
                // It will publish CardVFXApplyEffectsEvent at the hit frame and
                // CardVFXCompleteEvent when done; we handle both via subscriptions.
                _vfxInFlight = true;
                EventBus.Publish(
                    new CardPlayVFXRequestedEvent { Card = card, AmountOverrides = amountOverrides }
                );
            }
            else
            {
                // No VFX — resolve effects immediately then signal discard is ready.
                ApplyCardEffects(card, amountOverrides);
                EventBus.Publish(new CardVFXCompleteEvent { Card = card });
            }
        }

        /// <summary>
        /// Resolves all gameplay effects for a played card — damage, policy shifts, Momentum, Echo.
        /// Called either immediately (no VFX) or from the VFX animation's ApplyEffects event (with VFX).
        /// </summary>
        private void ApplyCardEffects(CardData card, int[] amountOverrides)
        {
            var ctx = _effectResolver.ResolveCardEffects(card, isPlayerCard: true, amountOverrides);

            // Power card (Slay-the-Spire style): its effects resolved above; now activate its
            // passives for the rest of the battle. The card is exhausted below so it leaves play.
            if (card.IsPower)
                _passiveResolver?.ActivateCardPassives(card);

            // If any effect flagged exhaust — or this is a Power card — move the card from
            // discard → exhaust pile now (PlayCardAtIndex already moved it hand → discard).
            if (ctx.ShouldExhaust || card.IsPower)
                _playerDeck.ExhaustFromDiscard(card);

            // The crowd reacts: policy/single-target hostility shifts + echo-chamber refresh.
            _crowd.OnCardPlayed(card, FocusedEnemy, FocusedEnemyIndex);
            foreach (var enemy in _enemies)
                enemy.CheckBecameHostile();
            CheckAndAdvanceFocusAfterCardPlay();
            TriggerMomentum();

            // Immediately end the battle if all enemies are dead (or player died e.g. from Thorns).
            if (CheckAndEndBattleIfOver())
                return;

            // Echo — replay the card a second time; consume the stack BEFORE the replay to
            // prevent a second Echo stack (if any) from triggering an infinite chain.
            int echoStacks = _effectResolver.PlayerStatusEffects.GetStacks<EchoStatus>();
            if (echoStacks > 0)
            {
                _effectResolver.PlayerStatusEffects.RemoveStacks<EchoStatus>(1);
                // Notify the UI and any passive listening for status changes that
                // one Echo stack was consumed (negative Stacks = removed).
                EventBus.Publish(
                    new StatusEffectAppliedEvent
                    {
                        Behavior = StatusRegistry.Get<EchoStatus>(),
                        Stacks = -1,
                        IsToPlayer = true,
                        EnemyIndex = -1,
                    }
                );
                GameLogger.LogInfo<BattleManager>($"Echo triggered — replaying {card.CardName}");
                _effectResolver.ResolveCardEffects(card, isPlayerCard: true);
                CheckAndAdvanceFocusAfterCardPlay();
                CheckAndEndBattleIfOver();
            }
        }

        private bool CanPlayCard(CardData card, BattleStats stats)
        {
            // Scandals and flagged Status cards are never playable
            if (card.IsUnplayable)
                return false;

            foreach (var cost in card.Costs)
            {
                if (cost.CostType == CostType.ActionPoints)
                {
                    if (stats.CurrentActionPoints < GetEffectiveCardCost(card))
                        return false;
                }
                else if (cost.CostType == CostType.Patronage)
                {
                    if (_patronage < cost.CurrentAmount)
                        return false;
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
                    int effective = GetEffectiveCardCost(card);
                    stats.SpendActionPoints(effective);
                    GameLogger.LogInfo<BattleManager>($"Paid {effective} AP for {card.CardName}");
                }
                else if (cost.CostType == CostType.Patronage)
                {
                    SpendPatronage(cost.CurrentAmount);
                    GameLogger.LogInfo<BattleManager>(
                        $"Paid {cost.CurrentAmount} Patronage for {card.CardName}"
                    );
                }
            }
        }

        /// <summary>
        /// Called after each card resolves. If the focused enemy just died, publishes
        /// EnemyDefeatedEvent and auto-advances focus to the next living enemy.
        /// </summary>
        private void CheckAndAdvanceFocusAfterCardPlay()
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
        /// Single source of truth for the effective AP cost of a card this battle.
        /// Applies (in order): status effect modifiers (Focus, Energized, Entangled),
        /// then per-card battle overrides (ReduceCardCost / MakeCardFree effects).
        /// Result is floored at 0.
        /// </summary>
        public int GetEffectiveCardCost(CardData card)
        {
            if (card?.Costs == null || card.Costs.Count == 0)
                return 0;
            // Find the AP cost wherever it sits in the list — a card may be double-gated
            // (e.g. Patronage + Energy), so we don't assume the AP cost is Costs[0].
            CardCost cost = null;
            foreach (var c in card.Costs)
                if (c.CostType == CostType.ActionPoints)
                {
                    cost = c;
                    break;
                }
            if (cost == null)
                return 0;

            StatusEffectManager statusMgr = _effectResolver?.PlayerStatusEffects;
            int baseCost =
                statusMgr != null
                    ? statusMgr.ModifyCardCost(cost.CurrentAmount)
                    : cost.CurrentAmount;

            // Per-card battle override (ReduceCardCost / MakeCardFree)
            int reduction = _playerDeck?.GetCardCostReduction(card) ?? 0;
            if (reduction == int.MaxValue)
                return 0; // MakeCardFree sentinel
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
                if (effects == null || effects.Count == 0)
                    continue;
                var overrides = new int[effects.Count];
                for (int j = 0; j < effects.Count; j++)
                    overrides[j] = UnityEngine.Random.Range(0, 4); // [0, 3] inclusive
                _confusedOverrides[i] = overrides;
            }
            GameLogger.LogInfo<BattleManager>(
                $"Confused: randomized amounts for {_confusedOverrides.Count} cards in hand"
            );
        }

        /// <summary>
        /// If the player has Momentum stacks, deals stacks damage to a random living enemy.
        /// Called once per card play (before Echo replay).
        /// </summary>
        private void TriggerMomentum()
        {
            int stacks =
                _effectResolver?.PlayerStatusEffects?.GetStacks<MomentumStatus>() ?? 0;
            if (stacks <= 0)
                return;

            var living = LivingEnemies.ToList();
            if (living.Count == 0)
                return;

            var target = living[UnityEngine.Random.Range(0, living.Count)];
            // Momentum presses the opinion meter through the ledger (absorbs once, then raises opinion).
            GameLogger.LogInfo<BattleManager>(
                $"Momentum pressing opinion by {stacks} vs {target.EnemyData.EnemyName}"
            );
            _opinion.ApplyPressure(
                stacks,
                toPlayer: false,
                attackerName: "Player",
                sourceEnemyIndex: -1,
                targetEnemyIndex: _enemies.IndexOf(target)
            );
        }

        /// <summary>
        /// After a card is removed from the hand, all hand indices above the played index shift
        /// down by 1. This keeps _confusedOverrides aligned with the updated hand layout.
        /// </summary>
        private void ShiftConfusedOverridesAfterPlay(int playedIndex)
        {
            if (_confusedOverrides.Count == 0)
                return;
            var shifted = new Dictionary<int, int[]>(_confusedOverrides.Count);
            foreach (var kvp in _confusedOverrides)
            {
                if (kvp.Key == playedIndex)
                    continue; // this entry is now gone
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
        private bool CheckAndEndBattleIfOver()
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
            // Session shields decay at the start of every turn, then Ritual refills them.
            _opinion.DecayShields();

            if (_isPlayerTurn)
            {
                // Fresh tally each player turn for Sermon-style harvest scaling.
                _conversionsThisTurn = 0;

                _playerStats.StartTurn();
                _effectResolver.PlayerStatusEffects.OnTurnStart(_playerStats);

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

                // Confused: randomize card effect amounts for this turn
                if (_effectResolver.PlayerStatusEffects.HasStatus<ConfusedStatus>())
                    ApplyConfusedOverrides();
                else
                    _confusedOverrides.Clear();
            }
            else
            {
                foreach (var enemy in LivingEnemies)
                {
                    enemy.Stats.StartTurn();
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
                _playerStats.EndTurn();
                // Apply opinion-affecting statuses (read at current stacks) before OnTurnEnd decrements.
                ApplyTurnEndOpinionStatuses(_effectResolver.PlayerStatusEffects);
                _effectResolver.PlayerStatusEffects.OnTurnEnd(_playerStats);

                // Echo-chamber decay — bleeds sentiment while the whole room is receptive.
                // Checked after the player's full turn, so breaking the chamber this turn avoids it.
                _crowd.ApplyTurnEndDecay();

                _playerDeck.EndTurn();
            }
            else
            {
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
                _opinion.ApplyPressure(
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

        #region Battle States

        /// <summary>Shared base for the battle's FSM states — holds the owning BattleManager.</summary>
        private abstract class BattleStateBase : State
        {
            protected readonly BattleManager _manager;

            protected BattleStateBase(BattleManager manager) => _manager = manager;
        }

        /// <summary>Initialize State — draws the player's opening hand.</summary>
        private class InitializeState : BattleStateBase
        {
            public InitializeState(BattleManager manager)
                : base(manager) { }

            public override void OnEnter()
            {
                GameLogger.LogInfo<BattleManager>("Initializing battle...");

                // Patronage banks across turns but not across battles — start each battle empty.
                _manager.SetPatronage(0);
                _manager.SetAttention(0);
                _manager._firstCardPlayedThisBattle = false;

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
        private class TurnStartState : BattleStateBase
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
                    _manager._playerTurnNumber++;
                    _manager._passiveResolver?.FireTurnStart(_manager._playerTurnNumber);

                    // Count bonus draws BEFORE snapshotting (BecameHostileThisTurn reflects last turn's escalations)
                    int bonusDraws = 0;
                    foreach (var enemy in _manager._enemies)
                    {
                        if (
                            !enemy.IsDefeated
                            && (enemy.Stats.IsHostile || enemy.BecameHostileThisTurn)
                        )
                            bonusDraws++;
                    }

                    // Snapshot hostility for the new turn (resets BecameHostileThisTurn on all enemies)
                    foreach (var enemy in _manager._enemies)
                        enemy.SnapshotHostilityForTurn();

                    // Draw cards — base draw plus one per hostile/newly-hostile enemy
                    int totalDraw = _manager._cardsPerTurn + bonusDraws;
                    _manager._playerDeck.StartTurn(totalDraw);

                    if (bonusDraws > 0)
                        GameLogger.LogInfo<BattleManager>(
                            $"Hostile crowd: drawing +{bonusDraws} extra card(s) ({totalDraw} total)"
                        );

                    // Every living enemy declares their intent (Slay the Spire timing)
                    for (int i = 0; i < _manager._enemies.Count; i++)
                    {
                        var enemy = _manager._enemies[i];
                        if (enemy.IsDefeated)
                            continue;
                        EnemyMoveData intent = enemy.SelectNextMove(_manager._enemies);
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

        /// <summary>Player Turn State — waits for EndTurnRequestedEvent from the UI.</summary>
        private class PlayerTurnState : BattleStateBase
        {
            public PlayerTurnState(BattleManager manager)
                : base(manager) { }

            public override void OnEnter() =>
                GameLogger.LogInfo<BattleManager>("Player's turn started");

            public override void OnExit() =>
                GameLogger.LogInfo<BattleManager>("Player's turn ended");

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
        private class OpponentTurnState : BattleStateBase
        {
            public OpponentTurnState(BattleManager manager)
                : base(manager) { }

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

                // Two-pass resolution. Pass 1: modifier intents (e.g. RileOthers) resolve first so
                // their board changes — amplifying allies' hostility, summoning bodies — land before
                // the direct hits. Pass 2: direct intents (attacks, shields) resolve left to right.
                for (int i = 0; i < enemyCount; i++)
                {
                    var intent = _manager._enemies[i].CurrentIntent;
                    if (intent != null && IsModifierIntent(intent.MoveType))
                    {
                        yield return _manager.StartCoroutine(ResolveSingleEnemyAction(i));
                        if (_manager.CurrentState == BattleState.BattleEnd)
                            yield break;
                    }
                }

                for (int i = 0; i < enemyCount; i++)
                {
                    var intent = _manager._enemies[i].CurrentIntent;
                    if (intent != null && !IsModifierIntent(intent.MoveType))
                    {
                        yield return _manager.StartCoroutine(ResolveSingleEnemyAction(i));
                        if (_manager.CurrentState == BattleState.BattleEnd)
                            yield break;
                    }
                }

                // Restore resolver to the player's current focused target
                if (_manager.FocusedEnemy != null)
                    _manager._effectResolver.SetFocusedOpponent(
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
            /// stop once <see cref="CurrentState"/> is BattleEnd.
            /// </summary>
            private IEnumerator ResolveSingleEnemyAction(int i)
            {
                var enemy = _manager._enemies[i];
                if (enemy.IsDefeated || enemy.CurrentIntent == null)
                    yield break;

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
                    yield break;
                }

                // Doubt (pacify): a doubting enemy may hold back its action (soft skip, 25% per stack).
                int doubt = enemy.StatusEffects.GetStacks<DoubtStatus>();
                if (doubt > 0)
                {
                    float doubtSkip = Mathf.Clamp01(doubt * 0.25f);
                    if (UnityEngine.Random.value < doubtSkip)
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
                        yield break;
                    }
                }

                // Receptive enemies have a chance to hold back (20% per negative hostility stack).
                if (enemy.Stats.IsReceptive)
                {
                    float skipChance = Mathf.Clamp01(Mathf.Abs(enemy.Stats.CurrentHostility) * 0.20f);
                    if (UnityEngine.Random.value < skipChance)
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
                        yield break;
                    }
                }

                // Signal the UI: this enemy is about to act (shake + highlight intent panel)
                EventBus.Publish(new EnemyActingEvent { EnemyIndex = i, Move = enemy.CurrentIntent });

                // Brief pause so the player sees the signal before damage lands
                yield return new WaitForSeconds(_manager._perEnemyAttackDelay);

                GameLogger.LogInfo<BattleManager>(
                    $"Enemy [{i}] {enemy.EnemyData.EnemyName} executes: {enemy.CurrentIntent.MoveName}"
                );

                // Temporarily point EffectResolver at this enemy as the caster
                _manager._effectResolver.SetFocusedOpponent(
                    enemy.Stats,
                    enemy.StatusEffects,
                    i,
                    enemy.EnemyData.EnemyName
                );
                var move = enemy.CurrentIntent;
                yield return _manager.StartCoroutine(
                    _manager._effectResolver.ResolveEnemyMoveEffects(move)
                );

                // If the player was defeated by this move, end the battle before any further actions.
                if (_manager.CheckAndEndBattleIfOver())
                    yield break;

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

        /// <summary>Turn End State — cleanup effects, check victory, advance.</summary>
        private class TurnEndState : BattleStateBase
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
                    _manager._playerTurnsElapsed++;

                    int remaining =
                        _manager._maxTurns > 0
                            ? Mathf.Max(0, _manager._maxTurns - _manager._playerTurnsElapsed)
                            : 0;

                    EventBus.Publish(
                        new TurnLimitUpdatedEvent
                        {
                            PlayerTurnsElapsed = _manager._playerTurnsElapsed,
                            MaxTurns = _manager._maxTurns,
                            TurnsRemaining = remaining,
                        }
                    );

                    if (
                        _manager._maxTurns > 0
                        && _manager._playerTurnsElapsed >= _manager._maxTurns
                    )
                    {
                        // Judgment — outcome decided by majority opinion
                        var ledger = _manager._opinion;
                        int threshold = ledger.MaxOpinion / 2;
                        bool isVictory = ledger.CurrentOpinion >= threshold;

                        _manager._battleResult = new BattleResult
                        {
                            isVictory = isVictory,
                            turnsToWin = _manager._currentTurn,
                            finalPlayerSupport = ledger.CurrentSupport,
                            finalPlayerHostility = _manager._playerStats.CurrentHostility,
                            finalOpinion = ledger.CurrentOpinion,
                            wasJudgmentVictory = isVictory,
                        };

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

        /// <summary>Battle End State — publishes the result event.</summary>
        private class BattleEndState : BattleStateBase
        {
            public BattleEndState(BattleManager manager)
                : base(manager) { }

            public override void OnEnter()
            {
                BattleResult result = _manager.GetBattleResult();
                GameLogger.LogInfo<BattleManager>(
                    $"Battle ended — {(result.isVictory ? "VICTORY" : "DEFEAT")}"
                );
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
        public OriginType playerOrigin;
        public OriginStats originStats;
        public List<CardData> playerDeck = new List<CardData>();

        /// <summary>All enemies present in this room (1–5). Order = display order.</summary>
        public List<EnemyData> enemies = new List<EnemyData>();

        /// <summary>Maximum number of player turns before Judgment is called. 0 = no limit.</summary>
        public int? maxTurns;

        /// <summary>Starting Opinion Meter value. When null, defaults to half of maxOpinion.</summary>
        public int? startingOpinion;

        /// <summary>Maximum Opinion Meter value. Defaults to 100.</summary>
        public int? maxOpinion;

        /// <summary>Gets the player's battle stats based on their origin.</summary>
        public OriginBattleStats GetPlayerStats()
        {
            return originStats != null
                ? originStats.GetStatsForOrigin(playerOrigin)
                : new OriginBattleStats { maxActionPoints = 3 };
        }
    }

    /// <summary>Result data from a completed battle.</summary>
    [Serializable]
    public class BattleResult
    {
        public bool isVictory;
        public int turnsToWin;
        public int finalPlayerSupport;
        public int finalPlayerHostility;
        public int finalOpinion;
        public bool wasJudgmentVictory;

        // TODO: Add rewards when reward system exists
    }
}
