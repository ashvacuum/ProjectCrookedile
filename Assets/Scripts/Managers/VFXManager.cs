using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crookedile.Core;
using Crookedile.Utilities;
using Crookedile.Data.VFX;

namespace Crookedile.Managers
{
    /// <summary>
    /// Centralised VFX service. Handles two types of visual effects:
    ///
    ///   1. <b>Feel feedbacks</b> — delegates to <see cref="FeedbackManager"/> for shake,
    ///      scale-punch, and tween animations on UI elements (RectTransform-aware).
    ///
    ///   2. <b>Particle bursts</b> — spawns pooled particle prefabs in world space,
    ///      converting UI RectTransform positions to world space automatically.
    ///
    /// Entry points:
    ///   <see cref="Play(VFXEvent, RectTransform)"/>   — UI element target (most common for battle UI).
    ///   <see cref="Play(VFXEvent, Transform)"/>        — world-space Transform target.
    ///   <see cref="PlayAtWorld(VFXEvent, Vector3)"/>   — explicit world-space position.
    ///
    /// Setup: add this component to the persistent Managers GameObject alongside AudioManager.
    /// Assign <see cref="_uiCamera"/> to the camera that renders your UI canvas, or leave null
    /// to use <c>Camera.main</c>.
    /// </summary>
    [Debuggable("VFX", LogLevel.Warning)]
    public class VFXManager : Singleton<VFXManager>
    {
        // ─── Inspector Fields ─────────────────────────────────────────────────

        [Header("UI Camera")]
        [Tooltip("Camera used to convert RectTransform positions to screen space.\n" +
                 "Leave null to use Camera.main (correct for most setups).")]
        [SerializeField] private Camera _uiCamera;

        // ─── Runtime State ────────────────────────────────────────────────────

        /// <summary>Per-prefab object pool. Reuses inactive particle instances before instantiating new ones.</summary>
        private readonly Dictionary<GameObject, Queue<GameObject>> _pools
            = new Dictionary<GameObject, Queue<GameObject>>();

        // ─── Lifecycle ────────────────────────────────────────────────────────

        protected override void OnAwake()
        {
            // Pool is populated lazily as prefabs are first requested.
        }

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Plays a VFX event aimed at a UI element.
        /// Feel feedback is passed as a RectTransform target; particle is spawned at the
        /// element's screen-converted world position.
        /// </summary>
        public void Play(VFXEvent evt, RectTransform target)
        {
            if (evt == null) return;
            PlayFeel(evt, target);
            if (evt.ParticlePrefab != null)
                SpawnParticleAtUI(evt.ParticlePrefab, target, evt.Offset);
        }

        /// <summary>
        /// Plays a VFX event aimed at a world-space Transform.
        /// If <paramref name="target"/> is a RectTransform, delegates to
        /// <see cref="Play(VFXEvent, RectTransform)"/> for correct canvas-space handling.
        /// </summary>
        public void Play(VFXEvent evt, Transform target)
        {
            if (evt == null) return;

            // Redirect to RectTransform overload if applicable — it handles UI-space conversion.
            if (target is RectTransform rt) { Play(evt, rt); return; }

            PlayFeel(evt, target);
            if (evt.ParticlePrefab != null && target != null)
                SpawnParticleAtWorld(evt.ParticlePrefab, target.position + evt.Offset);
            else if (evt.ParticlePrefab != null)
                SpawnParticleAtWorld(evt.ParticlePrefab, evt.Offset);
        }

        /// <summary>
        /// Plays a VFX event at an explicit world-space position.
        /// Feel plays without a transform target; particles spawn at <paramref name="worldPos"/>.
        /// </summary>
        public void PlayAtWorld(VFXEvent evt, Vector3 worldPos)
        {
            if (evt == null) return;

            // Feel at default position (no transform override).
            if (!string.IsNullOrEmpty(evt.FeedbackId))
                FeedbackManager.Instance?.Play(evt.FeedbackId);

            if (evt.ParticlePrefab != null)
                SpawnParticleAtWorld(evt.ParticlePrefab, worldPos + evt.Offset);
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

        // ─── Internal — Particles ─────────────────────────────────────────────

        /// <summary>
        /// Converts a RectTransform's on-screen position to a world-space point just in front
        /// of the camera, then spawns the particle there.
        ///
        /// Works for Screen Space – Overlay and Screen Space – Camera canvas modes.
        /// For Overlay, the RectTransform position is already in screen pixels;
        /// <c>RectTransformUtility.WorldToScreenPoint</c> with the camera handles both modes correctly.
        /// </summary>
        private void SpawnParticleAtUI(GameObject prefab, RectTransform uiTarget, Vector3 offset)
        {
            if (uiTarget == null) { SpawnParticleAtWorld(prefab, offset); return; }

            Camera cam = _uiCamera != null ? _uiCamera : Camera.main;
            if (cam == null) { SpawnParticleAtWorld(prefab, offset); return; }

            // Convert the RectTransform's world position to screen-space pixel coordinates.
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, uiTarget.position);

            // Project screen position to world space at a depth just in front of the camera.
            Vector3 worldPos = cam.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, cam.nearClipPlane + 1f));

            SpawnParticleAtWorld(prefab, worldPos + offset);
        }

        private void SpawnParticleAtWorld(GameObject prefab, Vector3 worldPos)
        {
            GameObject instance = GetFromPool(prefab);
            instance.transform.position = worldPos;
            instance.SetActive(true);

            var ps = instance.GetComponent<ParticleSystem>();
            float lifetime = ps != null
                ? ps.main.duration + ps.main.startLifetime.constantMax
                : 3f;

            StartCoroutine(ReturnAfterDelay(instance, prefab, lifetime));
        }

        // ─── Internal — Pool ──────────────────────────────────────────────────

        private GameObject GetFromPool(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out var queue))
            {
                queue = new Queue<GameObject>();
                _pools[prefab] = queue;
            }

            // Drain pool until we find a valid inactive instance.
            while (queue.Count > 0)
            {
                var candidate = queue.Dequeue();
                if (candidate != null && !candidate.activeSelf)
                    return candidate;
            }

            // None available — instantiate a fresh one.
            return Instantiate(prefab);
        }

        private IEnumerator ReturnAfterDelay(GameObject instance, GameObject prefab, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            if (instance != null)
            {
                instance.SetActive(false);

                if (_pools.TryGetValue(prefab, out var queue))
                    queue.Enqueue(instance);
            }
        }
    }
}
