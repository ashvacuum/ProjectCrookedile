using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Gameplay.Battle;
using Crookedile.Data.Cards;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Main UI controller for battle screen.
    /// Displays player/opponent stats, hand, and battle controls.
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        [Header("Player Stats")]
        [SerializeField] private TMP_Text playerResolveText;
        [SerializeField] private TMP_Text playerComposureText;
        [SerializeField] private TMP_Text playerHostilityText;
        [SerializeField] private TMP_Text playerAPText;

        [Header("Opponent Stats")]
        [SerializeField] private TMP_Text opponentResolveText;
        [SerializeField] private TMP_Text opponentComposureText;
        [SerializeField] private TMP_Text opponentHostilityText;
        [SerializeField] private TMP_Text opponentAPText;

        [Header("Battle Info")]
        [SerializeField] private TMP_Text turnNumberText;
        [SerializeField] private TMP_Text phaseText;

        [Header("Hand Display")]
        [SerializeField] private Transform cardButtonContainer;
        [SerializeField] private GameObject cardButtonPrefab;

        [Header("Controls")]
        [SerializeField] private Button endTurnButton;

        [Header("Battle Log")]
        [SerializeField] private TMP_Text battleLogText;
        [SerializeField] private ScrollRect battleLogScrollRect;
        [SerializeField] private int maxLogLines = 20;

        [Header("Status Effects")]
        [SerializeField] private Transform playerStatusContainer;
        [SerializeField] private Transform opponentStatusContainer;
        [SerializeField] private GameObject statusEffectPrefab;

        [Header("Battle Result")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;

        private BattleManager battleManager;
        private List<CardButton> activeCardButtons = new List<CardButton>();
        private List<string> battleLogLines = new List<string>();

        #region Initialization

        private void Awake()
        {
            // Setup button listeners
            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }

            // Hide result panels
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<TurnEndedEvent>(OnTurnEnded);
            EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Subscribe<BattleEndedEvent>(OnBattleEnded);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<TurnEndedEvent>(OnTurnEnded);
            EventBus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
        }

        /// <summary>
        /// Initialize with BattleManager reference.
        /// </summary>
        public void Initialize(BattleManager manager)
        {
            battleManager = manager;
            RefreshUI();
        }

        #endregion

        #region Event Handlers

        private void OnBattleStarted(BattleStartedEvent evt)
        {
            AddLogEntry("=== Battle Started ===");
            RefreshUI();
        }

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            string turnOwner = evt.IsPlayerTurn ? "Player" : "Opponent";
            AddLogEntry($"--- Turn {evt.TurnNumber}: {turnOwner} ---");
            RefreshUI();
        }

        private void OnTurnEnded(TurnEndedEvent evt)
        {
            RefreshUI();
        }

        private void OnCardPlayed(CardPlayedEvent evt)
        {
            string player = evt.IsPlayer ? "Player" : "Opponent";
            AddLogEntry($"{player} played: {evt.Card.CardName}");
            RefreshUI();
        }

        private void OnBattleEnded(BattleEndedEvent evt)
        {
            if (evt.Result.isVictory)
            {
                AddLogEntry("=== VICTORY ===");
                if (victoryPanel != null) victoryPanel.SetActive(true);
            }
            else
            {
                AddLogEntry("=== DEFEAT ===");
                if (defeatPanel != null) defeatPanel.SetActive(true);
            }

            // Disable controls
            if (endTurnButton != null) endTurnButton.interactable = false;
            ClearCardButtons();
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// Refreshes all UI elements to match current battle state.
        /// </summary>
        public void RefreshUI()
        {
            if (battleManager == null) return;

            UpdateStatsDisplay();
            UpdateBattleInfo();
            UpdateHandDisplay();
            UpdateStatusEffects();
        }

        private void UpdateStatsDisplay()
        {
            // Player stats
            var playerStats = battleManager.PlayerStats;
            if (playerResolveText != null)
                playerResolveText.text = $"Resolve: {playerStats.CurrentResolve}/{playerStats.MaxResolve}";
            if (playerComposureText != null)
                playerComposureText.text = $"Composure: {playerStats.CurrentComposure}";
            if (playerHostilityText != null)
                playerHostilityText.text = $"Hostility: {playerStats.CurrentHostility} ({playerStats.HostilityDamageMultiplier:F1}x)";
            if (playerAPText != null)
                playerAPText.text = $"AP: {playerStats.CurrentActionPoints}/{playerStats.MaxActionPoints}";

            // Opponent stats
            var opponentStats = battleManager.OpponentStats;
            if (opponentResolveText != null)
                opponentResolveText.text = $"Resolve: {opponentStats.CurrentResolve}/{opponentStats.MaxResolve}";
            if (opponentComposureText != null)
                opponentComposureText.text = $"Composure: {opponentStats.CurrentComposure}";
            if (opponentHostilityText != null)
                opponentHostilityText.text = $"Hostility: {opponentStats.CurrentHostility} ({opponentStats.HostilityDamageMultiplier:F1}x)";
            if (opponentAPText != null)
                opponentAPText.text = $"AP: {opponentStats.CurrentActionPoints}/{opponentStats.MaxActionPoints}";
        }

        private void UpdateBattleInfo()
        {
            if (turnNumberText != null)
                turnNumberText.text = $"Turn: {battleManager.CurrentTurn}";

            if (phaseText != null)
            {
                string phase = battleManager.CurrentState.ToString();
                string turnOwner = battleManager.IsPlayerTurn ? "Player" : "Opponent";
                phaseText.text = $"{phase} ({turnOwner})";
            }

            // Enable/disable end turn button
            if (endTurnButton != null)
            {
                bool canEndTurn = battleManager.IsPlayerTurn &&
                                  battleManager.CurrentState == BattleState.PlayerTurn;
                endTurnButton.interactable = canEndTurn;
            }
        }

        private void UpdateHandDisplay()
        {
            if (cardButtonContainer == null || cardButtonPrefab == null) return;

            // Clear existing buttons
            ClearCardButtons();

            // Only show hand during player's turn
            if (!battleManager.IsPlayerTurn) return;

            int currentAP = battleManager.PlayerStats.CurrentActionPoints;
            var hand = battleManager.PlayerDeck.Hand;

            for (int i = 0; i < hand.Count; i++)
            {
                CardData card = hand[i];
                GameObject buttonObj = Instantiate(cardButtonPrefab, cardButtonContainer);
                CardButton cardButton = buttonObj.GetComponent<CardButton>();

                if (cardButton != null)
                {
                    int handIndex = i; // Capture for closure
                    cardButton.Initialize(card, handIndex, currentAP, () => OnCardButtonClicked(card, handIndex));
                    cardButton.PlayDrawAnimation();
                    activeCardButtons.Add(cardButton);
                }
            }
        }

        /// <summary>
        /// Refreshes card affordability dimming when AP changes mid-turn without rebuilding the hand.
        /// </summary>
        private void RefreshCardAffordability()
        {
            if (!battleManager.IsPlayerTurn) return;
            int currentAP = battleManager.PlayerStats.CurrentActionPoints;
            foreach (var cardButton in activeCardButtons)
            {
                if (cardButton != null)
                    cardButton.RefreshVisuals(currentAP);
            }
        }

        private void UpdateStatusEffects()
        {
            // TODO: Display active status effects
            // For now, this is placeholder - will implement when we add status effect icons
        }

        private void ClearCardButtons()
        {
            foreach (var button in activeCardButtons)
            {
                if (button != null && button.gameObject != null)
                    Destroy(button.gameObject);
            }
            activeCardButtons.Clear();
        }

        #endregion

        #region Battle Log

        private void AddLogEntry(string message)
        {
            battleLogLines.Add(message);

            // Trim log if too long
            if (battleLogLines.Count > maxLogLines)
            {
                battleLogLines.RemoveAt(0);
            }

            UpdateBattleLog();
        }

        private void UpdateBattleLog()
        {
            if (battleLogText == null) return;

            battleLogText.text = string.Join("\n", battleLogLines);

            // Auto-scroll to bottom
            if (battleLogScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                battleLogScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        #endregion

        #region Input Handlers

        private void OnEndTurnClicked()
        {
            if (battleManager != null && battleManager.IsPlayerTurn)
            {
                EventBus.Publish(new EndTurnRequestedEvent());
                AddLogEntry("Player ended turn");
            }
        }

        private void OnCardButtonClicked(CardData card, int handIndex)
        {
            if (battleManager != null && battleManager.IsPlayerTurn)
            {
                EventBus.Publish(new PlayCardRequestedEvent
                {
                    Card = card,
                    HandIndex = handIndex
                });
            }
        }

        #endregion
    }
}
