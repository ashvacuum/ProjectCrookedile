using System;
using System.Collections.Generic;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Runs its inner effects once per living enemy carrying the filter status — the
    /// "invoke your Fanatics" iteration primitive ("For each Fanatic enemy: ...").
    ///
    /// During each iteration the context's TriggeringEnemyIndex points at the matching enemy,
    /// so inner effects authored with <see cref="Crookedile.Data.TargetType.TriggeringEnemy"/>
    /// hit THAT enemy ("give each Fanatic 2 Strength"), while other targets read the live
    /// board as usual ("per Fanatic, Silence a random hostile enemy").
    /// </summary>
    [Serializable]
    public class ForEachEnemyWithStatusEffect : BattleEffect
    {
        [Tooltip("Enemies carrying this status are iterated (1+ stacks).")]
        [SerializeReference]
        private StatusBehavior _filterStatus;

        [SerializeReference]
        [Tooltip(
            "Effects run once per matching enemy. Use target TriggeringEnemy to hit the "
                + "matching enemy itself."
        )]
        [ListDrawerSettings(ShowFoldout = true)]
        [SerializeField]
        private List<BattleEffect> _effects = new List<BattleEffect>();

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (_filterStatus == null || _effects == null || _effects.Count == 0)
            {
                GameLogger.LogWarning<ForEachEnemyWithStatusEffect>(
                    "No filter status or no effects authored — no-op"
                );
                return;
            }
            if (ctx.AllEnemies == null)
                return;

            // Snapshot matches first — inner effects may change statuses mid-iteration.
            var matches = new List<int>();
            for (int i = 0; i < ctx.AllEnemies.Count; i++)
            {
                var enemy = ctx.AllEnemies[i];
                if (!enemy.IsDefeated && enemy.StatusEffects.GetStacks(_filterStatus) > 0)
                    matches.Add(i);
            }

            if (matches.Count == 0)
                return;
            GameLogger.LogInfo<ForEachEnemyWithStatusEffect>(
                $"{matches.Count} {_filterStatus.DisplayName} enemies — iterating"
            );

            int saved = ctx.TriggeringEnemyIndex;
            foreach (int index in matches)
            {
                ctx.TriggeringEnemyIndex = index;
                foreach (var effect in _effects)
                    effect?.Execute(ctx, amountOverride);
            }
            ctx.TriggeringEnemyIndex = saved;
        }

        public override string GetDescription()
        {
            string status = _filterStatus?.DisplayName ?? "(no status)";
            var parts = new List<string>();
            if (_effects != null)
                foreach (var e in _effects)
                    if (e != null)
                        parts.Add(e.GetDescription());
            string inner = parts.Count > 0 ? string.Join(". ", parts) : "(no effects)";
            return $"For each {status} enemy: {inner}";
        }
    }
}
