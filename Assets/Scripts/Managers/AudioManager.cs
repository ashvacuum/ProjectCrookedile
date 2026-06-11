using System.Collections.Generic;
using System.Threading;
using Crookedile.Core;
using Crookedile.Data.Audio;
using Crookedile.Utilities;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Crookedile.Managers
{
    [Debuggable("Audio", LogLevel.Warning)]
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Audio Sources")]
        [SerializeField]
        private AudioSource _musicSource;

        [SerializeField]
        private AudioSource _sfxSource;

        [SerializeField]
        private int _sfxPoolSize = 10;

        [Header("Settings")]
        [SerializeField]
        private float _masterVolume = 1f;

        [SerializeField]
        private float _musicVolume = 1f;

        [SerializeField]
        private float _sfxVolume = 1f;

        [Header("Sound Library")]
        [Tooltip(
            "ScriptableObject database of named clips. Use 'Refresh Database' on the asset to auto-populate."
        )]
        [SerializeField]
        private SoundLibrary _soundLibrary;

        private Queue<AudioSource> _sfxPool = new Queue<AudioSource>();
        private List<AudioSource> _activeSfxSources = new List<AudioSource>();

        protected override void OnAwake()
        {
            if (_musicSource == null)
            {
                _musicSource = gameObject.AddComponent<AudioSource>();
                _musicSource.loop = true;
                _musicSource.playOnAwake = false;
            }

            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
                _sfxSource.playOnAwake = false;
            }

            CreateSfxPool();
            ApplyVolume();
        }

        private void CreateSfxPool()
        {
            for (int i = 0; i < _sfxPoolSize; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.outputAudioMixerGroup = _sfxSource.outputAudioMixerGroup;
                source.playOnAwake = false;
                _sfxPool.Enqueue(source);
            }
        }

        public void PlayMusic(AudioClip clip, bool loop = true, float fadeDuration = 0f)
        {
            if (clip == null)
            {
                GameLogger.LogWarning("Audio", "Trying to play null music clip");
                return;
            }

            if (fadeDuration > 0f)
            {
                CrossfadeTo(clip, loop, fadeDuration).Forget();
            }
            else
            {
                _musicSource.clip = clip;
                _musicSource.loop = loop;
                _musicSource.Play();
            }

            GameLogger.LogInfo("Audio", $"Playing music: {clip.name}");
        }

        /// <summary>
        /// Cancellation source for the active music fade. Each new fade cancels the previous
        /// one so overlapping PlayMusic/StopMusic calls can't fight over the volume.
        /// </summary>
        private CancellationTokenSource _musicFadeCts;

        private CancellationToken NextMusicFadeToken()
        {
            _musicFadeCts?.Cancel();
            _musicFadeCts?.Dispose();
            _musicFadeCts = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy()
            );
            return _musicFadeCts.Token;
        }

        private async UniTaskVoid CrossfadeTo(AudioClip clip, bool loop, float fadeDuration)
        {
            var ct = NextMusicFadeToken();
            await FadeMusicVolume(_musicSource.volume, 0f, fadeDuration / 2f, ct);
            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.Play();
            await FadeMusicVolume(0f, _musicVolume * _masterVolume, fadeDuration / 2f, ct);
        }

        private async UniTask FadeMusicVolume(
            float from,
            float to,
            float duration,
            CancellationToken ct
        )
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(from, to, elapsed / duration);
                await UniTask.Yield(ct);
            }
            _musicSource.volume = to;
        }

        public void StopMusic(float fadeDuration = 0f)
        {
            if (fadeDuration > 0f)
            {
                FadeOutAndStop(fadeDuration).Forget();
            }
            else
            {
                _musicSource.Stop();
            }
        }

        private async UniTaskVoid FadeOutAndStop(float fadeDuration)
        {
            var ct = NextMusicFadeToken();
            await FadeMusicVolume(_musicSource.volume, 0f, fadeDuration, ct);
            _musicSource.Stop();
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null)
            {
                GameLogger.LogWarning("Audio", "Trying to play null SFX clip");
                return;
            }

            AudioSource source = GetAvailableSfxSource();
            if (source != null)
            {
                source.clip = clip;
                source.volume = _sfxVolume * _masterVolume * volumeScale;
                source.Play();
                _activeSfxSources.Add(source);
            }
        }

        /// <param name="pitch">Pitch multiplier (1 = normal speed). Applies to the shared _sfxSource before the one-shot.</param>
        public void PlaySfxOneShot(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
        {
            if (clip == null)
                return;
            _sfxSource.pitch = pitch;
            _sfxSource.PlayOneShot(clip, _sfxVolume * _masterVolume * volumeScale);
        }

        /// <summary>
        /// Plays a clip from the SoundLibrary looked up by GUID.
        /// Called by code that stores the auto-generated ID.
        /// No-op if not found or library is unassigned.
        /// </summary>
        public void PlaySoundByID(string id)
        {
            var data = ResolveClip(id, byName: false);
            if (data != null)
                PlaySfxOneShot(data.Clip, data.Volume, data.Pitch);
        }

        /// <summary>
        /// Plays a clip from the SoundLibrary looked up by human-readable ClipName.
        /// Called by code that stores the designer-set name.
        /// No-op if not found or library is unassigned.
        /// </summary>
        public void PlaySoundByName(string clipName)
        {
            var data = ResolveClip(clipName, byName: true);
            if (data != null)
                PlaySfxOneShot(data.Clip, data.Volume, data.Pitch);
        }

        /// <summary>
        /// Plays a clip from the SoundLibrary. Tries GUID first, then ClipName as fallback.
        /// Used by VFXAnimatedImage AnimationEvents where either format may be supplied.
        /// </summary>
        public void PlaySound(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            if (_soundLibrary == null)
            {
                GameLogger.LogWarning(
                    "Audio",
                    "PlaySound: no SoundLibrary assigned on AudioManager."
                );
                return;
            }
            var data = _soundLibrary.GetByIDOrName(value);
            if (data == null)
            {
                GameLogger.LogWarning(
                    "Audio",
                    $"PlaySound: '{value}' not found in SoundLibrary by ID or name."
                );
                return;
            }
            PlaySfxOneShot(data.Clip, data.Volume, data.Pitch);
        }

        private Data.Audio.AudioClipData ResolveClip(string value, bool byName)
        {
            if (string.IsNullOrEmpty(value))
                return null;
            if (_soundLibrary == null)
            {
                GameLogger.LogWarning(
                    "Audio",
                    "PlaySound: no SoundLibrary assigned on AudioManager."
                );
                return null;
            }
            var data = byName ? _soundLibrary.GetByName(value) : _soundLibrary.GetByID(value);
            if (data == null)
                GameLogger.LogWarning("Audio", $"PlaySound: '{value}' not found in SoundLibrary.");
            return data;
        }

        private AudioSource GetAvailableSfxSource()
        {
            // Clean up finished sources
            _activeSfxSources.RemoveAll(s => !s.isPlaying);

            // Try to get from pool
            if (_sfxPool.Count > 0)
            {
                return _sfxPool.Dequeue();
            }

            // Check if any active source is finished
            foreach (var source in _activeSfxSources)
            {
                if (!source.isPlaying)
                {
                    _activeSfxSources.Remove(source);
                    return source;
                }
            }

            // Create new source if needed
            AudioSource newSource = gameObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            return newSource;
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            ApplyVolume();
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            ApplyVolume();
        }

        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            ApplyVolume();
        }

        private void ApplyVolume()
        {
            if (_musicSource != null)
            {
                _musicSource.volume = _musicVolume * _masterVolume;
            }
        }
    }
}
