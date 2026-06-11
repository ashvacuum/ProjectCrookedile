using System.Collections.Generic;
using System.Linq;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.UI.Battle;
using UnityEngine;

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
        [SerializeField]
        private OriginType playerOrigin = OriginType.FaithLeader;

        [Tooltip(
            "Central OriginDatabase asset — source of the player's max AP and portrait. "
                + "If null, defaults to 3 AP and no portrait."
        )]
        [SerializeField]
        private OriginDatabase originDatabase;

        [Header("Scene References")]
        [SerializeField]
        private BattleManager battleManager;

        [SerializeField]
        private BattleUI battleUI;

        [Header("Session")]
        [Tooltip(
            "Battle session asset defining the rounds (enemies, turn limits, starting opinion). "
                + "REQUIRED — for a quick single-fight test, make a one-round session."
        )]
        [SerializeField]
        private BattleSession battleSession;

        [Header("Settings")]
        [Tooltip("Automatically start the battle when the scene loads")]
        [SerializeField]
        private bool startOnAwake = true;

        #region Deck Definitions
        // Each entry is (assetName, count). Name must match the .asset filename in Resources/Cards/.

        private static readonly (string name, int count)[] FaithLeaderDeck =
        {
            ("Find Common Ground", 4),
            ("Blessing", 2),
            ("Accusation", 2),
            ("Deflect", 1),
            ("Gather Thoughts", 1),
        };

        private static readonly (string name, int count)[] NepoBabyDeck =
        {
            ("Family Name", 2),
            ("Inherited Privelege", 1), // Note: typo matches the asset filename
            ("Pull Strings", 2),
            ("Call In Favor", 2),
            ("Backroom Deal", 1),
            ("Dynasty Network", 1),
            ("Trust Fund", 1),
        };

        private static readonly (string name, int count)[] ActorDeck =
        {
            ("Charming Gambit", 2),
            ("All or Nothing", 1),
            ("Bold Accusation", 2),
            ("Spotlight Hog", 2),
            ("High Stakes", 1),
            ("Ego Trip", 1),
            ("Fan Favorite", 1),
        };

        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (startOnAwake)
                StartTestBattle();
        }

        #endregion

        #region Public API
        /// <summary>
        /// Builds the player deck from Resources and starts the battle against the assigned enemy.
        /// Can also be called from a UI button or other test harness.
        /// </summary>
        public void StartTestBattle()
        {
            if (battleSession == null || battleSession.RoundCount == 0)
            {
                Debug.LogError(
                    "[BattleTestStarter] No BattleSession assigned (or it has no rounds). "
                        + "Create one via Right-click → Crookedile / Battle Session and assign it."
                );
                return;
            }

            List<CardData> playerDeck;
            List<EnemyData> battleEnemies;

            if (RunState.Current == null)
            {
        #endregion

                #region First battle of the run
                // Build the starter deck from the hardcoded template and initialise RunState.

                CardData[] allCards = Resources.LoadAll<CardData>("Cards");

                if (allCards == null || allCards.Length == 0)
                {
                    Debug.LogError(
                        "[BattleTestStarter] No CardData assets found in Resources/Cards/. "
                            + "Make sure all card .asset files are in Assets/Resources/Cards/."
                    );
                    return;
                }

                Debug.Log($"[BattleTestStarter] Loaded {allCards.Length} card assets.");

                playerDeck = BuildDeck(playerOrigin, allCards);

                if (playerDeck.Count == 0)
                {
                    Debug.LogError(
                        "[BattleTestStarter] Player deck could not be built. "
                            + "Check the warnings above for missing card names."
                    );
                    return;
                }

                List<List<EnemyData>> battleQueue = battleSession.BuildBattleQueue();
                Debug.Log(
                    $"[BattleTestStarter] Session '{battleSession.name}' — "
                        + $"{battleQueue.Count} rounds."
                );

                RunState.Create(playerOrigin, playerDeck, battleQueue);
                Debug.Log($"[BattleTestStarter] RunState created for origin: {playerOrigin}");
            }
            else
            {
                #endregion

                #region Returning from a reward screen
                // RunState already holds the updated deck, HP, and battle index.
                playerOrigin = RunState.Current.Origin;
                playerDeck = RunState.Current.Deck;
                Debug.Log(
                    $"[BattleTestStarter] Continuing run — deck has {playerDeck.Count} cards, "
                        + $"battle index {RunState.Current.CurrentBattleIndex}."
                );
            }

            // Resolve this battle's enemy list from RunState's queue.
            battleEnemies =
                RunState.Current?.CurrentBattleEnemies?.Where(e => e != null).ToList()
                ?? new List<EnemyData>();

            if (battleEnemies.Count == 0)
            {
                Debug.LogError(
                    "[BattleTestStarter] No valid enemies for this battle — "
                        + "check the session asset's rounds."
                );
                return;
            }

            Debug.Log(
                $"[BattleTestStarter] Player ({playerOrigin}): {playerDeck.Count} cards | "
                    + $"Enemies: {string.Join(", ", battleEnemies.Select(e => e.EnemyName))}"
            );

            // Resolve per-round Opinion Meter settings from the session.
            int battleIndex = RunState.Current?.CurrentBattleIndex ?? 0;
            BattleSession.BattleRound currentRound = battleSession.GetRound(battleIndex);

            int roundMaxTurns = currentRound != null ? currentRound.maxTurns : 5;
            int roundStartOpinion = currentRound != null ? currentRound.startingOpinion : 50;

            // Assemble BattleSetup
            var setup = new BattleSetup
            {
                playerOrigin = playerOrigin,
                originDatabase = originDatabase,
                playerDeck = playerDeck,
                enemies = battleEnemies,
                maxTurns = roundMaxTurns > 0 ? roundMaxTurns : (int?)null,
                startingOpinion = roundStartOpinion,
            };

            Debug.Log(
                $"[BattleTestStarter] Opinion Meter: start={roundStartOpinion}, "
                    + $"maxTurns={(roundMaxTurns > 0 ? roundMaxTurns.ToString() : "none")} "
                    + $"(source: {(currentRound != null ? $"session round '{currentRound.label}'" : "defaults — round index out of range")})"
            );

            // Wire BattleUI before starting (BattleUI needs BattleManager reference)
            if (battleUI != null)
                battleUI.Initialize(battleManager);

            // Fire!
            battleManager.StartBattle(setup);
        }

                #endregion

        #region Deck Builder
        private List<CardData> BuildDeck(OriginType origin, CardData[] allCards)
        {
            (string name, int count)[] template = origin switch
            {
                OriginType.FaithLeader => FaithLeaderDeck,
                OriginType.NepoBaby => NepoBabyDeck,
                OriginType.Actor => ActorDeck,
                _ => System.Array.Empty<(string, int)>(),
            };

            if (template.Length == 0)
            {
                Debug.LogError(
                    $"[BattleTestStarter] No deck template defined for origin: {origin}"
                );
                return new List<CardData>();
            }

            var deck = new List<CardData>();

            foreach (var (cardName, count) in template)
            {
                // Match by asset filename (c.name) first, then by CardData.CardName field
                CardData found = System.Array.Find(
                    allCards,
                    c => c.name == cardName || c.CardName == cardName
                );

                if (found == null)
                {
                    Debug.LogWarning(
                        $"[BattleTestStarter] Card not found: '{cardName}' "
                            + $"(origin: {origin}). Skipping."
                    );
                    continue;
                }

                for (int i = 0; i < count; i++)
                    deck.Add(found);
            }

            return deck;
        }
    }
}
        #endregion
