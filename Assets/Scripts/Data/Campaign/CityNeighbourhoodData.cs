using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data.Campaign
{
    // Where on the generated city a neighbourhood's area is centred (city-generator.md §4.8).
    public enum DistrictPlacement
    {
        Anywhere,
        Downtown,
        Coast,
        Avenue,
        Edge,
    }

    // A kind of neighbourhood on the generated city (city-generator.md §4.5): how its buildings and streets look and
    // where it tends to sit. Districts point at one, and the campaign city also scatters scenery copies of each type
    // around town, so housing is everywhere, not only where there is somewhere to visit. Only the city reads it:
    // travel times stay on CampaignTravelData's roads and encounters still come from the pool.
    [CreateAssetMenu(menuName = "Crookedile/Campaign/City Neighbourhood", fileName = "New Neighbourhood")]
    public class CityNeighbourhoodData : ScriptableObject
    {
        [Tooltip(
            "Where the area is centred on each run's city: Downtown = nearest the centre, Coast = nearest the sea, Avenue = beside an avenue partway out, Edge = farthest out, Anywhere = a random spot."
        )]
        [SerializeField]
        private DistrictPlacement _placement = DistrictPlacement.Anywhere;

        [Tooltip(
            "How far the area reaches from its centre, in cells. Lots inside build to this type; where areas overlap the nearest centre wins."
        )]
        [SerializeField, Min(1f), SuffixLabel("cells", true)]
        private float _areaRadiusCells = 20f;

        [Title("Buildings")]
        [Tooltip("Storeys of a building in the area (min, max).")]
        [SerializeField, MinMaxSlider(1, 15, true)]
        private Vector2Int _storeys = new Vector2Int(1, 3);

        [Tooltip("Lot side length in the area, in cells (min, max). Bigger lots read as bigger buildings.")]
        [SerializeField, MinMaxSlider(1, 6, true), SuffixLabel("cells", true)]
        private Vector2Int _lotSize = new Vector2Int(1, 3);

        [Tooltip(
            "Chance that a free lot in the area gets a building; the rest stay as yards, plazas and gaps. High is dense, low is open."
        )]
        [SerializeField, Range(0f, 1f)]
        private float _lotFill = 0.85f;

        [Title("Streets")]
        [Tooltip(
            "Cells between side streets in the area, so block length (min, max). Short blocks give a tight web of small roads."
        )]
        [SerializeField, MinMaxSlider(2, 16, true), SuffixLabel("cells", true)]
        private Vector2Int _blockCells = new Vector2Int(4, 7);

        [Tooltip("Chance of a side street on each side at every block corner. High gives many small roads.")]
        [SerializeField, Range(0f, 1f)]
        private float _streetBranchChance = 0.5f;

        [Title("Around town")]
        [Tooltip(
            "Extra scenery areas of this type scattered over each run's city beyond the districts that use it. Nothing to visit there; it is what the town is made of."
        )]
        [SerializeField, Min(0)]
        private int _sceneryAreas;

        [Tooltip(
            "Land outside every area is built as this type. Mark one type, ordinary housing; if several are marked, the first a district reaches wins."
        )]
        [SerializeField]
        private bool _townFallback;

        public DistrictPlacement Placement => _placement;
        public float AreaRadiusCells => _areaRadiusCells;
        public Vector2Int Storeys => _storeys;
        public Vector2Int LotSize => _lotSize;
        public float LotFill => _lotFill;
        public Vector2Int BlockCells => _blockCells;
        public float StreetBranchChance => _streetBranchChance;
        public int SceneryAreas => _sceneryAreas;
        public bool TownFallback => _townFallback;
    }
}
