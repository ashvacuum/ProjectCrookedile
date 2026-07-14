using System;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Fires whenever an enemy's hostility value changes, with an optional direction filter —
    /// the generic sibling of the threshold triggers (BecameHostile/BecameReceptive/etc.).
    /// Pairs with TargetType.TriggeringEnemy ("whoever was just riled gets Shame") and the
    /// LastHostilityGained/Lost amount sources (stacks equal to the shift).
    /// </summary>
    [Serializable]
    public class HostilityChangedTrigger : PassiveTriggerBase
    {
        public enum Direction
        {
            Any = 0,
            Raised = 1, // hostility went up (riled)
            Lowered = 2, // hostility went down (de-escalated)
        }

        [Tooltip("Which direction of change fires this passive.")]
        [SerializeField]
        private Direction _direction = Direction.Any;

        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<HostilityChangedEvent>())
                return false;
            var e = ctx.As<HostilityChangedEvent>();
            if (e.IsPlayer)
                return false;

            int delta = e.NewValue - e.OldValue;
            return _direction switch
            {
                Direction.Raised => delta > 0,
                Direction.Lowered => delta < 0,
                _ => delta != 0,
            };
        }

        public override Type EventType => typeof(HostilityChangedEvent);

        public override string TriggerLabel =>
            _direction switch
            {
                Direction.Raised => "When an enemy's Hostility rises",
                Direction.Lowered => "When an enemy's Hostility falls",
                _ => "When an enemy's Hostility changes",
            };
    }
}
