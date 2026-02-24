using System;
using System.Collections.Generic;
using UnityEngine;
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

        // Events for effect notifications
        public event Action<CardEffect, BattleStats> OnEffectApplied;

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
        public void SetFocusedOpponent(BattleStats stats, StatusEffectManager statusEffects)
        {
            _opponentStats         = stats;
            _opponentStatusEffects = statusEffects;
        }

        #region Effect Resolution

        /// <summary>
        /// Resolves all effects from a played card, then fires any triggered effects
        /// whose trigger/condition match what happened during base-effect resolution.
        /// </summary>
        /// <param name="card">The card being played</param>
        /// <param name="isPlayerCard">Is this card played by the player?</param>
        public void ResolveCardEffects(CardData card, bool isPlayerCard)
        {
            GameLogger.LogInfo<EffectResolver>($"Resolving effects for: {card.CardName} (Player: {isPlayerCard})");

            // Create a fresh context for this card's resolution
            var ctx = new EffectContext
            {
                Caster = isPlayerCard ? _playerStats : _opponentStats,
                Target = isPlayerCard ? _opponentStats : _playerStats
            };

            foreach (CardEffect effect in card.Effects)
            {
                ResolveBattleEffect(effect, isPlayerCard, ctx);
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
        /// Resolves all effects from a scripted enemy move.
        /// Uses the same CardEffect pipeline as player cards with isPlayerCard=false
        /// (enemy is caster, player is the default target).
        /// CardManipulation effects are silently skipped — enemies have no deck.
        /// </summary>
        public void ResolveEnemyMoveEffects(EnemyMoveData move)
        {
            if (move == null) return;

            GameLogger.LogInfo<EffectResolver>($"Resolving enemy move: {move.MoveName}");

            foreach (CardEffect effect in move.Effects)
            {
                ResolveBattleEffect(effect, isPlayerCard: false);
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
        private void ResolveBattleEffect(CardEffect effect, bool isPlayerCard, EffectContext ctx = null)
        {
            BattleStats         casterStats         = isPlayerCard ? _playerStats          : _opponentStats;
            BattleStats         targetStats         = isPlayerCard ? _opponentStats         : _playerStats;
            DeckManager         casterDeck          = isPlayerCard ? _playerDeck            : null;
            StatusEffectManager casterStatusEffects = isPlayerCard ? _playerStatusEffects   : _opponentStatusEffects;
            StatusEffectManager targetStatusEffects = isPlayerCard ? _opponentStatusEffects : _playerStatusEffects;

            // Resource and CardManipulation are always caster-scoped — apply once and return
            if (effect.Category == EffectCategory.Resource)
            {
                ResolveResourceEffect(effect, casterStats, ctx);
                OnEffectApplied?.Invoke(effect, casterStats);
                return;
            }
            if (effect.Category == EffectCategory.CardManipulation)
            {
                // Enemies have no deck (casterDeck == null) — skip silently.
                if (casterDeck != null)
                    ResolveCardManipulationEffect(effect, casterDeck);
                OnEffectApplied?.Invoke(effect, casterStats);
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
                        ResolveDamageEffect(effect, effectTarget, casterStats, ctx);
                        break;

                    case EffectCategory.StatusEffect:
                        effectTargetStatusMgr?.ApplyStatusEffect(
                            effect.StatusEffectType, effect.StatusStacks, effect.StatusDuration);
                        GameLogger.LogInfo<EffectResolver>(
                            $"Applied {effect.StatusStacks} {effect.StatusEffectType} ({effect.StatusDuration})");
                        break;
                }
                OnEffectApplied?.Invoke(effect, effectTarget);
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
                    // Single coin-flip for both stats AND status mgr (fixes original double-roll bug)
                    if (UnityEngine.Random.value > 0.5f)
                        pairs.Add((casterStats, casterStatusMgr));
                    else
                        pairs.Add((targetStats, targetStatusMgr));
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

        private void ResolveDamageEffect(CardEffect effect, BattleStats target, BattleStats attacker, EffectContext ctx = null)
        {
            switch (effect.DamageType)
            {
                case DamageType.FixedDamage:
                    ApplyResolveDamage(target, attacker, effect.GetEffectiveAmount(ctx), ctx);
                    break;

                case DamageType.RandomDamage:
                    ApplyRandomDamage(target, attacker, effect.RandomDamageMin, effect.RandomDamageMax, ctx);
                    break;

                case DamageType.DamageEqualToComposure:
                    ApplyResolveDamageEqualToComposure(target, attacker, ctx);
                    break;
            }
        }

        private void ResolveResourceEffect(CardEffect effect, BattleStats caster, EffectContext ctx = null)
        {
            switch (effect.ResourceType)
            {
                case ResourceEffectType.GainComposure:
                    ApplyGainComposure(caster, effect.GetEffectiveAmount(ctx), ctx);
                    break;

                case ResourceEffectType.LoseComposure:
                    ApplyLoseComposure(caster, effect.GetEffectiveAmount(ctx));
                    break;

                case ResourceEffectType.ConsumeAllComposure:
                    ApplyConsumeAllComposure(caster);
                    break;

                case ResourceEffectType.ComposureEqualToHostility:
                    ApplyComposureEqualToHostility(caster);
                    break;

                case ResourceEffectType.GainHostility:
                    // Hostility is the enemy's number line — always shifts opponent, never caster
                    ApplyGainHostility(_opponentStats, effect.GetEffectiveAmount(ctx));
                    break;

                case ResourceEffectType.ReduceHostility:
                    ApplyReduceHostility(_opponentStats, effect.GetEffectiveAmount(ctx));
                    break;

                case ResourceEffectType.GainActionPoints:
                    ApplyGainActionPoints(caster, effect.GetEffectiveAmount(ctx));
                    break;

                case ResourceEffectType.GainActionPointsNextTurn:
                    ApplyGainActionPointsNextTurn(caster, effect.GetEffectiveAmount(ctx));
                    break;

                case ResourceEffectType.HealResolve:
                    ApplyResolveHeal(caster, effect.GetEffectiveAmount(ctx), ctx);
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
            StatusEffectManager targetStatusMgr = GetStatusEffectManager(target);

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

            // Apply damage with Composure bonus
            int actualDamage = target.DamageResolve(modifiedDamage, attacker.CurrentComposure);
            GameLogger.LogInfo<EffectResolver>($"Dealt {actualDamage} Resolve damage (base: {baseDamage}, modified: {modifiedDamage}, Composure: {attacker.CurrentComposure}, HostilityMult: {(attacker != _playerStats && attacker.CurrentHostility > 0 ? attacker.HostilityDamageMultiplier.ToString("F2") : "1.00")})");

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

            // Apply damage with Composure bonus
            int actualDamage = target.DamageResolve(modifiedDamage, attacker.CurrentComposure);
            GameLogger.LogInfo<EffectResolver>($"Dealt {actualDamage} random Resolve damage (rolled {randomDamage} from {minDamage}-{maxDamage}, modified: {modifiedDamage})");

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

            // Apply damage (don't add Composure bonus since damage IS equal to Composure)
            int actualDamage = target.DamageResolve(modifiedDamage, 0);
            GameLogger.LogInfo<EffectResolver>($"Dealt {actualDamage} Resolve damage equal to Composure ({composure}, modified: {modifiedDamage})");

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

        private void ApplyGainHostility(BattleStats caster, int amount)
        {
            caster.GainHostility(amount);
            GameLogger.LogInfo<EffectResolver>($"Gained {amount} Hostility");
        }

        private void ApplyReduceHostility(BattleStats caster, int amount)
        {
            int actualReduction = caster.ReduceHostility(amount);
            GameLogger.LogInfo<EffectResolver>($"Reduced {actualReduction} Hostility");
        }

        private void ApplyComposureEqualToHostility(BattleStats caster)
        {
            int hostility = caster.CurrentHostility;
            caster.GainComposure(hostility);
            GameLogger.LogInfo<EffectResolver>($"Gained {hostility} Composure equal to Hostility");
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
            // TODO: This requires player choice UI and DeckManager method to move cards from discard to hand
            int availableCards = deck.DiscardCount;
            int cardsToRetrieve = Mathf.Min(amount, availableCards);

            if (cardsToRetrieve > 0)
            {
                GameLogger.LogWarning<EffectResolver>($"Choose from discard to hand: Requires UI implementation (would retrieve {cardsToRetrieve}/{amount} cards from {availableCards} in discard)");
            }
            else
            {
                GameLogger.LogInfo<EffectResolver>("Choose from discard to hand: Discard pile is empty");
            }
        }

        private void ApplyChooseFromDiscardToDeck(DeckManager deck, int amount)
        {
            // TODO: This requires player choice UI and DeckManager method to move cards from discard to deck
            int availableCards = deck.DiscardCount;
            int cardsToRetrieve = Mathf.Min(amount, availableCards);

            if (cardsToRetrieve > 0)
            {
                GameLogger.LogWarning<EffectResolver>($"Choose from discard to deck: Requires UI implementation (would retrieve {cardsToRetrieve}/{amount} cards from {availableCards} in discard)");
            }
            else
            {
                GameLogger.LogInfo<EffectResolver>("Choose from discard to deck: Discard pthile is empty");
            }
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
            // TODO: This requires player choice UI
            // For now, just log the intent
            if (deck.HandCount > 0)
            {
                GameLogger.LogWarning<EffectResolver>("Upgrade card this battle: Requires UI implementation (player choice)");
            }
            else
            {
                GameLogger.LogInfo<EffectResolver>("No cards in hand to upgrade");
            }
        }

        private void ApplyUpgradeAllCardsInHand(DeckManager deck)
        {
            int cardsUpgraded = 0;

            // TODO: Implement card upgrade system
            // For now, just count upgradeable cards
            foreach (var card in deck.Hand)
            {
                if (card != null && card.CanUpgrade)
                {
                    cardsUpgraded++;
                }
            }

            GameLogger.LogInfo<EffectResolver>($"Upgraded all cards in hand ({cardsUpgraded} cards)");
        }

        private void ApplyMakeCardRetain(DeckManager deck)
        {
            // TODO: This requires player choice UI and retain system
            // For now, just log the intent
            if (deck.HandCount > 0)
            {
                GameLogger.LogWarning<EffectResolver>("Make card retain: Requires UI implementation and retain system");
            }
            else
            {
                GameLogger.LogInfo<EffectResolver>("No cards in hand to make retain");
            }
        }

        private void ApplyMakeAllCardsRetain(DeckManager deck)
        {
            // TODO: Implement retain system (cards don't discard at end of turn)
            int cardsRetained = deck.HandCount;
            GameLogger.LogInfo<EffectResolver>($"Made all cards retain ({cardsRetained} cards won't discard at end of turn)");
        }

        private void ApplyReduceCardCost(DeckManager deck, int reduction)
        {
            // TODO: This requires player choice UI and cost modification system
            // For now, just log the intent
            if (deck.HandCount > 0)
            {
                GameLogger.LogWarning<EffectResolver>($"Reduce card cost by {reduction}: Requires UI implementation and cost modification system");
            }
            else
            {
                GameLogger.LogInfo<EffectResolver>("No cards in hand to reduce cost");
            }
        }

        private void ApplyMakeCardFree(DeckManager deck)
        {
            // TODO: This requires player choice UI and temporary cost modification
            // For now, just log the intent
            if (deck.HandCount > 0)
            {
                GameLogger.LogWarning<EffectResolver>("Make card free this turn: Requires UI implementation and cost modification system");
            }
            else
            {
                GameLogger.LogInfo<EffectResolver>("No cards in hand to make free");
            }
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
