using System.Collections.Generic;
using UnityEngine;

namespace Crookedile.Data.Campaign
{
    public struct CampaignVisitPlan
    {
        public int TravelMinutes { get; private set; }
        public int EncounterMinutes { get; private set; }
        public int TravelMinutesSaved { get; private set; }
        public int EncounterMinutesSaved { get; private set; }
        public int WaitMinutes { get; private set; }
        public int ArrivalMinute { get; private set; }
        public int StartMinute { get; private set; }
        public int FinishMinute { get; private set; }
        public float TrafficMultiplier { get; private set; }
        public string BlockedReason { get; private set; }

        public bool CanEnter
        {
            get { return string.IsNullOrEmpty(BlockedReason); }
        }

        public static CampaignVisitPlan Calculate(
            EncounterData encounter,
            CampaignTravelData network,
            DistrictData from,
            int clockMinute,
            int minutesRemaining,
            IReadOnlyList<AllyData> allies = null
        )
        {
            var plan = new CampaignVisitPlan { TrafficMultiplier = 1f };
            if (encounter == null)
            {
                plan.BlockedReason = "No encounter assigned.";
                return plan;
            }

            int travel = 0;
            int clear = 0;
            if (
                encounter.District != null
                && (
                    network == null
                    || !network.TryGetTravel(
                        from,
                        encounter.District,
                        clockMinute,
                        out travel,
                        out clear
                    )
                )
            )
            {
                plan.BlockedReason = "No road from your current district.";
                return plan;
            }

            var modifiers = new OverworldPassive.VisitModifiers();
            if (allies != null)
            {
                var counted = new HashSet<AllyData>();
                foreach (var ally in allies)
                {
                    if (ally == null || !counted.Add(ally))
                    {
                        continue;
                    }

                    if (ally.OverworldPassives == null)
                    {
                        continue;
                    }

                    foreach (var passive in ally.OverworldPassives)
                    {
                        if (passive != null)
                        {
                            passive.ModifyVisit(encounter, ref modifiers);
                        }
                    }
                }
            }

            plan.TravelMinutes = ReduceMinutes(travel, modifiers.TravelTimeReductionPercent);
            plan.EncounterMinutes = ReduceMinutes(
                encounter.DurationMinutes,
                modifiers.EncounterTimeReductionPercent
            );
            plan.TravelMinutesSaved = travel - plan.TravelMinutes;
            plan.EncounterMinutesSaved = encounter.DurationMinutes - plan.EncounterMinutes;
            plan.TrafficMultiplier = clear > 0 ? (float)travel / clear : 1f;
            plan.ArrivalMinute = clockMinute + plan.TravelMinutes;
            plan.StartMinute = Mathf.Max(plan.ArrivalMinute, encounter.OpeningMinute);
            plan.WaitMinutes = plan.StartMinute - plan.ArrivalMinute;
            plan.FinishMinute = plan.StartMinute + plan.EncounterMinutes;

            if (encounter.ClosingMinute <= encounter.OpeningMinute)
            {
                plan.BlockedReason = "Invalid opening window.";
            }
            else if (plan.StartMinute >= encounter.ClosingMinute)
            {
                plan.BlockedReason = "Closed by the time you arrive.";
            }
            else if (
                plan.FinishMinute - clockMinute > minutesRemaining
                || plan.FinishMinute > CampaignTravelData.MINUTES_PER_DAY
            )
            {
                plan.BlockedReason = "Not enough time for travel, waiting, and the encounter.";
            }

            return plan;
        }

        private static int ReduceMinutes(int minutes, int reductionPercent)
        {
            return minutes == 0
                ? 0
                : Mathf.Max(
                    1,
                    (minutes * (100 - Mathf.Clamp(reductionPercent, 0, 100)) + 99) / 100
                );
        }
    }
}
