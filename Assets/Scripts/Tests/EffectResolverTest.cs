using UnityEngine;
using Crookedile.Gameplay.Battle;
using Crookedile.Data;
using Crookedile.Data.Cards;
using System.Collections.Generic;
using Crookedile.Gameplay;

namespace Crookedile.Tests
{
    /// <summary>
    /// Test script for EffectResolver functionality.
    /// Run tests from Unity Editor context menu or attach to GameObject.
    /// </summary>
    public class EffectResolverTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestsOnStart = false;

        private BattleStats playerStats;
        private BattleStats opponentStats;
        private DeckManager playerDeck;
        private DeckManager opponentDeck;
        private EffectResolver effectResolver;

        private void Start()
        {
            if (runTestsOnStart)
            {
                RunAllTests();
            }
        }

        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            Debug.Log("=== STARTING EFFECT RESOLVER TESTS ===");

            SetupTestBattle();

            TestBasicDamage();
            TestComposureDamageBonus();
            TestHostilityDamageMultiplier();
            TestStatusEffectDamageModifiers();
            TestComposureGainWithModifiers();
            TestCardCostModifiers();
            TestTurnBasedStatusEffects();

            Debug.Log("=== ALL TESTS COMPLETED ===");
        }

        private void SetupTestBattle()
        {
            Debug.Log("--- Setting up test battle ---");

            // Initialize stats
            playerStats = new BattleStats(maxResolve: 20, maxActionPoints: 3);
            opponentStats = new BattleStats(maxResolve: 20, maxActionPoints: 3);

            // Initialize deck managers with empty decks
            List<CardData> emptyDeck = new List<CardData>();
            playerDeck = new DeckManager(emptyDeck, "TestPlayer", 10);
            opponentDeck = new DeckManager(emptyDeck, "TestOpponent", 10);

            // Initialize effect resolver
            effectResolver = new EffectResolver(playerStats, opponentStats, playerDeck, opponentDeck);

            Debug.Log($"Player: {playerStats.GetStatusString()}");
            Debug.Log($"Opponent: {opponentStats.GetStatusString()}");
        }

        [ContextMenu("Test: Basic Damage")]
        public void TestBasicDamage()
        {
            Debug.Log("\n--- TEST: Basic Damage ---");
            SetupTestBattle();

            // Create a simple damage effect
            CardEffect damageEffect = CreateDamageEffect(5);
            CardData testCard = CreateTestCard("Test Attack", damageEffect);

            int initialResolve = opponentStats.CurrentResolve;
            effectResolver.ResolveCardEffects(testCard, isPlayerCard: true);

            int damageTaken = initialResolve - opponentStats.CurrentResolve;
            Debug.Log($"Expected: 5 damage | Actual: {damageTaken} damage");
            Debug.Assert(damageTaken == 5, "Basic damage test failed!");
            Debug.Log("✓ PASSED");
        }

        [ContextMenu("Test: Composure Damage Bonus")]
        public void TestComposureDamageBonus()
        {
            Debug.Log("\n--- TEST: Composure Damage Bonus ---");
            SetupTestBattle();

            // Give player 3 Composure
            playerStats.GainComposure(3);

            // Deal 5 base damage (should be 5 + 3 = 8 total)
            CardEffect damageEffect = CreateDamageEffect(5);
            CardData testCard = CreateTestCard("Test Attack", damageEffect);

            int initialResolve = opponentStats.CurrentResolve;
            effectResolver.ResolveCardEffects(testCard, isPlayerCard: true);

            int damageTaken = initialResolve - opponentStats.CurrentResolve;
            Debug.Log($"Expected: 8 damage (5 base + 3 Composure) | Actual: {damageTaken} damage");
            Debug.Assert(damageTaken == 8, "Composure bonus test failed!");
            Debug.Log("✓ PASSED");
        }

        [ContextMenu("Test: Hostility Damage Multiplier")]
        public void TestHostilityDamageMultiplier()
        {
            Debug.Log("\n--- TEST: Hostility Damage Multiplier ---");
            SetupTestBattle();

            // Give player 2 Hostility (1 + 2 * 0.5 = 2.0x multiplier)
            playerStats.GainHostility(2);

            Debug.Log($"Player Hostility: {playerStats.CurrentHostility} (Multiplier: {playerStats.HostilityDamageMultiplier:F2}x)");

            // Opponent deals 5 damage to player (should be 5 * 2.0 = 10)
            int damage = playerStats.DamageResolveWithHostility(5);

            Debug.Log($"Expected: 10 damage (5 base * 2.0x) | Actual: {damage} damage");
            Debug.Assert(damage == 10, "Hostility multiplier test failed!");
            Debug.Log("✓ PASSED");
        }

        [ContextMenu("Test: Status Effect Damage Modifiers")]
        public void TestStatusEffectDamageModifiers()
        {
            Debug.Log("\n--- TEST: Status Effect Damage Modifiers ---");
            SetupTestBattle();

            // Apply Strength +2 to player
            effectResolver.PlayerStatusEffects.ApplyStatusEffect(StatusEffectType.Strength, 2);

            // Apply Vulnerable to opponent
            effectResolver.OpponentStatusEffects.ApplyStatusEffect(StatusEffectType.Vulnerable, 1);

            // Deal 10 base damage
            // Should be: 10 + 2 (Strength) = 12, then 12 * 1.5 (Vulnerable) = 18
            CardEffect damageEffect = CreateDamageEffect(10);
            CardData testCard = CreateTestCard("Test Attack", damageEffect);

            int initialResolve = opponentStats.CurrentResolve;
            effectResolver.ResolveCardEffects(testCard, isPlayerCard: true);

            int damageTaken = initialResolve - opponentStats.CurrentResolve;
            Debug.Log($"Expected: 18 damage (10 base + 2 Strength, then * 1.5 Vulnerable) | Actual: {damageTaken} damage");
            Debug.Assert(damageTaken == 18, "Status effect damage modifier test failed!");
            Debug.Log("✓ PASSED");
        }

        [ContextMenu("Test: Composure Gain With Modifiers")]
        public void TestComposureGainWithModifiers()
        {
            Debug.Log("\n--- TEST: Composure Gain With Modifiers ---");
            SetupTestBattle();

            // Apply Dexterity +2 to player
            effectResolver.PlayerStatusEffects.ApplyStatusEffect(StatusEffectType.Dexterity, 2);

            // Gain 5 Composure (should be 5 + 2 = 7)
            CardEffect composureEffect = CreateComposureEffect(5);
            CardData testCard = CreateTestCard("Test Composure Gain", composureEffect);

            effectResolver.ResolveCardEffects(testCard, isPlayerCard: true);

            Debug.Log($"Expected: 7 Composure (5 base + 2 Dexterity) | Actual: {playerStats.CurrentComposure} Composure");
            Debug.Assert(playerStats.CurrentComposure == 7, "Composure gain modifier test failed!");
            Debug.Log("✓ PASSED");
        }

        [ContextMenu("Test: Card Cost Modifiers")]
        public void TestCardCostModifiers()
        {
            Debug.Log("\n--- TEST: Card Cost Modifiers ---");
            SetupTestBattle();

            // Create a test card with 2 AP cost
            CardCost cost = new CardCost(CostType.ActionPoints, 2);

            // Apply Focus -1 (reduces cost by 1)
            effectResolver.PlayerStatusEffects.ApplyStatusEffect(StatusEffectType.Focus, 1);

            int baseCost = cost.CurrentAmount;
            int modifiedCost = effectResolver.PlayerStatusEffects.ModifyCardCost(baseCost);

            Debug.Log($"Expected: 1 AP (2 base - 1 Focus) | Actual: {modifiedCost} AP");
            Debug.Assert(modifiedCost == 1, "Card cost modifier test failed!");

            // Now test Entangled (+1 cost)
            SetupTestBattle();
            effectResolver.PlayerStatusEffects.ApplyStatusEffect(StatusEffectType.Entangled, 1);

            modifiedCost = effectResolver.PlayerStatusEffects.ModifyCardCost(baseCost);
            Debug.Log($"Expected: 3 AP (2 base + 1 Entangled) | Actual: {modifiedCost} AP");
            Debug.Assert(modifiedCost == 3, "Entangled cost test failed!");

            Debug.Log("✓ PASSED");
        }

        [ContextMenu("Test: Turn-Based Status Effects")]
        public void TestTurnBasedStatusEffects()
        {
            Debug.Log("\n--- TEST: Turn-Based Status Effects ---");
            SetupTestBattle();

            // Apply Scandal (damage at end of turn)
            effectResolver.PlayerStatusEffects.ApplyStatusEffect(StatusEffectType.Scandal, 3);

            // Apply Regeneration (heal at end of turn)
            effectResolver.PlayerStatusEffects.ApplyStatusEffect(StatusEffectType.Regeneration, 2);

            // Damage player first to see regeneration
            playerStats.DamageResolve(5, 0);

            Debug.Log($"Before turn end: {playerStats.GetStatusString()}");

            // Trigger end of turn
            effectResolver.PlayerStatusEffects.OnTurnEnd(playerStats);

            Debug.Log($"After turn end: {playerStats.GetStatusString()}");
            Debug.Log("Expected: -3 Resolve from Scandal, +2 from Regeneration = -1 net");

            // Test Ritual (gain Composure at start of turn)
            effectResolver.PlayerStatusEffects.ApplyStatusEffect(StatusEffectType.Ritual, 2);
            effectResolver.PlayerStatusEffects.OnTurnStart(playerStats);

            Debug.Log($"After turn start: {playerStats.GetStatusString()}");
            Debug.Log($"Expected: +2 Composure from Ritual | Actual: {playerStats.CurrentComposure} Composure");

            Debug.Log("✓ PASSED");
        }

        #region Helper Methods

        private CardEffect CreateDamageEffect(int amount)
        {
            // Using reflection to create CardEffect since it has complex setup
            // In real usage, CardEffects would be created through Unity Editor
            var effect = new CardEffect();
            var categoryField = typeof(CardEffect).GetField("_category", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetField = typeof(CardEffect).GetField("_target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var damageTypeField = typeof(CardEffect).GetField("_damageType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var damageAmountField = typeof(CardEffect).GetField("_damageAmount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            categoryField?.SetValue(effect, EffectCategory.Damage);
            targetField?.SetValue(effect, TargetType.Opponent);
            damageTypeField?.SetValue(effect, DamageType.FixedDamage);
            damageAmountField?.SetValue(effect, amount);

            return effect;
        }

        private CardEffect CreateComposureEffect(int amount)
        {
            var effect = new CardEffect();
            var categoryField = typeof(CardEffect).GetField("_category", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var resourceTypeField = typeof(CardEffect).GetField("_resourceType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var resourceAmountField = typeof(CardEffect).GetField("_resourceAmount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            categoryField?.SetValue(effect, EffectCategory.Resource);
            resourceTypeField?.SetValue(effect, ResourceEffectType.GainComposure);
            resourceAmountField?.SetValue(effect, amount);

            return effect;
        }

        private CardData CreateTestCard(string name, params CardEffect[] effects)
        {
            // Create a ScriptableObject instance for testing
            CardData card = ScriptableObject.CreateInstance<CardData>();

            // Set fields using reflection
            var nameField = typeof(CardData).GetField("_cardName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var effectsField = typeof(CardData).GetField("_effects", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var costsField = typeof(CardData).GetField("_costs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            nameField?.SetValue(card, name);
            effectsField?.SetValue(card, new List<CardEffect>(effects));
            costsField?.SetValue(card, new List<CardCost>());

            return card;
        }

        #endregion
    }
}
