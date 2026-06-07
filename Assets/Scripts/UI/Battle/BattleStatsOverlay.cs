using Crookedile.Core;
using Crookedile.Gameplay.Battle;
using TMPro;
using UnityEngine;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Debug/prototype stats overlay — displays the live battle state as plain text.
    /// Shows Opinion, Support, Denial, player AP, focused enemy hostility, and turn/phase info.
    /// Not intended as production UI; sized and positioned for editor testing.
    /// </summary>
    public class BattleStatsOverlay : MonoBehaviour
    {
        [Header("Opinion Meter")]
        [SerializeField]
        private TMP_Text opinionText;

        [SerializeField]
        private TMP_Text supportText;

        [SerializeField]
        private TMP_Text denialText;

        [Header("Player")]
        [SerializeField]
        private TMP_Text playerAPText;

        [Tooltip("Nepo Baby banked Patronage. Optional — leave null for other classes.")]
        [SerializeField]
        private TMP_Text patronageText;

        [Tooltip("Celebrity banked Attention. Optional — leave null for other classes.")]
        [SerializeField]
        private TMP_Text attentionText;

        [Header("Focused Enemy")]
        [SerializeField]
        private TMP_Text focusedEnemyHostilityText;

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
            // Banked resources change mid-turn (not just on state change) — refresh live.
            EventBus.Subscribe<PatronageChangedEvent>(OnPatronageChanged);
            EventBus.Subscribe<AttentionChangedEvent>(OnAttentionChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BattleStateChangedEvent>(OnBattleStateChanged);
            EventBus.Unsubscribe<PatronageChangedEvent>(OnPatronageChanged);
            EventBus.Unsubscribe<AttentionChangedEvent>(OnAttentionChanged);
        }

        public void Initialize(BattleManager manager)
        {
            battleManager = manager;
            RefreshStats();
        }

        #endregion

        #region Event Handlers

        private void OnBattleStateChanged(BattleStateChangedEvent evt) => RefreshStats();

        private void OnPatronageChanged(PatronageChangedEvent evt) => RefreshStats();

        private void OnAttentionChanged(AttentionChangedEvent evt) => RefreshStats();

        #endregion

        #region UI Updates

        public void RefreshStats()
        {
            if (battleManager == null)
                return;

            if (opinionText != null)
                opinionText.text =
                    $"Opinion: {battleManager.CurrentOpinion} / {battleManager.MaxOpinion}";

            if (supportText != null)
                supportText.text = $"Support: {battleManager.CurrentSupport}";

            if (denialText != null)
                denialText.text = $"Denial: {battleManager.CurrentDenial}";

            var playerStats = battleManager.PlayerStats;
            if (playerAPText != null && playerStats != null)
                playerAPText.text =
                    $"AP: {playerStats.CurrentActionPoints}/{playerStats.MaxActionPoints}";

            if (patronageText != null)
                patronageText.text = $"Patronage: {battleManager.CurrentPatronage}";

            if (attentionText != null)
                attentionText.text = $"Attention: {battleManager.CurrentAttention}";

            var enemyStats = battleManager.OpponentStats;
            if (focusedEnemyHostilityText != null)
                focusedEnemyHostilityText.text =
                    enemyStats != null
                        ? $"Hostility: {enemyStats.CurrentHostility} ({enemyStats.HostilityDamageMultiplier:F1}x)"
                        : "Hostility: —";

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
