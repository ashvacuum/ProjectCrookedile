using System;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Fires the moment a Faith Leader conversion resolves — pacify stacks consumed, burst fired.
    ///
    /// The alternative is a <see cref="StatusAppliedToEnemyTrigger"/> filtered to Fanatic or
    /// Jaded, which works only because <c>ConvertPacifiedEffect</c> happens to award those
    /// statuses; retune the award list and every passive hanging off it goes quiet. This reads
    /// the conversion itself.
    /// </summary>
    [Serializable]
    public class EnemyConvertedTrigger : PassiveTriggerBase
    {
        // ponytail: one bool, because nothing wants silenced-only yet. If a "punish the
        // non-believer" passive ever does, this becomes a three-value enum.
        [Tooltip(
            "Skip Hardened targets, which spend the same fuel but are silenced instead of "
                + "converted — no burst, no Jaded, no convert to reward."
        )]
        [SerializeField]
        private bool _convertedOnly = true;

        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<EnemyConvertedEvent>())
                return false;
            return !_convertedOnly || !ctx.As<EnemyConvertedEvent>().WasSilenced;
        }

        public override Type EventType => typeof(EnemyConvertedEvent);

        public override string TriggerLabel =>
            _convertedOnly ? "When you convert an enemy" : "When you convert or silence an enemy";
    }
}
