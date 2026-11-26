using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.City
{
    // Draws a CityMapData (city-generator.md §5-6): the ground as square chunks, one mesh per chunk with each
    // cell's atlas piece baked in, and a half-cube per building lot, grouped by the chunk its front cell is in.
    // In Play mode chunks outside the camera's view switch their renderers off, so the cost follows what is on
    // screen rather than the map's size. Everything it spawns is DontSave and rebuilt on enable, so the scene
    // stores only the map reference.
    [ExecuteAlways]
    public sealed class CityMapView : MonoBehaviour
    {
        // Neighbour bits in the road atlas's own frame: N = +column, E = +row (the cell's image up and right).
        private const int NORTH = 1;
        private const int EAST = 2;
        private const int SOUTH = 4;
        private const int WEST = 8;

        // Chunk submeshes, one per ground material.
        private const int GROUND_SUBMESH = 0;
        private const int SEA_SUBMESH = 1;
        private const int AVENUE_SUBMESH = 2;

        // Ground never overlaps a building, so every chunk draws below every building.
        private const int GROUND_SORTING_ORDER = -1000;
        private static readonly int RoofBaseColorId = Shader.PropertyToID("_RoofBaseColor");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int SpecularStrengthId = Shader.PropertyToID("_SpecularStrength");
        private static readonly int OcclusionStrengthId = Shader.PropertyToID("_OcclusionStrength");
        private static readonly int HeightMapId = Shader.PropertyToID("_CityHeightMap");
        private static readonly int HeightMapSizeId = Shader.PropertyToID("_CityHeightMapSize");
        private static readonly int MapOriginId = Shader.PropertyToID("_CityMapOrigin");
        private static readonly int GridId = Shader.PropertyToID("_CityGrid");
        private static readonly int SelectionColorId = Shader.PropertyToID("_SelectionColor");

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

        // One cell's quad, around its centre; uv.x runs along +row and uv.y along +column, like the road preview tile.
        private static readonly Vector3[] CellCorners =
        {
            IsoGrid.CellToLocal(-0.5f, -0.5f),
            IsoGrid.CellToLocal(0.5f, -0.5f),
            IsoGrid.CellToLocal(0.5f, 0.5f),
            IsoGrid.CellToLocal(-0.5f, 0.5f),
        };
        private static readonly Vector2[] CornerUvs =
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
        };

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
            "Side of a ground chunk, in cells. Each chunk is one mesh and the unit of culling: bigger means fewer objects, smaller means tighter culling."
        )]
        [SerializeField, Range(4, 64), FoldoutGroup("Chunks and culling", Expanded = false)]
        private int _chunkCells = 16;

        [Tooltip("Camera whose view decides which chunks draw in Play mode. Blank uses the main camera.")]
        [SerializeField, FoldoutGroup("Chunks and culling")]
        private Camera _cullCamera;

        [Tooltip(
            "Extra room around the camera's view before a chunk is switched off, in world units, so nothing pops in at the screen edge while panning."
        )]
        [SerializeField, Min(0f), SuffixLabel("units", true), FoldoutGroup("Chunks and culling")]
        private float _cullMargin = 2f;

        [Tooltip(
            "Open sea drawn past the coast edge, in cells, so the city looks out over water and the camera can pan out over it. The coast is the map edge that is mostly sea; 0 draws none."
        )]
        [SerializeField, Range(0, 128), SuffixLabel("cells", true), FoldoutGroup("Chunks and culling")]
        private int _seaApronCells = 32;

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

        // A square block of cells: one ground mesh, and the buildings whose front cell is inside it.
        private sealed class Chunk
        {
            public RectInt Cells;
            public GameObject Root;
            public Mesh Mesh;
            public MeshRenderer Ground;
            public readonly List<MeshRenderer> Buildings = new List<MeshRenderer>();
            public Bounds Bounds;
            public bool Visible = true;
        }

        private readonly List<Chunk> _chunks = new List<Chunk>();
        private GameObject _apron;
        private Mesh _apronMesh;
        private readonly List<GameObject> _buildings = new List<GameObject>();
        private Vector2Int _builtSize;
        private int _builtChunkCells;
        private Material _seaMaterial;
        private Material _avenueMaterial;
        private Texture2D _clearTexture;
        private Texture2D _heightMap;
        private MaterialPropertyBlock _glowBlock;

        public CityMapData Map => _map;
        public CityMapRandomizer Randomizer => _randomizer;

        // From land toward the open sea in grid steps, (0, +1) when the last column is the coast; zero when no edge is
        // mostly sea. Set on rebuild.
        public Vector2Int SeaOutward { get; private set; }

        // How far the open sea reaches past the coast edge, in cells; 0 when there is no coast.
        public int SeaApronCells => SeaOutward == Vector2Int.zero ? 0 : _seaApronCells;

        // Lights a building with the shader's selection glow: rgb is the colour, alpha the strength, and
        // an alpha of 0 turns it off. The index is the building's place in Map.Buildings. Cleared when the
        // buildings are rebuilt.
        public void SetBuildingGlow(int index, Color glow)
        {
            if (index < 0 || index >= _buildings.Count || _buildings[index] == null)
            {
                return;
            }

            if (!_buildings[index].TryGetComponent(out MeshRenderer meshRenderer))
            {
                return;
            }

            _glowBlock ??= new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(_glowBlock);
            _glowBlock.SetColor(SelectionColorId, glow);
            meshRenderer.SetPropertyBlock(_glowBlock);
        }

        // Draws another map, such as one generated for the run.
        public void Show(CityMapData map)
        {
            _map = map;
            Rebuild();
        }

        #region Lifecycle

        private void OnEnable()
        {
            Rebuild();
        }

        private void Update()
        {
            // The shadow test maps world positions to cells, so it follows the map if the view moves.
            Shader.SetGlobalVector(MapOriginId, transform.position);

            // Edit mode draws everything, so the Scene view and the painter never lose chunks off the game camera.
            if (Application.isPlaying)
            {
                Cull();
            }
        }

        private void OnDisable()
        {
            Clear();
            Shader.SetGlobalVector(HeightMapSizeId, Vector4.zero);
            DestroyOwned(_seaMaterial);
            DestroyOwned(_avenueMaterial);
            DestroyOwned(_clearTexture);
            DestroyOwned(_heightMap);
            _glow.Release();
        }

        private void OnValidate()
        {
            // Glow tuning shows straight away; the bake is cheap next to rebuilding the ground.
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
            _builtChunkCells = _chunkCells;
            for (int row = 0; row < _builtSize.x; row += _builtChunkCells)
            {
                for (int column = 0; column < _builtSize.y; column += _builtChunkCells)
                {
                    var cells = new RectInt(
                        row,
                        column,
                        Mathf.Min(_builtChunkCells, _builtSize.x - row),
                        Mathf.Min(_builtChunkCells, _builtSize.y - column)
                    );
                    _chunks.Add(CreateChunk(cells));
                }
            }

            RebuildBuildings();
            BuildSeaApron();
        }

        // Re-picks the art of these cells and their neighbours; call after painting ground or roads. Rebuilds only
        // the chunks those cells fall in.
        public void RefreshCells(IEnumerable<Vector2Int> cells)
        {
            if (NeedsFullRebuild())
            {
                Rebuild();
                return;
            }

            var dirty = new HashSet<int>();
            foreach (Vector2Int cell in cells)
            {
                for (int row = -1; row <= 1; row++)
                {
                    for (int column = -1; column <= 1; column++)
                    {
                        Vector2Int neighbour = cell + new Vector2Int(row, column);
                        if (_map.InBounds(neighbour))
                        {
                            dirty.Add(ChunkIndex(neighbour));
                        }
                    }
                }
            }

            foreach (int index in dirty)
            {
                BuildChunkMesh(_chunks[index]);
                UpdateChunkBounds(_chunks[index]);
            }
        }

        public void RebuildBuildings()
        {
            for (int index = 0; index < _buildings.Count; index++)
            {
                DestroyOwned(_buildings[index]);
            }

            _buildings.Clear();
            foreach (Chunk chunk in _chunks)
            {
                chunk.Buildings.Clear();
            }

            PublishHeightMap();
            _glow.Publish(_map);
            if (_map != null && _buildingPrefab != null && _chunks.Count > 0)
            {
                for (int index = 0; index < _map.Buildings.Count; index++)
                {
                    _buildings.Add(CreateBuilding(_map.Buildings[index]));
                }
            }

            foreach (Chunk chunk in _chunks)
            {
                UpdateChunkBounds(chunk);
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
                && (_builtSize != _map.Size || _builtChunkCells != _chunkCells || _chunks.Count == 0);
        }

        #endregion

        #region Private API

        private int ChunksAcross => (_builtSize.y + _builtChunkCells - 1) / _builtChunkCells;

        private int ChunkIndex(Vector2Int cell)
        {
            return ((cell.x / _builtChunkCells) * ChunksAcross) + (cell.y / _builtChunkCells);
        }

        private Chunk CreateChunk(RectInt cells)
        {
            var chunk = new Chunk
            {
                Cells = cells,
                Root = new GameObject($"Chunk {cells.x},{cells.y}")
                {
                    hideFlags = HideFlags.HideAndDontSave,
                },
                Mesh = new Mesh { name = $"City Chunk {cells.x},{cells.y}", hideFlags = HideFlags.HideAndDontSave },
            };
            chunk.Root.transform.SetParent(transform, false);
            chunk.Root.AddComponent<MeshFilter>().sharedMesh = chunk.Mesh;
            chunk.Ground = chunk.Root.AddComponent<MeshRenderer>();
            chunk.Ground.sortingOrder = GROUND_SORTING_ORDER;
            chunk.Ground.sharedMaterials = new[] { _groundMaterial, _seaMaterial, _avenueMaterial };
            BuildChunkMesh(chunk);
            return chunk;
        }

        // Every cell of the chunk as a quad, sorted into the ground, sea and avenue submeshes. Ground cells carry
        // their atlas piece and rotation in uv2 (see the half-cube shader's atlasData), so one mesh draws them all.
        private void BuildChunkMesh(Chunk chunk)
        {
            int cellCount = chunk.Cells.width * chunk.Cells.height;
            var vertices = new List<Vector3>(cellCount * 4);
            var uvs = new List<Vector2>(cellCount * 4);
            var atlas = new List<Vector4>(cellCount * 4);
            var submeshes = new[] { new List<int>(), new List<int>(), new List<int>() };

            foreach (Vector2Int cell in chunk.Cells.allPositionsWithin)
            {
                int submesh = CellLook(cell, out Vector2Int atlasCell, out int quarterTurns);
                Vector3 center = IsoGrid.CellToLocal(cell);
                int first = vertices.Count;
                Vector4 rect = CityRoadTile.AtlasRect(atlasCell.x, atlasCell.y);
                for (int corner = 0; corner < 4; corner++)
                {
                    vertices.Add(center + CellCorners[corner]);
                    uvs.Add(CornerUvs[corner]);
                    // w = 2 marks a baked cell (the shader tests w > 1.5: a missing channel can read w = 1). Sea and
                    // avenue are flat placeholder colours and keep the per-renderer atlas path (w = 0).
                    if (submesh == GROUND_SUBMESH)
                    {
                        Vector2 turned = TurnAtlasUv(CornerUvs[corner], quarterTurns);
                        atlas.Add(
                            new Vector4(rect.x + (turned.x * rect.z), rect.y + (turned.y * rect.w), quarterTurns, 2f)
                        );
                    }
                    else
                    {
                        atlas.Add(Vector4.zero);
                    }
                }

                submeshes[submesh].AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
            }

            // Ground faces up in the 3D city, like a roof; tangents run along +row with uv.x.
            Vector3 up = IsoGrid.UpView;
            var tangent = new Vector4(IsoGrid.RowView.x, IsoGrid.RowView.y, IsoGrid.RowView.z, 1f);
            var normals = new Vector3[vertices.Count];
            var tangents = new Vector4[vertices.Count];
            var faceData = new Vector2[vertices.Count];
            var colors = new Color[vertices.Count];
            for (int index = 0; index < vertices.Count; index++)
            {
                normals[index] = up;
                tangents[index] = tangent;
                colors[index] = Color.white;
            }

            Mesh mesh = chunk.Mesh;
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.normals = normals;
            mesh.tangents = tangents;
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, faceData);
            mesh.SetUVs(2, atlas);
            mesh.colors = colors;
            mesh.subMeshCount = submeshes.Length;
            for (int index = 0; index < submeshes.Length; index++)
            {
                mesh.SetTriangles(submeshes[index], index);
            }

            mesh.RecalculateBounds();
        }

        // Which submesh the cell draws in and, for ground, its atlas piece. Avenues count as road, so a street
        // meeting an avenue joins it.
        private int CellLook(Vector2Int cell, out Vector2Int atlasCell, out int quarterTurns)
        {
            atlasCell = Vector2Int.zero;
            quarterTurns = 0;
            if (_map.GetGround(cell) == CityMapData.Ground.Sea)
            {
                return SEA_SUBMESH;
            }

            CityMapData.Road road = _map.GetRoad(cell);
            if (road == CityMapData.Road.Avenue)
            {
                return AVENUE_SUBMESH;
            }

            if (road == CityMapData.Road.Street)
            {
                (atlasCell, quarterTurns) = RoadByMask[NeighbourMask(cell)];
                return GROUND_SUBMESH;
            }

            // Cosmetic only: a fixed hash so a cell keeps its look between rebuilds.
            atlasCell =
                ((cell.x * 73856093) ^ (cell.y * 19349663)) % 6 == 0
                    ? PavementCracked
                    : PavementPlain;
            return GROUND_SUBMESH;
        }

        // The coast is the map edge with the most sea, if more than half of it is sea.
        private Vector2Int DetectSeaOutward()
        {
            Vector2Int size = _map.Size;
            (Vector2Int outward, int length, System.Func<int, Vector2Int> cell)[] edges =
            {
                (Vector2Int.left, size.y, index => new Vector2Int(0, index)),
                (Vector2Int.right, size.y, index => new Vector2Int(size.x - 1, index)),
                (Vector2Int.down, size.x, index => new Vector2Int(index, 0)),
                (Vector2Int.up, size.x, index => new Vector2Int(index, size.y - 1)),
            };
            Vector2Int best = Vector2Int.zero;
            float bestShare = 0.5f;
            foreach ((Vector2Int outward, int length, System.Func<int, Vector2Int> cell) in edges)
            {
                int sea = 0;
                for (int index = 0; index < length; index++)
                {
                    sea += _map.GetGround(cell(index)) == CityMapData.Ground.Sea ? 1 : 0;
                }

                float share = sea / (float)Mathf.Max(1, length);
                if (share > bestShare)
                {
                    best = outward;
                    bestShare = share;
                }
            }

            return best;
        }

        // One flat sea quad past the coast edge, as wide as that edge. Its uvs sit mid-face so the shader's edge
        // highlight and selection glow never draw on it; it has no atlas data, so it takes the sea material's look.
        private void BuildSeaApron()
        {
            SeaOutward = DetectSeaOutward();
            if (SeaOutward == Vector2Int.zero || _seaApronCells <= 0)
            {
                return;
            }

            Vector2 min = new Vector2(-0.5f, -0.5f);
            Vector2 max = new Vector2(_builtSize.x - 0.5f, _builtSize.y - 0.5f);
            if (SeaOutward.x > 0)
            {
                min.x = max.x;
                max.x += _seaApronCells;
            }
            else if (SeaOutward.x < 0)
            {
                max.x = min.x;
                min.x -= _seaApronCells;
            }
            else if (SeaOutward.y > 0)
            {
                min.y = max.y;
                max.y += _seaApronCells;
            }
            else
            {
                max.y = min.y;
                min.y -= _seaApronCells;
            }

            _apronMesh = new Mesh { name = "City Sea Apron", hideFlags = HideFlags.HideAndDontSave };
            _apronMesh.vertices = new[]
            {
                IsoGrid.CellToLocal(min.x, min.y),
                IsoGrid.CellToLocal(max.x, min.y),
                IsoGrid.CellToLocal(max.x, max.y),
                IsoGrid.CellToLocal(min.x, max.y),
            };
            Vector3 up = IsoGrid.UpView;
            var tangent = new Vector4(IsoGrid.RowView.x, IsoGrid.RowView.y, IsoGrid.RowView.z, 1f);
            _apronMesh.normals = new[] { up, up, up, up };
            _apronMesh.tangents = new[] { tangent, tangent, tangent, tangent };
            var middle = new Vector2(0.5f, 0.5f);
            _apronMesh.uv = new[] { middle, middle, middle, middle };
            _apronMesh.SetUVs(1, new[] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero });
            _apronMesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            _apronMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            _apronMesh.RecalculateBounds();

            _apron = new GameObject("Sea Apron") { hideFlags = HideFlags.HideAndDontSave };
            _apron.transform.SetParent(transform, false);
            _apron.AddComponent<MeshFilter>().sharedMesh = _apronMesh;
            MeshRenderer apronRenderer = _apron.AddComponent<MeshRenderer>();
            apronRenderer.sortingOrder = GROUND_SORTING_ORDER;
            apronRenderer.sharedMaterial = _seaMaterial;
        }

        // The half-cube shader's TransformAtlasUV for a quarter-turn count, baked on the CPU.
        private static Vector2 TurnAtlasUv(Vector2 uv, int quarterTurns)
        {
            switch (quarterTurns)
            {
                case 1:
                    return new Vector2(uv.y, 1f - uv.x);
                case 2:
                    return new Vector2(1f - uv.x, 1f - uv.y);
                case 3:
                    return new Vector2(1f - uv.y, uv.x);
                default:
                    return uv;
            }
        }

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

        // Parented to the chunk its front cell is in, so culling switches it with that chunk.
        private GameObject CreateBuilding(CityMapData.Building building)
        {
            Vector2Int front = building.FrontCell;
            Chunk chunk = _chunks[ChunkIndex(front)];
            GameObject instance = Instantiate(_buildingPrefab, chunk.Root.transform, false);
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
                meshRenderer.enabled = chunk.Visible;
                chunk.Buildings.Add(meshRenderer);
            }

            return instance;
        }

        // The chunk's ground plus every building on it, roofs included, so a tall building never pops in late.
        private static void UpdateChunkBounds(Chunk chunk)
        {
            chunk.Bounds = chunk.Ground.bounds;
            foreach (MeshRenderer building in chunk.Buildings)
            {
                if (building != null)
                {
                    chunk.Bounds.Encapsulate(building.bounds);
                }
            }
        }

        // Switches each chunk's renderers on or off by whether its bounds touch the camera's view plus the margin.
        // Renderers, not GameObjects: disabling a building would make HalfCubeBuilding throw its mesh away and
        // rebuild it each time it scrolls back into view.
        // ponytail: orthographic cameras only, which is all the city uses; a perspective camera skips culling.
        private void Cull()
        {
            Camera viewer = _cullCamera != null ? _cullCamera : Camera.main;
            if (viewer == null || !viewer.orthographic)
            {
                return;
            }

            float halfHeight = viewer.orthographicSize + _cullMargin;
            float halfWidth = (viewer.orthographicSize * viewer.aspect) + _cullMargin;
            Vector3 center = viewer.transform.position;
            var view = Rect.MinMaxRect(
                center.x - halfWidth,
                center.y - halfHeight,
                center.x + halfWidth,
                center.y + halfHeight
            );
            foreach (Chunk chunk in _chunks)
            {
                Vector3 min = chunk.Bounds.min;
                Vector3 max = chunk.Bounds.max;
                bool visible = view.Overlaps(Rect.MinMaxRect(min.x, min.y, max.x, max.y));
                if (visible == chunk.Visible)
                {
                    continue;
                }

                chunk.Visible = visible;
                chunk.Ground.enabled = visible;
                foreach (MeshRenderer building in chunk.Buildings)
                {
                    if (building != null)
                    {
                        building.enabled = visible;
                    }
                }
            }
        }

        private void EnsureSharedAssets()
        {
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

        // Destroying a chunk's root destroys its buildings with it.
        private void Clear()
        {
            foreach (Chunk chunk in _chunks)
            {
                DestroyOwned(chunk.Root);
                DestroyOwned(chunk.Mesh);
            }

            _chunks.Clear();
            _buildings.Clear();
            DestroyOwned(_apron);
            DestroyOwned(_apronMesh);
            _apron = null;
            _apronMesh = null;
            SeaOutward = Vector2Int.zero;
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
