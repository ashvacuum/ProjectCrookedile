using UnityEngine;

namespace Crookedile.Data.Campaign
{
    [CreateAssetMenu(menuName = "Crookedile/Campaign/District", fileName = "New District")]
    public class DistrictData : ScriptableObject
    {
        [Tooltip("District name shown in travel previews. Blank uses the asset name.")]
        [SerializeField]
        private string _displayName;

        [Tooltip(
            "Position in the Encounter Designer district diagram. Negative coordinates use automatic placement."
        )]
        [SerializeField]
        private Vector2 _mapPosition = new Vector2(-1f, -1f);

        public Vector2 MapPosition
        {
            get { return _mapPosition; }
        }

        public string DisplayName
        {
            get { return string.IsNullOrWhiteSpace(_displayName) ? name : _displayName; }
        }
    }
}
