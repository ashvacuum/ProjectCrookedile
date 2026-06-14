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
    /// Run bootstrap + quick-start for battle scenes. On first load it creates the
    /// RunState (starter deck + session battle queue); on reloads after a reward pick
    /// it continues the existing run. The back half of this loop lives in
    /// <c>PostBattleFlow</c> (advance index, reload scene) — when the real metagame
    /// lands, this class is promoted to a RunDirector, not deleted.
    ///
    /// Drop on a GameObject alongside BattleManager and BattleUI; assign a
    /// BattleSession and (preferably) the per-origin starter deck lists below.
    /// </summary>
    public class BattleTestStarter : MonoBehaviour
    {
        /// <summary>One starter-deck entry: a direct card asset reference plus copy count.</summary>
        [System.Serializable]
        public class StarterCardEntry
        {
            public CardData card;

            [Min(1)]
            public int count = 1;
        }

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

        [Header("Starter Decks (direct asset references)")]
        [Tooltip(
            "Preferred deck source — rename-proof direct references. When an origin's list is "
                + "empty, falls back to the legacy name-string templates against Resources/Cards/."
        )]
        [SerializeField]
        private List<StarterCardEntry> faithLeaderDeck = new List<StarterCardEntry>();

        [SerializeField]
        private List<StarterCardEntry> nepoBabyDeck = new List<StarterCardEntry>();

        [SerializeField]
        private List<StarterCardEntry> actorDeck = new List<StarterCardEntry>();

        [Header("Settings")]
        [Tooltip("Automatically start the battle when the scene loads")]
        [SerializeField]
        private bool startOnAwake = true;

        #region Legacy name templates (fallback while the serialized lists are unpopulated)
        // Each entry is (assetName, count). Name must match the .asset filename in Resources/Cards/.
        // Fragile by nature (renames break silently) — populate the serialized lists instead.

        private static readonly (string name, int count)[] FaithLeaderTemplate =
        {
            ("Find Common Ground", 4),
            ("Blessing", 2),
            ("Accusation", 2),
            ("Deflect", 1),
            ("Gather Thoughts", 1),
        };

        private static readonly (string name, int count)[] NepoBabyTemplate =
        {
            ("Family Name", 2),
            ("Inherited Privelege", 1), // Note: typo matches the asset filename
            ("Pull Strings", 2),
            ("Call In Favor", 2),
            ("Backroom Deal", 1),
            ("Dynasty Network", 1),
            ("Trust Fund", 1),
        };

        private static readonly (string name, int count)[] ActorTemplate =
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
        /// Ensures a RunState exists (creating it with a starter deck on first load),
        /// assembles this round's BattleSetup, and starts the battle.
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

            if (!EnsureRunState(out List<CardData> playerDeck))
                return;

            BattleSetup setup = BuildSetup(playerDeck);
            if (setup == null)
                return;

            // Wire BattleUI before starting (BattleUI needs the BattleManager reference).
            if (battleUI != null)
                battleUI.Initialize(battleManager);

            battleManager.StartBattle(setup);
        }

        #endregion

        #region Run bootstrap

        /// <summary>
        /// First load: builds the starter deck and creates RunState with the session's
        /// battle queue. Reloads (returning from a reward screen): continues the run —
        /// RunState already holds the updated deck and battle index.
        /// </summary>
        private bool EnsureRunState(out List<CardData> playerDeck)
        {
            // Continue an existing run — but only if it actually carries a deck. A stale or
            // empty RunState (e.g. left over in the editor, or a prior half-built run) would
            // otherwise launch a cardless battle; fall through to a fresh build instead.
            if (RunState.Current != null && RunState.Current.Deck != null && RunState.Current.Deck.Count > 0)
            {
                playerOrigin = RunState.Current.Origin;
                playerDeck = RunState.Current.Deck;
                Debug.Log(
                    $"[BattleTestStarter] Continuing run — deck has {playerDeck.Count} cards, "
                        + $"battle index {RunState.Current.CurrentBattleIndex}."
                );
                return true;
            }

            if (RunState.Current != null)
            {
                Debug.LogWarning(
                    "[BattleTestStarter] RunState exists but its deck is empty — "
                        + "rebuilding a fresh starter deck."
                );
                RunState.Clear();
            }

            playerDeck = BuildDeck(playerOrigin);
            if (playerDeck.Count == 0)
            {
                Debug.LogError(
                    $"[BattleTestStarter] Player deck for {playerOrigin} is empty. "
                        + "Assign card asset references (with count >= 1) to the "
                        + $"{playerOrigin} starter-deck list on BattleTestStarter, "
                        + "and make sure the origin matches the list you filled."
                );
                return false;
            }

            List<List<EnemyData>> battleQueue = battleSession.BuildBattleQueue();
            Debug.Log(
                $"[BattleTestStarter] Session '{battleSession.name}' — {battleQueue.Count} rounds."
            );

            RunState.Create(playerOrigin, playerDeck, battleQueue);
            Debug.Log($"[BattleTestStarter] RunState created for origin: {playerOrigin}");
            return true;
        }

        /// <summary>Resolves this round's enemies + opinion settings into a BattleSetup.</summary>
        private BattleSetup BuildSetup(List<CardData> playerDeck)
        {
            List<EnemyData> battleEnemies =
                RunState.Current?.CurrentBattleEnemies?.Where(e => e != null).ToList()
                ?? new List<EnemyData>();

            if (battleEnemies.Count == 0)
            {
                Debug.LogError(
                    "[BattleTestStarter] No valid enemies for this battle — "
                        + "check the session asset's rounds."
                );
                return null;
            }

            Debug.Log(
                $"[BattleTestStarter] Player ({playerOrigin}): {playerDeck.Count} cards | "
                    + $"Enemies: {string.Join(", ", battleEnemies.Select(e => e.EnemyName))}"
            );

            int battleIndex = RunState.Current?.CurrentBattleIndex ?? 0;
            BattleSession.BattleRound currentRound = battleSession.GetRound(battleIndex);

            int roundMaxTurns = currentRound != null ? currentRound.maxTurns : 5;
            int roundStartOpinion = currentRound != null ? currentRound.startingOpinion : 50;

            Debug.Log(
                $"[BattleTestStarter] Opinion Meter: start={roundStartOpinion}, "
                    + $"maxTurns={(roundMaxTurns > 0 ? roundMaxTurns.ToString() : "none")} "
                    + $"(source: {(currentRound != null ? $"session round '{currentRound.label}'" : "defaults — round index out of range")})"
            );

            return new BattleSetup
            {
                playerOrigin = playerOrigin,
                originDatabase = originDatabase,
                playerDeck = playerDeck,
                enemies = battleEnemies,
                maxTurns = roundMaxTurns > 0 ? roundMaxTurns : (int?)null,
                startingOpinion = roundStartOpinion,
            };
        }

        #endregion

        #region Deck Builder

        /// <summary>
        /// Builds the starter deck for <paramref name="origin"/>. Prefers the serialized
        /// asset-reference list; falls back to the legacy name templates when it's empty.
        /// </summary>
        private List<CardData> BuildDeck(OriginType origin)
        {
            List<StarterCardEntry> entries = origin switch
            {
                OriginType.FaithLeader => faithLeaderDeck,
                OriginType.NepoBaby => nepoBabyDeck,
                OriginType.Actor => actorDeck,
                _ => null,
            };

            if (entries != null && entries.Count > 0)
            {
                var deck = new List<CardData>();
                foreach (var entry in entries)
                {
                    if (entry?.card == null)
                    {
                        Debug.LogWarning(
                            $"[BattleTestStarter] Empty starter-deck entry for {origin} — skipping."
                        );
                        continue;
                    }
                    // Inspector-added list elements can land with count 0 (Unity skips the C#
                    // field initializer), which would silently add zero copies — treat any
                    // non-positive count as a single copy.
                    int copies = Mathf.Max(1, entry.count);
                    for (int i = 0; i < copies; i++)
                        deck.Add(entry.card);
                }
                Debug.Log(
                    $"[BattleTestStarter] Built {origin} deck from serialized list: "
                        + $"{deck.Count} cards ({entries.Count} entries)."
                );
                return deck;
            }

            Debug.LogWarning(
                $"[BattleTestStarter] No serialized starter deck for {origin} — "
                    + "falling back to legacy name templates (populate the list to fix)."
            );
            return BuildDeckFromNameTemplate(origin);
        }

        private List<CardData> BuildDeckFromNameTemplate(OriginType origin)
        {
            (string name, int count)[] template = origin switch
            {
                OriginType.FaithLeader => FaithLeaderTemplate,
                OriginType.NepoBaby => NepoBabyTemplate,
                OriginType.Actor => ActorTemplate,
                _ => System.Array.Empty<(string, int)>(),
            };

            if (template.Length == 0)
            {
                Debug.LogError(
                    $"[BattleTestStarter] No deck template defined for origin: {origin}"
                );
                return new List<CardData>();
            }

            CardData[] allCards = Resources.LoadAll<CardData>("Cards");
            if (allCards == null || allCards.Length == 0)
            {
                Debug.LogError(
                    "[BattleTestStarter] No CardData assets found in Resources/Cards/. "
                        + "Make sure all card .asset files are in Assets/Resources/Cards/."
                );
                return new List<CardData>();
            }

            var deck = new List<CardData>();
            foreach (var (cardName, count) in template)
            {
                // Match by asset filename (c.name) first, then by CardData.CardName field.
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

        #endregion
    }
}
