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
        /// Attempts to retrieve the icon and color for a given status effect type.
        /// Returns false (with null icon and white color) when no entry is configured.
        /// </summary>
        public bool TryGet(StatusEffectType type, out Sprite icon, out Color color)
        {
            if (_lookup == null) BuildLookup();

            if (_lookup.TryGetValue(type, out Entry entry))
            {
                icon  = entry.icon;
                color = entry.color;
                return true;
            }

            icon  = null;
            color = Color.white;
            return false;
        }

        private void OnValidate() => _lookup = null;  // invalidate cache when edited in Inspector
    }
}
