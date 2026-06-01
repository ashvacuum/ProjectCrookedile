using System.Collections.Generic;
using System.Linq;
using Crookedile.Core;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Manages all status effects for a single combatant.
    /// Tracks buffs/debuffs, applies their effects, and handles duration/stacks.
    /// </summary>
    public class StatusEffectManager
    {
        private List<StatusEffect> _activeEffects = new List<StatusEffect>();

        // Type-indexed mirror of _activeEffects for O(1) lookups (GetStacks/HasEffect/etc.).
        // Always kept in sync via AddEffectInternal/RemoveEffectInternal — never mutate
        // _activeEffects directly.
        private readonly Dictionary<StatusEffectType, StatusEffect> _byType =
            new Dictionary<StatusEffectType, StatusEffect>();

        private string _ownerName; // For logging
        private BattleStats _owner; // Optional — used to sync Hardened/Fanatic flags

        public IReadOnlyList<StatusEffect> ActiveEffects => _activeEffects;

        public StatusEffectManager(string ownerName, BattleStats owner = null)
        {
            _ownerName = ownerName;
            _owner = owner;
        }

        #region Apply/Remove Effects

        /// <summary>
        /// Applies a status effect. Stacks if already present, otherwise adds new.
        /// </summary>
        public void ApplyStatusEffect(
            StatusEffectType type,
            int stacks,
            StatusDurationType durationType = StatusDurationType.DecreasePerTurn
        )
        {
            _byType.TryGetValue(type, out StatusEffect existing);

            if (existing != null)
            {
                // Stunned is non-stackable: a second application is ignored entirely.
                if (type == StatusEffectType.Stunned)
                {
                    GameLogger.LogInfo<StatusEffectManager>(
                        $"{_ownerName}: {type} already active — re-application ignored"
                    );
                    return;
                }

                // Stack exists — add (or subtract) stacks.
                existing.AddStacks(stacks);

                // If the effect has been neutralised (e.g. +3 Strength cancelled by -3), remove it
                // immediately so the UI doesn't show a lingering 0-stack badge.
                if (existing.Stacks == 0)
                {
                    RemoveEffectInternal(existing);
                    GameLogger.LogInfo<StatusEffectManager>(
                        $"{_ownerName}: {type} neutralised — removed"
                    );
                    return;
                }

                GameLogger.LogInfo<StatusEffectManager>(
                    $"{_ownerName}: {type} stacked {stacks:+0;-0} (now {existing.Stacks} stacks)"
                );
            }
            else
            {
                // New effect
                StatusEffect newEffect = new StatusEffect(type, stacks, durationType);
                AddEffectInternal(newEffect);
                string durationText = durationType switch
                {
                    StatusDurationType.Permanent => "permanent",
                    StatusDurationType.RemoveEndOfTurn => "until end of turn",
                    _ => $"{stacks} stacks",
                };
                GameLogger.LogInfo<StatusEffectManager>(
                    $"{_ownerName}: Applied {type} ({durationText})"
                );
            }

            SyncHostilityFlags();
        }

        private void SyncHostilityFlags()
        {
            if (_owner == null)
                return;
            _owner.SetHardened(HasEffect(StatusEffectType.Hardened));
            _owner.SetFanatic(HasEffect(StatusEffectType.Fanatic));
        }

        /// <summary>
        /// Removes all stacks of a status effect.
        /// </summary>
        public void RemoveStatusEffect(StatusEffectType type)
        {
            if (_byType.TryGetValue(type, out StatusEffect effect))
            {
                RemoveEffectInternal(effect);
                GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: Removed {type}");
                SyncHostilityFlags();
            }
        }

        /// <summary>
        /// Removes X stacks of a status effect.
        /// </summary>
        public void RemoveStacks(StatusEffectType type, int amount)
        {
            if (_byType.TryGetValue(type, out StatusEffect effect))
            {
                if (effect.ReduceStacks(amount))
                {
                    RemoveEffectInternal(effect);
                    GameLogger.LogInfo<StatusEffectManager>(
                        $"{_ownerName}: {type} depleted and removed"
                    );
                }
                else
                {
                    GameLogger.LogInfo<StatusEffectManager>(
                        $"{_ownerName}: {type} reduced by {amount} stacks (now {effect.Stacks})"
                    );
                }
            }
        }

        /// <summary>
        /// Clears all status effects.
        /// </summary>
        public void ClearAll()
        {
            _activeEffects.Clear();
            _byType.Clear();
            GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: All status effects cleared");
        }

        #endregion

        #region Query Effects

        /// <summary>
        /// Gets the total stacks of a specific status effect.
        /// </summary>
        public int GetStacks(StatusEffectType type)
        {
            return _byType.TryGetValue(type, out StatusEffect effect) ? effect.Stacks : 0;
        }

        /// <summary>
        /// Checks if combatant has a specific status effect.
        /// </summary>
        public bool HasEffect(StatusEffectType type)
        {
            return _byType.ContainsKey(type);
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

            GameLogger.LogInfo<StatusEffectManager>(
                $"{_ownerName}: Turn start status effects triggered"
            );
        }

        /// <summary>
        /// Called at the end of turn. Triggers turn-end effects and decrements stacks.
        /// </summary>
        public void OnTurnEnd(BattleStats ownerStats)
        {
            // Single in-place pass preserving forward trigger order. When an effect is removed we
            // don't advance the index (the next element slides into the current slot), so removal
            // is O(1) per element and there's no separate toRemove list.
            int i = 0;
            while (i < _activeEffects.Count)
            {
                StatusEffect effect = _activeEffects[i];

                // Trigger turn-end effects
                TriggerEffectWithStats(effect, StatusTriggerTiming.OnTurnEnd, ownerStats);

                // Handle duration types
                if (effect.DurationType == StatusDurationType.RemoveEndOfTurn)
                {
                    RemoveEffectAt(i);
                    GameLogger.LogInfo<StatusEffectManager>(
                        $"{_ownerName}: {effect.Type} removed (end of turn)"
                    );
                    continue; // index now points at the next (shifted) element
                }

                if (effect.DurationType == StatusDurationType.DecreasePerTurn)
                {
                    // Decrement stacks
                    if (effect.DecrementStack())
                    {
                        RemoveEffectAt(i);
                        GameLogger.LogInfo<StatusEffectManager>(
                            $"{_ownerName}: {effect.Type} depleted and removed"
                        );
                        continue;
                    }

                    GameLogger.LogInfo<StatusEffectManager>(
                        $"{_ownerName}: {effect.Type} reduced to {effect.Stacks} stacks"
                    );
                }

                i++;
            }
        }

        /// <summary>
        /// Called when the player's turn begins. Removes all effects with
        /// <see cref="StatusDurationType.RemoveAtPlayerTurnStart"/> duration (e.g. Stunned).
        /// </summary>
        public void OnPlayerTurnStart()
        {
            int i = 0;
            while (i < _activeEffects.Count)
            {
                StatusEffect e = _activeEffects[i];
                if (e.DurationType == StatusDurationType.RemoveAtPlayerTurnStart)
                {
                    RemoveEffectAt(i);
                    GameLogger.LogInfo<StatusEffectManager>(
                        $"{_ownerName}: {e.Type} removed (player turn start)"
                    );
                    continue;
                }
                i++;
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
        public int ModifyDamageTaken(
            int baseDamage,
            BattleStats attackerStats,
            bool isAttackerPlayer = false
        )
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

            // Apply Thorns — reflects incoming pressure back as Opinion gain.
            // isAttackerPlayer = false when an enemy attacked the player, so IsToPlayer = false
            // routes to RaiseOpinion in BattleManager (the defender looks good hitting back).
            int thornsStacks = GetStacks(StatusEffectType.Thorns);
            if (thornsStacks > 0)
            {
                GameLogger.LogInfo<StatusEffectManager>(
                    $"{_ownerName}: Thorns reflected {thornsStacks} to Opinion Meter"
                );
                EventBus.Publish(
                    new DamageDealtEvent
                    {
                        Amount = thornsStacks,
                        IsToPlayer = isAttackerPlayer,
                        AttackerName = _ownerName,
                        SourceEnemyIndex = -1,
                        TargetEnemyIndex = -1,
                    }
                );
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
                final *= 2; // show doubled — Exposed will fire on the actual hit
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
                final = 1; // show 1 — Intangible stack consumed on actual hit
            return Mathf.Max(0, Mathf.RoundToInt(final));
        }

        /// <summary>
        /// Modifies Support gained based on active effects (Dexterity/Frail).
        /// </summary>
        public int ModifySupportGained(int baseSupport)
        {
            float final = baseSupport;
            final += GetStacks(StatusEffectType.Dexterity);
            if (HasEffect(StatusEffectType.Frail))
                final *= 0.75f;
            return Mathf.Max(0, Mathf.RoundToInt(final));
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

        // --- Collection mutation: keep _activeEffects and _byType in lockstep ---

        private void AddEffectInternal(StatusEffect effect)
        {
            _activeEffects.Add(effect);
            _byType[effect.Type] = effect;
        }

        private void RemoveEffectInternal(StatusEffect effect)
        {
            _activeEffects.Remove(effect);
            _byType.Remove(effect.Type);
        }

        private void RemoveEffectAt(int index)
        {
            StatusEffect effect = _activeEffects[index];
            _activeEffects.RemoveAt(index);
            _byType.Remove(effect.Type);
        }

        private void TriggerEffect(StatusEffect effect, StatusTriggerTiming timing)
        {
            // Effects that don't need stats
            if (GetEffectTiming(effect.Type) != timing)
                return;

            switch (effect.Type)
            {
                // Most effects are handled in Modify methods above
                // This is for pure trigger-based effects
                default:
                    break;
            }
        }

        private void TriggerEffectWithStats(
            StatusEffect effect,
            StatusTriggerTiming timing,
            BattleStats ownerStats
        )
        {
            if (GetEffectTiming(effect.Type) != timing)
                return;

            switch (effect.Type)
            {
                case StatusEffectType.Scandal:
                    // Routes through DamageDealtEvent → BattleManager absorbs through Support → LowerOpinion.
                    EventBus.Publish(
                        new DamageDealtEvent
                        {
                            Amount = effect.Stacks,
                            IsToPlayer = true,
                            AttackerName = "Scandal",
                            SourceEnemyIndex = -1,
                            TargetEnemyIndex = -1,
                        }
                    );
                    GameLogger.LogInfo<StatusEffectManager>(
                        $"{_ownerName}: Scandal applied {effect.Stacks} opinion pressure"
                    );
                    break;

                case StatusEffectType.Regeneration:
                    // Regeneration raises the opinion meter directly.
                    EventBus.Publish(new OpinionRaisedDirectlyEvent { Amount = effect.Stacks });
                    GameLogger.LogInfo<StatusEffectManager>(
                        $"{_ownerName}: Regeneration raised opinion by {effect.Stacks}"
                    );
                    break;
                // Ritual handled by BattleManager.StartTurn — reads stacks directly and calls GainSupport/GainDenial.
            }
        }

        private StatusTriggerTiming GetEffectTiming(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Scandal => StatusTriggerTiming.OnTurnEnd,
                StatusEffectType.Regeneration => StatusTriggerTiming.OnTurnEnd,
                // Ritual: handled by BattleManager.StartTurn directly; no trigger needed here.
                _ => StatusTriggerTiming.Passive,
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
                StatusEffectType.Stunned => true,
                StatusEffectType.Rattled => true,
                StatusEffectType.Hardened => true,
                StatusEffectType.Fanatic => true,
                _ => false,
            };
        }

        #endregion
    }
}
