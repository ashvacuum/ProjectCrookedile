using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crookedile.Utilities
{
    public interface IState
    {
        void OnEnter();
        void OnUpdate();
        void OnExit();
    }

    public class StateMachine<T> where T : Enum
    {
        private Dictionary<T, IState> _states = new Dictionary<T, IState>();
        private IState _currentState;
        private T _currentStateType;

        public T CurrentStateType => _currentStateType;
        public IState CurrentState => _currentState;

        public void RegisterState(T stateType, IState state)
        {
            if (_states.ContainsKey(stateType))
            {
                GameLogger.LogWarning("Core", $"State {stateType} already registered. Overwriting.");
            }
            _states[stateType] = state;
        }

        public void ChangeState(T newStateType)
        {
            if (!_states.ContainsKey(newStateType))
            {
                GameLogger.LogError("Core", $"State {newStateType} not registered!");
                return;
            }

            if (_currentState != null && EqualityComparer<T>.Default.Equals(_currentStateType, newStateType))
            {
                return; // Already in this state
            }

            _currentState?.OnExit();

            _currentStateType = newStateType;
            _currentState = _states[newStateType];

            _currentState?.OnEnter();
        }

        public void Update()
        {
            _currentState?.OnUpdate();
        }

        public bool IsInState(T stateType)
        {
            return EqualityComparer<T>.Default.Equals(_currentStateType, stateType);
        }
    }

    // Simple state implementation helper
    public abstract class State : IState
    {
        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnExit() { }
    }
}
