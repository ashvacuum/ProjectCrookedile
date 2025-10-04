using System;
using System.Collections.Generic;

namespace Crookedile.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> subscribers = new Dictionary<Type, List<Delegate>>();

        public static void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null) return;

            var eventType = typeof(T);

            if (!subscribers.ContainsKey(eventType))
            {
                subscribers[eventType] = new List<Delegate>();
            }

            subscribers[eventType].Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null) return;

            var eventType = typeof(T);

            if (subscribers.ContainsKey(eventType))
            {
                subscribers[eventType].Remove(handler);
            }
        }

        public static void Publish<T>(T gameEvent) where T : IGameEvent
        {
            if (gameEvent == null) return;

            var eventType = typeof(T);

            if (!subscribers.ContainsKey(eventType)) return;

            var handlerList = subscribers[eventType];

            for (int i = handlerList.Count - 1; i >= 0; i--)
            {
                var handler = handlerList[i] as Action<T>;

                if (handler != null)
                {
                    try
                    {
                        handler.Invoke(gameEvent);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"Error invoking event handler for {eventType.Name}: {ex.Message}");
                    }
                }
                else
                {
                    handlerList.RemoveAt(i);
                }
            }
        }

        public static void Clear()
        {
            subscribers.Clear();
        }

        public static void Clear<T>() where T : IGameEvent
        {
            var eventType = typeof(T);
            if (subscribers.ContainsKey(eventType))
            {
                subscribers[eventType].Clear();
            }
        }
    }
}
