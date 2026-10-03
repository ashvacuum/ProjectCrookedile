using System;
using System.IO;
using Crookedile.Data;
using Crookedile.Data.Save;
using NUnit.Framework;

namespace Crookedile.Tests
{
    /// <summary>The save debug tools: dumps, snapshots, wipe, edits and corruption.</summary>
    public class SaveDebugTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "crookedile-savedebug-" + Guid.NewGuid().ToString("N"));
            SaveSystem.UseRoot(_root);
            RunState.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            RunState.Clear();
            SaveSystem.UseRoot(null);
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        [Test]
        public void Describe_WithNoProfilesDoesNotCreateOne()
        {
            string dump = SaveDebug.Describe();

            StringAssert.Contains("No profiles.", dump);
            Assert.AreEqual(0, SaveSystem.Profiles.Count);
        }

        [Test]
        public void Describe_NamesTheRunsCards()
        {
            SaveSystem.StartNewRun(OriginType.NepoBaby, seed: 1);

            string dump = SaveDebug.Describe();

            StringAssert.Contains("Blow the Allowance", dump);
            StringAssert.Contains("Origin: NepoBaby", dump);
            StringAssert.Contains("(active)", dump);
        }

        [Test]
        public void Snapshot_RestoresTheSavedState()
        {
            var run = SaveSystem.StartNewRun(OriginType.FaithLeader, seed: 2);
            run.AdjustFunds(100);
            int funds = run.Funds;
            SaveDebug.SaveSnapshot("rich");

            SaveSystem.AbandonRun();
            Assert.IsFalse(SaveSystem.HasRunInProgress);

            Assert.IsTrue(SaveDebug.RestoreSnapshot("rich"));
            Assert.IsTrue(SaveSystem.HasRunInProgress);
            Assert.AreEqual(funds, SaveSystem.ContinueRun(null, out _).Funds);
            CollectionAssert.Contains(SaveDebug.ListSnapshots(), "rich");
        }

        [Test]
        public void Snapshot_RejectsPathTricks()
        {
            Assert.IsFalse(SaveDebug.SaveSnapshot("../escape"));
            Assert.IsFalse(SaveDebug.RestoreSnapshot("nope"));
        }

        [Test]
        public void Wipe_DeletesSavesButNothingElse()
        {
            SaveSystem.StartNewRun(OriginType.FaithLeader, seed: 3);
            SaveDebug.SaveSnapshot("keep");
            string foreign = Path.Combine(_root, "Unity", "other-data.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(foreign));
            File.WriteAllText(foreign, "not ours");

            SaveDebug.WipeAll();

            Assert.IsFalse(Directory.Exists(Path.Combine(_root, "profiles")));
            Assert.IsFalse(File.Exists(Path.Combine(_root, "profiles.idx")));
            Assert.IsTrue(File.Exists(foreign), "Wipe must not touch files the save system doesn't own.");
            CollectionAssert.Contains(SaveDebug.ListSnapshots(), "keep");
            Assert.AreEqual(0, SaveSystem.Profiles.Count);
        }

        [Test]
        public void SetCounter_PersistsAndZeroClears()
        {
            SaveDebug.SetCounter(ProfileCounters.RunsWon, 5);
            SaveSystem.Reload();
            Assert.AreEqual(5, SaveSystem.ActiveProfile.GetCounter(ProfileCounters.RunsWon));

            SaveDebug.SetCounter(ProfileCounters.RunsWon, 0);
            Assert.IsFalse(SaveSystem.ActiveProfile.Counters.ContainsKey(ProfileCounters.RunsWon));
        }

        [Test]
        public void RevokeUnlock_RemovesTheGrant()
        {
            SaveSystem.GrantUnlock("some-card");
            SaveSystem.MarkUnlocksSeen(new[] { "some-card" });

            SaveDebug.RevokeUnlock("some-card");

            Assert.IsFalse(SaveSystem.ActiveProfile.GrantedUnlocks.Contains("some-card"));
            Assert.IsFalse(SaveSystem.ActiveProfile.SeenUnlocks.Contains("some-card"));
        }

        [Test]
        public void CorruptRun_RecoversFromBackup_UnlessTheBackupGoesToo()
        {
            var run = SaveSystem.StartNewRun(OriginType.FaithLeader, seed: 4);
            SaveSystem.Checkpoint();

            SaveDebug.Corrupt(SaveDebug.SaveFile.Run);
            Assert.IsTrue(SaveSystem.TryReadRunSave(SaveSystem.ActiveProfile.Id, out _, out bool usedBackup));
            Assert.IsTrue(usedBackup);

            SaveDebug.Corrupt(SaveDebug.SaveFile.Run, backupToo: true);
            Assert.IsFalse(SaveSystem.TryReadRunSave(SaveSystem.ActiveProfile.Id, out _, out _));
        }

        [Test]
        public void FindCard_MatchesLooselyAndRejectsAmbiguity()
        {
            Assert.AreEqual("Trust Fund", SaveDebug.FindCard("trustfund", out _)?.CardName);
            Assert.AreEqual("Trust Fund", SaveDebug.FindCard("Trust  Fund!", out _)?.CardName);
            Assert.IsNull(SaveDebug.FindCard("zzzz-no-card", out string missing));
            Assert.IsNotNull(missing);
        }
    }
}
