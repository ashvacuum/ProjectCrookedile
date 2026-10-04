using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crookedile.Data;
using Crookedile.Data.Campaign;
using Crookedile.Data.Cards;
using Crookedile.Gameplay.Battle;

namespace Crookedile.EditorTools.Playtest
{
    /// <summary>
    /// Plays one whole campaign run with a bot. Mirrors CampaignFlow (draw the day, enter,
    /// resolve chains, end the day) and PostBattleFlow (victory → reward, extra rounds, defeat
    /// ends the run) through the same RunState / pool / option calls, minus scenes and UI.
    /// </summary>
    // ponytail: a copy of CampaignFlow's control flow, not a shared one. If CampaignFlow's rules
    // change (boss gating, locations per day), change them here too — or extract a UI-free
    // CampaignDriver both call once the flow stops moving.
    public static class CampaignHarness
    {
        /// <summary>Visits in one day before the run is declared stuck (an event loop, a free chain).</summary>
        private const int MaxVisitsPerDay = 30;

        public static IEnumerator Run(
            EncounterPoolData pool,
            CardDatabase cards,
            OriginType origin,
            ICampaignBot bot,
            OriginPassive[] originPassives,
            int seed,
            int locationsPerDay,
            CampaignRecord rec,
            List<BattleRecord> battleRecords
        )
        {
            var rng = new System.Random(seed);
            RunState.Clear();
            RunState.Create(origin, cards.GetStarterDeck(origin), null, isCampaignRun: true, seed: seed);
            var state = RunState.Current;
            state.ConfigureTravel(pool.Travel);

            int visitsToday = 0, dayOfVisits = state.Day;
            while (rec.EndReason == null)
            {
                if (state.Day > pool.Days)
                {
                    rec.EndReason = "Completed";
                    break;
                }
                if (state.Day != dayOfVisits)
                {
                    dayOfVisits = state.Day;
                    visitsToday = 0;
                }
                if (++visitsToday > MaxVisitsPerDay)
                {
                    rec.EndReason = "Stuck";
                    break;
                }

                // A chain is a consequence, not a choice: free, but still a visit.
                EncounterData next;
                bool charge;
                if (state.NextEncounter != null)
                {
                    next = state.NextEncounter;
                    state.ClearNextEncounter();
                    charge = false;
                }
                else
                {
                    if (state.TodaysLocationsDay != state.Day)
                        state.SetTodaysLocations(
                            state.Day,
                            pool.DrawForDay(state.Day, locationsPerDay, state.Seed, state.VisitedLocationIds, state)
                        );
                    foreach (var loc in state.TodaysLocations)
                        rec.Offered.Add(loc.name);

                    foreach (var loc in state.TodaysLocations.Where(l => !Playable(l)))
                        Note(rec, $"'{loc.name}' has no battle session — the map refuses to enter it.");
                    var enterable = state.TodaysLocations.Where(l => Playable(l) && state.PlanVisit(l).CanEnter).ToList();
                    var boss = state.TodaysLocations.FirstOrDefault(l => l is BattleEncounterData b && b.IsBoss);
                    if (boss != null && !Playable(boss))
                    {
                        // The map turns End Day into "Face the boss" while one is on offer; an
                        // unenterable boss leaves a real player with no way forward.
                        rec.EndReason = "Stuck";
                        Note(rec, $"Boss '{boss.name}' can't be fought — the run can't finish.");
                        break;
                    }
                    next = bot.ChooseLocation(state, enterable, rng);
                    // Mirrors the map: while a boss is on offer, "End Day" becomes "Face the boss".
                    if (next == null && boss != null && enterable.Contains(boss))
                        next = boss;
                    // Ending the day at HQ is free and strictly better than the bare End Day
                    // button, so every bot stops there when it can and lets its strategy pick.
                    if (next == null)
                        next = enterable.FirstOrDefault(IsDayEnder);
                    if (next == null)
                    {
                        if (enterable.Count == 0)
                            rec.DaysWithNothingToDo++;
                        rec.DaysEndedWithoutHq++;
                        state.AdvanceDay();
                        continue;
                    }
                    charge = true;
                }

                if (!Playable(next))
                {
                    Note(rec, $"Chain into '{next.name}' skipped — it has no battle session.");
                    continue;
                }
                if (charge ? !state.TryVisit(next) : !MarkFree(state, next))
                {
                    rec.Errors.Add($"Could not enter '{next.name}' on day {state.Day}.");
                    state.AdvanceDay();
                    continue;
                }
                rec.Visited.Add($"D{state.Day}:{next.name}");

                if (next is BattleEncounterData battle)
                {
                    rec.Battles++;
                    bool won = false;
                    yield return FightEncounter(battle, bot, originPassives, rng, rec, battleRecords, w => won = w);
                    if (!won)
                    {
                        rec.EndReason = "Defeated";
                        rec.DefeatedBy = battle.name;
                        break;
                    }
                    rec.BattlesWon++;
                    var offers = cards.GenerateRewardOffer(origin, 3, null, state.Rng);
                    var reward = bot.ChooseReward(offers, rng);
                    if (reward != null)
                        state.AddCardToDeck(reward);
                }
                else if (next is EventEncounterData evt)
                {
                    rec.Events++;
                    var available = new List<EventOption>();
                    foreach (var option in evt.Options)
                    {
                        if (option.IsAvailable(state))
                            available.Add(option);
                        else
                            rec.OptionsSeenLocked.Add($"{evt.name} / {option.Label}");
                    }
                    if (available.Count == 0)
                    {
                        rec.Errors.Add($"'{evt.name}' had no available option (player trapped in the panel).");
                        continue;
                    }
                    var chosen = bot.ChooseOption(state, available, rng);
                    Bump(rec.OptionsChosen, $"{evt.name} / {chosen.Label}");
                    try
                    {
                        chosen.Apply(state);
                    }
                    catch (Exception ex)
                    {
                        rec.Errors.Add($"'{evt.name} / {chosen.Label}' threw {ex.GetType().Name}: {ex.Message}");
                    }
                    if (state.PendingCardChoice != null)
                        state.ResolveCardChoice(bot.ChooseFromDeck(state.PendingCardChoice.Candidates, rng));
                }

                yield return null;
            }

            rec.DayReached = Math.Min(state.Day, pool.Days);
            rec.Funds = state.Funds;
            rec.Credibility = state.Credibility;
            rec.DeckSize = state.Deck.Count;
            rec.Allies = state.Allies.Count;
            RunState.Clear();
        }

        /// <summary>A battle the map can start: it has a session with at least one round.</summary>
        private static bool Playable(EncounterData encounter) =>
            encounter is not BattleEncounterData battle
            || (battle.Session != null && battle.Session.RoundCount > 0);

        /// <summary>Records a problem once per run, however many days it recurs.</summary>
        private static void Note(CampaignRecord rec, string message)
        {
            if (!rec.Errors.Contains(message))
                rec.Errors.Add(message);
        }

        /// <summary>An HQ-like stop: an event whose every option ends the day.</summary>
        public static bool IsDayEnder(EncounterData encounter) =>
            encounter is EventEncounterData evt
            && evt.Options.Count > 0
            && evt.Options.All(o => o.Outcomes.Any(x => x is AdvanceDayOutcome));

        private static bool MarkFree(RunState state, EncounterData encounter)
        {
            state.MarkVisited(encounter.ID);
            state.RemoveTodaysLocation(encounter);
            return true;
        }

        /// <summary>Every round of a battle encounter, as PostBattleFlow runs them.</summary>
        private static IEnumerator FightEncounter(
            BattleEncounterData battle,
            ICampaignBot bot,
            OriginPassive[] originPassives,
            System.Random rng,
            CampaignRecord run,
            List<BattleRecord> battleRecords,
            Action<bool> onDone
        )
        {
            var state = RunState.Current;
            state.StartEncounter(battle.Session.BuildBattleQueue());
            while (true)
            {
                var round = battle.Session.GetRound(state.CurrentBattleIndex);
                var setup = new BattleSetup
                {
                    playerOrigin = state.Origin,
                    originDatabase = OriginDatabase.Shared,
                    playerDeck = state.Deck,
                    enemies = state.CurrentBattleEnemies.Where(e => e != null).ToList(),
                    boss = round?.boss,
                    maxTurns = round != null && round.maxTurns > 0 ? round.maxTurns : (int?)null,
                    startingOpinion = round?.startingOpinion ?? 50,
                    maxOpinion = round != null && round.maxOpinion > 0 ? round.maxOpinion : 100,
                };
                var rec = new BattleRecord
                {
                    Context = $"campaign:{run.Id}",
                    Bot = bot.BattleBot.Name,
                    Origin = state.Origin.ToString(),
                    Encounter = battle.name,
                    Seed = rng.Next(),
                };
                battleRecords.Add(rec);
                yield return BattleHarness.Run(setup, bot.BattleBot, originPassives, rec.Seed, rec);

                if (!rec.Win)
                {
                    onDone(false);
                    yield break;
                }
                state.RecordBattleVictory();
                if (!state.HasNextBattle)
                {
                    onDone(true);
                    yield break;
                }
                state.AdvanceToNextBattle();
            }
        }

        private static void Bump(Dictionary<string, int> map, string key) =>
            map[key] = (map.TryGetValue(key, out int n) ? n : 0) + 1;
    }
}
