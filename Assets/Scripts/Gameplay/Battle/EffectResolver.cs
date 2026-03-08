using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Resolves card effects during battle.
    /// Applies damage, healing, status effects, and other battle effects to combatants.
    /// </summary>
    [Debuggable("EffectResolver", LogLevel.Info)]
    public class EffectResolver
    {
        private BattleStats _playerStats;
        private BattleStats _opponentStats;           // focused enemy (for single-target effects)
        private DeckManager _playerDeck;
        // Note: enemies have no deck — _opponentDeck was removed when the system switched
        // from player-vs-player to player-vs-scripted-enemy.
        private IReadOnlyList<EnemyController> _allEnemies; // all enemies — used for multi-target effects
        private StatusEffectManager _playerStatusEffects;
        private StatusEffectManager _opponentStatusEffects; // focused enemy's status mgr
        private string _attackerName       = "Player";
        private int    _attackerEnemyIndex = -1;            // -1 = player is the attacker

        // All battle events are published via EventBus — no C# event wiring required.
        // See BattleEvents.cs for the full event catalogue.

        public EffectResolver(BattleStats playerStats, BattleStats opponentStats,
                              DeckManager playerDeck,
                              IReadOnlyList<EnemyController> allEnemies = null)
        {
            _playerStats           = playerStats;
            _opponentStats         = opponentStats;
            _playerDeck            = playerDeck;
            _allEnemies            = allEnemies;
            _playerStatusEffects   = new StatusEffectManager("Player");
            _opponentStatusEffects = new StatusEffectManager("Opponent");
        }

        public StatusEffectManager PlayerStatusEffects   => _playerStatusEffects;
        public StatusEffectManager OpponentStatusEffects => _opponentStatusEffects;

        /// <summary>
        /// Retargets the resolver to a different enemy.
        /// Call this before resolving any effect that should apply to a specific enemy
        /// (e.g. before each enemy acts during the opponent turn, or when the player
        /// changes their focused target).
        /// Both stats AND status effects must be swapped together.
        /// </summary>
        public void SetFocusedOpponent(BattleStats stats, StatusEffectManager statusEffects,
                                        int enemyIndex = -1, string enemyName = "Opponent")
        {
            _opponentStats         = stats;
            _opponentStatusEffects = statusEffects;
            _attackerEnemyIndex    = enemyIndex;
            _attackerName          = enemyName;
        }

        #region New BattleEffect Coordinator (Phase 2 — parallel with legacy)

        /// <summary>
        /// Creates an <see cref="EffectExecutionContext"/> from the resolver's current state.
        /// Used by the new <see cref="BattleEffect"/>-based resolution path.
        /// </summary>
        /// <param name="isPlayerCard">
        /// True for player cards (caster = player, target = focused enemy).
        /// False for enemy moves (caster = focused enemy, target = player, deck = null).
        /// </param>
        public EffectExecutionContext CreateContext(bool isPlayerCard)
        {
            BattleStats         caster         = isPlayerCard ? _playerStats          : _opponentStats;
            BattleStats         target         = isPlayerCard ? _opponentStats         : _playerStats;
            DeckManager         deck           = isPlayerCard ? _playerDeck            : null;
            StatusEffectManager casterStatus   = isPlayerCard ? _playerStatusEffects   : _opponentStatusEffects;
            StatusEffectManager targetStatus   = isPlayerCard ? _opponentStatusEffects : _playerStatusEffects;

            return new EffectExecutionContext(
                caster:               caster,
                target:               target,
                playerStats:          _playerStats,
                isPlayerCard:         isPlayerCard,
                deck:                 deck,
                allEnemies:           _allEnemies,
                casterStatusEffects:  casterStatus,
                targetStatusEffects:  targetStatus,
                playerStatusEffects:  _playerStatusEffects,
                attackerName:         isPlayerCard ? "Player" : _attackerName,
                attackerEnemyIndex:   isPlayerCard ? -1 : _attackerEnemyIndex);
        }

        /// <summary>
        /// New coordinator — resolves a card's <see cref="BattleEffect"/> list using the
        /// polymorphic self-executing hierarchy. Falls back to the legacy path when
        /// <see cref="CardData.NewEffects"/> is empty (migration window).
        ///
        /// Triggered effects still use <see cref="CardEffect"/> (Phase 2 migration).
        /// </summary>
        public EffectExecutionContext ResolveCardEffectsNew(
            CardData card, bool isPlayerCard, int[] amountOverrides = null)
        {
            GameLogger.LogInfo<EffectResolver>(
                $"[New] Resolving effects for: {card.CardName} (Player: {isPlayerCard})");

            var execCtx = CreateContext(isPlayerCard);

            var effects = card.NewEffects;
            for (int j = 0; j < effects.Count; j++)
            {
                if (effects[j] == null) continue;
                int? overrideAmount = (amountOverrides != null && j < amountOverrides.Length)
                    ? (int?)amountOverrides[j] : null;
                effects[j].Execute(execCtx, overrideAmount);
            }

            // Triggered effects still use the legacy EffectContext path during the migration window.
            // Build a compatible EffectContext from the accumulated execution results.
            if (card.TriggeredEffects != null && card.TriggeredEffects.Count > 0)
            {
                var legacyCtx = new EffectContext
                {
                    Caster              = execCtx.Caster,
                    Target              = execCtx.Target,
                    LastDamageDealt     = execCtx.LastDamageDealt,
                    LastHealAmount      = execCtx.LastHealAmount,
                    LastComposureGained = execCtx.LastComposureGained,
                    LastTargetDied      = execCtx.LastTargetDied,
                    ShouldExhaust       = execCtx.ShouldExhaust,
                };
                ResolveTriggeredEffects(card.TriggeredEffects, legacyCtx, isPlayerCard);

                // Sync back any changes triggered effects made
                execCtx.ShouldExhaust = legacyCtx.ShouldExhaust;
            }

            return execCtx;
        }

        /// <summary>
        /// New coordinator — resolves an enemy move's <see cref="BattleEffect"/> list with a
        /// delay between effects, identical to <see cref="ResolveEnemyMoveEffects"/> but using
        /// self-executing effects. Falls back to the legacy path when <c>NewEffects</c> is empty.
        /// </summary>
        public IEnumerator ResolveEnemyMoveEffectsNew(EnemyMoveData move)
        {
            if (move == null) yield break;

            // Fall back to legacy path if the move hasn't been migrated yet
            if (move.NewEffects == null || move.NewEffects.Count == 0)
            {
                yield return ResolveEnemyMoveEffects(move);
                yield break;
            }

            GameLogger.LogInfo<EffectResolver>($"[New] Resolving enemy move: {move.MoveName}");

            var execCtx = CreateContext(isPlayerCard: false);
            var effects  = move.NewEffects;
            for (int i = 0; i < effects.Count; i++)
            {
                effects[i]?.Execute(execCtx);
                if (i < effects.Count - 1)
                    yield return new WaitForSeconds(EffectStepDelay);
            }
        }

        #endregion

        #region Effect Resolution

        /// <summary>
        /// Resolves all effects from a played card, then fires any triggered effects
        /// whose trigger/condition match what happened during base-effect resolution.
        /// </summary>
        /// <param name="card">The card being played</param>
        /// <param name="isPlayerCard">Is this card played by the player?</param>
        /// <param name="amountOverrides">
        /// Optional per-effect amount overrides (one int per CardEffect, indexed by position).
        /// Used by the Confused status effect to randomise card values 0–3 this turn.
        /// Pass null (default) for normal resolution.
        /// </param>
        public EffectContext ResolveCardEffects(CardData card, bool isPlayerCard, int[] amountOverrides = null)
        {
            GameLogger.LogInfo<EffectResolver>($"Resolving effects for: {card.CardName} (Player: {isPlayerCard})");

            // Create a fresh context for this card's resolution
            var ctx = new EffectContext
            {
                Caster = isPlayerCard ? _playerStats : _opponentStats,
                Target = isPlayerCard ? _opponentStats : _playerStats
            };

            for (int j = 0; j < card.Effects.Count; j++)
            {
                int? overrideAmount = (amountOverrides != null && j < amountOverrides.Length)
                    ? (int?)amountOverrides[j] : null;
                ResolveBattleEffect(card.Effects[j], isPlayerCard, ctx, overrideAmount);
            }

            // Fire any triggered effects that reacted to what happened above
            if (card.TriggeredEffects != null && card.TriggeredEffects.Count > 0)
                ResolveTriggeredEffects(card.TriggeredEffects, ctx, isPlayerCard);

            return ctx;
        }

        /// <summary>
        /// Evaluates each TriggeredEffect in the list against the accumulated EffectContext,
        /// and resolves those whose trigger occurred and whose condition is satisfied.
        /// Called automatically at the end of <see cref="ResolveCardEffects"/> when the card
        /// has triggered effects defined.
        /// </summary>
        private void ResolveTriggeredEffects(
            IReadOnlyList<TriggeredEffect> triggered, EffectContext ctx, bool isPlayerCard)
        {
            StatusEffectManager targetStatusMgr =
                isPlayerCard ? _opponentStatusEffects : _playerStatusEffects;

            foreach (var te in triggered)
            {
                if (!TriggerOccurred(te.Trigger, ctx))            continue;
                if (!EvaluateCondition(te.Condition, te.ConditionThreshold, ctx, targetStatusMgr)) continue;

                GameLogger.LogInfo<EffectResolver>($"Triggered effect fired: '{te.Name}'");
                // Share the same ctx so the response can also read context values
                ResolveBattleEffect(te.ResponseEffect, isPlayerCard, ctx);
            }
        }

        /// <summary>Returns true if the given trigger event occurred during the card's resolution.</summary>
        private static bool TriggerOccurred(EffectTrigger trigger, EffectContext ctx)
        {
            switch (trigger)
            {
                case EffectTrigger.OnDamageDealt:     return ctx.LastDamageDealt > 0;
                case EffectTrigger.OnKill:            return ctx.LastTargetDied;
                case EffectTrigger.OnComposureGained: return ctx.LastComposureGained > 0;
                case EffectTrigger.OnHeal:            return ctx.LastHealAmount > 0;
                case EffectTrigger.OnStatusApplied:   return true; // conservative: trust the card authored this correctly
                case EffectTrigger.OnDamageTaken:
                    // OnDamageTaken cannot fire during card resolution — the player never takes
                    // damage while playing their own cards. Use PassiveTrigger.OnDamageTaken in
                    // an OriginPassive (EventBus-based) for reactive damage responses instead.
                    GameLogger.LogWarning<EffectResolver>(
                        "EffectTrigger.OnDamageTaken is reserved and will never fire during card " +
                        "resolution. Use PassiveTrigger.OnDamageTaken in an OriginPassive instead.");
                    return false;
                default: return false;
            }
        }

        /// <summary>
        /// Returns true if the extra condition on the triggered effect is satisfied.
        /// </summary>
        private static bool EvaluateCondition(
            EffectCondition condition, int threshold, EffectContext ctx,
            StatusEffectManager targetStatusMgr) => condition switch
        {
            EffectCondition.Always                 => true,
            EffectCondition.IfDamageDealt          => ctx.LastDamageDealt > 0,
            EffectCondition.IfTargetDied           => ctx.LastTargetDied,
            EffectCondition.IfTargetHasBuff        => targetStatusMgr?.HasAnyBuff()   ?? false,
            EffectCondition.IfTargetHasDebuff      => targetStatusMgr?.HasAnyDebuff() ?? false,
            EffectCondition.IfAmountAboveThreshold =>
                ctx.LastDamageDealt > threshold || ctx.LastHealAmount > threshold || ctx.LastComposureGained > threshold,
            _                                      => true
        };

        /// <summary>
        /// Delay (in seconds) yielded between each effect in a multi-effect enemy move,
        /// so that floating damage texts appear sequentially rather than all at once.
        /// Configurable from BattleManager (or set directly).
        /// </summary>
        public float EffectStepDelay = 0.15f;

        /// <summary>
        /// Resolves all effects from a scripted enemy move sequentially, yielding
        /// <see cref="EffectStepDelay"/> seconds between each effect so that visual
        /// feedback (floating numbers, VFX) appears one at a time.
        ///
        /// Returns an IEnumerator — must be run via MonoBehaviour.StartCoroutine.
        /// Uses the same CardEffect pipeline as player cards with isPlayerCard=false
        /// (enemy is caster, player is the default target).
        /// CardManipulation effects are silently skipped — enemies have no deck.
        /// </summary>
        public IEnumerator ResolveEnemyMoveEffects(EnemyMoveData move)
        {
            if (move == null) yield break;

            GameLogger.LogInfo<EffectResolver>($"Resolving enemy move: {move.MoveName}");

            var effects = move.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                ResolveBattleEffect(effects[i], isPlayerCard: false);
                // Pause between effects (not after the last one)
                if (i < effects.Count - 1)
                    yield return new WaitForSeconds(EffectStepDelay);
            }
        }

        /// <summary>
        /// Resolves a single battle effect.
        /// Resource and CardManipulation effects are always caster-scoped and applied once.
        /// Damage and StatusEffect effects are target-scoped and applied to every pair
        /// returned by ResolveTargetPairs (supports multi-target TargetTypes).
        /// </summary>
        /// <param name="ctx">
        /// Optional EffectContext from the enclosing card resolution. When provided,
        /// damage/heal/composure amounts are written to it so triggered effects can react.
        /// Pass null for enemy move effects (no triggered-effect evaluation needed).
        /// </param>
        /// <param name="amountOverride">
        /// Optional override for the effect's amount. Used by Confused to randomise values.
        /// </param>
        private void ResolveBattleEffect(CardEffect effect, bool isPlayerCard, EffectContext ctx = null, int? amountOverride = null)
        {
            BattleStats         casterStats         = isPlayerCard ? _playerStats          : _opponentStats;
            BattleStats         targetStats         = isPlayerCard ? _opponentStats         : _playerStats;
            DeckManager         casterDeck          = isPlayerCard ? _playerDeck            : null;
            StatusEffectManager casterStatusEffects = isPlayerCard ? _playerStatusEffects   : _opponentStatusEffects;
            StatusEffectManager targetStatusEffects = isPlayerCard ? _opponentStatusEffects : _playerStatusEffects;

            // Resource and CardManipulation are always caster-scoped — apply once and return.
            // targetStats is forwarded so that opponent-scoped resource effects (ReduceHostility,
            // RaiseTargetHostility) hit the right BattleStats rather than hardcoding _opponentStats.
            if (effect.Category == EffectCategory.Resource)
            {
                ResolveResourceEffect(effect, casterStats, targetStats, ctx, amountOverride);
                EventBus.Publish(new EffectAppliedEvent { Effect = effect, IsPlayer = isPlayerCard });
                return;
            }
            if (effect.Category == EffectCategory.CardManipulation)
            {
                // Enemies have no deck (casterDeck == null) — skip silently.
                if (casterDeck != null)
                    ResolveCardManipulationEffect(effect, casterDeck, isPlayerCard, ctx);
                EventBus.Publish(new EffectAppliedEvent { Effect = effect, IsPlayer = isPlayerCard });
                return;
            }

            // Damage and StatusEffect are target-scoped — resolve the full target list and iterate
            var targets = ResolveTargetPairs(effect.Target, isPlayerCard,
                casterStats, casterStatusEffects, targetStats, targetStatusEffects);

            foreach (var (effectTarget, effectTargetStatusMgr) in targets)
            {
                switch (effect.Category)
                {
                    case EffectCategory.Damage:
                        ResolveDamageEffect(effect, effectTarget, casterStats, ctx, amountOverride);
                        break;

                    case EffectCategory.StatusEffect:
                        effectTargetStatusMgr?.ApplyStatusEffect(
                            effect.StatusEffectType, effect.StatusStacks, effect.StatusDuration);
                        GameLogger.LogInfo<EffectResolver>(
                            $"Applied {effect.StatusStacks} {effect.StatusEffectType} ({effect.StatusDuration})");
                        EventBus.Publish(new StatusEffectAppliedEvent
                        {
                            StatusType = effect.StatusEffectType,
                            Stacks     = effect.StatusStacks,
                            IsToPlayer = effectTarget == _playerStats,
                        });
                        break;
                }
                EventBus.Publish(new EffectAppliedEvent { Effect = effect, IsPlayer = isPlayerCard });
            }
        }

        /// <summary>
        /// Resolves a TargetType into a list of (BattleStats, StatusEffectManager) pairs.
        /// Single-target types return 1 element; multi-target types return N (one per living combatant).
        /// </summary>
        private List<(BattleStats stats, StatusEffectManager statusMgr)> ResolveTargetPairs(
            TargetType targetType, bool isPlayerCard,
            BattleStats casterStats,   StatusEffectManager casterStatusMgr,
            BattleStats targetStats,   StatusEffectManager targetStatusMgr)
        {
            var pairs = new List<(BattleStats, StatusEffectManager)>();

            switch (targetType)
            {
                case TargetType.Self:
                    pairs.Add((casterStats, casterStatusMgr));
                    break;

                case TargetType.Opponent:
                    pairs.Add((targetStats, targetStatusMgr));
                    break;

                case TargetType.Random:
                    if (isPlayerCard)
                    {
                        // Player cards: pick a random living enemy — never the player themselves.
                        if (_allEnemies != null)
                        {
                            var living = new List<(BattleStats, StatusEffectManager)>();
                            foreach (var e in _allEnemies)
                                if (!e.IsDefeated) living.Add((e.Stats, e.StatusEffects));
                            if (living.Count > 0)
                                pairs.Add(living[UnityEngine.Random.Range(0, living.Count)]);
                        }
                    }
                    else
                    {
                        // Enemy cards: target the player.
                        pairs.Add((targetStats, targetStatusMgr));
                    }
                    break;

                case TargetType.All:
                    // Player + every living enemy
                    pairs.Add((_playerStats, _playerStatusEffects));
                    if (_allEnemies != null)
                        foreach (var e in _allEnemies)
                            if (!e.IsDefeated) pairs.Add((e.Stats, e.StatusEffects));
                    break;

                case TargetType.AllOpponents:
                    if (isPlayerCard)
                    {
                        // Hit all living enemies
                        if (_allEnemies != null)
                            foreach (var e in _allEnemies)
                                if (!e.IsDefeated) pairs.Add((e.Stats, e.StatusEffects));
                    }
                    else
                    {
                        // Enemy card — only one player to target
                        pairs.Add((_playerStats, _playerStatusEffects));
                    }
                    break;

                case TargetType.AllAllies:
                    if (!isPlayerCard)
                    {
                        // Buff all living enemies
                        if (_allEnemies != null)
                            foreach (var e in _allEnemies)
                                if (!e.IsDefeated) pairs.Add((e.Stats, e.StatusEffects));
                    }
                    else
                    {
                        // Player has no other allies — same as Self
                        pairs.Add((_playerStats, _playerStatusEffects));
                    }
                    break;

                default:
                    GameLogger.LogWarning<EffectResolver>($"Unhandled TargetType {targetType} — falling back to Opponent");
                    pairs.Add((targetStats, targetStatusMgr));
                    break;
            }

            return pairs;
        }

        private void ResolveDamageEffect(CardEffect effect, BattleStats target, BattleStats attacker, EffectContext ctx = null, int? amountOverride = null)
        {
            switch (effect.DamageType)
            {
                case DamageType.FixedDamage:
                    int baseAmount = amountOverride ?? effect.GetEffectiveAmount(ctx);
                    ApplyResolveDamage(target, attacker, baseAmount, ctx);
                    break;

                case DamageType.RandomDamage:
                    // Confused randomises effect amounts but not random-damage min/max ranges
                    ApplyRandomDamage(target, attacker, effect.RandomDamageMin, effect.RandomDamageMax, ctx);
                    break;

                case DamageType.DamageEqualToComposure:
                    ApplyResolveDamageEqualToComposure(target, attacker, ctx);
                    break;
            }
        }

        private void ResolveResourceEffect(CardEffect effect, BattleStats caster, BattleStats target,
                                            EffectContext ctx = null, int? amountOverride = null)
        {
            // Local helper: returns the override amount when Confused is active, otherwise the normal effect amount
            int GetAmount() => amountOverride ?? effect.GetEffectiveAmount(ctx);

            switch (effect.ResourceType)
            {
                case ResourceEffectType.GainComposure:
                    ApplyGainComposure(caster, GetAmount(), ctx);
                    break;

                case ResourceEffectType.LoseComposure:
                    ApplyLoseComposure(caster, GetAmount());
                    break;

                case ResourceEffectType.ConsumeAllComposure:
                    ApplyConsumeAllComposure(caster);
                    break;

                case ResourceEffectType.ComposureEqualToHostility:
                    ApplyComposureEqualToHostility(caster, ctx);
                    break;

                // Opponent-scoped: reduce/raise the *target's* hostility, not the caster's.
                // Uses the resolved target (focused enemy for player cards, player for enemy cards)
                // rather than _opponentStats so multi-enemy and enemy-card scenarios are correct.
                case ResourceEffectType.ReduceHostility:
                    ApplyReduceHostility(target, GetAmount());
                    break;

                case ResourceEffectType.RaiseTargetHostility:
                    ApplyRaiseTargetHostility(target, GetAmount());
                    break;

                case ResourceEffectType.GainActionPoints:
                    ApplyGainActionPoints(caster, GetAmount());
                    break;

                case ResourceEffectType.GainActionPointsNextTurn:
                    ApplyGainActionPointsNextTurn(caster, GetAmount());
                    break;

                case ResourceEffectType.HealResolve:
                    ApplyResolveHeal(caster, GetAmount(), ctx);
                    break;
            }
        }

        private void ResolveCardManipulationEffect(CardEffect effect, DeckManager deck,
                                                    bool isPlayerCard = false, EffectContext ctx = null)
        {
            switch (effect.CardManipulationType)
            {
                case CardManipulationType.DrawCards:
                    ApplyDrawCards(deck, effect.CardAmount);
                    break;

                case CardManipulationType.ChooseFromDiscardToHand:
                    ApplyChooseFromDiscardToHand(deck, effect.CardAmount);
                    break;

                case CardManipulationType.ChooseFromDiscardToDeck:
                    ApplyChooseFromDiscardToDeck(deck, effect.CardAmount);
                    break;

                case CardManipulationType.DiscardCards:
                    ApplyDiscardCards(deck, effect.CardAmount);
                    break;

                case CardManipulationType.DiscardHand:
                    ApplyDiscardHand(deck, effect);
                    break;

                case CardManipulationType.ExhaustThisCard:
                    ApplyExhaustCard(ctx);
                    break;

                case CardManipulationType.ChooseToDiscard:
                    ApplyChooseToDiscard(deck, effect.CardAmount, effect);
                    break;

                case CardManipulationType.AddCardToDeck:
                    ApplyAddCardToDeck(deck, effect.CardToAdd, effect.CardAmount);
                    break;

                case CardManipulationType.AddCardToHand:
                    ApplyAddCardToHand(deck, effect.CardToAdd, effect.CardAmount);
                    break;

                case CardManipulationType.UpgradeCardThisBattle:
                    ApplyUpgradeCardThisBattle(deck, effect);
                    break;

                case CardManipulationType.UpgradeAllCardsInHand:
                    ApplyUpgradeAllCardsInHand(deck);
                    break;

                case CardManipulationType.MakeCardRetain:
                    ApplyMakeCardRetain(deck, effect);
                    break;

                case CardManipulationType.MakeAllCardsRetain:
                    ApplyMakeAllCardsRetain(deck);
                    break;

                case CardManipulationType.ReduceCardCost:
                    ApplyReduceCardCost(deck, effect.CostReduction, effect);
                    break;

                case CardManipulationType.MakeCardFree:
                    ApplyMakeCardFree(deck, effect);
                    break;

                case CardManipulationType.ChanceRoll:
                    ApplyChanceRoll(effect, isPlayerCard, ctx);
                    break;
            }
        }

        private void ApplyChanceRoll(CardEffect effect, bool isPlayerCard, EffectContext ctx)
        {
            if (!RandomHelper.Chance(effect.ChancePercent / 100f))
            {
                GameLogger.LogInfo<EffectResolver>($"Chance roll failed ({effect.ChancePercent}%)");
                return;
            }

            GameLogger.LogInfo<EffectResolver>(
                $"Chance roll succeeded ({effect.ChancePercent}%) — resolving {effect.ChanceEffects.Count} effect(s)");

            foreach (var childEffect in effect.ChanceEffects)
            {
                if (childEffect != null)
                    ResolveBattleEffect(childEffect, isPlayerCard, ctx);
            }
        }

        #endregion

        #region Core Damage & Healing

        /// <summary>
        /// Shared damage pipeline: applies attacker/target status modifiers and the hostility
        /// multiplier, calls DamageResolve, publishes DamageDealtEvent, and writes to ctx.
        /// All three damage-type methods (fixed, random, composure-equal) funnel through here
        /// so that modifier logic lives in exactly one place.
        /// </summary>
        private void ApplyDamagePipeline(BattleStats target, BattleStats attacker, int rawDamage, EffectContext ctx)
        {
            StatusEffectManager attackerStatusMgr = GetStatusEffectManager(attacker);
            StatusEffectManager targetStatusMgr   = GetStatusEffectManager(target);

            // Apply attacker's damage modifiers (Strength, Weakened, Exposed)
            int modifiedDamage = attackerStatusMgr.ModifyDamageDealt(rawDamage);

            // Apply target's damage taken modifiers (Vulnerable, Plated, Intangible, Thorns)
            modifiedDamage = targetStatusMgr.ModifyDamageTaken(modifiedDamage, attacker);

            // Hostile enemies (positive hostility) deal amplified damage; neutral and receptive don't
            if (attacker != _playerStats && attacker.CurrentHostility > 0)
            {
                float hostilityMult = Mathf.Max(0.1f, attacker.HostilityDamageMultiplier);
                modifiedDamage      = Mathf.RoundToInt(modifiedDamage * hostilityMult);
            }

            // Apply damage — target's Composure shield absorbs first, then Resolve takes the rest
            int actualDamage = target.DamageResolve(modifiedDamage);
            GameLogger.LogInfo<EffectResolver>(
                $"Dealt {actualDamage} damage (raw: {rawDamage}, modified: {modifiedDamage}, " +
                $"HostilityMult: {(attacker != _playerStats && attacker.CurrentHostility > 0 ? attacker.HostilityDamageMultiplier.ToString("F2") : "1.00")})");

            if (actualDamage > 0)
            {
                bool isPlayerAttacking = attacker == _playerStats;
                EventBus.Publish(new DamageDealtEvent
                {
                    Amount           = actualDamage,
                    IsToPlayer       = target == _playerStats,
                    AttackerName     = isPlayerAttacking ? "Player" : _attackerName,
                    SourceEnemyIndex = isPlayerAttacking ? -1 : _attackerEnemyIndex,
                    TargetEnemyIndex = isPlayerAttacking ? _attackerEnemyIndex : -1,
                });
            }

            // Accumulate into context so triggered effects can react (e.g. lifesteal)
            if (ctx != null)
            {
                ctx.LastDamageDealt += actualDamage;
                if (target.CurrentResolve <= 0) ctx.LastTargetDied = true;
            }
        }

        private void ApplyResolveDamage(BattleStats target, BattleStats attacker, int baseDamage, EffectContext ctx = null)
        {
            ApplyDamagePipeline(target, attacker, baseDamage, ctx);
        }

        private void ApplyResolveHeal(BattleStats target, int amount, EffectContext ctx = null)
        {
            int actualHealing = target.RestoreResolve(amount);
            GameLogger.LogInfo<EffectResolver>($"Restored {actualHealing} Resolve");

            if (actualHealing > 0)
                EventBus.Publish(new HealingAppliedEvent { Amount = actualHealing, IsToPlayer = target == _playerStats });

            if (ctx != null) ctx.LastHealAmount += actualHealing;
        }

        private void ApplyRandomDamage(BattleStats target, BattleStats attacker, int minDamage, int maxDamage, EffectContext ctx = null)
        {
            int randomDamage = RandomHelper.Range(minDamage, maxDamage + 1);
            GameLogger.LogInfo<EffectResolver>($"Random damage roll: {randomDamage} (range {minDamage}–{maxDamage})");
            ApplyDamagePipeline(target, attacker, randomDamage, ctx);
        }

        #endregion

        #region Composure

        private void ApplyGainComposure(BattleStats target, int amount, EffectContext ctx = null)
        {
            // Apply Composure gain modifiers (Dexterity, Frail)
            StatusEffectManager targetStatusMgr = GetStatusEffectManager(target);
            int modifiedAmount = targetStatusMgr.ModifyComposureGained(amount);

            target.GainComposure(modifiedAmount);
            GameLogger.LogInfo<EffectResolver>($"Gained {modifiedAmount} Composure (base: {amount})");

            if (ctx != null) ctx.LastComposureGained += modifiedAmount;
        }

        private void ApplyLoseComposure(BattleStats target, int amount)
        {
            int actualLoss = target.LoseComposure(amount);
            GameLogger.LogInfo<EffectResolver>($"Lost {actualLoss} Composure");
        }

        private void ApplyResolveDamageEqualToComposure(BattleStats target, BattleStats attacker, EffectContext ctx = null)
        {
            int composure = attacker.CurrentComposure;
            GameLogger.LogInfo<EffectResolver>($"Damage-equal-to-Composure: raw value = {composure}");
            ApplyDamagePipeline(target, attacker, composure, ctx);
        }

        private void ApplyConsumeAllComposure(BattleStats caster)
        {
            int consumed = caster.ConsumeAllComposure();
            GameLogger.LogInfo<EffectResolver>($"Consumed {consumed} Composure");
        }

        #endregion

        #region Hostility

        private void ApplyReduceHostility(BattleStats target, int amount)
        {
            int actualReduction = target.ReduceHostility(amount);
            GameLogger.LogInfo<EffectResolver>($"Reduced {actualReduction} Hostility");
        }

        private void ApplyRaiseTargetHostility(BattleStats target, int amount)
        {
            target.GainHostility(amount);
            GameLogger.LogInfo<EffectResolver>($"Raised target Hostility by {amount} (now {target.CurrentHostility})");
        }

        private void ApplyComposureEqualToHostility(BattleStats caster, EffectContext ctx = null)
        {
            int hostility = caster.CurrentHostility;
            // Route through ApplyGainComposure so Dexterity/Frail modifiers are respected
            ApplyGainComposure(caster, hostility, ctx);
            GameLogger.LogInfo<EffectResolver>($"Gained Composure equal to Hostility ({hostility})");
        }

        #endregion

        #region Action Points

        private void ApplyGainActionPoints(BattleStats target, int amount)
        {
            target.GainActionPoints(amount);
            GameLogger.LogInfo<EffectResolver>($"Gained {amount} Action Points");
        }

        private void ApplyGainActionPointsNextTurn(BattleStats target, int amount)
        {
            target.GainActionPointsNextTurn(amount);
            GameLogger.LogInfo<EffectResolver>($"Will gain {amount} AP next turn");
        }

        #endregion

        #region Card Draw/Discard Effects

        private void ApplyDrawCards(DeckManager deck, int amount)
        {
            int cardsDrawn = deck.DrawCards(amount);
            GameLogger.LogInfo<EffectResolver>($"Drew {cardsDrawn} cards");
        }

        private void ApplyDiscardCards(DeckManager deck, int amount)
        {
            // Randomly discard cards from hand
            int cardsDiscarded = 0;
            for (int i = 0; i < amount && deck.HandCount > 0; i++)
            {
                int randomIndex = RandomHelper.Range(0, deck.HandCount);
                if (deck.DiscardCard(deck.Hand[randomIndex]))
                {
                    cardsDiscarded++;
                }
            }
            GameLogger.LogInfo<EffectResolver>($"Discarded {cardsDiscarded} cards");
        }

        private void ApplyExhaustCard(EffectContext ctx)
        {
            // Signal BattleManager to move the played card from discard → exhaust
            // after all effects on this card have resolved.
            if (ctx != null) ctx.ShouldExhaust = true;
            GameLogger.LogInfo<EffectResolver>("Card flagged for exhaust after play");
        }

        private void ApplyDiscardHand(DeckManager deck, CardEffect effect)
        {
            List<CardData> discarded = deck.DiscardHand();
            GameLogger.LogInfo<EffectResolver>($"Discarded entire hand ({discarded.Count} cards)");

            int drawAmount = effect.DiscardDrawAmount;
            if (drawAmount <= 0 || discarded.Count == 0) return;

            int count = Mathf.Min(drawAmount, discarded.Count);
            string title = count == 1 ? "Reclaim 1 card" : $"Reclaim {count} cards";
            EventBus.Publish(new CardChoiceRequestedEvent
            {
                Title         = title,
                Choices       = discarded,
                RequiredCount = count,
                OnConfirmed   = chosen =>
                {
                    foreach (var card in chosen)
                        deck.MoveFromDiscardToHand(card);
                }
            });
        }

        private void ApplyChooseToDiscard(DeckManager deck, int amount, CardEffect effect)
        {
            if (deck.HandCount == 0)
            {
                GameLogger.LogInfo<EffectResolver>("ChooseToDiscard: hand is empty — no-op");
                return;
            }
            int count = Mathf.Min(amount, deck.HandCount);
            string title = count == 1 ? "Choose a card to Discard" : $"Choose {count} cards to Discard";
            ResolveCardSelection(deck.Hand, effect.SelectionMode, effect.FilterCardType,
                title, count,
                chosen => { foreach (var card in chosen) deck.DiscardCard(card); });
        }

        /// <summary>
        /// Central card-selection resolver for single-pool choice-based effects.
        /// Supports three modes:
        /// <list type="bullet">
        ///   <item><see cref="CardSelectionMode.PlayerChoice"/> — opens <see cref="CardChoicePanel"/> via EventBus</item>
        ///   <item><see cref="CardSelectionMode.RandomAny"/> — picks randomly from the full pool</item>
        ///   <item><see cref="CardSelectionMode.RandomByType"/> — filters by <paramref name="filterType"/> then picks randomly</item>
        /// </list>
        /// </summary>
        private void ResolveCardSelection(
            IReadOnlyList<CardData> pool,
            CardSelectionMode       mode,
            CardType                filterType,
            string                  choiceTitle,
            int                     count,
            Action<List<CardData>>  onResolved)
        {
            // Build the candidate list — apply type filter for RandomByType
            var candidates = new List<CardData>();
            foreach (var c in pool)
            {
                if (c == null) continue;
                if (mode == CardSelectionMode.RandomByType && c.CardType != filterType) continue;
                candidates.Add(c);
            }

            if (candidates.Count == 0)
            {
                GameLogger.LogInfo<EffectResolver>("ResolveCardSelection: no candidates — no-op");
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
                    OnConfirmed   = onResolved
                });
            }
            else
            {
                // Random without replacement
                int pickCount = Mathf.Min(count, candidates.Count);
                var chosen    = new List<CardData>();
                var remaining = new List<CardData>(candidates);
                for (int i = 0; i < pickCount; i++)
                {
                    int idx = RandomHelper.Range(0, remaining.Count);
                    chosen.Add(remaining[idx]);
                    remaining.RemoveAt(idx);
                }
                GameLogger.LogInfo<EffectResolver>($"ResolveCardSelection: randomly picked {chosen.Count} card(s)");
                onResolved?.Invoke(chosen);
            }
        }

        private void ApplyChooseFromDiscardToHand(DeckManager deck, int amount)
        {
            if (deck.DiscardCount == 0)
            {
                GameLogger.LogInfo<EffectResolver>("ChooseFromDiscardToHand: discard is empty — no-op");
                return;
            }
            int count = Mathf.Min(amount, deck.DiscardCount);
            string title = count == 1 ? "Choose a card from Discard" : $"Choose {count} cards from Discard";
            EventBus.Publish(new CardChoiceRequestedEvent
            {
                Title         = title,
                Choices       = deck.DiscardPile,
                RequiredCount = count,
                OnConfirmed   = chosen => { foreach (var c in chosen) deck.MoveFromDiscardToHand(c); }
            });
        }

        private void ApplyChooseFromDiscardToDeck(DeckManager deck, int amount)
        {
            if (deck.DiscardCount == 0)
            {
                GameLogger.LogInfo<EffectResolver>("ChooseFromDiscardToDeck: discard is empty — no-op");
                return;
            }
            int count = Mathf.Min(amount, deck.DiscardCount);
            string title = count == 1 ? "Choose a card — return to Deck" : $"Choose {count} cards — return to Deck";
            EventBus.Publish(new CardChoiceRequestedEvent
            {
                Title         = title,
                Choices       = deck.DiscardPile,
                RequiredCount = count,
                OnConfirmed   = chosen => { foreach (var c in chosen) deck.MoveFromDiscardToDeck(c); }
            });
        }

        private void ApplyAddCardToDeck(DeckManager deck, CardData card, int amount)
        {
            if (card == null)
            {
                GameLogger.LogWarning<EffectResolver>("Cannot add card to deck: No card specified");
                return;
            }

            deck.AddCardsToDeck(card, amount);
            GameLogger.LogInfo<EffectResolver>($"Added {amount}x {card.CardName} to deck");
        }

        private void ApplyAddCardToHand(DeckManager deck, CardData card, int amount)
        {
            if (card == null)
            {
                GameLogger.LogWarning<EffectResolver>("Cannot add card to hand: No card specified");
                return;
            }

            int cardsAdded = deck.AddCardsToHand(card, amount);
            GameLogger.LogInfo<EffectResolver>($"Added {cardsAdded}/{amount}x {card.CardName} to hand");
        }

        private void ApplyUpgradeCardThisBattle(DeckManager deck, CardEffect effect)
        {
            var upgradeable = new List<CardData>();
            foreach (var c in deck.Hand)
                if (c != null && c.CanUpgrade) upgradeable.Add(c);

            if (upgradeable.Count == 0)
            {
                GameLogger.LogInfo<EffectResolver>("UpgradeCardThisBattle: no upgradeable cards in hand — no-op");
                return;
            }
            ResolveCardSelection(upgradeable, effect.SelectionMode, effect.FilterCardType,
                "Choose a card to Upgrade", 1,
                chosen =>
                {
                    if (chosen.Count == 0) return;
                    CardData upgraded = chosen[0].GetCurrentVersion();
                    deck.SwapCardInHand(chosen[0], upgraded);
                });
        }

        private void ApplyUpgradeAllCardsInHand(DeckManager deck)
        {
            // Collect (old, upgraded) pairs first — don't modify the list while iterating
            var pairs = new List<(CardData old, CardData upgraded)>();
            foreach (var card in deck.Hand)
                if (card != null && card.CanUpgrade)
                    pairs.Add((card, card.GetCurrentVersion()));

            foreach (var (old, upgraded) in pairs)
                deck.SwapCardInHand(old, upgraded);

            GameLogger.LogInfo<EffectResolver>($"Upgraded {pairs.Count} cards in hand");
        }

        private void ApplyMakeCardRetain(DeckManager deck, CardEffect effect)
        {
            if (deck.HandCount == 0)
            {
                GameLogger.LogInfo<EffectResolver>("MakeCardRetain: hand is empty — no-op");
                return;
            }
            ResolveCardSelection(deck.Hand, effect.SelectionMode, effect.FilterCardType,
                "Choose a card to Retain", 1,
                chosen => { if (chosen.Count > 0) deck.RetainCard(chosen[0]); });
        }

        private void ApplyMakeAllCardsRetain(DeckManager deck)
        {
            // Snapshot the hand before iterating so we're not affected by any future
            // structural changes to the collection.
            var snapshot = new List<CardData>(deck.Hand);
            int count = 0;
            foreach (var card in snapshot)
                if (card != null && deck.RetainCard(card)) count++;

            GameLogger.LogInfo<EffectResolver>($"Retained all {count} cards in hand");
        }

        private void ApplyReduceCardCost(DeckManager deck, int reduction, CardEffect effect)
        {
            if (deck.HandCount == 0)
            {
                GameLogger.LogInfo<EffectResolver>("ReduceCardCost: hand is empty — no-op");
                return;
            }
            ResolveCardSelection(deck.Hand, effect.SelectionMode, effect.FilterCardType,
                $"Choose a card — Reduce cost by {reduction}", 1,
                chosen => { if (chosen.Count > 0) deck.ApplyCostReduction(chosen[0], reduction); });
        }

        private void ApplyMakeCardFree(DeckManager deck, CardEffect effect)
        {
            if (deck.HandCount == 0)
            {
                GameLogger.LogInfo<EffectResolver>("MakeCardFree: hand is empty — no-op");
                return;
            }
            ResolveCardSelection(deck.Hand, effect.SelectionMode, effect.FilterCardType,
                "Choose a card — Make it Free", 1,
                chosen => { if (chosen.Count > 0) deck.MakeCardFreeThisBattle(chosen[0]); });
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets the StatusEffectManager for the given BattleStats.
        /// Searches the full enemy list so multi-target effects (AllOpponents, All)
        /// can look up the correct manager for each enemy, not just the focused one.
        /// </summary>
        private StatusEffectManager GetStatusEffectManager(BattleStats stats)
        {
            if (stats == _playerStats) return _playerStatusEffects;

            if (_allEnemies != null)
                foreach (var enemy in _allEnemies)
                    if (enemy.Stats == stats) return enemy.StatusEffects;

            GameLogger.LogWarning<EffectResolver>("GetStatusEffectManager: unknown BattleStats — returning null");
            return null;
        }

        #endregion
    }
}
