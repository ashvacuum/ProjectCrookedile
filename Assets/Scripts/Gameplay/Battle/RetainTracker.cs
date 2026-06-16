using System.Collections.Generic;
using Crookedile.Data.Cards;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Tracks retain marks for cards in hand, extracted from DeckManager.
    ///
    /// Marks are COUNTED per CardData, not set-membership: duplicate copies of one card
    /// asset share the same CardData reference, so a set would retain every copy when one
    /// was marked. A count of N protects exactly N copies at end-of-turn discard.
    ///
    /// Two scopes: turn marks expire when <see cref="ClearTurnMarks"/> runs after the
    /// end-of-turn discard; battle marks persist until the copy is played/force-discarded
    /// (consuming its mark) or the deck is re-initialized.
    /// </summary>
    public class RetainTracker
    {
        private readonly Dictionary<CardData, int> _turnMarks = new Dictionary<CardData, int>();
        private readonly Dictionary<CardData, int> _battleMarks = new Dictionary<CardData, int>();

        /// <summary>
        /// Adds a retain mark for the card. Marks are capped at the number of copies actually
        /// in hand — retaining the same copy twice must not bank a phantom mark for a copy
        /// drawn later. Returns false when every copy in hand is already covered.
        /// </summary>
        public bool TryMark(CardData card, int copiesInHand, bool untilEndOfBattle)
        {
            var marks = untilEndOfBattle ? _battleMarks : _turnMarks;
            marks.TryGetValue(card, out int current);
            if (current >= copiesInHand)
                return false;
            marks[card] = current + 1;
            return true;
        }

        /// <summary>
        /// Spends a mark when a copy leaves the hand by being played or force-discarded,
        /// so the mark can't transfer to another copy of the same card. Turn marks spend first.
        /// </summary>
        public void ConsumeMark(CardData card)
        {
            if (!TryConsume(_turnMarks, card))
                TryConsume(_battleMarks, card);
        }

        /// <summary>
        /// Starts an end-of-turn discard pass over local allowance copies, so each mark
        /// protects exactly one copy this pass. Battle-long marks are consumed locally
        /// only — the master counts persist.
        /// </summary>
        public DiscardPass BeginDiscardPass() => new DiscardPass(_turnMarks, _battleMarks);

        /// <summary>One-turn retains expire after the end-of-turn discard; battle marks persist.</summary>
        public void ClearTurnMarks() => _turnMarks.Clear();

        /// <summary>Clears all marks (deck re-initialization).</summary>
        public void Reset()
        {
            _turnMarks.Clear();
            _battleMarks.Clear();
        }

        private static bool TryConsume(Dictionary<CardData, int> marks, CardData card)
        {
            if (!marks.TryGetValue(card, out int count) || count <= 0)
                return false;
            if (count == 1)
                marks.Remove(card);
            else
                marks[card] = count - 1;
            return true;
        }

        /// <summary>Per-pass allowances for one end-of-turn discard sweep.</summary>
        public class DiscardPass
        {
            private readonly Dictionary<CardData, int> _turnLeft;
            private readonly Dictionary<CardData, int> _battleLeft;

            internal DiscardPass(
                Dictionary<CardData, int> turnMarks,
                Dictionary<CardData, int> battleMarks
            )
            {
                _turnLeft = new Dictionary<CardData, int>(turnMarks);
                _battleLeft = new Dictionary<CardData, int>(battleMarks);
            }

            /// <summary>True when the card had an unspent allowance this pass (now consumed).</summary>
            public bool TryRetain(CardData card) =>
                TryConsume(_turnLeft, card) || TryConsume(_battleLeft, card);
        }
    }
}
