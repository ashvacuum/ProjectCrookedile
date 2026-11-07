using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data.Campaign
{
    /// <summary>
    /// Raises <see cref="HealPercentOfMaxCredibilityOutcome"/> to this ally's percentage of max
    /// Credibility when it is higher. Several such allies don't stack: the largest percentage wins.
    /// </summary>
    [Serializable]
    public class StrongerCredibilityHealPassive : OverworldPassive
    {
        [Tooltip(
            "Percent of max Credibility that Credibility heals restore while this ally is recruited. Used only when higher than the heal's own percent; the largest among allies wins."
        )]
        [Range(0f, 100f)]
        [SuffixLabel("%", Overlay = true)]
        [SerializeField]
        private float _percentOfMax = 20f;

        public float PercentOfMax => Mathf.Clamp(_percentOfMax, 0f, 100f);

        public override void ModifyVisit(EncounterData encounter, ref VisitModifiers modifiers) { }

        public override string GetDescription() =>
            $"Credibility heals restore {PercentOfMax:0.##}% of max Credibility.";

        /// <summary>The largest percent of max among <paramref name="allies"/>, or 0 when none has this passive.</summary>
        public static float BestPercent(IEnumerable<AllyData> allies)
        {
            float best = 0f;
            if (allies == null)
                return best;
            foreach (var ally in allies)
            {
                if (ally?.OverworldPassives == null)
                    continue;
                foreach (var passive in ally.OverworldPassives)
                    if (passive is StrongerCredibilityHealPassive heal)
                        best = Mathf.Max(best, heal.PercentOfMax);
            }
            return best;
        }
    }
}
