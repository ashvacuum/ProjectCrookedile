using System;
using Crookedile.Data;
using Crookedile.Data.Cards;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// For card-event triggers: passes only if the card in the triggering event matches
    /// the configured <see cref="CardType"/>.
    ///
    /// Works with <see cref="CardPlayedTrigger"/>, <see cref="CardDrawnTrigger"/>,
    /// <see cref="CardDiscardedTrigger"/>, <see cref="CardExhaustedTrigger"/>, etc.
    /// </summary>
    [Serializable]
    public class IfCardTypeCondition : PassiveConditionBase
    {
        [Tooltip("The passive fires only if the triggering card is of this type.")]
        [SerializeField] private CardType _requiredType = CardType.Pressure;

        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            // Try each card-event type
            if (ctx.EventCtx.Is<CardPlayedEvent>())
                return ctx.EventCtx.As<CardPlayedEvent>().Card?.CardType == _requiredType;
            if (ctx.EventCtx.Is<CardDrawnEvent>())
                return ctx.EventCtx.As<CardDrawnEvent>().Card?.CardType == _requiredType;
            if (ctx.EventCtx.Is<CardDiscardedEvent>())
                return ctx.EventCtx.As<CardDiscardedEvent>().Card?.CardType == _requiredType;
            if (ctx.EventCtx.Is<CardExhaustedEvent>())
                return ctx.EventCtx.As<CardExhaustedEvent>().Card?.CardType == _requiredType;
            if (ctx.EventCtx.Is<CardRetainedEvent>())
                return ctx.EventCtx.As<CardRetainedEvent>().Card?.CardType == _requiredType;
            if (ctx.EventCtx.Is<CardRecoveredEvent>())
                return ctx.EventCtx.As<CardRecoveredEvent>().Card?.CardType == _requiredType;

            return true; // non-card event — don't block
        }

        public override string ConditionLabel => $"the card is a {_requiredType} card";
    }
}
