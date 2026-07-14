using System;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Enemy;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Replaces the declared intent of the targeted enemies with an authored
    /// <see cref="EnemyMoveData"/> — the player commanding the crowd ("Send them Forth":
    /// your Fanatics abandon their attack and act for you on their turn).
    ///
    /// The replacement executes on the enemy's turn through the normal pipeline, so author
    /// its effects knowing the enemy is the caster (RaiseOpinion works in your favor; the
    /// enemy's own skip checks — Stunned/Silenced/Doubt — still apply). The intent badge
    /// updates immediately. Combine with <see cref="ForEachEnemyWithStatusEffect"/> and
    /// target TriggeringEnemy to command every Fanatic.
    /// </summary>
    [Serializable]
    public class OverrideIntentEffect : BattleEffect
    {
        [Tooltip("Whose intent to replace.")]
        [SerializeField]
        private TargetType _target = TargetType.Opponent;

        public override TargetType Target => _target;

        [Tooltip("The move the enemy will perform instead (enemy is the caster).")]
        [SerializeField]
        private EnemyMoveData _replacementMove;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (_replacementMove == null)
            {
                GameLogger.LogWarning<OverrideIntentEffect>("No replacement move authored — no-op");
                return;
            }
            if (ctx.AllEnemies == null)
                return;

            foreach (var (targetStats, _) in ctx.GetTargets(_target))
            {
                if (targetStats == null || targetStats == ctx.PlayerStats)
                    continue;

                int index = targetStats.OwnerEnemyIndex;
                if (index < 0 || index >= ctx.AllEnemies.Count)
                    continue;
                var enemy = ctx.AllEnemies[index];
                if (enemy.IsDefeated)
                    continue;

                enemy.OverrideIntent(_replacementMove);
                EventBus.Publish(
                    new EnemyIntentDeclaredEvent { Move = _replacementMove, EnemyIndex = index }
                );
                GameLogger.LogInfo<OverrideIntentEffect>(
                    $"Enemy [{index}] intent overridden → {_replacementMove.MoveName}"
                );
            }
        }

        public override string GetDescription()
        {
            string move = _replacementMove != null ? _replacementMove.MoveName : "(no move)";
            string targetStr = _target == TargetType.Opponent ? "the target" : $"{_target}";
            return $"Command {targetStr}: they {move} this turn instead";
        }
    }
}
