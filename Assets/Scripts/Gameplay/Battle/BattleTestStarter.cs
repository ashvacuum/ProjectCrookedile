using System.Collections.Generic;
using UnityEngine;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.UI.Battle;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Quick-start script for testing battles in the editor.
    /// Loads CardData assets from Resources/Cards/, assembles the player's starter deck,
    /// and fires BattleManager.StartBattle() — no manual deck setup needed.
    ///
    /// Drop this on a GameObject in your battle test scene alongside BattleManager and BattleUI.
    /// Assign an EnemyData ScriptableObject to enemyData to choose who to fight.
    ///
    /// Create enemy assets via: Right-click → Crookedile / Enemy / Enemy Data
    /// </summary>
    public class BattleTestStarter : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Origin the player will use in this test")]
        [SerializeField] private OriginType playerOrigin = OriginType.FaithLeader;

        [Tooltip("OriginStats ScriptableObject — controls player Resolve/AP per origin. " +
                 "If null, defaults to 20 Resolve / 3 AP.")]
        [SerializeField] private OriginStats originStats;

        [Header("Enemy")]
        [Tooltip("The EnemyData ScriptableObject defining who the player will fight. " +
                 "Create via: Right-click → Crookedile / Enemy / Enemy Data")]
        [SerializeField] private EnemyData enemyData;

        [Header("Scene References")]
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private BattleUI battleUI;

        [Header("Settings")]
        [Tooltip("Automatically start the battle when the scene loads")]
        [SerializeField] private bool startOnAwake = true;

        // ─── Deck Definitions ─────────────────────────────────────────────────────
        // Each entry is (assetName, count). Name must match the .asset filename in Resources/Cards/.

        private static readonly (string name, int count)[] FaithLeaderDeck =
        {
            ("Find Common Ground", 4),
            ("Blessing",           2),
            ("Accusation",         2),
            ("Deflect",            1),
            ("Gather Thoughts",    1),
        };

        private static readonly (string name, int count)[] NepoBabyDeck =
        {
            ("Family Name",         2),
            ("Inherited Privelege", 1),  // Note: typo matches the asset filename
            ("Pull Strings",        2),
            ("Call In Favor",       2),
            ("Backroom Deal",       1),
            ("Dynasty Network",     1),
            ("Trust Fund",          1),
        };

        private static readonly (string name, int count)[] ActorDeck =
        {
            ("Charming Gambit", 2),
            ("All or Nothing",  1),
            ("Bold Accusation", 2),
            ("Spotlight Hog",   2),
            ("High Stakes",     1),
            ("Ego Trip",        1),
            ("Fan Favorite",    1),
        };

        // ─── Unity Lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            if (startOnAwake)
                StartTestBattle();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Builds the player deck from Resources and starts the battle against the assigned enemy.
        /// Can also be called from a UI button or other test harness.
        /// </summary>
        public void StartTestBattle()
        {
            if (enemyData == null)
            {
                Debug.LogError("[BattleTestStarter] No EnemyData assigned. " +
                               "Create one via Right-click → Crookedile / Enemy / Enemy Data " +
                               "and assign it to the enemyData field.");
                return;
            }

            // Load every CardData asset from Resources/Cards/
            CardData[] allCards = Resources.LoadAll<CardData>("Cards");

            if (allCards == null || allCards.Length == 0)
            {
                Debug.LogError("[BattleTestStarter] No CardData assets found in Resources/Cards/. " +
                               "Make sure all card .asset files are in Assets/Resources/Cards/.");
                return;
            }

            Debug.Log($"[BattleTestStarter] Loaded {allCards.Length} card assets.");

            // Build the player deck
            List<CardData> playerDeck = BuildDeck(playerOrigin, allCards);

            if (playerDeck.Count == 0)
            {
                Debug.LogError("[BattleTestStarter] Player deck could not be built. " +
                               "Check the warnings above for missing card names.");
                return;
            }

            Debug.Log($"[BattleTestStarter] Player ({playerOrigin}): {playerDeck.Count} cards | " +
                      $"Enemy: {enemyData.EnemyName} ({enemyData.Moves.Count} moves, {enemyData.MaxResolve} Resolve)");

            // Assemble BattleSetup
            var setup = new BattleSetup
            {
                playerOrigin = playerOrigin,
                originStats  = originStats,   // null → BattleManager defaults to 20 Resolve / 3 AP
                playerDeck   = playerDeck,
                enemyData    = enemyData,
            };

            // Wire BattleUI before starting (BattleUI needs BattleManager reference)
            if (battleUI != null)
                battleUI.Initialize(battleManager);

            // Fire!
            battleManager.StartBattle(setup);
        }

        // ─── Deck Builder ─────────────────────────────────────────────────────────

        private List<CardData> BuildDeck(OriginType origin, CardData[] allCards)
        {
            (string name, int count)[] template = origin switch
            {
                OriginType.FaithLeader => FaithLeaderDeck,
                OriginType.NepoBaby    => NepoBabyDeck,
                OriginType.Actor       => ActorDeck,
                _                      => System.Array.Empty<(string, int)>()
            };

            if (template.Length == 0)
            {
                Debug.LogError($"[BattleTestStarter] No deck template defined for origin: {origin}");
                return new List<CardData>();
            }

            var deck = new List<CardData>();

            foreach (var (cardName, count) in template)
            {
                // Match by asset filename (c.name) first, then by CardData.CardName field
                CardData found = System.Array.Find(allCards,
                    c => c.name == cardName || c.CardName == cardName);

                if (found == null)
                {
                    Debug.LogWarning($"[BattleTestStarter] Card not found: '{cardName}' " +
                                     $"(origin: {origin}). Skipping.");
                    continue;
                }

                for (int i = 0; i < count; i++)
                    deck.Add(found);
            }

            return deck;
        }
    }
}
