using System.Collections.Generic;
using System.Linq;
using Crookedile.Data;
using Crookedile.Gameplay.Battle;
using Sirenix.OdinInspector;
using UnityEngine;

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
        RandomSequential,
    }

    /// <summary>
    /// The enemy's behavioural stance, derived from its current hostility. Each stance
    /// maps to its own move list on <see cref="EnemyData"/>, so an enemy's available moves
    /// are driven entirely by how it currently feels about the player.
    /// </summary>
    public enum EnemyStance
    {
        /// <summary>Hostility &gt; 0 — uses the aggressive move list.</summary>
        Aggressive,

        /// <summary>Hostility == 0 — uses the neutral move list.</summary>
        Neutral,

        /// <summary>Hostility &lt; 0 — uses the receptive move list.</summary>
        Receptive,
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
        #region Identity
        [Header("Identity")]
        [HorizontalGroup("ID")]
        [ReadOnly]
        [HideLabel]
        [Tooltip("Unique identifier for this enemy. Auto-generated GUID.")]
        [SerializeField]
        private string _id;

        [Tooltip("Display name shown in the battle UI")]
        [SerializeField]
        private string _enemyName = "Unknown Enemy";

        [Tooltip("Portrait or artwork sprite shown in the battle scene")]
        [SerializeField]
        private Sprite _portrait;

        #endregion

        #region Hostility
        [Header("Hostility")]
        [Tooltip(
            "Starting position on the hostility number line. "
                + "Negative = receptive (open to persuasion), 0 = neutral/guarded, positive = hostile."
        )]
        [SerializeField]
        private int _startingHostility = 0;

        [Tooltip("Maximum hostile level this enemy can reach (positive). Default 5.")]
        [SerializeField]
        private int _maxHostility = 5;

        [Tooltip("Maximum receptive level this enemy can reach (negative). Default -3.")]
        [SerializeField]
        private int _minHostility = -3;

        #endregion

        #region Starting Status Effects
        [Header("Starting Effects")]
        [Tooltip(
            "Status effects (buffs or debuffs) applied to this enemy at the start of every battle."
        )]
        [SerializeField]
        private List<StartingStatusEntry> _startingEffects = new List<StartingStatusEntry>();

        #endregion

        #region Passives
        [Header("Passives")]
        [Tooltip(
            "Reactive abilities that fire off this enemy's own battle events — hostility rising/"
                + "falling, maxing out, crossing into receptive/hostile, etc. Reuses the same "
                + "trigger/condition/effect system as card and origin passives; TargetType.Self "
                + "and .AllAllies resolve relative to this enemy."
        )]
        [SerializeReference]
        [SerializeField]
        private List<BattlePassive> _passives = new List<BattlePassive>();

        #endregion

        #region Move Set
        [Header("Move Set")]
        [Tooltip("How the enemy selects their move each turn.")]
        [SerializeField]
        private EnemyMovePattern _movePattern = EnemyMovePattern.Sequential;

        [Tooltip(
            "Moves used while the enemy is Aggressive (Hostility > 0). "
                + "For Sequential pattern, moves play in order 0 → 1 → 2 → 0 …"
        )]
        [SerializeField]
        private List<EnemyMoveData> _aggressiveMoves = new List<EnemyMoveData>();

        [Tooltip(
            "Moves used while the enemy is Neutral (Hostility == 0). "
                + "For Sequential pattern, moves play in order 0 → 1 → 2 → 0 …"
        )]
        [SerializeField]
        private List<EnemyMoveData> _neutralMoves = new List<EnemyMoveData>();

        [Tooltip(
            "Moves used while the enemy is Receptive (Hostility < 0). "
                + "For Sequential pattern, moves play in order 0 → 1 → 2 → 0 …"
        )]
        [SerializeField]
        private List<EnemyMoveData> _receptiveMoves = new List<EnemyMoveData>();

        #endregion

        #region Properties
        /// <summary>Unique identifier for this enemy. Auto-generated GUID.</summary>
        public string ID => _id;
        public string EnemyName => _enemyName;
        public Sprite Portrait => _portrait;
        public int StartingHostility => _startingHostility;
        public int MaxHostility => _maxHostility;
        public int MinHostility => _minHostility;
        public EnemyMovePattern MovePattern => _movePattern;

        /// <summary>Moves used while Aggressive (Hostility &gt; 0).</summary>
        public IReadOnlyList<EnemyMoveData> AggressiveMoves => _aggressiveMoves;

        /// <summary>Moves used while Neutral (Hostility == 0).</summary>
        public IReadOnlyList<EnemyMoveData> NeutralMoves => _neutralMoves;

        /// <summary>Moves used while Receptive (Hostility &lt; 0).</summary>
        public IReadOnlyList<EnemyMoveData> ReceptiveMoves => _receptiveMoves;

        /// <summary>
        /// Every authored move across all three stance lists. Allocates a new list each call —
        /// intended for editor tooling and validation, not per-frame gameplay use.
        /// </summary>
        public IReadOnlyList<EnemyMoveData> Moves =>
            _aggressiveMoves.Concat(_neutralMoves).Concat(_receptiveMoves).ToList();

        public IReadOnlyList<StartingStatusEntry> StartingEffects => _startingEffects;

        /// <summary>Reactive passives that fire off this enemy's own battle events.</summary>
        public IReadOnlyList<BattlePassive> Passives => _passives;

        /// <summary>
        /// Returns the move list that backs the given <paramref name="stance"/>.
        /// </summary>
        public IReadOnlyList<EnemyMoveData> GetMovesForStance(EnemyStance stance) =>
            stance switch
            {
                EnemyStance.Aggressive => _aggressiveMoves,
                EnemyStance.Receptive => _receptiveMoves,
                _ => _neutralMoves,
            };

        /// <summary>Copies the enemy ID to the clipboard.</summary>
        [Button("Copy ID", ButtonSizes.Small)]
        [HorizontalGroup("ID", Width = 80)]
        private void CopyIDToClipboard()
        {
            GUIUtility.systemCopyBuffer = _id;
            Debug.Log($"Copied enemy ID to clipboard: {_id}");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                _id = System.Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        private void Reset()
        {
            _id = System.Guid.NewGuid().ToString();
        }
#endif
    }

    /// <summary>
    /// Authoring entry for a status applied at battle start: a polymorphic
    /// <see cref="StatusBehavior"/> (inspector type dropdown) plus stacks and duration.
    /// </summary>
    [System.Serializable]
    public class StartingStatusEntry
    {
        [Tooltip("The status to apply — pick a StatusBehavior subclass from the type dropdown.")]
        [SerializeReference]
        private StatusBehavior _behavior;

        [Tooltip("Number of stacks applied at battle start.")]
        [SerializeField]
        private int _stacks = 1;

        [Tooltip("How the status duration is tracked.")]
        [SerializeField]
        private StatusDurationType _duration = StatusDurationType.Permanent;

        public StatusBehavior Behavior => _behavior;
        public int Stacks => _stacks;
        public StatusDurationType Duration => _duration;
        #endregion
    }
}
