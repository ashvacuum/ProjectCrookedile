using Crookedile.Core;
using Crookedile.Gameplay.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField]
        private TMP_Text playerResolveText;

        [SerializeField]
        private TMP_Text playerComposureText;

        [SerializeField]
        private TMP_Text playerHostilityText;

        [SerializeField]
        private TMP_Text playerAPText;

        [Header("Opponent Stats")]
        [SerializeField]
        private TMP_Text opponentResolveText;

        [SerializeField]
        private TMP_Text opponentComposureText;

        [SerializeField]
        private TMP_Text opponentHostilityText;

        [SerializeField]
        private TMP_Text opponentAPText;

        [Header("Battle Info")]
        [SerializeField]
        private TMP_Text turnInfoText;

        [SerializeField]
        private TMP_Text phaseText;

        [Header("Result (optional)")]
        [Tooltip(
            "Assign the shared BattleResultPanel if this overlay needs to show victory/defeat."
        )]
        [SerializeField]
        private BattleResultPanel resultPanel;

        private BattleManager battleManager;

        #region Initialization

        private void OnEnable()
        {
            EventBus.Subscribe<BattleStateChangedEvent>(OnBattleStateChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BattleStateChangedEvent>(OnBattleStateChanged);
        }

        public void Initialize(BattleManager manager)
        {
            battleManager = manager;
            RefreshStats();
        }

        #endregion

        #region Event Handlers

        private void OnBattleStateChanged(BattleStateChangedEvent evt) => RefreshStats();

        #endregion

        #region UI Updates

        public void RefreshStats()
        {
            if (battleManager == null)
                return;
            UpdatePlayerStats();
            UpdateOpponentStats();
            UpdateBattleInfo();
        }

        private void UpdatePlayerStats()
        {
            var stats = battleManager.PlayerStats;
            if (stats == null)
                return;

            if (playerResolveText != null)
                playerResolveText.text = $"AP: {stats.CurrentActionPoints}/{stats.MaxActionPoints}";
            if (playerComposureText != null)
                playerComposureText.text = "";
            if (playerHostilityText != null)
                playerHostilityText.text =
                    $"Hostility: {stats.CurrentHostility} ({stats.HostilityDamageMultiplier:F1}x)";
            if (playerAPText != null)
                playerAPText.text = $"AP: {stats.CurrentActionPoints}/{stats.MaxActionPoints}";
        }

        private void UpdateOpponentStats()
        {
            var stats = battleManager.OpponentStats;
            if (stats == null)
                return;

            if (opponentResolveText != null)
                opponentResolveText.text = $"Hostility: {stats.CurrentHostility}";
            if (opponentComposureText != null)
                opponentComposureText.text = "";
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
                phaseText.text = battleManager.CurrentState.ToString();
        }

        #endregion
    }
}
