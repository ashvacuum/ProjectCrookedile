using System.Collections.Generic;
using System.Linq;
using Crookedile.Data.Database;

namespace Crookedile.Data.Audio
{
    /// <summary>
    /// ScriptableObject database of named audio clips.
    /// Supports lookup by GUID (ID) and by human-readable name (ClipName).
    ///
    /// Setup:
    ///   1. Create via Assets → Create → Crookedile → Database → Sound Library
    ///   2. Create AudioClipData assets (Assets → Create → Crookedile → Audio → Audio Clip Data).
    ///   3. Click "Refresh Database" to auto-populate from all AudioClipData assets in the project.
    ///   4. Assign this asset to AudioManager in the Inspector.
    ///
    /// Usage (AnimationEvent — accepts either ID or ClipName):
    ///   Function: PlaySound   String: "crack_hit"
    /// </summary>
    [UnityEngine.CreateAssetMenu(
        fileName = "SoundLibrary",
        menuName = "Crookedile/Database/Sound Library"
    )]
    public class SoundLibrary : GameDatabase<AudioClipData>
    {
        private Dictionary<string, AudioClipData> _nameMap;

        protected override string GetItemID(AudioClipData item) => item.ID;

        #region Initialisation
        protected override void OnEnable()
        {
            base.OnEnable();
            BuildNameMap();
        }

#if UNITY_EDITOR
        public override void RefreshDatabase()
        {
            base.RefreshDatabase();
            BuildNameMap();
        }
#endif

        private void BuildNameMap()
        {
            _nameMap = new Dictionary<string, AudioClipData>(
                System.StringComparer.OrdinalIgnoreCase
            );
            foreach (var item in _items)
            {
                if (item != null && !string.IsNullOrEmpty(item.ClipName))
                    _nameMap[item.ClipName] = item;
            }
        }

        #endregion

        #region ID Lookup
        // GetByID(string id) inherited from GameDatabase<T>

        #endregion

        #region Name Lookup
        /// <summary>
        /// Returns the clip whose ClipName matches (case-insensitive).
        /// Returns null if not found.
        /// </summary>
        public AudioClipData GetByName(string clipName)
        {
            if (_nameMap == null)
                BuildNameMap();
            return _nameMap.TryGetValue(clipName, out var result) ? result : null;
        }

        /// <summary>
        /// Tries ID first, then falls back to ClipName.
        /// Use this when the string could be either format.
        /// </summary>
        public AudioClipData GetByIDOrName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;
            return GetByID(value) ?? GetByName(value);
        }

        #endregion

        #region Simple Queries
        /// <summary>Returns all clips belonging to the given category (case-insensitive).</summary>
        public List<AudioClipData> GetByCategory(string category) =>
            FindAll(c =>
                string.Equals(c.Category, category, System.StringComparison.OrdinalIgnoreCase)
            );

        /// <summary>Returns all distinct category strings present in the library.</summary>
        public List<string> GetAllCategories() =>
            GetAll()
                .Select(c => c.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

        #endregion

        #region Advanced Search
        /// <summary>
        /// Performs a filtered search. All non-empty criteria are AND-ed together.
        /// </summary>
        public List<AudioClipData> Search(SoundSearchQuery query)
        {
            var results = GetAll();

            if (!string.IsNullOrEmpty(query.Category))
                results = results
                    .Where(c =>
                        string.Equals(
                            c.Category,
                            query.Category,
                            System.StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .ToList();

            if (!string.IsNullOrEmpty(query.IdContains))
                results = results
                    .Where(c =>
                        c.ID != null
                        && c.ID.IndexOf(query.IdContains, System.StringComparison.OrdinalIgnoreCase)
                            >= 0
                    )
                    .ToList();

            if (!string.IsNullOrEmpty(query.NameContains))
                results = results
                    .Where(c =>
                        c.ClipName != null
                        && c.ClipName.IndexOf(
                            query.NameContains,
                            System.StringComparison.OrdinalIgnoreCase
                        ) >= 0
                    )
                    .ToList();

            return results;
        }
    }

    /// <summary>Query object for SoundLibrary.Search. Empty fields are ignored.</summary>
    [System.Serializable]
    public class SoundSearchQuery
    {
        /// <summary>Filter to a specific category (exact match, case-insensitive). Empty = all categories.</summary>
        public string Category;

        /// <summary>Filter to IDs containing this substring (case-insensitive). Empty = all.</summary>
        public string IdContains;

        /// <summary>Filter to ClipNames containing this substring (case-insensitive). Empty = all.</summary>
        public string NameContains;
    }
}
        #endregion
