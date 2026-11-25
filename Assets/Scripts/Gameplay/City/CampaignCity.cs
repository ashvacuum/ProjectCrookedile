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

        [Tooltip("How districts are placed on each run's city. Each district's own rule is on its DistrictData.")]
        [SerializeField, InlineProperty, HideLabel]
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

        // The city's extent in world space, for clamping the camera.
        public Rect WorldBounds
        {
            get
            {
                if (_map == null || _view == null)
                {
                    return default;
                }

                Vector2Int size = _map.Size;
                Vector3 origin = _view.transform.position;
                float left = IsoGrid.CellToLocal(-0.5f, -0.5f).x;
                float right = IsoGrid.CellToLocal(size.x - 0.5f, size.y - 0.5f).x;
                float bottom = IsoGrid.CellToLocal(-0.5f, size.y - 0.5f).y;
                float top = IsoGrid.CellToLocal(size.x - 0.5f, -0.5f).y;
                return Rect.MinMaxRect(origin.x + left, origin.y + bottom, origin.x + right, origin.y + top);
            }
        }

        // Lights the district's building with the selection glow (alpha 0 turns it off). No-op for a district
        // with no building.
        public void SetDistrictGlow(DistrictData district, Color glow)
        {
            if (_view != null && district != null && _districtBuildings.TryGetValue(district, out int index))
            {
                _view.SetBuildingGlow(index, glow);
            }
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

            // The randomizer stands in for the §4 generator; its own System.Random keeps run and battle draws untouched.
            _downtown = _view.Randomizer.Fill(_map, run.Seed);
            _view.Show(_map);
            if (_sky != null)
            {
                _sky.Rain.PuddleSeed = run.Seed;
            }

            PlaceDistricts(run);
            GameLogger.LogInfo<CampaignCity>(
                $"Built the run's city from seed {run.Seed}: {_map.Buildings.Count} buildings, "
                    + $"{_districtPositions.Count} districts placed.",
                this
            );
            Built?.Invoke();
        }

        private void PlaceDistricts(RunState run)
        {
            _districtPositions.Clear();
            _districtBuildings.Clear();
            if (run.Travel == null)
            {
                return;
            }

            List<DistrictData> districts = run.Travel.Districts();
            Dictionary<DistrictData, int> placed = _placer.Place(_map, _downtown, districts, run.Seed);
            foreach (DistrictData district in districts)
            {
                if (!placed.TryGetValue(district, out int index))
                {
                    GameLogger.LogWarning<CampaignCity>($"No building left for {district.DisplayName}; it has no marker.", this);
                    continue;
                }

                // The marker stands on the roof: the lot's centre lifted by the wall height.
                _districtBuildings[district] = index;
                CityMapData.Building building = _map.Buildings[index];
                Vector2 center = building.Lot.center - new Vector2(0.5f, 0.5f);
                _districtPositions[district] =
                    _view.transform.position
                    + IsoGrid.CellToLocal(center.x, center.y)
                    + (Vector3.up * building.WallHeight);
            }
        }

        #endregion
    }
}
