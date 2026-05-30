using System.Collections.Generic;
using UnityEngine;

namespace Crookedile.Data.Cards
{
    /// <summary>
    /// Defines a texture atlas containing multiple card artworks.
    /// Maps card IDs to UV coordinates within the atlas.
    /// </summary>
    [CreateAssetMenu(fileName = "New Card Atlas", menuName = "Crookedile/Cards/Card Visual Atlas")]
    public class CardVisualAtlas : ScriptableObject
    {
        [Header("Atlas Settings")]
        [Tooltip("The texture atlas containing all card art")]
        [SerializeField]
        private Texture2D atlasTexture;

        [Tooltip("Number of cards horizontally in the atlas")]
        [SerializeField]
        private int columns = 6;

        [Tooltip("Number of cards vertically in the atlas")]
        [SerializeField]
        private int rows = 7;

        [Header("Card Mappings")]
        [Tooltip(
            "Maps card IDs to their atlas index (0 = top-left, incrementing left-to-right, top-to-bottom)"
        )]
        [SerializeField]
        private List<CardAtlasEntry> cardMappings = new List<CardAtlasEntry>();

        private Dictionary<string, Rect> uvCache;

        public Texture2D AtlasTexture => atlasTexture;
        public int Columns => columns;
        public int Rows => rows;

        private void OnEnable()
        {
            BuildUVCache();
        }

        /// <summary>
        /// Gets the UV rect for a specific card ID.
        /// </summary>
        /// <param name="cardId">The card's unique ID</param>
        /// <returns>UV rect, or default rect if not found</returns>
        public Rect GetUVRect(string cardId)
        {
            if (uvCache == null)
                BuildUVCache();

            if (uvCache.TryGetValue(cardId, out Rect rect))
                return rect;

            Debug.LogWarning($"Card ID '{cardId}' not found in atlas '{name}'. Using default UV.");
            return new Rect(0, 0, 1f / columns, 1f / rows);
        }

        /// <summary>
        /// Gets the UV rect for a specific atlas index.
        /// </summary>
        /// <param name="index">Atlas index (0 = top-left, incrementing left-to-right, top-to-bottom)</param>
        /// <returns>UV rect for that index</returns>
        public Rect GetUVRectByIndex(int index)
        {
            int col = index % columns;
            int row = index / columns;

            float uvWidth = 1f / columns;
            float uvHeight = 1f / rows;

            // UV coordinates start from bottom-left in Unity
            float x = col * uvWidth;
            float y = 1f - (row + 1) * uvHeight;

            return new Rect(x, y, uvWidth, uvHeight);
        }

        /// <summary>
        /// Checks if this atlas contains a specific card ID.
        /// </summary>
        public bool ContainsCard(string cardId)
        {
            if (uvCache == null)
                BuildUVCache();

            return uvCache.ContainsKey(cardId);
        }

        private void BuildUVCache()
        {
            uvCache = new Dictionary<string, Rect>();

            foreach (var entry in cardMappings)
            {
                if (!string.IsNullOrEmpty(entry.cardId))
                {
                    uvCache[entry.cardId] = GetUVRectByIndex(entry.atlasIndex);
                }
            }
        }

        /// <summary>
        /// Editor helper: Automatically assigns atlas indices based on card list order.
        /// </summary>
        [ContextMenu("Auto-Assign Indices")]
        private void AutoAssignIndices()
        {
            for (int i = 0; i < cardMappings.Count; i++)
            {
                cardMappings[i].atlasIndex = i;
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        [System.Serializable]
        public class CardAtlasEntry
        {
            [Tooltip("The card's unique ID (from CardData)")]
            public string cardId;

            [Tooltip(
                "Index in the atlas (0 = top-left, incrementing left-to-right, top-to-bottom)"
            )]
            public int atlasIndex;

            [Tooltip("Optional reference to the CardData for easy lookup")]
            public CardData cardReference;
        }
    }
}
