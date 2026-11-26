using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crookedile.Gameplay.City
{
    // A city on the iso grid (city-generator.md §3): ground and road per cell, plus building lots. The generator will
    // write one per run; as an asset it is a saved test map that the painter edits.
    [CreateAssetMenu(menuName = "Crookedile/Campaign/City Map", fileName = "CityMap")]
    public sealed class CityMapData : ScriptableObject
    {
        // Park is open land kept free of streets and lots by the generator: fields, plazas, empty patches. River is
        // water that roads may cross, so a road on a river cell is a bridge; sea takes no road at all.
        public enum Ground : byte
        {
            Land,
            Sea,
            Park,
            River,
        }

        public enum Road : byte
        {
            None,
            Street,
            Avenue,
        }

        [Serializable]
        public sealed class Building
        {
            [Tooltip("The lot's lowest row and column.")]
            [SerializeField]
            private Vector2Int _origin;

            [Tooltip("Rows (right wall) by columns (left wall) of the lot.")]
            [SerializeField]
            private Vector2Int _size = Vector2Int.one;

            [Tooltip("Wall height in world units. 0.5 is one storey.")]
            [SerializeField, Min(0.05f)]
            private float _wallHeight = 0.5f;

            [Tooltip("Flat roof, or a gable with its ridge along the lot's longer side.")]
            [SerializeField]
            private RoofStyle _roof = RoofStyle.Flat;

            [Tooltip("Slope of a gable roof, in degrees (under 30, or the back slope vanishes from the camera).")]
            [SerializeField, Range(5f, 28f)]
            private float _roofPitch = 20f;

            [Tooltip("Tint on the roof faces. White, or a clear colour from older maps, leaves the roof as painted.")]
            [SerializeField]
            private Color _roofColor = Color.white;

            public Building(RectInt lot, float wallHeight)
                : this(lot, wallHeight, RoofStyle.Flat, 20f, Color.white) { }

            public Building(RectInt lot, float wallHeight, RoofStyle roof, float roofPitch, Color roofColor)
            {
                _origin = lot.position;
                _size = lot.size;
                _wallHeight = wallHeight;
                _roof = roof;
                _roofPitch = Mathf.Clamp(roofPitch, 5f, 28f);
                _roofColor = roofColor;
            }

            public RectInt Lot => new RectInt(_origin, _size);
            public float WallHeight => _wallHeight;
            public RoofStyle Roof => _roof;
            public float RoofPitch => _roofPitch;
            public Color RoofColor => _roofColor.a > 0f ? _roofColor : Color.white;

            // How far a gable's ridge rises above the eaves (0 for a flat roof), in world units.
            public float RidgeRise =>
                _roof == RoofStyle.Gable
                    ? HalfCubeBuilding.RidgeRise(Mathf.Min(_size.x, _size.y), _roofPitch)
                    : 0f;

            // The top of the building: the ridge on a gable, the roof on a flat roof.
            public float TopHeight => _wallHeight + RidgeRise;

            // The single height the shadow test uses for the whole lot: a gable's average, eaves plus half the rise.
            public float ShadowHeight => _wallHeight + (RidgeRise * 0.5f);

            // The lot cell nearest the camera, where the half-cube's pivot sits.
            public Vector2Int FrontCell => new Vector2Int(_origin.x, _origin.y + _size.y - 1);
        }

        [Tooltip("Map size in rows by columns.")]
        [SerializeField]
        private Vector2Int _size = new Vector2Int(64, 64);

        [SerializeField, HideInInspector]
        private Vector2Int _storedSize;

        [SerializeField, HideInInspector]
        private Ground[] _ground = Array.Empty<Ground>();

        [SerializeField, HideInInspector]
        private Road[] _roads = Array.Empty<Road>();

        [Tooltip("Building lots. The painter adds and removes these; edit heights here.")]
        [SerializeField]
        private List<Building> _buildings = new List<Building>();

        public Vector2Int Size => _size;
        public IReadOnlyList<Building> Buildings => _buildings;

        #region Lifecycle

        private void OnEnable()
        {
            FitArraysToSize();
        }

        private void OnValidate()
        {
            _size = Vector2Int.Max(_size, Vector2Int.one);
            FitArraysToSize();
        }

        #endregion

        #region Public API

        // Changes the map's rows by columns, keeping the cells that still fit; at least 1 x 1.
        public void Resize(Vector2Int size)
        {
            _size = Vector2Int.Max(size, Vector2Int.one);
            FitArraysToSize();
        }

        public bool InBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < _size.x && cell.y < _size.y;
        }

        public Ground GetGround(Vector2Int cell)
        {
            return InBounds(cell) ? _ground[Index(cell)] : Ground.Land;
        }

        public Road GetRoad(Vector2Int cell)
        {
            return InBounds(cell) ? _roads[Index(cell)] : Road.None;
        }

        // Sea clears any road; neither can go under a building. Returns whether the cell changed.
        public bool SetGround(Vector2Int cell, Ground ground)
        {
            if (!InBounds(cell) || BuildingAt(cell) >= 0 || GetGround(cell) == ground)
            {
                return false;
            }

            _ground[Index(cell)] = ground;
            // Sea, park and river are open ground: painting them clears a road (a road may be painted back over a park
            // or across a river, as a bridge).
            if (ground != Ground.Land)
            {
                _roads[Index(cell)] = Road.None;
            }

            return true;
        }

        // Roads only go on land with no building. Returns whether the cell changed.
        public bool SetRoad(Vector2Int cell, Road road)
        {
            if (!InBounds(cell) || BuildingAt(cell) >= 0 || GetRoad(cell) == road)
            {
                return false;
            }

            if (road != Road.None && GetGround(cell) == Ground.Sea)
            {
                return false;
            }

            _roads[Index(cell)] = road;
            return true;
        }

        // Back to bare land: no sea, roads or lots.
        public void Clear()
        {
            Array.Clear(_ground, 0, _ground.Length);
            Array.Clear(_roads, 0, _roads.Length);
            _buildings.Clear();
        }

        public int BuildingAt(Vector2Int cell)
        {
            for (int index = 0; index < _buildings.Count; index++)
            {
                if (_buildings[index].Lot.Contains(cell))
                {
                    return index;
                }
            }

            return -1;
        }

        // A lot must be inside the map, on land, off roads and clear of other lots.
        public bool CanPlace(RectInt lot)
        {
            foreach (Vector2Int cell in lot.allPositionsWithin)
            {
                if (
                    !InBounds(cell)
                    || GetGround(cell) != Ground.Land
                    || GetRoad(cell) != Road.None
                    || BuildingAt(cell) >= 0
                )
                {
                    return false;
                }
            }

            return lot.width > 0 && lot.height > 0;
        }

        public bool AddBuilding(RectInt lot, float wallHeight)
        {
            return AddBuilding(lot, wallHeight, RoofStyle.Flat, 20f, Color.white);
        }

        public bool AddBuilding(RectInt lot, float wallHeight, RoofStyle roof, float roofPitch, Color roofColor)
        {
            if (!CanPlace(lot))
            {
                return false;
            }

            _buildings.Add(new Building(lot, wallHeight, roof, roofPitch, roofColor));
            return true;
        }

        public bool RemoveBuildingAt(Vector2Int cell)
        {
            int index = BuildingAt(cell);
            if (index < 0)
            {
                return false;
            }

            _buildings.RemoveAt(index);
            return true;
        }

        #endregion

        #region Private API

        private int Index(Vector2Int cell)
        {
            return (cell.x * _size.y) + cell.y;
        }

        // Resizing keeps every cell that still fits and drops lots that no longer do.
        private void FitArraysToSize()
        {
            int count = _size.x * _size.y;
            if (_storedSize == _size && _ground.Length == count && _roads.Length == count)
            {
                return;
            }

            Ground[] ground = new Ground[count];
            Road[] roads = new Road[count];
            int oldColumns = _storedSize.y;
            for (int row = 0; row < Mathf.Min(_size.x, _storedSize.x); row++)
            {
                for (int column = 0; column < Mathf.Min(_size.y, _storedSize.y); column++)
                {
                    int oldIndex = (row * oldColumns) + column;
                    if (oldIndex < _ground.Length && oldIndex < _roads.Length)
                    {
                        ground[(row * _size.y) + column] = _ground[oldIndex];
                        roads[(row * _size.y) + column] = _roads[oldIndex];
                    }
                }
            }

            _ground = ground;
            _roads = roads;
            _storedSize = _size;
            RectInt bounds = new RectInt(Vector2Int.zero, _size);
            _buildings.RemoveAll(building =>
                !bounds.Contains(building.Lot.min)
                || !bounds.Contains(building.Lot.max - Vector2Int.one)
            );
        }

        #endregion
    }
}
