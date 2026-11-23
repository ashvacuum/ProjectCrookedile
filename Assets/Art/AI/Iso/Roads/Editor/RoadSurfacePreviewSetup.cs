using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Crookedile.Art.Iso;

namespace Crookedile.Editor
{
    public static class RoadSurfacePreviewSetup
    {
        private const string ROOT_PATH = "Assets/Art/AI/Iso/Roads";
        private const string ALBEDO_PATH = ROOT_PATH + "/RoadSurfaceAtlas_Albedo.png";
        private const string NORMAL_PATH = ROOT_PATH + "/RoadSurfaceAtlas_Normal.png";
        private const string SPECULAR_PATH = ROOT_PATH + "/RoadSurfaceAtlas_Specular.png";
        private const string OCCLUSION_PATH = ROOT_PATH + "/RoadSurfaceAtlas_Occlusion.png";
        private const string MATERIAL_PATH = ROOT_PATH + "/RoadSurfaceLit.mat";
        private const string MESH_PATH = ROOT_PATH + "/RoadSurfaceTileMesh.asset";
        private const string SCENE_PATH = ROOT_PATH + "/RoadSurfacePreview.unity";
        private const string SHADER_NAME = "Crookedile/2D/Half Cube Lit";
        private const float TILE_HALF_WIDTH = 0.4f;
        private const float TILE_HALF_HEIGHT = 0.2f;
        private static readonly string[] TILE_NAMES =
        {
            "Road Straight", "Road Corner", "Road T Junction", "Road Cross",
            "Crosswalk", "Road End", "Driveway", "Manhole",
            "Pavement Plain", "Pavement Cracked", "Pavement Corner", "Pavement T Junction",
            "Curb Ramp", "Sidewalk Corner", "Grass Verge", "Storm Drain",
        };

        [MenuItem("Crookedile/Art/Build Road Surface Preview")]
        public static void BuildPreview()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Shader shader = Shader.Find(SHADER_NAME);
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(ALBEDO_PATH);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NORMAL_PATH);
            Texture2D specular = AssetDatabase.LoadAssetAtPath<Texture2D>(SPECULAR_PATH);
            Texture2D occlusion = AssetDatabase.LoadAssetAtPath<Texture2D>(OCCLUSION_PATH);
            if (shader == null || albedo == null || normal == null || specular == null || occlusion == null)
            {
                throw new System.InvalidOperationException("A road atlas texture or the half-cube shader failed to import.");
            }

            Material material = CreateRoadMaterial(shader, albedo, normal, specular, occlusion);
            Mesh mesh = CreateRoadTileMesh();
            Mesh savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MESH_PATH);
            if (savedMesh == null)
            {
                AssetDatabase.CreateAsset(mesh, MESH_PATH);
                savedMesh = mesh;
            }
            else
            {
                EditorUtility.CopySerialized(mesh, savedMesh);
                Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(savedMesh);
            }

            BuildPreviewScene(savedMesh, material);
            AssetDatabase.SaveAssets();
        }

        private static Material CreateRoadMaterial(Shader shader, Texture2D albedo, Texture2D normal, Texture2D specular, Texture2D occlusion)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
            if (material == null)
            {
                material = new Material(shader);
                material.name = "Road Surface Lit";
                AssetDatabase.CreateAsset(material, MATERIAL_PATH);
            }

            material.shader = shader;
            material.enableInstancing = true;
            material.SetTexture("_MainTex", albedo);
            material.SetTexture("_NormalMap", normal);
            material.SetTexture("_SpecularMap", specular);
            material.SetTexture("_OcclusionMap", occlusion);
            material.SetFloat("_OcclusionStrength", 0.65f);
            material.SetColor("_Color", Color.white);
            material.SetFloat("_AmbientStrength", 0.38f);
            material.SetFloat("_DiffuseStrength", 0.8f);
            material.SetFloat("_SpecularStrength", 0.035f);
            material.SetFloat("_SpecularPower", 32f);
            material.SetFloat("_NormalStrength", 1.25f);
            material.SetFloat("_SpecularEdgeOnly", 0f);
            material.SetFloat("_EdgeHighlightStrength", 0f);
            material.SetFloat("_EmissionStrength", 0f);
            material.SetFloat("_NormalFlipX", 1f);
            material.SetFloat("_NormalFlipY", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh CreateRoadTileMesh()
        {
            Vector3[] vertices =
            {
                new Vector3(-TILE_HALF_WIDTH, 0f, 0f),
                new Vector3(0f, TILE_HALF_HEIGHT, 0f),
                new Vector3(TILE_HALF_WIDTH, 0f, 0f),
                new Vector3(0f, -TILE_HALF_HEIGHT, 0f),
            };
            Vector3[] normals = { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            Vector4[] tangents =
            {
                new Vector4(1f, 0f, 0f, 1f),
                new Vector4(1f, 0f, 0f, 1f),
                new Vector4(1f, 0f, 0f, 1f),
                new Vector4(1f, 0f, 0f, 1f),
            };
            Vector2[] uvs =
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            };
            Vector2[] faceIds = { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
            Color[] colors = { Color.white, Color.white, Color.white, Color.white };
            int[] triangles = { 0, 1, 2, 0, 2, 3 };

            Mesh mesh = new Mesh();
            mesh.name = "Road Surface Tile Mesh";
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.tangents = tangents;
            mesh.uv = uvs;
            mesh.SetUVs(1, faceIds);
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void BuildPreviewScene(Mesh mesh, Material material)
        {
            Scene scene = SceneManager.GetSceneByPath(SCENE_PATH);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            }
            else
            {
                GameObject[] oldRoots = scene.GetRootGameObjects();
                for (int index = 0; index < oldRoots.Length; index++)
                {
                    Object.DestroyImmediate(oldRoots[index]);
                }
            }

            SceneManager.SetActiveScene(scene);
            Camera camera = CreateCamera();
            Light2D light = CreateKeyLight();
            GameObject tileRoot = new GameObject("Road Surface Tiles");

            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    int tileIndex = (row * 4) + column;
                    Vector3 position = new Vector3(
                        (column + row) * TILE_HALF_WIDTH,
                        (row - column) * TILE_HALF_HEIGHT,
                        0f
                    );
                    GameObject tile = CreateRoadTile(tileIndex, row, column, mesh, material, light, position);
                    tile.transform.SetParent(tileRoot.transform, true);
                }
            }

            camera.transform.position = new Vector3(1.2f, 0f, -10f);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, SCENE_PATH))
            {
                throw new System.InvalidOperationException("The road surface preview scene could not be saved.");
            }

            SceneManager.SetActiveScene(scene);
        }

        private static Camera CreateCamera()
        {
            Camera camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 1f;
            camera.backgroundColor = new Color(0.13f, 0.15f, 0.18f, 1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            return camera;
        }

        private static Light2D CreateKeyLight()
        {
            Vector3 lightPosition = new Vector3(0.4f, 1.1f, -1f);
            GameObject lightObject = new GameObject("Road Preview Key Light");
            lightObject.transform.position = lightPosition;
            Light2D light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.blendStyleIndex = 1;
            light.color = new Color(1f, 0.88f, 0.72f, 1f);
            light.intensity = 1.2f;
            light.pointLightOuterRadius = 4f;
            light.pointLightOuterAngle = 125f;
            light.pointLightInnerAngle = 85f;
            light.shadowsEnabled = true;
            light.shadowIntensity = 0.55f;
            light.transform.up = new Vector3(1.2f - lightPosition.x, -lightPosition.y, 0f).normalized;

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

        private static GameObject CreateRoadTile(int tileIndex, int row, int column, Mesh mesh, Material material, Light2D light, Vector3 position)
        {
            GameObject tile = new GameObject(TILE_NAMES[tileIndex]);
            tile.transform.position = position;
            MeshFilter meshFilter = tile.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            MeshRenderer meshRenderer = tile.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = true;
            meshRenderer.sortingOrder = -Mathf.RoundToInt(position.y * 100f);

            tile.AddComponent<CityRoadTile>().SetAtlasCell(row, column, 0);

            HalfCubeLightLink lightLink = tile.AddComponent<HalfCubeLightLink>();
            SerializedObject serializedLink = new SerializedObject(lightLink);
            serializedLink.FindProperty("_aimLightAtObject").boolValue = false;
            serializedLink.ApplyModifiedPropertiesWithoutUndo();
            lightLink.SetLight(light);
            return tile;
        }
    }
}
