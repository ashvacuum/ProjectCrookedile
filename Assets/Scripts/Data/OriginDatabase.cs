using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// The unique in-battle resource an archetype runs on. Faith Leader has none (it runs on statuses).
    /// </summary>
    public enum ArchetypeResource
    {
        None, // Faith Leader — stack-to-convert, no banked resource
        Patronage, // Nepo Baby — sacrifice cards to bank Patronage
        Attention, // Celebrity (Actor) — court attention, spend as a meter hit
    }

    /// <summary>
    /// Central registry of per-archetype (<see cref="OriginType"/>) configuration that is otherwise
    /// scattered across passive assets, tag conventions and code: display name, description, color,
    /// icon, unique resource, starter-deck tag, and the origin's <see cref="OriginPassive"/>.
    /// The single source of truth for per-origin battle data (AP, portrait, passive, resource);
    /// replaced the old OriginStats asset. The Content Audit validates it.
    ///
    /// Create via: Assets → Create → Crookedile → Origin Database
    /// (or generate a seeded one: Crookedile → Generate → Origin Database).
    /// </summary>
    [CreateAssetMenu(menuName = "Crookedile/Origin Database", fileName = "OriginDatabase")]
    public class OriginDatabase : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public OriginType Type;
            public string DisplayName;

            [TextArea(2, 4)]
            public string Description;

            public Color Color;

            [Tooltip("Small UI icon (menus, labels).")]
            public Sprite Icon;

            [Tooltip("Character portrait shown in the player slot during battle.")]
            public Sprite Portrait;

            public ArchetypeResource Resource;

            [Tooltip("The origin's starter passive asset.")]
            public OriginPassive Passive;

            [Tooltip("Tag used to collect this origin's starter cards (CardDatabase.GetStarterDeck).")]
            public string StarterTag;

            [Tooltip("Max Action Points per turn.")]
            public int MaxActionPoints;
        }

        [SerializeField]
        private Entry[] _entries = Array.Empty<Entry>();

        public IReadOnlyList<Entry> Entries => _entries;

        private Dictionary<OriginType, Entry> _map;

        private void OnEnable() => BuildMap();

        private void BuildMap()
        {
            _map = new Dictionary<OriginType, Entry>();
            if (_entries == null)
                return;
            foreach (var e in _entries)
                _map[e.Type] = e;
        }

        public bool TryGet(OriginType type, out Entry entry)
        {
            if (_map == null)
                BuildMap();
            return _map.TryGetValue(type, out entry);
        }

        public ArchetypeResource GetResource(OriginType type) =>
            TryGet(type, out var e) ? e.Resource : ArchetypeResource.None;

        public OriginPassive GetPassive(OriginType type) =>
            TryGet(type, out var e) ? e.Passive : null;
    }
}
