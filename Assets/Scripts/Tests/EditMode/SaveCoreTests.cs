using System;
using System.Collections.Generic;
using System.IO;
using Crookedile.Data;
using Crookedile.Data.Save;
using NUnit.Framework;

namespace Crookedile.Tests
{
    /// <summary>The Unity-free save core: envelope, file store, RNG and the save data formats.</summary>
    public class SaveCoreTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "crookedile-save-tests-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        [Test]
        public void Crc32_MatchesTheStandardCheckValue()
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes("123456789");
            Assert.AreEqual(0xCBF43926u, Crc32.Compute(bytes));
        }

        [Test]
        public void Envelope_RoundTripsPayloadAndSchemaVersion()
        {
            var payload = new byte[] { 1, 2, 3, 250 };
            byte[] file = SaveEnvelope.Wrap(SaveKind.Run, 7, payload);

            byte[] back = SaveEnvelope.Unwrap(file, SaveKind.Run, out ushort version);

            CollectionAssert.AreEqual(payload, back);
            Assert.AreEqual(7, version);
        }

        [Test]
        public void Envelope_RejectsAFlippedPayloadBit()
        {
            byte[] file = SaveEnvelope.Wrap(SaveKind.Run, 1, new byte[] { 10, 20, 30 });
            file[file.Length - 1] ^= 0x01;

            Assert.Throws<SaveCorruptException>(() => SaveEnvelope.Unwrap(file, SaveKind.Run, out _));
        }

        [Test]
        public void Envelope_RejectsTheWrongKind()
        {
            byte[] file = SaveEnvelope.Wrap(SaveKind.Profile, 1, new byte[] { 1 });

            Assert.Throws<SaveCorruptException>(() => SaveEnvelope.Unwrap(file, SaveKind.Run, out _));
        }

        [Test]
        public void Envelope_RejectsTruncatedFiles()
        {
            byte[] file = SaveEnvelope.Wrap(SaveKind.Run, 1, new byte[] { 1, 2, 3 });
            Array.Resize(ref file, file.Length - 2);

            Assert.Throws<SaveCorruptException>(() => SaveEnvelope.Unwrap(file, SaveKind.Run, out _));
        }

        [Test]
        public void Store_WritesThenReads()
        {
            var store = new SaveFileStore(_root);
            byte[] file = SaveEnvelope.Wrap(SaveKind.Run, 1, new byte[] { 9 });

            store.Write("a/run.sav", file);
            bool ok = store.TryRead("a/run.sav", f => SaveEnvelope.Unwrap(f, SaveKind.Run, out _), out var payload, out bool usedBackup);

            Assert.IsTrue(ok);
            Assert.IsFalse(usedBackup);
            CollectionAssert.AreEqual(new byte[] { 9 }, payload);
            Assert.IsFalse(File.Exists(store.PathOf("a/run.sav") + ".tmp"));
        }

        [Test]
        public void Store_FallsBackToTheBackupWhenTheMainFileIsCorrupt()
        {
            var store = new SaveFileStore(_root);
            store.Write("run.sav", SaveEnvelope.Wrap(SaveKind.Run, 1, new byte[] { 1 }));
            store.Write("run.sav", SaveEnvelope.Wrap(SaveKind.Run, 1, new byte[] { 2 }));
            File.WriteAllBytes(store.PathOf("run.sav"), new byte[] { 0, 1, 2 });

            bool ok = store.TryRead("run.sav", f => SaveEnvelope.Unwrap(f, SaveKind.Run, out _), out var payload, out bool usedBackup);

            Assert.IsTrue(ok);
            Assert.IsTrue(usedBackup);
            CollectionAssert.AreEqual(new byte[] { 1 }, payload);
        }

        [Test]
        public void Store_ReportsFailureWhenNothingIsReadable()
        {
            var store = new SaveFileStore(_root);

            bool ok = store.TryRead("missing.sav", f => f, out _, out _);

            Assert.IsFalse(ok);
        }

        [Test]
        public void Store_DeleteRemovesFileAndBackup()
        {
            var store = new SaveFileStore(_root);
            store.Write("run.sav", SaveEnvelope.Wrap(SaveKind.Run, 1, new byte[] { 1 }));
            store.Write("run.sav", SaveEnvelope.Wrap(SaveKind.Run, 1, new byte[] { 2 }));

            store.Delete("run.sav");

            Assert.IsFalse(store.Exists("run.sav"));
        }

        [Test]
        public void RunRng_SameSeedGivesTheSameSequence()
        {
            var a = new RunRng(12345);
            var b = new RunRng(12345);
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(a.Next(1000), b.Next(1000));
        }

        [Test]
        public void RunRng_ResumesExactlyFromSavedState()
        {
            var rng = new RunRng(42);
            for (int i = 0; i < 17; i++)
                rng.Next();
            var resumed = new RunRng(rng.GetState());

            for (int i = 0; i < 50; i++)
                Assert.AreEqual(rng.NextDouble(), resumed.NextDouble());
        }

        [Test]
        public void RunRng_StaysInRange()
        {
            var rng = new RunRng(7);
            for (int i = 0; i < 10000; i++)
            {
                int n = rng.Next(-5, 5);
                Assert.That(n, Is.InRange(-5, 4));
                double d = rng.NextDouble();
                Assert.That(d, Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
                Assert.That(rng.Next(3), Is.InRange(0, 2));
            }
        }

        [Test]
        public void Profile_RoundTrips()
        {
            var profile = new ProfileData
            {
                Id = "p1",
                DisplayName = "Joevi",
                CreatedUtcTicks = 123,
                LastPlayedUtcTicks = 456,
                UnlockAll = true,
            };
            profile.AddToCounter(ProfileCounters.RunsWon, 3);
            profile.AddToCounter(ProfileCounters.RunsWonAs(OriginType.NepoBaby), 1);
            profile.GrantedUnlocks.Add("card-a");
            profile.SeenUnlocks.Add("card-b");

            var back = ProfileData.FromBytes(profile.ToBytes());

            Assert.AreEqual("p1", back.Id);
            Assert.AreEqual("Joevi", back.DisplayName);
            Assert.AreEqual(123, back.CreatedUtcTicks);
            Assert.AreEqual(456, back.LastPlayedUtcTicks);
            Assert.IsTrue(back.UnlockAll);
            Assert.AreEqual(3, back.GetCounter(ProfileCounters.RunsWon));
            Assert.AreEqual(1, back.GetCounter("runs_won_nepobaby"));
            CollectionAssert.AreEquivalent(new[] { "card-a" }, back.GrantedUnlocks);
            CollectionAssert.AreEquivalent(new[] { "card-b" }, back.SeenUnlocks);
        }

        [Test]
        public void ProfileIndex_RoundTrips()
        {
            var index = new ProfileIndex { ActiveProfileId = "b" };
            index.Profiles.Add(new ProfileIndex.Entry { Id = "a", DisplayName = "A" });
            index.Profiles.Add(new ProfileIndex.Entry { Id = "b", DisplayName = null });

            var back = ProfileIndex.FromBytes(index.ToBytes());

            Assert.AreEqual("b", back.ActiveProfileId);
            Assert.AreEqual(2, back.Profiles.Count);
            Assert.AreEqual("A", back.Profiles[0].DisplayName);
            Assert.IsNull(back.Profiles[1].DisplayName);
        }

        [Test]
        public void RunSave_RoundTripsEveryField()
        {
            var run = new RunSaveData
            {
                SavedUtcTicks = 99,
                Origin = 1,
                IsCampaignRun = true,
                Seed = -77,
                RngState = new uint[] { 1, 2, 3, uint.MaxValue },
                Funds = 600,
                Credibility = 65,
                MaxHours = 8,
                Day = 3,
                MinutesRemaining = 200,
                ElapsedMinutes = 280,
                DistrictName = "Quiapo",
                TodaysLocationsDay = 3,
                PendingBattleId = "battle",
                NextEncounterId = null,
                OpenEventId = "event",
                NextBattleHostility = 2,
                BattleQueue = new List<List<string>> { new List<string> { "e1", "e2" }, new List<string>() },
                CurrentBattleIndex = 1,
            };
            run.Deck.Add(new RunSaveData.CardEntry { Id = "c1", Upgraded = false });
            run.Deck.Add(new RunSaveData.CardEntry { Id = "c2", Upgraded = true });
            run.AllyIds.Add("ally");
            run.VisitedLocationIds.Add("loc");
            run.Flags.Add("took_the_bribe");
            run.TodaysLocationIds.Add("loc2");
            run.UnlockedContent.Add("trust-fund");
            run.RunCounters["battles_won"] = 4;

            var back = RunSaveData.FromBytes(run.ToBytes());

            Assert.AreEqual(99, back.SavedUtcTicks);
            Assert.AreEqual(1, back.Origin);
            Assert.IsTrue(back.IsCampaignRun);
            Assert.AreEqual(-77, back.Seed);
            CollectionAssert.AreEqual(run.RngState, back.RngState);
            Assert.AreEqual(600, back.Funds);
            Assert.AreEqual(65, back.Credibility);
            Assert.AreEqual(8, back.MaxHours);
            Assert.AreEqual(3, back.Day);
            Assert.AreEqual(200, back.MinutesRemaining);
            Assert.AreEqual(280, back.ElapsedMinutes);
            Assert.AreEqual("Quiapo", back.DistrictName);
            Assert.AreEqual(2, back.Deck.Count);
            Assert.AreEqual("c2", back.Deck[1].Id);
            Assert.IsTrue(back.Deck[1].Upgraded);
            CollectionAssert.AreEqual(new[] { "ally" }, back.AllyIds);
            CollectionAssert.AreEqual(new[] { "loc" }, back.VisitedLocationIds);
            CollectionAssert.AreEqual(new[] { "took_the_bribe" }, back.Flags);
            Assert.AreEqual(3, back.TodaysLocationsDay);
            CollectionAssert.AreEqual(new[] { "loc2" }, back.TodaysLocationIds);
            Assert.AreEqual("battle", back.PendingBattleId);
            Assert.IsNull(back.NextEncounterId);
            Assert.AreEqual("event", back.OpenEventId);
            Assert.AreEqual(2, back.NextBattleHostility);
            Assert.AreEqual(2, back.BattleQueue.Count);
            CollectionAssert.AreEqual(new[] { "e1", "e2" }, back.BattleQueue[0]);
            Assert.AreEqual(1, back.CurrentBattleIndex);
            CollectionAssert.AreEqual(new[] { "trust-fund" }, back.UnlockedContent);
            Assert.AreEqual(4, back.RunCounters["battles_won"]);
        }

        [Test]
        public void RunSave_WithoutABattleQueueRoundTripsAsNull()
        {
            var back = RunSaveData.FromBytes(new RunSaveData { BattleQueue = null }.ToBytes());

            Assert.IsNull(back.BattleQueue);
        }

        [Test]
        public void RunSave_ReadAsAProfileIsRejected()
        {
            byte[] file = new RunSaveData().ToBytes();

            Assert.Throws<SaveCorruptException>(() => ProfileData.FromBytes(file));
        }
    }
}
