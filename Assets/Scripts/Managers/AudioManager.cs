using System.Collections.Generic;
using UnityEngine;
using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Managers
{
    [Debuggable("Audio", LogLevel.Warning)]
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private int _sfxPoolSize = 10;

        [Header("Settings")]
        [SerializeField] private float _masterVolume = 1f;
        [SerializeField] private float _musicVolume = 1f;
        [SerializeField] private float _sfxVolume = 1f;

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
                StartCoroutine(FadeMusicOut(fadeDuration / 2f, () =>
                {
                    _musicSource.clip = clip;
                    _musicSource.loop = loop;
                    _musicSource.Play();
                    StartCoroutine(FadeMusicIn(fadeDuration / 2f));
                }));
            }
            else
            {
                _musicSource.clip = clip;
                _musicSource.loop = loop;
                _musicSource.Play();
            }

            GameLogger.LogInfo("Audio", $"Playing music: {clip.name}");
        }

        private System.Collections.IEnumerator FadeMusicIn(float duration)
        {
            float elapsed = 0f;
            float targetVolume = _musicVolume * _masterVolume;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
                yield return null;
            }

            _musicSource.volume = targetVolume;
        }

        private System.Collections.IEnumerator FadeMusicOut(float duration, System.Action onComplete = null)
        {
            float elapsed = 0f;
            float startVolume = _musicSource.volume;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            _musicSource.volume = 0f;
            onComplete?.Invoke();
        }

        public void StopMusic(float fadeDuration = 0f)
        {
            if (fadeDuration > 0f)
            {
                StartCoroutine(FadeMusicOut(fadeDuration, () => _musicSource.Stop()));
            }
            else
            {
                _musicSource.Stop();
            }
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

        public void PlaySfxOneShot(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip, _sfxVolume * _masterVolume * volumeScale);
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
