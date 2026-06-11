using System.Collections.Generic;
using Crookedile.Data.Cards;
using Crookedile.Data.VFX;
using Crookedile.Gameplay.Battle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data.Enemy
{
    /// <summary>
    /// Enemy intent categories, used to drive the intent display (icon and colour) and give
    /// the player a quick read on what the enemy is about to do.
    /// Integer assignments are explicit to preserve existing .asset serialization.
    /// When adding new values, always append with the next integer — never reorder.
    /// </summary>
    public enum EnemyMoveType
    {
        Attack = 0, // Pure damage or debuffs to the player
        Defend = 1, // Gains Shield or heals itself
        Buff = 2, // Applies a self-buff only
        Debuff = 3, // Applies a debuff to the player without direct damage
        OffensiveBuff = 4, // Attacks AND buffs itself in the same move
        DebuffAttack = 5, // Debuffs the player AND deals damage
        SummonMinion = 6, // Spawns a new enemy mid-battle (enemy-exclusive)
        Idle = 7, // Does nothing this turn — waits or holds position
        DefendOpinion = 8, // Gains Denial to shield the opinion meter from the player's pressure
        RileOthers = 9, // Raises the other enemies' Hostility, amplifying their attacks
    }

    /// <summary>
    /// Determines when a move is included in the selection pool.
    /// Checked each time SelectNextMove() is called, so conditions that change mid-battle
    /// (e.g. living minion count) are always evaluated on the latest state.
    /// </summary>
    public enum EnemyMoveCondition
    {
        None, // Default — move is always eligible
        OnlyIfNoMinionsAlive, // Skip if any living enemy already matches MinionToSummon
        OnTurnOrAfter, // Eligible from turn ConditionTurn onward (Escalator clocks)
        BeforeTurn, // Eligible only before turn ConditionTurn (opening behavior)
        EveryNTurns, // Eligible only on turns divisible by ConditionTurn (periodic moves)
    }

    /// <summary>
    /// Which stances a move is usable in. Combinable flags — a move can serve two stances
    /// (e.g. Neutral | Receptive). Stored value 0 on assets authored before this field
    /// existed is treated as Any by the selection filter, so old data keeps working.
    /// </summary>
    [System.Flags]
    public enum MoveStanceMask
    {
        Hostile = 1 << 0, // Usable while hostility > 0
        Neutral = 1 << 1, // Usable while hostility == 0
        Receptive = 1 << 2, // Usable while hostility < 0
        Any = Hostile | Neutral | Receptive,
    }

    /// <summary>
    /// One scripted move an enemy can perform on their turn.
    /// Effects use the polymorphic BattleEffect system — EffectResolver handles them
    /// with isPlayerCard=false (enemy is caster, player is target).
    ///
    /// Create via: Right-click → Crookedile / Enemy / Enemy Move
    /// </summary>
    [CreateAssetMenu(fileName = "New Enemy Move", menuName = "Crookedile/Enemy/Enemy Move")]
    public class EnemyMoveData : ScriptableObject
    {
        #region Identity
        [Header("Identity")]
        [Tooltip("Internal name of this move, e.g. 'Aggressive Debate'")]
        [SerializeField]
        private string _moveName;

        [Tooltip("Broad category — used to colour-code the intent badge in the UI")]
        [SerializeField]
        private EnemyMoveType _moveType = EnemyMoveType.Attack;

        #endregion

        #region Intent Description
        [Header("Intent")]
        [Tooltip(
            "Short description shown to the player before the enemy acts. "
                + "e.g. 'Will deal 8 damage' or 'Will gain 4 Denial'"
        )]
        [TextArea(2, 3)]
        [SerializeField]
        private string _intentDescription;

        #endregion

        #region Effects
        [Header("Effects")]
        [Tooltip("Polymorphic effect list. Avoid CardManipulation effects — enemies have no deck.")]
        [SerializeReference]
        [SerializeField]
        private List<BattleEffect> _effects = new List<BattleEffect>();

        #endregion

        #region VFX
        [Header("VFX")]
        [Tooltip(
            "Optional VFX to play on the player slot when this move executes. "
                + "Non-blocking — damage resolves alongside the animation."
        )]
        [SerializeField]
        private VFXEvent _moveVFX;

        #endregion

        #region Summon
        [Header("Summon")]
        [ShowIf("_moveType", EnemyMoveType.SummonMinion)]
        [Tooltip("The enemy definition to spawn when this move executes.")]
        [SerializeField]
        private EnemyData _minionToSummon;

        [ShowIf("_moveType", EnemyMoveType.SummonMinion)]
        [MinValue(1)]
        [Tooltip("How many copies of the minion to summon (capped to keep total enemies ≤ 5).")]
        [SerializeField]
        private int _minionCount = 1;

        #endregion

        #region Selection Condition
        [Header("Condition")]
        [Tooltip(
            "When this move is included in the selection pool.\n\n"
                + "None: always eligible.\n"
                + "OnlyIfNoMinionsAlive: only selected when no living enemy matches MinionToSummon "
                + "(boss re-summons minions only after they have all been killed).\n"
                + "OnTurnOrAfter / BeforeTurn / EveryNTurns: turn-gated — see Condition Turn below."
        )]
        [SerializeField]
        private EnemyMoveCondition _condition = EnemyMoveCondition.None;

        [ShowIf(
            "@_condition == EnemyMoveCondition.OnTurnOrAfter || "
                + "_condition == EnemyMoveCondition.BeforeTurn || "
                + "_condition == EnemyMoveCondition.EveryNTurns"
        )]
        [MinValue(1)]
        [Tooltip(
            "Turn parameter for the selected condition.\n"
                + "OnTurnOrAfter: first turn the move becomes eligible.\n"
                + "BeforeTurn: last eligible turn is this turn minus one.\n"
                + "EveryNTurns: eligible on turns divisible by this value."
        )]
        [SerializeField]
        private int _conditionTurn = 1;

        [Tooltip(
            "Which stances this move is usable in. The enemy only picks moves matching its "
                + "current stance; if none match, all condition-eligible moves are used as a "
                + "fallback so the enemy is never stuck."
        )]
        [SerializeField]
        private MoveStanceMask _stanceRequirement = MoveStanceMask.Any;

        #endregion

        #region Properties
        public string MoveName => _moveName;
        public EnemyMoveType MoveType => _moveType;
        public string IntentDescription => _intentDescription;

        /// <summary>Polymorphic effect list for this enemy move.</summary>
        public List<BattleEffect> Effects => _effects;

        /// <summary>
        /// Auto-generated hover tooltip text — one line per effect.
        /// Uses the new effect list when populated, otherwise the legacy list.
        /// </summary>
        public string Description
        {
            get
            {
                if (_effects == null || _effects.Count == 0)
                    return string.Empty;
                var lines = new string[_effects.Count];
                for (int i = 0; i < _effects.Count; i++)
                    lines[i] = _effects[i]?.GetDescription() ?? string.Empty;
                return string.Join("\n", lines);
            }
        }
        public VFXEvent MoveVFX => _moveVFX;
        public EnemyData MinionToSummon => _minionToSummon;
        public int MinionCount => _minionCount;
        public EnemyMoveCondition Condition => _condition;
        public int ConditionTurn => _conditionTurn;

        /// <summary>
        /// Stance gate, with the value 0 (assets serialized before this field existed)
        /// normalized to <see cref="MoveStanceMask.Any"/>.
        /// </summary>
        public MoveStanceMask StanceRequirement =>
            _stanceRequirement == 0 ? MoveStanceMask.Any : _stanceRequirement;
    }
}
        #endregion
