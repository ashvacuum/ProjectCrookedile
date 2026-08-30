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
        [Tooltip(
            "Flavour text shown to the player. Leave blank to fall back to the description "
                + "generated from the passives below, which is always accurate and never drifts."
        )]
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
        // Reads back what the passives below actually do, so a hand-written description can be
        // checked against the mechanics instead of being taken on trust.
        [InfoBox("@EditorSafeAutoDescription()", InfoMessageType.None)]
        [SerializeReference]
        [SerializeField]
        private List<BattlePassive> _passives = new List<BattlePassive>();

        public string Id => _id;
        public string AllyName => _allyName;
        public Sprite Icon => _icon;
        public CardRarity Rarity => _rarity;
        public IReadOnlyList<BattlePassive> Passives => _passives;

        /// <summary>
        /// What this ally does, assembled from its passives. Generated rather than typed, so it
        /// cannot drift from the mechanics the way a hand-written line does the moment a passive
        /// is retuned.
        /// </summary>
        public string AutoDescription
        {
            get
            {
                if (_passives == null || _passives.Count == 0)
                    return "Does nothing yet.";

                var parts = new List<string>();
                foreach (var passive in _passives)
                    if (passive != null)
                        parts.Add(passive.GetDescription());
                return parts.Count == 0 ? "Does nothing yet." : string.Join("\n", parts);
            }
        }

        /// <summary>
        /// Shown to the player: the authored flavour when there is any, the generated mechanical
        /// description otherwise — so an ally is never described as nothing at all.
        /// </summary>
        public string Description =>
            string.IsNullOrWhiteSpace(_description) ? AutoDescription : _description;

#if UNITY_EDITOR
        /// <summary>
        /// Guarded wrapper for the inspector InfoBox — one half-authored passive must not break
        /// the whole asset's inspector. Same affordance as RunOutcome's.
        /// </summary>
        private string EditorSafeAutoDescription()
        {
            try
            {
                return AutoDescription;
            }
            catch (System.Exception e)
            {
                return $"(description error: {e.GetType().Name})";
            }
        }
#endif

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
