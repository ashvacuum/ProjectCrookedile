using System;
using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Setup data for initializing a battle.
    /// Player brings a card deck and origin; opponents are one or more scripted enemies.
    /// </summary>
    [Serializable]
    public class BattleSetup
    {
        public OriginType playerOrigin;

        [Tooltip("Central origin database — source of the player's max AP and portrait.")]
        public OriginDatabase originDatabase;

        public List<CardData> playerDeck = new List<CardData>();

        /// <summary>All enemies present in this room (1–5). Order = display order.</summary>
        public List<EnemyData> enemies = new List<EnemyData>();

        /// <summary>Maximum number of player turns before Judgment is called. 0 = no limit.</summary>
        public int? maxTurns;

        /// <summary>Starting Opinion Meter value. When null, defaults to half of maxOpinion.</summary>
        public int? startingOpinion;

        /// <summary>Maximum Opinion Meter value. Defaults to 100.</summary>
        public int? maxOpinion;

        /// <summary>Max AP per turn for the player's origin. Defaults to 3 when unconfigured.</summary>
        public int GetPlayerMaxActionPoints()
        {
            if (
                originDatabase != null
                && originDatabase.TryGet(playerOrigin, out var entry)
                && entry.MaxActionPoints > 0
            )
                return entry.MaxActionPoints;
            return 3;
        }

        /// <summary>Portrait sprite for the player's origin, or null when unconfigured.</summary>
        public Sprite GetPlayerPortrait() =>
            originDatabase != null && originDatabase.TryGet(playerOrigin, out var entry)
                ? entry.Portrait
                : null;
    }

    /// <summary>Result data from a completed battle.</summary>
    [Serializable]
    public class BattleResult
    {
        public bool isVictory;
        public int turnsToWin;
        public int finalPlayerSupport;
        public int finalPlayerHostility;
        public int finalOpinion;
        public bool wasJudgmentVictory;

        // TODO: Add rewards when reward system exists
    }
}
