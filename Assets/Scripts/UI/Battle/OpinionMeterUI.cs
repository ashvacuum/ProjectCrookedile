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

        [Header("Juice")]
        [Tooltip(
            "Optional 'chip' bar drawn BEHIND the fill, outside the layout group: same parent and "
                + "left edge as the bar container, pivot x = 0. On a drop it holds the old width, "
                + "then drains; on a gain it jumps ahead and the fill catches up."
        )]
        [SerializeField]
        private Image _ghostFill;

        [Tooltip("Ghost tint while draining after a drop.")]
        [SerializeField]
        private Color _ghostLossColor = new Color(1f, 0.85f, 0.3f);

        [Tooltip("Ghost tint while leading a gain.")]
        [SerializeField]
        private Color _ghostGainColor = new Color(0.75f, 1f, 0.75f);

        [Tooltip("Seconds the ghost lingers at the old width before draining.")]
        [SerializeField]
        private float _ghostDelay = 0.35f;

        [Tooltip("Seconds the ghost takes to drain, and the value label to count to its new number.")]
        [SerializeField]
        private float _ghostDuration = 0.45f;

        [Tooltip("Shift (as a fraction of max Opinion) at which shake/punch reach full strength.")]
        [Range(0.01f, 1f)]
        [SerializeField]
        private float _fullJuiceShift = 0.15f;

        [Tooltip("Pixels the meter shakes on a full-strength drop.")]
        [SerializeField]
        private float _shakeStrength = 18f;

        [Tooltip("Scale punch on a full-strength gain.")]
        [SerializeField]
        private float _punchScale = 0.12f;

        [Tooltip("Color the fill flashes to on any shift before settling back.")]
        [SerializeField]
        private Color _flashColor = Color.white;

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

        private float _lastFillWidth;

        // Number the value label currently reads; counts toward the real value.
        private int _displayedOpinion;
        private int _countTarget = -1;

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

            if (_barFill != null && !Mathf.Approximately(barFillWidth, _lastFillWidth))
            {
                AnimateGhost(_lastFillWidth, barFillWidth);
                AnimateWidth(_barFill.rectTransform, barFillWidth);
                _lastFillWidth = barFillWidth;
            }
            AnimateWidth(_playerSupportBar, playerSupportWidth);
            AnimateWidth(_enemyDenialBar, enemyDenialWidth);

            // Kick() owns the color while its flash is running.
            if (_barFill != null && !DOTween.IsTweening(_barFill))
                _barFill.color = BarColor(pct);

            CountValueText(currentOpinion, maxOpinion);
            _hasPainted = true;

            RefreshTurnCountdown(turnsElapsed, maxTurns);
        }

        /// <summary>
        /// One-shot impact for an Opinion shift: shake on a drop, punch on a gain, fill flash,
        /// value-label punch. Strength scales with the shift relative to max Opinion. The bar
        /// widths themselves move through <see cref="Refresh"/>.
        /// </summary>
        public void Kick(int oldValue, int newValue, int maxValue)
        {
            int delta = newValue - oldValue;
            if (delta == 0 || maxValue <= 0)
                return;
            float strength = Mathf.Clamp01(Mathf.Abs(delta) / (maxValue * _fullJuiceShift));
            strength = Mathf.Lerp(0.3f, 1f, strength); // small shifts still register

            var root = (RectTransform)transform;
            root.DOComplete();
            if (delta < 0)
                root.DOShakeAnchorPos(0.35f, _shakeStrength * strength, vibrato: 20)
                    .SetLink(gameObject);
            else
                root.DOPunchScale(Vector3.one * _punchScale * strength, 0.35f, vibrato: 8)
                    .SetLink(gameObject);

            if (_barFill != null)
            {
                DOTween.Kill(_barFill);
                _barFill.color = _flashColor;
                _barFill
                    .DOColor(BarColor((float)newValue / maxValue), 0.3f)
                    .SetTarget(_barFill)
                    .SetLink(gameObject);
            }

            if (_valueText != null)
            {
                _valueText.transform.DOComplete();
                _valueText.transform.DOPunchScale(Vector3.one * 0.25f * strength, 0.3f)
                    .SetLink(gameObject);
            }
        }

        /// <summary>World position of the fill's leading edge at <paramref name="value"/> — where
        /// an Opinion shift visibly lands, for VFX that should spark there.</summary>
        public Vector3 EdgeWorldPosition(int value, int maxValue)
        {
            var c = new Vector3[4];
            (_barContainer != null ? _barContainer : (RectTransform)transform).GetWorldCorners(c);
            float pct = maxValue > 0 ? Mathf.Clamp01((float)value / maxValue) : 0f;
            // Corners: 0 bottom-left, 1 top-left, 2 top-right, 3 bottom-right.
            return Vector3.Lerp((c[0] + c[1]) * 0.5f, (c[3] + c[2]) * 0.5f, pct);
        }

        #endregion

        #region Private

        private Color BarColor(float pct) => pct < 0.30f ? _dangerBarColor : _normalBarColor;

        /// <summary>
        /// Drop: ghost holds the old width, then drains to the new one. Gain: ghost jumps to the
        /// new width and the (slower) fill catches up to it.
        /// </summary>
        private void AnimateGhost(float from, float to)
        {
            if (_ghostFill == null)
                return;
            var rt = _ghostFill.rectTransform;
            DOTween.Kill(rt);

            if (!_hasPainted || to >= from)
            {
                _ghostFill.color = _ghostGainColor;
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, to);
                return;
            }

            _ghostFill.color = _ghostLossColor;
            // A drop landing mid-drain keeps draining from wherever the ghost is now.
            float start = Mathf.Max(rt.rect.width, from);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, start);
            DOTween
                .To(
                    () => rt.rect.width,
                    w => rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w),
                    to,
                    _ghostDuration
                )
                .SetDelay(_ghostDelay)
                .SetEase(Ease.InQuad)
                .SetTarget(rt)
                .SetLink(gameObject);
        }

        /// <summary>Rolls the value label toward <paramref name="value"/> instead of snapping.</summary>
        private void CountValueText(int value, int maxValue)
        {
            if (_valueText == null || value == _countTarget)
                return;
            _countTarget = value;
            DOTween.Kill(_valueText);
            if (!_hasPainted || _ghostDuration <= 0f)
            {
                _displayedOpinion = value;
                _valueText.text = $"Opinion: {value} / {maxValue}";
                return;
            }
            DOTween
                .To(
                    () => _displayedOpinion,
                    v =>
                    {
                        _displayedOpinion = v;
                        _valueText.text = $"Opinion: {v} / {maxValue}";
                    },
                    value,
                    _ghostDuration
                )
                .SetEase(Ease.OutCubic)
                .SetTarget(_valueText)
                .SetLink(gameObject);
        }

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
