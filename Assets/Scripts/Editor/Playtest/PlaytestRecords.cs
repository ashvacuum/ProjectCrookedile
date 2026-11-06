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
        public int Fanatics; // following Fanatics in the room before the play

        // Opinion later pushed by Fanatics this play helped make: its share of every follow from an
        // enemy it put Guilt on or converted. Lands after the play, so OpinionDelta misses it.
        public int SetupOpinion;

        /// <summary>Everything this play is worth: its own Opinion plus the Fanatic follows it set up.</summary>
        public int TotalOpinion => OpinionDelta + SetupOpinion;
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
        public int WinLine; // Opinion the turn limit required (BattleManager.JudgmentWinLine)
        public int MaxOpinion = 100; // the meter's size, for win-margin tiers
        public int Day; // campaign day the fight happened on; 0 for standalone battles
        public string Boss; // the rival's name in a boss debate; null in a standard fight
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
        public readonly Dictionary<string, int> BossBundles = new(); // bundle the boss committed to, per turn

        // Utility per card, credited to the card played or the card whose passive fired.
        public readonly Dictionary<string, int> SupportGained = new();
        public readonly Dictionary<string, int> DamagePrevented = new(); // its Support that absorbed an enemy hit
        public readonly Dictionary<string, int> EnergyGained = new(); // mid-turn energy, not the turn refill
        public readonly Dictionary<string, int> CardsEnabled = new(); // later plays that spent that energy
        public readonly Dictionary<string, int> PassiveOpinion = new(); // Opinion its passives raised later
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

        // Credibility is the run's HP. Lost is keyed by encounter, gained by "event / option".
        public readonly Dictionary<string, (int total, int times)> CredibilityLost = new();
        public readonly Dictionary<string, (int total, int times)> CredibilityGained = new();
        public readonly List<int> CredibilityByDay = new(); // at the start of each day reached, day 1 first
        public readonly List<int> DeckByDay = new(); // deck size at the start of each day reached
        public readonly List<int> RaresByDay = new(); // Rare cards in the deck at the start of each day reached
        public int StrongOffers; // win rewards drawn from the strong-win pool (80%+)
        public int RareGuarantees; // win rewards with a guaranteed Rare (maxed meter)
        public int RaresTaken; // Rare cards the bot picked from any reward

        // Time on the map, over paid visits (chains are free and HQ ends the day).
        public int PaidVisits;
        public int TravelMinutes; // includes TrafficMinutes
        public int TrafficMinutes; // travel beyond the clear-road time
        public int WaitMinutes; // arrived before the encounter opened
        public int EncounterMinutes;
        public int DaysEnded;
        public int MinutesLeftAtDayEnd; // summed over DaysEnded

        // Offered locations still unvisited when the day ended, by why they weren't entered.
        public readonly Dictionary<string, int> LeftUnvisited = new();
        public int BlockedByTraffic; // blocked, but the same visit fits on clear roads
    }
}
