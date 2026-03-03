using System;
using System.Collections.Generic;
using UnityEngine;
using Crookedile.Gameplay.Battle;

namespace Crookedile.Data.Battle
{
    /// <summary>
    /// ScriptableObject that maps each <see cref="StatusEffectType"/> to a display icon and tint color.
    /// One asset is shared by all <see cref="Crookedile.UI.Battle.StatusEffectPanelUI"/> instances.
    ///
    /// Create via: Right-click → Crookedile / Battle / Status Effect Icon Map
    /// </summary>
    [CreateAssetMenu(fileName = "StatusEffectIconMap", menuName = "Crookedile/Battle/Status Effect Icon Map")]
    public class StatusEffectIconMapSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public StatusEffectType type;
            [Tooltip("Icon sprite shown in the buff/debuff pill.")]
            public Sprite           icon;
            [Tooltip("Tint color applied to the icon image.")]
            public Color            color;
            [Tooltip("Short display name shown in the tooltip header (e.g. \"Poison\").")]
            public string           effectName;
            [TextArea(1, 3)]
            [Tooltip("One-sentence description of what the effect does.")]
            public string           description;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        private Dictionary<StatusEffectType, Entry> _lookup;

        private void BuildLookup()
        {
            _lookup = new Dictionary<StatusEffectType, Entry>(_entries.Count);
            foreach (var entry in _entries)
                _lookup[entry.type] = entry;
        }

        /// <summary>
        /// Attempts to retrieve the icon, color, name, and description for a given status effect type.
        /// Returns false (with null icon, white color, and empty strings) when no entry is configured.
        /// </summary>
        public bool TryGet(StatusEffectType type, out Sprite icon, out Color color,
                           out string effectName, out string description)
        {
            if (_lookup == null) BuildLookup();

            if (_lookup.TryGetValue(type, out Entry entry))
            {
                icon        = entry.icon;
                color       = entry.color;
                effectName  = entry.effectName;
                description = entry.description;
                return true;
            }

            icon        = null;
            color       = Color.white;
            effectName  = string.Empty;
            description = string.Empty;
            return false;
        }

        /// <summary>
        /// Backwards-compatible 2-out-param overload. Existing callers (StatusEffectPanelUI) are unaffected.
        /// </summary>
        public bool TryGet(StatusEffectType type, out Sprite icon, out Color color)
            => TryGet(type, out icon, out color, out _, out _);

        private void OnValidate() => _lookup = null;  // invalidate cache when edited in Inspector
    }
}
