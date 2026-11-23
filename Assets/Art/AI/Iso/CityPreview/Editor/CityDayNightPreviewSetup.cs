using System.Collections.Generic;
using Crookedile.Art.Iso;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Crookedile.Editor
{
    public static class CityDayNightPreviewSetup
    {
        private const string ROOT_PATH = "Assets/Art/AI/Iso/CityPreview";
        private const string ROAD_MATERIAL_PATH = "Assets/Art/AI/Iso/Roads/RoadSurfaceLit.mat";
        private const string ROAD_MESH_PATH = "Assets/Art/AI/Iso/Roads/RoadSurfaceTileMesh.asset";
        private const string HOUSE_PREFAB_PATH = "Assets/Art/AI/Iso/HalfCubePrototype/HalfCubePreview.prefab";
        private const string SCENE_PATH = ROOT_PATH + "/CityDayNightPreview.unity";
        private const int GRID_SIZE = 5;
        private const int GRID_CENTER = GRID_SIZE / 2;
        private const float TILE_HALF_WIDTH = IsoGrid.HALF_WIDTH;
        private const float TILE_HALF_HEIGHT = IsoGrid.HALF_HEIGHT;
        private static readonly string[] HOUSE_NAMES = { "House North West", "House North East", "House South West", "House South East" };

        #region Public API

        [MenuItem("Crookedile/Art/Build Day Night City Preview")]
        public static void BuildPreview()
        {
            Material roadMaterial = AssetDatabase.LoadAssetAtPath<Material>(ROAD_MATERIAL_PATH);
            Mesh roadMesh = AssetDatabase.LoadAssetAtPath<Mesh>(ROAD_MESH_PATH);
            GameObject housePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HOUSE_PREFAB_PATH);
            if (roadMaterial == null || roadMesh == null || housePrefab == null)
            {
                throw new System.InvalidOperationException("Build the road and half-cube previews before building the city preview.");
            }

            Scene scene = OpenPreviewScene();
            SceneManager.SetActiveScene(scene);

            Vector3 cityCenter = GridPosition(GRID_CENTER, GRID_CENTER);
            Camera previewCamera = CreateCamera(cityCenter);
            Light2D ambientLight = CreateAmbientLight();
            Light2D keyLight = CreateKeyLight(cityCenter);
            GameObject roadRoot = new GameObject("City Road Tiles");
            GameObject houseRoot = new GameObject("City Houses");
            List<Renderer> litRenderers = new List<Renderer>(GRID_SIZE * GRID_SIZE + HOUSE_NAMES.Length);

            CreateRoadGrid(roadMesh, roadMaterial, keyLight, roadRoot.transform, litRenderers);
            CreateHouses(housePrefab, keyLight, houseRoot.transform, litRenderers);

            CityDayNightCycle cycle = new GameObject("Day Night Cycle").AddComponent<CityDayNightCycle>();
            cycle.Configure(keyLight, ambientLight, previewCamera, litRenderers.ToArray(), cityCenter);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, SCENE_PATH))
            {
                throw new System.InvalidOperationException("The day-night city preview scene could not be saved.");
            }

            AssetDatabase.SaveAssets();
            SceneManager.SetActiveScene(scene);
        }

        #endregion

        #region Private API

        private static Scene OpenPreviewScene()
        {
            Scene scene = SceneManager.GetSceneByPath(SCENE_PATH);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Scene activeScene = SceneManager.GetActiveScene();
                scene = IsUnfinishedCityPreview(activeScene)
                    ? activeScene
                    : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            }

            GameObject[] oldRoots = scene.GetRootGameObjects();
            for (int index = 0; index < oldRoots.Length; index++)
            {
                Object.DestroyImmediate(oldRoots[index]);
            }

            return scene;
        }

        private static bool IsUnfinishedCityPreview(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene.path.Length > 0 || scene.name != "Untitled")
            {
                return false;
            }

            bool hasRoadRoot = false;
            bool hasHouseRoot = false;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                hasRoadRoot |= roots[index].name == "City Road Tiles";
                hasHouseRoot |= roots[index].name == "City Houses";
            }

            return hasRoadRoot && hasHouseRoot;
        }

        private static Camera CreateCamera(Vector3 cityCenter)
        {
            Camera camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 2f;
            camera.backgroundColor = new Color(0.53f, 0.72f, 0.86f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(cityCenter.x, cityCenter.y, -10f);
            return camera;
        }

        private static Light2D CreateAmbientLight()
        {
            GameObject lightObject = new GameObject("Day Night Ambient");
            Light2D light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.blendStyleIndex = 0;
            light.color = Color.white;
            light.intensity = 1f;
            return light;
        }

        private static Light2D CreateKeyLight(Vector3 cityCenter)
        {
            GameObject lightObject = new GameObject("Moving Day Night Key Light");
            lightObject.transform.position = new Vector3(cityCenter.x + 2.75f, cityCenter.y, -1f);
            Light2D light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.blendStyleIndex = 1;
            light.color = new Color(1f, 0.8f, 0.58f);
            light.intensity = 0.9f;
            light.pointLightOuterRadius = 6f;
            light.pointLightOuterAngle = 360f;
            light.pointLightInnerAngle = 360f;
            light.shadowsEnabled = true;
            light.shadowIntensity = 0.7f;

            SerializedObject serializedLight = new SerializedObject(light);
            SerializedProperty normalDistance = serializedLight.FindProperty("m_NormalMapDistance");
            if (normalDistance != null)
            {
                normalDistance.floatValue = 1.5f;
            }

            SerializedProperty normalQuality = serializedLight.FindProperty("m_NormalMapQuality");
            if (normalQuality != null)
            {
                normalQuality.intValue = (int)Light2D.NormalMapQuality.Accurate;
            }

            serializedLight.ApplyModifiedProperties();
            return light;
        }

        private static void CreateRoadGrid(Mesh mesh, Material material, Light2D keyLight, Transform parent, List<Renderer> litRenderers)
        {
            for (int row = 0; row < GRID_SIZE; row++)
            {
                for (int column = 0; column < GRID_SIZE; column++)
                {
                    int tileIndex = GetTileIndex(row, column);
                    Vector3 position = GridPosition(row, column);
                    GameObject tile = new GameObject(GetTileName(tileIndex));
                    tile.transform.position = position;
                    tile.transform.SetParent(parent, true);
                    MeshFilter meshFilter = tile.AddComponent<MeshFilter>();
                    meshFilter.sharedMesh = mesh;
                    MeshRenderer meshRenderer = tile.AddComponent<MeshRenderer>();
                    meshRenderer.sharedMaterial = material;
                    meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                    meshRenderer.sortingOrder = -Mathf.RoundToInt(position.y * 100f);
                    CityRoadTile roadTile = tile.AddComponent<CityRoadTile>();
                    int quarterTurns = column == GRID_CENTER && row != GRID_CENTER ? 1 : 0;
                    roadTile.SetAtlasCell(tileIndex / 4, tileIndex % 4, quarterTurns);

                    HalfCubeLightLink lightLink = tile.AddComponent<HalfCubeLightLink>();
                    SetLightLink(lightLink, keyLight);
                    litRenderers.Add(meshRenderer);
                }
            }
        }

        private static void CreateHouses(GameObject housePrefab, Light2D keyLight, Transform parent, List<Renderer> litRenderers)
        {
            int houseIndex = 0;
            int[] houseRows = { 0, 0, GRID_SIZE - 1, GRID_SIZE - 1 };
            int[] houseColumns = { 0, GRID_SIZE - 1, 0, GRID_SIZE - 1 };
            for (int index = 0; index < houseRows.Length; index++)
            {
                GameObject house = PrefabUtility.InstantiatePrefab(housePrefab) as GameObject;
                if (house == null)
                {
                    throw new System.InvalidOperationException("A city house could not be instantiated from the half-cube prefab.");
                }

                house.name = HOUSE_NAMES[houseIndex++];
                Vector3 position = GridPosition(houseRows[index], houseColumns[index]);
                house.transform.position = new Vector3(position.x, position.y, -0.05f);
                house.transform.localScale = Vector3.one * 0.9f;
                house.transform.SetParent(parent, true);

                MeshRenderer meshRenderer = house.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.sortingOrder = -Mathf.RoundToInt(position.y * 100f) + 5;
                }

                HalfCubeLightLink lightLink = house.GetComponent<HalfCubeLightLink>();
                if (lightLink != null)
                {
                    SetLightLink(lightLink, keyLight);
                }

                Renderer[] houseRenderers = house.GetComponentsInChildren<Renderer>();
                for (int rendererIndex = 0; rendererIndex < houseRenderers.Length; rendererIndex++)
                {
                    litRenderers.Add(houseRenderers[rendererIndex]);
                }
            }
        }

        private static void SetLightLink(HalfCubeLightLink lightLink, Light2D keyLight)
        {
            SerializedObject serializedLink = new SerializedObject(lightLink);
            SerializedProperty aimAtObject = serializedLink.FindProperty("_aimLightAtObject");
            if (aimAtObject != null)
            {
                aimAtObject.boolValue = false;
                serializedLink.ApplyModifiedPropertiesWithoutUndo();
            }

            lightLink.SetLight(keyLight);
        }

        private static int GetTileIndex(int row, int column)
        {
            if (row == GRID_CENTER && column == GRID_CENTER)
            {
                return 3;
            }

            if (row == GRID_CENTER && column == 1)
            {
                return 4;
            }

            if (row == GRID_CENTER && column == 3)
            {
                return 15;
            }

            if (row == GRID_CENTER || column == GRID_CENTER)
            {
                return 0;
            }

            return (row + column) % 3 == 0 ? 9 : 8;
        }

        private static string GetTileName(int tileIndex)
        {
            if (tileIndex == 3)
            {
                return "Road Cross";
            }

            if (tileIndex == 4)
            {
                return "Crosswalk";
            }

            if (tileIndex == 15)
            {
                return "Storm Drain";
            }

            return tileIndex == 0 ? "Road Straight" : tileIndex == 9 ? "Pavement Cracked" : "Pavement Plain";
        }

        private static Vector3 GridPosition(int row, int column)
        {
            return new Vector3(
                (column + row) * TILE_HALF_WIDTH,
                (row - column) * TILE_HALF_HEIGHT,
                0f
            );
        }

        #endregion
    }
}
