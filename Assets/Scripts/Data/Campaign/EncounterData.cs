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

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                _id = System.Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        protected virtual void Reset()
        {
            _id = System.Guid.NewGuid().ToString();
        }
#endif
    }
}
