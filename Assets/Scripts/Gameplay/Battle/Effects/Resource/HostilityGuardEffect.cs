using System;
using System.Collections.Generic;
using System.Linq;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Conditions that <see cref="HostilityGuardEffect"/> can check before executing its inner effects.
    /// All checks consider only living (non-defeated) enemies.
    /// </summary>
    public enum HostilityCondition
    {
        TargetIsHostile, // Focused target has Hostility > 0
        TargetIsReceptive, // Focused target has Hostility < 0
        TargetIsNeutral, // Focused target has Hostility == 0
        AnyEnemyHostile, // At least one living enemy is hostile
        AnyEnemyReceptive, // At least one living enemy is receptive
        AllEnemiesHostile, // Every living enemy is hostile
        AllEnemiesReceptive, // Every living enemy is receptive
        NoEnemiesHostile, // No living enemies are hostile (all neutral or receptive)
        NoEnemiesReceptive, // No living enemies are receptive (all neutral or hostile)
        MoreHostileThanReceptive, // Hostile enemies strictly outnumber receptive ones
        MoreReceptiveThanHostile, // Receptive enemies strictly outnumber hostile ones
    }

    /// <summary>
    /// Conditional wrapper effect. Evaluates a <see cref="HostilityCondition"/> against the
    /// current battle state and, only if the condition is met, executes all inner effects in order.
    ///
    /// Enables cards like "If target is hostile — deal 6 damage" or
    /// "If all enemies are receptive — draw 2 cards" without bespoke effect classes per combination.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class HostilityGuardEffect : BattleEffect
    {
        [Tooltip("The condition that must be true for the inner effects to execute.")]
        [SerializeField]
        private HostilityCondition _condition = HostilityCondition.TargetIsHostile;

        [Tooltip("Effects executed when the condition is satisfied.")]
        [SerializeReference]
        private List<BattleEffect> _effects = new List<BattleEffect>();

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (!EvaluateCondition(ctx))
            {
                GameLogger.LogInfo<HostilityGuardEffect>(
                    $"Condition [{_condition}] not met — inner effects skipped"
                );
                return;
            }

            GameLogger.LogInfo<HostilityGuardEffect>(
                $"Condition [{_condition}] met — executing {_effects.Count} inner effect(s)"
            );

            foreach (var effect in _effects)
                effect?.Execute(ctx, amountOverride);
        }

        public override string GetDescription()
        {
            string conditionText = _condition switch
            {
                HostilityCondition.TargetIsHostile => "If target is Hostile",
                HostilityCondition.TargetIsReceptive => "If target is Receptive",
                HostilityCondition.TargetIsNeutral => "If target is Neutral",
                HostilityCondition.AnyEnemyHostile => "If any enemy is Hostile",
                HostilityCondition.AnyEnemyReceptive => "If any enemy is Receptive",
                HostilityCondition.AllEnemiesHostile => "If all enemies are Hostile",
                HostilityCondition.AllEnemiesReceptive => "If all enemies are Receptive",
                HostilityCondition.NoEnemiesHostile => "If no enemies are Hostile",
                HostilityCondition.NoEnemiesReceptive => "If no enemies are Receptive",
                HostilityCondition.MoreHostileThanReceptive =>
                    "If Hostile enemies outnumber Receptive",
                HostilityCondition.MoreReceptiveThanHostile =>
                    "If Receptive enemies outnumber Hostile",
                _ => _condition.ToString(),
            };

            string innerDesc =
                _effects.Count > 0
                    ? string.Join(
                        ", ",
                        _effects.Where(e => e != null).Select(e => e.GetDescription())
                    )
                    : "nothing";

            return $"{conditionText}: {innerDesc}";
        }

        #region Condition evaluation
        private bool EvaluateCondition(EffectExecutionContext ctx)
        {
            return _condition switch
            {
                HostilityCondition.TargetIsHostile => ctx.Target?.IsHostile ?? false,
                HostilityCondition.TargetIsReceptive => ctx.Target?.IsReceptive ?? false,
                HostilityCondition.TargetIsNeutral => !(ctx.Target?.IsHostile ?? false)
                    && !(ctx.Target?.IsReceptive ?? false),
                HostilityCondition.AnyEnemyHostile => AnyLiving(ctx, e => e.Stats.IsHostile),
                HostilityCondition.AnyEnemyReceptive => AnyLiving(ctx, e => e.Stats.IsReceptive),
                HostilityCondition.AllEnemiesHostile => AllLiving(ctx, e => e.Stats.IsHostile),
                HostilityCondition.AllEnemiesReceptive => AllLiving(ctx, e => e.Stats.IsReceptive),
                HostilityCondition.NoEnemiesHostile => !AnyLiving(ctx, e => e.Stats.IsHostile),
                HostilityCondition.NoEnemiesReceptive => !AnyLiving(ctx, e => e.Stats.IsReceptive),
                HostilityCondition.MoreHostileThanReceptive => CountLiving(
                    ctx,
                    e => e.Stats.IsHostile
                ) > CountLiving(ctx, e => e.Stats.IsReceptive),
                HostilityCondition.MoreReceptiveThanHostile => CountLiving(
                    ctx,
                    e => e.Stats.IsReceptive
                ) > CountLiving(ctx, e => e.Stats.IsHostile),
                _ => false,
            };
        }

        private static bool AnyLiving(
            EffectExecutionContext ctx,
            Func<EnemyController, bool> predicate
        )
        {
            if (ctx.AllEnemies == null)
                return false;
            foreach (var enemy in ctx.AllEnemies)
                if (!enemy.IsDefeated && predicate(enemy))
                    return true;
            return false;
        }

        private static bool AllLiving(
            EffectExecutionContext ctx,
            Func<EnemyController, bool> predicate
        )
        {
            if (ctx.AllEnemies == null || ctx.AllEnemies.Count == 0)
                return false;
            foreach (var enemy in ctx.AllEnemies)
                if (!enemy.IsDefeated && !predicate(enemy))
                    return false;
            return true;
        }

        private static int CountLiving(
            EffectExecutionContext ctx,
            Func<EnemyController, bool> predicate
        )
        {
            if (ctx.AllEnemies == null)
                return 0;
            int count = 0;
            foreach (var enemy in ctx.AllEnemies)
                if (!enemy.IsDefeated && predicate(enemy))
                    count++;
            return count;
        }

        #endregion
    }
}
