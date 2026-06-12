using System.Collections.Generic;
using Crookedile.Data.Cards;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Per-card AP cost reductions applied this battle (individual card effects),
    /// extracted from DeckManager. Reductions stack additively; the floor is applied
    /// at play time. <see cref="FreeSentinel"/> signals "free" (0 AP regardless of base cost).
    ///
    /// Supports a single snapshot/restore pair for transient passes
    /// (MakeAllCardsFreeNextPlayEffect): snapshot before the free-all, restore after
    /// the next card play so pre-existing permanent reductions survive the revert.
    /// </summary>
    public class CardCostOverrides
    {
        /// <summary>Sentinel reduction meaning "this card costs 0 AP this battle".</summary>
        public const int FreeSentinel = int.MaxValue;

        private readonly Dictionary<CardData, int> _reductions =
            new Dictionary<CardData, int>();

        // Non-null only while a "next-play-only" free-all pass is active.
        private Dictionary<CardData, int> _snapshot;

        /// <summary>Stacks an additive reduction onto the card. Returns the new total.</summary>
        public int ApplyReduction(CardData card, int reduction)
        {
            _reductions.TryGetValue(card, out int current);
            _reductions[card] = current + reduction;
            return _reductions[card];
        }

        /// <summary>Makes the card cost 0 AP for the rest of the battle.</summary>
        public void MakeFree(CardData card) => _reductions[card] = FreeSentinel;

        /// <summary>
        /// Total reduction applied to this card this battle; 0 if none,
        /// <see cref="FreeSentinel"/> if the card was made free.
        /// </summary>
        public int GetReduction(CardData card) =>
            _reductions.TryGetValue(card, out int r) ? r : 0;

        /// <summary>
        /// Captures the current reductions. Safe to call with a snapshot already active
        /// (overwrites the old one). Returns the number of entries captured.
        /// </summary>
        public int Snapshot()
        {
            _snapshot = new Dictionary<CardData, int>(_reductions);
            return _snapshot.Count;
        }

        /// <summary>
        /// Restores reductions to the captured snapshot, discarding transient changes.
        /// Returns false (no-op) when no snapshot exists.
        /// </summary>
        public bool RestoreSnapshot()
        {
            if (_snapshot == null)
                return false;
            _reductions.Clear();
            foreach (var kvp in _snapshot)
                _reductions[kvp.Key] = kvp.Value;
            _snapshot = null;
            return true;
        }
    }
}
