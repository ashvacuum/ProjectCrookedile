namespace Crookedile.Gameplay.Battle
{
    using System;
    using System.Collections.Generic;
    using Crookedile.Core;
    using Crookedile.Data;
    using Crookedile.Data.Cards;
    using Crookedile.Utilities;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for all battle effects in the Crookedile effect system.
    ///
    /// Each concrete subclass owns only the serialized fields it actually needs — no
    /// ShowIf conditionals, no shared field pollution. To add a new effect type: create a
    /// new <c>[Serializable]</c> class inheriting from this, implement
    /// <see cref="Execute"/> and <see cref="GetDescription"/>. No other file changes required.
    ///
    /// Stored via <c>[SerializeReference]</c> on <see cref="Data.Cards.CardData"/> and
    /// <see cref="Data.Enemy.EnemyMoveData"/> — Odin Inspector renders a type-picker
    /// dropdown for heterogeneous lists.
    /// </summary>
    [Serializable]
    public abstract class BattleEffect
    {
        [SerializeField]
        private string _name;

        /// <summary>Optional designer label — shown in the inspector list for readability.</summary>
        public string Name => _name;

        /// <summary>
        /// The target type of this effect. Defaults to <see cref="TargetType.Self"/> for effects
        /// that have no explicit target (buffs, deck manipulation, etc.).
        /// Override in subclasses that expose a <c>_target</c> field.
        /// Used by <c>BattleManager</c> to determine whether a card raises hostility on a single enemy.
        /// </summary>
        public virtual TargetType Target => TargetType.Self;

        /// <summary>
        /// Executes this effect using the provided execution context.
        /// The context carries all dependencies (caster, target, deck, status managers)
        /// and accumulates results (damage dealt, healing applied, composure gained) for
        /// triggered effects to read.
        /// </summary>
        /// <param name="ctx">Shared context for this card/move resolution.</param>
        /// <param name="amountOverride">
        /// Optional per-effect amount override — set by the Confused status effect to
        /// randomise card values 0–3. Effects that have a "main amount" should use
        /// <c>amountOverride ?? _amount</c>. Effects without a discrete amount ignore it.
        /// </param>
        public abstract void Execute(EffectExecutionContext ctx, int? amountOverride = null);

        /// <summary>
        /// Returns a human-readable description of what this effect does.
        /// Used for card tooltips, enemy move descriptions, and the card-design editor.
        /// </summary>
        public abstract string GetDescription();

        #region Shared pressure helpers
        /// <summary>
        /// Applies opinion-meter pressure from <paramref name="attacker"/> to <paramref name="target"/>.
        /// The target's shield absorbs first; the remainder is published as
        /// <see cref="DamageDealtEvent"/> which BattleManager routes to the opinion meter.
        /// Hostile-enemy multiplier still applies when enemies attack.
        /// </summary>
        /// <returns>Post-shield pressure that reached the opinion meter.</returns>
        protected static int ApplyPressure(
            BattleStats target,
            BattleStats attacker,
            int basePressure,
            EffectExecutionContext ctx
        )
        {
            StatusEffectManager attackerMgr = ctx.GetStatusEffectManager(attacker);
            StatusEffectManager targetMgr = ctx.GetStatusEffectManager(target);

            int mod = attackerMgr?.ModifyDamageDealt(basePressure) ?? basePressure;
            mod =
                targetMgr?.ModifyDamageTaken(mod, attacker, isAttackerPlayer: ctx.IsPlayerCard)
                ?? mod;

            // Hostile enemies amplify their opinion-meter pressure.
            if (!ctx.IsPlayerCard && attacker.CurrentHostility > 0)
                mod = Mathf.RoundToInt(mod * Mathf.Max(0.1f, attacker.HostilityDamageMultiplier));

            // Shield absorbs first — the remainder is what actually hits the opinion meter.
            int remainder = target.AbsorbThroughShield(mod);
            if (remainder > 0)
            {
                EventBus.Publish(
                    new DamageDealtEvent
                    {
                        Amount = remainder,
                        IsToPlayer = target == ctx.PlayerStats,
                        AttackerName = ctx.AttackerName,
                        SourceEnemyIndex = ctx.IsPlayerCard ? -1 : ctx.AttackerEnemyIndex,
                        TargetEnemyIndex = ctx.IsPlayerCard ? ctx.AttackerEnemyIndex : -1,
                    }
                );
            }

            ctx.LastDamageDealt += remainder;
            return remainder;
        }

        // Keep the old name as a redirect so any call sites not yet updated still compile.
        protected static int ApplyResolveDamage(
            BattleStats target,
            BattleStats attacker,
            int baseDamage,
            EffectExecutionContext ctx
        ) => ApplyPressure(target, attacker, baseDamage, ctx);

        #endregion

        #region Shared shield helpers
        /// <summary>
        /// Applies Shield gain to <paramref name="target"/>, respecting Dexterity/Frail
        /// status modifiers. Accumulates <see cref="EffectExecutionContext.LastShieldGained"/>.
        /// </summary>
        protected static void ApplyGainShield(
            BattleStats target,
            int amount,
            EffectExecutionContext ctx
        )
        {
            StatusEffectManager mgr = ctx.GetStatusEffectManager(target);
            int modified = mgr?.ModifyShieldGained(amount) ?? amount;
            target.GainShield(modified);
            ctx.LastShieldGained += modified;
        }

        /// <summary>
        /// Removes Shield from <paramref name="target"/> and accumulates
        /// <see cref="EffectExecutionContext.LastShieldLost"/>.
        /// </summary>
        /// <returns>Actual Shield removed after clamping.</returns>
        protected static int ApplyLoseShield(
            BattleStats target,
            int amount,
            EffectExecutionContext ctx
        )
        {
            int actual = target.LoseShield(amount);
            ctx.LastShieldLost += actual;
            return actual;
        }

        #endregion

        #region Shared card-selection helper
        /// <summary>
        /// Central card-selection resolver — routes to player-choice UI or random auto-pick.
        /// <list type="bullet">
        ///   <item><see cref="CardSelectionMode.PlayerChoice"/> — publishes <see cref="CardChoiceRequestedEvent"/></item>
        ///   <item><see cref="CardSelectionMode.RandomAny"/> — picks randomly from full pool</item>
        ///   <item><see cref="CardSelectionMode.RandomByType"/> — filters by <paramref name="filterType"/>, then picks randomly</item>
        /// </list>
        /// </summary>
        protected static void ResolveCardSelection(
            IReadOnlyList<CardData> pool,
            CardSelectionMode mode,
            CardType filterType,
            string choiceTitle,
            int count,
            Action<List<CardData>> onResolved
        )
        {
            var candidates = new List<CardData>();
            foreach (var c in pool)
            {
                if (c == null)
                    continue;
                if (mode == CardSelectionMode.RandomByType && c.CardType != filterType)
                    continue;
                candidates.Add(c);
            }

            if (candidates.Count == 0)
            {
                onResolved?.Invoke(new List<CardData>());
                return;
            }

            if (mode == CardSelectionMode.PlayerChoice)
            {
                EventBus.Publish(
                    new CardChoiceRequestedEvent
                    {
                        Title = choiceTitle,
                        Choices = candidates,
                        RequiredCount = Mathf.Min(count, candidates.Count),
                        OnConfirmed = onResolved,
                    }
                );
            }
            else
            {
                int pickCount = Mathf.Min(count, candidates.Count);
                var chosen = new List<CardData>();
                var remaining = new List<CardData>(candidates);
                for (int i = 0; i < pickCount; i++)
                {
                    int idx = RandomHelper.Range(0, remaining.Count);
                    chosen.Add(remaining[idx]);
                    remaining.RemoveAt(idx);
                }
                onResolved?.Invoke(chosen);
            }
        }
    }
}
        #endregion
