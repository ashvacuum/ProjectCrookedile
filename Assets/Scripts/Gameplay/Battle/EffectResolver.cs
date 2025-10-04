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

                case CardManipulationType.DiscardCards:
                    ApplyDiscardCards(deck, effect.CardAmount);
                    break;

                case CardManipulationType.ExhaustThisCard:
                    ApplyExhaustCard(deck);
                    break;
            }
        }

        #endregion

        #region Core Damage & Healing

        private void ApplyResolveDamage(BattleStats target, BattleStats attacker, int baseDamage)
        {
            int actualDamage = target.DamageResolve(baseDamage, attacker.CurrentComposure);
            GameLogger.LogInfo<EffectResolver>($"Dealt {actualDamage} Resolve damage (base: {baseDamage}, Composure: {attacker.CurrentComposure})");
        }

        private void ApplyResolveHeal(BattleStats target, int amount)
        {
            int actualHealing = target.RestoreResolve(amount);
            GameLogger.LogInfo<EffectResolver>($"Restored {actualHealing} Resolve");
        }

        private void ApplyRandomDamage(BattleStats target, BattleStats attacker, int minDamage, int maxDamage)
        {
            int randomDamage = RandomHelper.Range(minDamage, maxDamage + 1);
            int actualDamage = target.DamageResolve(randomDamage, attacker.CurrentComposure);
            GameLogger.LogInfo<EffectResolver>($"Dealt {actualDamage} random Resolve damage (rolled {randomDamage} from {minDamage}-{maxDamage})");
        }

        #endregion

        #region Composure

        private void ApplyGainComposure(BattleStats target, int amount)
        {
            target.GainComposure(amount);
            GameLogger.LogInfo<EffectResolver>($"Gained {amount} Composure");
        }

        private void ApplyLoseComposure(BattleStats target, int amount)
        {
            int actualLoss = target.LoseComposure(amount);
            GameLogger.LogInfo<EffectResolver>($"Lost {actualLoss} Composure");
        }

        private void ApplyResolveDamageEqualToComposure(BattleStats target, BattleStats attacker)
        {
            int composure = attacker.CurrentComposure;
            int actualDamage = target.DamageResolve(composure, 0); // Don't double-count Composure
            GameLogger.LogInfo<EffectResolver>($"Dealt {actualDamage} Resolve damage equal to Composure ({composure})");
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

        #endregion
    }
}
