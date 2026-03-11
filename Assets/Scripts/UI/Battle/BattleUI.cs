using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Gameplay.Battle;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Utilities;
using Crookedile.UI.Reward;
using Random = UnityEngine.Random;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Thin FSM coordinator for the battle screen UI.
    ///
    /// Responsibilities:
    ///  - Holds references to all panel components and the BattleManager.
    ///  - Subscribes to EventBus events and translates them into FSM state transitions.
    ///  - Manages enemy slot instantiation and card zone viewer overlays.
    ///  - Owns stats / battle-info text updates (small, centralised).
    ///
    /// Hand display, battle log, and result panels are delegated to their own
    /// MonoBehaviour components (HandPanel, BattleLogPanel, BattleResultPanel).
    /// Each BattleUIState inner class owns the show/hide/enable logic for one state.
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        // ── Panels (extracted subsystems) ─────────────────────────────────────
        [Header("Panels")]
        [Tooltip("Manages card hand display and object pool.")]
        [SerializeField] private HandPanel         handPanel;
        [Tooltip("Battle log text + auto-scroll.")]
        [SerializeField] private BattleLogPanel    logPanel;
        [Tooltip("Victory / defeat result panels.")]
        [SerializeField] private BattleResultPanel resultPanel;

        // ── Enemy Slots ───────────────────────────────────────────────────────
        [Header("Enemy Slots")]
        [Tooltip("Parent transform that enemy slot panels are spawned into.")]
        [SerializeField] private Transform  enemySlotContainer;
        [Tooltip("Prefab with an EnemySlotUI component — instantiated once per enemy.")]
        [SerializeField] private GameObject enemySlotPrefab;

        // ── VFX Anchors ───────────────────────────────────────────────────────
        [Header("VFX Anchors")]
        [Tooltip("RectTransform of the player stats panel — fallback VFX target when PlayerSlotUI is not assigned.")]
        [field: SerializeField] public RectTransform PlayerStatsPanel { get; private set; }

        // ── Player Slot ───────────────────────────────────────────────────────
        [Header("Player Slot")]
        [Tooltip("PlayerSlotUI instance in the scene. Provides the portrait, health bar, and VFX anchor for player-targeted effects.")]
        [SerializeField] private PlayerSlotUI _playerSlotUI;

        /// <summary>
        /// RectTransform anchor at the player slot — preferred target for VFX and floating damage numbers.
        /// Falls back to <see cref="PlayerStatsPanel"/> when no slot is assigned.
        /// </summary>
        public RectTransform PlayerSlotTransform =>
            _playerSlotUI != null ? _playerSlotUI.SlotRect : PlayerStatsPanel;

        // ── Battle Info ───────────────────────────────────────────────────────
        [Header("Battle Info")]
        [SerializeField] private TMP_Text turnNumberText;
        [SerializeField] private TMP_Text phaseText;
        [Tooltip("Seconds the turn/phase label stays fully visible before fading.")]
        [SerializeField] private float _battleInfoHoldTime = 1.5f;
        [Tooltip("Seconds the fade-out takes after the hold delay.")]
        [SerializeField] private float _battleInfoFadeTime = 0.5f;

        // ── Controls ──────────────────────────────────────────────────────────
        [Header("Controls")]
        [SerializeField] private Button endTurnButton;
        [Tooltip("Actor passive — shown on the Actor's first player turn only.")]
        [SerializeField] private Button             improviseButton;
        [Tooltip("Card selection modal shared by Improvise and ChooseFromDiscard effects.")]
        [SerializeField] private CardSelectionPanel cardSelectionPanel;
        [Tooltip("General-purpose interactive card picker for card-choice effects (ChooseFromDiscard, Upgrade, Retain, etc.).")]
        [SerializeField] private CardChoicePanel    cardChoicePanel;

        // ── Card Zone Buttons ─────────────────────────────────────────────────
        [Header("Card Zone Buttons")]
        [SerializeField] private Button   discardZoneButton;
        [SerializeField] private Button   exhaustZoneButton;
        [SerializeField] private Button   deckZoneButton;
        [SerializeField] private TMP_Text discardCountText;
        [SerializeField] private TMP_Text exhaustCountText;
        [SerializeField] private TMP_Text deckCountText;

        [Header("Card Zone Panel")]
        [SerializeField] private CardZonePanel cardZonePanel;

        [Header("Reward")]
        [Tooltip("CardDatabase ScriptableObject used to generate post-battle card offers.")]
        [SerializeField] private CardDatabase  _cardDatabase;
        [Tooltip("Reward screen overlay panel (starts inactive). Shown after a victory Continue click.")]
        [SerializeField] private RewardScreen  _rewardScreen;

        [Header("Card Grant Animation")]
        [Tooltip("Seconds for the zone count text to scale up on card grant arrival.")]
        [SerializeField] private float _countPunchDuration = 0.25f;
        [Tooltip("Scale multiplier applied to the count text at the peak of the punch.")]
        [SerializeField] private float _countPunchScale = 1.4f;

        // ── Runtime ───────────────────────────────────────────────────────────
        private BattleManager               battleManager;
        private StateMachine<BattleUIState> _fsm;
        private BattleResult                _lastBattleResult;
        private List<EnemySlotUI>           _enemySlots = new List<EnemySlotUI>();
        private CardChoiceRequestedEvent    _pendingCardChoice;
        private bool                        _handRefreshPending;
        /// <summary>Card button extracted from hand on CardPlayedEvent, waiting for VFX to finish before animating to discard.</summary>
        private CardButton                  _pendingDiscardButton;
        private HashSet<CardData>           _pendingDrawnCards = new HashSet<CardData>();
        private Coroutine                   _battleInfoFade;
        private Coroutine                   _countPunchCoroutine;

        #region Initialization

        private void Awake()
        {
            if (endTurnButton != null)
                endTurnButton.onClick.AddListener(OnEndTurnClicked);

        }

        private void OnEnable()  => SubscribeToEvents();
        private void OnDisable() => UnsubscribeFromEvents();

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<TurnEndedEvent>(OnTurnEnded);
            EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Subscribe<BattleEndedEvent>(OnBattleEnded);
            EventBus.Subscribe<EnemyIntentDeclaredEvent>(OnEnemyIntentDeclared);
            EventBus.Subscribe<EnemyHostilityChangedEvent>(OnEnemyHostilityChanged);
            EventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            EventBus.Subscribe<EnemySummonedEvent>(OnEnemySummoned);
            EventBus.Subscribe<CardChoiceRequestedEvent>(OnCardChoiceRequested);
            EventBus.Subscribe<ResolveChangedEvent>(OnResolveChanged);
            EventBus.Subscribe<ComposureChangedEvent>(OnComposureChanged);
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Subscribe<EnemyActingEvent>(OnEnemyActing);
            EventBus.Subscribe<CardDrawnEvent>(OnCardDrawn);
            EventBus.Subscribe<StatusEffectAppliedEvent>(OnStatusEffectApplied);
            EventBus.Subscribe<CardVFXCompleteEvent>(OnCardVFXComplete);
            EventBus.Subscribe<CardGrantedEvent>(OnCardGranted);
            EventBus.Subscribe<CardExhaustedEvent>(OnCardExhausted);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<TurnEndedEvent>(OnTurnEnded);
            EventBus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
            EventBus.Unsubscribe<EnemyIntentDeclaredEvent>(OnEnemyIntentDeclared);
            EventBus.Unsubscribe<EnemyHostilityChangedEvent>(OnEnemyHostilityChanged);
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            EventBus.Unsubscribe<EnemySummonedEvent>(OnEnemySummoned);
            EventBus.Unsubscribe<CardChoiceRequestedEvent>(OnCardChoiceRequested);
            EventBus.Unsubscribe<ResolveChangedEvent>(OnResolveChanged);
            EventBus.Unsubscribe<ComposureChangedEvent>(OnComposureChanged);
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Unsubscribe<EnemyActingEvent>(OnEnemyActing);
            EventBus.Unsubscribe<CardDrawnEvent>(OnCardDrawn);
            EventBus.Unsubscribe<StatusEffectAppliedEvent>(OnStatusEffectApplied);
            EventBus.Unsubscribe<CardVFXCompleteEvent>(OnCardVFXComplete);
            EventBus.Unsubscribe<CardGrantedEvent>(OnCardGranted);
            EventBus.Unsubscribe<CardExhaustedEvent>(OnCardExhausted);
        }

        /// <summary>
        /// Called by BattleManager (or a scene initializer) once the battle is ready.
        /// Sets up zone viewers, builds the FSM, and enters the Idle state.
        /// </summary>
        public void Initialize(BattleManager manager)
        {
            battleManager = manager;

            discardZoneButton?.onClick.AddListener(ShowDiscardZone);
            exhaustZoneButton?.onClick.AddListener(ShowExhaustZone);
            deckZoneButton?.onClick.AddListener(ShowDeckZone);

            if (resultPanel != null)
                resultPanel.OnContinueClicked += OnResultContinueClicked;

            // Build FSM — state classes are inner private classes below.
            _fsm = new StateMachine<BattleUIState>();
            _fsm.RegisterState(BattleUIState.Idle,                new IdleBattleUIState(this));
            _fsm.RegisterState(BattleUIState.PlayerTurn,          new PlayerTurnBattleUIState(this));
_fsm.RegisterState(BattleUIState.WaitingForCardChoice, new WaitingForCardChoiceBattleUIState(this));
            _fsm.RegisterState(BattleUIState.BattleEnd,           new BattleEndBattleUIState(this));
            _fsm.ChangeState(BattleUIState.Idle);

            UpdateStatsDisplay();
        }

        #endregion

        #region Event Handlers

        private void OnBattleStarted(BattleStartedEvent evt)
        {
            logPanel?.AddEntry("=== Battle Started ===");
            _playerSlotUI?.Initialize(battleManager, evt.Setup.GetPlayerStats().portrait);
            BuildEnemySlots();
            UpdateStatsDisplay();
            _fsm?.ChangeState(BattleUIState.Idle);
        }

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            string owner = evt.IsPlayerTurn ? "Player" : "Opponent";
            logPanel?.AddEntry($"--- Turn {evt.TurnNumber}: {owner} ---");
            UpdateStatsDisplay();
            UpdateBattleInfo();
            _fsm?.ChangeState(evt.IsPlayerTurn ? BattleUIState.PlayerTurn : BattleUIState.Idle);
        }

        private void OnTurnEnded(TurnEndedEvent evt)
        {
            UpdateStatsDisplay();
            _fsm?.ChangeState(BattleUIState.Idle);
        }

        private void OnCardPlayed(CardPlayedEvent evt)
        {
            logPanel?.AddEntry($"{(evt.IsPlayer ? "Player" : "Opponent")} played: {evt.Card.CardName}");
            UpdateStatsDisplay();

            if (evt.IsPlayer)
            {
                // Extract the card from hand immediately so the layout closes the gap,
                // but hold it — the discard animation fires in OnCardVFXComplete so the
                // sequence is: VFX resolves → card flies to discard → new draws appear.
                GameLogger.LogInfo("Card", $"Extracted '{evt.Card.CardName}' from hand — awaiting VFX complete before discard");
                _pendingDiscardButton = handPanel?.ExtractCard(evt.Card);
            }
            else
            {
                // Enemy card — no VFX sequencing needed; refresh hand immediately.
                if (!_handRefreshPending)
                {
                    _handRefreshPending = true;
                    StartCoroutine(RefreshHandNextFrame());
                }
            }
        }

        /// <summary>
        /// Fires after a played card's VFX animation fully completes (or immediately if no VFX).
        /// Begins the discard animation; once the card lands in the discard pile the hand
        /// refreshes — so newly drawn cards appear AFTER the discard, not during VFX.
        /// </summary>
        private void OnCardVFXComplete(CardVFXCompleteEvent evt)
        {
            GameLogger.LogInfo("Card", $"CardVFXComplete for '{evt.Card?.CardName}' — starting discard animation");

            if (_pendingDiscardButton != null)
            {
                var btn = _pendingDiscardButton;
                _pendingDiscardButton = null;

                CardFlyAnimator.Instance?.AnimateDiscardOut(btn, () =>
                {
                    GameLogger.LogInfo("Card", $"Discard animation done for '{btn.CardData?.CardName}' — returning to pool and refreshing hand");
                    BattlePoolManager.Instance?.ReturnCard(btn);

                    // Refresh hand AFTER discard so any drawn cards appear once the discard lands.
                    if (!_handRefreshPending)
                    {
                        _handRefreshPending = true;
                        StartCoroutine(RefreshHandNextFrame());
                    }
                });
            }
            else
            {
                // No card to discard (no-VFX card that was already handled, or edge case).
                GameLogger.LogWarning("Card", $"CardVFXComplete for '{evt.Card?.CardName}' but no pending discard button found");
                if (!_handRefreshPending)
                {
                    _handRefreshPending = true;
                    StartCoroutine(RefreshHandNextFrame());
                }
            }
        }

        private void OnCardDrawn(CardDrawnEvent evt)
        {
            if (!evt.IsPlayer) return;              // enemy draws don't affect the player's hand panel
            _pendingDrawnCards.Add(evt.Card);       // track which cards are new this batch
            if (_handRefreshPending) return;        // coroutine already running — just add to batch
            _handRefreshPending = true;
            StartCoroutine(RefreshHandNextFrame());
        }

        private void OnCardGranted(CardGrantedEvent evt)
        {
            if (!evt.IsPlayer) return;
            Transform target  = evt.ToDiscard ? discardZoneButton.transform : deckZoneButton.transform;
            TMP_Text  counter = evt.ToDiscard ? discardCountText            : deckCountText;
            StartCoroutine(CardGrantedAnimationSequence(evt.Card, target, counter));
        }

        private void OnCardExhausted(CardExhaustedEvent evt)
        {
            // Ensure exhaust count is always up-to-date regardless of trigger source
            // (ExhaustFromDiscard does not go through CardPlayedEvent → UpdateStatsDisplay).
            if (!evt.IsPlayer) return;
            UpdateStatsDisplay();
        }

        private IEnumerator RefreshHandNextFrame()
        {
            yield return null;  // wait one frame so all draw events from one effect batch together
            _handRefreshPending = false;
            var drawn = _pendingDrawnCards.Count > 0
                ? new HashSet<CardData>(_pendingDrawnCards) : null;
            _pendingDrawnCards.Clear();

            // If cards were drawn, merge them in and animate only the new ones;
            // otherwise just reposition and re-init the existing buttons.
            if (drawn != null)
                handPanel?.AddDrawnCards(drawn, battleManager, OnCardButtonClicked);
            else
                handPanel?.RearrangeCurrentHand(battleManager, OnCardButtonClicked);
        }

        /// <summary>
        /// Rents a card button, initialises it display-only, then asks CardFlyAnimator to
        /// show it at screen centre and fly it to the target zone.  On arrival the count
        /// text receives a scale-punch and the button is returned to the pool.
        /// </summary>
        private IEnumerator CardGrantedAnimationSequence(CardData card, Transform targetZone, TMP_Text countText)
        {
            var btn = BattlePoolManager.Instance?.RentCard(card.CardType, transform);
            if (btn == null)
            {
                UpdateStatsDisplay();
                yield break;
            }

            int ap   = battleManager?.PlayerStats.CurrentActionPoints ?? 0;
            int cost = battleManager?.GetEffectiveCardCost(card) ?? 1;
            btn.Initialize(card, 0, ap, cost, forceUnplayable: true);

            bool arrived = false;
            CardFlyAnimator.Instance?.AnimateCardGranted(btn, targetZone, () =>
            {
                UpdateStatsDisplay();
                PunchCountText(countText);
                BattlePoolManager.Instance?.ReturnCard(btn);
                arrived = true;
            });

            yield return new WaitUntil(() => arrived);
        }

        private void PunchCountText(TMP_Text text)
        {
            if (text == null) return;
            if (_countPunchCoroutine != null) StopCoroutine(_countPunchCoroutine);
            _countPunchCoroutine = StartCoroutine(CountTextPunchRoutine(text));
        }

        private IEnumerator CountTextPunchRoutine(TMP_Text text)
        {
            if (text == null) yield break;

            float halfDuration = _countPunchDuration * 0.5f;

            // Scale up
            float t = 0f;
            while (t < halfDuration)
            {
                t += Time.deltaTime;
                float frac = Mathf.Clamp01(t / halfDuration);
                text.transform.localScale = Vector3.one * Mathf.Lerp(1f, _countPunchScale, frac);
                yield return null;
            }

            // Ease-out back to 1 — (1-t)² decelerates to a smooth stop
            t = 0f;
            while (t < halfDuration)
            {
                t += Time.deltaTime;
                float frac      = Mathf.Clamp01(t / halfDuration);
                float remaining = (1f - frac) * (1f - frac);
                text.transform.localScale = Vector3.one * Mathf.Lerp(1f, _countPunchScale, remaining);
                yield return null;
            }

            text.transform.localScale = Vector3.one;
            _countPunchCoroutine = null;
        }

        private void OnEnemyIntentDeclared(EnemyIntentDeclaredEvent evt)
        {
            if (evt.Move != null)
                logPanel?.AddEntry($"Enemy [{evt.EnemyIndex}] intends: {evt.Move.IntentDescription}");
            if (evt.EnemyIndex < _enemySlots.Count)
                _enemySlots[evt.EnemyIndex]?.UpdateIntent(evt.Move);
        }

        private void OnEnemyHostilityChanged(EnemyHostilityChangedEvent evt)
        {
            if (evt.EnemyIndex < _enemySlots.Count)
            {
                _enemySlots[evt.EnemyIndex]?.Refresh();
                _enemySlots[evt.EnemyIndex]?.PulseHostility();
            }
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            logPanel?.AddEntry($"{evt.EnemyName} defeated!");
            if (evt.EnemyIndex >= _enemySlots.Count) return;

            var slot = _enemySlots[evt.EnemyIndex];
            if (slot == null) return;

            if (BattlePoolManager.Instance != null) BattlePoolManager.Instance.ReturnSlot(slot);
            else Destroy(slot.gameObject);

            _enemySlots[evt.EnemyIndex] = null;
        }

        private void OnEnemySummoned(EnemySummonedEvent evt)
        {
            logPanel?.AddEntry($"{evt.EnemyData.EnemyName} was summoned!");
            AddEnemySlot(evt.EnemyIndex);
            UpdateStatsDisplay();
        }

        private void OnBattleEnded(BattleEndedEvent evt)
        {
            string outcome = evt.Result.isVictory ? "=== VICTORY ===" : "=== DEFEAT ===";
            logPanel?.AddEntry(outcome);
            _lastBattleResult = evt.Result;

            // Persist the player's remaining HP so it carries into the next battle.
            if (evt.Result.isVictory)
                RunState.Current?.UpdateResolve(evt.Result.finalPlayerResolve);

            _fsm?.ChangeState(BattleUIState.BattleEnd);
        }

        private void OnCardChoiceRequested(CardChoiceRequestedEvent evt)
        {
            _pendingCardChoice = evt;
            _fsm?.ChangeState(BattleUIState.WaitingForCardChoice);
        }

        private void OnResolveChanged(ResolveChangedEvent evt)         => UpdateStatsDisplay();
        private void OnComposureChanged(ComposureChangedEvent evt)      => UpdateStatsDisplay();
        private void OnStatusEffectApplied(StatusEffectAppliedEvent evt)
        {
            UpdateStatsDisplay();

            // When Stunned is applied to an enemy, immediately hide their intent display.
            // The enemy can't act this turn, so showing a planned move would be misleading.
            if (evt.StatusType == StatusEffectType.Stunned && !evt.IsToPlayer && evt.Stacks > 0)
            {
                for (int i = 0; i < _enemySlots.Count; i++)
                {
                    if (battleManager != null &&
                        i < battleManager.Enemies.Count &&
                        battleManager.Enemies[i].StatusEffects.HasEffect(StatusEffectType.Stunned))
                        _enemySlots[i]?.ClearIntent();
                }
            }
        }

        private void OnDamageDealt(DamageDealtEvent evt)
        {
            if (!evt.IsToPlayer) return;
            logPanel?.AddEntry($"{evt.AttackerName} dealt {evt.Amount} damage");
        }

        private void OnEnemyActing(EnemyActingEvent evt)
        {
            if (evt.EnemyIndex >= _enemySlots.Count) return;
            _enemySlots[evt.EnemyIndex]?.PulseIntent();
            _enemySlots[evt.EnemyIndex]?.ClearIntent();
        }

        #endregion

        #region UI Updates (stats + battle-info text; kept in BattleUI for direct field access)

        internal void UpdateStatsDisplay()
        {
            if (battleManager == null) return;

            _playerSlotUI?.Refresh();

            // Enemy slots — refresh + focus highlight
            for (int i = 0; i < _enemySlots.Count; i++)
            {
                _enemySlots[i]?.Refresh();
                _enemySlots[i]?.SetSelected(i == battleManager.FocusedEnemyIndex);
            }

            // Card zone counts
            DeckManager deck = battleManager.PlayerDeck;
            if (deck != null)
            {
                if (discardCountText != null) discardCountText.text = deck.DiscardCount.ToString();
                if (exhaustCountText != null) exhaustCountText.text = deck.ExhaustCount.ToString();
                if (deckCountText    != null) deckCountText.text    = deck.DeckCount.ToString();
            }
        }

        internal void UpdateBattleInfo()
        {
            if (battleManager == null) return;

            if (turnNumberText != null)
            {
                turnNumberText.text  = $"Turn: {battleManager.CurrentTurn}";
                turnNumberText.alpha = 1f;
            }

            if (phaseText != null)
            {
                string turnOwner = battleManager.IsPlayerTurn ? "Player" : "Opponent";
                phaseText.text  = $"{battleManager.CurrentState} ({turnOwner})";
                phaseText.alpha = 1f;
            }

            // Restart the fade — cancel any in-progress fade so the text shows fully first.
            if (_battleInfoFade != null) StopCoroutine(_battleInfoFade);
            _battleInfoFade = StartCoroutine(FadeBattleInfo());
        }

        private IEnumerator FadeBattleInfo()
        {
            yield return new WaitForSeconds(_battleInfoHoldTime);

            float elapsed = 0f;
            while (elapsed < _battleInfoFadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / _battleInfoFadeTime);
                if (turnNumberText != null) turnNumberText.alpha = alpha;
                if (phaseText      != null) phaseText.alpha      = alpha;
                yield return null;
            }

            if (turnNumberText != null) turnNumberText.alpha = 0f;
            if (phaseText      != null) phaseText.alpha      = 0f;
            _battleInfoFade = null;
        }

        #endregion

        #region Enemy Slots

        private void BuildEnemySlots()
        {
            // Return all current slots to the pool (or destroy if no pool).
            foreach (var slot in _enemySlots)
            {
                if (slot == null) continue;
                if (BattlePoolManager.Instance != null) BattlePoolManager.Instance.ReturnSlot(slot);
                else Destroy(slot.gameObject);
            }
            _enemySlots.Clear();

            if (enemySlotContainer == null || battleManager == null) return;
            if (BattlePoolManager.Instance == null && enemySlotPrefab == null) return;

            for (int i = 0; i < battleManager.Enemies.Count; i++)
            {
                EnemySlotUI slot = BattlePoolManager.Instance != null
                    ? BattlePoolManager.Instance.RentSlot(enemySlotContainer)
                    : Instantiate(enemySlotPrefab, enemySlotContainer).GetComponent<EnemySlotUI>();

                if (slot != null)
                {
                    slot.Initialize(i, battleManager, battleManager.PlayerOrigin, battleManager.Enemies[i].EnemyData);
                    _enemySlots.Add(slot);
                }
            }
        }

        /// <summary>
        /// Spawns a new enemy slot for the enemy at <paramref name="index"/> in
        /// <c>BattleManager.Enemies</c>. Called when a <c>SummonMinion</c> move fires.
        /// </summary>
        private void AddEnemySlot(int index)
        {
            if (enemySlotContainer == null || battleManager == null) return;
            if (BattlePoolManager.Instance == null && enemySlotPrefab == null) return;
            if (index >= battleManager.Enemies.Count) return;

            EnemySlotUI slot = BattlePoolManager.Instance != null
                ? BattlePoolManager.Instance.RentSlot(enemySlotContainer)
                : Instantiate(enemySlotPrefab, enemySlotContainer).GetComponent<EnemySlotUI>();

            if (slot != null)
            {
                slot.Initialize(index, battleManager, battleManager.PlayerOrigin, battleManager.Enemies[index].EnemyData);
                _enemySlots.Add(slot);
            }
        }

        /// <summary>
        /// Returns the <see cref="RectTransform"/> of the enemy slot at the given index,
        /// or null if the index is out of range or the slot has been destroyed.
        /// Used by <see cref="BattleFeedbackController"/> to aim VFX at specific enemy panels.
        /// </summary>
        public RectTransform GetEnemySlotTransform(int index)
        {
            if (index < 0 || index >= _enemySlots.Count || _enemySlots[index] == null)
                return null;
            return _enemySlots[index].GetComponent<RectTransform>();
        }

        #endregion

        #region Input Handlers

        private void OnEndTurnClicked()
        {
            if (battleManager != null && battleManager.IsPlayerTurn)
            {
                logPanel?.AddEntry("Player ended turn");
                EventBus.Publish(new EndTurnRequestedEvent());
            }
        }

        private void OnCardButtonClicked(CardData card, int handIndex)
        {
            GameLogger.LogInfo("Card", $"OnCardButtonClicked: '{card?.CardName}' [handIndex={handIndex}]  battleManager={(battleManager != null ? "set" : "null")}  IsPlayerTurn={battleManager?.IsPlayerTurn}");
            if (battleManager != null && battleManager.IsPlayerTurn)
            {
                GameLogger.LogInfo("Card", $"Publishing PlayCardRequestedEvent for '{card?.CardName}'");
                EventBus.Publish(new PlayCardRequestedEvent { Card = card, HandIndex = handIndex });
            }
            else
            {
                GameLogger.LogWarning("Card", $"Card play blocked in BattleUI — battleManager={(battleManager != null ? "set" : "null")}  IsPlayerTurn={battleManager?.IsPlayerTurn}");
            }
        }


        /// <summary>
        /// Fired by <see cref="BattleResultPanel.OnContinueClicked"/> after a victory.
        /// Generates a reward offer and opens the reward screen.
        /// On defeat (or if reward infrastructure isn't set up yet) clears the run and reloads.
        /// </summary>
        private void OnResultContinueClicked()
        {
            if (!_lastBattleResult.isVictory || _cardDatabase == null || _rewardScreen == null)
            {
                // Defeat — wipe RunState so the next scene load starts a fresh run.
                RunState.Clear();
                SceneLoader.Instance?.ReloadCurrentScene();
                return;
            }

            var offers = _cardDatabase.GenerateRewardOffer(battleManager.PlayerOrigin, count: 3);
            _rewardScreen.Open(offers, OnRewardChosen);
        }

        /// <summary>
        /// Callback from <see cref="RewardScreen"/> once the player picks a card (or skips).
        /// Adds the card to <see cref="RunState.Current"/>, advances the session battle index,
        /// and reloads the scene. Clears RunState when the session is fully complete.
        /// </summary>
        private void OnRewardChosen(CardData picked)
        {
            if (picked != null)
                RunState.Current?.AddCardToDeck(picked);

            if (RunState.Current?.HasNextBattle == true)
            {
                // More battles remain — advance the index and reload into the next fight.
                RunState.Current.AdvanceToNextBattle();
                SceneLoader.Instance?.ReloadCurrentScene();
            }
            else
            {
                // Session complete (or no session). Clear RunState and restart for playtesting.
                Debug.Log("[BattleUI] Run complete! Clearing RunState — next scene load starts fresh.");
                RunState.Clear();
                SceneLoader.Instance?.ReloadCurrentScene();
            }
        }

        #endregion

        #region Card Zone Viewers

        private void ShowDiscardZone()
        {
            if (cardZonePanel == null || battleManager?.PlayerDeck == null) return;
            cardZonePanel.Open("Discard Pile", battleManager.PlayerDeck.DiscardPile);
        }

        private void ShowExhaustZone()
        {
            if (cardZonePanel == null || battleManager?.PlayerDeck == null) return;
            cardZonePanel.Open("Exhaust Pile", battleManager.PlayerDeck.ExhaustPile);
        }

        private void ShowDeckZone()
        {
            if (cardZonePanel == null || battleManager?.PlayerDeck == null) return;

            // Shuffle display copy — don't reveal the real draw order.
            var display = new List<CardData>(battleManager.PlayerDeck.DrawPile);
            for (int i = display.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (display[i], display[j]) = (display[j], display[i]);
            }
            cardZonePanel.Open("Draw Pile", display);
        }

        #endregion

        // ══════════════════════════════════════════════════════════════════════
        // FSM State Classes
        // Pattern mirrors BattleManager's inner state classes.
        // Each receives `this` (the BattleUI) in its constructor so it can
        // access all panels and the BattleManager without extra coupling.
        // ══════════════════════════════════════════════════════════════════════

        #region State: Idle

        private sealed class IdleBattleUIState : State
        {
            private readonly BattleUI _ui;
            public IdleBattleUIState(BattleUI ui) => _ui = ui;

            public override void OnEnter()
            {
                _ui.handPanel?.ClearHand();
                if (_ui.endTurnButton  != null) _ui.endTurnButton.interactable = false;
                if (_ui.improviseButton != null) _ui.improviseButton.gameObject.SetActive(false);
                _ui.UpdateStatsDisplay();
            }
        }

        #endregion

        #region State: PlayerTurn

        private sealed class PlayerTurnBattleUIState : State
        {
            private readonly BattleUI _ui;
            public PlayerTurnBattleUIState(BattleUI ui) => _ui = ui;

            public override void OnEnter()
            {
                _ui.handPanel?.RefreshNormalHand(_ui.battleManager, _ui.OnCardButtonClicked);
                if (_ui.endTurnButton != null) _ui.endTurnButton.interactable = true;

                _ui.UpdateStatsDisplay();
                _ui.UpdateBattleInfo();
            }
        }

        #endregion

        #region State: WaitingForCardChoice

        private sealed class WaitingForCardChoiceBattleUIState : State
        {
            private readonly BattleUI _ui;
            public WaitingForCardChoiceBattleUIState(BattleUI ui) => _ui = ui;

            public override void OnEnter()
            {
                var evt = _ui._pendingCardChoice;
                if (evt == null)
                {
                    _ui._fsm?.ChangeState(BattleUIState.PlayerTurn);
                    return;
                }

                // Disable end-turn so the player can't skip out of the choice
                if (_ui.endTurnButton != null) _ui.endTurnButton.interactable = false;

                _ui.cardChoicePanel.Open(evt.Title, evt.Choices, evt.RequiredCount, OnConfirmed);
            }

            public override void OnExit()
            {
                _ui.cardChoicePanel?.Close();
                if (_ui.endTurnButton != null) _ui.endTurnButton.interactable = true;
            }

            private void OnConfirmed(List<CardData> selected)
            {
                _ui._pendingCardChoice?.OnConfirmed?.Invoke(selected);
                _ui._pendingCardChoice = null;
                _ui._fsm?.ChangeState(BattleUIState.PlayerTurn);
            }
        }

        #endregion

        #region State: BattleEnd

        private sealed class BattleEndBattleUIState : State
        {
            private readonly BattleUI _ui;
            public BattleEndBattleUIState(BattleUI ui) => _ui = ui;

            public override void OnEnter()
            {
                _ui.resultPanel?.Show(_ui._lastBattleResult.isVictory);
                _ui.handPanel?.ClearHand();
                if (_ui.endTurnButton   != null) _ui.endTurnButton.interactable = false;
                if (_ui.improviseButton != null) _ui.improviseButton.gameObject.SetActive(false);
                _ui.UpdateStatsDisplay();
            }
        }

        #endregion
    }
}
