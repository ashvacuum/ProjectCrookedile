using System.IO;
using Crookedile.Art.Iso;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Crookedile.Editor
{
    // Scene-view painter for a CityMapView (city-generator.md §7). Select the view, pick this tool, choose a brush,
    // then click or drag on the map. Every stroke is one Undo step.
    [EditorTool("City Map Painter", typeof(CityMapView))]
    public sealed class CityMapPainterTool : EditorTool
    {
        private enum Brush
        {
            Street,
            Avenue,
            ClearRoad,
            Land,
            Sea,
            Building,
            RemoveBuilding,
        }

        private static readonly string[] BrushNames =
        {
            "Street",
            "Avenue",
            "Clear road",
            "Land",
            "Sea",
            "Building",
            "Remove building",
        };
        private static Brush _brush;
        private static float _wallHeight = 0.5f;
        private static int _seed;
        private Vector2Int? _lotStart;

        public override GUIContent toolbarIcon => new GUIContent("City", "City Map Painter");

        #region Lifecycle

        public override void OnActivated()
        {
            Undo.undoRedoPerformed += RebuildAfterUndo;
        }

        public override void OnWillBeDeactivated()
        {
            Undo.undoRedoPerformed -= RebuildAfterUndo;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (!(window is SceneView) || !(target is CityMapView view) || view.Map == null)
            {
                return;
            }

            if (view.NeedsFullRebuild())
            {
                view.Rebuild();
            }

            DrawToolbar(view);
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            Event current = Event.current;
            if (current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlId);
                return;
            }

            if (!TryGetHoveredCell(view, current.mousePosition, out Vector2Int cell))
            {
                return;
            }

            if (_brush == Brush.Building)
            {
                HandleLotBrush(view, cell, current, controlId);
            }
            else
            {
                DrawLot(
                    view,
                    new RectInt(cell, Vector2Int.one),
                    view.Map.InBounds(cell) ? Color.white : Color.red
                );
                HandlePaintBrush(view, cell, current, controlId);
            }

            if (current.type == EventType.MouseMove || current.type == EventType.MouseDrag)
            {
                window.Repaint();
            }
        }

        #endregion

        #region Private API

        private static void DrawToolbar(CityMapView view)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10f, 10f, 620f, 72f), EditorStyles.helpBox);
            _brush = (Brush)GUILayout.Toolbar((int)_brush, BrushNames);
            if (_brush == Brush.Building)
            {
                _wallHeight = EditorGUILayout.Slider("Wall height", _wallHeight, 0.25f, 4f);
            }

            // Randomize rolls a new seed; Regenerate replays the seed in the field. Tuning is on the City Map View.
            GUILayout.BeginHorizontal();
            _seed = EditorGUILayout.IntField("Seed", _seed);
            bool randomize = GUILayout.Button("Randomize", GUILayout.Width(90f));
            bool regenerate = GUILayout.Button("Regenerate", GUILayout.Width(90f));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            Handles.EndGUI();

            if (randomize || regenerate)
            {
                if (randomize)
                {
                    _seed = UnityEngine.Random.Range(0, int.MaxValue);
                }

                Undo.RecordObject(view.Map, "Randomize City Map");
                view.Randomizer.Fill(view.Map, _seed);
                EditorUtility.SetDirty(view.Map);
                view.Rebuild();
            }
        }

        private void HandlePaintBrush(
            CityMapView view,
            Vector2Int cell,
            Event current,
            int controlId
        )
        {
            if (IsPress(current, controlId))
            {
                GUIUtility.hotControl = controlId;
                Undo.IncrementCurrentGroup();
                Paint(view, cell);
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && GUIUtility.hotControl == controlId)
            {
                Paint(view, cell);
                current.Use();
            }
            else if (current.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
            {
                GUIUtility.hotControl = 0;
                current.Use();
            }
        }

        private void HandleLotBrush(CityMapView view, Vector2Int cell, Event current, int controlId)
        {
            RectInt lot = LotBetween(_lotStart ?? cell, cell);
            DrawLot(view, lot, view.Map.CanPlace(lot) ? Color.green : Color.red);
            if (IsPress(current, controlId))
            {
                GUIUtility.hotControl = controlId;
                _lotStart = cell;
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && GUIUtility.hotControl == controlId)
            {
                current.Use();
            }
            else if (current.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
            {
                GUIUtility.hotControl = 0;
                _lotStart = null;
                Undo.RecordObject(view.Map, "Place Building Lot");
                if (view.Map.AddBuilding(lot, _wallHeight))
                {
                    EditorUtility.SetDirty(view.Map);
                    view.RebuildBuildings();
                }

                current.Use();
            }
        }

        private static void Paint(CityMapView view, Vector2Int cell)
        {
            CityMapData map = view.Map;
            Undo.RecordObject(map, "Paint City Map");
            bool lotsChanged = false;
            bool changed;
            switch (_brush)
            {
                case Brush.Street:
                    changed = map.SetRoad(cell, CityMapData.Road.Street);
                    break;
                case Brush.Avenue:
                    changed = map.SetRoad(cell, CityMapData.Road.Avenue);
                    break;
                case Brush.ClearRoad:
                    changed = map.SetRoad(cell, CityMapData.Road.None);
                    break;
                case Brush.Land:
                    // Land means plain land: it also clears a street or avenue. `|` so both run.
                    changed =
                        map.SetGround(cell, CityMapData.Ground.Land)
                        | map.SetRoad(cell, CityMapData.Road.None);
                    break;
                case Brush.Sea:
                    changed = map.SetGround(cell, CityMapData.Ground.Sea);
                    break;
                case Brush.RemoveBuilding:
                    changed = lotsChanged = map.RemoveBuildingAt(cell);
                    break;
                default:
                    changed = false;
                    break;
            }

            if (!changed)
            {
                return;
            }

            EditorUtility.SetDirty(map);
            if (lotsChanged)
            {
                view.RebuildBuildings();
            }
            else
            {
                view.RefreshCells(new[] { cell });
            }
        }

        private static bool IsPress(Event current, int controlId)
        {
            return current.type == EventType.MouseDown
                && current.button == 0
                && !current.alt
                && HandleUtility.nearestControl == controlId;
        }

        private static RectInt LotBetween(Vector2Int from, Vector2Int to)
        {
            Vector2Int min = Vector2Int.Min(from, to);
            return new RectInt(min, Vector2Int.Max(from, to) - min + Vector2Int.one);
        }

        private static bool TryGetHoveredCell(
            CityMapView view,
            Vector2 mousePosition,
            out Vector2Int cell
        )
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            Plane mapPlane = new Plane(view.transform.forward, view.transform.position);
            if (!mapPlane.Raycast(ray, out float distance))
            {
                cell = default;
                return false;
            }

            cell = IsoGrid.LocalToCell(
                view.transform.InverseTransformPoint(ray.GetPoint(distance))
            );
            return true;
        }

        private static void DrawLot(CityMapView view, RectInt lot, Color color)
        {
            float rowMin = lot.xMin - 0.5f;
            float rowMax = lot.xMax - 0.5f;
            float columnMin = lot.yMin - 0.5f;
            float columnMax = lot.yMax - 0.5f;
            Transform mapTransform = view.transform;
            Vector3[] outline =
            {
                mapTransform.TransformPoint(IsoGrid.CellToLocal(rowMin, columnMin)),
                mapTransform.TransformPoint(IsoGrid.CellToLocal(rowMax, columnMin)),
                mapTransform.TransformPoint(IsoGrid.CellToLocal(rowMax, columnMax)),
                mapTransform.TransformPoint(IsoGrid.CellToLocal(rowMin, columnMax)),
                mapTransform.TransformPoint(IsoGrid.CellToLocal(rowMin, columnMin)),
            };
            Handles.color = color;
            Handles.DrawAAPolyLine(3f, outline);
        }

        private void RebuildAfterUndo()
        {
            if (target is CityMapView view)
            {
                view.Rebuild();
            }
        }

        #endregion
    }

    // Builds and opens a scene ready for painting: camera, CitySun and a CityMapView on a 64 x 64 test map.
    public static class CityMapEditorScene
    {
        private const string ROOT_PATH = "Assets/Art/AI/Iso/CityMap";
        private const string MAP_PATH = ROOT_PATH + "/CityMap.asset";
        private const string SCENE_PATH = ROOT_PATH + "/CityMapEditor.unity";
        private const string GROUND_MATERIAL_PATH = "Assets/Art/AI/Iso/Roads/RoadSurfaceLit.mat";
        private const string BUILDING_PREFAB_PATH =
            "Assets/Art/AI/Iso/HalfCubePrototype/HalfCubePreview.prefab";

        [MenuItem("Crookedile/Art/Open City Map Editor")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = File.Exists(SCENE_PATH)
                ? EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single)
                : BuildScene();
            CityMapView view = Object.FindFirstObjectByType<CityMapView>();
            if (view == null || !scene.IsValid())
            {
                throw new System.InvalidOperationException($"{SCENE_PATH} has no CityMapView.");
            }
            // Scenes built before the sun existed get one; the city is lit by nothing else.
            if (Object.FindFirstObjectByType<CitySun>() == null)
            {
                new GameObject("Sun").AddComponent<CitySun>();
                EditorSceneManager.MarkSceneDirty(scene);
            }

            Selection.activeGameObject = view.gameObject;
            // The selection only reaches the tool manager on the next editor update.
            EditorApplication.delayCall += () =>
            {
                if (Selection.activeGameObject == view.gameObject)
                {
                    ToolManager.SetActiveTool<CityMapPainterTool>();
                }
            };
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null && view.Map != null)
            {
                Vector2Int size = view.Map.Size;
                sceneView.in2DMode = true;
                sceneView.LookAt(
                    view.transform.TransformPoint(
                        IsoGrid.CellToLocal(size.x * 0.5f, size.y * 0.5f)
                    ),
                    Quaternion.identity,
                    12f
                );
            }
        }

        private static Scene BuildScene()
        {
            Material groundMaterial = AssetDatabase.LoadAssetAtPath<Material>(GROUND_MATERIAL_PATH);
            GameObject buildingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                BUILDING_PREFAB_PATH
            );
            if (groundMaterial == null || buildingPrefab == null)
            {
                throw new System.InvalidOperationException(
                    "Build the road and half-cube previews before opening the city map editor."
                );
            }

            CityMapData map = AssetDatabase.LoadAssetAtPath<CityMapData>(MAP_PATH);
            if (map == null)
            {
                map = ScriptableObject.CreateInstance<CityMapData>();
                AssetDatabase.CreateAsset(map, MAP_PATH);
            }

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );
            Vector3 center = IsoGrid.CellToLocal(map.Size.x * 0.5f, map.Size.y * 0.5f);

            Camera camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.13f, 0.15f, 0.18f);
            camera.transform.position = new Vector3(center.x, center.y, -10f);

            new GameObject("Sun").AddComponent<CitySun>();

            CityMapView view = new GameObject("City Map").AddComponent<CityMapView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("_map").objectReferenceValue = map;
            serializedView.FindProperty("_groundMaterial").objectReferenceValue = groundMaterial;
            serializedView.FindProperty("_buildingPrefab").objectReferenceValue = buildingPrefab;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            view.Rebuild();

            if (!EditorSceneManager.SaveScene(scene, SCENE_PATH))
            {
                throw new System.InvalidOperationException(
                    "The city map editor scene could not be saved."
                );
            }

            AssetDatabase.SaveAssets();
            return scene;
        }
    }
}
