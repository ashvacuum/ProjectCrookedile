using System;
using System.Collections.Generic;
using Crookedile.Data.Campaign;
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

        #region Campaign state
        /// <summary>
        /// True when this run was started from the campaign map (<c>campaign.unity</c>),
        /// as opposed to a standalone test run via <c>BattleTestStarter</c>'s inspector
        /// fallback. Lets <c>PostBattleFlow</c> tell the two apart — both look identical
        /// otherwise (a RunState with a non-empty deck).
        /// </summary>
        public bool IsCampaignRun { get; private set; }

        /// <summary>Hours remaining today. Spent by choosing a map location.</summary>
        public int Hours { get; private set; }

        /// <summary>Hours a fresh day refills to. Set once at <see cref="Create"/>.</summary>
        public int MaxHours { get; private set; }

        /// <summary>Current campaign day, starting at 1.</summary>
        public int Day { get; private set; } = 1;

        /// <summary>
        /// Seed for campaign generation. The same seed replays the same sequence of encounters,
        /// which is what makes a run shareable and a report reproducible. Set at
        /// <see cref="Create"/>; pass 0 there to get a random one.
        /// </summary>
        public int Seed { get; private set; }

        /// <summary>Meta currency accumulated this run (placeholder name — see metagame-campaign.md).</summary>
        public int Funds { get; private set; }

        /// <summary>
        /// Standing with the public — the meta axis event choices trade against Funds.
        /// A meta stat only: battle reads Opinion, never this (see the enemy-bible audit).
        /// </summary>
        public int Credibility { get; private set; }

        /// <summary>
        /// The encounter chosen on the campaign map, waiting to be consumed by the battle
        /// scene. Set by <c>CampaignFlow</c> just before loading <c>main.unity</c>; the
        /// battle-scene starter must clear it via <see cref="ClearPendingBattle"/> right
        /// after reading it so a stray reload can't replay it.
        /// </summary>
        public BattleEncounterData PendingBattle { get; private set; }

        /// <summary>Stable ids of non-repeatable locations already resolved this run.</summary>
        public HashSet<string> VisitedLocationIds { get; private set; }

        /// <summary>Spends Hours, clamped so it never goes negative.</summary>
        public void SpendHours(int amount) => Hours = Mathf.Max(0, Hours - amount);

        /// <summary>
        /// Applies a signed change to Funds, clamped at zero. Signed rather than gain-only
        /// because event choices need to be able to cost the player something — a choice layer
        /// where every option is a pure gain has no decision in it.
        /// </summary>
        public void AdjustFunds(int delta) => Funds = Mathf.Max(0, Funds + delta);

        /// <summary>Applies a signed change to Credibility, clamped at zero.</summary>
        // ponytail: floor only, no ceiling. Add a max if Credibility ever drives a threshold
        // that unbounded growth would trivialise.
        public void AdjustCredibility(int delta) =>
            Credibility = Mathf.Max(0, Credibility + delta);

        /// <summary>Ends the day: advances <see cref="Day"/> and refills <see cref="Hours"/>.</summary>
        public void AdvanceDay()
        {
            Day++;
            Hours = MaxHours;
        }

        /// <summary>
        /// Points the run at a chosen battle encounter: rebuilds the battle queue from its
        /// session and resets the queue index. Call before setting <see cref="PendingBattle"/>
        /// and loading <c>main.unity</c>.
        /// </summary>
        public void StartEncounter(List<List<EnemyData>> queue)
        {
            BattleQueue = queue;
            CurrentBattleIndex = 0;
        }

        /// <summary>Sets the encounter the battle scene should consume on load.</summary>
        public void SetPendingBattle(BattleEncounterData data) => PendingBattle = data;

        /// <summary>Clears <see cref="PendingBattle"/> — call once it's been consumed.</summary>
        public void ClearPendingBattle() => PendingBattle = null;

        /// <summary>Marks a map location as resolved so a non-repeatable one won't re-offer.</summary>
        public void MarkVisited(string locationId)
        {
            if (!string.IsNullOrEmpty(locationId))
                VisitedLocationIds.Add(locationId);
        }

        /// <summary>True if <paramref name="locationId"/> has already been resolved.</summary>
        public bool IsVisited(string locationId) => VisitedLocationIds.Contains(locationId);

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
            List<List<EnemyData>> battleQueue = null,
            bool isCampaignRun = false,
            int maxHours = 3,
            int seed = 0
        )
        {
            Current = new RunState
            {
                // 0 means "give me a run I didn't choose" — the normal play path. Any other
                // value is someone deliberately replaying a specific campaign.
                Seed = seed != 0 ? seed : Environment.TickCount,
                Origin = origin,
                Deck = starterDeck != null ? new List<CardData>(starterDeck) : new List<CardData>(),
                Relics = new List<RelicData>(),
                CurrentBattleIndex = 0,
                BattleQueue = battleQueue,
                IsCampaignRun = isCampaignRun,
                MaxHours = maxHours,
                Hours = maxHours,
                VisitedLocationIds = new HashSet<string>(),
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
