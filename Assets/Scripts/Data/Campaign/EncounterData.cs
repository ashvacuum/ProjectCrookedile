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
        [ReadOnly]
        [Tooltip("Unique identifier for this encounter. Auto-generated GUID.")]
        [SerializeField]
        private string _id;

        [SerializeField]
        private string _displayName;

        [TextArea(2, 4)]
        [SerializeField]
        private string _blurb;

        [Tooltip("Hours spent choosing this location.")]
        [Min(0)]
        [SerializeField]
        private int _hourCost = 1;

        [Tooltip(
            "How likely this is to be drawn, relative to everything else eligible the same "
                + "day. This is the encounter's own default — an EncounterPoolEntry can "
                + "override it per pool, and falls back to this when its own weight is left unset."
        )]
        [Min(0f)]
        [SerializeField]
        private float _dropWeight = 1f;

        /// <summary>Unique identifier for this encounter. Auto-generated GUID.</summary>
        public string ID => _id;
        public string DisplayName => _displayName;
        public string Blurb => _blurb;
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
        /// <summary>
        /// Keeps <see cref="_id"/> equal to the asset's own file GUID.
        ///
        /// It used to fill the id only when blank, which meant duplicating an asset (Ctrl+D)
        /// carried the original's id into the copy — two encounters answering to one id, so
        /// visiting either marked both visited and the dependency graph threw on the collision.
        /// Unity already guarantees a unique, rename- and move-stable id per asset file; minting
        /// a second one by hand was the whole bug.
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
