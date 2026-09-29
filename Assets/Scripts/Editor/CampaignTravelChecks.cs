using System;
using System.Collections.Generic;
using System.Reflection;
using Crookedile.Data;
using Crookedile.Data.Campaign;
using Crookedile.Utilities;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    public static class CampaignTravelChecks
    {
        [MenuItem("Crookedile/Campaign/Run Travel Checks")]
        public static void Run()
        {
            var hq = ScriptableObject.CreateInstance<DistrictData>();
            var market = ScriptableObject.CreateInstance<DistrictData>();
            var park = ScriptableObject.CreateInstance<DistrictData>();
            var island = ScriptableObject.CreateInstance<DistrictData>();
            var network = ScriptableObject.CreateInstance<CampaignTravelData>();
            var encounter = ScriptableObject.CreateInstance<EventEncounterData>();
            try
            {
                var rush = new CampaignTravelData.TrafficWindow();
                Set(rush, "_startMinute", 480);
                Set(rush, "_endMinute", 540);
                Set(rush, "_multiplier", 3f);
                var direct = Road(hq, market, 10);
                Set(direct, "_traffic", new List<CampaignTravelData.TrafficWindow> { rush });
                Set(network, "_headquarters", hq);
                Set(
                    network,
                    "_roads",
                    new List<CampaignTravelData.Road>
                    {
                        direct,
                        Road(hq, park, 8),
                        Road(park, market, 8),
                    }
                );

                Require(
                    network.TryGetTravel(hq, market, 480, out int minutes, out int clear)
                        && minutes == 16
                        && clear == 16,
                    "Rush hour should select the clear alternate route."
                );
                Require(
                    network.TryGetTravel(hq, market, 540, out minutes, out clear) && minutes == 10,
                    "Traffic ends at the exclusive boundary."
                );
                var route = new List<CampaignTravelData.Road>();
                Require(
                    network.TryGetTravel(hq, market, 480, out minutes, out clear, route)
                        && route.Count == 2
                        && route[0].From == hq
                        && route[0].To == park
                        && route[1].To == market,
                    "The diagram must receive the ordered quickest route during rush hour."
                );
                Require(
                    network.TryGetTravel(market, hq, 540, out minutes, out clear, route)
                        && route.Count == 1
                        && route[0] == direct,
                    "The diagram must switch to the direct road after rush hour, including reverse travel."
                );
                Require(
                    !network.TryGetTravel(hq, island, 480, out minutes, out clear, route)
                        && route.Count == 0,
                    "An unreachable destination must clear the previous highlighted route."
                );
                route.Add(direct);
                Require(
                    network.TryGetTravel(hq, hq, 480, out minutes, out clear, route)
                        && route.Count == 0,
                    "A local trip must clear the highlighted route."
                );
                Require(
                    network.TryGetTravel(market, hq, 540, out minutes, out clear) && minutes == 10,
                    "Bidirectional roads must work in reverse."
                );
                Require(
                    network.TryGetTravel(hq, hq, 480, out minutes, out clear) && minutes == 0,
                    "Same district has zero travel cost."
                );
                Require(
                    !network.TryGetTravel(hq, island, 480, out minutes, out clear),
                    "Disconnected districts must be unreachable."
                );
                Set(network, "_roads", new List<CampaignTravelData.Road> { direct });
                Require(
                    network.TryGetTravel(hq, market, 480, out minutes, out clear)
                        && minutes == 30
                        && clear == 10,
                    "Traffic must multiply road duration."
                );
                Set(direct, "_bidirectional", false);
                Require(
                    !network.TryGetTravel(market, hq, 540, out minutes, out clear),
                    "One-way roads must not allow reverse travel."
                );
                Set(direct, "_bidirectional", true);

                Set(encounter, "_id", "travel-check");
                Set(encounter, "_district", market);
                Set(encounter, "_hourCost", 0);
                Set(encounter, "_extraMinutes", 30);
                Set(encounter, "_openingMinute", 540);
                Set(encounter, "_closingMinute", 600);
                var plan = CampaignVisitPlan.Calculate(encounter, network, hq, 480, 180);
                Require(
                    plan.CanEnter
                        && plan.TravelMinutes == 30
                        && plan.WaitMinutes == 30
                        && plan.ArrivalMinute == 510
                        && plan.FinishMinute == 570,
                    "Early arrival must include waiting."
                );
                Require(
                    !CampaignVisitPlan.Calculate(encounter, network, hq, 480, 89).CanEnter,
                    "Insufficient combined travel, wait, and event budget must be blocked."
                );
                Require(
                    !CampaignVisitPlan.Calculate(encounter, network, hq, 590, 180).CanEnter,
                    "Arrival exactly at closing must be blocked."
                );
                Require(
                    CampaignVisitPlan.Calculate(encounter, network, hq, 589, 180).CanEnter,
                    "Entry before closing can finish after closing."
                );
                Require(
                    !CampaignVisitPlan.Calculate(encounter, null, hq, 480, 180).CanEnter,
                    "Assigned districts need a network."
                );

                var state = (RunState)Activator.CreateInstance(typeof(RunState), true);
                Property(state, "MaxHours", 3);
                state.ConfigureTravel(network);
                state.SetTodaysLocations(1, new List<EncounterData> { encounter });
                Property(state, "VisitedLocationIds", new HashSet<string>());
                Require(
                    !state.TrySpendMinutes(-1) && state.ClockMinute == 480,
                    "Negative time spending must not create time."
                );
                Require(!state.TrySpendMinutes(181), "Overspending must not mutate the clock.");
                Require(
                    state.TryVisit(encounter)
                        && state.ClockMinute == 570
                        && state.MinutesRemaining == 90
                        && state.CurrentDistrict == market,
                    "Committing must charge exactly the preview and remember the destination."
                );
                Require(
                    state.IsVisited(encounter.ID) && state.TodaysLocations.Count == 0,
                    "A committed encounter is consumed once."
                );
                Require(
                    !state.TryVisit(encounter) && state.ClockMinute == 570,
                    "Double entry must not spend time twice."
                );
                state.ConfigureTravel(null);
                Require(
                    state.CurrentDistrict == market
                        && state.ClockMinute == 570
                        && state.Travel == network,
                    "Scene re-entry must not reset travel or the clock."
                );
                state.AdvanceDay();
                Require(
                    state.Day == 2
                        && state.ClockMinute == 480
                        && state.MinutesRemaining == 180
                        && state.CurrentDistrict == hq,
                    "A new day resets time and returns to HQ."
                );
                state.SetTodaysLocations(2, new List<EncounterData> { encounter });
                Require(
                    state.TrySpendMinutes(111)
                        && !state.TryVisit(encounter)
                        && state.CurrentDistrict == hq
                        && state.TodaysLocations.Count == 1
                        && state.MinutesRemaining == 69,
                    "A blocked visit must not consume the encounter or change location."
                );
                Require(
                    state.TrySpendMinutes(1)
                        && state.MinutesRemaining == 68
                        && state.ClockMinute == 592,
                    "Minute spending must preserve fractional hours exactly."
                );

                Set(network, "_dayStartMinute", 360);
                var earlyState = (RunState)Activator.CreateInstance(typeof(RunState), true);
                Property(earlyState, "MaxHours", 18);
                earlyState.ConfigureTravel(network);
                Require(
                    earlyState.ClockMinute == 360 && earlyState.MinutesRemaining == 1080,
                    "Earlier starts must receive the full budget up to midnight."
                );

                Set(encounter, "_district", null);
                Set(encounter, "_openingMinute", 0);
                Set(encounter, "_closingMinute", 0);
                plan = CampaignVisitPlan.Calculate(encounter, null, null, 480, 180);
                Require(
                    plan.CanEnter && plan.TravelMinutes == 0 && plan.FinishMinute == 510,
                    "Legacy encounters remain local and available all day."
                );
                Require(
                    !CampaignVisitPlan.Calculate(encounter, null, null, 1430, 180).CanEnter,
                    "Visits must not cross midnight."
                );
                Set(encounter, "_openingMinute", 700);
                Set(encounter, "_closingMinute", 600);
                Require(
                    !CampaignVisitPlan.Calculate(encounter, null, null, 480, 180).CanEnter,
                    "Reversed time windows must be blocked."
                );
                CheckAllyBonuses(network, hq, market, encounter);
                GameLogger.LogInfo("Campaign", "Campaign travel and ally bonus checks passed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(encounter);
                UnityEngine.Object.DestroyImmediate(network);
                UnityEngine.Object.DestroyImmediate(hq);
                UnityEngine.Object.DestroyImmediate(market);
                UnityEngine.Object.DestroyImmediate(park);
                UnityEngine.Object.DestroyImmediate(island);
            }
        }

        private static void CheckAllyBonuses(
            CampaignTravelData network,
            DistrictData hq,
            DistrictData market,
            EventEncounterData encounter
        )
        {
            var driver = ScriptableObject.CreateInstance<AllyData>();
            var organizer = ScriptableObject.CreateInstance<AllyData>();
            var restored = ScriptableObject.CreateInstance<AllyData>();
            try
            {
                var travelPassive = new ReduceTravelTimePassive();
                var durationPassive = new ReduceEncounterDurationPassive();
                Set(travelPassive, "_reductionPercent", 25);
                Set(durationPassive, "_reductionPercent", 50);
                Set(
                    driver,
                    "_overworldPassives",
                    new List<OverworldPassive> { travelPassive, null, durationPassive }
                );
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(driver), restored);
                Require(
                    restored.OverworldPassives.Count == 3
                        && restored.OverworldPassives[0] is ReduceTravelTimePassive savedTravel
                        && savedTravel.ReductionPercent == 25
                        && restored.OverworldPassives[1] == null
                        && restored.OverworldPassives[2]
                            is ReduceEncounterDurationPassive savedDuration
                        && savedDuration.ReductionPercent == 50,
                    "Ally serialization must preserve concrete overworld passive types and authored values."
                );
                Set(encounter, "_district", market);
                Set(encounter, "_openingMinute", 0);
                Set(encounter, "_closingMinute", 0);
                Set(network, "_dayStartMinute", 480);
                var allies = new List<AllyData> { driver };
                var plan = CampaignVisitPlan.Calculate(encounter, network, hq, 480, 180, allies);
                Require(
                    plan.TravelMinutes == 23
                        && plan.EncounterMinutes == 15
                        && plan.FinishMinute == 518,
                    "Ally reductions apply after traffic and round duration up to whole minutes."
                );
                Require(
                    plan.TravelMinutesSaved == 7
                        && plan.EncounterMinutesSaved == 15
                        && plan.TrafficMultiplier == 3f,
                    "Preview savings must match adjusted costs without disguising heavy traffic."
                );
                Require(
                    driver.HasOverworldPassives
                        && driver.AutoDescription.Contains("25%")
                        && driver.AutoDescription.Contains("50%"),
                    "Campaign-only allies must describe their bonuses."
                );
                allies.Add(driver);
                allies.Add(null);
                Require(
                    CampaignVisitPlan
                        .Calculate(encounter, network, hq, 480, 180, allies)
                        .FinishMinute == 518,
                    "Duplicate and empty ally references must not multiply bonuses."
                );

                Set(encounter, "_openingMinute", 540);
                plan = CampaignVisitPlan.Calculate(encounter, network, hq, 480, 180, allies);
                Require(
                    plan.WaitMinutes == 37 && plan.StartMinute == 540 && plan.FinishMinute == 555,
                    "Faster travel must not discount waiting or bypass opening time."
                );
                Set(encounter, "_openingMinute", 0);
                Set(encounter, "_closingMinute", 505);
                Require(
                    plan.CanEnter
                        && CampaignVisitPlan
                            .Calculate(encounter, network, hq, 480, 180, allies)
                            .CanEnter
                        && !CampaignVisitPlan.Calculate(encounter, network, hq, 480, 180).CanEnter,
                    "Faster travel must allow arrival before a previously missed closing time."
                );
                Set(encounter, "_closingMinute", 0);

                var state = (RunState)Activator.CreateInstance(typeof(RunState), true);
                Property(state, "MaxHours", 3);
                Property(state, "Allies", new List<AllyData>());
                Property(state, "VisitedLocationIds", new HashSet<string>());
                state.ConfigureTravel(network);
                state.SetTodaysLocations(1, new List<EncounterData> { encounter });
                state.AddAlly(driver);
                state.AddAlly(driver);
                Require(
                    state.Allies.Count == 1
                        && state.TryVisit(encounter)
                        && state.ClockMinute == 518
                        && state.MinutesRemaining == 142,
                    "Recruited allies must affect the actual visit charge once, exactly as previewed."
                );

                var extraTravel = new ReduceTravelTimePassive();
                var extraDuration = new ReduceEncounterDurationPassive();
                Set(extraTravel, "_reductionPercent", 100);
                Set(extraDuration, "_reductionPercent", 100);
                Set(
                    organizer,
                    "_overworldPassives",
                    new List<OverworldPassive> { extraTravel, extraDuration }
                );
                allies.Add(organizer);
                plan = CampaignVisitPlan.Calculate(encounter, network, hq, 480, 180, allies);
                Require(
                    plan.TravelMinutes == 1 && plan.EncounterMinutes == 1,
                    "Stacked bonuses cap at 100 percent and positive durations retain a one-minute floor."
                );
                Set(encounter, "_district", null);
                Set(encounter, "_extraMinutes", 0);
                plan = CampaignVisitPlan.Calculate(encounter, null, null, 480, 180, allies);
                Require(
                    plan.TravelMinutes == 0
                        && plan.EncounterMinutes == 0
                        && plan.FinishMinute == 480,
                    "Already-free local encounters must remain free with bonuses."
                );
                Set(travelPassive, "_reductionPercent", -25);
                Set(durationPassive, "_reductionPercent", 150);
                Require(
                    travelPassive.ReductionPercent == 0 && durationPassive.ReductionPercent == 100,
                    "Runtime must clamp malformed authored percentages."
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(driver);
                UnityEngine.Object.DestroyImmediate(organizer);
                UnityEngine.Object.DestroyImmediate(restored);
            }
        }

        private static CampaignTravelData.Road Road(DistrictData from, DistrictData to, int minutes)
        {
            var road = new CampaignTravelData.Road();
            Set(road, "_from", from);
            Set(road, "_to", to);
            Set(road, "_baseMinutes", minutes);
            return road;
        }

        private static void Set(object target, string field, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var info = type.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
                if (info != null)
                {
                    info.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new InvalidOperationException($"Missing field {field}.");
        }

        private static void Property(object target, string name, object value)
        {
            target.GetType().GetProperty(name).SetValue(target, value);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
