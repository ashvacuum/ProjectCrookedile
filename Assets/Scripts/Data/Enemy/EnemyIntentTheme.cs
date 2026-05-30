using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data.Enemy
{
    /// <summary>
    /// One entry in the theme table — pairs a move type with its icon and badge colour.
    /// </summary>
    [Serializable]
    public struct EnemyIntentEntry
    {
        [HideLabel, EnumToggleButtons]
        public EnemyMoveType Type;

        [PreviewField(45)]
        [Tooltip("Sprite shown in the intent icon slot. Leave null to hide the icon.")]
        public Sprite Icon;

        [Tooltip("Colour applied to the intent badge background for this move type.")]
        public Color Color;
    }

    /// <summary>
    /// ScriptableObject that maps every <see cref="EnemyMoveType"/> to a display icon and badge colour.
    /// Assign one theme asset to every <c>EnemyIntentDisplay</c> in the battle scene so all intent
    /// visuals can be changed from a single asset.
    ///
    /// Create via: Assets → Create → Crookedile → Enemy → Intent Theme
    /// </summary>
    [CreateAssetMenu(fileName = "DefaultIntentTheme", menuName = "Crookedile/Enemy/Intent Theme")]
    public class EnemyIntentTheme : ScriptableObject
    {
        [Tooltip(
            "One entry per EnemyMoveType. Missing types fall back to (null icon, white badge)."
        )]
        [SerializeField]
        private EnemyIntentEntry[] _entries;

        private Dictionary<EnemyMoveType, EnemyIntentEntry> _lookup;

        private void OnEnable() => BuildLookup();

        private void BuildLookup()
        {
            _lookup = new Dictionary<EnemyMoveType, EnemyIntentEntry>();
            if (_entries == null)
                return;
            foreach (var entry in _entries)
                _lookup[entry.Type] = entry;
        }

        /// <summary>
        /// Returns the icon sprite and badge colour for the given move type.
        /// Falls back to <c>(null, Color.white)</c> if the type has no entry.
        /// </summary>
        public (Sprite icon, Color color) GetVisual(EnemyMoveType type)
        {
            if (_lookup == null)
                BuildLookup();
            return _lookup.TryGetValue(type, out var entry)
                ? (entry.Icon, entry.Color)
                : (null, Color.white);
        }
    }
}
