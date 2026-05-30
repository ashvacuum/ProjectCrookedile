using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Rolls a percentage chance and, on success, executes all nested child effects.
    /// Child effects are a <c>[SerializeReference]</c> list of <see cref="BattleEffect"/> — Odin
    /// renders them recursively with the same type-picker as the parent card's effect list.
    ///
    /// This replaces the old <c>CardManipulationType.ChanceRoll</c> path and eliminates the
    /// need for the <c>[SerializeReference] List&lt;CardEffect&gt;</c> workaround.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class ChanceRollEffect : BattleEffect
    {
        [Range(1, 100)]
        [Tooltip("Percentage probability (1–100) that the nested effects will fire.")]
        [SerializeField] private int _chancePercent = 50;

        [SerializeReference]
        [Tooltip("Effects to execute when the roll succeeds.")]
        [ListDrawerSettings(ShowFoldout = true)]
        [SerializeField] private List<BattleEffect> _effects = new List<BattleEffect>();

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (!RandomHelper.Chance(_chancePercent / 100f))
            {
                GameLogger.LogInfo<ChanceRollEffect>($"Chance roll failed ({_chancePercent}%)");
                return;
            }

            GameLogger.LogInfo<ChanceRollEffect>(
                $"Chance roll succeeded ({_chancePercent}%) — resolving {_effects.Count} effect(s)");

            foreach (var child in _effects)
                child?.Execute(ctx, amountOverride);
        }

        public override string GetDescription()
        {
            if (_effects == null || _effects.Count == 0)
                return $"{_chancePercent}% chance: (no effects)";

            var parts = new string[_effects.Count];
            for (int i = 0; i < _effects.Count; i++)
                parts[i] = _effects[i]?.GetDescription() ?? "???";

            return $"{_chancePercent}% chance: {string.Join(", ", parts)}";
        }
    }
}
