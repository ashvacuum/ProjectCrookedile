namespace Crookedile.Gameplay.Battle
{
    using System;
    using System.Collections.Generic;
    using Crookedile.Core;
    using Crookedile.Data;
    using Crookedile.Data.Cards;
    using Crookedile.Utilities;
    using Sirenix.OdinInspector;
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
    // Live description shown above each effect's fields in the inspector (card/enemy authoring),
    // so designers see what an effect does without reading code. Inherited by every concrete effect.
    [InfoBox(
        "@$value == null ? \"(no effect chosen)\" : $value.EditorSafeDescription()",
        InfoMessageType.None
    )]
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

        /// <summary>
        /// Guarded wrapper around <see cref="GetDescription"/> used by the inspector InfoBox. A single
        /// effect with a latent null-deref in its description must not throw and break the entire
        /// CardData / EnemyMoveData inspector — it shows an error marker on that one row instead.
        /// Referenced by reflection from the <c>[InfoBox]</c> attribute, so it must stay public.
        /// </summary>
        public string EditorSafeDescription()
        {
            try
            {
                return GetDescription();
            }
            catch (Exception e)
            {
                return $"(description error: {e.GetType().Name})";
            }
        }

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

        /// <summary>
        /// Resolves a scaled effect amount:
        ///   base (fixed or context-sourced; Confused override wins)
        ///   × per-X context value (e.g. HostileEnemyCount = "per hostile enemy")
        ///   × flat multiplier.
        /// Sentinels for backward compatibility with assets authored before scaling existed:
        /// a per-X of None or FixedAmount means "no scaling" (missing enum fields deserialize
        /// to FixedAmount), and a multiplier ≤ 0 is treated as 1 (missing floats deserialize to 0).
        /// </summary>
        protected static int ResolveScaledAmount(
            EffectExecutionContext ctx,
            int? amountOverride,
            int fixedAmount,
            EffectContextValue source,
            EffectContextValue perXSource,
            float multiplier
        )
        {
            int baseAmount = ResolveAmount(ctx, amountOverride, fixedAmount, source);
            int scale = IsScaling(perXSource) ? ctx.GetValue(perXSource) : 1;
            float mult = multiplier <= 0f ? 1f : multiplier;
            return Mathf.RoundToInt(baseAmount * scale * mult);
        }

        /// <summary>Human-readable scaled amount, e.g. "5 per HostileEnemyCount ×2".</summary>
        protected static string DescribeScaledAmount(
            int fixedAmount,
            EffectContextValue source,
            EffectContextValue perXSource,
            float multiplier
        )
        {
            string text = DescribeAmount(fixedAmount, source);
            if (IsScaling(perXSource))
                text = $"{text} per {perXSource}";
            if (multiplier > 0f && !Mathf.Approximately(multiplier, 1f))
                text = $"{text} ×{multiplier:0.##}";
            return text;
        }

        private static bool IsScaling(EffectContextValue perXSource) =>
            perXSource != EffectContextValue.None && perXSource != EffectContextValue.FixedAmount;

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
                // Test-harness fallback (no ledger): nothing absorbs, so the full amount applies.
                EventBus.Publish(
                    new DamageDealtEvent
                    {
                        Amount = amount,
                        Absorbed = 0,
                        Applied = amount,
                        IsToPlayer = toPlayer,
                        AttackerName = attackerName,
                        SourceEnemyIndex = sourceEnemyIndex,
                        TargetEnemyIndex = targetEnemyIndex,
                    }
                );
            }
        }

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
            // Shame (pacify status): a shamed enemy can't defend the meter — its own statuses fold
            // the Denial it gains (ModifyDenialGained). Counts toward the pacify threshold.
            if (ctx.CasterStatusEffects != null)
                amount = ctx.CasterStatusEffects.ModifyDenialGained(amount);
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
        ///   <item><see cref="CardSelectionMode.ThisCard"/> — resolves to <paramref name="thisCard"/> (ctx.OwnerCard), ignoring the pool</item>
        /// </list>
        /// </summary>
        protected static void ResolveCardSelection(
            IReadOnlyList<CardData> pool,
            CardSelectionMode mode,
            CardType filterType,
            string choiceTitle,
            int count,
            Action<List<CardData>> onResolved,
            bool allowFewer = false,
            CardData thisCard = null
        )
        {
            // ThisCard: the card the effect is printed on — bypasses the pool entirely (the
            // played card sits in the discard, not the hand, at resolve time).
            if (mode == CardSelectionMode.ThisCard)
            {
                var self = new List<CardData>();
                if (thisCard != null)
                    self.Add(thisCard);
                onResolved?.Invoke(self);
                return;
            }

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
                        AllowFewer = allowFewer,
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
