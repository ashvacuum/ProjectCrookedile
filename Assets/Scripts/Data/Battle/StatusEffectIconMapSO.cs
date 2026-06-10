using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crookedile.Data.Battle
{
    /// <summary>
    /// ScriptableObject that maps each status id (<see cref="Crookedile.Gameplay.Battle.StatusBehavior.Id"/>,
    /// e.g. "guilt") to a display icon and tint color.
    /// One asset is shared by all <see cref="Crookedile.UI.Battle.StatusEffectPanelUI"/> instances.
    ///
    /// Create via: Right-click → Crookedile / Battle / Status Effect Icon Map
    /// </summary>
    [CreateAssetMenu(
        fileName = "StatusEffectIconMap",
        menuName = "Crookedile/Battle/Status Effect Icon Map"
    )]
    public class StatusEffectIconMapSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("Stable status id — must match a StatusBehavior.Id (lowercase, e.g. \"guilt\").")]
            public string id;

            [Tooltip("Icon sprite shown in the buff/debuff pill.")]
            public Sprite icon;

            [Tooltip("Tint color applied to the icon image.")]
            public Color color;

            [Tooltip("Short display name shown in the tooltip header (e.g. \"Guilt\").")]
            public string effectName;

            [TextArea(1, 3)]
            [Tooltip("One-sentence description of what the effect does.")]
            public string description;
        }

        [SerializeField]
        private List<Entry> _entries = new List<Entry>();

        private Dictionary<string, Entry> _lookup;

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, Entry>(
                _entries.Count,
                StringComparer.OrdinalIgnoreCase
            );
            foreach (var entry in _entries)
                if (!string.IsNullOrEmpty(entry.id))
                    _lookup[entry.id] = entry;
        }

        /// <summary>
        /// Attempts to retrieve the icon, color, name, and description for a given status id.
        /// Returns false (with null icon, white color, and empty strings) when no entry is configured.
        /// </summary>
        public bool TryGet(
            string id,
            out Sprite icon,
            out Color color,
            out string effectName,
            out string description
        )
        {
            if (_lookup == null)
                BuildLookup();

            if (id != null && _lookup.TryGetValue(id, out Entry entry))
            {
                icon = entry.icon;
                color = entry.color;
                effectName = entry.effectName;
                description = entry.description;
                return true;
            }

            icon = null;
            color = Color.white;
            effectName = string.Empty;
            description = string.Empty;
            return false;
        }

        /// <summary>
        /// 2-out-param overload for callers that only need the visual pair.
        /// </summary>
        public bool TryGet(string id, out Sprite icon, out Color color) =>
            TryGet(id, out icon, out color, out _, out _);

        private void OnValidate() => _lookup = null; // invalidate cache when edited in Inspector
    }
}
