using System.Collections.Generic;
using System.Linq;
using Crookedile.Data.Database;
using UnityEngine;

namespace Crookedile.Data.Enemy
{
    /// <summary>
    /// ScriptableObject database containing all enemy data for the game.
    /// Mirrors <see cref="Crookedile.Data.Cards.CardDatabase"/>: auto-populates from every
    /// EnemyData asset via "Refresh Database" in the inspector, keyed by <see cref="EnemyData.ID"/>.
    ///
    /// Create via: Right-click → Crookedile / Database / Enemy Database
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyDatabase", menuName = "Crookedile/Database/Enemy Database")]
    public class EnemyDatabase : GameDatabase<EnemyData>
    {
        protected override string GetItemID(EnemyData item) => item.ID;

        /// <summary>Gets an enemy by its display name (first match; names aren't guaranteed unique — use GetByID for that).</summary>
        public EnemyData GetByName(string enemyName) => Find(e => e.EnemyName == enemyName);

        /// <summary>Gets all enemies that have at least one reactive passive authored.</summary>
        public List<EnemyData> GetWithPassives() =>
            FindAll(enemy => enemy.Passives != null && enemy.Passives.Count > 0);

        /// <summary>Gets all enemies with at least one SummonMinion move (bosses/summoners).</summary>
        public List<EnemyData> GetSummoners() =>
            FindAll(enemy => enemy.Moves.Any(m => m != null && m.MinionToSummon != null));
    }
}
