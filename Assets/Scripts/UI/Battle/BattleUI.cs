using System.Collections;
using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Gameplay.Battle;
using Crookedile.UI.Reward;
using Crookedile.Utilities;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
    /// Structural UI changes are driven by <see cref="BattleStateChangedEvent"/> via <c>ConfigureForBattleState</c>.
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        #region Panels (extracted subsystems)
        [Header("Panels")]
        [Tooltip("Manages card hand display and object pool.")]
        [SerializeField]
        private HandPanel handPanel;

        [Tooltip("Battle log text + auto-scroll.")]
        [SerializeField]
        private BattleLogPanel logPanel;

        [Tooltip("Victory / defeat result panels.")]
        [SerializeField]
        private BattleResultPanel resultPanel;

        #endregion

        #region Enemy Slots
        [Header("Enemy Slots")]
        [Tooltip("Parent transform that enemy slot panels are spawned into.")]
        [SerializeField]
        private Transform enemySlotContainer;

        [Tooltip("Prefab with an EnemySlotUI component — instantiated once per enemy.")]
        [SerializeField]
        private GameObject enemySlotPrefab;

        #endregion

        #region VFX Anchors
        [Header("VFX Anchors")]
        [Tooltip(
            "RectTransform of the player stats panel — fallback VFX target when PlayerSlotUI is not assigned."
        )]
        [field: SerializeField]
        public RectTransform PlayerStatsPanel { get; private set; }

        #endregion

        #region Player Slot
        [Header("Player Slot")]
        [Tooltip(
            "PlayerSlotUI instance in the scene. Provides the portrait, health bar, and VFX anchor for player-targeted effects."
        )]
        [SerializeField]
        private PlayerSlotUI _playerSlotUI;

        /// <summary>
        /// RectTransform anchor at the player slot — preferred target for VFX and floating damage numbers.
        /// Falls back to <see cref="PlayerStatsPanel"/> when no slot is assigned.
        /// </summary>
        public RectTransform PlayerSlotTransform =>
            _playerSlotUI != null ? _playerSlotUI.SlotRect : PlayerStatsPanel;

        #endregion

        #region Opinion Meter
        [Header("Opinion Meter")]
        [Tooltip(
            "OpinionMeterUI instance in the scene — shows the shared opinion bar and turn countdown."
        )]
        [SerializeField]
        private OpinionMeterUI _opinionMeterUI;

        #endregion

        #region Battle Info
        [Header("Battle Info")]
        [SerializeField]
        private TMP_Text turnNumberText;

        [SerializeField]
        private TMP_Text phaseText;

        [Tooltip("Seconds the turn/phase label stays fully visible before fading.")]
        [SerializeField]
        private float _battleInfoHoldTime = 1.5f;

        [Tooltip("Seconds the fade-out takes after the hold delay.")]
        [SerializeField]
        private float _battleInfoFadeTime = 0.5f;

        #endregion

        #region Controls
        [Header("Controls")]
        [SerializeField]
        private Button endTurnButton;

        [Tooltip("Actor passive — shown on the Actor's first player turn only.")]
        [SerializeField]
        private Button improviseButton;

        [Tooltip("Card selection modal shared by Improvise and ChooseFromDiscard effects.")]
        [SerializeField]
        private CardSelectionPanel cardSelectionPanel;

        [Tooltip(
            "General-purpose interactive card picker for card-choice effects (ChooseFromDiscard, Upgrade, Retain, etc.)."
        )]
        [SerializeField]
        private CardChoicePanel cardChoicePanel;

        #endregion

        #region Card Zone Buttons
        [Header("Card Zone Buttons")]
        [SerializeField]
        private Button discardZoneButton;

        [SerializeField]
        private Button exhaustZoneButton;

        [SerializeField]
        private Button deckZoneButton;

        [SerializeField]
        private TMP_Text discardCountText;

        [SerializeField]
        private TMP_Text exhaustCountText;

        [SerializeField]
        private TMP_Text deckCountText;

        [Header("Card Zone Panel")]
        [SerializeField]
        private CardZonePanel cardZonePanel;

        [Header("Reward")]
        [Tooltip("CardDatabase ScriptableObject used to generate post-battle card offers.")]
        [SerializeField]
        private CardDatabase _cardDatabase;

        [Tooltip(
            "Reward screen overlay panel (starts inactive). Shown after a victory Continue click."
        )]
        [SerializeField]
        private RewardScreen _rewardScreen;

        [Header("Card Grant Animation")]
        [Tooltip("Seconds for the zone count text to scale up on card grant arrival.")]
        [SerializeField]
        private float _countPunchDuration = 0.25f;

        [Tooltip("Scale multiplier applied to the count text at the peak of the punch.")]
        [SerializeField]
        private float _countPunchScale = 1.4f;

        #endregion

        #region Runtime
        private BattleManager battleManager;
        private BattleResult _lastBattleResult;
        private bool _cardChoiceActive;
        private List<EnemySlotUI> _enemySlots = new List<EnemySlotUI>();
        private CardChoiceRequestedEvent _pendingCardChoice;
        private bool _handRefreshPending;

        /// <summary>Card button extracted from hand on CardPlayedEvent, waiting for VFX to finish before animating to discard.</summary>
        private CardButton _pendingDiscardButton;

        private HashSet<CardData> _pendingDrawnCards = new HashSet<CardData>();
        private Sequence _battleInfoFadeSeq;

        #region Initialization

        private void Awake()
        {
            if (endTurnButton != null)
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
        }

        private void OnEnable() => SubscribeToEvents();

        private void OnDisable() => UnsubscribeFromEvents();

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<BattleStateChangedEvent>(OnBattleStateChanged);
            EventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<TurnEndedEvent>(OnTurnEnded);
            EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Subscribe<BattleEndedEvent>(OnBattleEnded);
            EventBus.Subscribe<EnemyIntentDeclaredEvent>(OnEnemyIntentDeclared);
            EventBus.Subscribe<HostilityChangedEvent>(OnHostilityChanged);
            EventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            EventBus.Subscribe<EnemySummonedEvent>(OnEnemySummoned);
            EventBus.Subscribe<CardChoiceRequestedEvent>(OnCardChoiceRequested);
            EventBus.Subscribe<SupportChangedEvent>(OnSupportChanged);
            EventBus.Subscribe<DenialChangedEvent>(OnDenialChanged);
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Subscribe<EnemyActingEvent>(OnEnemyActing);
            EventBus.Subscribe<CardDrawnEvent>(OnCardDrawn);
            EventBus.Subscribe<StatusEffectAppliedEvent>(OnStatusEffectApplied);
            EventBus.Subscribe<CardVFXCompleteEvent>(OnCardVFXComplete);
            EventBus.Subscribe<CardGrantedEvent>(OnCardGranted);
            EventBus.Subscribe<CardExhaustedEvent>(OnCardExhausted);
            EventBus.Subscribe<OpinionChangedEvent>(OnOpinionChanged);
            EventBus.Subscribe<TurnLimitUpdatedEvent>(OnTurnLimitUpdated);
            EventBus.Subscribe<JudgmentEvent>(OnJudgment);
            EventBus.Subscribe<EnemySkippedTurnEvent>(OnEnemySkippedTurn);
            EventBus.Subscribe<EchoChamberChangedEvent>(OnEchoChamberChanged);
            EventBus.Subscribe<EnemyTurncoatEvent>(OnEnemyTurncoat);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<BattleStateChangedEvent>(OnBattleStateChanged);
            EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<TurnEndedEvent>(OnTurnEnded);
            EventBus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
            EventBus.Unsubscribe<EnemyIntentDeclaredEvent>(OnEnemyIntentDeclared);
            EventBus.Unsubscribe<HostilityChangedEvent>(OnHostilityChanged);
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            EventBus.Unsubscribe<EnemySummonedEvent>(OnEnemySummoned);
            EventBus.Unsubscribe<CardChoiceRequestedEvent>(OnCardChoiceRequested);
            EventBus.Unsubscribe<SupportChangedEvent>(OnSupportChanged);
            EventBus.Unsubscribe<DenialChangedEvent>(OnDenialChanged);
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Unsubscribe<EnemyActingEvent>(OnEnemyActing);
            EventBus.Unsubscribe<CardDrawnEvent>(OnCardDrawn);
            EventBus.Unsubscribe<StatusEffectAppliedEvent>(OnStatusEffectApplied);
            EventBus.Unsubscribe<CardVFXCompleteEvent>(OnCardVFXComplete);
            EventBus.Unsubscribe<CardGrantedEvent>(OnCardGranted);
            EventBus.Unsubscribe<CardExhaustedEvent>(OnCardExhausted);
            EventBus.Unsubscribe<OpinionChangedEvent>(OnOpinionChanged);
            EventBus.Unsubscribe<TurnLimitUpdatedEvent>(OnTurnLimitUpdated);
            EventBus.Unsubscribe<JudgmentEvent>(OnJudgment);
            EventBus.Unsubscribe<EnemySkippedTurnEvent>(OnEnemySkippedTurn);
            EventBus.Unsubscribe<EchoChamberChangedEvent>(OnEchoChamberChanged);
            EventBus.Unsubscribe<EnemyTurncoatEvent>(OnEnemyTurncoat);
        }

        /// <summary>
        /// Called by BattleTestStarter once the battle is ready.
        /// Wires zone viewers and result panel; the UI configures itself once
        /// <see cref="BattleStateChangedEvent"/> fires from BattleManager.
        /// </summary>
        public void Initialize(BattleManager manager)
        {
            battleManager = manager;

            discardZoneButton?.onClick.AddListener(ShowDiscardZone);
            exhaustZoneButton?.onClick.AddListener(ShowExhaustZone);
            deckZoneButton?.onClick.AddListener(ShowDeckZone);

            if (resultPanel != null)
                resultPanel.OnContinueClicked += OnResultContinueClicked;

            UpdateStatsDisplay();
        }

        #endregion

        #region Event Handlers

        private void OnBattleStateChanged(BattleStateChangedEvent evt)
        {
            ConfigureForBattleState(evt.Current);
        }

        private void ConfigureForBattleState(BattleState state)
        {
            switch (state)
            {
                case BattleState.Initialize:
                    UpdateStatsDisplay();
                    RefreshOpinionMeter();
                    break;

                case BattleState.TurnStart:
                    handPanel?.ClearHand();
                    if (endTurnButton != null)
                        endTurnButton.interactable = false;
                    UpdateStatsDisplay();
                    break;

                case BattleState.PlayerTurn:
                    handPanel?.RefreshNormalHand(battleManager, OnCardButtonClicked);
                    if (endTurnButton != null)
                        endTurnButton.interactable = !_cardChoiceActive;
                    UpdateStatsDisplay();
                    UpdateBattleInfo();
                    break;

                case BattleState.OpponentTurn:
                    handPanel?.ClearHand();
                    if (endTurnButton != null)
                        endTurnButton.interactable = false;
                    if (improviseButton != null)
                        improviseButton.gameObject.SetActive(false);
                    UpdateBattleInfo();
                    break;

                case BattleState.TurnEnd:
                    if (endTurnButton != null)
                        endTurnButton.interactable = false;
                    UpdateStatsDisplay();
                    break;

                case BattleState.BattleEnd:
                    handPanel?.ClearHand();
                    if (endTurnButton != null)
                        endTurnButton.interactable = false;
                    if (improviseButton != null)
                        improviseButton.gameObject.SetActive(false);
                    resultPanel?.Show(_lastBattleResult.isVictory);
                    UpdateStatsDisplay();
                    break;
            }
        }

        private void OnBattleStarted(BattleStartedEvent evt)
        {
            logPanel?.AddEntry("=== Battle Started ===");
            _playerSlotUI?.Initialize(battleManager, evt.Setup.GetPlayerStats().portrait);
            BuildEnemySlots();
            UpdateStatsDisplay();
            RefreshOpinionMeter();
        }

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            string owner = evt.IsPlayerTurn ? "Player" : "Opponent";
            logPanel?.AddEntry($"--- Turn {evt.TurnNumber}: {owner} ---");
        }

        private void OnTurnEnded(TurnEndedEvent evt)
        {
            // Data-only — structural changes handled by BattleStateChangedEvent.
        }

        private void OnCardPlayed(CardPlayedEvent evt)
        {
            logPanel?.AddEntry(
                $"{(evt.IsPlayer ? "Player" : "Opponent")} played: {evt.Card.CardName}"
            );
            UpdateStatsDisplay();

            if (evt.IsPlayer)
            {
                // Extract the card from hand immediately so the layout closes the gap,
                // but hold it — the discard animation fires in OnCardVFXComplete so the
                // sequence is: VFX resolves → card flies to discard → new draws appear.
                GameLogger.LogInfo(
                    "Card",
                    $"Extracted '{evt.Card.CardName}' from hand — awaiting VFX complete before discard"
                );
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
            GameLogger.LogInfo(
                "Card",
                $"CardVFXComplete for '{evt.Card?.CardName}' — starting discard animation"
            );

            if (_pendingDiscardButton != null)
            {
                var btn = _pendingDiscardButton;
                _pendingDiscardButton = null;

                CardFlyAnimator.Instance?.AnimateDiscardOut(
                    btn,
                    () =>
                    {
                        GameLogger.LogInfo(
                            "Card",
                            $"Discard animation done for '{btn.CardData?.CardName}' — returning to pool and refreshing hand"
                        );
                        BattlePoolManager.Instance?.ReturnCard(btn);

                        // Refresh hand AFTER discard so any drawn cards appear once the discard lands.
                        if (!_handRefreshPending)
                        {
                            _handRefreshPending = true;
                            StartCoroutine(RefreshHandNextFrame());
                        }
                    }
                );
            }
            else
            {
                // No card to discard (no-VFX card that was already handled, or edge case).
                GameLogger.LogWarning(
                    "Card",
                    $"CardVFXComplete for '{evt.Card?.CardName}' but no pending discard button found"
                );
                if (!_handRefreshPending)
                {
                    _handRefreshPending = true;
                    StartCoroutine(RefreshHandNextFrame());
                }
            }
        }

        private void OnCardDrawn(CardDrawnEvent evt)
        {
            if (!evt.IsPlayer)
                return; // enemy draws don't affect the player's hand panel
            _pendingDrawnCards.Add(evt.Card); // track which cards are new this batch
            if (_handRefreshPending)
                return; // coroutine already running — just add to batch
            _handRefreshPending = true;
            StartCoroutine(RefreshHandNextFrame());
        }

        private void OnCardGranted(CardGrantedEvent evt)
        {
            if (!evt.IsPlayer)
                return;
            Transform target = evt.ToDiscard
                ? discardZoneButton.transform
                : deckZoneButton.transform;
            TMP_Text counter = evt.ToDiscard ? discardCountText : deckCountText;
            StartCoroutine(CardGrantedAnimationSequence(evt.Card, target, counter));
        }

        private void OnCardExhausted(CardExhaustedEvent evt)
        {
            // Ensure exhaust count is always up-to-date regardless of trigger source
            // (ExhaustFromDiscard does not go through CardPlayedEvent → UpdateStatsDisplay).
            if (!evt.IsPlayer)
                return;
            UpdateStatsDisplay();
        }

        private IEnumerator RefreshHandNextFrame()
        {
            yield return null; // wait one frame so all draw events from one effect batch together
            _handRefreshPending = false;
            var drawn =
                _pendingDrawnCards.Count > 0 ? new HashSet<CardData>(_pendingDrawnCards) : null;
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
        private IEnumerator CardGrantedAnimationSequence(
            CardData card,
            Transform targetZone,
            TMP_Text countText
        )
        {
            var btn = BattlePoolManager.Instance?.RentCard(card.CardType, transform);
            if (btn == null)
            {
                UpdateStatsDisplay();
                yield break;
            }

            int ap = battleManager?.PlayerStats.CurrentActionPoints ?? 0;
            int cost = battleManager?.GetEffectiveCardCost(card) ?? 1;
            btn.Initialize(card, 0, ap, cost, forceUnplayable: true);

            bool arrived = false;
            CardFlyAnimator.Instance?.AnimateCardGranted(
                btn,
                targetZone,
                () =>
                {
                    UpdateStatsDisplay();
                    PunchCountText(countText);
                    BattlePoolManager.Instance?.ReturnCard(btn);
                    arrived = true;
                }
            );

            yield return new WaitUntil(() => arrived);
        }

        private void PunchCountText(TMP_Text text)
        {
            if (text == null)
                return;
            text.transform.DOKill();
            text.transform.DOScale(Vector3.one * _countPunchScale, _countPunchDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                    text
                        .transform.DOScale(Vector3.one, _countPunchDuration * 0.5f)
                        .SetEase(Ease.InQuad)
                )
                .SetLink(gameObject);
        }

        private void OnEnemyIntentDeclared(EnemyIntentDeclaredEvent evt)
        {
            if (evt.Move != null)
                logPanel?.AddEntry(
                    $"Enemy [{evt.EnemyIndex}] intends: {evt.Move.IntentDescription}"
                );
            if (evt.EnemyIndex < _enemySlots.Count)
                _enemySlots[evt.EnemyIndex]?.UpdateIntent(evt.Move);
        }

        private void OnHostilityChanged(HostilityChangedEvent evt)
        {
            // Player hostility (index -1) has no slot; only refresh real enemy slots.
            if (evt.EnemyIndex < 0 || evt.EnemyIndex >= _enemySlots.Count)
                return;
            _enemySlots[evt.EnemyIndex]?.Refresh();
            _enemySlots[evt.EnemyIndex]?.PulseHostility();
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            logPanel?.AddEntry($"{evt.EnemyName} defeated!");
            if (evt.EnemyIndex >= _enemySlots.Count)
                return;

            var slot = _enemySlots[evt.EnemyIndex];
            if (slot == null)
                return;

            if (BattlePoolManager.Instance != null)
                BattlePoolManager.Instance.ReturnSlot(slot);
            else
                Destroy(slot.gameObject);

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

            // Victory: update RunState so the next battle knows this one was won.
            if (evt.Result.isVictory)
                RunState.Current?.RecordBattleVictory();
            // Structural UI change (result panel, hand clear) handled by BattleStateChangedEvent → BattleState.BattleEnd.
        }

        private void OnCardChoiceRequested(CardChoiceRequestedEvent evt)
        {
            _pendingCardChoice = evt;
            _cardChoiceActive = true;
            if (endTurnButton != null)
                endTurnButton.interactable = false;
            cardChoicePanel?.Open(evt.Title, evt.Choices, evt.RequiredCount, OnCardChoiceConfirmed);
        }

        private void OnCardChoiceConfirmed(List<CardData> selected)
        {
            _pendingCardChoice?.OnConfirmed?.Invoke(selected);
            _pendingCardChoice = null;
            _cardChoiceActive = false;
            cardChoicePanel?.Close();
            if (endTurnButton != null)
                endTurnButton.interactable = true;
        }

        private void OnSupportChanged(SupportChangedEvent evt) => RefreshOpinionMeter();

        private void OnDenialChanged(DenialChangedEvent evt) => RefreshOpinionMeter();

        private void OnStatusEffectApplied(StatusEffectAppliedEvent evt)
        {
            UpdateStatsDisplay();

            // When Stunned is applied to an enemy, immediately hide their intent display.
            // The enemy can't act this turn, so showing a planned move would be misleading.
            if (evt.StatusType == StatusEffectType.Stunned && !evt.IsToPlayer && evt.Stacks > 0)
            {
                for (int i = 0; i < _enemySlots.Count; i++)
                {
                    if (
                        battleManager != null
                        && i < battleManager.Enemies.Count
                        && battleManager
                            .Enemies[i]
                            .StatusEffects.HasEffect(StatusEffectType.Stunned)
                    )
                        _enemySlots[i]?.ClearIntent();
                }
            }
        }

        private void OnDamageDealt(DamageDealtEvent evt)
        {
            if (!evt.IsToPlayer)
                return;
            logPanel?.AddEntry($"{evt.AttackerName} dealt {evt.Amount} damage");
        }

        private void OnOpinionChanged(OpinionChangedEvent evt)
        {
            RefreshOpinionMeter();
        }

        private void OnTurnLimitUpdated(TurnLimitUpdatedEvent evt)
        {
            RefreshOpinionMeter();
        }

        private void OnJudgment(JudgmentEvent evt)
        {
            string outcome = evt.IsVictory ? "VICTORY" : "DEFEAT";
            logPanel?.AddEntry(
                $"=== JUDGMENT: Opinion {evt.FinalOpinion} / {evt.Threshold * 2} — {outcome} ==="
            );
        }

        private void OnEnemySkippedTurn(EnemySkippedTurnEvent evt)
        {
            logPanel?.AddEntry($"{evt.EnemyName} held back this turn.");
        }

        private void OnEchoChamberChanged(EchoChamberChangedEvent evt)
        {
            logPanel?.AddEntry(
                evt.Active
                    ? "Echo chamber! The room agrees with you — opinion gains are halved and your lead will bleed. Provoke someone."
                    : "Echo chamber broken — the room has a dissenter again."
            );
        }

        private void OnEnemyTurncoat(EnemyTurncoatEvent evt)
        {
            string name =
                battleManager != null
                && evt.EnemyIndex >= 0
                && evt.EnemyIndex < battleManager.Enemies.Count
                    ? battleManager.Enemies[evt.EnemyIndex].EnemyData.EnemyName
                    : "An ally";
            logPanel?.AddEntry($"{name} turned on you! They'll hit harder for a turn.");
            if (evt.EnemyIndex >= 0 && evt.EnemyIndex < _enemySlots.Count)
            {
                _enemySlots[evt.EnemyIndex]?.Refresh();
                _enemySlots[evt.EnemyIndex]?.PulseHostility();
            }
        }

        private void RefreshOpinionMeter()
        {
            if (_opinionMeterUI == null || battleManager == null)
                return;
            _opinionMeterUI.Refresh(
                battleManager.CurrentOpinion,
                battleManager.MaxOpinion,
                battleManager.PlayerTurnsElapsed,
                battleManager.MaxTurns,
                battleManager.CurrentSupport,
                battleManager.CurrentDenial
            );
        }

        private void OnEnemyActing(EnemyActingEvent evt)
        {
            if (evt.EnemyIndex >= _enemySlots.Count)
                return;
            _enemySlots[evt.EnemyIndex]?.PulseIntent();
            _enemySlots[evt.EnemyIndex]?.ClearIntent();
        }

        #endregion

        #region UI Updates (stats + battle-info text; kept in BattleUI for direct field access)

        internal void UpdateStatsDisplay()
        {
            if (battleManager == null)
                return;

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
                if (discardCountText != null)
                    discardCountText.text = deck.DiscardCount.ToString();
                if (exhaustCountText != null)
                    exhaustCountText.text = deck.ExhaustCount.ToString();
                if (deckCountText != null)
                    deckCountText.text = deck.DeckCount.ToString();
            }

            // Refresh opinion meter so Support/Denial shields stay in sync.
            RefreshOpinionMeter();
        }

        internal void UpdateBattleInfo()
        {
            if (battleManager == null)
                return;

            if (turnNumberText != null)
            {
                turnNumberText.text = $"Turn: {battleManager.CurrentTurn}";
                turnNumberText.alpha = 1f;
            }

            if (phaseText != null)
            {
                string turnOwner = battleManager.IsPlayerTurn ? "Player" : "Opponent";
                phaseText.text = $"{battleManager.CurrentState} ({turnOwner})";
                phaseText.alpha = 1f;
            }

            // Restart the fade — cancel any in-progress fade so the text shows fully first.
            _battleInfoFadeSeq?.Kill();
            _battleInfoFadeSeq = DOTween
                .Sequence()
                .SetLink(gameObject)
                .AppendInterval(_battleInfoHoldTime)
                .AppendCallback(() =>
                {
                    if (turnNumberText != null)
                        DOTween
                            .To(
                                () => turnNumberText.alpha,
                                x => turnNumberText.alpha = x,
                                0f,
                                _battleInfoFadeTime
                            )
                            .SetLink(gameObject);
                    if (phaseText != null)
                        DOTween
                            .To(
                                () => phaseText.alpha,
                                x => phaseText.alpha = x,
                                0f,
                                _battleInfoFadeTime
                            )
                            .SetLink(gameObject);
                    _battleInfoFadeSeq = null;
                });
        }

        #endregion

        #region Enemy Slots

        private void BuildEnemySlots()
        {
            // Return all current slots to the pool (or destroy if no pool).
            foreach (var slot in _enemySlots)
            {
                if (slot == null)
                    continue;
                if (BattlePoolManager.Instance != null)
                    BattlePoolManager.Instance.ReturnSlot(slot);
                else
                    Destroy(slot.gameObject);
            }

            _enemySlots.Clear();

            if (enemySlotContainer == null || battleManager == null)
                return;
            if (BattlePoolManager.Instance == null && enemySlotPrefab == null)
                return;

            for (int i = 0; i < battleManager.Enemies.Count; i++)
            {
                EnemySlotUI slot =
                    BattlePoolManager.Instance != null
                        ? BattlePoolManager.Instance.RentSlot(enemySlotContainer)
                        : Instantiate(enemySlotPrefab, enemySlotContainer)
                            .GetComponent<EnemySlotUI>();

                if (slot != null)
                {
                    slot.Initialize(
                        i,
                        battleManager,
                        battleManager.PlayerOrigin,
                        battleManager.Enemies[i].EnemyData
                    );
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
            if (enemySlotContainer == null || battleManager == null)
                return;
            if (BattlePoolManager.Instance == null && enemySlotPrefab == null)
                return;
            if (index >= battleManager.Enemies.Count)
                return;

            EnemySlotUI slot =
                BattlePoolManager.Instance != null
                    ? BattlePoolManager.Instance.RentSlot(enemySlotContainer)
                    : Instantiate(enemySlotPrefab, enemySlotContainer).GetComponent<EnemySlotUI>();

            if (slot != null)
            {
                slot.Initialize(
                    index,
                    battleManager,
                    battleManager.PlayerOrigin,
                    battleManager.Enemies[index].EnemyData
                );
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
            GameLogger.LogInfo(
                "Card",
                $"OnCardButtonClicked: '{card?.CardName}' [handIndex={handIndex}]  battleManager={(battleManager != null ? "set" : "null")}  IsPlayerTurn={battleManager?.IsPlayerTurn}"
            );
            if (battleManager != null && battleManager.IsPlayerTurn)
            {
                GameLogger.LogInfo(
                    "Card",
                    $"Publishing PlayCardRequestedEvent for '{card?.CardName}'"
                );
                EventBus.Publish(new PlayCardRequestedEvent { Card = card, HandIndex = handIndex });
            }
            else
            {
                GameLogger.LogWarning(
                    "Card",
                    $"Card play blocked in BattleUI — battleManager={(battleManager != null ? "set" : "null")}  IsPlayerTurn={battleManager?.IsPlayerTurn}"
                );
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
                Debug.Log(
                    "[BattleUI] Run complete! Clearing RunState — next scene load starts fresh."
                );
                RunState.Clear();
                SceneLoader.Instance?.ReloadCurrentScene();
            }
        }

        #endregion

        #region Card Zone Viewers

        private void ShowDiscardZone()
        {
            if (cardZonePanel == null || battleManager?.PlayerDeck == null)
                return;
            cardZonePanel.Open("Discard Pile", battleManager.PlayerDeck.DiscardPile);
        }

        private void ShowExhaustZone()
        {
            if (cardZonePanel == null || battleManager?.PlayerDeck == null)
                return;
            cardZonePanel.Open("Exhaust Pile", battleManager.PlayerDeck.ExhaustPile);
        }

        private void ShowDeckZone()
        {
            if (cardZonePanel == null || battleManager?.PlayerDeck == null)
                return;

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
    }
}
        #endregion
