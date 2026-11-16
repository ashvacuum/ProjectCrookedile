using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// Registry of all <see cref="AllyData"/> in the game — the ally pool the run draws from.
    /// Look up by id. DATA SCAFFOLD: ally acquisition/persistence/registration is the future layer.
    ///
    /// Create via: Assets → Create → Crookedile → Database → Ally Database
    /// </summary>
    [CreateAssetMenu(menuName = "Crookedile/Database/Ally Database", fileName = "AllyDatabase")]
    public class AllyDatabase : ScriptableObject
    {
        // BuildIndex assigns by id, so a duplicate silently replaces the earlier entry.
        [InfoBox(
            "@DuplicateWarning()",
            InfoMessageType.Error,
            VisibleIf = "@!string.IsNullOrEmpty(DuplicateWarning())"
        )]
        [ListDrawerSettings(ListElementLabelName = "AllyName")]
        [SerializeField]
        private List<AllyData> _allies = new List<AllyData>();

        [Tooltip("Allies granted at random from the class's elite pool after an elite win.")]
        [MinValue(0)]
        [SerializeField]
        private int _eliteAllies = 1;

        [Tooltip("Allies offered from the class's boss pool after a boss win; the player keeps one.")]
        [MinValue(0)]
        [SerializeField]
        private int _bossAllyChoices = 3;

        public IReadOnlyList<AllyData> Allies => _allies;
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
            foreach (var ally in _allies)
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

        /// <summary>Allies listed twice, or blank rows — both make the index lie.</summary>
        private string DuplicateWarning()
        {
            if (_allies == null)
                return "";

            var seen = new HashSet<string>();
            var dupes = new List<string>();
            int blanks = 0;

            foreach (var ally in _allies)
            {
                if (ally == null)
                {
                    blanks++;
                    continue;
                }
                if (!seen.Add(ally.Id) && !dupes.Contains(ally.AllyName))
                    dupes.Add(ally.AllyName);
            }

            var parts = new List<string>();
            if (dupes.Count > 0)
                parts.Add($"listed twice: {string.Join(", ", dupes)} — the later row wins");
            if (blanks > 0)
                parts.Add($"{blanks} empty row(s)");
            return string.Join("; ", parts);
        }

        private Dictionary<string, AllyData> _byId;

        private void OnEnable() => BuildIndex();

        private void BuildIndex()
        {
            _byId = new Dictionary<string, AllyData>();
            if (_allies == null)
                return;
            foreach (var ally in _allies)
                if (ally != null && !string.IsNullOrEmpty(ally.Id))
                    _byId[ally.Id] = ally;
        }

        public AllyData GetById(string id)
        {
            if (_byId == null)
                BuildIndex();
            return id != null && _byId.TryGetValue(id, out var r) ? r : null;
        }

#if UNITY_EDITOR
        // Creates the ally beside the database, registers it, and selects it — the same one-click
        // authoring the encounter pool has, for the same reason.
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

            _allies.Add(ally);
            _byId = null; // rebuilt on next lookup, now that the roster changed

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.Selection.activeObject = ally;
        }
#endif
    }
}
