using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Crookedile.Core;
using Crookedile.Gameplay.Battle;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Pure stats reader — displays Resolve, Composure, Hostility, and AP for both
    /// combatants as a lightweight overlay.
    ///
    /// This component is intentionally limited to stat display.  End-turn input,
    /// improvise controls, and battle result panels are owned by <c>BattleUI</c> and its
    /// FSM states.  If the scene needs a result display on this overlay, assign the
    /// shared <c>BattleResultPanel</c> component to the <c>resultPanel</c> field.
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

        [Header("Result (optional)")]
        [Tooltip("Assign the shared BattleResultPanel if this overlay needs to show victory/defeat.")]
        [SerializeField] private BattleResultPanel resultPanel;

        private BattleManager battleManager;

        #region Initialization

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

        private void OnBattleStarted(BattleStartedEvent evt) => RefreshStats();
        private void OnTurnStarted(TurnStartedEvent evt)     => RefreshStats();
        private void OnTurnEnded(TurnEndedEvent evt)         => RefreshStats();
        private void OnCardPlayed(CardPlayedEvent evt)       => RefreshStats();

        private void OnBattleEnded(BattleEndedEvent evt)
        {
            resultPanel?.Show(evt.Result.isVictory);
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
            if (stats == null) return;

            if (playerResolveText   != null) playerResolveText.text   = $"HP: {stats.CurrentResolve}/{stats.MaxResolve}";
            if (playerComposureText != null) playerComposureText.text = $"Composure: {stats.CurrentComposure}";
            if (playerHostilityText != null) playerHostilityText.text = $"Hostility: {stats.CurrentHostility} ({stats.HostilityDamageMultiplier:F1}x)";
            if (playerAPText        != null) playerAPText.text        = $"AP: {stats.CurrentActionPoints}/{stats.MaxActionPoints}";
        }

        private void UpdateOpponentStats()
        {
            var stats = battleManager.OpponentStats;
            if (stats == null) return;

            if (opponentResolveText   != null) opponentResolveText.text   = $"HP: {stats.CurrentResolve}/{stats.MaxResolve}";
            if (opponentComposureText != null) opponentComposureText.text = $"Composure: {stats.CurrentComposure}";
            if (opponentHostilityText != null) opponentHostilityText.text = $"Hostility: {stats.CurrentHostility}";
            if (opponentAPText        != null) opponentAPText.text        = $"AP: {stats.CurrentActionPoints}/{stats.MaxActionPoints}";
        }

        private void UpdateBattleInfo()
        {
            if (turnInfoText != null)
            {
                string turnOwner = battleManager.IsPlayerTurn ? "Your Turn" : "Opponent's Turn";
                turnInfoText.text = $"Turn {battleManager.CurrentTurn} - {turnOwner}";
            }

            if (phaseText != null)
                phaseText.text = battleManager.CurrentState.ToString();
        }

        #endregion
    }
}
