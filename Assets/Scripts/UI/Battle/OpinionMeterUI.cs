using Crookedile.Core;
using Crookedile.Gameplay.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Displays the shared Opinion Meter and the Judgment turn countdown.
    /// Place one instance in the battle scene and wire it through <see cref="BattleUI"/>.
    ///
    /// The meter fills left-to-right: left = 0% opinion, right = 100%.
    /// A threshold marker sits at 50% — opinion must exceed this for a Judgment victory.
    /// </summary>
    public class OpinionMeterUI : MonoBehaviour
    {
        [Header("Opinion Bar")]
        [Tooltip("Filled Image (fill method = Horizontal) representing current opinion.")]
        [SerializeField]
        private Image _barFill;

        [Tooltip("Text label showing 'Opinion: X / Y'.")]
        [SerializeField]
        private TMP_Text _valueText;

        [Tooltip("RectTransform anchored at 50% of the bar width — marks the win threshold.")]
        [SerializeField]
        private RectTransform _thresholdMarker;

        [Header("Composure Shields")]
        [Tooltip(
            "Left shield RectTransform — player composure (resists opinion drops). "
                + "Width scales with composure value."
        )]
        [SerializeField]
        private RectTransform _playerShield;

        [Tooltip(
            "Right shield RectTransform — focused enemy composure (resists opinion rises). "
                + "Width scales with composure value."
        )]
        [SerializeField]
        private RectTransform _enemyShield;

        [Tooltip("Composure value that maps to maximum shield width.")]
        [SerializeField]
        private int _shieldFullValue = 20;

        [Tooltip("Maximum shield width in pixels at _shieldFullValue composure.")]
        [SerializeField]
        private float _shieldMaxWidth = 60f;

        [Header("Turn Countdown")]
        [Tooltip("Text label showing 'Turn X / Y' or hidden when there is no turn limit.")]
        [SerializeField]
        private TMP_Text _turnsText;

        [Tooltip("Color applied to the turns text when 2 or fewer turns remain.")]
        [SerializeField]
        private Color _urgentColor = new Color(0.9f, 0.2f, 0.2f);

        [Tooltip("Normal color for the turns text.")]
        [SerializeField]
        private Color _normalColor = Color.white;

        [Tooltip("Color applied to the opinion bar when opinion falls below 30%.")]
        [SerializeField]
        private Color _dangerBarColor = new Color(0.85f, 0.2f, 0.2f);

        [Tooltip("Normal opinion bar fill color.")]
        [SerializeField]
        private Color _normalBarColor = new Color(0.2f, 0.75f, 0.35f);

        #region Public API
        /// <summary>
        /// Updates the opinion bar fill and text, refreshes the turn countdown,
        /// and sizes the composure shields on each side.
        /// Call from <see cref="BattleUI"/> in response to opinion / composure / turn events.
        /// </summary>
        /// <param name="playerComposure">Player's current composure — sizes the left shield.</param>
        /// <param name="enemyComposure">Focused enemy's current composure — sizes the right shield.</param>
        public void Refresh(
            int currentOpinion,
            int maxOpinion,
            int turnsElapsed,
            int maxTurns,
            int playerComposure = 0,
            int enemyComposure = 0
        )
        {
            float pct = maxOpinion > 0 ? (float)currentOpinion / maxOpinion : 0f;

            if (_barFill != null)
            {
                _barFill.fillAmount = pct;
                _barFill.color = pct < 0.30f ? _dangerBarColor : _normalBarColor;
            }

            if (_valueText != null)
                _valueText.text = $"Opinion: {currentOpinion} / {maxOpinion}";

            RefreshTurnCountdown(turnsElapsed, maxTurns);
            SizeShield(_playerShield, playerComposure);
            SizeShield(_enemyShield, enemyComposure);
        }

        #endregion

        #region Private
        private void SizeShield(RectTransform shield, int composure)
        {
            if (shield == null)
                return;
            float w =
                _shieldFullValue > 0
                    ? Mathf.Clamp01((float)composure / _shieldFullValue) * _shieldMaxWidth
                    : 0f;
            var size = shield.sizeDelta;
            shield.sizeDelta = new Vector2(w, size.y);
            shield.gameObject.SetActive(composure > 0);
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
