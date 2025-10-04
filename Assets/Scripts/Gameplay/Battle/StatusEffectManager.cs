using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
                // Stack exists - add more stacks
                existing.AddStacks(stacks);
                GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: {type} stacked +{stacks} (now {existing.Stacks} stacks)");
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

        #endregion

        #region Trigger Effects

        /// <summary>
        /// Called at the start of turn. Triggers turn-start effects and decrements stacks.
        /// </summary>
        public void OnTurnStart(BattleStats ownerStats)
        {
            List<StatusEffect> toRemove = new List<StatusEffect>();

            foreach (StatusEffect effect in _activeEffects)
            {
                // Trigger turn-start effects
                TriggerEffectWithStats(effect, StatusTriggerTiming.OnTurnStart, ownerStats);
            }

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
        /// </summary>
        public int ModifyDamageTaken(int baseDamage, BattleStats attackerStats)
        {
            float finalDamage = baseDamage;

            // Apply Vulnerable (+50% damage)
            if (HasEffect(StatusEffectType.Vulnerable))
            {
                finalDamage *= 1.5f;
            }

            // Apply Plated (reduce damage)
            finalDamage -= GetStacks(StatusEffectType.Plated);

            // Apply Intangible (only take 1 damage, then remove)
            if (HasEffect(StatusEffectType.Intangible))
            {
                finalDamage = 1;
                RemoveStacks(StatusEffectType.Intangible, 1);
            }

            // Apply Thorns (deal damage back to attacker)
            int thornsStacks = GetStacks(StatusEffectType.Thorns);
            if (thornsStacks > 0 && attackerStats != null)
            {
                attackerStats.DamageResolve(thornsStacks, 0);
                GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: Thorns dealt {thornsStacks} damage back!");
            }

            return Mathf.Max(0, Mathf.RoundToInt(finalDamage));
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

            // Apply Focus (buff, reduce cost)
            finalCost -= GetStacks(StatusEffectType.Focus);

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
                    ownerStats.DamageResolve(effect.Stacks, 0);
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
                _ => false
            };
        }

        #endregion
    }
}
