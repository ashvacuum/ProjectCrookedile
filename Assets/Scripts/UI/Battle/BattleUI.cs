using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Data.Cards;
using Crookedile.Gameplay.Battle;
using Crookedile.Utilities;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// The battle screen's turn controls and its anchor directory. Every other panel wires and
    /// repaints itself (see <see cref="BattlePanel"/>); nothing is pushed from here.
    ///
    ///  - Turn controls: End Turn, Media Training, the turn/phase label.
    ///  - Card choices: answers <see cref="BattleManager.CardChoicePrompt"/> with the picker,
    ///    one at a time, and locks the turn controls while one is open.
    ///  - Anchors: where VFX and floating text aim (player slot, enemy slots, boss, meter).
    /// </summary>
    public class BattleUI : BattlePanel
    {
        #region Panels
        [Header("Panels")]
        [Tooltip("Battle log; records the player ending their turn.")]
        [SerializeField]
        [Required]
        private BattleLogPanel logPanel;

        #endregion

        #region Anchors
        [Header("Anchors")]
        [Tooltip("Enemy row; its slots are the VFX anchors for enemies.")]
        [SerializeField]
        [Required]
        private EnemyRowPanel enemyRow;

        [Tooltip("Separate rival podium. Hidden when the session has no boss.")]
        [SerializeField]
        private BossPanel bossPanel;

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

        public OpinionMeterUI OpinionMeter => _opinionMeterUI;

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
            "Optional authored Media Training button; when absent, one is created above End Turn."
        )]
        [SerializeField]
        private Button _mediaTrainingButton;
        private TMP_Text _mediaTrainingLabel;
        private int _displayedGlamourCost = -1;
        private int _displayedDrawCount = -1;

        [Tooltip(
            "General-purpose interactive card picker for card-choice effects (ChooseFromDiscard, Upgrade, Retain, etc.)."
        )]
        [SerializeField]
        private CardPickerPanel cardChoicePanel;

        #endregion

        #region Runtime
        private bool _cardChoiceActive;
        private CardChoiceRequest _pendingCardChoice;

        // Choices requested while another is open (several start-of-turn prompts at once) wait
        // here and open in request order.
        private readonly Queue<CardChoiceRequest> _queuedCardChoices =
            new Queue<CardChoiceRequest>();
        private Sequence _battleInfoFadeSeq;

        #endregion

        #region Initialization

        private void Awake()
        {
            if (endTurnButton != null)
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            InitializeMediaTrainingButton();
        }

        private void InitializeMediaTrainingButton()
        {
            if (_mediaTrainingButton == null && endTurnButton != null)
            {
                var go = new GameObject(
                    "MediaTrainingButton",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button)
                );
                var rect = (RectTransform)go.transform;
                var source = (RectTransform)endTurnButton.transform;
                rect.SetParent(source.parent, false);
                rect.anchorMin = source.anchorMin;
                rect.anchorMax = source.anchorMax;
                rect.pivot = source.pivot;
                rect.sizeDelta = new Vector2(
                    Mathf.Max(220, source.sizeDelta.x),
                    Mathf.Max(48, source.sizeDelta.y)
                );
                rect.anchoredPosition =
                    source.anchoredPosition + Vector2.up * (rect.sizeDelta.y + 12);
                go.GetComponent<Image>().color = new Color(0.18f, 0.28f, 0.38f, 1);
                _mediaTrainingButton = go.GetComponent<Button>();
                var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                label.transform.SetParent(rect, false);
                var labelRect = (RectTransform)label.transform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(8, 4);
                labelRect.offsetMax = new Vector2(-8, -4);
                _mediaTrainingLabel = label.GetComponent<TMP_Text>();
                var originalLabel = endTurnButton.GetComponentInChildren<TMP_Text>();
                if (originalLabel != null)
                    _mediaTrainingLabel.font = originalLabel.font;
                _mediaTrainingLabel.fontSize = 18;
                _mediaTrainingLabel.alignment = TextAlignmentOptions.Center;
                _mediaTrainingLabel.raycastTarget = false;
            }
            if (_mediaTrainingButton == null)
                return;

            _mediaTrainingLabel = _mediaTrainingButton.GetComponentInChildren<TMP_Text>();
            _mediaTrainingButton.onClick.AddListener(OnMediaTrainingClicked);
            _mediaTrainingButton.gameObject.SetActive(false);
        }

        private void OnMediaTrainingClicked()
        {
            if (!_cardChoiceActive && Battle != null)
                Battle.TryUseMediaTraining();
        }

        private void OnEnable()
        {
            // The battle may not have woken yet when this enables; BattleStartedEvent is published
            // before the first state runs, so the prompt is in place before any choice is asked.
            ClaimCardChoicePrompt();
            On<BattleStartedEvent>(_ => ClaimCardChoicePrompt());
            On<BattleStateChangedEvent>(OnBattleStateChanged);
            // A choice still waiting belongs to the finished battle.
            On<BattleEndedEvent>(_ => _queuedCardChoices.Clear());
            RefreshOnBattleChanges();
        }

        protected override void OnDisable()
        {
            if (Battle != null && Battle.CardChoicePrompt == OnCardChoiceRequested)
                Battle.CardChoicePrompt = null;
            base.OnDisable();
        }

        /// <summary>Takes the prompt only when nobody holds it: a test or playtest hook keeps its claim.</summary>
        private void ClaimCardChoicePrompt()
        {
            if (Battle != null)
                Battle.CardChoicePrompt ??= OnCardChoiceRequested;
        }

        #endregion

        #region Event Handlers

        private void OnBattleStateChanged(BattleStateChangedEvent evt)
        {
            if (endTurnButton != null)
                endTurnButton.interactable =
                    evt.Current == BattleState.PlayerTurn && !_cardChoiceActive;
            if (evt.Current == BattleState.PlayerTurn || evt.Current == BattleState.OpponentTurn)
                UpdateBattleInfo();
        }

        private void OnCardChoiceRequested(CardChoiceRequest evt)
        {
            if (_cardChoiceActive)
            {
                _queuedCardChoices.Enqueue(evt);
                return;
            }
            OpenCardChoice(evt);
        }

        private void OpenCardChoice(CardChoiceRequest evt)
        {
            _pendingCardChoice = evt;
            _cardChoiceActive = true;
            QueueRefresh(); // Media Training locks while a choice is open
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
            var confirmed = _pendingCardChoice;
            _pendingCardChoice = null;
            _cardChoiceActive = false;
            QueueRefresh();
            cardChoicePanel?.Close();
            if (endTurnButton != null)
                endTurnButton.interactable = true;
            // Resolve after closing: the callback may itself request another choice.
            confirmed?.OnConfirmed?.Invoke(selected);
            if (!_cardChoiceActive && _queuedCardChoices.Count > 0)
                OpenCardChoice(_queuedCardChoices.Dequeue());
        }

        #endregion

        #region Turn controls

        /// <summary>
        /// Shows the Media Training button only while it can be used (player turn, before any card
        /// play, enough Glamour, something to draw, no card choice open). Its inputs all queue a
        /// refresh, so this runs with it rather than every frame.
        /// </summary>
        protected override void Refresh()
        {
            if (_mediaTrainingButton == null)
                return;
            bool available =
                Battle != null && !_cardChoiceActive && Battle.CanUseMediaTraining;
            if (_mediaTrainingButton.gameObject.activeSelf != available)
                _mediaTrainingButton.gameObject.SetActive(available);
            if (
                available
                && _mediaTrainingLabel != null
                && (
                    _displayedGlamourCost != Battle.Celebrity.MediaTrainingCost
                    || _displayedDrawCount != Battle.Celebrity.MediaTrainingDraw
                )
            )
            {
                _displayedGlamourCost = Battle.Celebrity.MediaTrainingCost;
                _displayedDrawCount = Battle.Celebrity.MediaTrainingDraw;
                _mediaTrainingLabel.text =
                    $"Spend {Battle.Celebrity.MediaTrainingCost} Glamour: Draw {Battle.Celebrity.MediaTrainingDraw}";
            }
        }

        private void UpdateBattleInfo()
        {
            if (Battle == null)
                return;

            if (turnNumberText != null)
            {
                turnNumberText.text = $"Turn: {Battle.CurrentTurn}";
                turnNumberText.alpha = 1f;
            }

            if (phaseText != null)
            {
                phaseText.text = Battle.IsPlayerTurn ? "Your Turn" : "Opponent's Turn";
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

        public RectTransform BossTransform => bossPanel?.Anchor;

        #endregion

        #region Input Handlers

        private void OnEndTurnClicked()
        {
            if (Battle != null && Battle.IsPlayerTurn)
            {
                logPanel?.AddEntry("Player ended turn");
                Battle.RequestEndTurn();
            }
        }

        #endregion
    }
}
