using System.Collections.Generic;
using Crookedile.Data.Campaign;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Data.Save;

namespace Crookedile.Data
{
    /// <summary>Converting a run to and from its save data (<see cref="RunSaveData"/>).</summary>
    public partial class RunState
    {
        // Set by Restore: ConfigureTravel then keeps the saved clock and resolves this district.
        private bool _isRestored;
        private string _restoredDistrictName;

        /// <summary>Starts this run's unlock snapshot. Call once, when the run starts.</summary>
        public void SetUnlockedContent(IEnumerable<string> contentIds) =>
            UnlockedContent = new HashSet<string>(contentIds ?? new string[0]);

        /// <summary>
        /// The run as save data. <paramref name="openEventId"/> is the event the player is
        /// looking at but hasn't chosen in yet, so a resume reopens it.
        /// </summary>
        public RunSaveData ToSaveData(string openEventId = null)
        {
            var data = new RunSaveData
            {
                SavedUtcTicks = System.DateTime.UtcNow.Ticks,
                Origin = (int)Origin,
                IsCampaignRun = IsCampaignRun,
                Seed = Seed,
                RngState = Rng != null ? Rng.GetState() : new uint[4],
                Funds = Funds,
                Credibility = Credibility,
                MaxHours = MaxHours,
                Day = Day,
                MinutesRemaining = MinutesRemaining,
                ElapsedMinutes = ElapsedMinutes,
                DistrictName = CurrentDistrict != null ? CurrentDistrict.name : null,
                AllyIds = new List<string>(),
                VisitedLocationIds = new List<string>(VisitedLocationIds),
                Flags = new List<string>(Flags),
                TodaysLocationsDay = TodaysLocationsDay,
                TodaysLocationIds = new List<string>(),
                PendingBattleId = PendingBattle != null ? PendingBattle.ID : null,
                NextEncounterId = NextEncounter != null ? NextEncounter.ID : null,
                OpenEventId = openEventId,
                NextBattleHostility = NextBattleHostility,
                CurrentBattleIndex = CurrentBattleIndex,
                UnlockedContent = new List<string>(UnlockedContent),
                RunCounters = new Dictionary<string, int>(RunCounters),
            };

            foreach (var card in Deck)
                if (card != null)
                    data.Deck.Add(new RunSaveData.CardEntry { Id = card.ID, Upgraded = card.IsUpgraded });
            foreach (var ally in Allies)
                if (ally != null)
                    data.AllyIds.Add(ally.Id);
            foreach (var location in TodaysLocations)
                if (location != null)
                    data.TodaysLocationIds.Add(location.ID);

            if (BattleQueue != null)
            {
                data.BattleQueue = new List<List<string>>();
                foreach (var round in BattleQueue)
                {
                    var ids = new List<string>();
                    if (round != null)
                        foreach (var enemy in round)
                            if (enemy != null)
                                ids.Add(enemy.ID);
                    data.BattleQueue.Add(ids);
                }
            }
            return data;
        }

        /// <summary>
        /// Rebuilds a run from save data and makes it <see cref="Current"/>. Content that no
        /// longer resolves (a cut card or encounter) is dropped and listed in
        /// <paramref name="missing"/>, never fatal. <paramref name="openEvent"/> is the event to
        /// reopen, or null. Call <see cref="ConfigureTravel"/> afterwards, as for a new run.
        /// </summary>
        public static RunState Restore(
            RunSaveData data,
            SaveContent content,
            out EventEncounterData openEvent,
            out List<string> missing
        )
        {
            var lost = new List<string>();
            var run = new RunState
            {
                _isRestored = true,
                _restoredDistrictName = data.DistrictName,
                Origin = (OriginType)data.Origin,
                IsCampaignRun = data.IsCampaignRun,
                Seed = data.Seed,
                Rng = new RunRng(data.RngState),
                Funds = data.Funds,
                Credibility = data.Credibility,
                MaxHours = data.MaxHours,
                Day = data.Day,
                MinutesRemaining = data.MinutesRemaining,
                ElapsedMinutes = data.ElapsedMinutes,
                Deck = new List<CardData>(),
                Allies = new List<AllyData>(),
                VisitedLocationIds = new HashSet<string>(data.VisitedLocationIds),
                Flags = new HashSet<string>(data.Flags),
                NextBattleHostility = data.NextBattleHostility,
                CurrentBattleIndex = data.CurrentBattleIndex,
                UnlockedContent = new HashSet<string>(data.UnlockedContent),
                RunCounters = new Dictionary<string, int>(data.RunCounters),
            };

            foreach (var entry in data.Deck)
            {
                var card = content.Card(entry.Id);
                if (card == null)
                {
                    lost.Add($"card {entry.Id}");
                    continue;
                }
                run.Deck.Add(entry.Upgraded && card.CanUpgrade ? card.CreateUpgradedInstance() : card);
            }

            foreach (var id in data.AllyIds)
            {
                var ally = content.Ally(id);
                if (ally != null)
                    run.Allies.Add(ally);
                else
                    lost.Add($"ally {id}");
            }

            var todays = new List<EncounterData>();
            foreach (var id in data.TodaysLocationIds)
            {
                var encounter = content.Encounter(id);
                if (encounter != null)
                    todays.Add(encounter);
                else
                    lost.Add($"encounter {id}");
            }
            run.SetTodaysLocations(data.TodaysLocationsDay, todays);

            run.PendingBattle = Resolve<BattleEncounterData>(content, data.PendingBattleId, lost);
            run.NextEncounter = Resolve<EncounterData>(content, data.NextEncounterId, lost);
            openEvent = Resolve<EventEncounterData>(content, data.OpenEventId, lost);

            if (data.BattleQueue != null)
            {
                run.BattleQueue = new List<List<EnemyData>>();
                foreach (var round in data.BattleQueue)
                {
                    var enemies = new List<EnemyData>();
                    foreach (var id in round)
                    {
                        var enemy = content.Enemy(id);
                        if (enemy != null)
                            enemies.Add(enemy);
                        else
                            lost.Add($"enemy {id}");
                    }
                    run.BattleQueue.Add(enemies);
                }
            }

            missing = lost;
            Current = run;
            return run;
        }

        private static T Resolve<T>(SaveContent content, string id, List<string> lost)
            where T : EncounterData
        {
            if (string.IsNullOrEmpty(id))
                return null;
            if (content.Encounter(id) is T found)
                return found;
            lost.Add($"encounter {id}");
            return null;
        }
    }
}
