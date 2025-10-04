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
    /// Applies damage, healing, buffs, debuffs, and other battle effects to combatants.
    /// </summary>
    public class EffectResolver
    {
        private BattleStats _playerStats;
        private BattleStats _opponentStats;
        private DeckManager _playerDeck;
        private DeckManager _opponentDeck;

        // Events for effect notifications
        public event Action<CardEffect, BattleStats> OnEffectApplied;

        public EffectResolver(BattleStats playerStats, BattleStats opponentStats, DeckManager playerDeck, DeckManager opponentDeck)
        {
            _playerStats = playerStats;
            _opponentStats = opponentStats;
            _playerDeck = playerDeck;
            _opponentDeck = opponentDeck;
        }

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
                if (effect.EffectContext == EffectContext.Battle)
                {
                    ResolveBattleEffect(effect, isPlayerCard);
                }
                else
                {
                    // Campaign effects are handled outside of battle
                    GameLogger.LogInfo<EffectResolver>($"Skipping campaign effect: {effect.CampaignEffectType}");
                }
            }
        }

        /// <summary>
        /// Resolves a single battle effect.
        /// </summary>
        private void ResolveBattleEffect(CardEffect effect, bool isPlayerCard)
        {
            BattleStats casterStats = isPlayerCard ? _playerStats : _opponentStats;
            BattleStats targetStats = isPlayerCard ? _opponentStats : _playerStats;
            DeckManager casterDeck = isPlayerCard ? _playerDeck : _opponentDeck;
            DeckManager targetDeck = isPlayerCard ? _opponentDeck : _playerDeck;

            // Determine actual target based on effect target type
            BattleStats effectTarget = effect.TargetType switch
            {
                TargetType.Self => casterStats,
                TargetType.Opponent => targetStats,
                TargetType.All => null, // Special handling for All
                TargetType.Random => UnityEngine.Random.value > 0.5f ? casterStats : targetStats,
                _ => targetStats
            };

            // Apply effect based on type
            switch (effect.BattleEffectType)
            {
                case BattleEffectType.ConfidenceDamage:
                    ApplyConfidenceDamage(effectTarget, effect.Amount);
                    break;

                case BattleEffectType.EgoDamage:
                    ApplyEgoDamage(effectTarget, effect.Amount);
                    break;

                case BattleEffectType.ConfidenceRestore:
                    ApplyConfidenceRestore(effectTarget, effect.Amount);
                    break;

                case BattleEffectType.EgoRestore:
                    ApplyEgoRestore(effectTarget, effect.Amount);
                    break;

                case BattleEffectType.GainActionPoints:
                    ApplyGainActionPoints(casterStats, effect.Amount);
                    break;

                case BattleEffectType.DrawCards:
                    ApplyDrawCards(casterDeck, effect.Amount);
                    break;

                case BattleEffectType.DiscardCards:
                    ApplyDiscardCards(effect.TargetType == TargetType.Self ? casterDeck : targetDeck, effect.Amount);
                    break;

                case BattleEffectType.GainBlock:
                    ApplyGainBlock(effectTarget, effect.Amount);
                    break;

                case BattleEffectType.ApplyBuff:
                    ApplyBuff(effectTarget, effect.Amount);
                    break;

                case BattleEffectType.ApplyDebuff:
                    ApplyDebuff(effectTarget, effect.Amount);
                    break;

                case BattleEffectType.DestroyCard:
                    ApplyDestroyCard(targetDeck);
                    break;

                case BattleEffectType.ExhaustCard:
                    ApplyExhaustCard(casterDeck);
                    break;

                default:
                    GameLogger.LogWarning<EffectResolver>($"Unhandled battle effect type: {effect.BattleEffectType}");
                    break;
            }

            // Notify listeners
            OnEffectApplied?.Invoke(effect, effectTarget);
        }

        #endregion

        #region Damage Effects

        private void ApplyConfidenceDamage(BattleStats target, int amount)
        {
            int actualDamage = target.DamageConfidence(amount);
            GameLogger.LogInfo<EffectResolver>($"Dealt {actualDamage} Confidence damage");
        }

        private void ApplyEgoDamage(BattleStats target, int amount)
        {
            if (target.CurrentConfidence > 0)
            {
                GameLogger.LogWarning<EffectResolver>("Cannot damage Ego while Confidence is above 0 - applying to Confidence instead");
                ApplyConfidenceDamage(target, amount);
            }
            else
            {
                int actualDamage = target.DamageEgo(amount);
                GameLogger.LogInfo<EffectResolver>($"Dealt {actualDamage} Ego damage");
            }
        }

        #endregion

        #region Healing Effects

        private void ApplyConfidenceRestore(BattleStats target, int amount)
        {
            int actualHealing = target.RestoreConfidence(amount);
            GameLogger.LogInfo<EffectResolver>($"Restored {actualHealing} Confidence");
        }

        private void ApplyEgoRestore(BattleStats target, int amount)
        {
            int actualHealing = target.RestoreEgo(amount);
            GameLogger.LogInfo<EffectResolver>($"Restored {actualHealing} Ego");
        }

        #endregion

        #region Resource Effects

        private void ApplyGainActionPoints(BattleStats target, int amount)
        {
            target.GainActionPoints(amount);
            GameLogger.LogInfo<EffectResolver>($"Gained {amount} Action Points");
        }

        private void ApplyGainBlock(BattleStats target, int amount)
        {
            target.GainBlock(amount);
            GameLogger.LogInfo<EffectResolver>($"Gained {amount} Block");
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

        private void ApplyDestroyCard(DeckManager targetDeck)
        {
            // TODO: Implement card destruction (permanent removal)
            GameLogger.LogWarning<EffectResolver>("DestroyCard not yet implemented");
        }

        private void ApplyExhaustCard(DeckManager deck)
        {
            // Exhaust the card that was just played (handled by DeckManager)
            GameLogger.LogInfo<EffectResolver>("Card will be exhausted after play");
        }

        #endregion

        #region Status Effects

        private void ApplyBuff(BattleStats target, int amount)
        {
            // TODO: Implement buff system
            GameLogger.LogWarning<EffectResolver>($"Buff system not yet implemented (amount: {amount})");
        }

        private void ApplyDebuff(BattleStats target, int amount)
        {
            // TODO: Implement debuff system
            GameLogger.LogWarning<EffectResolver>($"Debuff system not yet implemented (amount: {amount})");
        }

        #endregion

        #region Utility

        /// <summary>
        /// Checks if an effect can be applied (e.g., enough resources, valid target).
        /// </summary>
        public bool CanApplyEffect(CardEffect effect, bool isPlayerCard)
        {
            // Basic validation
            if (effect.EffectContext != EffectContext.Battle)
            {
                return false;
            }

            // Add more complex validation as needed
            return true;
        }

        #endregion
    }
}
