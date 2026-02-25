using UnityEngine;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Owns the victory and defeat panel GameObjects.
    ///
    /// Extracted from <c>BattleUI</c> (and replaces the duplicate fields in
    /// <c>BattleStatsOverlay</c>).  Wire to a single shared instance in the scene so
    /// both scripts reference the same component.
    /// </summary>
    public class BattleResultPanel : MonoBehaviour
    {
        [Header("Result Panels")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;

        private void Awake()
        {
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
