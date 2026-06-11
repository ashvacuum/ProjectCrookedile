using System;
using System.Linq;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Applies pressure scaled by the number of hostile (or receptive) enemies.
    /// SUPERSEDED: prefer <see cref="ApplyPressureEffect"/> with Per X Source =
    /// HostileEnemyCount / ReceptiveEnemyCount — same result, more options.
    /// Kept because existing card assets (Condemnation) reference it.
    /// </summary>
    [Serializable]
    public class ApplyPressureBasedOnHostility : BattleEffect
    {
        [Tooltip("Which crowd to count: AllHostile or AllReceptive.")]
        [SerializeField]
        private TargetType _targetType = TargetType.AllHostile;

        [Tooltip("Pressure dealt per matching enemy.")]
        [MinValue(1)]
        [SerializeField]
        private int _damage = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (_targetType != TargetType.AllHostile && _targetType != TargetType.AllReceptive)
            {
                GameLogger.LogWarning<ApplyPressureBasedOnHostility>(
                    "Target type must be AllHostile or AllReceptive — no-op"
                );
                return;
            }

            int perEnemy = amountOverride ?? _damage;
            int count = ctx.GetTargets(_targetType).Count();
            if (count <= 0 || perEnemy <= 0)
                return;

            ApplyPressure(ctx.Target, ctx.Caster, count * perEnemy, ctx);
        }

#if UNITY_EDITOR
        public override System.Collections.Generic.IEnumerable<string> GetConfigurationIssues()
        {
            if (_targetType != TargetType.AllHostile && _targetType != TargetType.AllReceptive)
                yield return "Target type must be AllHostile or AllReceptive";
        }
#endif

        public override string GetDescription() =>
            $"Apply {_damage} pressure per "
            + (
                _targetType switch
                {
                    TargetType.AllHostile => "hostile",
                    TargetType.AllReceptive => "receptive",
                    _ => "(invalid target)",
                }
            )
            + " enemy";
    }
}
