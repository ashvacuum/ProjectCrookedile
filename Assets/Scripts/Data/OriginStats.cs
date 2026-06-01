using System;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// Defines per-origin battle statistics (AP and portrait).
    /// Differentiation between origins is handled by passives and starter decks.
    /// </summary>
    [CreateAssetMenu(fileName = "OriginStats", menuName = "Crookedile/Origin Stats")]
    public class OriginStats : ScriptableObject
    {
        [Header("Faith Leader (Religious)")]
        [Tooltip("Faith Leader - Support/shield specialist, +1 card draw at start")]
        public OriginBattleStats faithLeaderStats = new OriginBattleStats { maxActionPoints = 3 };

        [Header("Nepo Baby")]
        [Tooltip("Nepo Baby - Resource manipulation")]
        public OriginBattleStats nepoBabyStats = new OriginBattleStats { maxActionPoints = 3 };

        [Header("Actor (Celebrity)")]
        [Tooltip("Actor - Risk/reward specialist, first card each turn costs 1 less AP")]
        public OriginBattleStats actorStats = new OriginBattleStats { maxActionPoints = 3 };

        /// <summary>Gets the battle stats for a specific origin.</summary>
        public OriginBattleStats GetStatsForOrigin(OriginType origin)
        {
            return origin switch
            {
                OriginType.FaithLeader => faithLeaderStats,
                OriginType.NepoBaby => nepoBabyStats,
                OriginType.Actor => actorStats,
                _ => new OriginBattleStats { maxActionPoints = 3 },
            };
        }
    }

    /// <summary>Battle statistics for a specific origin.</summary>
    [Serializable]
    public class OriginBattleStats
    {
        [Tooltip("Maximum Action Points per turn")]
        public int maxActionPoints = 3;

        [Tooltip("Character portrait shown in the player slot during battle.")]
        public Sprite portrait;

        public string GetDescription() => $"AP: {maxActionPoints}";
    }
}
