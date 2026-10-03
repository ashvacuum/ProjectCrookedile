using System;
using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Blow the Allowance: burn a card from hand, play it for free, then push Sway on the
    /// focused enemy equal to its printed cost. The free play counts as a play for triggers but
    /// adds no Hostility of its own. Never a Policy; a card with no valid play (unplayable junk)
    /// only when <see cref="NepoBabyConfig.SeedCanTargetJunk"/> allows it.
    /// </summary>
    [Serializable]
    public class BurnAndPlayEffect : BattleEffect
    {
        [Tooltip("Sway pushed per point of the burned card's printed cost.")]
        [MinValue(0)]
        [SerializeField]
        private float _swayPerPrintedCost = 1f;

        [Tooltip("Multiplies the Sway (2 on the Enhanced version).")]
        [MinValue(0)]
        [SerializeField]
        private float _swayMultiplier = 1f;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null)
                return;

            bool junkAllowed = NepoBabyConfig.Current.SeedCanTargetJunk;
            var candidates = new List<CardData>();
            foreach (var card in ctx.Deck.Hand)
                if (NepoBabyRules.CanBurn(card) && (junkAllowed || !card.IsUnplayable))
                    candidates.Add(card);

            ResolveCardSelection(
                candidates,
                CardSelectionMode.PlayerChoice,
                CardType.Pressure, // ignored by PlayerChoice
                "Burn a card to play it free",
                1,
                chosen =>
                {
                    if (chosen.Count == 0 || !ctx.Deck.ExhaustCard(chosen[0]))
                        return;
                    var card = chosen[0];
                    ctx.CardsBurned++;
                    int printed = card.PrintedCost;

                    ctx.BattleManager?.CardPlay?.PlayOutOfHand(card);

                    int sway = Mathf.RoundToInt(printed * _swayPerPrintedCost * _swayMultiplier);
                    if (sway > 0)
                        foreach (var (target, _) in ctx.GetTargets(TargetType.Opponent))
                            ApplyOpinion(target, ctx.Caster, sway, ctx);
                    GameLogger.LogInfo<BurnAndPlayEffect>(
                        $"Burned {card.CardName} (printed {printed}), played it free, Sway {sway}"
                    );
                }
            );
        }

        public override string GetDescription()
        {
            float perCost = _swayPerPrintedCost * _swayMultiplier;
            string sway = Mathf.Approximately(perCost, 1f)
                ? "its printed cost"
                : $"{perCost:0.##}× its printed cost";
            return $"Burn a non-Policy card from hand and play it free, then shift Opinion by {sway}";
        }
    }
}
