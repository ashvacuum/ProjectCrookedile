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
        [Tooltip("Area centres (districts and scenery) keep at least this far apart when the city has room, so areas stay distinct.")]
        [SerializeField, Min(0f), SuffixLabel("cells", true)]
        private float _areaSpacingCells = 24f;

        [Tooltip(
            "Random spread added to each spot's score, so a rule picks among the few best spots rather than always the very best."
        )]
        [SerializeField, Min(0f), SuffixLabel("cells", true)]
        private float _jitterCells = 3f;

        // The cell each area is centred on, in the order of the rules given (districts first, then scenery areas, so
        // districts get the best spots). Candidates are open land (no sea, no road); an area that finds none is null.
        public List<Vector2Int?> PlaceCentres(
            CityMapData map,
            Vector2Int downtown,
            IReadOnlyList<DistrictPlacement> rules,
            int seed
        )
        {
            var centres = new List<Vector2Int?>(rules.Count);
            var candidates = new List<Vector2Int>();
            float farthest = 0f;
            foreach (Vector2Int cell in new RectInt(Vector2Int.zero, map.Size).allPositionsWithin)
            {
                if (map.GetGround(cell) == CityMapData.Ground.Sea || map.GetRoad(cell) != CityMapData.Road.None)
                {
                    continue;
                }

                candidates.Add(cell);
                farthest = Mathf.Max(farthest, Vector2.Distance(cell, downtown));
            }

            var rng = new Random(seed ^ 0x5D1C7);
            int[] seaDistance = SeaDistance(map);
            var taken = new List<Vector2Int>();
            foreach (DistrictPlacement rule in rules)
            {
                Vector2Int best = default;
                bool found = false;
                bool bestSpaced = false;
                float bestScore = float.MaxValue;
                foreach (Vector2Int cell in candidates)
                {
                    if (taken.Contains(cell))
                    {
                        continue;
                    }

                    bool spaced = taken.TrueForAll(other => Vector2.Distance(other, cell) >= _areaSpacingCells);
                    float score =
                        Score(rule, map, cell, downtown, farthest, seaDistance)
                        + (float)rng.NextDouble() * _jitterCells;
                    // A well-spaced spot always beats a crowded one; crowding is only the fallback.
                    if (!found || (spaced && !bestSpaced) || (spaced == bestSpaced && score < bestScore))
                    {
                        best = cell;
                        found = true;
                        bestSpaced = spaced;
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

        // Lower is better, in cells.
        private static float Score(
            DistrictPlacement placement,
            CityMapData map,
            Vector2Int cell,
            Vector2Int downtown,
            float farthest,
            int[] seaDistance
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
                    int steps = seaDistance[(cell.x * map.Size.y) + cell.y];
                    return steps == int.MaxValue ? float.MaxValue / 2f : steps;
                case DistrictPlacement.Avenue:
                    // Beside an avenue, about halfway out; away from one is a poor last resort.
                    return Mathf.Abs(fromDowntown - (farthest * 0.5f)) + (BesideAvenue(map, cell) ? 0f : farthest);
                default:
                    return 0f;
            }
        }

        // Steps from each cell to the nearest sea cell (4 neighbours), or int.MaxValue on a map with no sea.
        private static int[] SeaDistance(CityMapData map)
        {
            Vector2Int size = map.Size;
            var distance = new int[size.x * size.y];
            var frontier = new Queue<Vector2Int>();
            for (int row = 0; row < size.x; row++)
            {
                for (int column = 0; column < size.y; column++)
                {
                    var cell = new Vector2Int(row, column);
                    bool sea = map.GetGround(cell) == CityMapData.Ground.Sea;
                    distance[(row * size.y) + column] = sea ? 0 : int.MaxValue;
                    if (sea)
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
