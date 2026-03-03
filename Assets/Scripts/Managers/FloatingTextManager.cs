using System.Collections.Generic;
using UnityEngine;
using Crookedile.Core;
using Crookedile.UI;
using Crookedile.Utilities;

namespace Crookedile.Managers
{
    /// <summary>
    /// Spawns pooled floating text instances (e.g. damage numbers) on the VFX canvas.
    /// Positioning uses the same WorldToScreen → ScreenToCanvasLocal math as <see cref="VFXManager"/>
    /// so text aligns precisely with VFX effects on the same canvas.
    ///
    /// Setup:
    ///   • Add to the persistent Managers GameObject alongside VFXManager and AudioManager.
    ///   • Assign <see cref="_vfxCanvas"/> to the same Screen Space – Overlay canvas used by VFXManager.
    ///   • Assign <see cref="_floatingTextPrefab"/> to a prefab with a <see cref="FloatingText"/> component.
    /// </summary>
    [Debuggable("FloatingText", LogLevel.Warning)]
    public class FloatingTextManager : Singleton<FloatingTextManager>
    {
        // ─── Inspector Fields ─────────────────────────────────────────────────

        [Header("Canvas")]
        [Tooltip("Screen Space – Overlay canvas that floating text is spawned into. " +
                 "Should be the same canvas used by VFXManager.")]
        [SerializeField] private Canvas _vfxCanvas;

        [Header("Prefab")]
        [Tooltip("Prefab with a FloatingText component (TMP_Text child + RectTransform).")]
        [SerializeField] private GameObject _floatingTextPrefab;

        [Header("Pooling")]
        [Tooltip("Instances pre-instantiated at startup to avoid Instantiate spikes during play.")]
        [SerializeField] private int _initialPoolSize = 10;

        // ─── Runtime State ────────────────────────────────────────────────────

        private readonly Queue<GameObject> _pool = new Queue<GameObject>();

        // ─── Lifecycle ────────────────────────────────────────────────────────

        protected override void OnAwake()
        {
            if (_floatingTextPrefab == null) return;
            for (int i = 0; i < _initialPoolSize; i++)
            {
                var instance = Instantiate(_floatingTextPrefab);
                instance.SetActive(false);
                _pool.Enqueue(instance);
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Spawns a floating text label at <paramref name="target"/>'s canvas position.
        /// The text animates upward and fades out, then returns to pool automatically.
        /// No-op when required references are missing.
        /// </summary>
        public void Show(string text, RectTransform target, Color color)
        {
            if (_vfxCanvas == null || _floatingTextPrefab == null) return;

            GameObject instance = GetFromPool();
            if (instance == null) return;

            instance.transform.SetParent(_vfxCanvas.transform, worldPositionStays: false);

            // Position on the canvas using the same conversion used by VFXManager.
            var rt = instance.GetComponent<RectTransform>();
            if (rt != null)
            {
                Camera cam = _vfxCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : _vfxCanvas.worldCamera;

                Vector2 canvasLocalPos = Vector2.zero;
                if (target != null)
                {
                    Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, target.position);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        (RectTransform)_vfxCanvas.transform, screenPos, cam, out canvasLocalPos);
                }
                rt.anchoredPosition = canvasLocalPos;
            }

            instance.SetActive(true);
            var floatingText = instance.GetComponent<FloatingText>();
            if (floatingText != null)
            {
                floatingText.OnComplete = () => ReturnToPool(instance);
                floatingText.Animate(text, color);
            }
            else
            {
                ReturnToPool(instance);
            }
        }

        // ─── Pool Helpers ─────────────────────────────────────────────────────

        private GameObject GetFromPool()
        {
            while (_pool.Count > 0)
            {
                var candidate = _pool.Dequeue();
                if (candidate != null) return candidate;
            }
            // Pool exhausted — instantiate a fresh instance.
            return _floatingTextPrefab != null ? Instantiate(_floatingTextPrefab) : null;
        }

        private void ReturnToPool(GameObject instance)
        {
            if (instance == null) return;
            instance.SetActive(false);
            _pool.Enqueue(instance);
        }
    }
}
