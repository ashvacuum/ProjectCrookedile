using System;
using System.Collections.Generic;
using Crookedile.Data.Cards;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Switches on a Policy card's passives for the rest of the battle, as if it had been played,
    /// without the card leaving any pile. Meant for a battle-start passive (an ally that keeps a
    /// Policy in play every battle). Only the passives go live; the Policy's on-play effects do not
    /// resolve. Playing your own copy later stacks with it, the same as playing two copies.
    /// </summary>
    [Serializable]
    public class PutPolicyInPlayEffect : BattleEffect
    {
        [Tooltip("The Policy card whose passives are in play this battle.")]
        [SerializeField]
        private CardData _policy;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (_policy != null)
                ctx.BattleManager?.Passives?.ActivateCardPassives(_policy);
        }

        public override IEnumerable<string> GetConfigurationIssues()
        {
            if (_policy == null)
                yield return "No Policy picked — nothing goes into play.";
            else if (!_policy.IsActivatedPassive)
                yield return $"'{_policy.CardName}' is not a Policy; its passives never switch on when played.";
        }

        public override string GetDescription() =>
            $"{(_policy != null ? _policy.CardName : "(no Policy)")} is in play";
    }
}
