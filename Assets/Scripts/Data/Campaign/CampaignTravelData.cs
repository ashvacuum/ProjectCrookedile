using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data.Campaign
{
    [CreateAssetMenu(menuName = "Crookedile/Campaign/Travel Network", fileName = "Campaign Travel")]
    public class CampaignTravelData : ScriptableObject
    {
        public const int MINUTES_PER_HOUR = 60;
        public const int MINUTES_PER_DAY = 1440;
        public const int DEFAULT_START_MINUTE = 480;

        [Tooltip(
            "District where each day begins. Returning to HQ overnight costs no daytime minutes."
        )]
        [Required]
        [InlineEditor]
        [SerializeField]
        private DistrictData _headquarters;

        [Tooltip(
            "Clock minute at the start of each day: 480 = 08:00. The Hours budget still sets day length."
        )]
        [Range(0, MINUTES_PER_DAY - 1)]
        [SerializeField]
        private int _dayStartMinute = DEFAULT_START_MINUTE;

        [Tooltip("Road connections between districts. Unconnected districts cannot be visited.")]
        [SerializeField]
        private List<Road> _roads = new List<Road>();

        public DistrictData Headquarters
        {
            get { return _headquarters; }
        }

        public int DayStartMinute
        {
            get { return Mathf.Clamp(_dayStartMinute, 0, MINUTES_PER_DAY - 1); }
        }

        [Serializable]
        public class Road
        {
            [Tooltip("Road origin district.")]
            [Required]
            [InlineEditor]
            [SerializeField]
            private DistrictData _from;

            [Tooltip("Road destination district.")]
            [Required]
            [InlineEditor]
            [SerializeField]
            private DistrictData _to;

            [Tooltip(
                "Allow travel in both directions using the same duration and traffic schedule."
            )]
            [SerializeField]
            private bool _bidirectional = true;

            [Tooltip("Travel minutes with clear traffic.")]
            [Range(1, MINUTES_PER_DAY)]
            [SerializeField]
            private int _baseMinutes = 15;

            [Tooltip("Daily traffic windows. Overlapping windows use the highest multiplier.")]
            [SerializeField]
            private List<TrafficWindow> _traffic = new List<TrafficWindow>();

            internal DistrictData DestinationFrom(DistrictData district)
            {
                if (district == _from)
                {
                    return _to;
                }

                return _bidirectional && district == _to ? _from : null;
            }

            internal int BaseMinutes
            {
                get { return Mathf.Clamp(_baseMinutes, 1, MINUTES_PER_DAY); }
            }

            internal int MinutesAt(int departureMinute)
            {
                float multiplier = 1f;
                foreach (var window in _traffic)
                {
                    if (window != null)
                    {
                        multiplier = Mathf.Max(multiplier, window.MultiplierAt(departureMinute));
                    }
                }

                return Mathf.CeilToInt(BaseMinutes * multiplier);
            }
        }

        [Serializable]
        public class TrafficWindow
        {
            [Tooltip("Inclusive start minute, e.g. 420 = 07:00.")]
            [Range(0, MINUTES_PER_DAY - 1)]
            [SerializeField]
            private int _startMinute = 420;

            [Tooltip(
                "Exclusive end minute, e.g. 540 = 09:00. Must be later than start; split overnight windows."
            )]
            [Range(1, MINUTES_PER_DAY)]
            [ValidateInput(
                "@_endMinute > _startMinute",
                "Traffic window must end after it starts."
            )]
            [SerializeField]
            private int _endMinute = 540;

            [Tooltip(
                "Travel duration multiplier during this window: 1 = clear, 2 = twice as long."
            )]
            [Range(1f, 5f)]
            [SerializeField]
            private float _multiplier = 2f;

            internal float MultiplierAt(int minute)
            {
                return minute >= _startMinute && minute < _endMinute
                    ? Mathf.Clamp(_multiplier, 1f, 5f)
                    : 1f;
            }
        }

        public static string FormatTime(int minute)
        {
            return $"{minute / MINUTES_PER_HOUR:00}:{minute % MINUTES_PER_HOUR:00}";
        }

        public bool TryGetTravel(
            DistrictData from,
            DistrictData to,
            int departureMinute,
            out int travelMinutes,
            out int clearMinutes
        )
        {
            travelMinutes = 0;
            clearMinutes = 0;
            if (from == null || to == null)
            {
                return false;
            }

            var distances = new Dictionary<DistrictData, int> { { from, 0 } };
            var clearDistances = new Dictionary<DistrictData, int> { { from, 0 } };
            var visited = new HashSet<DistrictData>();

            // ponytail: district-scale Dijkstra with a departure-time traffic snapshot; use indexed adjacency for large networks.
            while (true)
            {
                DistrictData nearest = null;
                int shortest = int.MaxValue;
                foreach (var pair in distances)
                {
                    if (!visited.Contains(pair.Key) && pair.Value < shortest)
                    {
                        nearest = pair.Key;
                        shortest = pair.Value;
                    }
                }

                if (nearest == null)
                {
                    return false;
                }

                if (nearest == to)
                {
                    travelMinutes = shortest;
                    clearMinutes = clearDistances[nearest];
                    return true;
                }

                visited.Add(nearest);
                foreach (var road in _roads)
                {
                    if (road == null)
                    {
                        continue;
                    }

                    var next = road.DestinationFrom(nearest);
                    if (next == null || visited.Contains(next))
                    {
                        continue;
                    }

                    int candidate = shortest + road.MinutesAt(departureMinute);
                    if (!distances.TryGetValue(next, out int previous) || candidate < previous)
                    {
                        distances[next] = candidate;
                        clearDistances[next] = clearDistances[nearest] + road.BaseMinutes;
                    }
                }
            }
        }
    }
}
