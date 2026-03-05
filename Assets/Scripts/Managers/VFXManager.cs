using System;
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
    ///   2. <b>Animated Images</b> — spawns pooled instances of a single shared VFX prefab
    ///      onto <see cref="_vfxCanvas"/> at the target element's canvas position, then plays
    ///      the Animator state specified by <see cref="VFXEvent.AnimationStateName"/>.
    ///      Each instance carries a <see cref="VFXAnimatedImage"/> that self-deactivates (via
    ///      an <c>OnAnimationComplete</c> AnimationEvent on the clip's last frame), returning
    ///      it to pool automatically.
    ///
    /// Entry points:
    ///   <see cref="Play(VFXEvent, RectTransform)"/>  — UI element target (most common).
    ///   <see cref="Play(VFXEvent, Transform)"/>       — generic Transform (auto-converts if RectTransform).
    ///   <see cref="PlayAtWorld(VFXEvent, Vector3)"/>  — world-space point converted to canvas space.
    ///   <see cref="PlayAndGetInstance(VFXEvent, RectTransform)"/> — returns spawned VFXAnimatedImage
    ///      so callers can inject a <see cref="BattleVFXContext"/> for animation-event-driven timing.
    ///
    /// Setup:
    ///   • Add to the persistent Managers GameObject alongside AudioManager.
    ///   • Assign <see cref="_vfxCanvas"/> to a Screen Space – Overlay canvas above the game canvas.
    ///   • Assign <see cref="_vfxPrefab"/> to the shared VFX Base prefab (Image + Animator with all clips).
    /// </summary>
    [Debuggable("VFX", LogLevel.Warning)]
    public class VFXManager : Singleton<VFXManager>
    {
        // ─── Inspector Fields ─────────────────────────────────────────────────

        [Header("VFX Canvas")]
        [Tooltip("Screen Space – Overlay canvas that animated VFX images are spawned into.\n" +
                 "Must be sorted above the game UI canvas so effects render on top.")]
        [SerializeField] private Canvas _vfxCanvas;

        [Header("VFX Prefab")]
        [Tooltip("Single shared VFX prefab (Image + Animator) pooled by this manager.\n" +
                 "The Animator Controller must contain all animation clips; each VFXEvent\n" +
                 "specifies which state to play by name.")]
        [SerializeField] private GameObject _vfxPrefab;

        [Header("Pooling")]
        [Tooltip("Instances to pre-instantiate at startup. " +
                 "Avoids Instantiate spikes on the first VFX play during gameplay.")]
        [SerializeField] private int _initialPoolSize = 5;

        // ─── Runtime State ────────────────────────────────────────────────────

        /// <summary>Pool of inactive VFX instances. All instances share the same <see cref="_vfxPrefab"/>.</summary>
        private readonly Queue<GameObject> _pool = new Queue<GameObject>();

        // ─── Lifecycle ────────────────────────────────────────────────────────

        protected override void OnAwake()
        {
            if (_vfxPrefab == null) return;
            for (int i = 0; i < _initialPoolSize; i++)
            {
                var instance = Instantiate(_vfxPrefab);
                instance.SetActive(false);
                _pool.Enqueue(instance);
            }
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
            if (!string.IsNullOrEmpty(evt.AnimationStateName))
                SpawnAnimatedImageAt(evt.AnimationStateName, target, evt.Offset);
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
            if (!string.IsNullOrEmpty(evt.AnimationStateName))
                SpawnAnimatedImageAt(evt.AnimationStateName, null, evt.Offset);
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

            if (!string.IsNullOrEmpty(evt.AnimationStateName))
                SpawnAnimatedImageAtWorld(evt.AnimationStateName, worldPos, evt.Offset);
        }

        /// <summary>
        /// Plays a VFX event and returns the spawned <see cref="VFXAnimatedImage"/> so the caller
        /// can inject a <see cref="BattleVFXContext"/> for animation-event-driven effect timing
        /// (e.g. damage lands at the hit frame rather than immediately on card play).
        /// Returns null if <paramref name="evt"/> has no <see cref="VFXEvent.AnimationStateName"/>.
        /// </summary>
        public VFXAnimatedImage PlayAndGetInstance(VFXEvent evt, RectTransform target)
        {
            if (evt == null) return null;
            PlayFeel(evt, target);
            if (string.IsNullOrEmpty(evt.AnimationStateName)) return null;
            return SpawnAnimatedImageAt(evt.AnimationStateName, target, evt.Offset);
        }

        public VFXAnimatedImage PlayAndSetInstance(VFXEvent evt, RectTransform target, BattleVFXContext context)
        {
            if (evt == null) return null;
            PlayFeel(evt, target);
            if (string.IsNullOrEmpty(evt.AnimationStateName)) return null;
            return SpawnAnimatedImageAt(evt.AnimationStateName, target, evt.Offset, context);
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
        /// Spawns an animated UI image instance on the VFX canvas, positioned at the target's canvas location,
        /// and plays the specified Animator state. Returns the spawned <see cref="VFXAnimatedImage"/>, or null.
        ///
        /// Positioning uses a lightweight pivot <see cref="RectTransform"/> placed at the target position.
        /// The VFX instance is parented to that pivot at (0,0), so Animator position curves baked into the
        /// clip animate relative to the spawn point rather than overwriting the canvas-space placement we compute.
        /// </summary>
        private VFXAnimatedImage SpawnAnimatedImageAt(string stateName, RectTransform uiTarget, Vector2 pixelOffset)
        {
            if (_vfxCanvas == null)
            {
                GameLogger.LogWarning("VFX", "VFXManager: _vfxCanvas is not assigned — cannot spawn animated image.");
                return null;
            }

            GameObject instance = GetFromPool();
            if (instance == null) return null;
            
            instance.transform.SetParent(uiTarget.transform, worldPositionStays: false);
            instance.transform.SetAsLastSibling();
            instance.transform.localPosition = Vector3.zero;
            
            return ActivateInstance(instance, stateName);
        }
        
        private VFXAnimatedImage SpawnAnimatedImageAt(string stateName, RectTransform uiTarget, Vector2 pixelOffset, BattleVFXContext context)
        {
            if (_vfxCanvas == null)
            {
                GameLogger.LogWarning("VFX", "VFXManager: _vfxCanvas is not assigned — cannot spawn animated image.");
                return null;
            }

            GameObject instance = GetFromPool();
            if (instance == null) return null;
            
            instance.transform.SetParent(uiTarget.transform, worldPositionStays: false);
            instance.transform.SetAsLastSibling();
            
            return ActivateInstance(instance, stateName, ctx: context);
        }

        /// <summary>
        /// Spawns an animated image at a world-space position, converting it to canvas space first.
        /// Uses the same pivot-wrapper pattern as <see cref="SpawnAnimatedImageAt"/>.
        /// </summary>
        private VFXAnimatedImage SpawnAnimatedImageAtWorld(string stateName, Vector3 worldPos, Vector2 pixelOffset)
        {
            if (_vfxCanvas == null)
            {
                GameLogger.LogWarning("VFX", "VFXManager: _vfxCanvas is not assigned — cannot spawn animated image.");
                return null;
            }

            GameObject instance = GetFromPool();
            if (instance == null) return null;

            Camera cam = _vfxCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null : _vfxCanvas.worldCamera;

            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_vfxCanvas.transform, screenPos, cam, out Vector2 localPos);

            GameObject pivot   = new GameObject("VFXPivot");
            var        pivotRT = pivot.AddComponent<RectTransform>();
            pivotRT.SetParent(_vfxCanvas.transform, worldPositionStays: false);
            pivotRT.anchorMin = pivotRT.anchorMax = new Vector2(0.5f, 0.5f);
            pivotRT.pivot     = new Vector2(0.5f, 0.5f);
            pivotRT.sizeDelta = Vector2.zero;
            pivotRT.anchoredPosition = localPos + pixelOffset;

            instance.transform.SetParent(pivot.transform, worldPositionStays: false);
            RectTransform rt = instance.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }

            return ActivateInstance(instance, stateName, pivot);
        }

        /// <summary>
        /// Activates a pooled instance, wires the pool-return callback, and plays the named animation state.
        /// Falls back to a timed coroutine if the instance has no <see cref="VFXAnimatedImage"/> component.
        /// <paramref name="pivot"/> is the lightweight wrapper created by the spawn methods; it is
        /// destroyed and the instance re-parented to null when the animation completes.
        /// </summary>
        private VFXAnimatedImage ActivateInstance(GameObject instance, string stateName, GameObject pivot = null, BattleVFXContext ctx = null)
        {
            var controller = instance.GetComponent<VFXAnimatedImage>();
            if (controller != null)
            {
                controller.OnComplete = () =>
                {
                    // Detach from pivot before pooling so the instance can be re-parented next spawn.
                    instance.transform.SetParent(null);
                    if (pivot != null) Destroy(pivot);
                    ReturnToPool(instance);
                };
            }
            
            if(ctx != null)
                controller.SetBattleContext(ctx);

            instance.SetActive(true);

            // Play the specific animation state from time 0 (after SetActive so the Animator is awake).
            controller?.PlayAnimation(stateName);

            // Fallback: if no VFXAnimatedImage, estimate duration from the Animator and use a coroutine.
            if (controller == null)
            {
                float duration = GetStateDuration(instance.GetComponent<Animator>(), stateName);
                StartCoroutine(ReturnAfterDelay(instance, duration, pivot));
            }

            return controller;
        }

        // ─── Internal — Duration / Pool ───────────────────────────────────────

        /// <summary>
        /// Finds the length of the clip matching <paramref name="stateName"/> in the Animator's controller.
        /// Returns 1 second as a safe default if the clip is not found.
        /// </summary>
        private static float GetStateDuration(Animator anim, string stateName)
        {
            if (anim == null || anim.runtimeAnimatorController == null) return 1f;
            foreach (var clip in anim.runtimeAnimatorController.animationClips)
                if (clip.name == stateName) return clip.length;
            return 1f;
        }

        /// <summary>
        /// Runs <paramref name="callback"/> on the next frame.
        /// Used by <see cref="VFXAnimatedImage"/> to defer pool-return <c>SetParent</c> calls
        /// that would throw if invoked while a parent GameObject is mid-activation/deactivation.
        /// </summary>
        internal void DeferredCallback(System.Action callback)
        {
            if (callback == null) return;
            StartCoroutine(DeferOneFrame(callback));
        }

        private IEnumerator DeferOneFrame(System.Action callback)
        {
            yield return null;
            callback?.Invoke();
        }

        private void ReturnToPool(GameObject instance)
        {
            if (instance == null) return;
            instance.SetActive(false);
            instance.transform.SetParent(_vfxCanvas.transform);
            _pool.Enqueue(instance);
        }

        private IEnumerator ReturnAfterDelay(GameObject instance, float delay, GameObject pivot = null)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            instance.transform.SetParent(null);
            if (pivot != null) Destroy(pivot);
            ReturnToPool(instance);
        }

        private GameObject GetFromPool()
        {
            // Drain stale (destroyed) or already-active entries before returning a candidate.
            while (_pool.Count > 0)
            {
                var candidate = _pool.Dequeue();
                if (candidate != null && !candidate.activeSelf)
                    return candidate;
            }

            // Nothing available — instantiate a fresh instance from the shared prefab.
            if (_vfxPrefab == null)
            {
                GameLogger.LogWarning("VFX", "VFXManager: _vfxPrefab is not assigned — cannot spawn VFX.");
                return null;
            }
            return Instantiate(_vfxPrefab);
        }
    }
}
