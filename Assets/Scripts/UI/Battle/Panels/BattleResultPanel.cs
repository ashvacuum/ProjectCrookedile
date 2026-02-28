using System;
using UnityEngine;
using UnityEngine.UI;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Owns the victory and defeat panel GameObjects.
    ///
    /// Extracted from <c>BattleUI</c> (and replaces the duplicate fields in
    /// <c>BattleStatsOverlay</c>).  Wire to a single shared instance in the scene so
    /// both scripts reference the same component.
    ///
    /// The <see cref="OnContinueClicked"/> event fires when the player presses Continue
    /// on the victory panel. BattleUI subscribes to this to trigger the reward screen.
    /// </summary>
    public class BattleResultPanel : MonoBehaviour
    {
        [Header("Result Panels")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;

        [Header("Buttons")]
        [Tooltip("Continue button shown inside the victory panel. Fires OnContinueClicked when pressed.")]
        [SerializeField] private Button _continueButton;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Fired when the player presses Continue after a victory.</summary>
        public event Action OnContinueClicked;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _continueButton?.onClick.AddListener(() => OnContinueClicked?.Invoke());
            Hide();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Shows the victory or defeat panel based on <paramref name="isVictory"/>.</summary>
        public void Show(bool isVictory)
        {
            if (victoryPanel != null) victoryPanel.SetActive(isVictory);
            if (defeatPanel  != null) defeatPanel.SetActive(!isVictory);
        }

        /// <summary>Hides both panels.</summary>
        public void Hide()
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel  != null) defeatPanel.SetActive(false);
        }
    }
}
