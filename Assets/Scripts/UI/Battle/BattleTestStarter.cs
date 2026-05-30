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
            "OriginStats ScriptableObject — controls player Resolve/AP per origin. "
                + "If null, defaults to 20 Resolve / 3 AP."
        )]
        [SerializeField]
        private OriginStats originStats;

        [Header("Enemies")]
        [Tooltip(
            "The enemies present in this room (1–5). "
                + "Create via: Right-click → Crookedile / Enemy / Enemy Data"
        )]
        [SerializeField]
        private List<EnemyData> enemies = new List<EnemyData>();

        [Header("Scene References")]
        [SerializeField]
        private BattleManager battleManager;

        [SerializeField]
        private BattleUI battleUI;

        [Header("Session")]
        [Tooltip(
            "Optional multi-round session asset. If assigned, enemies for each battle are "
                + "taken from here instead of the Enemies list. Leave null for single-round testing."
        )]
        [SerializeField]
        private BattleSession battleSession;

        [Header("Settings")]
        [Tooltip("Automatically start the battle when the scene loads")]
        [SerializeField]
        private bool startOnAwake = true;

        [Tooltip(
            "Maximum player turns before Judgment. Used when no BattleSession is assigned. "
                + "0 = no turn limit. (Sessions define this per-round.)"
        )]
        [SerializeField]
        private int fallbackMaxTurns = 5;

        [Tooltip(
            "Starting Opinion Meter value (0–100). Used when no BattleSession is assigned. "
                + "(Sessions define this per-round.)"
        )]
        [Range(0, 100)]
        [SerializeField]
        private int fallbackStartingOpinion = 50;

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
            // Require at least one enemy source: a session asset OR the fallback enemies list.
            bool hasSession = battleSession != null && battleSession.RoundCount > 0;
            bool hasEnemies = enemies != null && enemies.Count > 0 && enemies.Any(e => e != null);
            if (!hasSession && !hasEnemies)
            {
                Debug.LogError(
                    "[BattleTestStarter] No enemies assigned. "
                        + "Either assign a BattleSession asset or assign enemies to the Enemies list. "
                        + "Create enemies via Right-click → Crookedile / Enemy / Enemy Data."
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

                // Build the battle queue from the session asset (if assigned) or wrap the
                // single enemies list as a one-round queue.
                List<List<EnemyData>> battleQueue;
                if (battleSession != null && battleSession.RoundCount > 0)
                {
                    battleQueue = battleSession.BuildBattleQueue();
                    Debug.Log(
                        $"[BattleTestStarter] Session '{battleSession.name}' — "
                            + $"{battleQueue.Count} rounds."
                    );
                }
                else
                {
                    battleQueue = new List<List<EnemyData>>
                    {
                        enemies.Where(e => e != null).ToList(),
                    };
                    Debug.Log("[BattleTestStarter] No session assigned — single-round fallback.");
                }

                // Starting HP: first battle always starts at the class's max HP.
                int initialResolve =
                    originStats != null
                        ? originStats.GetStatsForOrigin(playerOrigin).maxResolve
                        : 20;

                RunState.Create(playerOrigin, playerDeck, initialResolve, battleQueue);
                Debug.Log(
                    $"[BattleTestStarter] RunState created for origin: {playerOrigin} "
                        + $"({initialResolve} Resolve)"
                );
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
            var queueEnemies = RunState.Current?.CurrentBattleEnemies;
            if (queueEnemies != null && queueEnemies.Count > 0)
            {
                battleEnemies = queueEnemies.Where(e => e != null).ToList();
            }
            else
            {
                // Fallback: use the Inspector enemies list directly (no session assigned).
                battleEnemies = enemies.Where(e => e != null).ToList();
            }

            if (battleEnemies.Count == 0)
            {
                Debug.LogError(
                    "[BattleTestStarter] No valid enemies for this battle. "
                        + "Check the session asset or the Enemies list."
                );
                return;
            }

            Debug.Log(
                $"[BattleTestStarter] Player ({playerOrigin}): {playerDeck.Count} cards | "
                    + $"Enemies: {string.Join(", ", battleEnemies.Select(e => e.EnemyName))}"
            );

            // Resolve per-round Opinion Meter settings from the session, or fall back to inspector values.
            int battleIndex = RunState.Current?.CurrentBattleIndex ?? 0;
            BattleSession.BattleRound currentRound = battleSession?.GetRound(battleIndex);

            int roundMaxTurns = currentRound != null ? currentRound.maxTurns : fallbackMaxTurns;
            int roundStartOpinion =
                currentRound != null ? currentRound.startingOpinion : fallbackStartingOpinion;

            // Assemble BattleSetup
            var setup = new BattleSetup
            {
                playerOrigin    = playerOrigin,
                originStats     = originStats,
                playerDeck      = playerDeck,
                enemies         = battleEnemies,
                maxTurns        = roundMaxTurns > 0 ? roundMaxTurns : (int?)null,
                startingOpinion = roundStartOpinion,
            };

            Debug.Log(
                $"[BattleTestStarter] Opinion Meter: start={roundStartOpinion}, "
                    + $"maxTurns={(roundMaxTurns > 0 ? roundMaxTurns.ToString() : "none")} "
                    + $"(source: {(currentRound != null ? $"session round '{currentRound.label}'" : "fallback inspector values")})"
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
