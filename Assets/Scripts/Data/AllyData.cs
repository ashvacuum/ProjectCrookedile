using System.Collections.Generic;
using Crookedile.Gameplay.Battle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// Someone recruited into the campaign, who stays with the run and changes how battles play
    /// — the StS relic slot, framed as a person. Reuses <see cref="BattlePassive"/> the same way
    /// <c>OriginPassive</c> does, so behaviour is data-authored.
    ///
    /// Wired end to end: <c>RecruitAllyOutcome</c> → <c>RunState.Allies</c> → <c>BattleManager</c>
    /// registers the passives each battle. Only content is missing.
    ///
    /// Create via: Assets → Create → Crookedile → Campaign → Ally
    /// </summary>
    [CreateAssetMenu(menuName = "Crookedile/Campaign/Ally", fileName = "New Ally")]
    public class AllyData : ScriptableObject
    {
        [ReadOnly]
        [PropertyOrder(100)]
        [FoldoutGroup("Identity", Expanded = false)]
        [Tooltip("Unique identifier — the asset's own file GUID. Never edit by hand.")]
        [SerializeField]
        private string _id;

        [HorizontalGroup("Ally", 76)]
        [PreviewField(72, ObjectFieldAlignment.Left)]
        [HideLabel]
        [Tooltip("Their face. An ally is a person the player recruited, so show one.")]
        [SerializeField]
        private Sprite _icon;

        [VerticalGroup("Ally/Details")]
        [Required("An unnamed ally reads as a bug in the roster.")]
        [LabelText("Name")]
        [SerializeField]
        private string _allyName;

        [VerticalGroup("Ally/Details")]
        [TextArea(2, 4)]
        [SerializeField]
        private string _description;

        [VerticalGroup("Ally/Details")]
        [EnumToggleButtons]
        [SerializeField]
        private CardRarity _rarity = CardRarity.Basic;

        [Tooltip("The persistent passive(s) this ally grants. Same hierarchy as card/origin passives.")]
        [ValidateInput(
            "@_passives != null && _passives.Count > 0",
            "No passives — recruiting this ally would change nothing about the run.",
            InfoMessageType.Warning
        )]
        [SerializeReference]
        [SerializeField]
        private List<BattlePassive> _passives = new List<BattlePassive>();

        public string Id => _id;
        public string AllyName => _allyName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public CardRarity Rarity => _rarity;
        public IReadOnlyList<BattlePassive> Passives => _passives;

#if UNITY_EDITOR
        /// <summary>Keeps <see cref="_id"/> equal to the asset's file GUID, as EncounterData does.</summary>
        private void OnValidate()
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(path))
                return; // in-memory instance (tests, runtime) — keep whatever it has

            string assetGuid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(assetGuid) || _id == assetGuid)
                return;

            _id = assetGuid;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
