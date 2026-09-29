using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data.Campaign
{
    /// <summary>
    /// A location the player can choose on the campaign map. Abstract base for the
    /// concrete encounter types (<see cref="BattleEncounterData"/>,
    /// <see cref="EventEncounterData"/>; Shop later — see docs/metagame-campaign.md
    /// for the full type tree).
    /// </summary>
    public abstract class EncounterData : ScriptableObject
    {
        // Bottom of the inspector, not the top: it's derived from the asset GUID and never
        // typed, so it was pure noise above the fields you actually author.
        [ReadOnly]
        [PropertyOrder(100)]
        [FoldoutGroup("Identity", Expanded = false)]
        [Tooltip("Unique identifier — the asset's own file GUID. Never edit by hand.")]
        [SerializeField]
        private string _id;

        [Tooltip("Name shown on the map button. Falls back to the asset name while blank.")]
        [SerializeField]
        private string _displayName;

        [TextArea(2, 4)]
        [SerializeField]
        private string _blurb;

        [Tooltip("Shown at the top of the encounter panel when this location is entered.")]
        [SerializeField]
        private Sprite _image;

        [Tooltip("Encounter duration in hours, excluding travel and waiting.")]
        [Min(0)]
        [HorizontalGroup("Cost", LabelWidth = 80)]
        [LabelText("Hours")]
        [SerializeField]
        private int _hourCost = 1;

        [FoldoutGroup("Travel and time")]
        [Tooltip("District where this encounter happens. Blank means local: no travel or change of district.")]
        [InlineEditor]
#if UNITY_EDITOR
        [InlineButton(nameof(CreateDistrict), "New")]
#endif
        [SerializeField]
        private DistrictData _district;

        [FoldoutGroup("Travel and time")]
        [Tooltip("Extra duration minutes added to Hours. Use Hours 0 and Minutes 30 for a half-hour event.")]
        [Range(0, 59)]
        [SerializeField]
        private int _extraMinutes;

        [FoldoutGroup("Travel and time")]
        [Tooltip("Opening clock minute, inclusive: 480 = 08:00. Early arrivals wait until opening.")]
        [Range(0, 1439)]
        [SerializeField]
        private int _openingMinute;

        [FoldoutGroup("Travel and time")]
        [Tooltip("Latest entry minute, exclusive: 1020 = 17:00. 0 means midnight. Finishing later is allowed.")]
        [Range(0, 1440)]
        [ValidateInput("@_closingMinute == 0 || _closingMinute > _openingMinute", "Closing time must be after opening.")]
        [SerializeField]
        private int _closingMinute;

        public DistrictData District
        {
            get { return _district; }
        }

        public int DurationMinutes
        {
            get { return Mathf.Clamp(_hourCost, 0, 24) * 60 + Mathf.Clamp(_extraMinutes, 0, 59); }
        }

        public int OpeningMinute
        {
            get { return Mathf.Clamp(_openingMinute, 0, 1439); }
        }

        public int ClosingMinute
        {
            get { return _closingMinute == 0 ? 1440 : Mathf.Clamp(_closingMinute, 0, 1440); }
        }

        [Tooltip(
            "How likely this is to be drawn, relative to everything else eligible the same "
                + "day. This is the encounter's own default — an EncounterPoolEntry can "
                + "override it per pool, and falls back to this when its own weight is left unset."
        )]
        [Min(0f)]
        [HorizontalGroup("Cost", LabelWidth = 80)]
        [LabelText("Weight")]
        [ValidateInput(
            "@_dropWeight > 0f",
            "Weight 0 — every pool row inheriting this can never draw it.",
            InfoMessageType.Warning
        )]
        [SerializeField]
        private float _dropWeight = 1f;

        /// <summary>Unique identifier for this encounter. Auto-generated GUID.</summary>
        public string ID => _id;
        public string DisplayName => _displayName;
        public string Blurb => _blurb;
        public Sprite Image => _image;
        public int HourCost => _hourCost;

        /// <summary>
        /// This encounter's default draw weight, used whenever a pool entry doesn't override it.
        /// </summary>
        public float DropWeight => _dropWeight;

        // An encounter is self-contained: it has no "and then go here" of its own. Sequencing is
        // a property of a *choice*, so it lives on EventOption as a GoToEncounterOutcome — which
        // can point at a battle, making "refuse him and fight now" one option on one event.
        // Later availability is a dependency instead: a HasVisitedEncounter requirement on a
        // pool entry. See docs/campaign-encounters.md.

#if UNITY_EDITOR
        private void CreateDistrict()
        {
            if (_district != null)
            {
                return;
            }

            UnityEditor.Undo.RecordObject(this, "Assign new district");
            _district = Crookedile.Utilities.AuthoringAssets.CreateBeside<DistrictData>(this, "New District");
            UnityEditor.Undo.RegisterCreatedObjectUndo(_district, "Create district");
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Keeps <see cref="_id"/> equal to the asset's file GUID, so a duplicated asset can't
        /// inherit the original's id.
        /// </summary>
        protected virtual void OnValidate()
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

        protected virtual void Reset()
        {
            _id = System.Guid.NewGuid().ToString();
        }
#endif
    }
}
