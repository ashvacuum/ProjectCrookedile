using System;
using System.Collections.Generic;
using Crookedile.Core;
using UnityEngine;

namespace Crookedile.UI
{
    /// <summary>
    /// A piece of UI that wires itself to the <see cref="EventBus"/>. Subscriptions made with
    /// <see cref="On{T}"/> (normally in <c>OnEnable</c>) end in <see cref="OnDisable"/>, so no panel
    /// keeps its own unsubscribe list. Every panel and popup in the game derives from this.
    /// </summary>
    public abstract class UIPanel : MonoBehaviour
    {
        private readonly List<Action> _unsubscribers = new List<Action>();

        /// <summary>Subscribes until this panel is disabled.</summary>
        protected void On<T>(Action<T> handler)
            where T : IGameEvent
        {
            EventBus.Subscribe(handler);
            _unsubscribers.Add(() => EventBus.Unsubscribe(handler));
        }

        private bool _refreshQueued;

        /// <summary>Queues a <see cref="Refresh"/> whenever <typeparamref name="T"/> is published.</summary>
        protected void RefreshOn<T>()
            where T : IGameEvent => On<T>(_ => _refreshQueued = true);

        /// <summary>
        /// Queues one <see cref="Refresh"/> for the end of this frame. Several events often land in
        /// one resolution; coalescing repaints (and restarts tweens) once instead of once per event.
        /// </summary>
        protected void QueueRefresh() => _refreshQueued = true;

        /// <summary>Repaints this panel from current game state. Runs at most once per frame.</summary>
        protected virtual void Refresh() { }

        protected virtual void LateUpdate()
        {
            if (!_refreshQueued)
                return;
            _refreshQueued = false;
            Refresh();
        }

        protected virtual void OnDisable()
        {
            foreach (var unsubscribe in _unsubscribers)
                unsubscribe();
            _unsubscribers.Clear();
        }
    }
}
