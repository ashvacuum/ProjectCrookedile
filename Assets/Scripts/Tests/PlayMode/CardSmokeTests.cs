using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Gameplay.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Crookedile.Tests
{
    /// <summary>
    /// Plays every card in the card database once against a single enemy held receptive, neutral
    /// and hostile, with AP and Support topped up so cost never blocks it, then plays out one
    /// enemy turn. Fails on an event loop, a runaway chain of plays, any logged error, or a battle
    /// that stops returning to the player's turn. Says nothing about whether the card does what
    /// its text says.
    /// </summary>
    public class CardSmokeTests
    {
        public enum EnemyMood
        {
            Receptive,
            Neutral,
            Hostile,
        }

        /// <summary>Enemy used for every case; the first enemy in the database if it is missing.</summary>
        private const string EnemyName = "Swing Voter";

        /// <summary>Frames to wait for a phase. The simulation has no delays, so a healthy phase takes a few.</summary>
        private const int MaxFramesPerPhase = 600;

        /// <summary>Plays and replays one card may cause before it counts as runaway.</summary>
        private const int MaxPlaysFromOneCard = 30;

        /// <summary>Events one card play may publish before it counts as runaway.</summary>
        private const int MaxEventsFromOneCard = 5000;

        private GameObject _battleObject;

        public static IEnumerable<TestCaseData> Cases()
        {
            var cards = CardDatabase.Shared;
            if (cards == null)
                yield break;
            foreach (var card in cards.GetAll().Where(c => c != null).OrderBy(c => c.CardName))
            foreach (EnemyMood mood in System.Enum.GetValues(typeof(EnemyMood)))
                yield return new TestCaseData(card.ID, mood).SetName($"{card.CardName} vs {mood}").Returns(null);
        }

        [TearDown]
        public void TearDown()
        {
            if (_battleObject != null)
                Object.Destroy(_battleObject);
        }

        [UnityTest]
        [TestCaseSource(nameof(Cases))]
        public IEnumerator PlayingTheCardDoesNotLoopOrBreak(string cardId, EnemyMood mood)
        {
            var card = CardDatabase.Shared.GetByID(cardId);
            Assert.IsNotNull(card, $"No card with ID {cardId}.");
            var enemy = PickEnemy();
            Assert.IsNotNull(enemy, "The enemy database is empty.");

            int plays = 0, events = 0;
            bool ended = false;
            CardChoiceRequest pendingChoice = null;
            void OnPlayed(CardPlayedEvent e) => plays++;
            void OnReplayed(CardReplayedEvent e) => plays++;
            void OnEnded(BattleEndedEvent e) => ended = true;
            void OnChoice(CardChoiceRequest e) => pendingChoice = e;
            void OnAny() => events++;
            EventBus.Subscribe<CardPlayedEvent>(OnPlayed);
            EventBus.Subscribe<CardReplayedEvent>(OnReplayed);
            EventBus.Subscribe<BattleEndedEvent>(OnEnded);
            EventBus.AnyEventPublished += OnAny;
            int loopsBefore = EventBus.LoopsBroken;

            try
            {
                _battleObject = new GameObject("CardSmokeBattle");
                var bm = _battleObject.AddComponent<BattleManager>();
                bm.CardChoicePrompt = OnChoice;
                bm.ConfigureForSimulation(new OriginPassive[0]);
                bm.StartBattle(
                    new BattleSetup
                    {
                        playerOrigin = OriginType.FaithLeader,
                        originDatabase = OriginDatabase.Shared,
                        // Copies only, so the card is in the opening hand whatever the draw.
                        playerDeck = Enumerable.Repeat(card, 5).ToList(),
                        enemies = new List<EnemyData> { enemy },
                    }
                );

                yield return WaitForPlayerTurn(bm, () => ended, "the first player turn");
                if (ended)
                    Assert.Inconclusive("The battle ended before the card could be played.");

                var target = bm.Enemies[0];
                bm.SetFocusedEnemy(0);
                target.Stats.SetHostility(HostilityFor(mood, target.Stats));
                if (MoodOf(target.Stats) != mood)
                    Assert.Inconclusive($"{enemy.EnemyName} cannot be held {mood} (hostility limits).");
                bm.PlayerStats.GainActionPoints(99);
                bm.GainSupport(99);

                int handIndex = IndexOf(bm.PlayerDeck.Hand, card);
                if (handIndex < 0)
                    Assert.Inconclusive("The card was not in the opening hand.");
                if (!bm.CanPlayCard(card))
                    Assert.Inconclusive("Unplayable here even with AP and Support topped up.");

                plays = 0;
                events = 0;
                bm.RequestPlayCard(card, handIndex);
                yield return WaitForPlayerTurn(bm, () => ended, "the card to resolve", () => AnswerChoice(ref pendingChoice));
                Assert.LessOrEqual(plays, MaxPlaysFromOneCard, "Runaway plays/replays from one card.");
                Assert.LessOrEqual(events, MaxEventsFromOneCard, "Runaway events from one card.");

                if (!ended)
                {
                    bm.RequestEndTurn();
                    int turn = bm.CurrentTurn;
                    yield return WaitFor(
                        () => ended || (bm.CurrentTurn != turn && IsPlayerReady(bm)),
                        "the enemy turn to finish",
                        () => AnswerChoice(ref pendingChoice)
                    );
                }

                Assert.AreEqual(loopsBefore, EventBus.LoopsBroken, "The event bus broke an event loop.");
            }
            finally
            {
                EventBus.Unsubscribe<CardPlayedEvent>(OnPlayed);
                EventBus.Unsubscribe<CardReplayedEvent>(OnReplayed);
                EventBus.Unsubscribe<BattleEndedEvent>(OnEnded);
                EventBus.AnyEventPublished -= OnAny;
            }
        }

        private static EnemyData PickEnemy()
        {
            var all = Resources.Load<EnemyDatabase>("Databases/EnemyDatabase")?.GetAll().Where(e => e != null).ToList();
            if (all == null || all.Count == 0)
                return null;
            return all.FirstOrDefault(e => e.EnemyName == EnemyName) ?? all[0];
        }

        private static int HostilityFor(EnemyMood mood, Gameplay.BattleStats stats) =>
            mood switch
            {
                EnemyMood.Receptive => Mathf.Max(stats.MinHostility, -stats.NeutralZone - 1),
                EnemyMood.Hostile => Mathf.Min(stats.MaxHostility, stats.NeutralZone + 1),
                _ => 0,
            };

        private static EnemyMood MoodOf(Gameplay.BattleStats stats) =>
            stats.IsReceptive ? EnemyMood.Receptive
            : stats.IsHostile ? EnemyMood.Hostile
            : EnemyMood.Neutral;

        private static int IndexOf(IReadOnlyList<CardData> hand, CardData card)
        {
            for (int i = 0; i < hand.Count; i++)
                if (hand[i] == card)
                    return i;
            return -1;
        }

        private static bool IsPlayerReady(BattleManager bm) =>
            bm.CurrentState == BattleState.PlayerTurn && bm.IsPlayerTurn && !bm.IsCardResolving;

        /// <summary>Confirms a pending card choice with the first cards offered.</summary>
        private static void AnswerChoice(ref CardChoiceRequest pending)
        {
            if (pending == null)
                return;
            var choice = pending;
            pending = null;
            var pool = choice.Choices?.ToList() ?? new List<CardData>();
            choice.OnConfirmed?.Invoke(pool.Take(System.Math.Min(choice.RequiredCount, pool.Count)).ToList());
        }

        private delegate void FrameAction();

        private static IEnumerator WaitForPlayerTurn(BattleManager bm, System.Func<bool> ended, string what, FrameAction each = null) =>
            WaitFor(() => ended() || IsPlayerReady(bm), what, each);

        private static IEnumerator WaitFor(System.Func<bool> done, string what, FrameAction each = null)
        {
            for (int frame = 0; frame < MaxFramesPerPhase; frame++)
            {
                each?.Invoke();
                if (done())
                    yield break;
                yield return null;
            }
            Assert.Fail($"Stuck waiting for {what} after {MaxFramesPerPhase} frames (a loop, or a play that never resolves).");
        }
    }
}
