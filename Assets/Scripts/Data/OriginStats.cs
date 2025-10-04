using System;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// Defines the battle statistics for each origin type.
    /// All origins have same Resolve (HP), differentiated by passives and starter decks.
    /// </summary>
    [CreateAssetMenu(fileName = "OriginStats", menuName = "Crookedile/Origin Stats")]
    public class OriginStats : ScriptableObject
    {
        [Header("Faith Leader (Religious)")]
        [Tooltip("Faith Leader - Composure combo specialist, +1 card draw at start")]
        public OriginBattleStats faithLeaderStats = new OriginBattleStats
        {
            maxResolve = 20,        // Standard Resolve (HP)
            maxActionPoints = 3     // Standard AP
        };

        [Header("Nepo Baby")]
        [Tooltip("Nepo Baby - Resource manipulation, starts with 4 AP instead of 3")]
        public OriginBattleStats nepoBabyStats = new OriginBattleStats
        {
            maxResolve = 20,        // Standard Resolve (HP)
            maxActionPoints = 4     // Extra action point (Family Connections passive)
        };

        [Header("Actor (Celebrity)")]
        [Tooltip("Actor - Risk/reward specialist, first card each turn costs 1 less AP")]
        public OriginBattleStats actorStats = new OriginBattleStats
        {
            maxResolve = 20,        // Standard Resolve (HP)
            maxActionPoints = 3     // Standard AP
        };

        /// <summary>
        /// Gets the battle stats for a specific origin.
        /// </summary>
        public OriginBattleStats GetStatsForOrigin(OriginType origin)
        {
            return origin switch
            {
                OriginType.FaithLeader => faithLeaderStats,
                OriginType.NepoBaby => nepoBabyStats,
                OriginType.Actor => actorStats,
                _ => new OriginBattleStats { maxResolve = 20, maxActionPoints = 3 }
            };
        }
    }

    /// <summary>
    /// Battle statistics for a specific origin.
    /// </summary>
    [Serializable]
    public class OriginBattleStats
    {
        [Tooltip("Maximum Resolve (HP in negotiations)")]
        public int maxResolve = 20;

        [Tooltip("Maximum Action Points per turn")]
        public int maxActionPoints = 3;

        /// <summary>
        /// Gets a summary description of these stats.
        /// </summary>
        public string GetDescription()
        {
            return $"Resolve: {maxResolve} | AP: {maxActionPoints}";
        }
    }
}
