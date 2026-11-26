using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Campaign;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.City
{
    // Shows the run's city on the campaign map (city-generator.md §1): generated from RunState.Seed, so the same run
    // always draws the same city and nothing extra is saved, with each district placed on a building by its rule and a
    // sky that follows the campaign clock. It reads the run and never changes it.
    [Debuggable("City", LogLevel.Info)]
    public sealed class CampaignCity : MonoBehaviour
    {
        [Tooltip("The view that draws the generated city. Its Randomizer settings shape the city.")]
        [SerializeField]
        private CityMapView _view;

        [Tooltip("The sky, driven by the run's clock.")]
        [SerializeField]
        private CitySky _sky;

        [Tooltip(
            "Size of each run's city in rows by columns. More rows than columns gives a long coastal strip, like Metro Cebu along its shore; the randomizer puts the sea down the long side facing the camera."
        )]
        [SerializeField]
        private Vector2Int _mapSize = new Vector2Int(192, 112);

        [Tooltip(
            "How neighbourhood areas (districts and scenery) are spread over each run's city. Each type's own rule is on its CityNeighbourhoodData."
        )]
        [SerializeField, BoxGroup("Area placement"), InlineProperty, HideLabel]
        private CityDistrictPlacer _placer = new CityDistrictPlacer();

        private RunState _builtFor;
        private CityMapData _map;
        private Vector2Int _downtown;
        private readonly Dictionary<DistrictData, Vector3> _districtPositions = new Dictionary<DistrictData, Vector3>();
        private readonly Dictionary<DistrictData, int> _districtBuildings = new Dictionary<DistrictData, int>();

        // Raised after a new city is built, for the map camera and markers.
        public event System.Action Built;

        public bool IsBuilt => _builtFor != null && _map != null;

        public IReadOnlyDictionary<DistrictData, Vector3> DistrictPositions => _districtPositions;

        // Outlines the district's building with the selection outline (alpha 0 turns it off). No-op for a district
        // with no building.
        public void SetDistrictOutline(DistrictData district, Color outline)
        {
            if (_view != null && district != null && _districtBuildings.TryGetValue(district, out int index))
            {
                _view.SetBuildingOutline(index, outline);
            }
        }

        // The nearest view centre at which a view of halfWidth x halfHeight world units shows only the city: all four
        // of its corners stand on map cells or on the open sea past the coast (CityMapView.SeaApronCells). The three
        // land edges are hard limits; the coast side opens onto the sea. A view too big is centred (see
        // MaxViewHalfHeight). On the 2:1 grid each corner sits the same reach from the centre in rows and in
        // columns, so the centre only has to stay that reach inside the city on both axes.
        public Vector3 ClampViewCenter(Vector3 center, float halfWidth, float halfHeight)
        {
            if (_map == null || _view == null)
            {
                return center;
            }

            Vector3 origin = _view.transform.position;
            float reach = ((halfWidth / IsoGrid.HALF_WIDTH) + (halfHeight / IsoGrid.HALF_HEIGHT)) * 0.5f;
            Vector2 grid = IsoGrid.LocalToGrid(center - origin);
            GridExtent(out Vector2 min, out Vector2 max);
            float row = ClampAxis(grid.x, min.x + reach, max.x - reach);
            float column = ClampAxis(grid.y, min.y + reach, max.y - reach);
            Vector3 clamped = origin + IsoGrid.CellToLocal(row, column);
            return new Vector3(clamped.x, clamped.y, center.z);
        }

        // The largest half height a view of this aspect can have and still show only the city: its corner reach in
        // grid cells is at most half the city's narrow side, open sea included.
        public float MaxViewHalfHeight(float aspect)
        {
            if (_map == null || _view == null)
            {
                return float.MaxValue;
            }

            GridExtent(out Vector2 min, out Vector2 max);
            float narrow = Mathf.Min(max.x - min.x, max.y - min.y);
            return narrow / ((Mathf.Max(aspect, 0.01f) / IsoGrid.HALF_WIDTH) + (1f / IsoGrid.HALF_HEIGHT));
        }

        // The city in grid cells, cell edges included: the map, plus the open sea on the coast side.
        private void GridExtent(out Vector2 min, out Vector2 max)
        {
            Vector2Int size = _map.Size;
            min = new Vector2(-0.5f, -0.5f);
            max = new Vector2(size.x - 0.5f, size.y - 0.5f);
            Vector2Int outward = _view.SeaOutward;
            float sea = _view.SeaApronCells;
            if (outward.x > 0)
            {
                max.x += sea;
            }
            else if (outward.x < 0)
            {
                min.x -= sea;
            }
            else if (outward.y > 0)
            {
                max.y += sea;
            }
            else if (outward.y < 0)
            {
                min.y -= sea;
            }
        }

        private static float ClampAxis(float value, float min, float max)
        {
            return min > max ? (min + max) * 0.5f : Mathf.Clamp(value, min, max);
        }

        // Where the run's city is centred: downtown on the coast.
        public Vector3 DowntownPosition =>
            _view != null ? _view.transform.position + IsoGrid.CellToLocal(_downtown) : Vector3.zero;

        #region Lifecycle

        private void Update()
        {
            // The run can appear after this starts (CampaignFlow creates or loads it) or be replaced by a new run.
            RunState run = RunState.Current;
            if (run == null)
            {
                return;
            }

            if (run != _builtFor)
            {
                Build(run);
            }

            if (_sky != null)
            {
                _sky.MinuteOfDay = run.ClockMinute;
            }
        }

        private void OnDestroy()
        {
            if (_map != null)
            {
                Destroy(_map);
            }
        }

        #endregion

        #region Private API

        private void Build(RunState run)
        {
            _builtFor = run;
            if (_view == null)
            {
                return;
            }

            if (_map == null)
            {
                _map = ScriptableObject.CreateInstance<CityMapData>();
                _map.name = "Run City";
                _map.hideFlags = HideFlags.DontSave;
            }

            _map.Resize(_mapSize);

            // The randomizer stands in for the §4 generator; its own System.Random keeps run and battle draws untouched.
            // It asks for the neighbourhood areas once the coast, highway and avenues are down, then builds their
            // streets and lots to each area's type.
            List<DistrictData> districts = run.Travel != null ? run.Travel.Districts() : new List<DistrictData>();
            List<Placement> placements = PlanAreas(districts, out CityNeighbourhoodData fallback);
            _downtown = _view.Randomizer.Fill(
                _map,
                run.Seed,
                (map, downtown) => ChooseAreas(map, downtown, placements, fallback, run.Seed)
            );
            _view.Show(_map);
            if (_sky != null)
            {
                _sky.Rain.PuddleSeed = run.Seed;
            }

            PlaceDistricts(placements);
            GameLogger.LogInfo<CampaignCity>(
                $"Built the run's city from seed {run.Seed}: {_map.Buildings.Count} buildings, "
                    + $"{_districtPositions.Count} districts and {placements.Count - districts.Count} scenery areas placed.",
                this
            );
            Built?.Invoke();
        }

        // One area to centre on the city: a district (with its type, if it has one) or a scenery copy of a type.
        private sealed class Placement
        {
            public DistrictData District;
            public CityNeighbourhoodData Type;
            public Vector2Int? Centre;
        }

        // Districts first, so they get the best spots, then each type's scenery areas. The types are the ones the
        // network's districts use, in district order; the first marked as the town fallback builds the rest of town.
        private static List<Placement> PlanAreas(
            IReadOnlyList<DistrictData> districts,
            out CityNeighbourhoodData fallback
        )
        {
            var placements = new List<Placement>();
            var types = new List<CityNeighbourhoodData>();
            foreach (DistrictData district in districts)
            {
                placements.Add(new Placement { District = district, Type = district.Neighbourhood });
                if (district.Neighbourhood != null && !types.Contains(district.Neighbourhood))
                {
                    types.Add(district.Neighbourhood);
                }
            }

            fallback = types.Find(type => type.TownFallback);
            foreach (CityNeighbourhoodData type in types)
            {
                for (int copy = 0; copy < type.SceneryAreas; copy++)
                {
                    placements.Add(new Placement { Type = type });
                }
            }

            return placements;
        }

        // Centres every placement on the city and turns each typed one into an area, plus the town fallback.
        private List<CityArea> ChooseAreas(
            CityMapData map,
            Vector2Int downtown,
            List<Placement> placements,
            CityNeighbourhoodData fallback,
            int seed
        )
        {
            var rules = placements.ConvertAll(placement =>
                placement.Type != null ? placement.Type.Placement : DistrictPlacement.Anywhere
            );
            List<Vector2Int?> centres = _placer.PlaceCentres(map, downtown, rules, seed);
            var areas = new List<CityArea>(placements.Count + 1);
            for (int index = 0; index < placements.Count; index++)
            {
                placements[index].Centre = centres[index];
                if (centres[index].HasValue && placements[index].Type != null)
                {
                    areas.Add(AreaOf(placements[index].Type, centres[index].Value, placements[index].Type.AreaRadiusCells));
                }
            }

            if (fallback != null)
            {
                areas.Add(AreaOf(fallback, downtown, float.PositiveInfinity));
            }

            return areas;
        }

        private static CityArea AreaOf(CityNeighbourhoodData type, Vector2Int centre, float radius)
        {
            return new CityArea
            {
                Center = centre,
                Radius = radius,
                Storeys = type.Storeys,
                LotSize = type.LotSize,
                LotFill = type.LotFill,
                BlockCells = type.BlockCells,
                StreetBranchChance = type.StreetBranchChance,
                Grid = type.StreetLayout == StreetLayout.Grid,
                BuildingGap = type.BuildingGap,
                GableChance = type.GableChance,
                RoofPitch = type.RoofPitch,
                RoofColors = type.RoofColors,
            };
        }

        // Puts each district's marker on the tallest building near its area's centre.
        // ponytail: a district with no type searches a fixed 12 cells around its centre; give it a type to tune it.
        private void PlaceDistricts(List<Placement> placements)
        {
            _districtPositions.Clear();
            _districtBuildings.Clear();
            var taken = new HashSet<int>();
            foreach (Placement placement in placements)
            {
                DistrictData district = placement.District;
                if (district == null)
                {
                    continue;
                }

                float radius = placement.Type != null ? placement.Type.AreaRadiusCells : 12f;
                int index = placement.Centre.HasValue
                    ? _placer.PickBuilding(_map, placement.Centre.Value, radius, taken)
                    : -1;
                if (index < 0)
                {
                    GameLogger.LogWarning<CampaignCity>($"No building left for {district.DisplayName}; it has no marker.", this);
                    continue;
                }

                // The marker stands on the roof: the lot's centre lifted by the wall height.
                taken.Add(index);
                _districtBuildings[district] = index;
                CityMapData.Building building = _map.Buildings[index];
                Vector2 center = building.Lot.center - new Vector2(0.5f, 0.5f);
                _districtPositions[district] =
                    _view.transform.position
                    + IsoGrid.CellToLocal(center.x, center.y)
                    + (Vector3.up * building.TopHeight);
            }
        }

        #endregion
    }
}
