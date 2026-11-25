using UnityEngine;

namespace Crookedile.Data.Campaign
{
    // Where on the generated city a district's marker stands (city-generator.md §4.8). Only the drawing uses it: travel
    // times stay on CampaignTravelData's roads and encounters still come from the pool.
    public enum DistrictPlacement
    {
        Anywhere,
        Downtown,
        Coast,
        Avenue,
        Edge,
    }

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
            "Where the district stands on each run's city: Downtown = nearest the centre, Coast = nearest the sea, Avenue = on an avenue partway out, Edge = farthest out, Anywhere = a random building."
        )]
        [SerializeField]
        private DistrictPlacement _placement = DistrictPlacement.Anywhere;

        public Vector2 MapPosition
        {
            get { return _mapPosition; }
        }

        public DistrictPlacement Placement => _placement;

        public string DisplayName
        {
            get { return string.IsNullOrWhiteSpace(_displayName) ? name : _displayName; }
        }
    }
}
