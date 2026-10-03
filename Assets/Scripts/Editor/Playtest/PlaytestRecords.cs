using System;
using System.Collections.Generic;

namespace Crookedile.EditorTools.Playtest
{
    /// <summary>The room at the start of one player turn (and once more when the battle ends).</summary>
    [Serializable]
    public struct TurnSnapshot
    {
        public int Turn;
        public int Opinion;
        public int MaxOpinion;
        public float AvgHostility; // living enemies
        public int Hostile;
        public int Neutral;
        public int Receptive;
        public int Support;
        public int Denial;
        public bool EchoChamber;
    }

    /// <summary>Target mood at the moment a card was played — the bucket card scores are split by.</summary>
    public enum Mood
    {
        Hostile,
        Neutral,
        Receptive,
    }

    /// <summary>One card play and what it did to the meter and the room.</summary>
    [Serializable]
    public struct CardPlaySample
    {
        public string Card;
        public int Cost;
        public Mood TargetMood; // the focused enemy's stance before the play
        public int TargetHostility;
        public float RoomHostility; // average over living enemies, before the play
        public int OpinionDelta;
        public int RoomHostilityDelta; // summed over living enemies: negative = calmed the room
        public int Turn;
    }

    /// <summary>What one bot-played battle did. One row of battles.csv.</summary>
    [Serializable]
    public class BattleRecord
    {
        public string Context = "battle"; // "battle", or "campaign:<run id>" for fights inside a run
        public string Bot;
        public string Origin;
        public string Encounter;
        public string Enemies;
        public int Seed;
        public int DeckSize;

        public bool Win;
        public bool Judgment; // ended on the turn limit rather than a full/empty meter
        public bool TimedOut; // hit the frame budget
        public bool Softlock; // stopped advancing outside the player's turn
        public int Turns;
        public int StartOpinion;
        public int FinalOpinion;
        public int CardsPlayed;
        public int MaxCardsOneTurn;
        public int RejectedPlays; // CanPlayCard said yes, RequestPlayCard refused
        public int DamageToPlayer;

        public readonly Dictionary<string, int> CardPlays = new();
        public readonly Dictionary<string, int> CardOpinionDelta = new();
        public readonly Dictionary<string, int> CardBestPlay = new();
        public readonly HashSet<string> DeckCards = new();
        public readonly Dictionary<string, int> EnemyMoves = new();
        public readonly List<TurnSnapshot> Snapshots = new();
        public readonly List<CardPlaySample> Plays = new();
        public readonly List<string> Errors = new();
        public readonly List<string> Warnings = new();
    }

    /// <summary>What one bot-played campaign run did. One row of campaigns.csv.</summary>
    [Serializable]
    public class CampaignRecord
    {
        public string Id;
        public string Bot;
        public string Origin;
        public int Seed;

        public string EndReason; // Completed, Defeated, Stuck, Error
        public int DayReached;
        public string DefeatedBy;
        public int Funds;
        public int Credibility;
        public int DeckSize;
        public int Allies;
        public int Battles;
        public int BattlesWon;
        public int Events;
        public int DaysWithNothingToDo;
        public int DaysEndedWithoutHq;

        public readonly List<string> Visited = new();
        public readonly HashSet<string> Offered = new();
        public readonly Dictionary<string, int> OptionsChosen = new();
        public readonly HashSet<string> OptionsSeenLocked = new();
        public readonly List<string> Errors = new();
    }
}
