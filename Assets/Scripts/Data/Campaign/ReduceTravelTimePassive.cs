using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data.Campaign
{
    [Serializable]
    public class ReduceTravelTimePassive : OverworldPassive
    {
        [Tooltip(
            "Percent less travel time after traffic. Adds to other travel reductions, capped at 100%. Positive trips still take at least one minute."
        )]
        [Range(0, 100)]
        [SuffixLabel("%", Overlay = true)]
        [SerializeField]
        private int _reductionPercent = 25;

        public int ReductionPercent
        {
            get { return Mathf.Clamp(_reductionPercent, 0, 100); }
        }

        public override void ModifyVisit(EncounterData encounter, ref VisitModifiers modifiers)
        {
            modifiers.TravelTimeReductionPercent = Mathf.Min(
                100,
                modifiers.TravelTimeReductionPercent + ReductionPercent
            );
        }

        public override string GetDescription()
        {
            return $"Travel takes {ReductionPercent}% less time (minimum 1 minute).";
        }
    }
}
