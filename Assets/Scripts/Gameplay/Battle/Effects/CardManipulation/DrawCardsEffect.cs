using System;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Draws cards from the player's draw pile into hand.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class DrawCardsEffect : BattleEffect
    {
        [Tooltip("Base cards to draw. Ignored when Amount Source is not Fixed.")]
        [ShowIf("@_amountSource == EffectContextValue.FixedAmount")]
        [MinValue(1)]
        [SerializeField]
        private int _amount = 1;

        [Tooltip("Where to read the draw count from at runtime (e.g. ScandalsInHand).")]
        [SerializeField]
        private EffectContextValue _amountSource = EffectContextValue.FixedAmount;

        [Tooltip(
            "Optional scaling: multiply the amount by this context value "
                + "(e.g. HostileEnemyCount = 'per hostile enemy'). None = no scaling."
        )]
        [SerializeField]
        private EffectContextValue _perXSource = EffectContextValue.None;

        [Tooltip("Optional flat multiplier applied last. Values <= 0 are treated as 1.")]
        [SerializeField]
        private float _multiplier = 1f;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null)
                return; // enemies have no deck
            int amount = ResolveScaledAmount(
                ctx,
                amountOverride,
                _amount,
                _amountSource,
                _perXSource,
                _multiplier
            );
            if (amount <= 0)
                return;
            int drawn = ctx.Deck.DrawCards(amount);
            GameLogger.LogInfo<DrawCardsEffect>($"Drew {drawn} cards");
        }

        public override string GetDescription()
        {
            string amountStr = DescribeScaledAmount(_amount, _amountSource, _perXSource, _multiplier);
            return amountStr == "1" ? "Draw 1 card" : $"Draw {amountStr} cards";
        }
    }
}
