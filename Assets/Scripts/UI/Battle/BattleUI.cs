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

        // ── Player Stats ──────────────────────────────────────────────────────
        [Header("Player Stats")]
        [SerializeField] private TMP_Text playerResolveText;
        [SerializeField] private TMP_Text playerComposureText;
        [SerializeField] private TMP_Text playerHostilityText;
        [SerializeField] private TMP_Text playerAPText;

        // ── Enemy Slots ───────────────────────────────────────────────────────
        [Header("Enemy Slots")]
        [Tooltip("Parent transform that enemy slot panels are spawned into.")]
        [SerializeField] private Transform  enemySlotContainer;
        [Tooltip("Prefab with an EnemySlotUI component — instantiated once per enemy.")]
        [SerializeField] private GameObject enemySlotPrefab;

        // ── Battle Info ───────────────────────────────────────────────────────
        [Header("Battle Info")]
        [SerializeField] private TMP_Text turnNumberText;
        [SerializeField] private TMP_Text phaseText;

        // ── Controls ──────────────────────────────────────────────────────────
        [Header("Controls")]
        [SerializeField] private Button endTurnButton;
        [Tooltip("Actor passive — shown on the Actor's first player turn only.")]
        [SerializeField] private Button             improviseButton;
        [Tooltip("Card selection modal shared by Improvise and ChooseFromDiscard effects.")]
        [SerializeField] private CardSelectionPanel cardSelectionPanel;

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

        // ── Runtime ───────────────────────────────────────────────────────────
        private BattleManager             battleManager;
        private StateMachine<BattleUIState> _fsm;
        private BattleResult              _lastBattleResult;
        private List<EnemySlotUI>         _enemySlots = new List<EnemySlotUI>();

        #region Initialization

        private void Awake()
        {
            if (endTurnButton != null)
                endTurnButton.onClick.AddListener(OnEndTurnClicked);

            if (improviseButton != null)
            {
                improviseButton.onClick.AddListener(OnImproviseClicked);
                improviseButton.gameObject.SetActive(false);
            }
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

            // Build FSM — state classes are inner private classes below.
            _fsm = new StateMachine<BattleUIState>();
            _fsm.RegisterState(BattleUIState.Idle,       new IdleBattleUIState(this));
            _fsm.RegisterState(BattleUIState.PlayerTurn, new PlayerTurnBattleUIState(this));
            _fsm.RegisterState(BattleUIState.Improvise,  new ImproviseBattleUIState(this));
            _fsm.RegisterState(BattleUIState.BattleEnd,  new BattleEndBattleUIState(this));
            _fsm.ChangeState(BattleUIState.Idle);

            UpdateStatsDisplay();
        }

        #endregion

        #region Event Handlers

        private void OnBattleStarted(BattleStartedEvent evt)
        {
            logPanel?.AddEntry("=== Battle Started ===");
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
            string player = evt.IsPlayer ? "Player" : "Opponent";
            logPanel?.AddEntry($"{player} played: {evt.Card.CardName}");
            UpdateStatsDisplay();
            // Only refresh affordability — don't rebuild hand (card is still being resolved).
            if (battleManager?.PlayerStats != null)
                handPanel?.RefreshAffordability(battleManager.PlayerStats.CurrentActionPoints);
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
            if (evt.EnemyIndex < _enemySlots.Count)
                _enemySlots[evt.EnemyIndex]?.MarkDefeated();
        }

        private void OnBattleEnded(BattleEndedEvent evt)
        {
            string outcome = evt.Result.isVictory ? "=== VICTORY ===" : "=== DEFEAT ===";
            logPanel?.AddEntry(outcome);
            _lastBattleResult = evt.Result;
            _fsm?.ChangeState(BattleUIState.BattleEnd);
        }

        #endregion

        #region UI Updates (stats + battle-info text; kept in BattleUI for direct field access)

        internal void UpdateStatsDisplay()
        {
            if (battleManager == null) return;

            var playerStats = battleManager.PlayerStats;
            if (playerStats == null) return;

            if (playerResolveText  != null) playerResolveText.text  = $"Resolve: {playerStats.CurrentResolve}/{playerStats.MaxResolve}";
            if (playerComposureText != null) playerComposureText.text = $"Composure: {playerStats.CurrentComposure}";
            if (playerAPText       != null) playerAPText.text       = $"AP: {playerStats.CurrentActionPoints}/{playerStats.MaxActionPoints}";

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
                turnNumberText.text = $"Turn: {battleManager.CurrentTurn}";

            if (phaseText != null)
            {
                string turnOwner = battleManager.IsPlayerTurn ? "Player" : "Opponent";
                phaseText.text = $"{battleManager.CurrentState} ({turnOwner})";
            }
        }

        #endregion

        #region Enemy Slots

        private void BuildEnemySlots()
        {
            foreach (var slot in _enemySlots)
                if (slot != null) Destroy(slot.gameObject);
            _enemySlots.Clear();

            if (enemySlotContainer == null || enemySlotPrefab == null || battleManager == null) return;

            for (int i = 0; i < battleManager.Enemies.Count; i++)
            {
                var go   = Instantiate(enemySlotPrefab, enemySlotContainer);
                var slot = go.GetComponent<EnemySlotUI>();
                if (slot != null)
                {
                    slot.Initialize(i, battleManager, battleManager.PlayerOrigin);
                    _enemySlots.Add(slot);
                }
            }
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
            if (battleManager != null && battleManager.IsPlayerTurn)
            {
                EventBus.Publish(new PlayCardRequestedEvent { Card = card, HandIndex = handIndex });
            }
        }

        private void OnImproviseClicked()
        {
            _fsm?.ChangeState(BattleUIState.Improvise);
        }

        /// <summary>
        /// Called by <c>ImproviseBattleUIState</c> via <c>CardSelectionPanel.Open</c> callback
        /// when the player presses Discard.
        /// </summary>
        internal void OnImproviseConfirmed(List<CardData> selectedCards)
        {
            battleManager.TryPlayerImprovise(selectedCards);
            _fsm?.ChangeState(BattleUIState.PlayerTurn);
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

                bool showImprovise = _ui.battleManager?.IsImproviseAvailable ?? false;
                if (_ui.improviseButton != null)
                    _ui.improviseButton.gameObject.SetActive(showImprovise);

                _ui.UpdateStatsDisplay();
                _ui.UpdateBattleInfo();
            }
        }

        #endregion

        #region State: Improvise

        private sealed class ImproviseBattleUIState : State
        {
            private readonly BattleUI _ui;
            public ImproviseBattleUIState(BattleUI ui) => _ui = ui;

            public override void OnEnter()
            {
                if (_ui.cardSelectionPanel == null) return;

                _ui.cardSelectionPanel.OnCardReturnedToHand += OnCardReturned;
                _ui.cardSelectionPanel.Open("Improvise", _ui.OnImproviseConfirmed);
                _ui.handPanel?.RefreshImproviseHand(_ui.battleManager, _ui.cardSelectionPanel);

                // Hide the Improvise button while the modal is open
                if (_ui.improviseButton != null)
                    _ui.improviseButton.gameObject.SetActive(false);
            }

            public override void OnExit()
            {
                if (_ui.cardSelectionPanel != null)
                    _ui.cardSelectionPanel.OnCardReturnedToHand -= OnCardReturned;
                // Panel closes itself via OnDiscardClicked → OnImproviseConfirmed
            }

            private void OnCardReturned(CardData card)
            {
                // A card was sent back from the discard zone — rebuild the hand to show it again.
                _ui.handPanel?.RefreshImproviseHand(_ui.battleManager, _ui.cardSelectionPanel);
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
