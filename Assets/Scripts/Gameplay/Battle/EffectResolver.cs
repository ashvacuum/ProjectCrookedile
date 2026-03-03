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
        public void ResolveCardEffects(CardData card, bool isPlayerCard, int[] amountOverrides = null)
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
        private static bool TriggerOccurred(EffectTrigger trigger, EffectContext ctx) => trigger switch
        {
            EffectTrigger.OnDamageDealt     => ctx.LastDamageDealt > 0,
            EffectTrigger.OnKill            => ctx.LastTargetDied,
            EffectTrigger.OnComposureGained => ctx.LastComposureGained > 0,
            EffectTrigger.OnHeal            => ctx.LastHealAmount > 0,
            EffectTrigger.OnStatusApplied   => true,   // conservative: trust the card authored this correctly
            EffectTrigger.OnDamageTaken     => false,  // reserved — not fired from player cards yet
            _                               => false
        };

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

            // Resource and CardManipulation are always caster-scoped — apply once and return
            if (effect.Category == EffectCategory.Resource)
            {
                ResolveResourceEffect(effect, casterStats, ctx, amountOverride);
                EventBus.Publish(new EffectAppliedEvent { Effect = effect, IsPlayer = isPlayerCard });
                return;
            }
            if (effect.Category == EffectCategory.CardManipulation)
            {
                // Enemies have no deck (casterDeck == null) — skip silently.
                if (casterDeck != null)
                    ResolveCardManipulationEffect(effect, casterDeck);
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

        private void ResolveResourceEffect(CardEffect effect, BattleStats caster, EffectContext ctx = null, int? amountOverride = null)
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

                case ResourceEffectType.ReduceHostility:
                    ApplyReduceHostility(_opponentStats, GetAmount());
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

        private void ResolveCardManipulationEffect(CardEffect effect, DeckManager deck)
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
                    ApplyDiscardHand(deck);
                    break;

                case CardManipulationType.ExhaustThisCard:
                    ApplyExhaustCard(deck);
                    break;

                case CardManipulationType.AddCardToDeck:
                    ApplyAddCardToDeck(deck, effect.CardToAdd, effect.CardAmount);
                    break;

                case CardManipulationType.AddCardToHand:
                    ApplyAddCardToHand(deck, effect.CardToAdd, effect.CardAmount);
                    break;

                case CardManipulationType.UpgradeCardThisBattle:
                    ApplyUpgradeCardThisBattle(deck);
                    break;

                case CardManipulationType.UpgradeAllCardsInHand:
                    ApplyUpgradeAllCardsInHand(deck);
                    break;

                case CardManipulationType.MakeCardRetain:
                    ApplyMakeCardRetain(deck);
                    break;

                case CardManipulationType.MakeAllCardsRetain:
                    ApplyMakeAllCardsRetain(deck);
                    break;

                case CardManipulationType.ReduceCardCost:
                    ApplyReduceCardCost(deck, effect.CostReduction);
                    break;

                case CardManipulationType.MakeCardFree:
                    ApplyMakeCardFree(deck);
                    break;
            }
        }

        #endregion

        #region Core Damage & Healing

        private void ApplyResolveDamage(BattleStats target, BattleStats attacker, int baseDamage, EffectContext ctx = null)
        {
            // Get attacker and target status effect managers
            StatusEffectManager attackerStatusMgr = GetStatusEffectManager(attacker);
            StatusEffectManager targetStatusMgr   = GetStatusEffectManager(target);

            // Apply attacker's damage modifiers (Strength, Weakened, Exposed)
            int modifiedDamage = attackerStatusMgr.ModifyDamageDealt(baseDamage);

            // Apply target's damage taken modifiers (Vulnerable, Plated, Intangible, Thorns)
            modifiedDamage = targetStatusMgr.ModifyDamageTaken(modifiedDamage, attacker);

            // Hostile enemies (positive hostility) deal amplified damage; neutral and receptive don't
            if (attacker != _playerStats && attacker.CurrentHostility > 0)
            {
                float hostilityMult = Mathf.Max(0.1f, attacker.HostilityDamageMultiplier);
                modifiedDamage = Mathf.RoundToInt(modifiedDamage * hostilityMult);
            }

            // Apply damage — target's Composure shield absorbs first, then Resolve takes the rest
            int actualDamage = target.DamageResolve(modifiedDamage);
            GameLogger.LogInfo<EffectResolver>($"Dealt {actualDamage} damage (base: {baseDamage}, modified: {modifiedDamage}, HostilityMult: {(attacker != _playerStats && attacker.CurrentHostility > 0 ? attacker.HostilityDamageMultiplier.ToString("F2") : "1.00")})");

            if (actualDamage > 0)
            {
                bool isPlayerAttacking = attacker == _playerStats;
                EventBus.Publish(new DamageDealtEvent
                {
                    Amount            = actualDamage,
                    IsToPlayer        = target == _playerStats,
                    AttackerName      = isPlayerAttacking ? "Player" : _attackerName,
                    SourceEnemyIndex  = isPlayerAttacking ? -1 : _attackerEnemyIndex,
                    TargetEnemyIndex  = isPlayerAttacking ? _attackerEnemyIndex : -1,
                });
            }

            // Accumulate into context so triggered effects can react (e.g. lifesteal)
            if (ctx != null)
            {
                ctx.LastDamageDealt += actualDamage;
                if (target.CurrentResolve <= 0) ctx.LastTargetDied = true;
            }
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

            // Get attacker and target status effect managers
            StatusEffectManager attackerStatusMgr = GetStatusEffectManager(attacker);
            StatusEffectManager targetStatusMgr = GetStatusEffectManager(target);

            // Apply attacker's damage modifiers (Strength, Weakened, Exposed)
            int modifiedDamage = attackerStatusMgr.ModifyDamageDealt(randomDamage);

            // Apply target's damage taken modifiers (Vulnerable, Plated, Intangible, Thorns)
            modifiedDamage = targetStatusMgr.ModifyDamageTaken(modifiedDamage, attacker);

            // Hostile enemies (positive hostility) deal amplified damage; neutral and receptive don't
            if (attacker != _playerStats && attacker.CurrentHostility > 0)
            {
                float hostilityMult = Mathf.Max(0.1f, attacker.HostilityDamageMultiplier);
                modifiedDamage = Mathf.RoundToInt(modifiedDamage * hostilityMult);
            }

            // Apply damage — target's Composure shield absorbs first, then Resolve takes the rest
            int actualDamage = target.DamageResolve(modifiedDamage);
            GameLogger.LogInfo<EffectResolver>($"Dealt {actualDamage} random damage (rolled {randomDamage} from {minDamage}-{maxDamage}, modified: {modifiedDamage})");

            if (actualDamage > 0)
            {
                bool isPlayerAttacking = attacker == _playerStats;
                EventBus.Publish(new DamageDealtEvent
                {
                    Amount            = actualDamage,
                    IsToPlayer        = target == _playerStats,
                    AttackerName      = isPlayerAttacking ? "Player" : _attackerName,
                    SourceEnemyIndex  = isPlayerAttacking ? -1 : _attackerEnemyIndex,
                    TargetEnemyIndex  = isPlayerAttacking ? _attackerEnemyIndex : -1,
                });
            }

            if (ctx != null)
            {
                ctx.LastDamageDealt += actualDamage;
                if (target.CurrentResolve <= 0) ctx.LastTargetDied = true;
            }
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

            // Get attacker and target status effect managers
            StatusEffectManager attackerStatusMgr = GetStatusEffectManager(attacker);
            StatusEffectManager targetStatusMgr = GetStatusEffectManager(target);

            // Apply attacker's damage modifiers (Strength, Weakened, Exposed)
            int modifiedDamage = attackerStatusMgr.ModifyDamageDealt(composure);

            // Apply target's damage taken modifiers (Vulnerable, Plated, Intangible, Thorns)
            modifiedDamage = targetStatusMgr.ModifyDamageTaken(modifiedDamage, attacker);

            // Hostile enemies (positive hostility) deal amplified damage; neutral and receptive don't
            if (attacker != _playerStats && attacker.CurrentHostility > 0)
            {
                float hostilityMult = Mathf.Max(0.1f, attacker.HostilityDamageMultiplier);
                modifiedDamage = Mathf.RoundToInt(modifiedDamage * hostilityMult);
            }

            // Apply damage (damage value equals caster's Composure; target's Composure shield still absorbs first)
            int actualDamage = target.DamageResolve(modifiedDamage);
            GameLogger.LogInfo<EffectResolver>($"Dealt {actualDamage} damage equal to Composure ({composure}, modified: {modifiedDamage})");

            if (actualDamage > 0)
            {
                bool isPlayerAttacking = attacker == _playerStats;
                EventBus.Publish(new DamageDealtEvent
                {
                    Amount            = actualDamage,
                    IsToPlayer        = target == _playerStats,
                    AttackerName      = isPlayerAttacking ? "Player" : _attackerName,
                    SourceEnemyIndex  = isPlayerAttacking ? -1 : _attackerEnemyIndex,
                    TargetEnemyIndex  = isPlayerAttacking ? _attackerEnemyIndex : -1,
                });
            }

            if (ctx != null)
            {
                ctx.LastDamageDealt += actualDamage;
                if (target.CurrentResolve <= 0) ctx.LastTargetDied = true;
            }
        }

        private void ApplyConsumeAllComposure(BattleStats caster)
        {
            int consumed = caster.ConsumeAllComposure();
            GameLogger.LogInfo<EffectResolver>($"Consumed {consumed} Composure");
        }

        #endregion

        #region Hostility

        private void ApplyReduceHostility(BattleStats caster, int amount)
        {
            int actualReduction = caster.ReduceHostility(amount);
            GameLogger.LogInfo<EffectResolver>($"Reduced {actualReduction} Hostility");
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

        private void ApplyExhaustCard(DeckManager deck)
        {
            // Exhaust the card that was just played (handled by DeckManager)
            GameLogger.LogInfo<EffectResolver>("Card will be exhausted after play");
        }

        private void ApplyDiscardHand(DeckManager deck)
        {
            int cardsDiscarded = deck.DiscardHand();
            GameLogger.LogInfo<EffectResolver>($"Discarded entire hand ({cardsDiscarded} cards)");
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

        private void ApplyUpgradeCardThisBattle(DeckManager deck)
        {
            var upgradeable = new List<CardData>();
            foreach (var c in deck.Hand)
                if (c != null && c.CanUpgrade) upgradeable.Add(c);

            if (upgradeable.Count == 0)
            {
                GameLogger.LogInfo<EffectResolver>("UpgradeCardThisBattle: no upgradeable cards in hand — no-op");
                return;
            }
            EventBus.Publish(new CardChoiceRequestedEvent
            {
                Title         = "Choose a card to Upgrade",
                Choices       = upgradeable,
                RequiredCount = 1,
                OnConfirmed   = chosen =>
                {
                    if (chosen.Count == 0) return;
                    CardData old      = chosen[0];
                    CardData upgraded = old.GetCurrentVersion();
                    deck.SwapCardInHand(old, upgraded);
                }
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

        private void ApplyMakeCardRetain(DeckManager deck)
        {
            if (deck.HandCount == 0)
            {
                GameLogger.LogInfo<EffectResolver>("MakeCardRetain: hand is empty — no-op");
                return;
            }
            EventBus.Publish(new CardChoiceRequestedEvent
            {
                Title         = "Choose a card to Retain",
                Choices       = deck.Hand,
                RequiredCount = 1,
                OnConfirmed   = chosen => { if (chosen.Count > 0) deck.RetainCard(chosen[0]); }
            });
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

        private void ApplyReduceCardCost(DeckManager deck, int reduction)
        {
            if (deck.HandCount == 0)
            {
                GameLogger.LogInfo<EffectResolver>("ReduceCardCost: hand is empty — no-op");
                return;
            }
            EventBus.Publish(new CardChoiceRequestedEvent
            {
                Title         = $"Choose a card — Reduce cost by {reduction}",
                Choices       = deck.Hand,
                RequiredCount = 1,
                OnConfirmed   = chosen => { if (chosen.Count > 0) deck.ApplyCostReduction(chosen[0], reduction); }
            });
        }

        private void ApplyMakeCardFree(DeckManager deck)
        {
            if (deck.HandCount == 0)
            {
                GameLogger.LogInfo<EffectResolver>("MakeCardFree: hand is empty — no-op");
                return;
            }
            EventBus.Publish(new CardChoiceRequestedEvent
            {
                Title         = "Choose a card — Make it Free",
                Choices       = deck.Hand,
                RequiredCount = 1,
                OnConfirmed   = chosen => { if (chosen.Count > 0) deck.MakeCardFreeThisBattle(chosen[0]); }
            });
        }

        #endregion

        #region Utility

        /// <summary>
        /// Checks if an effect can be applied (e.g., enough resources, valid target).
        /// </summary>
        public bool CanApplyEffect(CardEffect effect, bool isPlayerCard)
        {
            // Basic validation - all effects are now battle effects
            return true;
        }

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
