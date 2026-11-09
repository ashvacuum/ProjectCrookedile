using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// For the rest of the battle, every effect that generates a token (Soundbite, Movie Quote)
    /// creates extra copies of it. Junk (Scandals, Heckles) is never copied. Stacks if applied again.
    /// </summary>
    [Serializable]
    public sealed class ExtraTokenCopiesEffect : BattleEffect
    {
        [Tooltip("Extra copies each token-generating effect creates for the rest of the battle.")]
        [MinValue(1)]
        [SerializeField]
        private int _extraCopies = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null) =>
            ctx.Deck?.AddExtraTokenCopies(_extraCopies);

        public override string GetDescription() =>
            _extraCopies == 1
                ? "Whenever you create a Soundbite or Movie Quote, create one more"
                : $"Whenever you create a Soundbite or Movie Quote, create {_extraCopies} more";

#if UNITY_EDITOR
        public override IEnumerable<string> GetConfigurationIssues()
        {
            if (_extraCopies <= 0)
                yield return "Effect amounts must be positive.";
        }
#endif
    }
}
