using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Manages all status effects for a single combatant.
    /// Tracks buffs/debuffs, applies their effects, and handles duration/stacks.
    /// </summary>
    public class StatusEffectManager
    {
        private List<StatusEffect> _activeEffects = new List<StatusEffect>();
        private string _ownerName; // For logging

        public IReadOnlyList<StatusEffect> ActiveEffects => _activeEffects;

        public StatusEffectManager(string ownerName)
        {
            _ownerName = ownerName;
        }

        #region Apply/Remove Effects

        /// <summary>
        /// Applies a status effect. Stacks if already present, otherwise adds new.
        /// </summary>
        public void ApplyStatusEffect(StatusEffectType type, int stacks, StatusDurationType durationType = StatusDurationType.DecreasePerTurn)
        {
            StatusEffect existing = _activeEffects.FirstOrDefault(e => e.Type == type);

            if (existing != null)
            {
                // Stunned is non-stackable: a second application is ignored entirely.
                if (type == StatusEffectType.Stunned)
                {
                    GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: {type} already active — re-application ignored");
                    return;
                }

                // Stack exists — add (or subtract) stacks.
                existing.AddStacks(stacks);

                // If the effect has been neutralised (e.g. +3 Strength cancelled by -3), remove it
                // immediately so the UI doesn't show a lingering 0-stack badge.
                if (existing.Stacks == 0)
                {
                    _activeEffects.Remove(existing);
                    GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: {type} neutralised — removed");
                    return;
                }

                GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: {type} stacked {stacks:+0;-0} (now {existing.Stacks} stacks)");
            }
            else
            {
                // New effect
                StatusEffect newEffect = new StatusEffect(type, stacks, durationType);
                _activeEffects.Add(newEffect);
                string durationText = durationType switch
                {
                    StatusDurationType.Permanent => "permanent",
                    StatusDurationType.RemoveEndOfTurn => "until end of turn",
                    _ => $"{stacks} stacks"
                };
                GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: Applied {type} ({durationText})");
            }
        }

        /// <summary>
        /// Removes all stacks of a status effect.
        /// </summary>
        public void RemoveStatusEffect(StatusEffectType type)
        {
            StatusEffect effect = _activeEffects.FirstOrDefault(e => e.Type == type);
            if (effect != null)
            {
                _activeEffects.Remove(effect);
                GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: Removed {type}");
            }
        }

        /// <summary>
        /// Removes X stacks of a status effect.
        /// </summary>
        public void RemoveStacks(StatusEffectType type, int amount)
        {
            StatusEffect effect = _activeEffects.FirstOrDefault(e => e.Type == type);
            if (effect != null)
            {
                if (effect.ReduceStacks(amount))
                {
                    _activeEffects.Remove(effect);
                    GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: {type} depleted and removed");
                }
                else
                {
                    GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: {type} reduced by {amount} stacks (now {effect.Stacks})");
                }
            }
        }

        /// <summary>
        /// Clears all status effects.
        /// </summary>
        public void ClearAll()
        {
            _activeEffects.Clear();
            GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: All status effects cleared");
        }

        #endregion

        #region Query Effects

        /// <summary>
        /// Gets the total stacks of a specific status effect.
        /// </summary>
        public int GetStacks(StatusEffectType type)
        {
            StatusEffect effect = _activeEffects.FirstOrDefault(e => e.Type == type);
            return effect?.Stacks ?? 0;
        }

        /// <summary>
        /// Checks if combatant has a specific status effect.
        /// </summary>
        public bool HasEffect(StatusEffectType type)
        {
            return _activeEffects.Any(e => e.Type == type);
        }

        /// <summary>
        /// Gets all active debuffs.
        /// </summary>
        public IEnumerable<StatusEffect> GetDebuffs()
        {
            // Debuffs are negative effects
            return _activeEffects.Where(e => IsDebuff(e.Type));
        }

        /// <summary>
        /// Gets all active buffs.
        /// </summary>
        public IEnumerable<StatusEffect> GetBuffs()
        {
            return _activeEffects.Where(e => !IsDebuff(e.Type));
        }

        /// <summary>
        /// Returns true if the combatant has at least one active debuff.
        /// Used by <see cref="EnemyHasAnyDebuffCondition"/> and similar passive condition checks.
        /// </summary>
        public bool HasAnyDebuff()
        {
            return _activeEffects.Any(e => IsDebuff(e.Type));
        }

        /// <summary>
        /// Returns true if the combatant has at least one active buff.
        /// Used by <see cref="EnemyHasAnyBuffCondition"/> and similar passive condition checks.
        /// </summary>
        public bool HasAnyBuff()
        {
            return _activeEffects.Any(e => !IsDebuff(e.Type));
        }

        #endregion

        #region Trigger Effects

        /// <summary>
        /// Called at the start of turn. Triggers turn-start effects and decrements stacks.
        /// </summary>
        public void OnTurnStart(BattleStats ownerStats)
        {
            foreach (StatusEffect effect in _activeEffects)
                TriggerEffectWithStats(effect, StatusTriggerTiming.OnTurnStart, ownerStats);

            GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: Turn start status effects triggered");
        }

        /// <summary>
        /// Called at the end of turn. Triggers turn-end effects and decrements stacks.
        /// </summary>
        public void OnTurnEnd(BattleStats ownerStats)
        {
            List<StatusEffect> toRemove = new List<StatusEffect>();

            foreach (StatusEffect effect in _activeEffects)
            {
                // Trigger turn-end effects
                TriggerEffectWithStats(effect, StatusTriggerTiming.OnTurnEnd, ownerStats);

                // Handle duration types
                if (effect.DurationType == StatusDurationType.RemoveEndOfTurn)
                {
                    toRemove.Add(effect);
                    GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: {effect.Type} removed (end of turn)");
                }
                else if (effect.DurationType == StatusDurationType.DecreasePerTurn)
                {
                    // Decrement stacks
                    if (effect.DecrementStack())
                    {
                        toRemove.Add(effect);
                        GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: {effect.Type} depleted and removed");
                    }
                    else
                    {
                        GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: {effect.Type} reduced to {effect.Stacks} stacks");
                    }
                }
            }

            // Remove expired effects
            foreach (StatusEffect effect in toRemove)
            {
                _activeEffects.Remove(effect);
            }
        }


        /// <summary>
        /// Called when the player's turn begins. Removes all effects with
        /// <see cref="StatusDurationType.RemoveAtPlayerTurnStart"/> duration (e.g. Stunned).
        /// </summary>
        public void OnPlayerTurnStart()
        {
            var toRemove = _activeEffects
                .Where(e => e.DurationType == StatusDurationType.RemoveAtPlayerTurnStart)
                .ToList();

            foreach (var e in toRemove)
            {
                _activeEffects.Remove(e);
                GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: {e.Type} removed (player turn start)");
            }
        }

        /// <summary>
        /// Modifies damage dealt based on active effects.
        /// </summary>
        public int ModifyDamageDealt(int baseDamage)
        {
            int finalDamage = baseDamage;

            // Apply Strength (buff)
            finalDamage += GetStacks(StatusEffectType.Strength);

            // Apply Weakened (debuff)
            finalDamage -= GetStacks(StatusEffectType.Weakened);

            // Apply Exposed (double damage, then remove)
            if (HasEffect(StatusEffectType.Exposed))
            {
                finalDamage *= 2;
                RemoveStatusEffect(StatusEffectType.Exposed);
            }

            return Mathf.Max(0, finalDamage);
        }

        /// <summary>
        /// Modifies damage taken based on active effects.
        /// <paramref name="isAttackerPlayer"/> is forwarded to the <see cref="DamageDealtEvent"/>
        /// published when Thorns reflects the hit to the Opinion Meter.
        /// </summary>
        public int ModifyDamageTaken(int baseDamage, BattleStats attackerStats, bool isAttackerPlayer = false)
        {
            float finalDamage = baseDamage;

            // Apply Vulnerable (+50% damage to opinion meter impact)
            if (HasEffect(StatusEffectType.Vulnerable))
            {
                finalDamage *= 1.5f;
            }

            // Apply Rattled — damage shifted by attacker's Hostility per stack.
            // Hostile attacker = hits land harder; receptive attacker = hits land softer.
            // Naturally caps at 0 via the Mathf.Max at the end.
            int rattledStacks = GetStacks(StatusEffectType.Rattled);
            if (rattledStacks != 0 && attackerStats != null)
            {
                finalDamage += attackerStats.CurrentHostility * rattledStacks;
            }

            // Apply Plated (reduce damage)
            finalDamage -= GetStacks(StatusEffectType.Plated);

            // Apply Intangible (only take 1 damage, then remove)
            if (HasEffect(StatusEffectType.Intangible))
            {
                finalDamage = 1;
                RemoveStacks(StatusEffectType.Intangible, 1);
            }

            // Apply Thorns — reflects the hit as Opinion gain instead of Resolve damage.
            // isAttackerPlayer = false when an enemy attacked the player, so IsToPlayer = false
            // routes to RaiseOpinion in BattleManager (the defender looks good hitting back).
            int thornsStacks = GetStacks(StatusEffectType.Thorns);
            if (thornsStacks > 0)
            {
                GameLogger.LogInfo<StatusEffectManager>(
                    $"{_ownerName}: Thorns reflected {thornsStacks} to Opinion Meter");
                EventBus.Publish(new DamageDealtEvent
                {
                    Amount           = thornsStacks,
                    IsToPlayer       = isAttackerPlayer,
                    AttackerName     = _ownerName,
                    SourceEnemyIndex = -1,
                    TargetEnemyIndex = -1,
                });
            }

            return Mathf.Max(0, Mathf.RoundToInt(finalDamage));
        }

        /// <summary>
        /// Preview version of ModifyDamageDealt — applies Strength/Weakened/Exposed math
        /// WITHOUT consuming Exposed. Safe to call repeatedly for UI display.
        /// </summary>
        public int PreviewDamageDealt(int baseDamage)
        {
            int final = baseDamage;
            final += GetStacks(StatusEffectType.Strength);
            final -= GetStacks(StatusEffectType.Weakened);
            if (HasEffect(StatusEffectType.Exposed))
                final *= 2;                     // show doubled — Exposed will fire on the actual hit
            return Mathf.Max(0, final);
        }

        /// <summary>
        /// Preview version of ModifyDamageTaken — applies Vulnerable/Plated/Intangible math
        /// WITHOUT consuming Intangible stacks or triggering Thorns. Safe to call for UI display.
        /// </summary>
        /// <param name="attackerHostility">
        /// Pass the attacker's current Hostility when known (e.g. from intent preview) so
        /// <see cref="StatusEffectType.Rattled"/> can be factored in. Defaults to 0 (no adjustment).
        /// </param>
        public int PreviewDamageTaken(int incomingDamage, int attackerHostility = 0)
        {
            float final = incomingDamage;
            if (HasEffect(StatusEffectType.Vulnerable))
                final *= 1.5f;
            int rattled = GetStacks(StatusEffectType.Rattled);
            if (rattled != 0)
                final += attackerHostility * rattled;
            final -= GetStacks(StatusEffectType.Plated);
            if (HasEffect(StatusEffectType.Intangible))
                final = 1;                      // show 1 — Intangible stack consumed on actual hit
            return Mathf.Max(0, Mathf.RoundToInt(final));
        }

        /// <summary>
        /// Modifies Composure gained based on active effects.
        /// </summary>
        public int ModifyComposureGained(int baseComposure)
        {
            float finalComposure = baseComposure;

            // Apply Dexterity (buff)
            finalComposure += GetStacks(StatusEffectType.Dexterity);

            // Apply Frail (debuff, -25%)
            if (HasEffect(StatusEffectType.Frail))
            {
                finalComposure *= 0.75f;
            }

            return Mathf.Max(0, Mathf.RoundToInt(finalComposure));
        }

        /// <summary>
        /// Modifies card AP cost based on active effects.
        /// </summary>
        public int ModifyCardCost(int baseCost)
        {
            int finalCost = baseCost;

            // Apply Focus (buff, reduce cost by stack count)
            finalCost -= GetStacks(StatusEffectType.Focus);

            // Apply Energized (buff, reduce cost by stack count each turn)
            finalCost -= GetStacks(StatusEffectType.Energized);

            // Apply Entangled (debuff, +1 cost)
            if (HasEffect(StatusEffectType.Entangled))
            {
                finalCost += 1;
            }

            return Mathf.Max(0, finalCost);
        }

        #endregion

        #region Private Helpers

        private void TriggerEffect(StatusEffect effect, StatusTriggerTiming timing)
        {
            // Effects that don't need stats
            if (GetEffectTiming(effect.Type) != timing) return;

            switch (effect.Type)
            {
                // Most effects are handled in Modify methods above
                // This is for pure trigger-based effects
                default:
                    break;
            }
        }

        private void TriggerEffectWithStats(StatusEffect effect, StatusTriggerTiming timing, BattleStats ownerStats)
        {
            if (GetEffectTiming(effect.Type) != timing) return;

            switch (effect.Type)
            {
                case StatusEffectType.Scandal:
                    // Take damage at end of turn
                    ownerStats.DamageResolve(effect.Stacks);
                    GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: Scandal dealt {effect.Stacks} damage");
                    break;

                case StatusEffectType.Regeneration:
                    // Heal at end of turn
                    ownerStats.RestoreResolve(effect.Stacks);
                    GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: Regeneration healed {effect.Stacks} Resolve");
                    break;

                case StatusEffectType.Ritual:
                    // Gain Composure at start of turn
                    ownerStats.GainComposure(effect.Stacks);
                    GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: Ritual granted {effect.Stacks} Composure");
                    break;
            }
        }

        private StatusTriggerTiming GetEffectTiming(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Scandal => StatusTriggerTiming.OnTurnEnd,
                StatusEffectType.Regeneration => StatusTriggerTiming.OnTurnEnd,
                StatusEffectType.Ritual => StatusTriggerTiming.OnTurnStart,
                _ => StatusTriggerTiming.Passive
            };
        }

        private bool IsDebuff(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Weakened => true,
                StatusEffectType.Vulnerable => true,
                StatusEffectType.Frail => true,
                StatusEffectType.Entangled => true,
                StatusEffectType.Exposed => true,
                StatusEffectType.Scandal => true,
                StatusEffectType.Confused => true,
                StatusEffectType.Silenced => true,
                StatusEffectType.Stunned  => true,
                StatusEffectType.Rattled  => true,
                _ => false
            };
        }

        #endregion
    }
}
