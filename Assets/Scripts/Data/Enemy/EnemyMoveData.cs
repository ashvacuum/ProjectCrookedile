using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Data.Cards;
using Crookedile.Data.VFX;

namespace Crookedile.Data.Enemy
{
    /// <summary>
    /// Seven broad categories of enemy intent, used to drive the intent display
    /// (icon and colour) and give the player a quick read on what the enemy is about to do.
    /// Integer assignments are explicit to preserve existing .asset serialization.
    /// </summary>
    public enum EnemyMoveType
    {
        Attack        = 0,  // Pure damage or debuffs to the player
        Defend        = 1,  // Gains Composure or heals itself
        Buff          = 2,  // Applies a self-buff only
        Debuff        = 3,  // Applies a debuff to the player without direct damage
        OffensiveBuff = 4,  // Attacks AND buffs itself in the same move
        DebuffAttack  = 5,  // Debuffs the player AND deals damage
        SummonMinion  = 6   // Spawns a new enemy mid-battle (enemy-exclusive)
    }

    /// <summary>
    /// One scripted move an enemy can perform on their turn.
    /// Effects reuse the existing CardEffect system — EffectResolver handles them
    /// with isPlayerCard=false (enemy is caster, player is target).
    ///
    /// Create via: Right-click → Crookedile / Enemy / Enemy Move
    /// </summary>
    [CreateAssetMenu(fileName = "New Enemy Move", menuName = "Crookedile/Enemy/Enemy Move")]
    public class EnemyMoveData : ScriptableObject
    {
        // ─── Identity ─────────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Internal name of this move, e.g. 'Aggressive Debate'")]
        [SerializeField] private string _moveName;

        [Tooltip("Broad category — used to colour-code the intent badge in the UI")]
        [SerializeField] private EnemyMoveType _moveType = EnemyMoveType.Attack;

        // ─── Intent Description ───────────────────────────────────────────────────

        [Header("Intent")]
        [Tooltip("Short description shown to the player before the enemy acts. " +
                 "e.g. 'Will deal 8 damage' or 'Will gain 4 Composure'")]
        [TextArea(2, 3)]
        [SerializeField] private string _intentDescription;

        // ─── Effects ──────────────────────────────────────────────────────────────

        [Header("Effects")]
        [Tooltip("The effects that execute when this move is played. " +
                 "Uses the same CardEffect system as player cards. " +
                 "Avoid CardManipulation effects — enemies have no deck.")]
        [SerializeField] private List<CardEffect> _effects = new List<CardEffect>();

        // ─── VFX ─────────────────────────────────────────────────────────────────

        [Header("VFX")]
        [Tooltip("Optional VFX to play on the player slot when this move executes. " +
                 "Non-blocking — damage resolves alongside the animation.")]
        [SerializeField] private VFXEvent _moveVFX;

        // ─── Summon ───────────────────────────────────────────────────────────────

        [Header("Summon")]
        [ShowIf("_moveType", EnemyMoveType.SummonMinion)]
        [Tooltip("The enemy definition to spawn when this move executes.")]
        [SerializeField] private EnemyData _minionToSummon;

        [ShowIf("_moveType", EnemyMoveType.SummonMinion)]
        [MinValue(1)]
        [Tooltip("How many copies of the minion to summon (capped to keep total enemies ≤ 5).")]
        [SerializeField] private int _minionCount = 1;

        // ─── Properties ───────────────────────────────────────────────────────────

        public string            MoveName          => _moveName;
        public EnemyMoveType     MoveType          => _moveType;
        public string            IntentDescription => _intentDescription;

        /// <summary>
        /// Auto-generated hover tooltip text — one line per effect, built from
        /// <see cref="CardEffect.GetDescription()"/>. Empty when the move has no effects.
        /// </summary>
        public string Description
        {
            get
            {
                if (_effects == null || _effects.Count == 0) return string.Empty;
                var lines = new string[_effects.Count];
                for (int i = 0; i < _effects.Count; i++)
                    lines[i] = _effects[i].GetDescription();
                return string.Join("\n", lines);
            }
        }
        public IReadOnlyList<CardEffect> Effects   => _effects;
        public VFXEvent          MoveVFX           => _moveVFX;
        public EnemyData         MinionToSummon    => _minionToSummon;
        public int               MinionCount       => _minionCount;
    }
}
