using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Sweeps a gradient Image across a card to produce a gloss/shine highlight.
    ///
    /// No shader involved: the shine is a plain UI Image (a soft diagonal white gradient)
    /// parented under a <see cref="RectMask2D"/>, tweened from fully off one edge to fully
    /// off the other. The mask is what turns a sliding rectangle into a shine.
    ///
    /// Use <see cref="Play"/> for one-shot highlights (card drawn, card upgraded, reward
    /// revealed) and <see cref="_loopInterval"/> for an idle shimmer on rare cards.
    ///
    /// Setup:
    ///   1. Add an empty child to the card sized to the card art, NOT the card root — a
    ///      RectMask2D on the root would also clip CardButton's selection outline.
    ///   2. Add this component to that child (RectMask2D is added automatically).
    ///   3. Add a gradient Image as a child of it and assign it to <see cref="_shine"/>.
    ///      Rotate it ~20-30° for the classic diagonal sweep.
    /// </summary>
    [RequireComponent(typeof(RectMask2D))]
    public class CardShine : MonoBehaviour
    {
        #region Inspector
        [Tooltip("The gradient Image swept across the mask. Must be a child of this object.")]
        [SerializeField]
        private RectTransform _shine;

        [Tooltip("Seconds for one edge-to-edge sweep.")]
        [SerializeField]
        private float _duration = 0.5f;

        [Tooltip("Sweep once as soon as the card becomes visible.")]
        [SerializeField]
        private bool _playOnEnable;

        [Tooltip(
            "Seconds of pause between sweeps for a looping idle shimmer (rare/upgraded cards).\n"
                + "Set to 0 or less for a one-shot sweep driven by Play() instead."
        )]
        [SerializeField]
        private float _loopInterval;

        [SerializeField]
        private Ease _ease = Ease.InOutSine;

        #endregion

        private Tween _tween;

        #region Lifecycle
        private void Awake()
        {
            // A raycast-target shine sits on top of the card and eats its clicks. Forced here
            // rather than left to the prefab, because the symptom (dead cards) looks nothing
            // like the cause.
            if (_shine != null && _shine.TryGetComponent<Graphic>(out var g))
                g.raycastTarget = false;
            SetHidden();
        }

        private void OnEnable()
        {
            if (_playOnEnable || _loopInterval > 0f)
                Play();
        }

        private void OnDisable() => Stop();

        #endregion

        #region API
        /// <summary>
        /// Runs a sweep. Loops with <see cref="_loopInterval"/> between passes when that is
        /// positive, otherwise plays once. Restarts cleanly if already running.
        /// </summary>
        public void Play()
        {
            if (_shine == null)
                return;

            Stop();

            // Travel far enough that the gradient starts and ends fully outside the mask,
            // so no hard edge is ever visible inside it.
            float half = (((RectTransform)transform).rect.width + _shine.rect.width) * 0.5f;
            _shine.anchoredPosition = new Vector2(-half, _shine.anchoredPosition.y);

            _shine.gameObject.SetActive(true);
            _tween = _shine
                .DOAnchorPosX(half, _duration)
                .SetEase(_ease)
                .SetLink(gameObject);

            if (_loopInterval > 0f)
                _tween = DOTween
                    .Sequence()
                    .SetLink(gameObject)
                    .Append(_tween)
                    .AppendInterval(_loopInterval)
                    .SetLoops(-1);
            else
                _tween.OnComplete(SetHidden);
        }

        /// <summary>Stops any running sweep and parks the gradient off-mask.</summary>
        public void Stop()
        {
            _tween?.Kill();
            _tween = null;
            SetHidden();
        }

        #endregion

        /// <summary>
        /// Hides the gradient at rest. Deactivates rather than repositioning, because in Awake
        /// the mask's rect can still be zero-width before the first layout pass — parking by
        /// position would leave the shine sitting visible in the middle of the card.
        /// </summary>
        private void SetHidden()
        {
            if (_shine != null)
                _shine.gameObject.SetActive(false);
        }
    }
}
