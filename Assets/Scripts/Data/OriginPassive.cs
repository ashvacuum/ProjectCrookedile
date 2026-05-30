using System.Collections.Generic;
using Crookedile.Gameplay.Battle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// Defines one or more passive abilities for an origin (like Slay the Spire relics).
    /// Each passive is a polymorphic <see cref="BattlePassive"/> with its own trigger,
    /// conditions, and effects — all configured in the Inspector.
    ///
    /// Create via: Assets → Create → Crookedile → Origin Passive
    /// </summary>
    [CreateAssetMenu(fileName = "New Origin Passive", menuName = "Crookedile/Origin Passive")]
    public class OriginPassive : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Name of the passive ability, e.g. 'Discipline'")]
        [SerializeField]
        private string _passiveName;

        [Tooltip("Which origin has this passive")]
        [SerializeField]
        private OriginType _origin;

        [Header("Description")]
        [TextArea(2, 4)]
        [Tooltip("Description shown to the player in the UI")]
        [SerializeField]
        private string _description;

        [Tooltip("Icon representing this passive")]
        [SerializeField]
        private Sprite _icon;

        [Title("Passives")]
        [Tooltip(
            "Polymorphic passives using the BattlePassive + BattleEffect hierarchy.\n"
                + "Add entries here with a trigger, optional conditions, and one or more effects."
        )]
        [SerializeReference]
        [SerializeField]
        private List<BattlePassive> _passives = new List<BattlePassive>();

        #region Properties

        public string PassiveName => _passiveName;
        public OriginType Origin => _origin;
        public string Description => _description;
        public Sprite Icon => _icon;

        /// <summary>Polymorphic passives using the BattlePassive + BattleEffect hierarchy.</summary>
        public IReadOnlyList<BattlePassive> Passives => _passives;

        #endregion

        /// <summary>Returns formatted passive text for UI display.</summary>
        public string GetFormattedText() => $"<b>{_passiveName}</b>\n{_description}";
    }
}
