using UnityEngine;
using Crookedile.Managers;

namespace Crookedile.Data.Audio
{
    /// <summary>
    /// Scriptable Event — holds an <see cref="AudioClip"/> and playback settings.
    /// Call <see cref="Play"/> from anywhere; it routes to <see cref="AudioManager"/> automatically.
    ///
    /// Usage:
    ///   Assign an AudioEvent SO to a field, then call <c>myEvent.Play()</c> to fire SFX,
    ///   or <c>myEvent.PlayMusic()</c> to crossfade the background music track.
    ///
    /// Create via:  Assets → Create → Crookedile → Audio → Audio Event
    /// </summary>
    [CreateAssetMenu(menuName = "Crookedile/Audio/Audio Event", fileName = "NewAudioEvent")]
    public class AudioEvent : ScriptableObject
    {
        [Header("Clip")]
        [Tooltip("The audio clip to play. Leave null to make this entry a no-op.")]
        [SerializeField] private AudioClip _clip;

        [Tooltip("Volume scale applied on top of AudioManager's master/sfx volume settings.")]
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;

        [Header("Pitch")]
        [Tooltip("Base pitch multiplier (1 = normal speed).")]
        [SerializeField] private float _pitch = 1f;

        [Tooltip("Random ± variance added to pitch each play, for natural variation. 0 = no variance.")]
        [SerializeField, Range(0f, 0.5f)] private float _pitchVariance = 0f;

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Plays this event as a one-shot SFX.
        /// Safe to call when <see cref="_clip"/> is null or <see cref="AudioManager"/> is absent — both are no-ops.
        /// </summary>
        public void Play()
        {
            if (_clip == null || AudioManager.Instance == null) return;

            float pitch = _pitch + Random.Range(-_pitchVariance, _pitchVariance);
            AudioManager.Instance.PlaySfxOneShot(_clip, _volume, pitch);
        }

        /// <summary>
        /// Crossfades the background music to this event's clip.
        /// Safe to call when <see cref="_clip"/> is null or <see cref="AudioManager"/> is absent.
        /// </summary>
        /// <param name="fadeDuration">Total crossfade duration in seconds. 0 = instant swap.</param>
        public void PlayMusic(float fadeDuration = 0f)
        {
            if (_clip == null || AudioManager.Instance == null) return;
            AudioManager.Instance.PlayMusic(_clip, true, fadeDuration);
        }
    }
}
