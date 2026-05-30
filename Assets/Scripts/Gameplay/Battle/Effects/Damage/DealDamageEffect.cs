using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Deals a fixed amount of Resolve damage to one or more targets.
    /// The amount can optionally be sourced from the runtime <see cref="EffectExecutionContext"/>
    /// (e.g. equal to the last damage dealt — lifesteal-style chaining).
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class DealDamageEffect : BattleEffect
    {
        [Tooltip("Who receives the damage.")]
        [SerializeField] private TargetType _target = TargetType.Opponent;

        public override TargetType Target => _target;

        [Tooltip("Base damage amount. Ignored when Amount Source is not Fixed.")]
        [ShowIf("@_amountSource == EffectContextValue.FixedAmount")]
        [MinValue(1)]
        [SerializeField] private int _amount = 5;

        [Tooltip("Where to read the damage amount from at runtime.\n" +
                 "FixedAmount = use the authored Amount field.\n" +
                 "Other options read accumulated values from the effect context (e.g. LastDamageDealt for lifesteal).")]
        [SerializeField] private EffectContextValue _amountSource = EffectContextValue.FixedAmount;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int baseDamage = amountOverride
                ?? (_amountSource == EffectContextValue.FixedAmount ? _amount : ctx.GetValue(_amountSource));

            foreach (var (target, _) in ctx.GetTargets(_target))
                ApplyResolveDamage(target, ctx.Caster, baseDamage, ctx);
        }

        public override string GetDescription()
        {
            string amountStr = _amountSource == EffectContextValue.FixedAmount
                ? _amount.ToString()
                : _amountSource.ToString();
            return $"Deal {amountStr} damage to {_target}";
        }
    }
}
