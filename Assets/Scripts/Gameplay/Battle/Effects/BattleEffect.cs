namespace Crookedile.Gameplay.Battle
{
    using System;
    using System.Collections.Generic;
    using Crookedile.Core;
    using Crookedile.Data;
    using Crookedile.Data.Cards;
    using Crookedile.Utilities;
    using UnityEngine;

    public enum DamagePreviewType
    {
        Fixed,
        Random,
        EqualToShield,
    }

    public struct DamagePreview
    {
        public DamagePreviewType Type;
        public int Amount;
        public int MinAmount;
        public int MaxAmount;
    }

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
        /// Returns damage preview data for intent display. Non-damage effects return null.
        /// Override in damage subclasses to expose their amounts without executing.
        /// </summary>
        public virtual DamagePreview? GetDamagePreview() => null;

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only health check: yields human-readable configuration problems with this effect
        /// (e.g. an unset required reference that would make it silently no-op). Empty when correctly
        /// configured. Override in subclasses that have required fields. Consumed by the Card Database
        /// health view so the database is provably consistent. Stripped from player builds.
        /// </summary>
        public virtual IEnumerable<string> GetConfigurationIssues()
        {
            yield break;
        }
#endif

        /// <summary>
        /// Executes this effect using the provided execution context.
        /// The context carries all dependencies (caster, target, deck, status managers)
        /// and accumulates results (pressure applied, opinion raised, support gained) for
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

        #region Amount resolution
        /// <summary>
        /// Resolves an effect's runtime amount: a Confused override wins; otherwise the authored
        /// fixed value (when <paramref name="source"/> is FixedAmount) or the context-sourced value.
        /// Shared by amount-based effects so the resolution rule lives in one place.
        /// </summary>
        protected static int ResolveAmount(
            EffectExecutionContext ctx,
            int? amountOverride,
            int fixedAmount,
            EffectContextValue source
        ) =>
            amountOverride
            ?? (source == EffectContextValue.FixedAmount ? fixedAmount : ctx.GetValue(source));

        /// <summary>Human-readable amount for descriptions: the fixed value, or the source name.</summary>
        protected static string DescribeAmount(int fixedAmount, EffectContextValue source) =>
            source == EffectContextValue.FixedAmount ? fixedAmount.ToString() : source.ToString();

        #endregion

        #region Shared pressure helpers
        /// <summary>
        /// Applies opinion-meter pressure from <paramref name="attacker"/> to <paramref name="target"/>.
        /// Routes through <see cref="OpinionLedger.ApplyPressure"/> which absorbs through the session
        /// shield, moves the meter, and publishes <see cref="DamageDealtEvent"/> as a notification.
        /// Hostile-enemy multiplier still applies when enemies attack. Thorns on the target reflects
        /// pressure back through the same ledger.
        /// </summary>
        /// <returns>Pressure that reached the opinion meter, pre session-shield absorption.</returns>
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
            int thornsReflected = 0;
            if (targetMgr != null)
                mod = targetMgr.ModifyDamageTaken(
                    mod,
                    attacker,
                    isAttackerPlayer: ctx.IsPlayerCard,
                    thornsReflected: out thornsReflected
                );

            // Hostile enemies amplify their opinion-meter pressure.
            // HostilityDamageMultiplier already floors at 0.1, so no extra clamp needed.
            if (!ctx.IsPlayerCard && attacker.CurrentHostility > 0)
                mod = Mathf.RoundToInt(mod * attacker.HostilityDamageMultiplier);

            // Thorns reflects back at the attacker's side first (preserving prior ordering).
            if (thornsReflected > 0)
                RoutePressure(
                    ctx,
                    thornsReflected,
                    toPlayer: ctx.IsPlayerCard,
                    attackerName: targetMgr?.OwnerName ?? "Thorns",
                    sourceEnemyIndex: -1,
                    targetEnemyIndex: -1
                );

            RoutePressure(
                ctx,
                mod,
                toPlayer: target == ctx.PlayerStats,
                attackerName: ctx.AttackerName,
                sourceEnemyIndex: ctx.IsPlayerCard ? -1 : ctx.AttackerEnemyIndex,
                targetEnemyIndex: ctx.IsPlayerCard ? ctx.AttackerEnemyIndex : -1
            );

            ctx.LastDamageDealt += mod;
            return mod;
        }

        /// <summary>
        /// Sends opinion pressure through the battle's <see cref="OpinionLedger"/> (the command path).
        /// When no BattleManager is present (e.g. the unit-test harness), falls back to publishing
        /// the notification only — there is no meter to move.
        /// </summary>
        private static void RoutePressure(
            EffectExecutionContext ctx,
            int amount,
            bool toPlayer,
            string attackerName,
            int sourceEnemyIndex,
            int targetEnemyIndex
        )
        {
            if (amount <= 0)
                return;

            OpinionLedger ledger = ctx.BattleManager?.Opinion;
            if (ledger != null)
            {
                ledger.ApplyPressure(
                    amount,
                    toPlayer,
                    attackerName,
                    sourceEnemyIndex,
                    targetEnemyIndex
                );
            }
            else
            {
                EventBus.Publish(
                    new DamageDealtEvent
                    {
                        Amount = amount,
                        IsToPlayer = toPlayer,
                        AttackerName = attackerName,
                        SourceEnemyIndex = sourceEnemyIndex,
                        TargetEnemyIndex = targetEnemyIndex,
                    }
                );
            }
        }

        // Keep the old name as a redirect so any call sites not yet updated still compile.
        protected static int ApplyResolveDamage(
            BattleStats target,
            BattleStats attacker,
            int baseDamage,
            EffectExecutionContext ctx
        ) => ApplyPressure(target, attacker, baseDamage, ctx);

        #endregion

        #region Session shield helpers

        protected static void ApplyGainSupport(int amount, EffectExecutionContext ctx)
        {
            if (ctx.BattleManager == null)
                return;
            int modified = ctx.PlayerStatusEffects?.ModifySupportGained(amount) ?? amount;
            ctx.BattleManager.GainSupport(modified);
            ctx.LastSupportGained += modified;
        }

        protected static void ApplyGainDenial(int amount, EffectExecutionContext ctx)
        {
            if (ctx.BattleManager == null)
                return;
            // Shame (pacify status): a shamed enemy can't defend the meter — the Denial it gains
            // is reduced by its Shame stacks. Counts toward the pacify threshold.
            int shame = ctx.CasterStatusEffects?.GetStacks(StatusEffectType.Shame) ?? 0;
            if (shame > 0)
                amount = Mathf.Max(0, amount - shame);
            ctx.BattleManager.GainDenial(amount);
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
        #endregion
    }
}
