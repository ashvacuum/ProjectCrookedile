using System;
using System.Collections.Generic;
using Crookedile.Data.Audio;
using Crookedile.Data.VFX;
using Crookedile.Gameplay.Battle;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// How a status is grouped for presentation and rules-of-thumb. Purely descriptive.
    /// </summary>
    public enum StatusCategory
    {
        Debuff, // hurts the bearer
        Buff, // helps the bearer
        Pacify, // Faith Leader: counts toward conversion (Guilt/Shame/Doubt)
        Threshold, // Faith Leader: Jaded (raises conversion cost)
        HostilityFlag, // Hardened/Fanatic/Turncoat/Devotion
        Special, // everything else
    }

    /// <summary>
    /// Central, authorable registry of presentation + metadata for every <see cref="StatusEffectType"/>.
    /// One entry per status: name, description, icon, color, category, and the SFX/VFX that play when
    /// it is applied. This is the single place a designer fills in so a status is fully presentable;
    /// the Content Audit validates that every enum value has a complete entry.
    ///
    /// Create via: Assets → Create → Crookedile → Status Effect Database
    /// (or generate a pre-seeded one: Crookedile → Generate → Status Effect Database).
    /// </summary>
    [CreateAssetMenu(menuName = "Crookedile/Status Effect Database", fileName = "StatusEffectDatabase")]
    public class StatusEffectDatabase : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public StatusEffectType Type;
            public string DisplayName;

            [TextArea(2, 4)]
            public string Description;

            public StatusCategory Category;
            public Sprite Icon;
            public Color Color;

            [Tooltip("Sound played when this status is applied. Optional.")]
            public AudioEvent ApplySfx;

            [Tooltip("Visual played when this status is applied. Optional.")]
            public VFXEvent ApplyVfx;
        }

        [SerializeField]
        private Entry[] _entries = Array.Empty<Entry>();

        public IReadOnlyList<Entry> Entries => _entries;

        private Dictionary<StatusEffectType, Entry> _map;

        private void OnEnable() => BuildMap();

        private void BuildMap()
        {
            _map = new Dictionary<StatusEffectType, Entry>();
            if (_entries == null)
                return;
            foreach (var e in _entries)
                _map[e.Type] = e; // last wins on duplicates
        }

        /// <summary>True (and populates <paramref name="entry"/>) if the status has an entry.</summary>
        public bool TryGet(StatusEffectType type, out Entry entry)
        {
            if (_map == null)
                BuildMap();
            return _map.TryGetValue(type, out entry);
        }

        public string GetDescription(StatusEffectType type) =>
            TryGet(type, out var e) ? e.Description : string.Empty;

        public Sprite GetIcon(StatusEffectType type) => TryGet(type, out var e) ? e.Icon : null;

        public Color GetColor(StatusEffectType type, Color fallback) =>
            TryGet(type, out var e) && e.Color.a > 0f ? e.Color : fallback;
    }
}
