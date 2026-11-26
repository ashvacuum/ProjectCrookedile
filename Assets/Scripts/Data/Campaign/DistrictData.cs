using Sirenix.OdinInspector;
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

        [Tooltip(
            "What the district looks like on each run's city and where it sits: its buildings, streets and placement rule. Several districts can share a type."
        )]
        [SerializeField, InlineEditor]
        private CityNeighbourhoodData _neighbourhood;

        public Vector2 MapPosition
        {
            get { return _mapPosition; }
        }

        // Null for a district with no look of its own: it still gets a marker, on the town around it.
        public CityNeighbourhoodData Neighbourhood => _neighbourhood;

        public string DisplayName
        {
            get { return string.IsNullOrWhiteSpace(_displayName) ? name : _displayName; }
        }
    }
}
