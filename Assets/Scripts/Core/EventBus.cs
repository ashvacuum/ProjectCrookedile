using System;
using System.Collections.Generic;

namespace Crookedile.Core
{
    /// <summary>
    /// Static publish/subscribe event bus. Decouples publishers from subscribers
    /// so systems can react to game events without holding direct references to each other.
    ///
    /// All events must implement <see cref="IGameEvent"/>.
    /// See <c>Assets/Scripts/Gameplay/Battle/BattleEvents.cs</c> for the full event catalogue.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> subscribers =
            new Dictionary<Type, List<Delegate>>();

        /// <summary>
        /// Registers a handler to receive events of type <typeparamref name="T"/>.
        /// Call this in <c>OnEnable()</c> (MonoBehaviour) or at construction time.
        /// Always pair with a matching <see cref="Unsubscribe{T}"/> call to prevent memory leaks.
        /// </summary>
        /// <typeparam name="T">The event struct type to listen for (must implement <see cref="IGameEvent"/>).</typeparam>
        /// <param name="handler">The method to invoke when the event is published. Must not be null.</param>
        public static void Subscribe<T>(Action<T> handler)
            where T : IGameEvent
        {
            if (handler == null)
                return;

            var eventType = typeof(T);

            if (!subscribers.ContainsKey(eventType))
            {
                subscribers[eventType] = new List<Delegate>();
            }

            subscribers[eventType].Add(handler);
        }

        /// <summary>
        /// Removes a previously registered handler for events of type <typeparamref name="T"/>.
        /// Call this in <c>OnDisable()</c> or <c>OnDestroy()</c> to prevent stale callbacks.
        /// Safe to call even if the handler was never subscribed.
        /// </summary>
        /// <typeparam name="T">The event struct type to stop listening for.</typeparam>
        /// <param name="handler">The exact handler reference passed to <see cref="Subscribe{T}"/>.</param>
        public static void Unsubscribe<T>(Action<T> handler)
            where T : IGameEvent
        {
            if (handler == null)
                return;

            var eventType = typeof(T);

            if (subscribers.ContainsKey(eventType))
            {
                subscribers[eventType].Remove(handler);
            }
        }

        /// <summary>
        /// Publishes an event to all currently registered handlers of type <typeparamref name="T"/>.
        /// Handlers are invoked in reverse subscription order so the list can be safely mutated during iteration.
        ///
        /// Error policy — bad subscribers are auto-removed and remaining subscribers always execute:
        /// <list type="bullet">
        ///   <item><see cref="UnityEngine.MissingReferenceException"/> — target MonoBehaviour was destroyed
        ///         without calling <see cref="Unsubscribe{T}"/>. Handler is removed silently with a warning.</item>
        ///   <item>Any other exception — handler threw unexpectedly. Handler is removed and an error is logged
        ///         so the bug is visible without blocking the rest of the publish.</item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">The event struct type to publish.</typeparam>
        /// <param name="gameEvent">The event data to pass to each subscriber.</param>
        public static void Publish<T>(T gameEvent)
            where T : IGameEvent
        {
            if (gameEvent == null)
                return;

            var eventType = typeof(T);

            if (!subscribers.ContainsKey(eventType))
                return;

            var handlerList = subscribers[eventType];

            for (int i = handlerList.Count - 1; i >= 0; i--)
            {
                var handler = handlerList[i] as Action<T>;

                if (handler == null)
                {
                    // Delegate stored with wrong type — should never happen; clean up silently.
                    handlerList.RemoveAt(i);
                    continue;
                }

                try
                {
                    handler.Invoke(gameEvent);
                }
                catch (UnityEngine.MissingReferenceException)
                {
                    // Target Unity object was destroyed without unsubscribing.
                    // Auto-remove so it never fires again; warn so the missing Unsubscribe call is visible.
                    UnityEngine.Debug.LogWarning(
                        $"[EventBus] Removed stale handler '{handler.Method.Name}' for {eventType.Name} "
                            + $"— target was destroyed without calling Unsubscribe."
                    );
                    handlerList.RemoveAt(i);
                }
                catch (Exception ex)
                {
                    // Handler threw an unexpected exception.
                    // Auto-remove to prevent repeat errors on every future publish; log as error so the bug is visible.
                    UnityEngine.Debug.LogError(
                        $"[EventBus] Removed bad handler '{handler.Method.Name}' for {eventType.Name} "
                            + $"after unhandled exception: {ex}"
                    );
                    handlerList.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Removes all subscribers for all event types.
        /// Call this when tearing down a scene or resetting game state to prevent stale callbacks.
        /// </summary>
        public static void Clear()
        {
            subscribers.Clear();
        }

        /// <summary>
        /// Removes all subscribers for the specific event type <typeparamref name="T"/> only.
        /// Prefer this over <see cref="Clear()"/> when only one event type needs resetting.
        /// </summary>
        /// <typeparam name="T">The event struct type whose subscribers should be cleared.</typeparam>
        public static void Clear<T>()
            where T : IGameEvent
        {
            var eventType = typeof(T);
            if (subscribers.ContainsKey(eventType))
            {
                subscribers[eventType].Clear();
            }
        }
    }
}
