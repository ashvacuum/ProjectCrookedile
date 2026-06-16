using System;
using Crookedile.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Fires when a status effect is applied to the player.
    /// Optionally filters to a specific <see cref="StatusBehavior"/>.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class StatusAppliedToPlayerTrigger : PassiveTriggerBase
    {
        [Tooltip("Enable to restrict to a specific status type.")]
        [SerializeField]
        private bool _filterByStatus = false;

        [ShowIf("_filterByStatus")]
        [Tooltip("Only fire when this specific status is applied to the player.")]
        [SerializeReference]
        private StatusBehavior _filterStatus;

        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<StatusEffectAppliedEvent>())
                return false;
            var e = ctx.As<StatusEffectAppliedEvent>();
            if (!e.IsToPlayer)
                return false;
            if (_filterByStatus && _filterStatus != null && e.StatusId != _filterStatus.Id)
                return false;
            return true;
        }

        public override Type EventType => typeof(StatusEffectAppliedEvent);

        public override string TriggerLabel =>
            _filterByStatus
                ? $"When {_filterStatus?.DisplayName ?? "(none)"} is applied to you"
                : "When a status is applied to you";
    }
}
