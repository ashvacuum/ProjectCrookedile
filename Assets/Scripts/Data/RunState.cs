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
    /// once the map and ally systems are built. Until then, everything that needs to
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
        /// Allies acquired this run. Their passives are registered with every battle's
        /// PassiveResolver, so they behave exactly like origin passives in battle.
        /// </summary>
        public List<AllyData> Allies { get; private set; }

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

        /// <summary>
        /// The run's campaign-level random stream, derived from <see cref="Seed"/>. Everything
        /// the seed is supposed to reproduce draws from here: reward offers, random-card
        /// outcomes, anything else decided on the map.
        ///
        /// <para><b>Battle RNG deliberately stays on <c>UnityEngine.Random</c>.</b> Two reasons:
        /// a shared global stream would make this seed depend on how many times combat happened
        /// to roll — so adding one shuffle anywhere silently changes every later campaign draw —
        /// and seeding battle would make reloading a fight replay identical draws.</para>
        ///
        /// Note the seed fixes the *map*; the stream still diverges if the player makes
        /// different choices, which is correct — identical play gives identical results.
        /// </summary>
        public System.Random Rng { get; private set; }

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

        /// <summary>
        /// Narrative flags set by event choices — "took the bribe", "met the Fixer's sister".
        /// The one thing <see cref="VisitedLocationIds"/> can't express: which *option* was
        /// picked, not merely which encounter was resolved.
        ///
        /// Free-form strings on purpose. An enum would need a code change per story beat, which
        /// is exactly the wrong friction for content authored over months.
        /// </summary>
        public HashSet<string> Flags { get; private set; }

        /// <summary>Sets a narrative flag. Idempotent; blank names ignored.</summary>
        public void SetFlag(string flag)
        {
            if (!string.IsNullOrWhiteSpace(flag))
                Flags.Add(flag.Trim());
        }

        /// <summary>Clears a narrative flag. No-op when unset.</summary>
        public void ClearFlag(string flag) => Flags.Remove(flag?.Trim());

        /// <summary>True when <paramref name="flag"/> has been set this run.</summary>
        public bool HasFlag(string flag) =>
            !string.IsNullOrWhiteSpace(flag) && Flags.Contains(flag.Trim());

        /// <summary>
        /// The locations currently on offer, and the day they were drawn for.
        ///
        /// Lives here rather than on the map screen because it has to survive a scene load: a
        /// battle unloads the campaign scene, and re-drawing the day on return would produce a
        /// *different* offering — the just-visited location is now in
        /// <see cref="VisitedLocationIds"/>, which shifts the exclude set and cascades through
        /// the weighted rolls. The player would leave to fight one location and come back to a
        /// different map for the same day.
        /// </summary>
        public List<EncounterData> TodaysLocations { get; private set; } =
            new List<EncounterData>();

        /// <summary>Day <see cref="TodaysLocations"/> was drawn for; -1 when nothing is drawn.</summary>
        public int TodaysLocationsDay { get; private set; } = -1;

        /// <summary>Records a freshly drawn day's offering.</summary>
        public void SetTodaysLocations(int day, List<EncounterData> locations)
        {
            TodaysLocationsDay = day;
            TodaysLocations = locations ?? new List<EncounterData>();
        }

        /// <summary>Drops a location from the current offering once it's been resolved.</summary>
        public void RemoveTodaysLocation(EncounterData encounter) =>
            TodaysLocations.Remove(encounter);

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
        public void AdjustCredibility(int delta) => Credibility = Mathf.Max(0, Credibility + delta);

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

        /// <summary>
        /// The encounter to resolve instead of returning to the map, when the one just finished
        /// chained forward, which only a chosen event option can do — via
        /// <c>GoToEncounterOutcome</c>. Encounters have no unconditional "and then" of their
        /// own: sequencing is a property of a choice, not of an encounter.
        ///
        /// Distinct from <see cref="PendingBattle"/>, which is specifically "the battle scene
        /// should load this on the next scene load". This one is a campaign-layer instruction:
        /// don't go back to roaming yet.
        /// </summary>
        public EncounterData NextEncounter { get; private set; }

        /// <summary>Queues the next encounter in a chain. Null clears it.</summary>
        public void SetNextEncounter(EncounterData encounter) => NextEncounter = encounter;

        /// <summary>Clears <see cref="NextEncounter"/> — call once it's been consumed.</summary>
        public void ClearNextEncounter() => NextEncounter = null;

        /// <summary>Marks a map location as resolved so a non-repeatable one won't re-offer.</summary>
        public void MarkVisited(string locationId)
        {
            if (!string.IsNullOrEmpty(locationId))
                VisitedLocationIds.Add(locationId);
        }

        /// <summary>True if <paramref name="locationId"/> has already been resolved.</summary>
        public bool IsVisited(string locationId) => VisitedLocationIds.Contains(locationId);

        #endregion

        #region Card choice
        /// <summary>
        /// A "pick a card from your deck" prompt raised by an outcome and drawn by the campaign
        /// screen. Runtime only — never serialized, never survives a scene load, because a choice
        /// is always opened and answered inside one screen.
        /// </summary>
        public class CardChoice
        {
            public string Prompt;
            public List<CardData> Candidates;
            public Action<CardData> OnPicked;
        }

        /// <summary>Non-null while the player owes a card pick. The screen draws it over everything.</summary>
        public CardChoice PendingCardChoice { get; private set; }

        /// <summary>
        /// Asks the player to pick one of <paramref name="candidates"/>. Ignored when the list is
        /// empty — an outcome with nothing to offer must no-op rather than open a picker with no
        /// answer, which the player could not get out of.
        /// </summary>
        public void RequestCardChoice(
            string prompt,
            List<CardData> candidates,
            Action<CardData> onPicked
        )
        {
            if (candidates == null || candidates.Count == 0 || onPicked == null)
                return;

            PendingCardChoice = new CardChoice
            {
                Prompt = prompt,
                Candidates = candidates,
                OnPicked = onPicked,
            };
        }

        /// <summary>Answers the open choice. Clears it before invoking so the callback can raise another.</summary>
        public void ResolveCardChoice(CardData picked)
        {
            CardChoice choice = PendingCardChoice;
            if (choice == null)
                return;

            PendingCardChoice = null;

            if (picked != null)
                choice.OnPicked(picked);
        }

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
        /// <param name="maxHours">
        /// Hours per day. Overridden by the origin's own <c>MaxHours</c> when that is non-zero,
        /// so day length can differ per archetype without every call site knowing.
        /// </param>
        /// <remarks>
        /// Starting Funds and Credibility come from <see cref="OriginDatabase"/> rather than
        /// parameters: they're tuned design data, and resolving them here means no run-creation
        /// path can silently start a player at zero by forgetting to pass them.
        /// </remarks>
        public static RunState Create(
            OriginType origin,
            List<CardData> starterDeck,
            List<List<EnemyData>> battleQueue = null,
            bool isCampaignRun = false,
            int maxHours = 3,
            int seed = 0
        )
        {
            var start = OriginDatabase.Shared?.GetCampaignStart(origin) ?? (0, 0, 0);
            int hours = start.maxHours > 0 ? start.maxHours : maxHours;
            int resolvedSeed = seed != 0 ? seed : Environment.TickCount;

            Current = new RunState
            {
                // 0 means "give me a run I didn't choose" — the normal play path. Any other
                // value is someone deliberately replaying a specific campaign.
                Seed = resolvedSeed,
                // Offset so the reward stream doesn't mirror the encounter draws, which derive
                // from the same seed in EncounterPoolData.
                Rng = new System.Random(unchecked(resolvedSeed * 31 + 17)),
                Funds = start.funds,
                Credibility = start.credibility,
                Origin = origin,
                Deck = starterDeck != null ? new List<CardData>(starterDeck) : new List<CardData>(),
                Allies = new List<AllyData>(),
                CurrentBattleIndex = 0,
                BattleQueue = battleQueue,
                IsCampaignRun = isCampaignRun,
                MaxHours = hours,
                Hours = hours,
                VisitedLocationIds = new HashSet<string>(),
                Flags = new HashSet<string>(),
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
        /// Swaps a deck card for its upgraded version, in place. No-op when the card isn't in the
        /// deck or has no upgrade authored (<see cref="CardData.CanUpgrade"/>).
        ///
        /// <para>The replacement is a runtime <c>Instantiate</c> clone, so the deck ends up holding
        /// an instance rather than the shared asset — deliberate, and the same thing the battle-side
        /// upgrade does. It lives as long as the run references it.</para>
        /// </summary>
        public void UpgradeCardInDeck(CardData card)
        {
            if (card == null || !card.CanUpgrade)
                return;

            int index = Deck.IndexOf(card);
            if (index < 0)
                return;

            Deck[index] = card.CreateUpgradedInstance();
        }

        /// <summary>Deck cards that can still be upgraded, optionally of one type only.</summary>
        public List<CardData> GetUpgradeableCards(CardType? ofType = null)
        {
            var results = new List<CardData>();

            for (int i = 0; i < Deck.Count; i++)
            {
                CardData card = Deck[i];
                if (card == null || !card.CanUpgrade)
                    continue;
                if (ofType.HasValue && card.CardType != ofType.Value)
                    continue;

                results.Add(card);
            }

            return results;
        }

        /// <summary>
        /// Adds <paramref name="ally"/> to the run. Duplicates and <c>null</c> are ignored
        /// (allies are unique per run, StS-style).
        /// </summary>
        public void AddAlly(AllyData ally)
        {
            if (ally != null && !Allies.Contains(ally))
                Allies.Add(ally);
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
