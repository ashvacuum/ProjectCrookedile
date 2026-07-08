using System.Collections.Generic;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// Holds the state of the current run (origin, accumulated deck, HP, and battle queue).
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
        #region Static accessor
        /// <summary>The active run, or <c>null</c> if no run has been created yet.</summary>
        public static RunState Current { get; private set; }

        #endregion

        #region Run data
        /// <summary>The player's chosen origin for this run.</summary>
        public OriginType Origin { get; private set; }

        /// <summary>
        /// The player's current deck — includes starter cards plus all cards
        /// picked up via reward screens during this run.
        /// </summary>
        public List<CardData> Deck { get; private set; }

        /// <summary>
        /// Relics acquired this run. Their passives are registered with every battle's
        /// PassiveResolver, so they behave exactly like origin passives in battle.
        /// </summary>
        public List<RelicData> Relics { get; private set; }

        #endregion

        #region Battle queue
        /// <summary>
        /// Ordered list of enemy groups — one per encounter.
        /// <c>null</c> when no session was defined (single-round mode).
        /// </summary>
        public List<List<EnemyData>> BattleQueue { get; private set; }

        /// <summary>Index of the current battle in <see cref="BattleQueue"/>.</summary>
        public int CurrentBattleIndex { get; private set; }

        /// <summary>True when there is at least one more battle after the current one.</summary>
        public bool HasNextBattle =>
            BattleQueue != null && CurrentBattleIndex + 1 < BattleQueue.Count;

        /// <summary>
        /// Enemy list for the current battle, or <c>null</c> if no queue is defined
        /// or the index is out of range.
        /// </summary>
        public List<EnemyData> CurrentBattleEnemies =>
            BattleQueue != null && CurrentBattleIndex < BattleQueue.Count
                ? BattleQueue[CurrentBattleIndex]
                : null;

        #endregion

        #region Construction
        private RunState() { }

        /// <summary>
        /// Creates a new run and stores it in <see cref="Current"/>.
        /// Call once from the run-start flow (origin selection / main menu).
        /// </summary>
        /// <param name="origin">The origin the player selected.</param>
        /// <param name="starterDeck">Initial deck cards (shallow copy is taken).</param>
        /// <param name="battleQueue">
        /// Optional ordered list of enemy groups (one per round).
        /// Pass <c>null</c> for a single-round session where <c>BattleTestStarter</c>
        /// handles enemy selection directly.
        /// </param>
        public static RunState Create(
            OriginType origin,
            List<CardData> starterDeck,
            List<List<EnemyData>> battleQueue = null
        )
        {
            Current = new RunState
            {
                Origin = origin,
                Deck = starterDeck != null ? new List<CardData>(starterDeck) : new List<CardData>(),
                Relics = new List<RelicData>(),
                CurrentBattleIndex = 0,
                BattleQueue = battleQueue,
            };
            return Current;
        }

        /// <summary>
        /// Clears <see cref="Current"/>, ending the run.
        /// The next call to <see cref="Create"/> will start a fresh run.
        /// </summary>
        public static void Clear() => Current = null;

        #endregion

        #region Mutation
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

        /// <summary>
        /// Adds <paramref name="relic"/> to the run. Duplicates and <c>null</c> are ignored
        /// (relics are unique per run, StS-style).
        /// </summary>
        public void AddRelic(RelicData relic)
        {
            if (relic != null && !Relics.Contains(relic))
                Relics.Add(relic);
        }

        /// <summary>Records that the current battle was won (advances meta state).</summary>
        public void RecordBattleVictory() { /* meta popularity update deferred */
        }

        /// <summary>
        /// Advances to the next battle in <see cref="BattleQueue"/>.
        /// Safe to call even when <see cref="HasNextBattle"/> is false (index clamps at Count).
        /// </summary>
        public void AdvanceToNextBattle()
        {
            if (BattleQueue != null)
                CurrentBattleIndex = Mathf.Min(CurrentBattleIndex + 1, BattleQueue.Count);
        }
        #endregion
    }
}
