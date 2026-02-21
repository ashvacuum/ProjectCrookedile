using System.Collections.Generic;
using UnityEngine;
using Crookedile.Data.Cards;

namespace Crookedile.Data.Enemy
{
    /// <summary>
    /// The four broad categories of enemy intent, used to colour-code the intent display
    /// and give the player a quick read on what the enemy is about to do.
    /// </summary>
    public enum EnemyMoveType
    {
        Attack,  // Deals damage or applies debuffs to the player
        Defend,  // Gains Composure or heals itself
        Buff,    // Applies a buff to itself
        Debuff   // Applies a debuff to the player without dealing direct damage
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

        [Tooltip("Optional icon shown in the intent panel. " +
                 "Leave null to show only text.")]
        [SerializeField] private Sprite _intentIcon;

        // ─── Effects ──────────────────────────────────────────────────────────────

        [Header("Effects")]
        [Tooltip("The effects that execute when this move is played. " +
                 "Uses the same CardEffect system as player cards. " +
                 "Avoid CardManipulation effects — enemies have no deck.")]
        [SerializeField] private List<CardEffect> _effects = new List<CardEffect>();

        // ─── Properties ───────────────────────────────────────────────────────────

        public string       MoveName           => _moveName;
        public EnemyMoveType MoveType          => _moveType;
        public string       IntentDescription  => _intentDescription;
        public Sprite       IntentIcon         => _intentIcon;
        public IReadOnlyList<CardEffect> Effects => _effects;
    }
}
