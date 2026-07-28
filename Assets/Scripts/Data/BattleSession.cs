using System;
using System.Collections.Generic;
using Crookedile.Data.Enemy;
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
            public List<EnemyData> enemies = new List<EnemyData>();

            [Space]
            [Tooltip("Maximum player turns before Judgment is called. 0 = no turn limit.")]
            public int maxTurns = 10;

            [Tooltip("Opinion Meter maximum (the win threshold). The meter runs 0..maxOpinion.")]
            [Min(1)]
            public int maxOpinion = 100;

            [Tooltip("Starting Opinion Meter value. Clamped to 0..maxOpinion at battle start.")]
            [Min(0)]
            public int startingOpinion = 50;
        }

        [Tooltip(
            "Ordered list of encounters. Player fights them in sequence, collecting rewards between each."
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
