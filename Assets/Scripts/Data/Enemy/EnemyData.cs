using System.Collections.Generic;
using UnityEngine;
using Crookedile.Data;
using Crookedile.Gameplay.Battle;

namespace Crookedile.Data.Enemy
{
    /// <summary>
    /// Controls the order in which an enemy cycles through their moves.
    /// </summary>
    public enum EnemyMovePattern
    {
        /// <summary>Cycles through moves in order: 0 → 1 → 2 → 0 → 1 → 2 …</summary>
        Sequential,

        /// <summary>Picks any move at random each turn.</summary>
        Random,

        /// <summary>Like Sequential but picks a random starting offset — then cycles in order from there.</summary>
        RandomSequential
    }

    /// <summary>
    /// Defines an enemy — their stats and the set of moves they can perform.
    /// Enemies do not have a card deck or Action Points; they execute scripted moves
    /// from their move list each turn.
    ///
    /// Create via: Right-click → Crookedile / Enemy / Enemy Data
    /// </summary>
    [CreateAssetMenu(fileName = "New Enemy", menuName = "Crookedile/Enemy/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        // ─── Identity ─────────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Display name shown in the battle UI")]
        [SerializeField] private string _enemyName = "Unknown Enemy";

        [Tooltip("Portrait or artwork sprite shown in the battle scene")]
        [SerializeField] private Sprite _portrait;

        // ─── Stats ────────────────────────────────────────────────────────────────

        [Header("Stats")]
        [Tooltip("Starting and maximum Resolve (HP equivalent). Reaches 0 = enemy defeated.")]
        [SerializeField] private int _maxResolve = 30;

        // Note: Enemies have no Action Points or deck — those systems are player-only.

        // ─── Hostility ────────────────────────────────────────────────────────────

        [Header("Hostility")]
        [Tooltip("Starting position on the hostility number line. " +
                 "Negative = receptive (open to persuasion), 0 = neutral/guarded, positive = hostile.")]
        [SerializeField] private int _startingHostility = 0;

        [Tooltip("Maximum hostile level this enemy can reach (positive). Default 5.")]
        [SerializeField] private int _maxHostility = 5;

        [Tooltip("Maximum receptive level this enemy can reach (negative). Default -3.")]
        [SerializeField] private int _minHostility = -3;

        // ─── Demographics ─────────────────────────────────────────────────────────

        [Header("Demographics")]
        [Tooltip("Socioeconomic class of this NPC. Used for demographic targeting by Policy cards.")]
        [SerializeField] private DemographicClass _demographicClass = DemographicClass.Middle;

        [Tooltip("Political values of this NPC. Determines how Policy card leans affect their hostility.\n" +
                 "Left cards: Progressive −1, Traditional +1\n" +
                 "Center cards: Moderate −1\n" +
                 "Right cards: Traditional −1, Progressive +1")]
        [SerializeField] private DemographicValues _demographicValues = DemographicValues.Moderate;

        // ─── Starting Status Effects ───────────────────────────────────────────────

        [Header("Starting Effects")]
        [Tooltip("Status effects (buffs or debuffs) applied to this enemy at the start of every battle.")]
        [SerializeField] private List<StatusEffect> _startingEffects = new List<StatusEffect>();

        // ─── Move Set ─────────────────────────────────────────────────────────────

        [Header("Move Set")]
        [Tooltip("How the enemy selects their move each turn.")]
        [SerializeField] private EnemyMovePattern _movePattern = EnemyMovePattern.Sequential;

        [Tooltip("The moves this enemy can perform. Must have at least one entry. " +
                 "For Sequential pattern, moves play in order 0 → 1 → 2 → 0 …")]
        [SerializeField] private List<EnemyMoveData> _moves = new List<EnemyMoveData>();

        // ─── Properties ───────────────────────────────────────────────────────────

        public string           EnemyName         => _enemyName;
        public Sprite           Portrait           => _portrait;
        public int              MaxResolve         => _maxResolve;
        public int              StartingHostility  => _startingHostility;
        public int              MaxHostility       => _maxHostility;
        public int              MinHostility       => _minHostility;
        public DemographicClass  DemographicClass  => _demographicClass;
        public DemographicValues DemographicValues => _demographicValues;
        public EnemyMovePattern MovePattern        => _movePattern;
        public IReadOnlyList<EnemyMoveData> Moves  => _moves;
        public IReadOnlyList<StatusEffect> StartingEffects => _startingEffects;
    }
}
