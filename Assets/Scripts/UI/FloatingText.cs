using System.Collections;
using UnityEngine;
using TMPro;

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
            StopAllCoroutines();
            StartCoroutine(AnimateRoutine());
        }

        private IEnumerator AnimateRoutine()
        {
            if (_text == null) { Finish(); yield break; }

            Vector2 startPos  = _rt.anchoredPosition;
            Vector2 endPos    = startPos + Vector2.up * _risePixels;
            Color   startColor = _text.color;
            Color   fadeColor  = new Color(startColor.r, startColor.g, startColor.b, 0f);

            float elapsed   = 0f;
            float fadeStart = _duration * (1f - _fadeFraction);

            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _duration);

                _rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

                if (elapsed >= fadeStart)
                {
                    float fadeT = Mathf.Clamp01((elapsed - fadeStart) / (_duration - fadeStart));
                    _text.color = Color.Lerp(startColor, fadeColor, fadeT);
                }

                yield return null;
            }

            Finish();
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
            StopAllCoroutines();
            OnComplete = null;
        }
    }
}
