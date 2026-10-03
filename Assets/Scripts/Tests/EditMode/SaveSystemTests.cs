using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Data.Save;
using Crookedile.Data.Unlocks;
using NUnit.Framework;
using UnityEngine;

namespace Crookedile.Tests
{
    /// <summary>
    /// Profiles, run save/continue/end and unlocks, against the real card database and a
    /// temporary save folder. Needs the Unity editor (assets load from Resources).
    /// </summary>
    public class SaveSystemTests
    {
        private string _root;
        private CardDatabase _cards;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "crookedile-savesystem-" + Guid.NewGuid().ToString("N"));
            SaveSystem.UseRoot(_root);
            RunState.Clear();
            UnlockRules.LocksEnabled = true;
            _cards = Resources.Load<CardDatabase>("Databases/CardDatabase");
            Assert.IsNotNull(_cards, "CardDatabase missing from Resources/Databases.");
        }

        [TearDown]
        public void TearDown()
        {
            UnlockRules.LocksEnabled = false;
            RunState.Clear();
            SaveSystem.UseRoot(null);
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        /// <summary>Forgets everything cached, as a restart of the game would.</summary>
        private void Relaunch()
        {
            RunState.Clear();
            SaveSystem.UseRoot(_root);
        }

        private CardData TrustFund() => _cards.GetAll().First(c => c.CardName == "Trust Fund");

        [Test]
        public void ActiveProfile_IsCreatedOnFirstUseAndSurvivesARelaunch()
        {
            string id = SaveSystem.ActiveProfile.Id;

            Relaunch();

            Assert.AreEqual(id, SaveSystem.ActiveProfile.Id);
            Assert.AreEqual(1, SaveSystem.Profiles.Count);
        }

        [Test]
        public void CreateProfile_StopsAtTheSlotLimit()
        {
            for (int i = SaveSystem.Profiles.Count; i < SaveSystem.MaxProfiles; i++)
                Assert.IsNotNull(SaveSystem.CreateProfile($"P{i}"));

            Assert.IsNull(SaveSystem.CreateProfile("one too many"));
        }

        [Test]
        public void Profiles_KeepTheirOwnRuns()
        {
            var first = SaveSystem.ActiveProfile;
            SaveSystem.StartNewRun(OriginType.FaithLeader, seed: 1);
            var second = SaveSystem.CreateProfile("Second");

            Assert.IsFalse(SaveSystem.HasRunInProgress, "A new profile starts without a run.");
            Assert.IsTrue(SaveSystem.SelectProfile(first.Id));
            Assert.IsTrue(SaveSystem.HasRunInProgress);
            Assert.AreNotEqual(first.Id, second.Id);
        }

        [Test]
        public void DeleteProfile_RemovesItsFilesAndPicksAnother()
        {
            var keep = SaveSystem.ActiveProfile;
            var doomed = SaveSystem.CreateProfile("Doomed");
            SaveSystem.StartNewRun(OriginType.FaithLeader, seed: 2);

            SaveSystem.DeleteProfile(doomed.Id);

            Assert.IsFalse(Directory.Exists(Path.Combine(_root, "profiles", doomed.Id)));
            Assert.AreEqual(keep.Id, SaveSystem.ActiveProfile.Id);
        }

        [Test]
        public void StartNewRun_SavesACampaignRunWithTheStarterDeck()
        {
            var run = SaveSystem.StartNewRun(OriginType.NepoBaby, seed: 3);

            Assert.IsTrue(SaveSystem.HasRunInProgress);
            Assert.IsTrue(run.IsCampaignRun);
            Assert.AreEqual(10, run.Deck.Count, "Nepo Baby's authored starter is 10 cards.");
            Assert.AreEqual(1, SaveSystem.ActiveProfile.GetCounter(ProfileCounters.RunsStarted));
        }

        [Test]
        public void ContinueRun_RestoresTheRunAsSaved()
        {
            var run = SaveSystem.StartNewRun(OriginType.NepoBaby, seed: 4);
            run.AdjustFunds(37);
            run.AdjustCredibility(-5);
            run.AdvanceDay();
            run.SetFlag("took_the_bribe");
            run.AddCardToDeck(TrustFund());
            run.UpgradeCardInDeck(run.Deck[0]);
            run.RecordBattleVictory();
            for (int i = 0; i < 5; i++)
                run.Rng.Next();
            SaveSystem.Checkpoint();

            var expectedDeck = run.Deck.Select(c => (c.ID, c.IsUpgraded)).ToList();
            int funds = run.Funds;
            int credibility = run.Credibility;
            var expectedRolls = Enumerable.Range(0, 10).Select(_ => run.Rng.Next(1000)).ToList();

            Relaunch();
            var restored = SaveSystem.ContinueRun(null, out var openEvent);

            Assert.IsNotNull(restored);
            Assert.AreSame(restored, RunState.Current);
            Assert.IsNull(openEvent);
            Assert.AreEqual(OriginType.NepoBaby, restored.Origin);
            Assert.AreEqual(funds, restored.Funds);
            Assert.AreEqual(credibility, restored.Credibility);
            Assert.AreEqual(2, restored.Day);
            Assert.IsTrue(restored.HasFlag("took_the_bribe"));
            CollectionAssert.AreEqual(expectedDeck, restored.Deck.Select(c => (c.ID, c.IsUpgraded)).ToList());
            Assert.AreEqual(1, restored.RunCounters[ProfileCounters.BattlesWon]);
            CollectionAssert.AreEqual(
                expectedRolls,
                Enumerable.Range(0, 10).Select(_ => restored.Rng.Next(1000)).ToList(),
                "The run's random stream must continue exactly where it was saved."
            );
        }

        [Test]
        public void ContinueRun_WithNothingSavedReturnsNull()
        {
            Assert.IsNull(SaveSystem.ContinueRun(null, out _));
        }

        [Test]
        public void TestRuns_NeverWriteASave()
        {
            RunState.Create(OriginType.FaithLeader, new List<CardData>(), isCampaignRun: false);

            SaveSystem.Checkpoint();

            Assert.IsFalse(SaveSystem.HasRunInProgress);
        }

        [Test]
        public void EndRun_WinningAsNepoBabyUnlocksTrustFund()
        {
            var trustFund = TrustFund();
            Assert.IsTrue(trustFund.IsUnlockable);
            Assert.IsFalse(UnlockRules.IsUnlocked(trustFund, SaveSystem.ActiveProfile));
            SaveSystem.StartNewRun(OriginType.NepoBaby, seed: 5);
            RunState.Current.RecordBattleVictory();

            var unlocked = SaveSystem.EndRun(victory: true);

            CollectionAssert.Contains(unlocked, trustFund);
            Assert.IsNull(RunState.Current);
            Assert.IsFalse(SaveSystem.HasRunInProgress);
            var profile = SaveSystem.ActiveProfile;
            Assert.AreEqual(1, profile.GetCounter(ProfileCounters.RunsWon));
            Assert.AreEqual(1, profile.GetCounter(ProfileCounters.RunsWonAs(OriginType.NepoBaby)));
            Assert.AreEqual(1, profile.GetCounter(ProfileCounters.BattlesWon));
            Assert.IsTrue(SaveSystem.GetUnlocks().First(u => u.Card == trustFund).Unlocked);
        }

        [Test]
        public void EndRun_LosingUnlocksNothing()
        {
            SaveSystem.StartNewRun(OriginType.NepoBaby, seed: 6);

            var unlocked = SaveSystem.EndRun(victory: false);

            CollectionAssert.IsEmpty(unlocked);
            Assert.AreEqual(1, SaveSystem.ActiveProfile.GetCounter(ProfileCounters.RunsLost));
            Assert.IsFalse(UnlockRules.IsUnlocked(TrustFund(), SaveSystem.ActiveProfile));
        }

        [Test]
        public void Unlocks_ApplyFromTheNextRun()
        {
            var trustFund = TrustFund();
            var run = SaveSystem.StartNewRun(OriginType.NepoBaby, seed: 7);
            Assert.IsFalse(UnlockRules.IsAvailableThisRun(trustFund));

            SaveSystem.GrantUnlock(trustFund.ID);
            Assert.IsFalse(UnlockRules.IsAvailableThisRun(trustFund), "A mid-run unlock waits for the next run.");

            SaveSystem.EndRun(victory: false);
            SaveSystem.StartNewRun(OriginType.NepoBaby, seed: 8);
            Assert.IsTrue(UnlockRules.IsAvailableThisRun(trustFund));
        }

        [Test]
        public void GrantedUnlocks_AndSeenMarks_SurviveARelaunch()
        {
            var trustFund = TrustFund();
            SaveSystem.GrantUnlock(trustFund.ID);
            SaveSystem.MarkUnlocksSeen(new[] { trustFund.ID });

            Relaunch();

            var status = SaveSystem.GetUnlocks().First(u => u.Card == trustFund);
            Assert.IsTrue(status.Unlocked);
            Assert.IsTrue(status.Seen);
        }

        [Test]
        public void UnlockAll_UnlocksEveryCard()
        {
            SaveSystem.SetUnlockAll(true);

            Assert.IsTrue(SaveSystem.GetUnlocks().All(u => u.Unlocked));
        }

        [Test]
        public void Restore_DropsContentThatNoLongerExists()
        {
            var real = _cards.GetAll().First(c => !c.IsUpgraded);
            var data = new RunSaveData { Day = 1, MaxHours = 8 };
            data.Deck.Add(new RunSaveData.CardEntry { Id = real.ID });
            data.Deck.Add(new RunSaveData.CardEntry { Id = "no-such-card" });

            var run = RunState.Restore(data, SaveContent.Load(null), out _, out var missing);

            Assert.AreEqual(1, run.Deck.Count);
            Assert.AreSame(real, run.Deck[0]);
            CollectionAssert.Contains(missing, "card no-such-card");
        }

        [Test]
        public void CorruptRunSave_FallsBackToItsBackup()
        {
            var run = SaveSystem.StartNewRun(OriginType.FaithLeader, seed: 9);
            run.AdjustFunds(10);
            SaveSystem.Checkpoint();
            int fundsBeforeLastSave = run.Funds;
            run.AdjustFunds(10);
            SaveSystem.Checkpoint();
            string path = Path.Combine(_root, "profiles", SaveSystem.ActiveProfile.Id, "run.sav");
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });

            Relaunch();
            var restored = SaveSystem.ContinueRun(null, out _);

            Assert.IsNotNull(restored);
            Assert.AreEqual(fundsBeforeLastSave, restored.Funds);
        }

        [Test]
        public void CardIds_AreTheAssetGuids()
        {
#if UNITY_EDITOR
            foreach (var card in _cards.GetAll())
            {
                string path = UnityEditor.AssetDatabase.GetAssetPath(card);
                Assert.AreEqual(UnityEditor.AssetDatabase.AssetPathToGUID(path), card.ID, card.name);
            }
#endif
        }
    }
}
