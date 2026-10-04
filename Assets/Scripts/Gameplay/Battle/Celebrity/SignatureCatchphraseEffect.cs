using System;
using System.Collections.Generic;
using Crookedile.Data.Cards;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    [Serializable]
    public sealed class SignatureCatchphraseEffect : BattleEffect
    {
        [Tooltip("The Soundbite token to replace.")]
        [SerializeField]
        private CardData _soundbite;

        [Tooltip("The Movie Quote token to use instead.")]
        [SerializeField]
        private CardData _movieQuote;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (!ctx.IsPlayerCard || ctx.BattleManager == null)
                return;

            ctx.Deck?.InstallTokenReplacement(_soundbite, _movieQuote);
        }

        public override string GetDescription() =>
            "For this battle, all your Soundbites become Movie Quotes";

#if UNITY_EDITOR
        public override IEnumerable<string> GetConfigurationIssues()
        {
            if (_soundbite == null || _movieQuote == null)
                yield return "Assign both the Soundbite and Movie Quote tokens.";
        }
#endif
    }
}
