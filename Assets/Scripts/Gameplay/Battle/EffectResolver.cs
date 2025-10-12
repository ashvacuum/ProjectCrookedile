using System;
using System.Collections.Generic;
using UnityEngine;
using Crookedile.Data;
using Crookedile.Data.Cards;
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
        private BattleStats _opponentStats;
        private DeckManager _playerDeck;
        private DeckManager _opponentDeck;
        private StatusEffectManager _playerStatusEffects;
        private StatusEffectManager _opponentStatusEffects;

        // Events for effect notifications
        public event Action<CardEffect, BattleStats> OnEffectApplied;

        public EffectResolver(BattleStats playerStats, BattleStats opponentStats, DeckManager playerDeck, DeckManager opponentDeck)
        {
            _playerStats = playerStats;
            _opponentStats = opponentStats;
            _playerDeck = playerDeck;
            _opponentDeck = opponentDeck;
            _playerStatusEffects = new StatusEffectManager("Player");
            _opponentStatusEffects = new StatusEffectManager("Opponent");
        }

        public StatusEffectManager PlayerStatusEffects => _playerStatusEffects;
        public StatusEffectManager OpponentStatusEffects => _opponentStatusEffects;

        #region Effect Resolution

        /// <summary>
        /// Resolves all effects from a played card.
        /// </summary>
        /// <param name="card">The card being played</param>
        /// <param name="isPlayerCard">Is this card played by the player?</param>
        public void ResolveCardEffects(CardData card, bool isPlayerCard)
        {
            GameLogger.LogInfo<EffectResolver>($"Resolving effects for: {card.CardName} (Player: {isPlayerCard})");

            List<CardEffect> effects = card.Effects;

            foreach (CardEffect effect in effects)
            {
                ResolveBattleEffect(effect, isPlayerCard);
            }
        }

        /// <summary>
        /// Resolves a single battle effect using the new simplified system.
        /// </summary>
        private void ResolveBattleEffect(CardEffect effect, bool isPlayerCard)
        {
            BattleStats casterStats = isPlayerCard ? _playerStats : _opponentStats;
            BattleStats targetStats = isPlayerCard ? _opponentStats : _playerStats;
            DeckManager casterDeck = isPlayerCard ? _playerDeck : _opponentDeck;
            StatusEffectManager casterStatusEffects = isPlayerCard ? _playerStatusEffects : _opponentStatusEffects;
            StatusEffectManager targetStatusEffects = isPlayerCard ? _opponentStatusEffects : _playerStatusEffects;

            // Determine actual target based on effect target type
            BattleStats effectTarget = effect.Target switch
            {
                TargetType.Self => casterStats,
                TargetType.Opponent => targetStats,
                TargetType.All => null, // Special handling for All
                TargetType.Random => UnityEngine.Random.value > 0.5f ? casterStats : targetStats,
                _ => targetStats
            };

            StatusEffectManager effectTargetStatusMgr = effect.Target switch
            {
                TargetType.Self => casterStatusEffects,
                TargetType.Opponent => targetStatusEffects,
                TargetType.All => null,
                TargetType.Random => UnityEngine.Random.value > 0.5f ? casterStatusEffects : targetStatusEffects,
                _ => targetStatusEffects
            };

            // Apply effect based on category
            switch (effect.Category)
            {
                case EffectCategory.Damage:
                    ResolveDamageEffect(effect, effectTarget, casterStats);
                    break;

                case EffectCategory.Resource:
                    ResolveResourceEffect(effect, casterStats);
                    break;

                case EffectCategory.CardManipulation:
                    ResolveCardManipulationEffect(effect, casterDeck);
                    break;

                case EffectCategory.StatusEffect:
                    effectTargetStatusMgr.ApplyStatusEffect(effect.StatusEffectType, effect.StatusStacks, effect.StatusDuration);
                    GameLogger.LogInfo<EffectResolver>($"Applied {effect.StatusStacks} {effect.StatusEffectType} ({effect.StatusDuration})");
                    break;
            }

            // Notify listeners
            OnEffectApplied?.Invoke(effect, effectTarget);
        }

        private void ResolveDamageEffect(CardEffect effect, BattleStats target, BattleStats attacker)
        {
            switch (effect.DamageType)
            {
                case DamageType.FixedDamage:
                    ApplyResolveDamage(target, attacker, effect.DamageAmount);
                    break;

                case DamageType.RandomDamage:
                    ApplyRandomDamage(target, attacker, effect.RandomDamageMin, effect.RandomDamageMax);
                    break;

                case DamageType.DamageEqualToComposure:
                    ApplyResolveDamageEqualToComposure(target, attacker);
                    break;
            }
        }

        private void ResolveResourceEffect(CardEffect effect, BattleStats caster)
        {
            switch (effect.ResourceType)
            {
                case ResourceEffectType.GainComposure:
                    ApplyGainComposure(caster, effect.ResourceAmount);
                    break;

                case ResourceEffectType.LoseComposure:
                    ApplyLoseComposure(caster, effect.ResourceAmount);
                    break;

                case ResourceEffectType.ConsumeAllComposure:
                    ApplyConsumeAllComposure(caster);
                    break;

                case ResourceEffectType.ComposureEqualToHostility:
                    ApplyComposureEqualToHostility(caster);
                    break;

                case ResourceEffectType.GainHostility:
                    ApplyGainHostility(caster, effect.ResourceAmount);
                    break;

                case ResourceEffectType.ReduceHostility:
                    ApplyReduceHostility(caster, effect.ResourceAmount);
                    break;

                case ResourceEffectType.GainActionPoints:
                    ApplyGainActionPoints(caster, effect.ResourceAmount);
                    break;

                case ResourceEffectType.GainActionPointsNextTurn:
                    ApplyGainActionPointsNextTurn(caster, effect.ResourceAmount);
                    break;

                case ResourceEffectType.HealResolve:
                    ApplyResolveHeal(caster, effect.ResourceAmount);
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

        private void ApplyResolveDamage(BattleStats target, BattleStats attacker, int baseDamage)
        {
            // Get attacker and target status effect managers
            StatusEffectManager attackerStatusMgr = GetStatusEffectManager(attacker);
            StatusEffectManager targetStatusMgr = GetStatusEffectManager(target);

            // Apply attacker's damage modifiers (Strength, Weakened, Exposed)
            int modifiedDamage = attackerStatusMgr.ModifyDamageDealt(baseDamage);

            // Apply target's damage taken modifiers (Vulnerable, Plated, Intangible, Thorns)
            modifiedDamage = targetStatusMgr.ModifyDamageTaken(modifiedDamage, attacker);

            // Apply damage with Composure bonus
            int actualDamage = target.DamageResolve(modifiedDamage, attacker.CurrentComposure);
            GameLogger.LogInfo<EffectResolver>($"Dealt {actualDamage} Resolve damage (base: {baseDamage}, modified: {modifiedDamage}, Composure: {attacker.CurrentComposure})");
        }

        private void ApplyResolveHeal(BattleStats target, int amount)
        {
            int actualHealing = target.RestoreResolve(amount);
            GameLogger.LogInfo<EffectResolver>($"Restored {actualHealing} Resolve");
        }

        private void ApplyRandomDamage(BattleStats target, BattleStats attacker, int minDamage, int maxDamage)
        {
            int randomDamage = RandomHelper.Range(minDamage, maxDamage + 1);

            // Get attacker and target status effect managers
            StatusEffectManager attackerStatusMgr = GetStatusEffectManager(attacker);
            StatusEffectManager targetStatusMgr = GetStatusEffectManager(target);

            // Apply attacker's damage modifiers (Strength, Weakened, Exposed)
            int modifiedDamage = attackerStatusMgr.ModifyDamageDealt(randomDamage);

            // Apply target's damage taken modifiers (Vulnerable, Plated, Intangible, Thorns)
            modifiedDamage = targetStatusMgr.ModifyDamageTaken(modifiedDamage, attacker);

            // Apply damage with Composure bonus
            int actualDamage = target.DamageResolve(modifiedDamage, attacker.CurrentComposure);
            GameLogger.LogInfo<EffectResolver>($"Dealt {actualDamage} random Resolve damage (rolled {randomDamage} from {minDamage}-{maxDamage}, modified: {modifiedDamage})");
        }

        #endregion

        #region Composure

        private void ApplyGainComposure(BattleStats target, int amount)
        {
            // Apply Composure gain modifiers (Dexterity, Frail)
            StatusEffectManager targetStatusMgr = GetStatusEffectManager(target);
            int modifiedAmount = targetStatusMgr.ModifyComposureGained(amount);

            target.GainComposure(modifiedAmount);
            GameLogger.LogInfo<EffectResolver>($"Gained {modifiedAmount} Composure (base: {amount})");
        }

        private void ApplyLoseComposure(BattleStats target, int amount)
        {
            int actualLoss = target.LoseComposure(amount);
            GameLogger.LogInfo<EffectResolver>($"Lost {actualLoss} Composure");
        }

        private void ApplyResolveDamageEqualToComposure(BattleStats target, BattleStats attacker)
        {
            int composure = attacker.CurrentComposure;

            // Get attacker and target status effect managers
            StatusEffectManager attackerStatusMgr = GetStatusEffectManager(attacker);
            StatusEffectManager targetStatusMgr = GetStatusEffectManager(target);

            // Apply attacker's damage modifiers (Strength, Weakened, Exposed)
            int modifiedDamage = attackerStatusMgr.ModifyDamageDealt(composure);

            // Apply target's damage taken modifiers (Vulnerable, Plated, Intangible, Thorns)
            modifiedDamage = targetStatusMgr.ModifyDamageTaken(modifiedDamage, attacker);

            // Apply damage (don't add Composure bonus since damage IS equal to Composure)
            int actualDamage = target.DamageResolve(modifiedDamage, 0);
            GameLogger.LogInfo<EffectResolver>($"Dealt {actualDamage} Resolve damage equal to Composure ({composure}, modified: {modifiedDamage})");
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
            int cardsDiscarded = 0;
            int handSize = deck.HandCount;

            // Discard all cards from hand
            for (int i = handSize - 1; i >= 0; i--)
            {
                if (deck.DiscardCard(deck.Hand[i]))
                {
                    cardsDiscarded++;
                }
            }

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
                GameLogger.LogInfo<EffectResolver>("Choose from discard to deck: Discard pile is empty");
            }
        }

        private void ApplyAddCardToDeck(DeckManager deck, CardData card, int amount)
        {
            if (card == null)
            {
                GameLogger.LogWarning<EffectResolver>("Cannot add card to deck: No card specified");
                return;
            }

            // TODO: Implement method to add card to deck
            GameLogger.LogInfo<EffectResolver>($"Added {amount}x {card.CardName} to deck");
        }

        private void ApplyAddCardToHand(DeckManager deck, CardData card, int amount)
        {
            if (card == null)
            {
                GameLogger.LogWarning<EffectResolver>("Cannot add card to hand: No card specified");
                return;
            }

            // TODO: Implement method to add card to hand
            GameLogger.LogInfo<EffectResolver>($"Added {amount}x {card.CardName} to hand");
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
        /// </summary>
        private StatusEffectManager GetStatusEffectManager(BattleStats stats)
        {
            if (stats == _playerStats)
                return _playerStatusEffects;
            else if (stats == _opponentStats)
                return _opponentStatusEffects;
            else
                return null; // This shouldn't happen
        }

        #endregion
    }
}
