using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Campaign;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Crookedile.Gameplay.Campaign
{
    /// <summary>
    /// Builds the campaign city from a seed: roads by recursive block subdivision, then buildings
    /// packed into the blocks those roads leave behind.
    ///
    /// <para><b>Deterministic from the seed alone.</b> The same seed always lays out the same
    /// city, which is what makes a run shareable and a bug reproducible. Uses its own
    /// <see cref="System.Random"/> rather than <c>RandomHelper</c>/<c>UnityEngine.Random</c>, on
    /// purpose and for the same reason <see cref="EncounterPoolData"/> does: seeding the city
    /// must not perturb battle RNG, and vice versa.</para>
    ///
    /// <para><b>Scenery only.</b> This never places a location hotspot. Where an encounter
    /// happens comes from the pool and the day window; the city is what it happens in front of
    /// (`metagame-campaign.md` §1.5).</para>
    ///
    /// Road *sprites* are not chosen here — paint the road tilemap with a `RuleTile` and it
    /// resolves its own corners, T-junctions and straights from the cells this writes.
    /// </summary>
    [Debuggable("Campaign", LogLevel.Info)]
    public class CityGenerator : MonoBehaviour
    {
        #region Inspector
        [Header("Tilemaps")]
        [Tooltip("Flat ground under everything. Chunk renderer mode is fine here.")]
        [SerializeField]
        private Tilemap _groundTilemap;

        [Tooltip("Road cells. Paint a RuleTile here so connectivity resolves itself.")]
        [SerializeField]
        private Tilemap _roadTilemap;

        [Tooltip("Ground fill tile. A RandomTile varies it per cell at no cost.")]
        [SerializeField]
        private TileBase _groundTile;

        [Tooltip("Road tile — a RuleTile, so it picks its own corner/T/straight sprite.")]
        [SerializeField]
        private TileBase _roadTile;

        [Tooltip("Spawned buildings are parented here. Cleared on every generate.")]
        [SerializeField]
        private Transform _buildingRoot;

        [Header("Layout")]
        [Tooltip("City size in cells.")]
        [SerializeField]
        private Vector2Int _size = new(48, 48);

        [Tooltip("A block wider than this gets split by a road. Lower = denser street grid.")]
        [MinValue(4)]
        [SerializeField]
        private int _maxBlock = 12;

        [Tooltip("A split that would leave a block thinner than this is refused instead.")]
        [MinValue(2)]
        [SerializeField]
        private int _minBlock = 4;

        [Header("Content")]
        [Tooltip("Zones ordered centre-outward: element 0 is downtown, the last is the edge.")]
        [SerializeField]
        private List<CityZoneData> _zones = new();

        [Header("Seed")]
        [Tooltip(
            "0 = take the active run's seed, so the city matches the campaign it belongs to. "
                + "Any other value overrides it, for authoring and bug repro."
        )]
        [SerializeField]
        private int _seedOverride;

        #endregion

        #region Runtime state
        /// <summary>Cells already covered by a road or a building footprint.</summary>
        private readonly HashSet<Vector3Int> _occupied = new();

        #endregion

        #region Entry points
        private void Start()
        {
            // Regenerating on load rather than baking into the scene: the city is a function of
            // the seed, so storing it would be storing a cache we'd then have to invalidate.
            Generate();
        }

        /// <summary>Builds the city for the current seed, replacing whatever is there.</summary>
        [Button(ButtonSizes.Large), PropertyOrder(-1)]
        public void Generate()
        {
            var seed = _seedOverride != 0 ? _seedOverride : RunState.Current?.Seed ?? 0;

            Generate(seed);
        }

        /// <summary>Builds the city for <paramref name="seed"/>, replacing whatever is there.</summary>
        public void Generate(int seed)
        {
            if (_groundTilemap == null || _roadTilemap == null || _buildingRoot == null)
            {
                GameLogger.LogWarning<CityGenerator>(
                    "Generate skipped — ground tilemap, road tilemap and building root must all be assigned.",
                    this
                );
                return;
            }

            Clear();

            var rng = new System.Random(seed);
            var bounds = new RectInt(
                0,
                0,
                Mathf.Max(_minBlock, _size.x),
                Mathf.Max(_minBlock, _size.y)
            );

            FillGround(bounds);

            var blocks = new List<RectInt>();
            Subdivide(bounds, rng, blocks);

            var placed = 0;
            foreach (var block in blocks)
                placed += FillBlock(block, bounds, rng);

            GameLogger.LogInfo<CityGenerator>(
                $"City generated — seed {seed}, {blocks.Count} blocks, {placed} buildings.",
                this
            );
        }

        /// <summary>Wipes the tilemaps and every spawned building.</summary>
        [Button]
        public void Clear()
        {
            _occupied.Clear();

            if (_groundTilemap != null)
                _groundTilemap.ClearAllTiles();

            if (_roadTilemap != null)
                _roadTilemap.ClearAllTiles();

            if (_buildingRoot == null)
                return;

            for (var i = _buildingRoot.childCount - 1; i >= 0; i--)
            {
                var child = _buildingRoot.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        #endregion

        #region Layout
        private void FillGround(RectInt bounds)
        {
            if (_groundTile == null)
                return;

            for (var x = bounds.xMin; x < bounds.xMax; x++)
            for (var y = bounds.yMin; y < bounds.yMax; y++)
                _groundTilemap.SetTile(new Vector3Int(x, y, 0), _groundTile);
        }

        /// <summary>
        /// Splits <paramref name="area"/> until every piece is at most <see cref="_maxBlock"/>
        /// across, laying a one-cell road along each split. Leaves in <paramref name="blocks"/>.
        /// </summary>
        private void Subdivide(RectInt area, System.Random rng, List<RectInt> blocks)
        {
            var splitVertically = area.width >= area.height;
            var span = splitVertically ? area.width : area.height;

            // A split costs one cell to the road, so it needs two minimum blocks plus that cell.
            if (span <= _maxBlock || span < _minBlock * 2 + 1)
            {
                blocks.Add(area);
                return;
            }

            // Cut somewhere in the middle band — never so close to an edge that the far side
            // would come out thinner than a block is allowed to be.
            var cut = rng.Next(_minBlock, span - _minBlock);

            if (splitVertically)
            {
                var roadX = area.xMin + cut;
                for (var y = area.yMin; y < area.yMax; y++)
                    SetRoad(new Vector3Int(roadX, y, 0));

                Subdivide(new RectInt(area.xMin, area.yMin, cut, area.height), rng, blocks);
                Subdivide(
                    new RectInt(roadX + 1, area.yMin, area.xMax - roadX - 1, area.height),
                    rng,
                    blocks
                );
            }
            else
            {
                var roadY = area.yMin + cut;
                for (var x = area.xMin; x < area.xMax; x++)
                    SetRoad(new Vector3Int(x, roadY, 0));

                Subdivide(new RectInt(area.xMin, area.yMin, area.width, cut), rng, blocks);
                Subdivide(
                    new RectInt(area.xMin, roadY + 1, area.width, area.yMax - roadY - 1),
                    rng,
                    blocks
                );
            }
        }

        private void SetRoad(Vector3Int cell)
        {
            _occupied.Add(cell);
            if (_roadTile != null)
                _roadTilemap.SetTile(cell, _roadTile);
        }

        #endregion

        #region Buildings
        /// <summary>
        /// Packs one block with buildings from its zone, largest footprint first so the big
        /// pieces get the room they need before the fillers eat it.
        /// </summary>
        private int FillBlock(RectInt block, RectInt bounds, System.Random rng)
        {
            var zone = ZoneFor(block, bounds);
            if (zone == null)
                return 0;

            var placed = 0;

            // ponytail: no rotation — a 3x1 stays 3x1. Author the turned version as its own row
            // if a zone needs both; add rotation here if packing density ever disappoints.
            foreach (var footprint in zone.FootprintsLargestFirst())
                for (var x = block.xMin; x < block.xMax; x++)
                for (var y = block.yMin; y < block.yMax; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    if (!IsFree(cell, footprint, block))
                        continue;

                    var building = Pick(zone, footprint, rng);
                    if (building == null)
                        continue;

                    Place(building, cell, footprint);
                    placed++;
                }

            return placed;
        }

        /// <summary>
        /// Zone by how far the block sits from the centre — element 0 downtown, last at the edge.
        /// </summary>
        private CityZoneData ZoneFor(RectInt block, RectInt bounds)
        {
            if (_zones.Count == 0)
                return null;

            var centre = bounds.center;
            var offset = block.center - centre;

            // Normalised Chebyshev distance: square bands, which read as a city centre rather
            // than the blobby rings a radial distance would give on a square map.
            var reach = Mathf.Max(
                Mathf.Abs(offset.x) / Mathf.Max(1f, bounds.width * 0.5f),
                Mathf.Abs(offset.y) / Mathf.Max(1f, bounds.height * 0.5f)
            );

            var index = Mathf.Clamp(Mathf.FloorToInt(reach * _zones.Count), 0, _zones.Count - 1);

            return _zones[index];
        }

        /// <summary>True when the whole footprint at <paramref name="origin"/> is free and inside the block.</summary>
        private bool IsFree(Vector3Int origin, Vector2Int footprint, RectInt block)
        {
            if (origin.x + footprint.x > block.xMax || origin.y + footprint.y > block.yMax)
                return false;

            for (var dx = 0; dx < footprint.x; dx++)
            for (var dy = 0; dy < footprint.y; dy++)
                if (_occupied.Contains(new Vector3Int(origin.x + dx, origin.y + dy, 0)))
                    return false;

            return true;
        }

        /// <summary>Weighted pick among this zone's rows of exactly <paramref name="footprint"/> size.</summary>
        private static CityBuilding Pick(CityZoneData zone, Vector2Int footprint, System.Random rng)
        {
            var total = 0f;
            foreach (var building in zone.Buildings)
                if (building.IsValid && building.Footprint == footprint)
                    total += building.Weight;

            if (total <= 0f)
                return null;

            var roll = (float)rng.NextDouble() * total;
            foreach (var building in zone.Buildings)
            {
                if (!building.IsValid || building.Footprint != footprint)
                    continue;

                roll -= building.Weight;
                if (roll <= 0f)
                    return building;
            }

            return null;
        }

        private void Place(CityBuilding building, Vector3Int origin, Vector2Int footprint)
        {
            // Position = the average of the footprint's cell centres, so the prefab's
            // base pivot lands on the middle of the block it covers. Averaging beats doing the
            // isometric offset by hand — the tilemap already knows where cells are, and this
            // stays correct for rectangles, where the centre is not on either diagonal.
            var sum = Vector3.zero;
            for (var dx = 0; dx < footprint.x; dx++)
            for (var dy = 0; dy < footprint.y; dy++)
            {
                var cell = new Vector3Int(origin.x + dx, origin.y + dy, 0);
                _occupied.Add(cell);
                sum += _groundTilemap.GetCellCenterWorld(cell);
            }

            var instance = Instantiate(
                building.Prefab,
                sum / (footprint.x * footprint.y),
                Quaternion.identity,
                _buildingRoot
            );
            instance.name = $"{building.Prefab.name} [{origin.x},{origin.y}]";
        }

        #endregion

        #region Self-check
        /// <summary>
        /// The one property worth proving: same seed, same city. Everything else about this
        /// generator is visible by looking at it; determinism is not.
        /// </summary>
        [Button, PropertyOrder(10)]
        private void VerifyDeterminism()
        {
            const int seed = 12345;

            Generate(seed);
            var first = Fingerprint();

            Generate(seed);
            var second = Fingerprint();

            if (first == second)
                GameLogger.LogInfo<CityGenerator>($"Determinism OK — seed {seed} → {first}.", this);
            else
                GameLogger.LogError<CityGenerator>(
                    $"Determinism BROKEN — seed {seed} gave '{first}' then '{second}'.",
                    this
                );
        }

        private string Fingerprint()
        {
            var names = new List<string>(_buildingRoot.childCount);
            for (var i = 0; i < _buildingRoot.childCount; i++)
                names.Add(_buildingRoot.GetChild(i).name);

            names.Sort(string.CompareOrdinal);
            return $"{names.Count}:{string.Join("|", names).GetHashCode()}";
        }

        #endregion
    }
}
