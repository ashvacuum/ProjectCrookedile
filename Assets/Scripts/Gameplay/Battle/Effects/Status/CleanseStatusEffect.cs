using System;
using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Removes status effects from the chosen targets — the Protector-role tool
    /// (e.g. a Fixer cleansing the player's pacify stacks off its neighbours), but
    /// equally authorable on player cards (cleanse your own debuffs).
    ///
    /// Modes: a specific status, all debuffs, all buffs, or everything.
    /// </summary>
    [Serializable]
    public class CleanseStatusEffect : BattleEffect
    {
        public enum CleanseMode
        {
            SpecificStatus, // Remove all stacks of the behavior chosen below
            AllDebuffs, // Remove every status whose IsDebuff is true
            AllBuffs, // Remove every status whose IsDebuff is false
            Everything, // Remove all statuses
        }

        [Tooltip("Whose statuses are removed.")]
        [SerializeField]
        private TargetType _target = TargetType.Self;

        public override TargetType Target => _target;

        [Tooltip("Which statuses to remove.")]
        [SerializeField]
        private CleanseMode _mode = CleanseMode.AllDebuffs;

        [Tooltip("The status to remove (SpecificStatus mode only).")]
        [SerializeReference]
        private StatusBehavior _behavior;

        [Tooltip("The most statuses removed per target (a random pick among matches). 0 = all.")]
        [Sirenix.OdinInspector.MinValue(0)]
        [SerializeField]
        private int _maxStatuses = 0;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (_mode == CleanseMode.SpecificStatus && _behavior == null)
            {
                GameLogger.LogWarning<CleanseStatusEffect>(
                    "SpecificStatus mode with no behavior assigned — no-op"
                );
                return;
            }

            foreach (var (targetStats, statusMgr) in ctx.GetTargets(_target))
            {
                if (statusMgr == null)
                    continue;

                // Snapshot first — RemoveStatus mutates the active list.
                var toRemove = new List<StatusEffect>();
                foreach (var effect in statusMgr.ActiveEffects)
                {
                    bool matches = _mode switch
                    {
                        CleanseMode.SpecificStatus => effect.Behavior.Id == _behavior.Id,
                        CleanseMode.AllDebuffs => effect.Behavior.IsDebuff,
                        CleanseMode.AllBuffs => !effect.Behavior.IsDebuff,
                        CleanseMode.Everything => true,
                        _ => false,
                    };
                    if (matches)
                        toRemove.Add(effect);
                }
                while (_maxStatuses > 0 && toRemove.Count > _maxStatuses)
                    toRemove.RemoveAt(RandomHelper.Range(0, toRemove.Count));

                foreach (var effect in toRemove)
                    statusMgr.RemoveStatusNotify(effect.Behavior);

                if (toRemove.Count > 0)
                    GameLogger.LogInfo<CleanseStatusEffect>(
                        $"Cleansed {toRemove.Count} status(es) from "
                            + $"{(targetStats == ctx.PlayerStats ? "Player" : statusMgr.OwnerName)}"
                    );
            }
        }

        public override string GetDescription()
        {
            string who = _target == TargetType.Opponent ? "the target" : _target.ToString();
            if (_maxStatuses == 1 && _mode == CleanseMode.AllBuffs)
                return $"Remove a buff from {who}";
            if (_maxStatuses == 1 && _mode == CleanseMode.AllDebuffs)
                return $"Remove a debuff from {who}";
            return _mode switch
            {
                CleanseMode.SpecificStatus =>
                    $"Remove {_behavior?.DisplayName ?? "(none)"} from {_target}",
                CleanseMode.AllDebuffs => $"Remove all debuffs from {_target}",
                CleanseMode.AllBuffs => $"Remove all buffs from {_target}",
                _ => $"Remove all statuses from {_target}",
            };
        }
    }
}
