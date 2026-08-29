using System;
using System.Collections.Generic;
using Crookedile.Data.Enemy;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// An ordered gauntlet of fights — <b>a test-harness construct</b>, not campaign content.
    /// Assign to the <c>battleSession</c> field on <c>BattleTestStarter</c>; if left null
    /// the starter falls back to its single <c>enemies</c> list as a one-round session.
    ///
    /// <para><b>In the campaign, prefer one fight per encounter.</b> A multi-round session can
    /// only chain battle → battle, with nothing possible between the fights. Sequences are built
    /// from a <c>GoToEncounterOutcome</c> on an event choice instead, which can point at a
    /// battle — so "refuse him and fight now" is one option on one event. A
    /// <see cref="Campaign.BattleEncounterData"/> should normally wrap a session of exactly one
    /// round.</para>
    ///
    /// Create via: Right-click → Crookedile / Battle Session
    /// </summary>
    [CreateAssetMenu(fileName = "BattleSession", menuName = "Crookedile/Battle Session")]
    public class BattleSession : ScriptableObject
    {
        /// <summary>
        /// One fight in the session — a label and the enemies the player faces.
        /// </summary>
        [Serializable]
        public class BattleRound
        {
            [Tooltip("Short display label shown in console logs (e.g. \"Round 1 — Town Square\").")]
            public string label = "Round";

            [Tooltip("Enemies present in this fight (1–5). Order = display order.")]
            [ListDrawerSettings(ShowFoldout = false, DraggableItems = true)]
            [ValidateInput(
                "@enemies != null && enemies.Count > 0 && enemies.TrueForAll(e => e != null)",
                "A round with no enemies (or an empty slot) starts a fight against nobody.",
                InfoMessageType.Error
            )]
            public List<EnemyData> enemies = new List<EnemyData>();

            [Space]
            [Tooltip("Maximum player turns before Judgment is called. 0 = no turn limit.")]
            [HorizontalGroup("Limits", LabelWidth = 100)]
            public int maxTurns = 10;

            [Tooltip("Opinion Meter maximum (the win threshold). The meter runs 0..maxOpinion.")]
            [Min(1)]
            [HorizontalGroup("Limits", LabelWidth = 100)]
            public int maxOpinion = 100;

            [Tooltip("Starting Opinion Meter value. Clamped to 0..maxOpinion at battle start.")]
            [PropertyRange(0, "maxOpinion")]
            [ValidateInput(
                "@startingOpinion <= maxOpinion",
                "Starts above the win threshold — the fight is over before a card is played.",
                InfoMessageType.Error
            )]
            public int startingOpinion = 50;

            /// <summary>Row label in the rounds list, so a session reads without expanding it.</summary>
            private string Summary =>
                enemies == null || enemies.Count == 0
                    ? $"{label} — (no enemies)"
                    : $"{label} — {string.Join(", ", enemies.ConvertAll(e => e == null ? "(empty)" : e.name))}";
        }

        [Tooltip(
            "Ordered list of encounters. Player fights them in sequence, collecting rewards between each."
        )]
        // Labelled by the round's own summary so a session reads as "who you fight, in order"
        // without expanding every entry — the question you actually ask of this asset.
        [ListDrawerSettings(ListElementLabelName = "Summary", ShowFoldout = true)]
        [InfoBox(
            "Campaign encounters should wrap exactly one round. A multi-round session can only "
                + "chain battle → battle with nothing between; sequences belong to an event "
                + "choice's GoToEncounterOutcome instead.",
            InfoMessageType.Warning,
            VisibleIf = "@rounds != null && rounds.Count > 1"
        )]
        public List<BattleRound> rounds = new List<BattleRound>();

        /// <summary>Number of rounds defined in this session.</summary>
        public int RoundCount => rounds?.Count ?? 0;

        /// <summary>
        /// Returns the round at <paramref name="index"/>, or <c>null</c> if out of range.
        /// </summary>
        public BattleRound GetRound(int index)
        {
            if (rounds == null || index < 0 || index >= rounds.Count)
                return null;
            return rounds[index];
        }

        /// <summary>
        /// Returns the enemy list for the round at <paramref name="index"/>, or <c>null</c> if out of range.
        /// </summary>
        public List<EnemyData> GetRoundEnemies(int index)
        {
            return GetRound(index)?.enemies;
        }

        /// <summary>
        /// Builds a battle-queue (list of enemy lists) from every round.
        /// </summary>
        public List<List<EnemyData>> BuildBattleQueue()
        {
            var queue = new List<List<EnemyData>>();
            if (rounds == null)
                return queue;
            foreach (var round in rounds)
            {
                if (round != null)
                    queue.Add(new List<EnemyData>(round.enemies ?? new List<EnemyData>()));
            }
            return queue;
        }
    }
}
