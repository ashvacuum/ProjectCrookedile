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

        /// <summary>The boxed event — for reflection-based reads (e.g. the EnemyIndex probe).</summary>
        public object RawEvent => _rawEvent;

        public PassiveEventContext(IGameEvent evt)
        {
            _rawEvent = evt; // boxing — structs become objects
            EventType = evt.GetType();
        }

        /// <summary>Returns true if the wrapped event is of type <typeparamref name="T"/>.</summary>
        public bool Is<T>()
            where T : struct, IGameEvent => EventType == typeof(T);

        /// <summary>
        /// Returns the wrapped event cast to <typeparamref name="T"/>.
        /// Returns <c>default(T)</c> if the event is a different type.
        /// </summary>
        public T As<T>()
            where T : struct, IGameEvent => Is<T>() ? (T)_rawEvent : default;

        /// <summary>
        /// The card a card event names (played, resolved, replayed, drawn, discarded, exhausted,
        /// retained, recovered), or null for any other event.
        /// </summary>
        public Crookedile.Data.Cards.CardData GetCard()
        {
            switch (_rawEvent)
            {
                case CardPlayedEvent e:
                    return e.Card;
                case CardPlayResolvedEvent e:
                    return e.Card;
                case CardReplayedEvent e:
                    return e.Card;
                case CardDrawnEvent e:
                    return e.Card;
                case CardDiscardedEvent e:
                    return e.Card;
                case CardExhaustedEvent e:
                    return e.Card;
                case CardRetainedEvent e:
                    return e.Card;
                case CardRecoveredEvent e:
                    return e.Card;
                default:
                    return null;
            }
        }
    }
}
