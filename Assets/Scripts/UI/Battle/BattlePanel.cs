using System;
using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Gameplay.Battle;
using UnityEngine;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// A piece of battle UI that wires itself. Subscriptions made with <see cref="On{T}"/> end in
    /// <see cref="OnDisable"/>, and <see cref="Battle"/> is the scene's battle, so a new panel needs
    /// no BattleUI field and no Bind call: drop it under the battle canvas.
    /// </summary>
    public abstract class BattlePanel : MonoBehaviour
    {
        private readonly List<Action> _unsubscribers = new List<Action>();

        /// <summary>The running battle; null before the BattleManager wakes.</summary>
        protected BattleManager Battle => BattleManager.Current;

        /// <summary>Subscribes until this panel is disabled.</summary>
        protected void On<T>(Action<T> handler)
            where T : IGameEvent
        {
            EventBus.Subscribe(handler);
            _unsubscribers.Add(() => EventBus.Unsubscribe(handler));
        }

        protected virtual void OnDisable()
        {
            foreach (var unsubscribe in _unsubscribers)
                unsubscribe();
            _unsubscribers.Clear();
        }
    }
}
