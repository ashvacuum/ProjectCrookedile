using System;
using Crookedile.Core;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Wraps a single published <see cref="IGameEvent"/> for type-safe dispatch to passive triggers.
    /// The event is boxed as an object so that different event structs can be handled uniformly
    /// without a switch statement.
    ///
    /// Usage in triggers:
    ///   if (!ctx.Is&lt;TurnStartedEvent&gt;()) return false;
    ///   var e = ctx.As&lt;TurnStartedEvent&gt;();
    /// </summary>
    public readonly struct PassiveEventContext
    {
        private readonly object _rawEvent;

        /// <summary>The runtime type of the wrapped event (never null).</summary>
        public Type EventType { get; }

        public PassiveEventContext(IGameEvent evt)
        {
            _rawEvent  = evt;          // boxing — structs become objects
            EventType  = evt.GetType();
        }

        /// <summary>Returns true if the wrapped event is of type <typeparamref name="T"/>.</summary>
        public bool Is<T>() where T : struct, IGameEvent => EventType == typeof(T);

        /// <summary>
        /// Returns the wrapped event cast to <typeparamref name="T"/>.
        /// Returns <c>default(T)</c> if the event is a different type.
        /// </summary>
        public T As<T>() where T : struct, IGameEvent => Is<T>() ? (T)_rawEvent : default;
    }
}
