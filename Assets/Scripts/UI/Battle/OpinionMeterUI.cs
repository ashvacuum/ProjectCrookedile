using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Displays the shared Opinion Meter as three side-by-side elements inside a HorizontalLayoutGroup:
    ///   [BarFill] [Support bar] [Denial bar]
    /// BarFill spans the FULL current opinion (so the fill never under-reports the label).
    /// The Support bar sits at the fill's right edge — incoming drops bite there first.
    /// The Denial bar sits beyond it — player gains must chew through it before the
    /// fill grows. Both bars render in the unfilled region; the background track shows
    /// through whatever remains. Widths tween smoothly on change.
    ///
    /// HorizontalLayoutGroup on _barContainer must have childControlWidth and childForceExpandWidth disabled.
    /// </summary>
    public class OpinionMeterUI : MonoBehaviour
    {
        [Header("Bar Container")]
        [Tooltip(
            "RectTransform with HorizontalLayoutGroup — parent of the Support bar, BarFill, Denial bar."
        )]
        [SerializeField]
        private RectTransform _barContainer;

        [Header("Bar Elements")]
        [Tooltip(
            "Support segment — rendered at the fill's right edge (drops bite there first). Width clamped to the unfilled region."
        )]
        [FormerlySerializedAs("_playerShield")]
        [SerializeField]
        private RectTransform _playerSupportBar;

        [Tooltip("Opinion fill — plain Image (not fill-method). Width = full current opinion.")]
        [SerializeField]
        private Image _barFill;

        [Tooltip(
            "Denial segment — rendered after Support (gains chew through it). Width clamped to the remaining unfilled region."
        )]
        [FormerlySerializedAs("_enemyShield")]
        [SerializeField]
        private RectTransform _enemyDenialBar;

        [Header("Animation")]
        [Tooltip("Seconds for segment widths to tween to their new size. 0 = snap.")]
        [SerializeField]
        private float _tweenDuration = 0.25f;

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

        #region Runtime

        // True after the first successful Refresh — the first paint snaps instead of
        // tweening from whatever stale widths the scene serialized.
        private bool _hasPainted;

        private void Awake()
        {
            // Enforce the [BarFill][Support bar][Denial bar] sibling order regardless of
            // how the scene hierarchy happens to be arranged.
            _barFill?.rectTransform.SetSiblingIndex(0);
            _playerSupportBar?.SetSiblingIndex(1);
            _enemyDenialBar?.SetSiblingIndex(2);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Anchor for VFX / floating numbers that target the meter itself (e.g. an Opinion shift,
        /// which moves this bar rather than depleting an enemy). Falls back to this transform.
        /// </summary>
        public RectTransform AnchorTransform =>
            _barFill != null ? _barFill.rectTransform
            : _barContainer != null ? _barContainer
            : (RectTransform)transform;

        /// <summary>
        /// Recalculates all three element widths (tweened) and updates text labels.
        /// Call from BattleUI in response to opinion / Support / Denial / turn events.
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
            if (total <= 0f)
            {
                // First-frame call before the canvas layout pass — force a layout so the
                // container has a real width instead of painting an empty bar.
                LayoutRebuilder.ForceRebuildLayoutImmediate(_barContainer);
                total = _barContainer.rect.width;
                if (total <= 0f)
                    return;
            }

            float pct = maxOpinion > 0 ? Mathf.Clamp01((float)currentOpinion / maxOpinion) : 0f;

            // Fill = full current opinion; Support/Denial bars live in the unfilled region to its right.
            float barFillWidth = pct * total;
            float unfilled = total - barFillWidth;

            float playerSupportWidth =
                maxOpinion > 0
                    ? Mathf.Min((float)playerSupport / maxOpinion * total, unfilled)
                    : 0f;
            float enemyDenialWidth =
                maxOpinion > 0
                    ? Mathf.Min(
                        (float)enemyDenial / maxOpinion * total,
                        unfilled - playerSupportWidth
                    )
                    : 0f;

            if (_barFill != null)
                AnimateWidth(_barFill.rectTransform, barFillWidth);
            AnimateWidth(_playerSupportBar, playerSupportWidth);
            AnimateWidth(_enemyDenialBar, enemyDenialWidth);
            _hasPainted = true;

            if (_barFill != null)
                _barFill.color = pct < 0.30f ? _dangerBarColor : _normalBarColor;

            if (_valueText != null)
                _valueText.text = $"Opinion: {currentOpinion} / {maxOpinion}";

            RefreshTurnCountdown(turnsElapsed, maxTurns);
        }

        #endregion

        #region Private

        /// <summary>
        /// Tweens a segment to <paramref name="width"/>, re-flowing the layout group each
        /// frame so siblings slide along. Snaps on the first paint and when tweening is off.
        /// </summary>
        private void AnimateWidth(RectTransform rt, float width)
        {
            if (rt == null)
                return;
            width = Mathf.Max(0f, width);

            DOTween.Kill(rt);

            if (!_hasPainted || _tweenDuration <= 0f)
            {
                rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
                LayoutRebuilder.MarkLayoutForRebuild(_barContainer);
                return;
            }

            DOTween
                .To(
                    () => rt.sizeDelta.x,
                    x =>
                    {
                        rt.sizeDelta = new Vector2(x, rt.sizeDelta.y);
                        LayoutRebuilder.MarkLayoutForRebuild(_barContainer);
                    },
                    width,
                    _tweenDuration
                )
                .SetEase(Ease.OutQuad)
                .SetTarget(rt)
                .SetLink(gameObject);
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
