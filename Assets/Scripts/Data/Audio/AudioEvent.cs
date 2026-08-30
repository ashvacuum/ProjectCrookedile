using Sirenix.OdinInspector;
using Crookedile.Managers;
using UnityEngine;

namespace Crookedile.Data.Audio
{
    /// <summary>
    /// Scriptable Event — holds an <see cref="AudioClip"/> and playback settings.
    /// Call <see cref="Play"/> from anywhere; it routes to <see cref="AudioManager"/> automatically.
    ///
    /// Usage:
    ///   Assign an AudioEvent SO to a field, then call <c>myEvent.Play()</c>.
    ///   • If <c>_isMusic</c> is false (default) → fires as a one-shot SFX with pitch variance.
    ///   • If <c>_isMusic</c> is true → crossfades into looping BGM with <c>_musicFadeDuration</c>.
    ///
    ///   This lets <see cref="BattleFeedbackController"/> call a uniform <c>Play()</c> without
    ///   needing to know whether a trigger should play SFX or change the music track.
    ///
    /// Create via:  Assets → Create → Crookedile → Audio → Audio Event
    /// </summary>
    [CreateAssetMenu(menuName = "Crookedile/Audio/Audio Event", fileName = "NewAudioEvent")]
    public class AudioEvent : ScriptableObject
    {
        [Header("Clip")]
        [Tooltip("The audio clip to play. Leave null to make this entry a no-op.")]
        [SerializeField]
        private AudioClip _clip;

        [Tooltip(
            "Volume scale applied on top of AudioManager's master/sfx volume settings. Ignored for music (music has its own volume track)."
        )]
        [SerializeField, Range(0f, 1f)]
        private float _volume = 1f;

        [Header("Pitch (SFX only)")]
        [Tooltip("Base pitch multiplier (1 = normal speed). Ignored when _isMusic is true.")]
        [SerializeField]
        [HideIf(nameof(_isMusic))]
        private float _pitch = 1f;

        [Tooltip(
            "Random ± variance added to pitch each play, for natural variation. 0 = no variance."
        )]
        [SerializeField, Range(0f, 0.5f)]
        [HideIf(nameof(_isMusic))]
        private float _pitchVariance = 0f;

        [Header("Music")]
        [Tooltip(
            "If true, Play() crossfades into looping background music instead of firing a one-shot SFX.\n"
                + "Tick this on AudioEvents that represent BGM tracks (battle theme, victory fanfare loop, etc.)."
        )]
        [SerializeField]
        private bool _isMusic = false;

        [Tooltip(
            "Crossfade duration in seconds when switching BGM. 0 = instant swap. Only used when _isMusic is true."
        )]
        [SerializeField]
        [ShowIf(nameof(_isMusic))]
        private float _musicFadeDuration = 0f;

        #region Public API
        /// <summary>
        /// Plays this event.
        /// • <c>_isMusic = false</c>: fires as a one-shot SFX with pitch/volume settings.
        /// • <c>_isMusic = true</c>: crossfades the background music to this clip (looping).
        ///
        /// Safe to call when <see cref="_clip"/> is null or <see cref="AudioManager"/> is absent — both are no-ops.
        /// </summary>
        public void Play()
        {
            if (_clip == null || AudioManager.Instance == null)
                return;

            if (_isMusic)
            {
                AudioManager.Instance.PlayMusic(_clip, true, _musicFadeDuration);
            }
            else
            {
                float pitch = _pitch + Random.Range(-_pitchVariance, _pitchVariance);
                AudioManager.Instance.PlaySfxOneShot(_clip, _volume, pitch);
            }
        }

        /// <summary>
        /// Explicitly crossfades the background music to this event's clip, ignoring <c>_isMusic</c>.
        /// Use when you always want music behaviour regardless of the flag.
        /// </summary>
        /// <param name="fadeDuration">Total crossfade duration in seconds. 0 = instant swap.</param>
        public void PlayMusic(float fadeDuration = 0f)
        {
            if (_clip == null || AudioManager.Instance == null)
                return;
            AudioManager.Instance.PlayMusic(_clip, true, fadeDuration);
        }
        #endregion
    }
}
