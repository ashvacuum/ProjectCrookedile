using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Transitional bridge between the legacy <see cref="StatusEffectType"/> enum and the new
    /// polymorphic <see cref="StatusBehavior"/> system, so both can run side by side during migration.
    /// Mapping is by id: every behaviour's <see cref="StatusBehavior.Id"/> equals its enum name
    /// (lower-cased). The "Status parity" audit verifies the mapping is total in both directions.
    ///
    /// Delete this (and the enum) in the final migration step, once all consumers use behaviours.
    /// </summary>
    public static class StatusBridge
    {
        /// <summary>The behaviour for a legacy enum value, or null if none is defined.</summary>
        public static StatusBehavior ToBehavior(StatusEffectType type) =>
            StatusRegistry.ById(type.ToString().ToLowerInvariant());

        /// <summary>The legacy enum value for a behaviour id, if one exists.</summary>
        public static bool TryToEnum(string id, out StatusEffectType type)
        {
            type = default;
            return !string.IsNullOrEmpty(id)
                && Enum.TryParse(id, ignoreCase: true, out type)
                && Enum.IsDefined(typeof(StatusEffectType), type);
        }

        public static bool TryToEnum(StatusBehavior behavior, out StatusEffectType type) =>
            TryToEnum(behavior?.Id, out type);
    }
}
