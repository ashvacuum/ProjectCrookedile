using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Crookedile.Core;
using Crookedile.Gameplay.Battle;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Displays battle stats as a simple overlay on top of the 3D card view.
    /// Shows Resolve, Composure, Hostility, and AP for both combatants.
    /// </summary>
    public class BattleStatsOverlay : MonoBehaviour
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
        [SerializeField] private TMP_Text turnInfoText;
        [SerializeField] private TMP_Text phaseText;

        [Header("Controls")]
        [SerializeField] private Button endTurnButton;

        [Header("Battle Result")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;

        private BattleManager battleManager;

        #region Initialization

        private void Awake()
        {
            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }

            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<TurnEndedEvent>(OnTurnEnded);
            EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Subscribe<BattleEndedEvent>(OnBattleEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<TurnEndedEvent>(OnTurnEnded);
            EventBus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
        }

        public void Initialize(BattleManager manager)
        {
            battleManager = manager;
            RefreshStats();
        }

        #endregion

        #region Event Handlers

        private void OnBattleStarted(BattleStartedEvent evt)
        {
            RefreshStats();
        }

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            RefreshStats();
        }

        private void OnTurnEnded(TurnEndedEvent evt)
        {
            RefreshStats();
        }

        private void OnCardPlayed(CardPlayedEvent evt)
        {
            RefreshStats();
        }

        private void OnBattleEnded(BattleEndedEvent evt)
        {
            if (evt.Result.isVictory)
            {
                if (victoryPanel != null) victoryPanel.SetActive(true);
            }
            else
            {
                if (defeatPanel != null) defeatPanel.SetActive(true);
            }

            if (endTurnButton != null) endTurnButton.interactable = false;
            RefreshStats();
        }

        #endregion

        #region UI Updates

        public void RefreshStats()
        {
            if (battleManager == null) return;

            UpdatePlayerStats();
            UpdateOpponentStats();
            UpdateBattleInfo();
        }

        private void UpdatePlayerStats()
        {
            var stats = battleManager.PlayerStats;

            if (playerResolveText != null)
                playerResolveText.text = $"HP: {stats.CurrentResolve}/{stats.MaxResolve}";

            if (playerComposureText != null)
                playerComposureText.text = $"Composure: {stats.CurrentComposure}";

            if (playerHostilityText != null)
                playerHostilityText.text = $"Hostility: {stats.CurrentHostility} ({stats.HostilityDamageMultiplier:F1}x)";

            if (playerAPText != null)
                playerAPText.text = $"AP: {stats.CurrentActionPoints}/{stats.MaxActionPoints}";
        }

        private void UpdateOpponentStats()
        {
            var stats = battleManager.OpponentStats;

            if (opponentResolveText != null)
                opponentResolveText.text = $"HP: {stats.CurrentResolve}/{stats.MaxResolve}";

            if (opponentComposureText != null)
                opponentComposureText.text = $"Composure: {stats.CurrentComposure}";

            if (opponentHostilityText != null)
                opponentHostilityText.text = $"Hostility: {stats.CurrentHostility}";

            if (opponentAPText != null)
                opponentAPText.text = $"AP: {stats.CurrentActionPoints}/{stats.MaxActionPoints}";
        }

        private void UpdateBattleInfo()
        {
            if (turnInfoText != null)
            {
                string turnOwner = battleManager.IsPlayerTurn ? "Your Turn" : "Opponent's Turn";
                turnInfoText.text = $"Turn {battleManager.CurrentTurn} - {turnOwner}";
            }

            if (phaseText != null)
            {
                phaseText.text = battleManager.CurrentState.ToString();
            }

            if (endTurnButton != null)
            {
                bool canEndTurn = battleManager.IsPlayerTurn &&
                                  battleManager.CurrentState == BattleState.PlayerTurn;
                endTurnButton.interactable = canEndTurn;
            }
        }

        #endregion

        #region Input Handlers

        private void OnEndTurnClicked()
        {
            if (battleManager != null && battleManager.IsPlayerTurn)
            {
                EventBus.Publish(new EndTurnRequestedEvent());
            }
        }

        #endregion
    }
}
