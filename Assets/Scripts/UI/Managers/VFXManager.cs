using System;
using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Data.VFX;
using Crookedile.UI;
using Crookedile.Utilities;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Crookedile.Managers
{
    /// <summary>
    /// Centralised VFX service for UI-space visual effects. Spawns pooled instances of a
    /// single shared VFX prefab onto <see cref="_vfxCanvas"/> at the target element's canvas
    /// position, then plays the Animator state specified by <see cref="VFXEvent.AnimationStateName"/>.
    ///      Each instance carries a <see cref="VFXAnimatedImage"/> whose playback-driver
    ///      coroutine self-deactivates it at the clip's end (code-timed — no AnimationEvents
    ///      in clips), returning it to pool automatically.
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
        #region Inspector Fields
        [Header("VFX Canvas")]
        [Tooltip(
            "Screen Space – Overlay canvas that animated VFX images are spawned into.\n"
                + "Must be sorted above the game UI canvas so effects render on top."
        )]
        [SerializeField]
        private Canvas _vfxCanvas;

        [Header("VFX Prefab")]
        [Tooltip(
            "Single shared VFX prefab (Image + Animator) pooled by this manager.\n"
                + "The Animator Controller must contain all animation clips; each VFXEvent\n"
                + "specifies which state to play by name."
        )]
        [SerializeField]
        private GameObject _vfxPrefab;

        [Header("Pooling")]
        [Tooltip(
            "Instances to pre-instantiate at startup. "
                + "Avoids Instantiate spikes on the first VFX play during gameplay."
        )]
        [SerializeField]
        private int _initialPoolSize = 5;

        #endregion

        #region Runtime State
        /// <summary>Pool of inactive VFX instances. All instances share the same <see cref="_vfxPrefab"/>.</summary>
        private readonly Queue<GameObject> _pool = new Queue<GameObject>();

        #endregion

        #region Lifecycle
        protected override void OnAwake()
        {
            if (_vfxPrefab == null)
                return;
            for (int i = 0; i < _initialPoolSize; i++)
            {
                // Parent to the VFX canvas immediately so instances are always canvas children at rest.
                var instance =
                    _vfxCanvas != null
                        ? Instantiate(_vfxPrefab, _vfxCanvas.transform)
                        : Instantiate(_vfxPrefab);
                instance.SetActive(false);
                _pool.Enqueue(instance);
            }
        }

        #endregion

        #region Public API
        /// <summary>
        /// Plays a VFX event aimed at a UI element.
        /// Feel feedback targets the element; animated image spawns at its canvas position.
        /// </summary>
        public void Play(VFXEvent evt, RectTransform target)
        {
            if (evt == null)
                return;
            if (!string.IsNullOrEmpty(evt.AnimationStateName))
                SpawnAnimatedImageAt(evt.AnimationStateName, target, evt.Offset, evt.HitTimeNormalized);
        }

        /// <summary>
        /// Plays a VFX event aimed at a generic Transform.
        /// Automatically delegates to <see cref="Play(VFXEvent, RectTransform)"/> if target is a RectTransform.
        /// </summary>
        public void Play(VFXEvent evt, Transform target)
        {
            if (evt == null)
                return;
            if (target is RectTransform rt)
            {
                Play(evt, rt);
                return;
            }
            if (!string.IsNullOrEmpty(evt.AnimationStateName))
                SpawnAnimatedImageAt(evt.AnimationStateName, null, evt.Offset, evt.HitTimeNormalized);
        }

        /// <summary>Plays a VFX event at an explicit world-space position.</summary>
        public void PlayAtWorld(VFXEvent evt, Vector3 worldPos)
        {
            if (evt == null)
                return;
            if (!string.IsNullOrEmpty(evt.AnimationStateName))
                SpawnAnimatedImageAtWorld(
                    evt.AnimationStateName,
                    worldPos,
                    evt.Offset,
                    evt.HitTimeNormalized
                );
        }

        /// <summary>
        /// Plays a VFX event and returns the spawned <see cref="VFXAnimatedImage"/> so the caller
        /// can inject a <see cref="BattleVFXContext"/> for animation-event-driven effect timing.
        /// Returns null if <paramref name="evt"/> has no <see cref="VFXEvent.AnimationStateName"/>.
        /// </summary>
        public VFXAnimatedImage PlayAndGetInstance(VFXEvent evt, RectTransform target)
        {
            if (evt == null)
                return null;
            if (string.IsNullOrEmpty(evt.AnimationStateName))
                return null;
            return SpawnAnimatedImageAt(
                evt.AnimationStateName,
                target,
                evt.Offset,
                evt.HitTimeNormalized
            );
        }

        public VFXAnimatedImage PlayAndSetInstance(
            VFXEvent evt,
            RectTransform target,
            BattleVFXContext context
        )
        {
            if (evt == null)
                return null;
            if (string.IsNullOrEmpty(evt.AnimationStateName))
                return null;
            return SpawnAnimatedImageAt(
                evt.AnimationStateName,
                target,
                evt.Offset,
                evt.HitTimeNormalized,
                context
            );
        }

        #region Internal — Animated Image Spawn
        /// <summary>
        /// Spawns an animated UI image instance parented to <paramref name="uiTarget"/> (or the VFX
        /// canvas root if no target is given), zeroes its local position, and plays the named state.
        /// The optional <paramref name="context"/> is forwarded to <see cref="VFXAnimatedImage.SetBattleContext"/>
        /// so animation-event-driven or coroutine-driven completion can fire battle callbacks in sync.
        /// </summary>
        private VFXAnimatedImage SpawnAnimatedImageAt(
            string stateName,
            RectTransform uiTarget,
            Vector2 pixelOffset,
            float hitTimeNormalized,
            BattleVFXContext context = null
        )
        {
            if (_vfxCanvas == null)
            {
                GameLogger.LogWarning(
                    "VFX",
                    "VFXManager: _vfxCanvas is not assigned — cannot spawn animated image."
                );
                return null;
            }

            GameObject instance = GetFromPool();
            if (instance == null)
                return null;

            Transform parent = uiTarget != null ? uiTarget.transform : _vfxCanvas.transform;
            instance.transform.SetParent(parent, worldPositionStays: false);
            instance.transform.SetAsLastSibling();
            instance.transform.localPosition = Vector3.zero;

            return ActivateInstance(instance, stateName, hitTimeNormalized, ctx: context);
        }

        /// <summary>
        /// Spawns an animated image at a world-space position, converting it to canvas space first.
        /// Uses the same pivot-wrapper pattern as <see cref="SpawnAnimatedImageAt"/>.
        /// </summary>
        private VFXAnimatedImage SpawnAnimatedImageAtWorld(
            string stateName,
            Vector3 worldPos,
            Vector2 pixelOffset,
            float hitTimeNormalized
        )
        {
            if (_vfxCanvas == null)
            {
                GameLogger.LogWarning(
                    "VFX",
                    "VFXManager: _vfxCanvas is not assigned — cannot spawn animated image."
                );
                return null;
            }

            GameObject instance = GetFromPool();
            if (instance == null)
                return null;

            Camera cam =
                _vfxCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : _vfxCanvas.worldCamera;

            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_vfxCanvas.transform,
                screenPos,
                cam,
                out Vector2 localPos
            );

            GameObject pivot = new GameObject("VFXPivot");
            var pivotRT = pivot.AddComponent<RectTransform>();
            pivotRT.SetParent(_vfxCanvas.transform, worldPositionStays: false);
            pivotRT.anchorMin = pivotRT.anchorMax = new Vector2(0.5f, 0.5f);
            pivotRT.pivot = new Vector2(0.5f, 0.5f);
            pivotRT.sizeDelta = Vector2.zero;
            pivotRT.anchoredPosition = localPos + pixelOffset;

            instance.transform.SetParent(pivot.transform, worldPositionStays: false);
            RectTransform rt = instance.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }

            return ActivateInstance(instance, stateName, hitTimeNormalized, pivot);
        }

        /// <summary>
        /// Activates a pooled instance, wires the pool-return callback, and plays the named animation state.
        /// Falls back to a timed coroutine if the instance has no <see cref="VFXAnimatedImage"/> component.
        /// <paramref name="pivot"/> is the lightweight wrapper created by the spawn methods; it is
        /// destroyed and the instance re-parented to null when the animation completes.
        /// </summary>
        private VFXAnimatedImage ActivateInstance(
            GameObject instance,
            string stateName,
            float hitTimeNormalized,
            GameObject pivot = null,
            BattleVFXContext ctx = null
        )
        {
            var controller = instance.GetComponent<VFXAnimatedImage>();
            if (controller != null)
            {
                controller.OnComplete = () =>
                {
                    if (pivot != null)
                        Destroy(pivot);
                    ReturnToPool(instance);
                };

                // SetBattleContext must be called AFTER OnComplete is wired (it chains on top of it)
                // and must be INSIDE the null guard — calling it on a null controller would crash
                // and leave _vfxInFlight permanently true.
                if (ctx != null)
                    controller.SetBattleContext(ctx);
            }
            else if (ctx != null)
            {
                // No VFXAnimatedImage component — resolve battle context immediately so
                // _vfxInFlight is never left stuck by a missing component on the prefab.
                GameLogger.LogWarning(
                    "VFX",
                    $"VFX instance '{instance.name}' has no VFXAnimatedImage component — resolving battle context immediately."
                );
                ctx.OnApplyEffects?.Invoke();
                ctx.OnComplete?.Invoke();
            }

            instance.SetActive(true);

            // Play the specific animation state from time 0 (after SetActive so the Animator is awake).
            controller?.PlayAnimation(stateName, hitTimeNormalized);

            // Fallback pool-return: if no VFXAnimatedImage, use a timed task to return the instance.
            if (controller == null)
            {
                float duration = GetStateDuration(instance.GetComponent<Animator>(), stateName);
                ReturnAfterDelay(instance, duration, pivot).Forget();
            }

            return controller;
        }

        #endregion

        #region Internal — Duration / Pool
        /// <summary>
        /// Finds the length of the clip matching <paramref name="stateName"/> in the Animator's controller.
        /// Returns 1 second as a safe default if the clip is not found.
        /// </summary>
        private static float GetStateDuration(Animator anim, string stateName)
        {
            if (anim == null || anim.runtimeAnimatorController == null)
                return 1f;
            foreach (var clip in anim.runtimeAnimatorController.animationClips)
                if (clip.name == stateName)
                    return clip.length;
            return 1f;
        }

        /// <summary>
        /// Runs <paramref name="callback"/> on the next frame.
        /// Used by <see cref="VFXAnimatedImage"/> to defer pool-return <c>SetParent</c> calls
        /// that would throw if invoked while a parent GameObject is mid-activation/deactivation.
        /// </summary>
        internal void DeferredCallback(System.Action callback)
        {
            if (callback == null)
                return;
            DeferOneFrame(callback).Forget();
        }

        private async UniTaskVoid DeferOneFrame(System.Action callback)
        {
            await UniTask.NextFrame(this.GetCancellationTokenOnDestroy());
            callback?.Invoke();
        }

        private void ReturnToPool(GameObject instance)
        {
            if (instance == null)
                return;
            // Parent back to the VFX canvas BEFORE deactivating so the instance is always
            // a canvas child while at rest in the pool (never parented to a gameplay object).
            if (_vfxCanvas != null)
                instance.transform.SetParent(_vfxCanvas.transform);
            instance.SetActive(false);
            _pool.Enqueue(instance);
        }

        private async UniTaskVoid ReturnAfterDelay(
            GameObject instance,
            float delay,
            GameObject pivot = null
        )
        {
            if (delay > 0f)
                await UniTask.WaitForSeconds(
                    delay,
                    cancellationToken: this.GetCancellationTokenOnDestroy()
                );
            if (pivot != null)
                Destroy(pivot);
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
                GameLogger.LogWarning(
                    "VFX",
                    "VFXManager: _vfxPrefab is not assigned — cannot spawn VFX."
                );
                return null;
            }
            return Instantiate(_vfxPrefab);
        }

        #endregion
        #endregion
    }
}
