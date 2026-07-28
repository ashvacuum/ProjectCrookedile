using System.Collections.Generic;
using Crookedile.Data.Database;
using UnityEngine;

namespace Crookedile.Data.Campaign
{
    /// <summary>
    /// ScriptableObject database of every encounter asset. Mirrors
    /// <see cref="Crookedile.Data.Enemy.EnemyDatabase"/>: auto-populates from every
    /// <see cref="EncounterData"/> asset via "Refresh Database", keyed by
    /// <see cref="EncounterData.ID"/>.
    ///
    /// Covers all subtypes — <c>t:EncounterData</c> matches derived assets — so battles,
    /// events, and anything added later land in one lookup rather than a database per type.
    ///
    /// Create via: Right-click → Crookedile / Database / Encounter Database
    /// </summary>
    [CreateAssetMenu(
        fileName = "EncounterDatabase",
        menuName = "Crookedile/Database/Encounter Database"
    )]
    public class EncounterDatabase : GameDatabase<EncounterData>
    {
        protected override string GetItemID(EncounterData item) => item.ID;

        /// <summary>Every encounter of a given concrete type (battles, events, ...).</summary>
        public List<T> GetOfType<T>()
            where T : EncounterData
        {
            var result = new List<T>();
            foreach (var item in _items)
                if (item is T typed)
                    result.Add(typed);
            return result;
        }

        /// <summary>
        /// Encounters with no drop weight, which can never be drawn from a pool unless an
        /// entry overrides them. Usually an authoring slip rather than intent.
        /// </summary>
        public List<EncounterData> GetUndrawable() => FindAll(e => e != null && e.DropWeight <= 0f);
    }
}
