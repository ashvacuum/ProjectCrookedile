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

    public class StateMachine<T>
        where T : Enum
    {
        private Dictionary<T, IState> _states = new Dictionary<T, IState>();
        private IState _currentState;
        private T _currentStateType;

        public T CurrentStateType => _currentStateType;
        public IState CurrentState => _currentState;

        /// <summary>
        /// Fired (previous, current) the instant a state becomes current, BEFORE its OnEnter runs.
        /// Firing here — not after ChangeState returns — means a state whose OnEnter synchronously
        /// transitions again still notifies in correct order, and CurrentStateType already equals
        /// the fired value when listeners read it.
        /// </summary>
        public event Action<T, T> StateEntered;

        public void RegisterState(T stateType, IState state)
        {
            if (_states.ContainsKey(stateType))
            {
                GameLogger.LogWarning(
                    "Core",
                    $"State {stateType} already registered. Overwriting."
                );
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

            if (
                _currentState != null
                && EqualityComparer<T>.Default.Equals(_currentStateType, newStateType)
            )
            {
                return; // Already in this state
            }

            _currentState?.OnExit();

            T previous = _currentStateType;
            _currentStateType = newStateType;
            _currentState = _states[newStateType];

            // Notify before OnEnter so nested (synchronous) transitions notify in entry order.
            StateEntered?.Invoke(previous, newStateType);

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
