using System.Collections.Generic;
using Crookedile.Data.Database;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// Registry of every <see cref="AllyData"/> in the project — the ally pool the run draws from.
    /// Like the other <see cref="GameDatabase{T}"/>s it collects its items itself: an ally asset
    /// is listed as soon as it is imported (<c>DatabaseAutoRefresh</c>), never by hand. Look up by
    /// id with <see cref="GameDatabase{T}.GetByID"/>.
    ///
    /// Create via: Assets → Create → Crookedile → Database → Ally Database
    /// </summary>
    [CreateAssetMenu(menuName = "Crookedile/Database/Ally Database", fileName = "AllyDatabase")]
    public class AllyDatabase : GameDatabase<AllyData>
    {
        [Tooltip("Allies granted at random from the class's elite pool after an elite win.")]
        [MinValue(0)]
        [SerializeField]
        private int _eliteAllies = 1;

        [Tooltip("Allies offered from the class's boss pool after a boss win; the player keeps one.")]
        [MinValue(0)]
        [SerializeField]
        private int _bossAllyChoices = 3;

        public IReadOnlyList<AllyData> Allies => _items;
        public int EliteAllies => _eliteAllies;
        public int BossAllyChoices => _bossAllyChoices;

        public const string ResourcePath = "Databases/AllyDatabase";
        private static AllyDatabase _cached;

        /// <summary>The shipped roster, loaded from Resources.</summary>
        public static AllyDatabase Shared
        {
            get
            {
                // Re-resolves on null rather than caching the miss — a domain reload clears this.
                if (_cached == null)
                    _cached = Resources.Load<AllyDatabase>(ResourcePath);
                return _cached;
            }
        }

        protected override string GetItemID(AllyData item) => item.Id;

        /// <summary>
        /// Up to <paramref name="count"/> random allies from <paramref name="pools"/> that
        /// <paramref name="origin"/> can be offered (its class allies plus shared ones), minus
        /// <paramref name="exclude"/>. When the pools hold both, the first two are one costly and
        /// one pure ally, so a boss pick is a real decision. A short pool returns fewer, never a
        /// duplicate.
        /// </summary>
        public List<AllyData> Draw(
            OriginType origin,
            int count,
            ICollection<AllyData> exclude,
            System.Random rng,
            params AllyPool[] pools
        )
        {
            var pure = new List<AllyData>();
            var costly = new List<AllyData>();
            foreach (var ally in _items)
            {
                if (
                    ally == null
                    || System.Array.IndexOf(pools, ally.Pool) < 0
                    || (exclude != null && exclude.Contains(ally))
                )
                    continue;
                if (ally.Origin.HasValue && ally.Origin.Value != origin)
                    continue;
                if (!pure.Contains(ally) && !costly.Contains(ally))
                    (ally.HasCost ? costly : pure).Add(ally);
            }

            var picked = new List<AllyData>();
            if (count >= 2 && pure.Count > 0 && costly.Count > 0)
            {
                picked.Add(TakeRandom(costly, rng));
                picked.Add(TakeRandom(pure, rng));
            }
            pure.AddRange(costly);
            while (picked.Count < count && pure.Count > 0)
                picked.Add(TakeRandom(pure, rng));
            return picked;
        }

        private static AllyData TakeRandom(List<AllyData> from, System.Random rng)
        {
            int i = rng.Next(from.Count);
            var ally = from[i];
            from.RemoveAt(i);
            return ally;
        }

#if UNITY_EDITOR
        // Creates the ally beside the database and selects it; the refresh lists it.
        [Button("New Ally", ButtonSizes.Medium)]
        private void CreateAlly()
        {
            string folder = System.IO.Path.GetDirectoryName(
                UnityEditor.AssetDatabase.GetAssetPath(this)
            );
            if (string.IsNullOrEmpty(folder))
                folder = "Assets";

            var ally = CreateInstance<AllyData>();
            UnityEditor.AssetDatabase.CreateAsset(
                ally,
                UnityEditor.AssetDatabase.GenerateUniqueAssetPath($"{folder}/New Ally.asset")
            );

            RefreshDatabase();
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.Selection.activeObject = ally;
        }
#endif
    }
}
