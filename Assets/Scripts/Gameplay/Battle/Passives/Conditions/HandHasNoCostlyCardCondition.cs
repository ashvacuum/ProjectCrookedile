using System;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Passes when no card in the player's hand has at least the given printed cost.</summary>
    [Serializable]
    public class HandHasNoCostlyCardCondition : PassiveConditionBase
    {
        [Min(1)]
        [Tooltip("A hand card with at least this printed cost fails the condition.")]
        [SerializeField]
        private int _minimumPrintedCost = 2;

        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            if (ctx.Deck == null)
                return false;
            foreach (var card in ctx.Deck.Hand)
                if (card != null && card.PrintedCost >= _minimumPrintedCost)
                    return false;
            return true;
        }

        public override string ConditionLabel =>
            $"your hand has no cards costing {_minimumPrintedCost}+";
    }
}
