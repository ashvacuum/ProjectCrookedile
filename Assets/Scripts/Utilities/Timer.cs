using System;
using UnityEngine;

namespace Crookedile.Utilities
{
    public class Timer
    {
        private float _duration;
        private float _timeRemaining;
        private bool _isRunning;
        private bool _isLooping;
        private Action _onComplete;

        public float Duration => _duration;
        public float TimeRemaining => _timeRemaining;
        public float Progress => _duration > 0 ? 1f - (_timeRemaining / _duration) : 1f;
        public bool IsRunning => _isRunning;
        public bool IsFinished => _timeRemaining <= 0f && !_isRunning;

        public Timer(float duration, bool autoStart = false, bool loop = false, Action onComplete = null)
        {
            _duration = duration;
            _timeRemaining = duration;
            _isLooping = loop;
            _onComplete = onComplete;
            _isRunning = autoStart;
        }

        public void Update(float deltaTime)
        {
            if (!_isRunning) return;

            _timeRemaining -= deltaTime;

            if (_timeRemaining <= 0f)
            {
                _onComplete?.Invoke();

                if (_isLooping)
                {
                    Reset();
                }
                else
                {
                    _isRunning = false;
                }
            }
        }

        public void Start()
        {
            _isRunning = true;
        }

        public void Stop()
        {
            _isRunning = false;
        }

        public void Reset()
        {
            _timeRemaining = _duration;
            _isRunning = true;
        }

        public void Reset(float newDuration)
        {
            _duration = newDuration;
            Reset();
        }
    }
}
