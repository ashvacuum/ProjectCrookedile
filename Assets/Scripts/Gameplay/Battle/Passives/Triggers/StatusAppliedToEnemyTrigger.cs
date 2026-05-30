using System;
using Crookedile.Data;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Fires when the player applies a status effect to an enemy.
    /// Optionally filters to a specific <see cref="StatusEffectType"/>.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class StatusAppliedToEnemyTrigger : PassiveTriggerBase
    {
        [Tooltip("Enable to restrict to a specific status type.")]
        [SerializeField] private bool _filterByStatus = false;

        [ShowIf("_filterByStatus")]
        [Tooltip("Only fire when this specific status is applied.")]
        [SerializeField] private StatusEffectType _filterStatus = StatusEffectType.Weakened;

        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<StatusEffectAppliedEvent>()) return false;
            var e = ctx.As<StatusEffectAppliedEvent>();
            if (e.IsToPlayer) return false;   // must be applied to an enemy
            if (_filterByStatus && e.StatusType != _filterStatus) return false;
            return true;
        }

        public override string TriggerLabel =>
            _filterByStatus
                ? $"When you apply {_filterStatus} to an enemy"
                : "When you apply a status to an enemy";
    }
}
