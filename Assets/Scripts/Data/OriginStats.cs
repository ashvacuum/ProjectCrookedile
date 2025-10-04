using System;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// Defines the battle statistics for each origin type.
    /// Different origins have different Ego/Confidence/Action Point values to match their playstyle.
    /// </summary>
    [CreateAssetMenu(fileName = "OriginStats", menuName = "Crookedile/Origin Stats")]
    public class OriginStats : ScriptableObject
    {
        [Header("Faith Leader (Religious)")]
        [Tooltip("Religious Leader - High defense, high confidence")]
        public OriginBattleStats faithLeaderStats = new OriginBattleStats
        {
            maxEgo = 80,            // Lower Ego (defensive playstyle)
            maxConfidence = 120,     // High Confidence (moral authority, strong faith)
            maxActionPoints = 3
        };

        [Header("Nepo Baby")]
        [Tooltip("Nepo Baby - Balanced but privileged, extra action points")]
        public OriginBattleStats nepoBabyStats = new OriginBattleStats
        {
            maxEgo = 100,           // Average Ego
            maxConfidence = 100,     // Average Confidence
            maxActionPoints = 4      // Extra action point (money buys options)
        };

        [Header("Actor (Celebrity)")]
        [Tooltip("Actor/Celebrity - Glass cannon, high damage but fragile")]
        public OriginBattleStats actorStats = new OriginBattleStats
        {
            maxEgo = 120,           // High Ego (charismatic, high energy)
            maxConfidence = 80,      // Lower Confidence (fragile to criticism, scandal-prone)
            maxActionPoints = 3
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
                _ => new OriginBattleStats { maxEgo = 100, maxConfidence = 100, maxActionPoints = 3 }
            };
        }
    }

    /// <summary>
    /// Battle statistics for a specific origin.
    /// </summary>
    [Serializable]
    public class OriginBattleStats
    {
        [Tooltip("Maximum Ego (true HP)")]
        public int maxEgo = 100;

        [Tooltip("Maximum Confidence (shield/multiplier)")]
        public int maxConfidence = 100;

        [Tooltip("Maximum Action Points per turn")]
        public int maxActionPoints = 3;

        /// <summary>
        /// Gets a summary description of these stats.
        /// </summary>
        public string GetDescription()
        {
            return $"Ego: {maxEgo} | Confidence: {maxConfidence} | AP: {maxActionPoints}";
        }
    }
}
