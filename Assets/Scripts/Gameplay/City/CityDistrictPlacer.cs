using System;
using System.Collections.Generic;
using Crookedile.Data.Campaign;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = System.Random;

namespace Crookedile.Gameplay.City
{
    // Picks one building per district on a generated city by its placement rule (city-generator.md §4.8), so the
    // district's marker has somewhere to stand. Seeded, so a run always places its districts the same way. Placement
    // is drawing only: travel times stay authored on the roads.
    [Serializable]
    public sealed class CityDistrictPlacer
    {
        [Tooltip("Districts keep at least this far apart, centre to centre, when the city has room.")]
        [SerializeField, Min(0f), SuffixLabel("cells", true)]
        private float _minSpacingCells = 8f;

        [Tooltip(
            "Random spread added to each building's score, so a rule picks among the few best spots rather than always the very best."
        )]
        [SerializeField, Min(0f), SuffixLabel("cells", true)]
        private float _jitterCells = 3f;

        // District to the index of its building in map.Buildings. A district with no building left is absent.
        public Dictionary<DistrictData, int> Place(
            CityMapData map,
            Vector2Int downtown,
            IReadOnlyList<DistrictData> districts,
            int seed
        )
        {
            var placed = new Dictionary<DistrictData, int>();
            IReadOnlyList<CityMapData.Building> buildings = map.Buildings;
            if (buildings.Count == 0)
            {
                return placed;
            }

            var rng = new Random(seed ^ 0x5D1C7);
            int[] seaDistance = SeaDistance(map);
            float farthest = 0f;
            foreach (CityMapData.Building building in buildings)
            {
                farthest = Mathf.Max(farthest, Vector2.Distance(building.Lot.center, downtown));
            }

            var taken = new List<Vector2>();
            foreach (DistrictData district in districts)
            {
                int best = -1;
                bool bestSpaced = false;
                float bestScore = float.MaxValue;
                for (int index = 0; index < buildings.Count; index++)
                {
                    if (placed.ContainsValue(index))
                    {
                        continue;
                    }

                    Vector2 center = buildings[index].Lot.center;
                    bool spaced = taken.TrueForAll(other => Vector2.Distance(other, center) >= _minSpacingCells);
                    float score =
                        Score(district.Placement, map, buildings[index], downtown, farthest, seaDistance)
                        + (float)rng.NextDouble() * _jitterCells;
                    // A well-spaced spot always beats a crowded one; crowding is only the fallback.
                    if ((spaced && !bestSpaced) || (spaced == bestSpaced && score < bestScore))
                    {
                        best = index;
                        bestSpaced = spaced;
                        bestScore = score;
                    }
                }

                if (best < 0)
                {
                    break;
                }

                placed[district] = best;
                taken.Add(buildings[best].Lot.center);
            }

            return placed;
        }

        // Lower is better, in cells.
        private static float Score(
            DistrictPlacement placement,
            CityMapData map,
            CityMapData.Building building,
            Vector2Int downtown,
            float farthest,
            int[] seaDistance
        )
        {
            float fromDowntown = Vector2.Distance(building.Lot.center, downtown);
            switch (placement)
            {
                case DistrictPlacement.Downtown:
                    return fromDowntown;
                case DistrictPlacement.Edge:
                    return farthest - fromDowntown;
                case DistrictPlacement.Coast:
                    return NearestSea(map, building.Lot, seaDistance);
                case DistrictPlacement.Avenue:
                    // On an avenue, about halfway out; off an avenue is a poor last resort.
                    return Mathf.Abs(fromDowntown - (farthest * 0.5f)) + (TouchesAvenue(map, building.Lot) ? 0f : farthest);
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

        private static float NearestSea(CityMapData map, RectInt lot, int[] seaDistance)
        {
            int nearest = int.MaxValue;
            foreach (Vector2Int cell in lot.allPositionsWithin)
            {
                nearest = Mathf.Min(nearest, seaDistance[(cell.x * map.Size.y) + cell.y]);
            }

            return nearest == int.MaxValue ? float.MaxValue / 2f : nearest;
        }

        private static bool TouchesAvenue(CityMapData map, RectInt lot)
        {
            for (int row = lot.xMin - 1; row <= lot.xMax; row++)
            {
                for (int column = lot.yMin - 1; column <= lot.yMax; column++)
                {
                    var cell = new Vector2Int(row, column);
                    if (!lot.Contains(cell) && map.GetRoad(cell) == CityMapData.Road.Avenue)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
