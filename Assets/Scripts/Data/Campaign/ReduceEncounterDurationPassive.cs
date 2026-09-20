using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data.Campaign
{
    [Serializable]
    public class ReduceEncounterDurationPassive : OverworldPassive
    {
        [Tooltip(
            "Percent less time spent in event and battle encounters. Adds to other duration reductions, capped at 100%. Does not reduce waiting; positive encounters still take at least one minute."
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
            modifiers.EncounterTimeReductionPercent = Mathf.Min(
                100,
                modifiers.EncounterTimeReductionPercent + ReductionPercent
            );
        }

        public override string GetDescription()
        {
            return $"Encounters take {ReductionPercent}% less time (minimum 1 minute; waiting unchanged).";
        }
    }
}
