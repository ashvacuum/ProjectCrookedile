using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crookedile.Data.Campaign
{
    /// <summary>
    /// Card reward offers (wins and the consolation pick after a loss) show this many more cards
    /// while the ally is recruited. Several such allies stack.
    /// </summary>
    [Serializable]
    public class ExtraCardRewardChoicesPassive : OverworldPassive
    {
        [Tooltip("Extra cards shown in every card reward offer while this ally is recruited.")]
        [Min(1)]
        [SerializeField]
        private int _extraChoices = 1;

        public int ExtraChoices => Mathf.Max(0, _extraChoices);

        public override void ModifyVisit(EncounterData encounter, ref VisitModifiers modifiers) { }

        public override string GetDescription() =>
            $"Card rewards show {ExtraChoices} more choice{(ExtraChoices == 1 ? "" : "s")}.";

        /// <summary>The extra choices summed over <paramref name="allies"/>; 0 when none has this passive.</summary>
        public static int Total(IEnumerable<AllyData> allies)
        {
            int total = 0;
            if (allies == null)
                return total;
            foreach (var ally in allies)
            {
                if (ally?.OverworldPassives == null)
                    continue;
                foreach (var passive in ally.OverworldPassives)
                    if (passive is ExtraCardRewardChoicesPassive extra)
                        total += extra.ExtraChoices;
            }
            return total;
        }
    }
}
