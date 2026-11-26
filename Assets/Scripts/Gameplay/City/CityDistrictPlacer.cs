using System;
using System.Collections.Generic;
using Crookedile.Data.Campaign;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = System.Random;

namespace Crookedile.Gameplay.City
{
    // Places neighbourhoods on a generated city (city-generator.md §4.5, §4.8). First, once the coast, highway and
    // avenues are down, it picks the centre of each area (districts and scenery) by its placement rule; the randomizer
    // then builds that area's streets and lots to its neighbourhood type. Afterwards it puts each district's marker on
    // the tallest building near its centre. Seeded, so a run always places its districts the same way. Placement is drawing only:
    // travel times stay authored on the roads.
    [Serializable]
    public sealed class CityDistrictPlacer
    {
        [Tooltip(
            "Area centres (districts and scenery) try to keep this far apart, so areas stay distinct. A spot closer than this to another centre pays for it in score, so an area shifts a little rather than jumping across the map."
        )]
        [SerializeField, Min(0f), SuffixLabel("cells", true)]
        private float _areaSpacingCells = 24f;

        [Tooltip(
            "Score cost per cell an area centre crowds inside the spacing. Higher spreads areas apart harder; lower keeps each nearer its ideal spot."
        )]
        [SerializeField, Min(0f)]
        private float _crowdingCost = 2f;

        [Tooltip(
            "Area centres stay within this share of the way from downtown to the farthest land, so districts stay reachable and the city concentrates around its centre. Edge then means the edge of this reach."
        )]
        [SerializeField, Range(0.1f, 1f)]
        private float _reachShare = 0.6f;

        [Tooltip(
            "Random spread added to each spot's score, so a rule picks among the few best spots rather than always the very best."
        )]
        [SerializeField, Min(0f), SuffixLabel("cells", true)]
        private float _jitterCells = 3f;

        // The cell each area is centred on, in the order of the rules given (districts first, then scenery areas, so
        // districts get the best spots). Candidates are open land (no sea, no road) within the reach of downtown; an
        // area that finds none is null. Each pick is the rule's score plus jitter plus a crowding cost for sitting
        // inside the spacing of a centre already placed, so crowding nudges an area rather than overriding its rule.
        public List<Vector2Int?> PlaceCentres(
            CityMapData map,
            Vector2Int downtown,
            IReadOnlyList<DistrictPlacement> rules,
            int seed
        )
        {
            var centres = new List<Vector2Int?>(rules.Count);
            var land = new List<Vector2Int>();
            float farthestLand = 0f;
            foreach (Vector2Int cell in new RectInt(Vector2Int.zero, map.Size).allPositionsWithin)
            {
                if (map.GetGround(cell) != CityMapData.Ground.Land || map.GetRoad(cell) != CityMapData.Road.None)
                {
                    continue;
                }

                land.Add(cell);
                farthestLand = Mathf.Max(farthestLand, Vector2.Distance(cell, downtown));
            }

            float reach = farthestLand * _reachShare;
            List<Vector2Int> candidates = land.FindAll(cell => Vector2.Distance(cell, downtown) <= reach);
            var rng = new Random(seed ^ 0x5D1C7);
            int[] seaDistance = WaterDistance(map, CityMapData.Ground.Sea);
            int[] riverDistance = WaterDistance(map, CityMapData.Ground.River);
            var taken = new List<Vector2Int>();
            foreach (DistrictPlacement rule in rules)
            {
                Vector2Int best = default;
                bool found = false;
                float bestScore = float.MaxValue;
                foreach (Vector2Int cell in candidates)
                {
                    float nearest = float.MaxValue;
                    foreach (Vector2Int other in taken)
                    {
                        nearest = Mathf.Min(nearest, Vector2.Distance(other, cell));
                    }

                    if (nearest < 0.5f)
                    {
                        continue;
                    }

                    float crowding = Mathf.Max(0f, _areaSpacingCells - nearest) * _crowdingCost;
                    float score =
                        Score(rule, map, cell, downtown, reach, seaDistance, riverDistance)
                        + crowding
                        + (float)rng.NextDouble() * _jitterCells;
                    if (score < bestScore)
                    {
                        best = cell;
                        found = true;
                        bestScore = score;
                    }
                }

                centres.Add(found ? best : (Vector2Int?)null);
                if (found)
                {
                    taken.Add(best);
                }
            }

            return centres;
        }

        // The building that stands for a district: the tallest within radius of its centre, nearest on a tie, else the
        // nearest anywhere. Skips buildings already taken by another district; -1 when none is left.
        public int PickBuilding(CityMapData map, Vector2Int centre, float radius, ICollection<int> taken)
        {
            IReadOnlyList<CityMapData.Building> buildings = map.Buildings;
            int tallest = -1;
            int nearest = -1;
            float tallestHeight = float.MinValue;
            float tallestDistance = float.MaxValue;
            float nearestDistance = float.MaxValue;
            for (int index = 0; index < buildings.Count; index++)
            {
                if (taken.Contains(index))
                {
                    continue;
                }

                float distance = Vector2.Distance(buildings[index].Lot.center, centre);
                if (distance < nearestDistance)
                {
                    nearest = index;
                    nearestDistance = distance;
                }

                float height = buildings[index].WallHeight;
                bool taller = height > tallestHeight || (Mathf.Approximately(height, tallestHeight) && distance < tallestDistance);
                if (distance <= radius && taller)
                {
                    tallest = index;
                    tallestHeight = height;
                    tallestDistance = distance;
                }
            }

            return tallest >= 0 ? tallest : nearest;
        }

        // Lower is better, in cells. `farthest` is the reach: Edge favours its edge, Avenue its halfway point.
        private static float Score(
            DistrictPlacement placement,
            CityMapData map,
            Vector2Int cell,
            Vector2Int downtown,
            float farthest,
            int[] seaDistance,
            int[] riverDistance
        )
        {
            float fromDowntown = Vector2.Distance(cell, downtown);
            switch (placement)
            {
                case DistrictPlacement.Downtown:
                    return fromDowntown;
                case DistrictPlacement.Edge:
                    return farthest - fromDowntown;
                case DistrictPlacement.Coast:
                    return Steps(seaDistance, map, cell);
                case DistrictPlacement.River:
                    return Steps(riverDistance, map, cell);
                case DistrictPlacement.Avenue:
                    // Beside an avenue, about halfway out; away from one is a poor last resort.
                    return Mathf.Abs(fromDowntown - (farthest * 0.5f)) + (BesideAvenue(map, cell) ? 0f : farthest);
                default:
                    return 0f;
            }
        }

        private static float Steps(int[] distance, CityMapData map, Vector2Int cell)
        {
            int steps = distance[(cell.x * map.Size.y) + cell.y];
            return steps == int.MaxValue ? float.MaxValue / 2f : steps;
        }

        // Steps from each cell to the nearest cell of that water (4 neighbours), or int.MaxValue on a map without it.
        private static int[] WaterDistance(CityMapData map, CityMapData.Ground water)
        {
            Vector2Int size = map.Size;
            var distance = new int[size.x * size.y];
            var frontier = new Queue<Vector2Int>();
            for (int row = 0; row < size.x; row++)
            {
                for (int column = 0; column < size.y; column++)
                {
                    var cell = new Vector2Int(row, column);
                    bool wet = map.GetGround(cell) == water;
                    distance[(row * size.y) + column] = wet ? 0 : int.MaxValue;
                    if (wet)
                    {
                        frontier.Enqueue(cell);
                    }
                }
            }

            Vector2Int[] steps = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            while (frontier.Count > 0)
            {
                Vector2Int cell = frontier.Dequeue();
                int next = distance[(cell.x * size.y) + cell.y] + 1;
                foreach (Vector2Int step in steps)
                {
                    Vector2Int neighbour = cell + step;
                    if (map.InBounds(neighbour) && distance[(neighbour.x * size.y) + neighbour.y] > next)
                    {
                        distance[(neighbour.x * size.y) + neighbour.y] = next;
                        frontier.Enqueue(neighbour);
                    }
                }
            }

            return distance;
        }

        // Whether any of the cell's eight neighbours is an avenue.
        private static bool BesideAvenue(CityMapData map, Vector2Int cell)
        {
            for (int row = -1; row <= 1; row++)
            {
                for (int column = -1; column <= 1; column++)
                {
                    if (map.GetRoad(cell + new Vector2Int(row, column)) == CityMapData.Road.Avenue)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
