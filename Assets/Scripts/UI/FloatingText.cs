using UnityEngine;
using TMPro;
using DG.Tweening;

namespace Crookedile.UI
{
    /// <summary>
    /// Animated floating text instance used by <see cref="Managers.FloatingTextManager"/>.
    /// Floats upward and fades out over <see cref="_duration"/> seconds, then invokes
    /// <see cref="OnComplete"/> so the manager can return it to the pool.
    ///
    /// Setup: attach to a prefab that has a <see cref="TMP_Text"/> child and a
    /// <see cref="RectTransform"/>. No external animation libraries required.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;

        [Tooltip("Total time (seconds) the text is visible before fully fading out.")]
        [SerializeField] private float _duration = 1.2f;

        [Tooltip("Pixels the text rises over its lifetime.")]
        [SerializeField] private float _risePixels = 60f;

        [Tooltip("Fraction of _duration spent fading out (0–1). E.g. 0.4 = last 40% fades out).")]
        [SerializeField] private float _fadeFraction = 0.4f;

        /// <summary>Assigned by FloatingTextManager. Invoked when the animation ends so
        /// the instance is returned to pool.</summary>
        internal System.Action OnComplete;

        private RectTransform _rt;

        private void Awake() => _rt = GetComponent<RectTransform>();

        /// <summary>
        /// Sets the text and color then begins the float-up + fade-out animation.
        /// Stops any running animation first so re-activation is safe.
        /// </summary>
        public void Animate(string text, Color color)
        {
            if (_text != null)
            {
                _text.text  = text;
                _text.color = color;
            }

            DOTween.Kill(gameObject);

            Vector2 startPos    = _rt.anchoredPosition;
            Color   startColor  = _text != null ? _text.color : Color.white;
            Color   fadeColor   = new Color(startColor.r, startColor.g, startColor.b, 0f);
            float   fadeStart   = _duration * (1f - _fadeFraction);
            float   fadeDuration = _duration - fadeStart;

            DOTween.Sequence().SetLink(gameObject)
                .Append(_rt.DOAnchorPos(startPos + Vector2.up * _risePixels, _duration).SetEase(Ease.Linear))
                .Insert(fadeStart, DOTween.To(() => _text.color, x => _text.color = x, fadeColor, fadeDuration))
                .OnComplete(Finish);
        }

        private void Finish()
        {
            var callback = OnComplete;
            OnComplete = null;
            gameObject.SetActive(false);
            callback?.Invoke();
        }

        private void OnDisable()
        {
            DOTween.Kill(gameObject);
            OnComplete = null;
        }
    }
}
