using System;
using System.Collections.Generic;
using System.Linq;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Auto-discovered registry of all concrete <see cref="StatusBehavior"/> types. Behaviors are
    /// stateless (per-application stacks/duration live on the status instance), so one canonical
    /// instance per type is shared. Looked up by <see cref="StatusBehavior.Id"/> or by concrete type.
    ///
    /// Replaces the StatusEffectType enum as the source of truth for "what statuses exist".
    /// </summary>
    public static class StatusRegistry
    {
        private static Dictionary<string, StatusBehavior> _byId;
        private static Dictionary<Type, StatusBehavior> _byType;

        private static void EnsureBuilt()
        {
            if (_byId != null)
                return;

            _byId = new Dictionary<string, StatusBehavior>(StringComparer.OrdinalIgnoreCase);
            _byType = new Dictionary<Type, StatusBehavior>();

            foreach (Type t in typeof(StatusBehavior).Assembly.GetTypes())
            {
                if (t.IsAbstract || !typeof(StatusBehavior).IsAssignableFrom(t))
                    continue;
                if (t.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                var instance = (StatusBehavior)Activator.CreateInstance(t);
                _byType[t] = instance;

                if (string.IsNullOrEmpty(instance.Id))
                {
                    GameLogger.LogWarning("StatusRegistry", $"{t.Name} has an empty Id — skipped.");
                    continue;
                }
                if (_byId.ContainsKey(instance.Id))
                {
                    GameLogger.LogWarning(
                        "StatusRegistry",
                        $"Duplicate status Id '{instance.Id}' ({t.Name} vs {_byId[instance.Id].GetType().Name})."
                    );
                    continue;
                }
                _byId[instance.Id] = instance;
            }
        }

        /// <summary>Canonical behavior for an id, or null.</summary>
        public static StatusBehavior ById(string id)
        {
            EnsureBuilt();
            return id != null && _byId.TryGetValue(id, out var b) ? b : null;
        }

        /// <summary>Canonical behavior for a concrete type, or null.</summary>
        public static StatusBehavior ByType(Type type)
        {
            EnsureBuilt();
            return type != null && _byType.TryGetValue(type, out var b) ? b : null;
        }

        public static T Get<T>()
            where T : StatusBehavior => ByType(typeof(T)) as T;

        /// <summary>All canonical behaviors (one per concrete type).</summary>
        public static IReadOnlyCollection<StatusBehavior> All
        {
            get
            {
                EnsureBuilt();
                return _byType.Values;
            }
        }

        public static IEnumerable<string> AllIds
        {
            get
            {
                EnsureBuilt();
                return _byId.Keys;
            }
        }
    }
}
