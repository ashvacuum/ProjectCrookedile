using System.Collections.Generic;
using Crookedile.Data.Cards;

namespace Crookedile.Data
{
    /// <summary>
    /// Holds the state of the current run (origin + accumulated deck).
    ///
    /// Intentionally minimal — a full <c>RunManager</c> system can layer on top of this
    /// once the map and relic systems are built. Until then, everything that needs to
    /// persist between battles can read from <see cref="Current"/>.
    ///
    /// Not a MonoBehaviour: no scene dependency, no serialization overhead.
    /// Created once at run-start via <see cref="Create"/> and replaced on new run.
    /// </summary>
    public class RunState
    {
        // ── Static accessor ───────────────────────────────────────────────────

        /// <summary>The active run, or <c>null</c> if no run has been created yet.</summary>
        public static RunState Current { get; private set; }

        // ── Run data ──────────────────────────────────────────────────────────

        /// <summary>The player's chosen origin for this run.</summary>
        public OriginType     Origin { get; private set; }

        /// <summary>
        /// The player's current deck — includes starter cards plus all cards
        /// picked up via reward screens during this run.
        /// </summary>
        public List<CardData> Deck   { get; private set; }

        // ── Construction ──────────────────────────────────────────────────────

        private RunState() { }

        /// <summary>
        /// Creates a new run and stores it in <see cref="Current"/>.
        /// Call once from the run-start flow (origin selection / main menu).
        /// </summary>
        /// <param name="origin">The origin the player selected.</param>
        /// <param name="starterDeck">Initial deck cards (shallow copy is taken).</param>
        public static RunState Create(OriginType origin, List<CardData> starterDeck)
        {
            Current = new RunState
            {
                Origin = origin,
                Deck   = starterDeck != null
                             ? new List<CardData>(starterDeck)
                             : new List<CardData>()
            };
            return Current;
        }

        // ── Mutation ──────────────────────────────────────────────────────────

        /// <summary>
        /// Appends <paramref name="card"/> to the deck.
        /// No-op if <paramref name="card"/> is <c>null</c>.
        /// </summary>
        public void AddCardToDeck(CardData card)
        {
            if (card != null)
                Deck.Add(card);
        }

        /// <summary>
        /// Removes <paramref name="card"/> from the deck.
        /// No-op if not found or <paramref name="card"/> is <c>null</c>.
        /// </summary>
        public void RemoveCardFromDeck(CardData card)
        {
            if (card != null)
                Deck.Remove(card);
        }
    }
}
