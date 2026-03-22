

namespace Crookedile.Gameplay.Battle
{
    
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using Crookedile.Core;
    using Crookedile.Data;
    using Crookedile.Data.Cards;
    using Crookedile.Utilities;
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
        [SerializeField] private string _name;

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

        // ─── Shared damage helpers ───────────────────────────────────────────────

        /// <summary>
        /// Applies Resolve damage from <paramref name="attacker"/> to <paramref name="target"/>,
        /// respecting status-effect modifiers (Strength, Weakened, Vulnerable, Plated,
        /// Intangible, Thorns) and the attacker's Hostility damage multiplier.
        /// Publishes <see cref="DamageDealtEvent"/>, accumulates <see cref="EffectExecutionContext.LastDamageDealt"/>,
        /// and sets <see cref="EffectExecutionContext.LastTargetDied"/> if the target is defeated.
        /// </summary>
        /// <returns>Actual damage applied after all modifiers.</returns>
        protected static int ApplyResolveDamage(
            BattleStats target, BattleStats attacker, int baseDamage, EffectExecutionContext ctx)
        {
            StatusEffectManager attackerMgr = ctx.GetStatusEffectManager(attacker);
            StatusEffectManager targetMgr   = ctx.GetStatusEffectManager(target);

            int mod = attackerMgr?.ModifyDamageDealt(baseDamage) ?? baseDamage;
            mod     = targetMgr?.ModifyDamageTaken(mod, attacker, isAttackerPlayer: ctx.IsPlayerCard) ?? mod;

            // Hostile enemies deal amplified damage; neutral and receptive don't.
            if (!ctx.IsPlayerCard && attacker.CurrentHostility > 0)
            {
                float mult = Mathf.Max(0.1f, attacker.HostilityDamageMultiplier);
                mod = Mathf.RoundToInt(mod * mult);
            }

            int actual = target.DamageResolve(mod);
            if (actual > 0)
            {
                EventBus.Publish(new DamageDealtEvent
                {
                    Amount           = actual,
                    IsToPlayer       = target == ctx.PlayerStats,
                    AttackerName     = ctx.AttackerName,
                    SourceEnemyIndex = ctx.IsPlayerCard ? -1 : ctx.AttackerEnemyIndex,
                    TargetEnemyIndex = ctx.IsPlayerCard ? ctx.AttackerEnemyIndex : -1,
                });
            }

            ctx.LastDamageDealt += actual;
            if (target.CurrentResolve <= 0) ctx.LastTargetDied = true;

            return actual;
        }

        // ─── Shared composure helpers ────────────────────────────────────────────

        /// <summary>
        /// Applies Composure gain to <paramref name="target"/>, respecting Dexterity/Frail
        /// status modifiers. Accumulates <see cref="EffectExecutionContext.LastComposureGained"/>.
        /// </summary>
        protected static void ApplyGainComposure(BattleStats target, int amount, EffectExecutionContext ctx)
        {
            StatusEffectManager mgr      = ctx.GetStatusEffectManager(target);
            int                 modified = mgr?.ModifyComposureGained(amount) ?? amount;
            target.GainComposure(modified);
            ctx.LastComposureGained += modified;
        }

        /// <summary>
        /// Removes Composure from <paramref name="target"/> and accumulates
        /// <see cref="EffectExecutionContext.LastComposureLost"/>.
        /// </summary>
        /// <returns>Actual Composure removed after clamping.</returns>
        protected static int ApplyLoseComposure(BattleStats target, int amount, EffectExecutionContext ctx)
        {
            int actual = target.LoseComposure(amount);
            ctx.LastComposureLost += actual;
            return actual;
        }

        // ─── Shared card-selection helper ────────────────────────────────────────

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
            CardSelectionMode       mode,
            CardType                filterType,
            string                  choiceTitle,
            int                     count,
            Action<List<CardData>>  onResolved)
        {
            var candidates = new List<CardData>();
            foreach (var c in pool)
            {
                if (c == null) continue;
                if (mode == CardSelectionMode.RandomByType && c.CardType != filterType) continue;
                candidates.Add(c);
            }

            if (candidates.Count == 0)
            {
                onResolved?.Invoke(new List<CardData>());
                return;
            }

            if (mode == CardSelectionMode.PlayerChoice)
            {
                EventBus.Publish(new CardChoiceRequestedEvent
                {
                    Title         = choiceTitle,
                    Choices       = candidates,
                    RequiredCount = Mathf.Min(count, candidates.Count),
                    OnConfirmed   = onResolved,
                });
            }
            else
            {
                int pickCount = Mathf.Min(count, candidates.Count);
                var chosen    = new List<CardData>();
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
