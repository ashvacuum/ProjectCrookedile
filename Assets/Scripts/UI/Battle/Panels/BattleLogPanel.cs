using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Owns battle log display: appending entries, trimming the buffer, and auto-scrolling.
    ///
    /// Extracted from <c>BattleUI</c> so the FSM event handlers can call
    /// <c>logPanel.AddEntry()</c> without <c>BattleUI</c> managing log state directly.
    /// </summary>
    public class BattleLogPanel : MonoBehaviour
    {
        [Header("Log Display")]
        [SerializeField] private TMP_Text   battleLogText;
        [SerializeField] private ScrollRect battleLogScrollRect;
        [SerializeField] private int        maxLogLines = 20;

        private readonly List<string> _lines = new List<string>();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Appends a new line to the battle log and auto-scrolls to the bottom.
        /// Older lines are trimmed when the buffer exceeds <c>maxLogLines</c>.
        /// </summary>
        public void AddEntry(string message)
        {
            _lines.Add(message);

            if (_lines.Count > maxLogLines)
                _lines.RemoveAt(0);

            Flush();
        }

        /// <summary>Clears all log entries.</summary>
        public void Clear()
        {
            _lines.Clear();
            if (battleLogText != null) battleLogText.text = string.Empty;
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void Flush()
        {
            if (battleLogText == null) return;

            battleLogText.text = string.Join("\n", _lines);

            if (battleLogScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                battleLogScrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }
}
