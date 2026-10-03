using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Passes when the player's hand holds no Heckles or Scandals.</summary>
    [Serializable]
    public class HandHasNoJunkCondition : PassiveConditionBase
    {
        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            if (ctx.Deck == null)
                return false;
            foreach (var card in ctx.Deck.Hand)
                if (card != null && card.IsJunk)
                    return false;
            return true;
        }

        public override string ConditionLabel => "your hand has no Heckles or Scandals";
    }
}
