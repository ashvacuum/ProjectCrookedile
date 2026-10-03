using System;
using Crookedile.Core;
using Crookedile.Data;
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

        [Tooltip(
            "Which enemies to snap. Opponent = the focused enemy; AllHostile = every hostile "
                + "enemy, and so on. Never counts as singling an enemy out."
        )]
        [SerializeField]
        private TargetType _targets = TargetType.Opponent;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            foreach (var (stats, _) in ctx.GetTargets(_targets))
            {
                if (stats == null)
                    continue;
                int targetValue = _mood switch
                {
                    TargetMood.Hostile => stats.MaxHostility,
                    TargetMood.Receptive => stats.MinHostility,
                    TargetMood.Neutral => 0,
                    _ => 0,
                };

                stats.SetHostility(targetValue);
                GameLogger.LogInfo<SetEnemyMoodEffect>(
                    $"Set mood to {_mood} (hostility → {targetValue})"
                );
            }
        }

        public override string GetDescription()
        {
            string who = _targets switch
            {
                TargetType.Opponent => "target",
                TargetType.AllHostile => "all Hostile enemies",
                TargetType.AllReceptive => "all Receptive enemies",
                TargetType.AllOpponents => "all enemies",
                _ => _targets.ToString(),
            };
            return _mood switch
            {
                TargetMood.Hostile => $"Set {who} to fully Hostile",
                TargetMood.Receptive => $"Set {who} to fully Receptive",
                TargetMood.Neutral => $"Set {who} to Neutral",
                _ => $"Set {who} mood: {_mood}",
            };
        }
    }

    public enum TargetMood
    {
        Hostile, // Snap to MaxHostility
        Receptive, // Snap to MinHostility
        Neutral, // Set to 0
    }
}
