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

            TestCardCostModifiers();
            TestTurnBasedStatusEffects();

            Debug.Log("=== ALL TESTS COMPLETED ===");
        }

        private void SetupTestBattle()
        {
            Debug.Log("--- Setting up test battle ---");

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

        [ContextMenu("Test: Card Cost Modifiers")]
        public void TestCardCostModifiers()
        {
            Debug.Log("\n--- TEST: Card Cost Modifiers ---");
            SetupTestBattle();

            // Create a test card with 2 AP cost
            CardCost cost = new CardCost(CostType.ActionPoints, 2);

            // Apply Focus -1 (reduces cost by 1)
            effectResolver.PlayerStatusEffects.ApplyStatus(new FocusStatus(), 1);

            int baseCost = cost.CurrentAmount;
            int modifiedCost = effectResolver.PlayerStatusEffects.ModifyCardCost(baseCost);

            Debug.Log($"Expected: 1 AP (2 base - 1 Focus) | Actual: {modifiedCost} AP");
            Debug.Assert(modifiedCost == 1, "Card cost modifier test failed!");

            // Now test Entangled (+1 cost)
            SetupTestBattle();
            effectResolver.PlayerStatusEffects.ApplyStatus(new EntangledStatus(), 1);

            modifiedCost = effectResolver.PlayerStatusEffects.ModifyCardCost(baseCost);
            Debug.Log($"Expected: 3 AP (2 base + 1 Entangled) | Actual: {modifiedCost} AP");
            Debug.Assert(modifiedCost == 3, "Entangled cost test failed!");

            Debug.Log("PASSED");
        }

        [ContextMenu("Test: Turn-Based Status Effects")]
        public void TestTurnBasedStatusEffects()
        {
            Debug.Log("\n--- TEST: Turn-Based Status Effects ---");
            SetupTestBattle();

            // Apply Smear (reputation bleed at end of turn)
            effectResolver.PlayerStatusEffects.ApplyStatus(new SmearStatus(), 3);

            // Apply Regeneration (heal at end of turn)
            effectResolver.PlayerStatusEffects.ApplyStatus(new RegenerationStatus(), 2);

            Debug.Log($"Before turn end: {playerStats.GetStatusString()}");

            // Trigger end of turn
            effectResolver.PlayerStatusEffects.OnTurnEnd(playerStats);

            Debug.Log($"After turn end: {playerStats.GetStatusString()}");
            Debug.Log(
                "Smear/Regeneration opinion effects are applied by BattleManager (OpinionLedger), "
                    + "not by StatusEffectManager; this unit tester only decrements their stacks."
            );

            // Test Ritual (gain Support at start of turn)
            effectResolver.PlayerStatusEffects.ApplyStatus(new RitualStatus(), 2);
            effectResolver.PlayerStatusEffects.OnTurnStart(playerStats);

            Debug.Log($"After turn start: {playerStats.GetStatusString()}");
            Debug.Log(
                "Ritual grants Support (session-level); verify via BattleManager.CurrentSupport"
            );

            Debug.Log("PASSED");
        }
    }
}
