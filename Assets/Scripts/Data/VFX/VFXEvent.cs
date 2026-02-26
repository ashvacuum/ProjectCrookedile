using UnityEngine;
using Crookedile.Managers;

namespace Crookedile.Data.VFX
{
    /// <summary>
    /// Scriptable Event — describes a visual effect that combines an optional Feel feedback
    /// (shake, punch, tween) with an optional particle burst.
    ///
    /// Either field can be left empty; the system no-ops cleanly for whichever is absent.
    ///
    /// Usage:
    ///   myVfxEvent.Play()               — no positional target
    ///   myVfxEvent.Play(rectTransform)  — aimed at a UI element (canvas-space converted)
    ///   myVfxEvent.Play(worldPos)       — aimed at an explicit world-space point
    ///
    /// Create via:  Assets → Create → Crookedile → VFX → VFX Event
    /// </summary>
    [CreateAssetMenu(menuName = "Crookedile/VFX/VFX Event", fileName = "NewVFXEvent")]
    public class VFXEvent : ScriptableObject
    {
        [Header("Feel Feedback")]
        [Tooltip("ID of a named MMF_Player registered in FeedbackManager. Leave empty to skip Feel.")]
        [SerializeField] private string _feedbackId;

        [Header("Particles")]
        [Tooltip("Particle prefab to spawn at the target position. Leave null to skip particles.")]
        [SerializeField] private GameObject _particlePrefab;

        [Tooltip("World-space offset added to the target position before spawning particles.")]
        [SerializeField] private Vector3 _offset = Vector3.zero;

        // ─── Properties ───────────────────────────────────────────────────────

        /// <summary>Named Feel player ID passed to <see cref="FeedbackManager.Play(string,Transform)"/>.</summary>
        public string FeedbackId => _feedbackId;

        /// <summary>Particle system prefab spawned at the effect target. May be null.</summary>
        public GameObject ParticlePrefab => _particlePrefab;

        /// <summary>World-space offset applied when positioning particles at the target.</summary>
        public Vector3 Offset => _offset;

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>Play with no positional target. Feel plays at default position; particles spawn at origin.</summary>
        public void Play() => VFXManager.Instance?.Play(this, (Transform)null);

        /// <summary>
        /// Play at a UI element's position.
        /// Converts the RectTransform's screen position to world space for accurate particle placement.
        /// </summary>
        public void Play(RectTransform target) => VFXManager.Instance?.Play(this, target);

        /// <summary>Play at an explicit world-space position.</summary>
        public void Play(Vector3 worldPos) => VFXManager.Instance?.PlayAtWorld(this, worldPos);
    }
}
