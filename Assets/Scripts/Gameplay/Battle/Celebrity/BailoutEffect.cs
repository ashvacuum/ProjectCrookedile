using System;
using System.Collections.Generic;
using Crookedile.Data.Cards;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    [Serializable]
    public sealed class BailoutEffect : BattleEffect
    {
        [Tooltip("Maximum Soundbites added per turn when unpaid Debt damages Opinion.")]
        [MinValue(1)]
        [SerializeField]
        private int _soundbiteCapPerTurn = 1;

        [Tooltip("Soundbite token added to hand when unpaid Debt damages Opinion.")]
        [SerializeField]
        private CardData _soundbite;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            var state = ctx.BattleManager?.Celebrity;
            if (state == null)
                return;

            int amount = amountOverride ?? _soundbiteCapPerTurn;
            state.BailoutCard = _soundbite;
            state.BailoutCapPerTurn += amount;
        }

        public override string GetDescription() =>
            $"Whenever unpaid Debt deals Opinion damage, add that many Soundbites to your hand (max {_soundbiteCapPerTurn} a turn)";

#if UNITY_EDITOR
        public override IEnumerable<string> GetConfigurationIssues()
        {
            if (_soundbite == null)
                yield return "Bailout needs a Soundbite card assigned";
        }
#endif
    }
}
