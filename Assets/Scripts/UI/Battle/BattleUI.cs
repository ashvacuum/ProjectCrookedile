using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Data.Cards;
using Crookedile.Gameplay.Battle;
using Crookedile.Utilities;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [Tooltip("Self-subscribing enemy row — owns slot spawning and per-slot event reactions.")]
        [SerializeField]
        private EnemyRowPanel enemyRow;

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

        /// <summary>Anchor for feedback that targets the Opinion Meter (the real damage sink).</summary>
        public RectTransform MeterTransform =>
            _opinionMeterUI != null ? _opinionMeterUI.AnchorTransform : null;

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

        [Tooltip(
            "General-purpose interactive card picker for card-choice effects (ChooseFromDiscard, Upgrade, Retain, etc.)."
        )]
        [SerializeField]
        private CardPickerPanel cardChoicePanel;

        #endregion

        #region Card Zone Bar
        [Header("Card Zone Bar")]
        [Tooltip(
            "Self-subscribing zone bar — owns deck/discard/exhaust buttons, counts, and grant animations."
        )]
        [SerializeField]
        private CardZoneBar cardZoneBar;

        [Header("Post-battle")]
        [Tooltip("Owns reward offers and RunState progression after the result panel's Continue.")]
        [SerializeField]
        private PostBattleFlow postBattleFlow;

        #endregion

        #region Runtime
        private BattleManager battleManager;
        private BattleResult _lastBattleResult;
        private bool _cardChoiceActive;
        private CardChoiceRequestedEvent _pendingCardChoice;
        private Sequence _battleInfoFadeSeq;

        /// <summary>One-frame coalescing flag — see <see cref="RequestStatsRefresh"/>.</summary>
        private bool _statsRefreshQueued;

        /// <summary>Unsubscribe actions collected by <see cref="Sub{T}"/>; run on disable.</summary>
        private readonly List<System.Action> _eventUnsubscribers = new List<System.Action>();

        #endregion

        #region Initialization

        private void Awake()
        {
            if (endTurnButton != null)
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
        }

        private void OnEnable() => SubscribeToEvents();

        private void OnDisable() => UnsubscribeFromEvents();

        /// <summary>
        /// Subscribes <paramref name="handler"/> and records the matching unsubscribe so
        /// <see cref="UnsubscribeFromEvents"/> can't drift out of sync with this list.
        /// </summary>
        private void Sub<T>(System.Action<T> handler)
            where T : IGameEvent
        {
            EventBus.Subscribe(handler);
            _eventUnsubscribers.Add(() => EventBus.Unsubscribe(handler));
        }

        private void SubscribeToEvents()
        {
            Sub<BattleStateChangedEvent>(OnBattleStateChanged);
            Sub<BattleStartedEvent>(OnBattleStarted);
            Sub<CardPlayedEvent>(OnCardPlayed);
            Sub<BattleEndedEvent>(OnBattleEnded);
            Sub<EnemySummonedEvent>(OnEnemySummoned);
            Sub<CardChoiceRequestedEvent>(OnCardChoiceRequested);
            Sub<SupportChangedEvent>(OnSupportChanged);
            Sub<DenialChangedEvent>(OnDenialChanged);
            Sub<StatusEffectAppliedEvent>(OnStatusEffectApplied);
            Sub<OpinionChangedEvent>(OnOpinionChanged);
            Sub<TurnLimitUpdatedEvent>(OnTurnLimitUpdated);
        }

        private void UnsubscribeFromEvents()
        {
            foreach (var unsub in _eventUnsubscribers)
                unsub();
            _eventUnsubscribers.Clear();
        }

        /// <summary>
        /// Called by BattleTestStarter once the battle is ready.
        /// Wires zone viewers and result panel; the UI configures itself once
        /// <see cref="BattleStateChangedEvent"/> fires from BattleManager.
        /// </summary>
        public void Initialize(BattleManager manager)
        {
            battleManager = manager;

            // Self-subscribing panels get their battle context here.
            logPanel?.Bind(manager);
            handPanel?.Bind(manager, OnCardButtonClicked);
            enemyRow?.Bind(manager);
            cardZoneBar?.Bind(manager);
            postBattleFlow?.Bind(manager);

            // One-shot wiring report — flags any panel ref left unassigned on this BattleUI.
            GameLogger.LogInfo(
                "BattleUI",
                "Panel wiring — "
                    + $"hand:{(handPanel != null ? "ok" : "MISSING")} "
                    + $"log:{(logPanel != null ? "ok" : "MISSING")} "
                    + $"enemyRow:{(enemyRow != null ? "ok" : "MISSING")} "
                    + $"zoneBar:{(cardZoneBar != null ? "ok" : "MISSING")} "
                    + $"postBattle:{(postBattleFlow != null ? "ok" : "MISSING")}"
            );

            RequestStatsRefresh();
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
                    RequestStatsRefresh();
                    break;

                case BattleState.TurnStart:
                    handPanel?.ClearHand();
                    if (endTurnButton != null)
                        endTurnButton.interactable = false;
                    RequestStatsRefresh();
                    break;

                case BattleState.PlayerTurn:
                    handPanel?.RequestHandRefresh();
                    if (endTurnButton != null)
                        endTurnButton.interactable = !_cardChoiceActive;
                    RequestStatsRefresh();
                    UpdateBattleInfo();
                    break;

                case BattleState.OpponentTurn:
                    handPanel?.DiscardHandAnimated();
                    if (endTurnButton != null)
                        endTurnButton.interactable = false;
                    UpdateBattleInfo();
                    break;

                case BattleState.TurnEnd:
                    if (endTurnButton != null)
                        endTurnButton.interactable = false;
                    RequestStatsRefresh();
                    break;

                case BattleState.BattleEnd:
                    handPanel?.ClearHand();
                    if (endTurnButton != null)
                        endTurnButton.interactable = false;
                    resultPanel?.Show(_lastBattleResult.isVictory);
                    RequestStatsRefresh();
                    break;
            }
        }

        private void OnBattleStarted(BattleStartedEvent evt)
        {
            _playerSlotUI?.Initialize(battleManager, evt.Setup.GetPlayerPortrait());
            // Enemy slots build themselves — EnemyRowPanel subscribes to BattleStartedEvent.
            RequestStatsRefresh();
        }

        private void OnCardPlayed(CardPlayedEvent evt)
        {
            // Narration → BattleLogPanel; hand choreography → HandPanel (both self-subscribe).
            RequestStatsRefresh();
        }

        private void OnEnemySummoned(EnemySummonedEvent evt)
        {
            // Slot spawn handled by EnemyRowPanel; this just repaints stats/focus.
            RequestStatsRefresh();
        }

        private void OnBattleEnded(BattleEndedEvent evt)
        {
            // Kept only for resultPanel.Show on BattleState.BattleEnd; run progression
            // (RunState victory record, rewards, reload) lives in PostBattleFlow.
            _lastBattleResult = evt.Result;
        }

        private void OnCardChoiceRequested(CardChoiceRequestedEvent evt)
        {
            _pendingCardChoice = evt;
            _cardChoiceActive = true;
            if (endTurnButton != null)
                endTurnButton.interactable = false;
            cardChoicePanel?.Open(
                evt.Title,
                evt.Choices,
                minCount: evt.AllowFewer ? 0 : evt.RequiredCount,
                maxCount: evt.RequiredCount,
                confirmLabel: "Confirm",
                OnCardChoiceConfirmed
            );
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

        private void OnSupportChanged(SupportChangedEvent evt) => RequestStatsRefresh();

        private void OnDenialChanged(DenialChangedEvent evt) => RequestStatsRefresh();

        private void OnStatusEffectApplied(StatusEffectAppliedEvent evt) => RequestStatsRefresh();

        private void OnOpinionChanged(OpinionChangedEvent evt) => RequestStatsRefresh();

        private void OnTurnLimitUpdated(TurnLimitUpdatedEvent evt) => RequestStatsRefresh();

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

        #endregion

        #region UI Updates (stats + battle-info text; kept in BattleUI for direct field access)

        /// <summary>
        /// Queues a stats refresh for the end of this frame. Several events often land in
        /// one resolution (damage + status + AP); coalescing in LateUpdate runs the full
        /// refresh — and its bar tweens — once instead of once per event.
        /// </summary>
        internal void RequestStatsRefresh() => _statsRefreshQueued = true;

        private void LateUpdate()
        {
            if (!_statsRefreshQueued)
                return;
            _statsRefreshQueued = false;
            UpdateStatsDisplay();
        }

        private void UpdateStatsDisplay()
        {
            if (battleManager == null)
                return;

            _playerSlotUI?.Refresh();

            // Enemy slots — refresh + focus highlight (owned by EnemyRowPanel)
            enemyRow?.RefreshAll(battleManager.FocusedEnemyIndex);

            // Card zone counts (owned by CardZoneBar)
            cardZoneBar?.RefreshCounts();

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
                phaseText.text = battleManager.IsPlayerTurn ? "Your Turn" : "Opponent's Turn";
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

        /// <summary>
        /// Returns the <see cref="RectTransform"/> of the enemy slot at the given index,
        /// or null if the index is out of range or the slot has been destroyed.
        /// Used by <see cref="BattleFeedbackController"/> to aim VFX at specific enemy panels.
        /// </summary>
        public RectTransform GetEnemySlotTransform(int index) => enemyRow?.GetSlotTransform(index);

        #endregion

        #region Input Handlers

        private void OnEndTurnClicked()
        {
            if (battleManager != null && battleManager.IsPlayerTurn)
            {
                logPanel?.AddEntry("Player ended turn");
                battleManager.RequestEndTurn();
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
                GameLogger.LogInfo("Card", $"Requesting card play for '{card?.CardName}'");
                battleManager.RequestPlayCard(card, handIndex);
            }
            else
            {
                GameLogger.LogWarning(
                    "Card",
                    $"Card play blocked in BattleUI — battleManager={(battleManager != null ? "set" : "null")}  IsPlayerTurn={battleManager?.IsPlayerTurn}"
                );
            }
        }

        #endregion
    }
}
