using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Displays the shared Opinion Meter as three side-by-side elements inside a HorizontalLayoutGroup:
    ///   [PlayerShield] [BarFill] [EnemyShield]
    /// Widths are proportional to the bar container's total width. The background track (a sibling Image)
    /// shows through the unfilled portion on the right.
    ///
    /// HorizontalLayoutGroup on _barContainer must have childControlWidth and childForceExpandWidth disabled.
    /// </summary>
    public class OpinionMeterUI : MonoBehaviour
    {
        [Header("Bar Container")]
        [Tooltip("RectTransform with HorizontalLayoutGroup — parent of PlayerShield, BarFill, EnemyShield.")]
        [SerializeField]
        private RectTransform _barContainer;

        [Header("Bar Elements")]
        [Tooltip("Left Image — Player Support. Width = support / maxOpinion * totalWidth, clamped to opinionWidth.")]
        [SerializeField]
        private RectTransform _playerShield;

        [Tooltip("Middle Image — plain Image (not fill-method). Width = opinionWidth − playerShieldWidth.")]
        [SerializeField]
        private Image _barFill;

        [Tooltip("Right Image — Enemy Denial. Width = denial / maxOpinion * totalWidth, clamped to unfilled width.")]
        [SerializeField]
        private RectTransform _enemyShield;

        [Header("Overlays")]
        [Tooltip("RectTransform pinned at 50% of bar width — marks the Judgment win threshold.")]
        [SerializeField]
        private RectTransform _thresholdMarker;

        [Tooltip("Text label showing 'Opinion: X / Y'.")]
        [SerializeField]
        private TMP_Text _valueText;

        [Tooltip("Text label showing 'Judgment: Turn X / Y'. Hidden when there is no turn limit.")]
        [SerializeField]
        private TMP_Text _turnsText;

        [Header("Colors")]
        [SerializeField]
        private Color _normalBarColor = new Color(0.2f, 0.75f, 0.35f);

        [SerializeField]
        private Color _dangerBarColor = new Color(0.85f, 0.2f, 0.2f);

        [SerializeField]
        private Color _normalColor = Color.white;

        [SerializeField]
        private Color _urgentColor = new Color(0.9f, 0.2f, 0.2f);

        #region Public API

        /// <summary>
        /// Recalculates all three element widths and updates text labels.
        /// Call from BattleUI in response to opinion / shield / turn events.
        /// </summary>
        public void Refresh(
            int currentOpinion,
            int maxOpinion,
            int turnsElapsed,
            int maxTurns,
            int playerSupport = 0,
            int enemyDenial = 0
        )
        {
            if (_barContainer == null)
                return;

            float total = _barContainer.rect.width;
            float pct = maxOpinion > 0 ? Mathf.Clamp01((float)currentOpinion / maxOpinion) : 0f;

            float opinionWidth = pct * total;
            float unfilledWidth = total - opinionWidth;

            float playerShieldWidth = maxOpinion > 0
                ? Mathf.Min((float)playerSupport / maxOpinion * total, opinionWidth)
                : 0f;
            float barFillWidth = opinionWidth - playerShieldWidth;
            float enemyShieldWidth = maxOpinion > 0
                ? Mathf.Min((float)enemyDenial / maxOpinion * total, unfilledWidth)
                : 0f;

            SetWidth(_playerShield, playerShieldWidth);
            if (_barFill != null)
                SetWidth(_barFill.rectTransform, barFillWidth);
            SetWidth(_enemyShield, enemyShieldWidth);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_barContainer);

            if (_barFill != null)
                _barFill.color = pct < 0.30f ? _dangerBarColor : _normalBarColor;

            if (_valueText != null)
                _valueText.text = $"Opinion: {currentOpinion} / {maxOpinion}";

            RefreshTurnCountdown(turnsElapsed, maxTurns);
        }

        #endregion

        #region Private

        private static void SetWidth(RectTransform rt, float width)
        {
            if (rt == null)
                return;
            rt.sizeDelta = new Vector2(Mathf.Max(0f, width), rt.sizeDelta.y);
        }

        private void RefreshTurnCountdown(int turnsElapsed, int maxTurns)
        {
            if (_turnsText == null)
                return;

            if (maxTurns <= 0)
            {
                _turnsText.gameObject.SetActive(false);
                return;
            }

            _turnsText.gameObject.SetActive(true);
            int remaining = Mathf.Max(0, maxTurns - turnsElapsed);
            _turnsText.text = $"Judgment: Turn {turnsElapsed} / {maxTurns}";
            _turnsText.color = remaining <= 2 ? _urgentColor : _normalColor;
        }

        #endregion
    }
}
