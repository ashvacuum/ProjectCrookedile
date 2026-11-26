using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = System.Random;

namespace Crookedile.Gameplay.City
{
    // A neighbourhood on the generated city (city-generator.md §4.5): cells within Radius of Center build their streets
    // and lots to this profile; where areas overlap the nearest centre wins. An infinite Radius makes the area the town
    // fallback: it covers only cells no other area does. The campaign fills these from its neighbourhood types.
    public struct CityArea
    {
        public Vector2Int Center;
        public float Radius;
        public Vector2Int Storeys;
        public Vector2Int LotSize;
        public float LotFill;
        public Vector2Int BlockCells;
        public float StreetBranchChance;
        // Regular streets every block across the area, anchored on Center; false leaves organic marching streets.
        public bool Grid;
        public float GableChance;
        public Vector2 RoofPitch;
        public IReadOnlyList<Color> RoofColors;
    }

    // Fills a CityMapData with a random city from a seed: coast, a highway down the length of the land, winding two-cell
    // avenues out of downtown, streets that march off the avenues until they die or close a block, and lots along the
    // roads. Inside a neighbourhood (CityArea) streets and lots follow its profile; elsewhere lots grow taller toward
    // downtown. A stand-in for the generator (city-generator.md §4).
    // ponytail: axis-only avenues, no landmarks. The §4 generator replaces this once diagonal art, landmarks and
    // BuildingData exist.
    [Serializable]
    public sealed class CityMapRandomizer
    {
        private static readonly Vector2Int[] Axes =
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down,
        };

        [Title("Coast")]

        [Tooltip(
            "Sea depth from the coast edge, in cells (min, max). The coastline wanders inside this range."
        )]
        [SerializeField, MinMaxSlider(1, 20, true), SuffixLabel("cells", true)]
        private Vector2Int _coastDepth = new Vector2Int(3, 8);

        [Tooltip(
            "On a map longer one way than the other, put the sea along one of the two long sides, so the city strings along the shore like Cebu. Off, or on a square map, any edge can be the coast."
        )]
        [SerializeField]
        private bool _coastOnLongSide = true;

        [Tooltip(
            "Put the sea on an edge facing the camera (the bottom of the screen: row 0 or the last column), so the city looks out to sea from the player's side."
        )]
        [SerializeField]
        private bool _coastFacesCamera = true;

        [Tooltip(
            "A straight two-cell highway running the whole length of the land, parallel to the coast, like a city's main north-south road."
        )]
        [SerializeField]
        private bool _highway = true;

        [Tooltip("Where the highway runs across the land: 0 = just inland of the coast, 1 = the far edge.")]
        [SerializeField, Range(0f, 1f), ShowIf(nameof(_highway))]
        private float _highwayInland = 0.5f;

        [Tooltip(
            "Open patches of park (fields, plazas, empty land) scattered over the city (min, max). Nothing is built or paved there, so the city breathes."
        )]
        [SerializeField, MinMaxSlider(0, 40, true)]
        private Vector2Int _openPatches = new Vector2Int(18, 28);

        [Tooltip("Radius of an open patch, in cells (min, max). Edges are ragged.")]
        [SerializeField, MinMaxSlider(1f, 16f, true), SuffixLabel("cells", true)]
        private Vector2 _openPatchRadius = new Vector2(2.5f, 7f);

        [Tooltip(
            "Open patches keep at least this far, beyond their own radius, from a neighbourhood's centre, so a district never sits in an empty field."
        )]
        [SerializeField, Min(0f), SuffixLabel("cells", true)]
        private float _openPatchClearance = 6f;

        [Tooltip(
            "Chance per cell along the coast that its depth steps in or out by one. Low keeps the shore smooth."
        )]
        [SerializeField, Range(0f, 1f)]
        private float _coastWobbleChance = 0.25f;

        [Tooltip(
            "How far downtown may sit from the middle of the coast, as a fraction of the coast's length."
        )]
        [SerializeField, Range(0f, 0.5f)]
        private float _downtownOffset = 0.2f;

        [Title("Avenues")]

        [Tooltip("Number of avenues leaving downtown (min, max).")]
        [SerializeField, MinMaxSlider(1, 8, true)]
        private Vector2Int _avenueCount = new Vector2Int(3, 5);

        [Tooltip("Length of each straight avenue run, in cells (min, max).")]
        [SerializeField, MinMaxSlider(2, 40, true), SuffixLabel("cells", true)]
        private Vector2Int _avenueRun = new Vector2Int(10, 20);

        [Tooltip(
            "Chance that an avenue jogs sideways after a run instead of carrying straight on."
        )]
        [SerializeField, Range(0f, 1f)]
        private float _avenueJogChance = 0.3f;

        [Tooltip(
            "Length of a sideways jog, in cells (min, max). Short jogs keep the avenue reading as one boulevard."
        )]
        [SerializeField, MinMaxSlider(1, 10, true), SuffixLabel("cells", true)]
        private Vector2Int _avenueJog = new Vector2Int(2, 4);

        [Title("Streets")]

        [Tooltip(
            "Chance that each avenue-side cell starts a street marching away from the avenue."
        )]
        [SerializeField, Range(0f, 1f)]
        private float _streetStartChance = 0.06f;

        [Tooltip(
            "Cells between side streets, so block length (min, max). Min is also the shortest street kept."
        )]
        [SerializeField, MinMaxSlider(2, 16, true), SuffixLabel("cells", true)]
        private Vector2Int _streetBlock = new Vector2Int(4, 7);

        [Tooltip(
            "Chance of a side street on each side at every block, at downtown (X) and at the far edge (Y). The falloff thins the town outward."
        )]
        [SerializeField, LabelText("Street Branch Chance (Downtown, Edge)")]
        private Vector2 _streetBranchChance = new Vector2(0.8f, 0.15f);

        [Tooltip(
            "Chance that a street turns left or right at a block corner instead of carrying straight on."
        )]
        [SerializeField, Range(0f, 1f)]
        private float _streetTurnChance = 0.1f;

        [Tooltip(
            "Chance per cell that a street just stops, leaving a dead end. Streets also stop on meeting a road (closing a block), at the sea or the map edge, or beside a parallel road; one that stops short of a block without meeting a road is erased."
        )]
        [SerializeField, Range(0f, 0.5f)]
        private float _streetDeathChance = 0.01f;

        [Title("Lots and buildings")]

        [Tooltip("Lot side length, in cells (min, max).")]
        [SerializeField, MinMaxSlider(1, 6, true), SuffixLabel("cells", true)]
        private Vector2Int _lotSize = new Vector2Int(1, 3);

        [Tooltip("Chance that a free lot gets a building; the rest stay empty land.")]
        [SerializeField, Range(0f, 1f)]
        private float _lotFill = 0.85f;

        [Tooltip("Storeys of a building at downtown (min, max).")]
        [SerializeField, MinMaxSlider(1, 12, true)]
        private Vector2Int _downtownStoreys = new Vector2Int(2, 4);

        [Tooltip("Storeys of a building at the far edge of town (min, max).")]
        [SerializeField, MinMaxSlider(1, 12, true)]
        private Vector2Int _edgeStoreys = new Vector2Int(1, 1);

        [Tooltip("Wall height of one storey, in world units.")]
        [SerializeField, Min(0.05f), SuffixLabel("units", true)]
        private float _storeyHeight = 0.5f;

        // Returns downtown, the cell district placement measures from. chooseAreas runs once the coast, highway and
        // avenues are down and before streets and lots, so a caller can centre neighbourhoods on what is there; null
        // gives a city with no neighbourhoods.
        public Vector2Int Fill(
            CityMapData map,
            int seed,
            Func<CityMapData, Vector2Int, IReadOnlyList<CityArea>> chooseAreas = null
        )
        {
            Random rng = new Random(seed);
            map.Clear();
            Vector2Int size = map.Size;

            // The sea edge, and inland pointing away from it. Only one edge is sea; the other three are land.
            Vector2Int inland = PickInland(rng, size);
            Vector2Int along = new Vector2Int(Mathf.Abs(inland.y), Mathf.Abs(inland.x));
            int coastLength = along.x != 0 ? size.x : size.y;
            int depthLimit = inland.x != 0 ? size.x : size.y;
            int depth = Range(rng, _coastDepth);
            int[] coast = new int[coastLength];
            for (int step = 0; step < coastLength; step++)
            {
                if (rng.NextDouble() < _coastWobbleChance)
                {
                    depth = Mathf.Clamp(depth + rng.Next(-1, 2), _coastDepth.x, _coastDepth.y);
                }

                coast[step] = depth;
                for (int into = 0; into < Mathf.Min(depth, depthLimit); into++)
                {
                    map.SetGround(
                        FromEdge(size, inland, along, step, into),
                        CityMapData.Ground.Sea
                    );
                }
            }

            int downtownStep = Mathf.Clamp(
                Mathf.RoundToInt(
                    coastLength * (0.5f + (((float)rng.NextDouble() * 2f) - 1f) * _downtownOffset)
                ),
                0,
                coastLength - 1
            );
            Vector2Int downtown = FromEdge(size, inland, along, downtownStep, coast[downtownStep]);

            if (_highway)
            {
                PaintHighway(map, inland, along, coastLength, depthLimit);
            }

            // Avenues head inland or along the coast, never back to sea.
            Vector2Int[] headings = { inland, along, -along };
            int avenues = Range(rng, _avenueCount);
            for (int index = 0; index < avenues; index++)
            {
                PaintAvenue(map, rng, downtown, headings[index % headings.Length], inland);
            }

            IReadOnlyList<CityArea> areas = chooseAreas?.Invoke(map, downtown) ?? Array.Empty<CityArea>();
            CarveOpenPatches(map, rng, areas);
            PaintGrids(map, rng, areas);
            MarchStreets(map, rng, downtown, areas);
            PlaceBuildings(map, rng, downtown, areas);
            return downtown;
        }

        // A straight two-cell avenue the length of the coast, at the inland fraction between the deepest coast and the
        // far edge. Where the coast reaches past it, the sea wins.
        private void PaintHighway(
            CityMapData map,
            Vector2Int inland,
            Vector2Int along,
            int coastLength,
            int depthLimit
        )
        {
            int nearest = Mathf.Min(_coastDepth.y + 1, depthLimit - 2);
            int into = Mathf.RoundToInt(Mathf.Lerp(nearest, depthLimit - 2, _highwayInland));
            for (int step = 0; step < coastLength; step++)
            {
                for (int lane = 0; lane < 2; lane++)
                {
                    map.SetRoad(
                        FromEdge(map.Size, inland, along, step, into + lane),
                        CityMapData.Road.Avenue
                    );
                }
            }
        }

        // The neighbourhood a cell belongs to: the nearest centre within its radius, else the town fallback, or none.
        private static bool TryAreaAt(IReadOnlyList<CityArea> areas, Vector2Int cell, out CityArea area)
        {
            int index = AreaIndexAt(areas, cell);
            area = index >= 0 ? areas[index] : default;
            return index >= 0;
        }

        private static int AreaIndexAt(IReadOnlyList<CityArea> areas, Vector2Int cell)
        {
            int nearest = -1;
            int fallback = -1;
            float best = float.MaxValue;
            for (int index = 0; index < areas.Count; index++)
            {
                if (float.IsPositiveInfinity(areas[index].Radius))
                {
                    fallback = fallback < 0 ? index : fallback;
                    continue;
                }

                float distance = Vector2.Distance(cell, areas[index].Center);
                if (distance <= areas[index].Radius && distance < best)
                {
                    best = distance;
                    nearest = index;
                }
            }

            return nearest >= 0 ? nearest : fallback;
        }

        private static bool InGridArea(IReadOnlyList<CityArea> areas, Vector2Int cell)
        {
            int index = AreaIndexAt(areas, cell);
            return index >= 0 && areas[index].Grid;
        }

        // Ragged circles of park on open land (not sea, not road), kept off neighbourhood centres. Laid after the
        // highway and avenues, which run through them, and before streets and lots, which then avoid them.
        private void CarveOpenPatches(CityMapData map, Random rng, IReadOnlyList<CityArea> areas)
        {
            Vector2Int size = map.Size;
            int patches = Range(rng, _openPatches);
            float maxRadius = Mathf.Max(_openPatchRadius.x, _openPatchRadius.y);
            for (int patch = 0; patch < patches; patch++)
            {
                float radius = Mathf.Lerp(_openPatchRadius.x, _openPatchRadius.y, (float)rng.NextDouble());
                Vector2Int centre = default;
                bool placed = false;
                // A few tries for a spot clear of every neighbourhood centre; a crowded city gets fewer patches.
                for (int attempt = 0; attempt < 12 && !placed; attempt++)
                {
                    centre = new Vector2Int(rng.Next(size.x), rng.Next(size.y));
                    placed = IsFree(map, centre) && ClearOfCentres(areas, centre, radius + _openPatchClearance);
                }

                if (!placed)
                {
                    continue;
                }

                int reach = Mathf.CeilToInt(maxRadius * 1.2f);
                for (int row = centre.x - reach; row <= centre.x + reach; row++)
                {
                    for (int column = centre.y - reach; column <= centre.y + reach; column++)
                    {
                        var cell = new Vector2Int(row, column);
                        if (!IsFree(map, cell))
                        {
                            continue;
                        }

                        // A cell hash wobbles the edge between 80% and 120% of the radius.
                        float wobble = 0.8f + (0.4f * Hash01(cell, patch));
                        if (Vector2.Distance(cell, centre) <= radius * wobble)
                        {
                            map.SetGround(cell, CityMapData.Ground.Park);
                        }
                    }
                }
            }
        }

        private static bool ClearOfCentres(IReadOnlyList<CityArea> areas, Vector2Int cell, float distance)
        {
            foreach (CityArea area in areas)
            {
                if (!float.IsPositiveInfinity(area.Radius) && Vector2.Distance(cell, area.Center) < distance)
                {
                    return false;
                }
            }

            return true;
        }

        private static float Hash01(Vector2Int cell, int salt)
        {
            uint hash = unchecked((uint)((cell.x * 73856093) ^ (cell.y * 19349663) ^ (salt * 83492791)));
            hash ^= hash >> 13;
            hash = unchecked(hash * 1274126177u);
            hash ^= hash >> 16;
            return (hash & 0xFFFF) / 65535f;
        }

        // Regular streets across each grid neighbourhood: one every block in both directions, anchored on the area's
        // centre (the town fallback's on downtown) with spacing rolled once per area from its block length. A grid line
        // that would run alongside an existing road is skipped, so it joins the road instead of doubling it.
        // ponytail: axis-aligned grids only; a rotated grid needs diagonal road art.
        private void PaintGrids(CityMapData map, Random rng, IReadOnlyList<CityArea> areas)
        {
            var spacing = new Vector2Int[areas.Count];
            bool anyGrid = false;
            for (int index = 0; index < areas.Count; index++)
            {
                if (!areas[index].Grid)
                {
                    continue;
                }

                anyGrid = true;
                spacing[index] = new Vector2Int(
                    Range(rng, areas[index].BlockCells) + 1,
                    Range(rng, areas[index].BlockCells) + 1
                );
            }

            if (!anyGrid)
            {
                return;
            }

            Vector2Int size = map.Size;
            for (int row = 0; row < size.x; row++)
            {
                for (int column = 0; column < size.y; column++)
                {
                    var cell = new Vector2Int(row, column);
                    if (!IsFree(map, cell))
                    {
                        continue;
                    }

                    int index = AreaIndexAt(areas, cell);
                    if (index < 0 || !areas[index].Grid)
                    {
                        continue;
                    }

                    Vector2Int offset = cell - areas[index].Center;
                    bool onRowLine = Modulo(offset.x, spacing[index].x) == 0;
                    bool onColumnLine = Modulo(offset.y, spacing[index].y) == 0;
                    if (!onRowLine && !onColumnLine)
                    {
                        continue;
                    }

                    // A line along one axis that would sit beside a road on the other runs alongside it: skip.
                    bool besideRoad = onRowLine && !onColumnLine
                        ? IsRoad(map, cell + Vector2Int.right) || IsRoad(map, cell + Vector2Int.left)
                        : !onRowLine && onColumnLine
                            && (IsRoad(map, cell + Vector2Int.up) || IsRoad(map, cell + Vector2Int.down));
                    if (!besideRoad)
                    {
                        map.SetRoad(cell, CityMapData.Road.Street);
                    }
                }
            }
        }

        private static int Modulo(int value, int divisor)
        {
            int result = value % divisor;
            return result < 0 ? result + divisor : result;
        }

        private void PaintAvenue(
            CityMapData map,
            Random rng,
            Vector2Int start,
            Vector2Int primary,
            Vector2Int inland
        )
        {
            Vector2Int cell = start;
            Vector2Int heading = primary;
            int guard = (map.Size.x + map.Size.y) * 4;
            while (map.InBounds(cell) && guard-- > 0)
            {
                int run = Range(rng, heading == primary ? _avenueRun : _avenueJog);
                for (int step = 0; step < run && map.InBounds(cell); step++)
                {
                    // A 2 x 2 brush keeps the avenue two cells wide on either axis and fills its corners.
                    for (int row = 0; row < 2; row++)
                    {
                        for (int column = 0; column < 2; column++)
                        {
                            map.SetRoad(
                                cell + new Vector2Int(row, column),
                                CityMapData.Road.Avenue
                            );
                        }
                    }

                    cell += heading;
                }

                if (heading != primary || rng.NextDouble() >= _avenueJogChance)
                {
                    heading = primary;
                    continue;
                }

                // Jog to either side, except back toward the sea.
                Vector2Int side =
                    new Vector2Int(primary.y, primary.x) * (rng.Next(2) == 0 ? 1 : -1);
                heading = side == -inland ? -side : side;
            }
        }

        // Streets are walkers that start beside avenues and march straight one cell at a time. At every block corner they
        // may sprout a side street each way (less often far from downtown) and rarely turn. Meeting another road closes a
        // block and ends the walker; one that stops short of a block without meeting a road is erased as a stub.
        private void MarchStreets(
            CityMapData map,
            Random rng,
            Vector2Int downtown,
            IReadOnlyList<CityArea> areas
        )
        {
            Queue<(Vector2Int cell, Vector2Int heading)> walkers =
                new Queue<(Vector2Int, Vector2Int)>();
            Vector2Int size = map.Size;
            for (int row = 0; row < size.x; row++)
            {
                for (int column = 0; column < size.y; column++)
                {
                    Vector2Int cell = new Vector2Int(row, column);
                    if (map.GetRoad(cell) != CityMapData.Road.Avenue)
                    {
                        continue;
                    }

                    foreach (Vector2Int axis in Axes)
                    {
                        if (IsFree(map, cell + axis) && rng.NextDouble() < _streetStartChance)
                        {
                            walkers.Enqueue((cell, axis));
                        }
                    }
                }
            }

            int budget = size.x * size.y;
            List<Vector2Int> painted = new List<Vector2Int>();
            while (walkers.Count > 0 && budget > 0)
            {
                (Vector2Int cell, Vector2Int heading) = walkers.Dequeue();
                Vector2Int blockRange = TryAreaAt(areas, cell, out CityArea startArea)
                    ? startArea.BlockCells
                    : _streetBlock;
                int shortest = Mathf.Min(blockRange.x, blockRange.y);
                int block = Range(rng, blockRange);
                int sinceCorner = 0;
                bool joined = false;
                painted.Clear();
                while (budget-- > 0)
                {
                    Vector2Int next = cell + heading;
                    Vector2Int side = new Vector2Int(heading.y, heading.x);
                    if (IsRoad(map, next))
                    {
                        joined = true;
                        break;
                    }

                    if (
                        !IsFree(map, next)
                        || IsRoad(map, next + side)
                        || IsRoad(map, next - side)
                        || InGridArea(areas, next)
                    )
                    {
                        // Sea, park, map edge, about to run alongside a road, or into a grid that has its own streets.
                        break;
                    }

                    map.SetRoad(next, CityMapData.Road.Street);
                    painted.Add(next);
                    cell = next;
                    if (rng.NextDouble() < _streetDeathChance)
                    {
                        break;
                    }

                    if (++sinceCorner < block)
                    {
                        continue;
                    }

                    sinceCorner = 0;
                    float branchChance;
                    if (TryAreaAt(areas, cell, out CityArea area))
                    {
                        block = Range(rng, area.BlockCells);
                        branchChance = area.StreetBranchChance;
                    }
                    else
                    {
                        block = Range(rng, _streetBlock);
                        branchChance = Mathf.Lerp(
                            _streetBranchChance.x,
                            _streetBranchChance.y,
                            Falloff(map, cell, downtown)
                        );
                    }
                    for (int sign = -1; sign <= 1; sign += 2)
                    {
                        if (rng.NextDouble() < branchChance)
                        {
                            walkers.Enqueue((cell, side * sign));
                        }
                    }

                    if (rng.NextDouble() < _streetTurnChance)
                    {
                        heading = side * (rng.Next(2) == 0 ? 1 : -1);
                    }
                }

                if (!joined && painted.Count < shortest)
                {
                    foreach (Vector2Int stub in painted)
                    {
                        map.SetRoad(stub, CityMapData.Road.None);
                    }
                }
            }
        }

        // 0 at downtown, 1 from halfway across the map's diagonal outward.
        private static float Falloff(CityMapData map, Vector2Int cell, Vector2Int downtown)
        {
            float farthest = Mathf.Max(1f, new Vector2(map.Size.x, map.Size.y).magnitude);
            return Mathf.Clamp01(Vector2.Distance(cell, downtown) / farthest * 2f);
        }

        private static bool IsRoad(CityMapData map, Vector2Int cell)
        {
            return map.GetRoad(cell) != CityMapData.Road.None;
        }

        // Whether any cell just outside the lot's four sides is a road.
        private static bool TouchesRoad(CityMapData map, RectInt lot)
        {
            for (int row = lot.xMin; row < lot.xMax; row++)
            {
                if (
                    IsRoad(map, new Vector2Int(row, lot.yMin - 1))
                    || IsRoad(map, new Vector2Int(row, lot.yMax))
                )
                {
                    return true;
                }
            }

            for (int column = lot.yMin; column < lot.yMax; column++)
            {
                if (
                    IsRoad(map, new Vector2Int(lot.xMin - 1, column))
                    || IsRoad(map, new Vector2Int(lot.xMax, column))
                )
                {
                    return true;
                }
            }

            return false;
        }

        private void PlaceBuildings(
            CityMapData map,
            Random rng,
            Vector2Int downtown,
            IReadOnlyList<CityArea> areas
        )
        {
            Vector2Int size = map.Size;
            bool[] taken = new bool[size.x * size.y];
            for (int row = 0; row < size.x; row++)
            {
                for (int column = 0; column < size.y; column++)
                {
                    Vector2Int cell = new Vector2Int(row, column);
                    if (taken[(row * size.y) + column] || !IsFree(map, cell))
                    {
                        continue;
                    }

                    // A neighbourhood's profile, or the town's: lots taller toward downtown.
                    bool inArea = TryAreaAt(areas, cell, out CityArea area);
                    Vector2Int lotSize = inArea ? area.LotSize : _lotSize;

                    // Every building fronts a road. A lot that touches none is left free for a later lot to cover
                    // part of; cells no road-touching lot reaches stay open land (yards, block interiors).
                    RectInt lot = GrowLot(
                        map,
                        taken,
                        cell,
                        Range(rng, lotSize),
                        Range(rng, lotSize)
                    );
                    if (!TouchesRoad(map, lot))
                    {
                        continue;
                    }

                    foreach (Vector2Int used in lot.allPositionsWithin)
                    {
                        taken[(used.x * size.y) + used.y] = true;
                    }

                    if (rng.NextDouble() >= (inArea ? area.LotFill : _lotFill))
                    {
                        continue;
                    }

                    if (inArea)
                    {
                        // Always three roof draws, so retuning a type's roofs never moves the city's lots or heights.
                        float height = Range(rng, area.Storeys) * _storeyHeight;
                        bool gable = rng.NextDouble() < area.GableChance;
                        float pitch = Mathf.Lerp(area.RoofPitch.x, area.RoofPitch.y, (float)rng.NextDouble());
                        int colorCount = area.RoofColors != null ? area.RoofColors.Count : 0;
                        int colorIndex = rng.Next(Mathf.Max(1, colorCount));
                        Color color = colorCount > 0 ? area.RoofColors[colorIndex] : Color.white;
                        map.AddBuilding(lot, height, gable ? RoofStyle.Gable : RoofStyle.Flat, pitch, color);
                        continue;
                    }

                    float distance = Falloff(map, cell, downtown);
                    int low = Mathf.RoundToInt(
                        Mathf.Lerp(_downtownStoreys.x, _edgeStoreys.x, distance)
                    );
                    int high = Mathf.RoundToInt(
                        Mathf.Lerp(_downtownStoreys.y, _edgeStoreys.y, distance)
                    );
                    map.AddBuilding(lot, Range(rng, new Vector2Int(low, high)) * _storeyHeight);
                }
            }
        }

        // The largest lot up to width x length from the cell that stays on free land.
        private static RectInt GrowLot(
            CityMapData map,
            bool[] taken,
            Vector2Int cell,
            int width,
            int length
        )
        {
            int rows = 1;
            while (
                rows < width && Clear(map, taken, new RectInt(cell, new Vector2Int(rows + 1, 1)))
            )
            {
                rows++;
            }

            int columns = 1;
            while (
                columns < length
                && Clear(map, taken, new RectInt(cell, new Vector2Int(rows, columns + 1)))
            )
            {
                columns++;
            }

            return new RectInt(cell, new Vector2Int(rows, columns));
        }

        private static bool Clear(CityMapData map, bool[] taken, RectInt lot)
        {
            foreach (Vector2Int cell in lot.allPositionsWithin)
            {
                if (!IsFree(map, cell) || taken[(cell.x * map.Size.y) + cell.y])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsFree(CityMapData map, Vector2Int cell)
        {
            return map.InBounds(cell)
                && map.GetGround(cell) == CityMapData.Ground.Land
                && map.GetRoad(cell) == CityMapData.Road.None;
        }

        // The cell `step` along the sea edge and `into` cells inland from it.
        private static Vector2Int FromEdge(
            Vector2Int size,
            Vector2Int inland,
            Vector2Int along,
            int step,
            int into
        )
        {
            Vector2Int edge = new Vector2Int(
                inland.x < 0 ? size.x - 1 : 0,
                inland.y < 0 ? size.y - 1 : 0
            );
            return edge + (along * step) + (inland * into);
        }

        // Inland points away from the sea. A coast along the long side runs the length of the map, so inland is
        // across the short side. The edges facing the camera are row 0 (inland +row) and the last column (inland
        // -column), the two edges along the bottom of the screen. Each rule narrows the choice; a seeded pick
        // breaks any tie.
        private Vector2Int PickInland(Random rng, Vector2Int size)
        {
            var candidates = new List<Vector2Int>(Axes);
            if (_coastOnLongSide && size.x != size.y)
            {
                candidates.RemoveAll(axis => size.x > size.y ? axis.x != 0 : axis.y != 0);
            }

            if (_coastFacesCamera)
            {
                candidates.RemoveAll(axis => axis != Vector2Int.right && axis != Vector2Int.down);
            }

            return candidates[rng.Next(candidates.Count)];
        }

        private static int Range(Random rng, Vector2Int range)
        {
            return rng.Next(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y) + 1);
        }
    }
}
