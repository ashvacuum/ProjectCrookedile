using System.Collections.Generic;
using Crookedile.Data.Battle;
using Crookedile.Gameplay.Battle;
using UnityEngine;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Renders all active status effects for one combatant as a row of icon pills.
    /// Works on both the player slot and enemy slots — assign the same
    /// <see cref="StatusEffectIconMapSO"/> asset to both.
    ///
    /// Call <see cref="Refresh"/> every time the combatant's effects may have changed
    /// (typically inside <c>EnemySlotUI.Refresh()</c> and <c>PlayerSlotUI.Refresh()</c>).
    ///
    /// Setup:
    ///   1. Add this component to a child GameObject with a HorizontalLayoutGroup.
    ///   2. Assign <see cref="_iconMap"/>, <see cref="_iconPrefab"/>, and <see cref="_container"/>.
    /// </summary>
    public class StatusEffectPanelUI : MonoBehaviour
    {
        [Tooltip("ScriptableObject mapping StatusEffectType → icon sprite and tint color.")]
        [SerializeField]
        private StatusEffectIconMapSO _iconMap;

        [Tooltip("Prefab with StatusEffectIconUI component (Image + optional TMP stack count).")]
        [SerializeField]
        private GameObject _iconPrefab;

        [Tooltip("Parent transform (HorizontalLayoutGroup) that holds the icon instances.")]
        [SerializeField]
        private Transform _container;

        private readonly Dictionary<StatusEffectType, StatusEffectIconUI> _active =
            new Dictionary<StatusEffectType, StatusEffectIconUI>();

        /// <summary>
        /// Synchronises the displayed icons with the current state of <paramref name="effects"/>.
        /// — Adds icons for newly applied effects.
        /// — Refreshes stack counts on existing icons.
        /// — Destroys icons whose effect has expired (stacks == 0).
        /// No-op when <paramref name="effects"/> is null.
        /// </summary>
        public void Refresh(StatusEffectManager effects)
        {
            if (effects == null)
                return;
            if (_iconMap == null)
            {
                Debug.LogWarning(
                    $"[StatusEffectPanelUI] _iconMap is not assigned on {gameObject.name} — assign a StatusEffectIconMapSO in the Inspector.",
                    this
                );
                return;
            }

            // Track which types are still active so we can remove expired ones afterwards.
            var seen = new HashSet<StatusEffectType>();

            foreach (StatusEffectType type in System.Enum.GetValues(typeof(StatusEffectType)))
            {
                int stacks = effects.GetStacks(type);
                if (stacks <= 0)
                    continue;

                seen.Add(type);

                if (_active.TryGetValue(type, out StatusEffectIconUI existing))
                {
                    existing.Refresh(stacks);
                }
                else
                {
                    // New effect — create icon if we have a prefab and a map entry.
                    if (_iconPrefab == null || _container == null)
                        continue;
                    if (!_iconMap.TryGet(type, out var icon, out var color))
                        continue;

                    var go = Instantiate(_iconPrefab, _container);
                    var ui = go.GetComponent<StatusEffectIconUI>();
                    if (ui == null)
                        continue;

                    ui.Setup(type, icon, color, stacks);
                    _active[type] = ui;
                }
            }

            // Remove icons for effects that are no longer active.
            var toRemove = new List<StatusEffectType>();
            foreach (var kvp in _active)
                if (!seen.Contains(kvp.Key))
                    toRemove.Add(kvp.Key);

            foreach (var type in toRemove)
            {
                if (_active[type] != null)
                    Destroy(_active[type].gameObject);
                _active.Remove(type);
            }
        }

        /// <summary>Clears all displayed icons immediately (e.g. on battle end).</summary>
        public void Clear()
        {
            foreach (var kvp in _active)
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            _active.Clear();
        }
    }
}
