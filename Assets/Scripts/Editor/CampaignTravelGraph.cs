using System.Collections.Generic;
using Crookedile.Data.Campaign;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    public class CampaignTravelGraph
    {
        private const float NODE_WIDTH = 160f;
        private const float NODE_HEIGHT = 46f;
        private readonly List<DistrictData> _districts = new List<DistrictData>();
        private readonly List<CampaignTravelData.Road> _route = new List<CampaignTravelData.Road>();
        private Vector2 _scroll;
        private DistrictData _selected;
        private DistrictData _dragging;
        private Vector2 _dragOffset;
        private int _dragUndoGroup;
        private DistrictData _connectTo;
        private EncounterPoolData _pool;

        public DistrictData Selected
        {
            get { return _selected; }
        }

        public Object Draw(
            EncounterPoolData pool,
            DistrictData from,
            int minute,
            EncounterData encounter
        )
        {
            if (_pool != pool)
            {
                _pool = pool;
                _selected = null;
                _connectTo = null;
                _scroll = Vector2.zero;
            }

            var network = pool.Travel;
            _districts.Clear();
            AddDistrict(network != null ? network.Headquarters : null);
            AddDistrict(from);
            if (network != null)
            {
                foreach (var road in network.Roads)
                {
                    if (road != null)
                    {
                        AddDistrict(road.From);
                        AddDistrict(road.To);
                    }
                }
            }

            foreach (var entry in pool.Entries)
            {
                AddDistrict(entry?.Encounter != null ? entry.Encounter.District : null);
            }

            if (!_districts.Contains(_selected))
            {
                _selected = null;
            }

            _route.Clear();
            var destination = encounter != null ? encounter.District : _selected;
            if (network != null && destination != null)
            {
                network.TryGetTravel(from, destination, minute, out _, out _, _route);
            }

            EditorGUILayout.LabelField("District connections", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Drag districts to arrange • Arrows show permitted travel • Blue = selected quickest route",
                EditorStyles.miniLabel
            );
            EditorGUILayout.LabelField(
                "Road labels: minutes / traffic multiplier • Orange = traffic delay • Road values precede ally discounts",
                EditorStyles.miniLabel
            );
            if (destination != null)
            {
                EditorGUILayout.LabelField(
                    "Route to "
                        + destination.DisplayName
                        + (
                            _route.Count == 0
                                ? destination == from
                                    ? " — already here"
                                    : " — unreachable"
                                : ""
                        ),
                    EditorStyles.miniLabel
                );
            }
            Object edit = null;
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(network == null))
                {
                    if (
                        GUILayout.Button(
                            new GUIContent(
                                "New district",
                                "Creates HQ if missing; otherwise adds a 15-minute two-way road from the selected district or HQ."
                            )
                        )
                    )
                    {
                        var district =
                            Crookedile.Utilities.AuthoringAssets.CreateBeside<DistrictData>(
                                pool,
                                "New District"
                            );
                        Undo.RegisterCreatedObjectUndo(district, "Create district");
                        if (network.Headquarters == null)
                        {
                            var serialized = new SerializedObject(network);
                            serialized.FindProperty("_headquarters").objectReferenceValue =
                                district;
                            serialized.ApplyModifiedProperties();
                        }
                        else
                        {
                            Connect(
                                network,
                                _selected != null ? _selected : network.Headquarters,
                                district
                            );
                        }

                        AddDistrict(district);
                        _selected = district;
                    }
                }

                using (new EditorGUI.DisabledScope(_selected == null))
                {
                    if (GUILayout.Button("Edit selected district"))
                    {
                        edit = _selected;
                    }
                }

                if (GUILayout.Button("Show all encounters"))
                {
                    _selected = null;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    _selected != null
                        ? "Connect from " + _selected.DisplayName
                        : "Select a district to connect",
                    GUILayout.Width(210f)
                );
                _connectTo = (DistrictData)
                    EditorGUILayout.ObjectField(_connectTo, typeof(DistrictData), false);
                using (
                    new EditorGUI.DisabledScope(
                        network == null
                            || _selected == null
                            || _connectTo == null
                            || _connectTo == _selected
                    )
                )
                {
                    if (GUILayout.Button("Add road", GUILayout.Width(80f)))
                    {
                        Connect(network, _selected, _connectTo);
                        AddDistrict(_connectTo);
                        edit = network;
                    }
                }
            }

            var viewport = GUILayoutUtility.GetRect(100f, 330f, GUILayout.ExpandWidth(true));
            float width = Mathf.Max(1000f, viewport.width - 20f);
            float height = Mathf.Max(500f, ((_districts.Count + 3) / 4) * 130f + 60f);
            for (int i = 0; i < _districts.Count; i++)
            {
                var rect = NodeRect(i);
                width = Mathf.Max(width, rect.xMax + 40f);
                height = Mathf.Max(height, rect.yMax + 40f);
            }

            _scroll = GUI.BeginScrollView(viewport, _scroll, new Rect(0f, 0f, width, height));
            int dragControl = GUIUtility.GetControlID(FocusType.Passive);
            if (network != null)
            {
                Handles.BeginGUI();
                var previousColor = Handles.color;
                foreach (var road in network.Roads)
                {
                    if (road == null || road.From == null || road.To == null)
                    {
                        continue;
                    }

                    var start = NodeRect(_districts.IndexOf(road.From)).center;
                    var end = NodeRect(_districts.IndexOf(road.To)).center;
                    var delta = end - start;
                    if (delta.sqrMagnitude < 1f)
                    {
                        continue;
                    }

                    int duration = road.MinutesAt(minute);
                    Handles.color =
                        _route.Contains(road) ? new Color(0.2f, 0.65f, 1f)
                        : duration > road.BaseMinutes ? new Color(0.9f, 0.5f, 0.15f)
                        : Color.gray;
                    Handles.DrawAAPolyLine(_route.Contains(road) ? 4f : 2f, start, end);
                    DrawArrow(Vector2.Lerp(start, end, 0.65f), delta.normalized);
                    if (road.Bidirectional)
                    {
                        DrawArrow(Vector2.Lerp(start, end, 0.35f), -delta.normalized);
                    }

                    var label = new Rect(
                        (start.x + end.x) / 2f - 70f,
                        (start.y + end.y) / 2f - 10f,
                        140f,
                        22f
                    );
                    if (
                        GUI.Button(
                            label,
                            new GUIContent(
                                $"{duration}m / {(float)duration / road.BaseMinutes:0.##}x",
                                "Edit road durations and traffic windows in the network"
                            ),
                            EditorStyles.miniButton
                        )
                    )
                    {
                        edit = network;
                    }
                }

                Handles.color = previousColor;
                Handles.EndGUI();
            }

            for (int i = 0; i < _districts.Count; i++)
            {
                var district = _districts[i];
                var rect = NodeRect(i);
                int count = 0;
                foreach (var entry in pool.Entries)
                {
                    if (entry?.Encounter != null && entry.Encounter.District == district)
                    {
                        count++;
                    }
                }

                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = district == _selected ? new Color(0.4f, 0.7f, 1f) : oldColor;
                GUI.Box(
                    rect,
                    district.DisplayName
                        + (network != null && district == network.Headquarters ? " [HQ]" : "")
                        + (district == from ? " [From]" : "")
                        + "\n"
                        + count
                        + " pool entries",
                    EditorStyles.helpBox
                );
                GUI.backgroundColor = oldColor;
                var current = Event.current;
                if (
                    current.type == EventType.MouseDown
                    && current.button == 0
                    && rect.Contains(current.mousePosition)
                )
                {
                    _selected = district;
                    _dragging = district;
                    _dragOffset = current.mousePosition - rect.position;
                    Undo.IncrementCurrentGroup();
                    _dragUndoGroup = Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("Move district");
                    GUIUtility.hotControl = dragControl;
                    current.Use();
                }

                if (_dragging == district && current.type == EventType.MouseDrag)
                {
                    var serialized = new SerializedObject(district);
                    var position = current.mousePosition - _dragOffset;
                    position.x = Mathf.Max(0f, position.x);
                    position.y = Mathf.Max(0f, position.y);
                    serialized.FindProperty("_mapPosition").vector2Value = position;
                    serialized.ApplyModifiedProperties();
                    GUI.changed = true;
                    current.Use();
                }
            }

            if (_dragging != null && Event.current.rawType == EventType.MouseUp)
            {
                _dragging = null;
                GUIUtility.hotControl = 0;
                Undo.CollapseUndoOperations(_dragUndoGroup);
            }

            GUI.EndScrollView();
            return edit;
        }

        private void AddDistrict(DistrictData district)
        {
            if (district != null && !_districts.Contains(district))
            {
                _districts.Add(district);
            }
        }

        private Rect NodeRect(int index)
        {
            var position = _districts[index].MapPosition;
            if (position.x < 0f || position.y < 0f)
            {
                position = new Vector2(40f + index % 4 * 240f, 40f + index / 4 * 130f);
            }

            return new Rect(position.x, position.y, NODE_WIDTH, NODE_HEIGHT);
        }

        private static void DrawArrow(Vector2 point, Vector2 direction)
        {
            var perpendicular = new Vector2(-direction.y, direction.x) * 5f;
            Handles.DrawAAPolyLine(
                2f,
                point - direction * 10f + perpendicular,
                point,
                point - direction * 10f - perpendicular
            );
        }

        private static void Connect(CampaignTravelData network, DistrictData from, DistrictData to)
        {
            var serialized = new SerializedObject(network);
            var roads = serialized.FindProperty("_roads");
            int index = roads.arraySize;
            roads.InsertArrayElementAtIndex(index);
            var road = roads.GetArrayElementAtIndex(index);
            road.FindPropertyRelative("_from").objectReferenceValue = from;
            road.FindPropertyRelative("_to").objectReferenceValue = to;
            road.FindPropertyRelative("_bidirectional").boolValue = true;
            road.FindPropertyRelative("_baseMinutes").intValue = 15;
            road.FindPropertyRelative("_traffic").ClearArray();
            serialized.ApplyModifiedProperties();
        }

        public static void DrawTimeline(
            EncounterData encounter,
            CampaignVisitPlan plan,
            int departure,
            int budget
        )
        {
            var rect = GUILayoutUtility.GetRect(100f, 60f, GUILayout.ExpandWidth(true));
            for (int hour = 0; hour <= 24; hour += 4)
            {
                float labelX = Mathf.Clamp(
                    rect.x + rect.width * hour / 24f - 20f,
                    rect.x,
                    rect.xMax - 40f
                );
                GUI.Label(
                    new Rect(labelX, rect.y, 40f, 18f),
                    $"{hour:00}:00",
                    EditorStyles.centeredGreyMiniLabel
                );
            }
            var track = new Rect(rect.x, rect.y + 20f, rect.width, 14f);
            EditorGUI.DrawRect(track, new Color(0.5f, 0.5f, 0.5f, 0.15f));
            DrawSpan(
                track,
                encounter.OpeningMinute,
                encounter.ClosingMinute,
                new Color(0.3f, 0.7f, 0.4f, 0.6f)
            );
            track.y += 17f;
            DrawSpan(
                track,
                departure,
                Mathf.Min(1440, departure + budget),
                new Color(0.5f, 0.5f, 0.5f, 0.2f)
            );
            DrawSpan(track, departure, plan.ArrivalMinute, new Color(0.2f, 0.6f, 0.9f));
            DrawSpan(track, plan.ArrivalMinute, plan.StartMinute, new Color(0.9f, 0.65f, 0.2f));
            DrawSpan(track, plan.StartMinute, plan.FinishMinute, new Color(0.65f, 0.4f, 0.85f));
            float x = rect.x + rect.width * departure / 1440f;
            EditorGUI.DrawRect(
                new Rect(x, rect.y + 18f, 1f, 35f),
                EditorGUIUtility.isProSkin ? Color.white : Color.black
            );
        }

        private static void DrawSpan(Rect track, int start, int end, Color color)
        {
            start = Mathf.Clamp(start, 0, 1440);
            end = Mathf.Clamp(end, 0, 1440);
            if (end <= start)
            {
                return;
            }

            EditorGUI.DrawRect(
                new Rect(
                    track.x + track.width * start / 1440f,
                    track.y,
                    track.width * (end - start) / 1440f,
                    track.height
                ),
                color
            );
        }
    }
}
