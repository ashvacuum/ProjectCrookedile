using System;
using Crookedile.Data;
using Crookedile.Data.Cards;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Installs a Celebrity tactic using the existing battle state and effect pipeline.</summary>
    [Serializable]
    public sealed class CelebrityTacticEffect : BattleEffect
    {
        [Tooltip("The tactic to install or resolve.")]
        [SerializeField]
        private CelebrityTactic _tactic;

        [Tooltip("Glamour payment, Debt relief, or Sway multiplier, depending on the tactic.")]
        [Min(1)]
        [SerializeField]
        private int _amount = 2;

        [Tooltip("Draw count, discount, promise's minimum printed cost, or Weakened stacks.")]
        [Min(1)]
        [SerializeField]
        private int _secondaryAmount = 1;

        [Tooltip("Signature Catchphrase: the Soundbite token to replace.")]
        [SerializeField]
        private CardData _soundbite;

        [Tooltip("Signature Catchphrase: the Movie Quote token to use instead.")]
        [SerializeField]
        private CardData _movieQuote;

        public override TargetType Target =>
            _tactic == CelebrityTactic.DisarmingCharm || _tactic == CelebrityTactic.CharmOffensive
                ? TargetType.Opponent
                : TargetType.Self;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (!ctx.IsPlayerCard || ctx.BattleManager == null)
                return;

            var state = ctx.BattleManager.Celebrity;
            switch (_tactic)
            {
                case CelebrityTactic.MediaTraining:
                    state.MediaTrainingCost = _amount;
                    state.MediaTrainingDraw = _secondaryAmount;
                    break;
                case CelebrityTactic.SignatureCatchphrase:
                    ctx.Deck?.InstallTokenReplacement(_soundbite, _movieQuote);
                    break;
                case CelebrityTactic.LineOfCredit:
                    state.ArmCredit(_amount, _secondaryAmount);
                    break;
                case CelebrityTactic.Overpromise:
                    state.NextSwayMultiplier = _amount;
                    break;
                case CelebrityTactic.PaidOffPromises:
                    state.MakePromise(_amount, _secondaryAmount);
                    break;
                case CelebrityTactic.DisarmingCharm:
                    if (
                        ctx.Target == null
                        || !ctx.Target.IsHostile
                        || ctx.TargetStatusEffects == null
                        || ctx.PlayerStatusEffects.GetStacks<GlamourStatus>() < _amount
                    )
                        return;

                    ctx.PlayerStatusEffects.RemoveStacksNotify<GlamourStatus>(_amount);
                    ctx.TargetStatusEffects.ApplyStatus(
                        StatusRegistry.Get<WeakenedStatus>(),
                        _secondaryAmount,
                        StatusDurationType.Permanent
                    );
                    break;
                case CelebrityTactic.CharmOffensive:
                    int x = ctx.BattleManager.CardPlay.LastEnergyPaid;
                    ApplyOpinion(ctx.Target, ctx.Caster, x * _amount, ctx);
                    ApplyGainSupport(x * _amount, ctx);
                    ctx.LastHostilityLost += ctx.Target.ReduceHostility(x);
                    break;
            }
        }

        public override string GetDescription() =>
            _tactic switch
            {
                CelebrityTactic.MediaTraining =>
                    $"At the start of your turn, you may spend {_amount} Glamour to draw {_secondaryAmount} card(s)",
                CelebrityTactic.SignatureCatchphrase =>
                    "For this battle, all your Soundbites become Movie Quotes",
                CelebrityTactic.LineOfCredit =>
                    $"The next time you take on Debt this turn, cancel up to {_amount} of that new Debt and discount a random card in hand by {_secondaryAmount} this turn",
                CelebrityTactic.Overpromise =>
                    $"The next card this turn deals {_amount} times its Sway",
                CelebrityTactic.PaidOffPromises =>
                    $"Defer up to {_amount} Debt until the end of your next turn. Play a card with printed cost {_secondaryAmount}+ next turn to cancel it (X uses energy spent)",
                CelebrityTactic.DisarmingCharm =>
                    $"Spend {_amount} Glamour to apply {_secondaryAmount} Weakened to a hostile opponent",
                _ =>
                    $"Deal {_amount}X Sway, gain {_amount}X Composure, reduce Hostility by X (X = energy spent)",
            };

#if UNITY_EDITOR
        public override System.Collections.Generic.IEnumerable<string> GetConfigurationIssues()
        {
            if (_amount <= 0 || _secondaryAmount <= 0)
                yield return "Tactic amounts must be positive.";
            if (
                _tactic == CelebrityTactic.SignatureCatchphrase
                && (_soundbite == null || _movieQuote == null)
            )
                yield return "Assign both the Soundbite and Movie Quote tokens.";
        }
#endif
    }
}
