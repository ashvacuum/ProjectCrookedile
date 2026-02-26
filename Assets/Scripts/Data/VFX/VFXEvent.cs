using UnityEngine;
using Crookedile.Managers;

namespace Crookedile.Data.VFX
{
    /// <summary>
    /// Scriptable Event — describes a visual effect combining an optional Feel feedback
    /// (shake, punch, tween on an existing UI element) with an optional animated Image prefab
    /// (Image + Animator) spawned on the VFX canvas at the target's position.
    ///
    /// Either field can be left empty; the system no-ops cleanly for whichever is absent.
    ///
    /// Usage:
    ///   myVfxEvent.Play()               — no positional target
    ///   myVfxEvent.Play(rectTransform)  — spawns at a UI element's position
    ///   myVfxEvent.Play(worldPos)       — spawns at a world-space point (converted to canvas space)
    ///
    /// Create via:  Assets → Create → Crookedile → VFX → VFX Event
    /// </summary>
    [CreateAssetMenu(menuName = "Crookedile/VFX/VFX Event", fileName = "NewVFXEvent")]
    public class VFXEvent : ScriptableObject
    {
        [Header("Feel Feedback")]
        [Tooltip("ID of a named MMF_Player registered in FeedbackManager. Leave empty to skip Feel.\n" +
                 "Feel handles shake, scale-punch, and tween effects on existing UI elements.")]
        [SerializeField] private string _feedbackId;

        [Header("Animated Image")]
        [Tooltip("UI prefab with Image + Animator components. Spawned on the VFX canvas at the target's\n" +
                 "position and deactivated when its animation ends. Leave null to skip the spawned effect.")]
        [SerializeField] private GameObject _animatedPrefab;

        [Tooltip("Canvas-space offset (in pixels) added to the target position after placement.")]
        [SerializeField] private Vector2 _offset = Vector2.zero;

        // ─── Properties ───────────────────────────────────────────────────────

        /// <summary>Named Feel player ID passed to <see cref="FeedbackManager.Play(string,Transform)"/>.</summary>
        public string FeedbackId => _feedbackId;

        /// <summary>Animated UI prefab (Image + Animator) spawned at the effect target. May be null.</summary>
        public GameObject AnimatedPrefab => _animatedPrefab;

        /// <summary>Canvas-space pixel offset applied when positioning the animated image at the target.</summary>
        public Vector2 Offset => _offset;

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>Play with no positional target. Feel plays at default position; animated image spawns at canvas center.</summary>
        public void Play() => VFXManager.Instance?.Play(this, (RectTransform)null);

        /// <summary>Play aimed at a UI element — animated image spawns at the element's canvas position.</summary>
        public void Play(RectTransform target) => VFXManager.Instance?.Play(this, target);

        /// <summary>Play at an explicit world-space position (converted to canvas space by VFXManager).</summary>
        public void Play(Vector3 worldPos) => VFXManager.Instance?.PlayAtWorld(this, worldPos);
    }
}
