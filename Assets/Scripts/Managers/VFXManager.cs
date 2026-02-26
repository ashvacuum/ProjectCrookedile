using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crookedile.Core;
using Crookedile.Utilities;
using Crookedile.Data.VFX;
using Crookedile.UI;

namespace Crookedile.Managers
{
    /// <summary>
    /// Centralised VFX service for UI-space visual effects. Handles two types:
    ///
    ///   1. <b>Feel feedbacks</b> — delegates to <see cref="FeedbackManager"/> for shake,
    ///      scale-punch, and tween animations on existing UI elements (RectTransform-aware).
    ///
    ///   2. <b>Animated Images</b> — spawns pooled UI prefabs (Image + Animator) onto
    ///      <see cref="_vfxCanvas"/> at the target element's canvas position. Each prefab
    ///      carries a <see cref="VFXAnimatedImage"/> component that self-deactivates (via
    ///      AnimationEvent) when its clip ends, returning it to pool automatically.
    ///
    /// Entry points:
    ///   <see cref="Play(VFXEvent, RectTransform)"/>  — UI element target (most common).
    ///   <see cref="Play(VFXEvent, Transform)"/>       — generic Transform (auto-converts if RectTransform).
    ///   <see cref="PlayAtWorld(VFXEvent, Vector3)"/>  — world-space point converted to canvas space.
    ///
    /// Setup:
    ///   • Add to the persistent Managers GameObject alongside AudioManager.
    ///   • Assign <see cref="_vfxCanvas"/> to a Screen Space – Overlay canvas that sits above the game canvas.
    /// </summary>
    [Debuggable("VFX", LogLevel.Warning)]
    public class VFXManager : Singleton<VFXManager>
    {
        // ─── Inspector Fields ─────────────────────────────────────────────────

        [Header("VFX Canvas")]
        [Tooltip("Screen Space – Overlay canvas that animated VFX images are spawned into.\n" +
                 "Must be sorted above the game UI canvas so effects render on top.")]
        [SerializeField] private Canvas _vfxCanvas;

        // ─── Runtime State ────────────────────────────────────────────────────

        /// <summary>Per-prefab object pool. Reuses inactive instances before instantiating new ones.</summary>
        private readonly Dictionary<GameObject, Queue<GameObject>> _pools
            = new Dictionary<GameObject, Queue<GameObject>>();

        // ─── Lifecycle ────────────────────────────────────────────────────────

        protected override void OnAwake()
        {
            // Pool is populated lazily the first time each prefab is requested.
        }

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Plays a VFX event aimed at a UI element.
        /// Feel feedback targets the element; animated image spawns at its canvas position.
        /// </summary>
        public void Play(VFXEvent evt, RectTransform target)
        {
            if (evt == null) return;
            PlayFeel(evt, target);
            if (evt.AnimatedPrefab != null)
                SpawnAnimatedImageAt(evt.AnimatedPrefab, target, evt.Offset);
        }

        /// <summary>
        /// Plays a VFX event aimed at a generic Transform.
        /// Automatically delegates to <see cref="Play(VFXEvent, RectTransform)"/> if target is a RectTransform.
        /// </summary>
        public void Play(VFXEvent evt, Transform target)
        {
            if (evt == null) return;

            // Redirect to the RectTransform overload — it handles canvas-space placement correctly.
            if (target is RectTransform rt) { Play(evt, rt); return; }

            // For non-UI transforms: play Feel (no position override) and spawn at canvas center.
            PlayFeel(evt, target);
            if (evt.AnimatedPrefab != null)
                SpawnAnimatedImageAt(evt.AnimatedPrefab, null, evt.Offset);
        }

        /// <summary>
        /// Plays a VFX event at an explicit world-space position.
        /// The world point is converted to canvas space; Feel plays without a transform target.
        /// </summary>
        public void PlayAtWorld(VFXEvent evt, Vector3 worldPos)
        {
            if (evt == null) return;

            if (!string.IsNullOrEmpty(evt.FeedbackId))
                FeedbackManager.Instance?.Play(evt.FeedbackId);

            if (evt.AnimatedPrefab != null)
                SpawnAnimatedImageAtWorld(evt.AnimatedPrefab, worldPos, evt.Offset);
        }

        // ─── Internal — Feel ──────────────────────────────────────────────────

        private void PlayFeel(VFXEvent evt, Transform target)
        {
            if (string.IsNullOrEmpty(evt.FeedbackId) || FeedbackManager.Instance == null) return;

            if (target != null)
                FeedbackManager.Instance.Play(evt.FeedbackId, target);
            else
                FeedbackManager.Instance.Play(evt.FeedbackId);
        }

        // ─── Internal — Animated Image Spawn ─────────────────────────────────

        /// <summary>
        /// Spawns an animated UI image instance on the VFX canvas, positioned at the target's canvas location.
        /// </summary>
        private void SpawnAnimatedImageAt(GameObject prefab, RectTransform uiTarget, Vector2 pixelOffset)
        {
            if (_vfxCanvas == null)
            {
                GameLogger.LogWarning("VFX", "VFXManager: _vfxCanvas is not assigned — cannot spawn animated image.");
                return;
            }

            GameObject instance = GetFromPool(prefab);
            instance.transform.SetParent(_vfxCanvas.transform, worldPositionStays: false);

            RectTransform rt = instance.GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector2 canvasLocalPos = Vector2.zero;

                if (uiTarget != null)
                {
                    // Convert the target's world position → screen space → VFX canvas local space.
                    // Using null camera for Screen Space – Overlay; the canvas's worldCamera otherwise.
                    Camera cam = _vfxCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                        ? null
                        : _vfxCanvas.worldCamera;

                    Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, uiTarget.position);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        (RectTransform)_vfxCanvas.transform, screenPos, cam, out canvasLocalPos);
                }

                rt.anchoredPosition = canvasLocalPos + pixelOffset;
            }

            ActivateInstance(instance, prefab);
        }

        /// <summary>
        /// Spawns an animated image at a world-space position, converting it to canvas space first.
        /// </summary>
        private void SpawnAnimatedImageAtWorld(GameObject prefab, Vector3 worldPos, Vector2 pixelOffset)
        {
            if (_vfxCanvas == null)
            {
                GameLogger.LogWarning("VFX", "VFXManager: _vfxCanvas is not assigned — cannot spawn animated image.");
                return;
            }

            Camera cam = _vfxCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _vfxCanvas.worldCamera;

            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

            GameObject instance = GetFromPool(prefab);
            instance.transform.SetParent(_vfxCanvas.transform, worldPositionStays: false);

            RectTransform rt = instance.GetComponent<RectTransform>();
            if (rt != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)_vfxCanvas.transform, screenPos, cam, out Vector2 localPos);
                rt.anchoredPosition = localPos + pixelOffset;
            }

            ActivateInstance(instance, prefab);
        }

        /// <summary>
        /// Activates a pooled instance and wires pool-return logic.
        /// Prefers <see cref="VFXAnimatedImage.OnComplete"/> callback (driven by AnimationEvent).
        /// Falls back to a timed coroutine reading the first clip's length from the Animator.
        /// </summary>
        private void ActivateInstance(GameObject instance, GameObject prefab)
        {
            // Wire the self-disabling callback if the prefab has VFXAnimatedImage.
            var controller = instance.GetComponent<VFXAnimatedImage>();
            if (controller != null)
            {
                controller.OnComplete = () => ReturnToPool(instance, prefab);
            }

            instance.SetActive(true);

            // Fallback: if no VFXAnimatedImage, use clip duration from the Animator.
            if (controller == null)
            {
                float duration = GetClipDuration(instance.GetComponent<Animator>());
                StartCoroutine(ReturnAfterDelay(instance, prefab, duration));
            }
        }

        // ─── Internal — Duration / Pool ───────────────────────────────────────

        /// <summary>
        /// Reads the length of the first animation clip in the Animator's controller.
        /// Returns 1 second as a safe default if no clip is found.
        /// </summary>
        private static float GetClipDuration(Animator anim)
        {
            if (anim == null || anim.runtimeAnimatorController == null) return 1f;
            var clips = anim.runtimeAnimatorController.animationClips;
            return clips.Length > 0 ? clips[0].length : 1f;
        }

        private void ReturnToPool(GameObject instance, GameObject prefab)
        {
            if (instance == null) return;
            instance.SetActive(false);
            if (_pools.TryGetValue(prefab, out var queue))
                queue.Enqueue(instance);
        }

        private IEnumerator ReturnAfterDelay(GameObject instance, GameObject prefab, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            ReturnToPool(instance, prefab);
        }

        private GameObject GetFromPool(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out var queue))
            {
                queue = new Queue<GameObject>();
                _pools[prefab] = queue;
            }

            // Drain until we find a valid inactive instance.
            while (queue.Count > 0)
            {
                var candidate = queue.Dequeue();
                if (candidate != null && !candidate.activeSelf)
                    return candidate;
            }

            // Nothing available — instantiate a fresh one.
            return Instantiate(prefab);
        }
    }
}
