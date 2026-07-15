using System.Collections.Generic;
using System.Linq;
using Crookedile.Core;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Manages all status effects for a single combatant. Statuses are stored per
    /// <see cref="StatusBehavior"/> (keyed by its stable Id); each status owns its own rules via
    /// the behavior hooks, so the modify-pipeline methods here just fold the active effects.
    /// </summary>
    public class StatusEffectManager
    {
        private List<StatusEffect> _activeEffects = new List<StatusEffect>();

        // Id-indexed mirror of _activeEffects for O(1) lookups (GetStacks/HasStatus/etc.).
        // Always kept in sync via AddEffectInternal/RemoveEffectInternal — never mutate
        // _activeEffects directly.
        private readonly Dictionary<string, StatusEffect> _byId =
            new Dictionary<string, StatusEffect>();

        private string _ownerName; // For logging
        private BattleStats _owner; // Optional — used to sync Hardened/Fanatic flags

        public IReadOnlyList<StatusEffect> ActiveEffects => _activeEffects;

        /// <summary>Display name of this manager's owner (used for combat-log attribution, e.g. Thorns).</summary>
        public string OwnerName => _ownerName;

        public StatusEffectManager(string ownerName, BattleStats owner = null)
        {
            _ownerName = ownerName;
            _owner = owner;
            // Warded (Protector): BattleStats asks us to spend a ward stack when a hostility
            // change is about to land.
            _owner?.SetWardConsumer(TryConsumeWardStack);
        }

        /// <summary>
        /// Spends one Warded stack if any are active. Returns true if a stack absorbed the hit.
        /// </summary>
        public bool TryConsumeWardStack()
        {
            if (GetStacks<WardedStatus>() <= 0)
                return false;

            RemoveStacksNotify<WardedStatus>(1);
            GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: Warded absorbed the hit");
            return true;
        }

        #region Apply/Remove Effects

        /// <summary>
        /// Applies a status. Stacks if already present, otherwise adds new.
        /// </summary>
        public void ApplyStatus(
            StatusBehavior behavior,
            int stacks,
            StatusDurationType durationType = StatusDurationType.DecreasePerTurn
        )
        {
            if (behavior == null)
            {
                GameLogger.LogWarning<StatusEffectManager>(
                    $"{_ownerName}: ApplyStatus called with a null behavior — ignored"
                );
                return;
            }

            // Warded (Protector): a ward stack eats an incoming debuff before it lands.
            // Only genuine applications (positive stacks) are blocked — stack removals pass.
            if (stacks > 0 && behavior.IsDebuff && TryConsumeWardStack())
            {
                GameLogger.LogInfo<StatusEffectManager>(
                    $"{_ownerName}: Warded blocked {behavior.DisplayName}"
                );
                return;
            }

            _byId.TryGetValue(behavior.Id, out StatusEffect existing);

            if (existing != null)
            {
                // Stunned is non-stackable: a second application is ignored entirely.
                if (behavior is StunnedStatus)
                {
                    GameLogger.LogInfo<StatusEffectManager>(
                        $"{_ownerName}: {behavior.DisplayName} already active — re-application ignored"
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
                        $"{_ownerName}: {behavior.DisplayName} neutralised — removed"
                    );
                    return;
                }

                GameLogger.LogInfo<StatusEffectManager>(
                    $"{_ownerName}: {behavior.DisplayName} stacked {stacks:+0;-0} (now {existing.Stacks} stacks)"
                );
            }
            else
            {
                // New effect
                StatusEffect newEffect = new StatusEffect(behavior, stacks, durationType);
                AddEffectInternal(newEffect);
                string durationText = durationType switch
                {
                    StatusDurationType.Permanent => "permanent",
                    StatusDurationType.RemoveEndOfTurn => "until end of turn",
                    _ => $"{stacks} stacks",
                };
                GameLogger.LogInfo<StatusEffectManager>(
                    $"{_ownerName}: Applied {behavior.DisplayName} ({durationText})"
                );
            }

            SyncHostilityFlags();
        }

        private void SyncHostilityFlags()
        {
            if (_owner == null)
                return;

            bool hardened = false;
            bool fanatic = false;
            int devotionResist = 0;
            foreach (StatusEffect e in _activeEffects)
            {
                hardened |= e.Behavior.BlocksHostilityReduction;
                fanatic |= e.Behavior.BlocksHostilityGain;
                devotionResist += e.Behavior.HostilityResistPerStack * e.Stacks;
            }

            _owner.SetHardened(hardened);
            _owner.SetFanatic(fanatic);
            _owner.SetDevotionResist(devotionResist);
        }

        /// <summary>
        /// Removes all stacks of a status.
        /// </summary>
        public void RemoveStatus(StatusBehavior behavior)
        {
            if (behavior != null && _byId.TryGetValue(behavior.Id, out StatusEffect effect))
            {
                RemoveEffectInternal(effect);
                GameLogger.LogInfo<StatusEffectManager>(
                    $"{_ownerName}: Removed {behavior.DisplayName}"
                );
                SyncHostilityFlags();
            }
        }

        public void RemoveStatus<T>()
            where T : StatusBehavior => RemoveStatus(StatusRegistry.Get<T>());

        /// <summary>
        /// Removes X stacks of a status.
        /// </summary>
        public void RemoveStacks(StatusBehavior behavior, int amount)
        {
            if (behavior != null && _byId.TryGetValue(behavior.Id, out StatusEffect effect))
            {
                if (effect.ReduceStacks(amount))
                {
                    RemoveEffectInternal(effect);
                    GameLogger.LogInfo<StatusEffectManager>(
                        $"{_ownerName}: {behavior.DisplayName} depleted and removed"
                    );
                }
                else
                {
                    GameLogger.LogInfo<StatusEffectManager>(
                        $"{_ownerName}: {behavior.DisplayName} reduced by {amount} stacks (now {effect.Stacks})"
                    );
                }
                SyncHostilityFlags();
            }
        }

        /// <summary>Removes X stacks of a status by behavior type.</summary>
        public void RemoveStacks<T>(int amount)
            where T : StatusBehavior => RemoveStacks(StatusRegistry.Get<T>(), amount);

        /// <summary>
        /// Removes all stacks of a status AND publishes the removal as a negative-stack
        /// <see cref="StatusEffectAppliedEvent"/> so badges and passives react.
        /// Single home for the publish-on-removal pattern — use this instead of
        /// hand-rolling the event at call sites. No-op if the status isn't active.
        /// </summary>
        public void RemoveStatusNotify(StatusBehavior behavior)
        {
            if (behavior == null || !_byId.TryGetValue(behavior.Id, out StatusEffect effect))
                return;
            int stacks = effect.Stacks;
            RemoveStatus(behavior);
            PublishStackChange(behavior, -stacks);
        }

        /// <summary>
        /// Removes <paramref name="amount"/> stacks of a status AND publishes the change as a
        /// negative-stack <see cref="StatusEffectAppliedEvent"/>. No-op if the status isn't active.
        /// </summary>
        public void RemoveStacksNotify(StatusBehavior behavior, int amount)
        {
            if (
                behavior == null
                || amount <= 0
                || !_byId.TryGetValue(behavior.Id, out StatusEffect effect)
            )
                return;
            int removed = Mathf.Min(amount, effect.Stacks);
            RemoveStacks(behavior, amount);
            PublishStackChange(behavior, -removed);
        }

        public void RemoveStacksNotify<T>(int amount)
            where T : StatusBehavior => RemoveStacksNotify(StatusRegistry.Get<T>(), amount);

        private void PublishStackChange(StatusBehavior behavior, int stacksDelta)
        {
            // The player's manager has no owner BattleStats; enemies carry their roster index.
            int enemyIndex = _owner?.OwnerEnemyIndex ?? -1;
            EventBus.Publish(
                new StatusEffectAppliedEvent
                {
                    Behavior = behavior,
                    Stacks = stacksDelta,
                    IsToPlayer = enemyIndex < 0,
                    EnemyIndex = enemyIndex,
                }
            );
        }

        /// <summary>
        /// Clears all status effects.
        /// </summary>
        public void ClearAll()
        {
            _activeEffects.Clear();
            _byId.Clear();
            GameLogger.LogInfo<StatusEffectManager>($"{_ownerName}: All status effects cleared");
            SyncHostilityFlags();
        }

        #endregion

        #region Query Effects

        /// <summary>Stacks of a status (0 if not present).</summary>
        public int GetStacks(StatusBehavior behavior) =>
            behavior != null && _byId.TryGetValue(behavior.Id, out StatusEffect e) ? e.Stacks : 0;

        /// <summary>Stacks of a status by behavior type.</summary>
        public int GetStacks<T>()
            where T : StatusBehavior => GetStacks(StatusRegistry.Get<T>());

        /// <summary>True if a status is present.</summary>
        public bool HasStatus(StatusBehavior behavior) =>
            behavior != null && _byId.ContainsKey(behavior.Id);

        public bool HasStatus<T>()
            where T : StatusBehavior => HasStatus(StatusRegistry.Get<T>());

        #endregion

        #region Query Effects (cont.)

        /// <summary>
        /// Gets all active debuffs.
        /// </summary>
        public IEnumerable<StatusEffect> GetDebuffs()
        {
            return _activeEffects.Where(e => e.Behavior.IsDebuff);
        }

        /// <summary>
        /// Gets all active buffs.
        /// </summary>
        public IEnumerable<StatusEffect> GetBuffs()
        {
            return _activeEffects.Where(e => !e.Behavior.IsDebuff);
        }

        /// <summary>
        /// Returns true if the combatant has at least one active debuff.
        /// Used by <see cref="EnemyHasAnyDebuffCondition"/> and similar passive condition checks.
        /// </summary>
        public bool HasAnyDebuff()
        {
            return _activeEffects.Any(e => e.Behavior.IsDebuff);
        }

        /// <summary>
        /// Returns true if the combatant has at least one active buff.
        /// Used by <see cref="EnemyHasAnyBuffCondition"/> and similar passive condition checks.
        /// </summary>
        public bool HasAnyBuff()
        {
            return _activeEffects.Any(e => !e.Behavior.IsDebuff);
        }

        #endregion

        #region Trigger Effects

        /// <summary>
        /// Called at the start of turn. (Opinion-affecting turn statuses — Ritual, Smear,
        /// Regeneration — are applied by BattleManager, which owns the meter.)
        /// </summary>
        public void OnTurnStart(BattleStats ownerStats)
        {
            GameLogger.LogInfo<StatusEffectManager>(
                $"{_ownerName}: Turn start status effects triggered"
            );
        }

        /// <summary>
        /// Called at the end of turn. Handles duration decay/removal. (Opinion-affecting turn
        /// statuses are applied by BattleManager before this runs.)
        /// </summary>
        public void OnTurnEnd(BattleStats ownerStats)
        {
            // Single in-place pass. When an effect is removed we don't advance the index (the next
            // element slides into the current slot), so removal is O(1) per element with no
            // separate toRemove list.
            int i = 0;
            while (i < _activeEffects.Count)
            {
                StatusEffect effect = _activeEffects[i];

                // Handle duration types
                if (effect.DurationType == StatusDurationType.RemoveEndOfTurn)
                {
                    RemoveEffectAt(i);
                    GameLogger.LogInfo<StatusEffectManager>(
                        $"{_ownerName}: {effect.DisplayName} removed (end of turn)"
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
                            $"{_ownerName}: {effect.DisplayName} depleted and removed"
                        );
                        continue;
                    }

                    GameLogger.LogInfo<StatusEffectManager>(
                        $"{_ownerName}: {effect.DisplayName} reduced to {effect.Stacks} stacks"
                    );
                }

                i++;
            }

            // Statuses may have faded (e.g. Devotion) — resync hostility-flag/resist state.
            SyncHostilityFlags();
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
                        $"{_ownerName}: {e.DisplayName} removed (player turn start)"
                    );
                    continue;
                }
                i++;
            }
        }

        /// <summary>
        /// Modifies pressure this combatant deals, folding every active status's outgoing hook
        /// (Strength/Turncoat add, Weakened/Guilt subtract).
        /// </summary>
        public int ModifyDamageDealt(int baseDamage)
        {
            float final = baseDamage;
            foreach (StatusEffect e in _activeEffects)
                final = e.Behavior.ModifyOutgoingPressure(final, e.Stacks);
            return Mathf.Max(0, Mathf.RoundToInt(final));
        }

        /// <summary>
        /// Modifies pressure this combatant takes, folding every active status's incoming hook in
        /// application order; hard overrides (Intangible) apply last, and consume-on-hit statuses
        /// (Exposed, Intangible) lose a stack afterwards. Pure with respect to the Opinion Meter —
        /// the Thorns reflection is returned via <paramref name="thornsReflected"/> for the caller
        /// to route through the ledger, rather than published from here.
        /// </summary>
        /// <param name="isAttackerPlayer">Forwarded by the caller to direct the reflected pressure.</param>
        /// <param name="thornsReflected">Opinion pressure to reflect back at the attacker (0 if no Thorns).</param>
        public int ModifyDamageTaken(
            int baseDamage,
            BattleStats attackerStats,
            bool isAttackerPlayer,
            out int thornsReflected
        )
        {
            int attackerHostility = attackerStats != null ? attackerStats.CurrentHostility : 0;
            float final = baseDamage;
            List<StatusEffect> consumed = null;

            foreach (StatusEffect e in _activeEffects)
            {
                if (!e.Behavior.IncomingOverride)
                    final = e.Behavior.ModifyIncomingPressure(final, e.Stacks, attackerHostility);
                if (e.Behavior.ConsumedOnIncomingHit)
                    (consumed ??= new List<StatusEffect>()).Add(e);
            }
            foreach (StatusEffect e in _activeEffects)
            {
                if (e.Behavior.IncomingOverride)
                    final = e.Behavior.ModifyIncomingPressure(final, e.Stacks, attackerHostility);
            }

            if (consumed != null)
                foreach (StatusEffect e in consumed)
                    RemoveStacks(e.Behavior, 1);

            // Thorns reflects incoming pressure back; the caller applies it via the ledger.
            thornsReflected = GetStacks<ThornsStatus>();
            if (thornsReflected > 0)
                GameLogger.LogInfo<StatusEffectManager>(
                    $"{_ownerName}: Thorns reflecting {thornsReflected} to Opinion Meter"
                );

            return Mathf.Max(0, Mathf.RoundToInt(final));
        }

        /// <summary>
        /// Preview version of ModifyDamageDealt — same fold, no side effects.
        /// Safe to call repeatedly for UI display.
        /// </summary>
        public int PreviewDamageDealt(int baseDamage)
        {
            return ModifyDamageDealt(baseDamage);
        }

        /// <summary>
        /// Preview version of ModifyDamageTaken — folds the incoming hooks WITHOUT consuming
        /// Exposed/Intangible stacks or triggering Thorns. Safe to call for UI display.
        /// </summary>
        /// <param name="attackerHostility">
        /// Pass the attacker's current Hostility when known (e.g. from intent preview) so
        /// Rattled can be factored in. Defaults to 0 (no adjustment).
        /// </param>
        public int PreviewDamageTaken(int incomingDamage, int attackerHostility = 0)
        {
            float final = incomingDamage;
            foreach (StatusEffect e in _activeEffects)
            {
                if (!e.Behavior.IncomingOverride)
                    final = e.Behavior.ModifyIncomingPressure(final, e.Stacks, attackerHostility);
            }
            foreach (StatusEffect e in _activeEffects)
            {
                if (e.Behavior.IncomingOverride)
                    final = e.Behavior.ModifyIncomingPressure(final, e.Stacks, attackerHostility);
            }
            return Mathf.Max(0, Mathf.RoundToInt(final));
        }

        /// <summary>
        /// Modifies Support gained based on active effects (Dexterity/Frail).
        /// </summary>
        public int ModifySupportGained(int baseSupport)
        {
            int final = baseSupport;
            foreach (StatusEffect e in _activeEffects)
                final = e.Behavior.ModifySupportGained(final, e.Stacks);
            return Mathf.Max(0, final);
        }

        /// <summary>
        /// Modifies Denial an enemy gains (Shame drops the enemy's shield).
        /// </summary>
        public int ModifyDenialGained(int baseDenial)
        {
            int final = baseDenial;
            foreach (StatusEffect e in _activeEffects)
                final = e.Behavior.ModifyDenialGained(final, e.Stacks);
            return Mathf.Max(0, final);
        }

        /// <summary>
        /// Modifies card AP cost based on active effects (Focus/Energized/Entangled).
        /// </summary>
        public int ModifyCardCost(int baseCost)
        {
            int final = baseCost;
            foreach (StatusEffect e in _activeEffects)
                final = e.Behavior.ModifyCardCost(final, e.Stacks);
            return Mathf.Max(0, final);
        }

        #endregion

        #region Private Helpers

        // --- Collection mutation: keep _activeEffects and _byId in lockstep ---

        private void AddEffectInternal(StatusEffect effect)
        {
            _activeEffects.Add(effect);
            _byId[effect.Id] = effect;
        }

        private void RemoveEffectInternal(StatusEffect effect)
        {
            _activeEffects.Remove(effect);
            _byId.Remove(effect.Id);
            effect.Behavior.OnDepleted(_owner);
        }

        private void RemoveEffectAt(int index)
        {
            StatusEffect effect = _activeEffects[index];
            _activeEffects.RemoveAt(index);
            _byId.Remove(effect.Id);
            effect.Behavior.OnDepleted(_owner);
        }

        #endregion
    }
}
