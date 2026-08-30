using System;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Crookedile.Data.VFX;
using UnityEngine;

namespace Crookedile.Data.Audio
{
    #region Trigger Enum
    /// <summary>
    /// Every battle moment that can trigger audio and/or visual feedback.
    /// Used as the key in <see cref="BattleSoundMap"/> to look up the correct
    /// <see cref="AudioEvent"/> and <see cref="VFXEvent"/> pair.
    /// </summary>
    public enum BattleAudioTrigger
    {
        // Lifecycle
        BattleStart,
        BattleVictory,
        BattleDefeat,

        // Turns
        PlayerTurnStart,
        OpponentTurnStart,

        // Cards
        CardPlayed,
        CardDrawn,
        CardDiscarded,
        CardExhausted,

        // Combat
        DamageDealtToPlayer,
        DamageDealtToEnemy,
        HealApplied,
        StatusEffectApplied,
        EnemyDefeated,

        // Enemy state
        EnemyIntentDeclared,
        EnemyHostilityChanged,

        // Resources
        SupportGained,
        SupportLost,
        APSpent,
        APGained,
    }

    #endregion

    #region ScriptableObject
    /// <summary>
    /// Maps every <see cref="BattleAudioTrigger"/> to an optional audio clip and/or visual effect.
    ///
    /// Configure once in the Inspector; leave any field null to silence that trigger.
    /// <see cref="BattleFeedbackController"/> calls <see cref="TryGet"/> at runtime to look up the pair.
    ///
    /// Create via:  Assets → Create → Crookedile → Audio → Battle Sound Map
    /// </summary>
    [CreateAssetMenu(menuName = "Crookedile/Audio/Battle Sound Map", fileName = "BattleSoundMap")]
    public class BattleSoundMap : ScriptableObject
    {
    #endregion

        #region Inspector Types
        [Serializable]
        public struct Entry
        {
            [Tooltip("The battle moment this entry responds to.")]
            public BattleAudioTrigger Trigger;

            [Tooltip("Audio to play when the trigger fires. Leave null for silence.")]
            public AudioEvent Sound;

            [Tooltip("Visual effect to play when the trigger fires. Leave null for no VFX.")]
            public VFXEvent Visual;
        }

        #endregion

        #region Inspector Fields
        [Tooltip("One entry per battle trigger. Triggers without an entry are silently ignored.")]
        [TableList(ShowIndexLabels = false, DefaultExpandedState = true)]
        [SerializeField]
        private Entry[] _entries;

        #endregion

        #region Runtime
        private Dictionary<BattleAudioTrigger, Entry> _map;

        private void OnEnable() => BuildMap();

        private void BuildMap()
        {
            _map = new Dictionary<BattleAudioTrigger, Entry>();
            if (_entries == null)
                return;

            foreach (var entry in _entries)
            {
                if (_map.ContainsKey(entry.Trigger))
                    Debug.LogWarning(
                        $"[BattleSoundMap] Duplicate trigger '{entry.Trigger}' in '{name}' — skipping.",
                        this
                    );
                else
                    _map[entry.Trigger] = entry;
            }
        }

        #endregion

        #region Public API
        /// <summary>
        /// Returns true and populates <paramref name="entry"/> if the trigger has a mapping.
        /// Returns false (no audio/VFX) if the trigger is not in the map.
        /// </summary>
        public bool TryGet(BattleAudioTrigger trigger, out Entry entry)
        {
            if (_map == null)
                BuildMap();
            return _map.TryGetValue(trigger, out entry);
        }
        #endregion
    }
}
