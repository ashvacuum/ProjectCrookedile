using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Art.Iso
{
    // Draws a CityMapData: one ground tile per cell and a half-cube per building lot (city-generator.md §5-6).
    // Everything it spawns is DontSave and rebuilt on enable, so the scene stores only the map reference.
    [ExecuteAlways]
    public sealed class CityMapView : MonoBehaviour
    {
        // Neighbour bits in the road atlas's own frame: N = +column, E = +row (the cell's image up and right).
        private const int NORTH = 1;
        private const int EAST = 2;
        private const int SOUTH = 4;
        private const int WEST = 8;

        // Ground never overlaps a building, so every tile draws below every building.
        private const int GROUND_SORTING_ORDER = -1000;
        private static readonly int RoofBaseColorId = Shader.PropertyToID("_RoofBaseColor");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int SpecularStrengthId = Shader.PropertyToID("_SpecularStrength");
        private static readonly int OcclusionStrengthId = Shader.PropertyToID("_OcclusionStrength");
        private static readonly int HeightMapId = Shader.PropertyToID("_CityHeightMap");
        private static readonly int HeightMapSizeId = Shader.PropertyToID("_CityHeightMapSize");
        private static readonly int MapOriginId = Shader.PropertyToID("_CityMapOrigin");
        private static readonly int GridId = Shader.PropertyToID("_CityGrid");

        // Road atlas cells (row, column) and which neighbours each connects to unrotated.
        private static readonly (Vector2Int cell, int mask)[] RoadPieces =
        {
            (new Vector2Int(0, 0), NORTH | SOUTH), // straight
            (new Vector2Int(0, 1), SOUTH | EAST), // corner
            (new Vector2Int(0, 2), WEST | EAST | SOUTH), // T junction
            (new Vector2Int(0, 3), NORTH | EAST | SOUTH | WEST), // cross
            (new Vector2Int(1, 1), SOUTH), // dead end
        };
        private static readonly Vector2Int PavementPlain = new Vector2Int(2, 0);
        private static readonly Vector2Int PavementCracked = new Vector2Int(2, 1);
        private static readonly (Vector2Int cell, int quarterTurns)[] RoadByMask = BuildRoadTable();

        [Tooltip("The map to draw. Any City Map asset works as a saved test map.")]
        [SerializeField]
        private CityMapData _map;

        [Tooltip(
            "Lit material using the road atlas; its pavement and street cells draw land and streets."
        )]
        [SerializeField]
        private Material _groundMaterial;

        [Tooltip("Half-cube building prefab spawned for each lot.")]
        [SerializeField]
        private GameObject _buildingPrefab;

        [Tooltip("Placeholder colour for sea cells until water art exists.")]
        [SerializeField]
        private Color _seaColor = new Color(0.2f, 0.45f, 0.65f);

        [Tooltip("Placeholder colour for avenue cells until avenue art exists.")]
        [SerializeField]
        private Color _avenueColor = new Color(0.5f, 0.48f, 0.56f);

        [Tooltip(
            "Tuning for the painter's Randomize button, which replaces the whole map with a random test city."
        )]
        [SerializeField, FoldoutGroup("Randomizer", Expanded = false), InlineProperty, HideLabel]
        private CityMapRandomizer _randomizer = new CityMapRandomizer();

        [Tooltip(
            "Window light spilling onto streets and nearby walls at night. Rebaked whenever the buildings change."
        )]
        [SerializeField, FoldoutGroup("Window Glow", Expanded = false), InlineProperty, HideLabel]
        private CityGlow _glow = new CityGlow();

        private readonly List<GameObject> _tiles = new List<GameObject>();
        private readonly List<GameObject> _buildings = new List<GameObject>();
        private Vector2Int _builtSize;
        private Mesh _tileMesh;
        private Material _seaMaterial;
        private Material _avenueMaterial;
        private Texture2D _clearTexture;
        private Texture2D _heightMap;

        public CityMapData Map => _map;
        public CityMapRandomizer Randomizer => _randomizer;

        #region Lifecycle

        private void OnEnable()
        {
            Rebuild();
        }

        private void Update()
        {
            // The shadow test maps world positions to cells, so it follows the map if the view moves.
            Shader.SetGlobalVector(MapOriginId, transform.position);
        }

        private void OnDisable()
        {
            Clear();
            Shader.SetGlobalVector(HeightMapSizeId, Vector4.zero);
            DestroyOwned(_tileMesh);
            DestroyOwned(_seaMaterial);
            DestroyOwned(_avenueMaterial);
            DestroyOwned(_clearTexture);
            DestroyOwned(_heightMap);
            _glow.Release();
        }

        private void OnValidate()
        {
            // Glow tuning shows straight away; the bake is cheap next to rebuilding the tiles.
            if (isActiveAndEnabled)
            {
                _glow.Publish(_map);
            }
        }

        #endregion

        #region Public API

        [Button]
        public void Rebuild()
        {
            Clear();
            if (_map == null || _groundMaterial == null)
            {
                return;
            }

            EnsureSharedAssets();
            _builtSize = _map.Size;
            for (int row = 0; row < _builtSize.x; row++)
            {
                for (int column = 0; column < _builtSize.y; column++)
                {
                    _tiles.Add(CreateTile(new Vector2Int(row, column)));
                }
            }

            RebuildBuildings();
        }

        // Re-picks the art of these cells and their neighbours; call after painting ground or roads.
        public void RefreshCells(IEnumerable<Vector2Int> cells)
        {
            if (NeedsFullRebuild())
            {
                Rebuild();
                return;
            }

            foreach (Vector2Int cell in cells)
            {
                for (int row = -1; row <= 1; row++)
                {
                    for (int column = -1; column <= 1; column++)
                    {
                        Vector2Int neighbour = cell + new Vector2Int(row, column);
                        if (_map.InBounds(neighbour))
                        {
                            ApplyTile(
                                _tiles[(neighbour.x * _builtSize.y) + neighbour.y],
                                neighbour
                            );
                        }
                    }
                }
            }
        }

        public void RebuildBuildings()
        {
            for (int index = 0; index < _buildings.Count; index++)
            {
                DestroyOwned(_buildings[index]);
            }

            _buildings.Clear();
            PublishHeightMap();
            _glow.Publish(_map);
            if (_map == null || _buildingPrefab == null)
            {
                return;
            }

            for (int index = 0; index < _map.Buildings.Count; index++)
            {
                _buildings.Add(CreateBuilding(_map.Buildings[index]));
            }
        }

        // One texel per cell holding the building height there, for the shader's shadow test (city-generator.md §6.2).
        // ponytail: one map at a time; a second enabled view overwrites the first's globals.
        private void PublishHeightMap()
        {
            if (_map == null)
            {
                Shader.SetGlobalVector(HeightMapSizeId, Vector4.zero);
                return;
            }

            Vector2Int size = _map.Size;
            if (_heightMap == null || _heightMap.width != size.x || _heightMap.height != size.y)
            {
                DestroyOwned(_heightMap);
                _heightMap = new Texture2D(size.x, size.y, TextureFormat.RFloat, false, true)
                {
                    name = "City Height Map",
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
            }

            Color[] heights = new Color[size.x * size.y];
            float tallest = 0f;
            foreach (CityMapData.Building building in _map.Buildings)
            {
                tallest = Mathf.Max(tallest, building.WallHeight);
                foreach (Vector2Int cell in building.Lot.allPositionsWithin)
                {
                    // Texel x is the row and y the column, matching the shader's Load(int2(row, column)).
                    heights[(cell.y * size.x) + cell.x] = new Color(
                        building.WallHeight,
                        0f,
                        0f,
                        0f
                    );
                }
            }

            _heightMap.SetPixels(heights);
            _heightMap.Apply(false);
            Shader.SetGlobalTexture(HeightMapId, _heightMap);
            Shader.SetGlobalVector(
                HeightMapSizeId,
                new Vector4(size.x, size.y, tallest / IsoGrid.SCREEN_HEIGHT_PER_CELL, 0f)
            );
            Shader.SetGlobalVector(MapOriginId, transform.position);
            Shader.SetGlobalVector(
                GridId,
                new Vector4(
                    IsoGrid.HALF_WIDTH,
                    IsoGrid.HALF_HEIGHT,
                    IsoGrid.SCREEN_HEIGHT_PER_CELL,
                    0f
                )
            );
        }

        public bool NeedsFullRebuild()
        {
            return _map != null
                && (_builtSize != _map.Size || _tiles.Count != _builtSize.x * _builtSize.y);
        }

        #endregion

        #region Private API

        private GameObject CreateTile(Vector2Int cell)
        {
            GameObject tile = new GameObject($"Cell {cell.x},{cell.y}")
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            tile.transform.SetParent(transform, false);
            tile.transform.localPosition = IsoGrid.CellToLocal(cell);
            tile.AddComponent<MeshFilter>().sharedMesh = _tileMesh;
            MeshRenderer meshRenderer = tile.AddComponent<MeshRenderer>();
            meshRenderer.sortingOrder = GROUND_SORTING_ORDER;
            tile.AddComponent<CityRoadTile>();
            ApplyTile(tile, cell);
            return tile;
        }

        // ponytail: one GameObject per cell (4,096 at 64 x 64); bake 16 x 16 chunk meshes (city-generator.md §6) if
        // building or painting gets slow.
        private void ApplyTile(GameObject tile, Vector2Int cell)
        {
            MeshRenderer meshRenderer = tile.GetComponent<MeshRenderer>();
            CityRoadTile roadTile = tile.GetComponent<CityRoadTile>();
            if (_map.GetGround(cell) == CityMapData.Ground.Sea)
            {
                meshRenderer.sharedMaterial = _seaMaterial;
                return;
            }

            CityMapData.Road road = _map.GetRoad(cell);
            if (road == CityMapData.Road.Avenue)
            {
                meshRenderer.sharedMaterial = _avenueMaterial;
                return;
            }

            meshRenderer.sharedMaterial = _groundMaterial;
            if (road == CityMapData.Road.Street)
            {
                (Vector2Int atlasCell, int quarterTurns) = RoadByMask[NeighbourMask(cell)];
                roadTile.SetAtlasCell(atlasCell.x, atlasCell.y, quarterTurns);
                return;
            }

            // Cosmetic only: a fixed hash so a cell keeps its look between rebuilds.
            Vector2Int pavement =
                ((cell.x * 73856093) ^ (cell.y * 19349663)) % 6 == 0
                    ? PavementCracked
                    : PavementPlain;
            roadTile.SetAtlasCell(pavement.x, pavement.y, 0);
        }

        // Avenues count as road, so a street meeting an avenue joins it.
        private int NeighbourMask(Vector2Int cell)
        {
            int mask = 0;
            mask |= IsRoad(cell + Vector2Int.up) ? NORTH : 0;
            mask |= IsRoad(cell + Vector2Int.right) ? EAST : 0;
            mask |= IsRoad(cell + Vector2Int.down) ? SOUTH : 0;
            mask |= IsRoad(cell + Vector2Int.left) ? WEST : 0;
            return mask;
        }

        private bool IsRoad(Vector2Int cell)
        {
            return _map.GetRoad(cell) != CityMapData.Road.None;
        }

        private GameObject CreateBuilding(CityMapData.Building building)
        {
            Vector2Int front = building.FrontCell;
            GameObject instance = Instantiate(_buildingPrefab, transform, false);
            instance.name = $"Building {building.Lot.x},{building.Lot.y}";
            instance.hideFlags = HideFlags.HideAndDontSave;
            instance.transform.localPosition = IsoGrid.CellToLocal(front);
            if (instance.TryGetComponent(out HalfCubeBuilding halfCube))
            {
                halfCube.Configure(building.Lot.width, building.Lot.height, building.WallHeight);
            }

            if (instance.TryGetComponent(out MeshRenderer meshRenderer))
            {
                meshRenderer.sortingOrder = IsoGrid.Depth(front);
            }

            return instance;
        }

        private void EnsureSharedAssets()
        {
            if (_tileMesh == null)
            {
                _tileMesh = CreateTileMesh();
            }

            if (_clearTexture == null)
            {
                _clearTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                };
                _clearTexture.SetPixel(0, 0, Color.clear);
                _clearTexture.Apply();
            }

            if (_seaMaterial == null)
            {
                _seaMaterial = CreatePlaceholderMaterial("Sea Placeholder", _seaColor);
            }

            if (_avenueMaterial == null)
            {
                _avenueMaterial = CreatePlaceholderMaterial("Avenue Placeholder", _avenueColor);
            }

            _seaMaterial.SetColor(RoofBaseColorId, _seaColor);
            _avenueMaterial.SetColor(RoofBaseColorId, _avenueColor);
        }

        // The half-cube shader fills transparent texels with _RoofBaseColor, so a clear texture gives a flat lit colour.
        private Material CreatePlaceholderMaterial(string materialName, Color color)
        {
            Material material = new Material(_groundMaterial)
            {
                name = materialName,
                hideFlags = HideFlags.HideAndDontSave,
            };
            material.SetTexture(MainTexId, _clearTexture);
            material.SetFloat(SpecularStrengthId, 0f);
            material.SetFloat(OcclusionStrengthId, 0f);
            material.SetColor(RoofBaseColorId, color);
            return material;
        }

        private static Mesh CreateTileMesh()
        {
            // Same layout as the road preview tile: uv.x runs along +row and uv.y along +column.
            Mesh mesh = new Mesh { name = "City Map Tile", hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = new[]
            {
                IsoGrid.CellToLocal(-0.5f, -0.5f),
                IsoGrid.CellToLocal(0.5f, -0.5f),
                IsoGrid.CellToLocal(0.5f, 0.5f),
                IsoGrid.CellToLocal(-0.5f, 0.5f),
            };
            // Ground faces up in the 3D city, like a roof; uv.x runs along +row.
            Vector3 up = IsoGrid.UpView;
            mesh.normals = new[] { up, up, up, up };
            Vector4 tangent = new Vector4(
                IsoGrid.RowView.x,
                IsoGrid.RowView.y,
                IsoGrid.RowView.z,
                1f
            );
            mesh.tangents = new[] { tangent, tangent, tangent, tangent };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            };
            mesh.SetUVs(1, new[] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero });
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        // For every neighbour mask, the atlas piece and quarter turns that connect exactly those neighbours.
        private static (Vector2Int, int)[] BuildRoadTable()
        {
            (Vector2Int, int)[] table = new (Vector2Int, int)[16];
            for (int mask = 0; mask < 16; mask++)
            {
                // A lone cell shows a straight; every other mask has an exact piece.
                table[mask] = (RoadPieces[0].cell, 0);
                foreach ((Vector2Int cell, int pieceMask) in RoadPieces)
                {
                    int rotated = pieceMask;
                    for (int turns = 0; turns < 4; turns++)
                    {
                        if (rotated == mask)
                        {
                            table[mask] = (cell, turns);
                            goto Found;
                        }

                        rotated = QuarterTurn(rotated);
                    }
                }

                Found:
                ;
            }

            return table;
        }

        // One shader quarter turn (TransformAtlasUV) shows the atlas's N side at W, E at N, S at E and W at S.
        private static int QuarterTurn(int mask)
        {
            int turned = 0;
            turned |= (mask & NORTH) != 0 ? WEST : 0;
            turned |= (mask & EAST) != 0 ? NORTH : 0;
            turned |= (mask & SOUTH) != 0 ? EAST : 0;
            turned |= (mask & WEST) != 0 ? SOUTH : 0;
            return turned;
        }

        private void Clear()
        {
            for (int index = 0; index < _tiles.Count; index++)
            {
                DestroyOwned(_tiles[index]);
            }

            _tiles.Clear();
            for (int index = 0; index < _buildings.Count; index++)
            {
                DestroyOwned(_buildings[index]);
            }

            _buildings.Clear();
            _builtSize = Vector2Int.zero;
        }

        private static void DestroyOwned(Object owned)
        {
            if (owned == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(owned);
            }
            else
            {
                DestroyImmediate(owned);
            }
        }

        #endregion
    }
}
