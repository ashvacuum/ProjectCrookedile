using System;
using Crookedile.Core;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Snaps the focused enemy's hostility to a specific mood tier, bypassing Hardened/Fanatic.
    /// Hostile = MaxHostility, Receptive = MinHostility, Neutral = 0.
    /// All normal hostility state-transition events still fire.
    /// </summary>
    [Serializable]
    public class SetEnemyMoodEffect : BattleEffect
    {
        [Tooltip("The mood to snap the target to.")]
        [SerializeField]
        private TargetMood _mood = TargetMood.Neutral;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Target == null)
                return;

            int targetValue = _mood switch
            {
                TargetMood.Hostile => ctx.Target.MaxHostility,
                TargetMood.Receptive => ctx.Target.MinHostility,
                TargetMood.Neutral => 0,
                _ => 0,
            };

            ctx.Target.SetHostility(targetValue);
            GameLogger.LogInfo<SetEnemyMoodEffect>(
                $"Set target mood to {_mood} (hostility → {targetValue})"
            );
        }

        public override string GetDescription() =>
            _mood switch
            {
                TargetMood.Hostile => "Set target to fully Hostile",
                TargetMood.Receptive => "Set target to fully Receptive",
                TargetMood.Neutral => "Neutralize target's Hostility",
                _ => $"Set target mood: {_mood}",
            };
    }

    public enum TargetMood
    {
        Hostile, // Snap to MaxHostility
        Receptive, // Snap to MinHostility
        Neutral, // Set to 0
    }
}
