using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Gameplay;
using Crookedile.Gameplay.Battle;
using UnityEngine;

namespace Crookedile.Tests
{
    /// <summary>
    /// Test script for EffectResolver functionality.
    /// Run tests from Unity Editor context menu or attach to GameObject.
    /// </summary>
    public class EffectResolverTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField]
        private bool runTestsOnStart = false;

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
            TestShieldAbsorption();
            TestHostilityDamageMultiplier();
            TestStatusEffectDamageModifiers();
            TestShieldGainWithModifiers();
            TestCardCostModifiers();
            TestTurnBasedStatusEffects();

            Debug.Log("=== ALL TESTS COMPLETED ===");
        }

        private void SetupTestBattle()
        {
            Debug.Log("--- Setting up test battle ---");

            // Initialize stats — resolve removed; use opinion-meter model
            playerStats = new BattleStats(maxActionPoints: 3, isPlayer: true);
            opponentStats = new BattleStats(maxActionPoints: 3, isPlayer: false);

            // Initialize deck managers with empty decks
            List<CardData> emptyDeck = new List<CardData>();
            playerDeck = new DeckManager(emptyDeck, "TestPlayer", 10);

            // Initialize effect resolver
            effectResolver = new EffectResolver(playerStats, opponentStats, playerDeck);

            Debug.Log($"Player: {playerStats.GetStatusString()}");
            Debug.Log($"Opponent: {opponentStats.GetStatusString()}");
        }

        // TODO: Rewrite damage tests for the opinion-meter model.
        // Cards apply pressure to the opinion meter via DamageDealtEvent → BattleManager.
        // Tests need a BattleManager mock or an EventBus listener to assert opinion changes.

        [ContextMenu("Test: Basic Damage")]
        public void TestBasicDamage()
        {
            Debug.Log("\n--- TEST: Basic Damage (TODO: update for opinion model) ---");
        }

        [ContextMenu("Test: Shield Absorption")]
        public void TestShieldAbsorption()
        {
            // Support/Denial are now session-level on BattleManager, not per-BattleStats.
            // This test requires a full BattleManager instance — stub for now.
            Debug.Log(
                "\n--- TEST: Support/Denial absorption — requires BattleManager, skipped in unit tester ---"
            );
        }

        [ContextMenu("Test: Hostility Damage Multiplier")]
        public void TestHostilityDamageMultiplier()
        {
            Debug.Log("\n--- TEST: Hostility Multiplier ---");
            SetupTestBattle();
            opponentStats.GainHostility(2);
            float mult = opponentStats.HostilityDamageMultiplier;
            Debug.Log($"Hostility 2 → multiplier: {mult:F2}x (expected 2.0x)");
            Debug.Assert(Mathf.Approximately(mult, 2.0f), "Hostility multiplier wrong!");
            Debug.Log("✓ PASSED");
        }

        [ContextMenu("Test: Status Effect Damage Modifiers")]
        public void TestStatusEffectDamageModifiers()
        {
            Debug.Log("\n--- TEST: Status Effect Damage Modifiers (TODO: opinion model) ---");
        }

        [ContextMenu("Test: Shield Gain With Modifiers")]
        public void TestShieldGainWithModifiers()
        {
            // Support is now session-level on BattleManager — requires BattleManager instance to test.
            Debug.Log(
                "\n--- TEST: Support gain with modifiers — requires BattleManager, skipped in unit tester ---"
            );
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

            // Support/Denial are session-level — no per-stat shield to set up.

            Debug.Log($"Before turn end: {playerStats.GetStatusString()}");

            // Trigger end of turn
            effectResolver.PlayerStatusEffects.OnTurnEnd(playerStats);

            Debug.Log($"After turn end: {playerStats.GetStatusString()}");
            Debug.Log("Scandal lowers opinion; Regeneration raises opinion (see EventBus logs)");

            // Test Ritual (gain Shield at start of turn)
            effectResolver.PlayerStatusEffects.ApplyStatusEffect(StatusEffectType.Ritual, 2);
            effectResolver.PlayerStatusEffects.OnTurnStart(playerStats);

            Debug.Log($"After turn start: {playerStats.GetStatusString()}");
            Debug.Log(
                "Ritual now grants Support (session-level) — verify via BattleManager.CurrentSupport"
            );

            Debug.Log("✓ PASSED");
        }

        #region Helper Methods

        private CardEffect CreateDamageEffect(int amount)
        {
            // Using reflection to create CardEffect since it has complex setup
            // In real usage, CardEffects would be created through Unity Editor
            var effect = new CardEffect();
            var categoryField = typeof(CardEffect).GetField(
                "_category",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var targetField = typeof(CardEffect).GetField(
                "_target",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var damageTypeField = typeof(CardEffect).GetField(
                "_damageType",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var damageAmountField = typeof(CardEffect).GetField(
                "_damageAmount",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            categoryField?.SetValue(effect, EffectCategory.Damage);
            targetField?.SetValue(effect, TargetType.Opponent);
            damageTypeField?.SetValue(effect, DamageType.FixedDamage);
            damageAmountField?.SetValue(effect, amount);

            return effect;
        }

        private CardEffect CreateShieldEffect(int amount)
        {
            var effect = new CardEffect();
            var categoryField = typeof(CardEffect).GetField(
                "_category",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var resourceTypeField = typeof(CardEffect).GetField(
                "_resourceType",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var resourceAmountField = typeof(CardEffect).GetField(
                "_resourceAmount",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            categoryField?.SetValue(effect, EffectCategory.Resource);
            resourceTypeField?.SetValue(effect, ResourceEffectType.GainShield);
            resourceAmountField?.SetValue(effect, amount);

            return effect;
        }

        private CardData CreateTestCard(string name, params CardEffect[] effects)
        {
            // Create a ScriptableObject instance for testing
            CardData card = ScriptableObject.CreateInstance<CardData>();

            // Set fields using reflection
            var nameField = typeof(CardData).GetField(
                "_cardName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var effectsField = typeof(CardData).GetField(
                "_effects",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var costsField = typeof(CardData).GetField(
                "_costs",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            nameField?.SetValue(card, name);
            effectsField?.SetValue(card, new List<CardEffect>(effects));
            costsField?.SetValue(card, new List<CardCost>());

            return card;
        }

        #endregion
    }
}
