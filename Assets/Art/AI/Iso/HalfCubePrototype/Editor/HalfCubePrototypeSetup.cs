using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Crookedile.Art.Iso;

namespace Crookedile.Editor
{
    public static class HalfCubePrototypeSetup
    {
        private const string ROOT_PATH = "Assets/Art/AI/Iso/HalfCubePrototype";
        private const string ALBEDO_PATH = ROOT_PATH + "/CampaignBlock_FaceAtlas_Albedo.png";
        private const string NORMAL_PATH = ROOT_PATH + "/CampaignBlock_FaceAtlas_Normal.png";
        private const string SPECULAR_PATH = ROOT_PATH + "/CampaignBlock_FaceAtlas_Specular.png";
        private const string OCCLUSION_PATH = ROOT_PATH + "/CampaignBlock_FaceAtlas_Occlusion.png";
        private const string EMISSION_PATH = ROOT_PATH + "/CampaignBlock_FaceAtlas_Emission.png";
        private const string MATERIAL_PATH = ROOT_PATH + "/HalfCubeSpriteLit.mat";
        private const string PREFAB_PATH = ROOT_PATH + "/HalfCubePreview.prefab";
        private const string SCENE_PATH = ROOT_PATH + "/HalfCubePreview.unity";
        private const string SHADER_NAME = "Crookedile/2D/Half Cube Lit";
        // Covers the 1254px source atlases without downscaling.
        private const int ATLAS_IMPORT_SIZE = 2048;
        private const float FACE_INSET = 0.035f;

        #region Public API

        [MenuItem("Crookedile/Art/Build Half Cube Preview")]
        public static void BuildPreview()
        {
            ConfigureImporter(ALBEDO_PATH, TextureImporterType.Default, true);
            ConfigureImporter(NORMAL_PATH, TextureImporterType.NormalMap, false);
            ConfigureImporter(SPECULAR_PATH, TextureImporterType.Default, false);
            ConfigureImporter(OCCLUSION_PATH, TextureImporterType.Default, false);
            ConfigureImporter(EMISSION_PATH, TextureImporterType.Default, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Shader shader = Shader.Find(SHADER_NAME);
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(ALBEDO_PATH);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NORMAL_PATH);
            Texture2D specular = AssetDatabase.LoadAssetAtPath<Texture2D>(SPECULAR_PATH);
            Texture2D occlusion = AssetDatabase.LoadAssetAtPath<Texture2D>(OCCLUSION_PATH);
            Texture2D emission = AssetDatabase.LoadAssetAtPath<Texture2D>(EMISSION_PATH);

            if (shader == null || albedo == null || normal == null || specular == null || occlusion == null || emission == null)
            {
                throw new InvalidOperationException("The half-cube shader or one of its textures failed to import.");
            }

            Material material = CreateMaterial(shader, albedo, normal, specular, occlusion, emission);
            GameObject prefabSource = CreateSpriteObject(material);
            PrefabUtility.SaveAsPrefabAsset(prefabSource, PREFAB_PATH);
            UnityEngine.Object.DestroyImmediate(prefabSource);

            CreatePreviewScene(material);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        #endregion

        #region Private API

        private static void ConfigureImporter(string assetPath, TextureImporterType textureType, bool isSprite)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (importer == null)
            {
                throw new InvalidOperationException($"Texture importer not found for {assetPath}.");
            }

            importer.textureType = textureType;
            importer.sRGBTexture = isSprite;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = ATLAS_IMPORT_SIZE;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;

            importer.SaveAndReimport();
        }

        private static Material CreateMaterial(Shader shader, Texture2D albedo, Texture2D normal, Texture2D specular, Texture2D occlusion, Texture2D emission)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
            if (material == null)
            {
                material = new Material(shader);
                material.name = "Half Cube Sprite Lit";
                AssetDatabase.CreateAsset(material, MATERIAL_PATH);
            }

            material.shader = shader;
            material.SetTexture("_MainTex", albedo);
            material.SetTexture("_NormalMap", normal);
            material.SetTexture("_SpecularMap", specular);
            material.SetTexture("_OcclusionMap", occlusion);
            material.SetFloat("_OcclusionStrength", 0.4f);
            material.SetTexture("_EmissionMap", emission);
            material.enableInstancing = true;
            material.SetVector("_RoofAtlasRect", GetAtlasRect(new Rect(0f, 0.5f, 0.5f, 0.5f)));
            material.SetVector("_LeftAtlasRect", GetAtlasRect(new Rect(0.5f, 0.5f, 0.5f, 0.5f)));
            material.SetVector("_RightAtlasRect", GetAtlasRect(new Rect(0f, 0f, 0.5f, 0.5f)));
            material.SetVector("_KeyLightDirection", new Vector4(-0.4f, 0.35f, -0.85f, 0f));
            material.SetColor("_KeyLightColor", new Color(1f, 0.88f, 0.72f, 1f));
            material.SetFloat("_AmbientStrength", 0.45f);
            material.SetFloat("_DiffuseStrength", 0.8f);
            material.SetFloat("_SpecularStrength", 0.35f);
            material.SetFloat("_SpecularPower", 24f);
            material.SetFloat("_SpecularEdgeOnly", 1f);
            material.SetFloat("_NormalFlipX", 1f);
            material.SetFloat("_NormalFlipY", 1f);
            material.SetColor("_EdgeHighlightColor", new Color(1f, 0.82f, 0.58f, 1f));
            material.SetFloat("_EdgeHighlightStrength", 0.5f);
            material.SetFloat("_EdgeHighlightWidth", 0.025f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Vector4 GetAtlasRect(Rect rect)
        {
            float insetX = rect.width * FACE_INSET;
            float insetY = rect.height * FACE_INSET;
            return new Vector4(rect.xMin + insetX, rect.yMin + insetY, rect.width - (insetX * 2f), rect.height - (insetY * 2f));
        }

        private static GameObject CreateSpriteObject(Material material)
        {
            GameObject gameObject = new GameObject("Half Cube");
            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            gameObject.AddComponent<HalfCubeLightLink>();
            // The caster goes first so the building can write its outline when it builds the mesh.
            gameObject.AddComponent<ShadowCaster2D>();
            gameObject.AddComponent<HalfCubeBuilding>();
            return gameObject;
        }

        private static void CreatePreviewScene(Material material)
        {
            Scene scene = SceneManager.GetSceneByPath(SCENE_PATH);
            bool createdScene = !scene.IsValid() || !scene.isLoaded;
            if (createdScene)
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            }
            else
            {
                GameObject[] existingRoots = scene.GetRootGameObjects();
                for (int index = 0; index < existingRoots.Length; index++)
                {
                    UnityEngine.Object.DestroyImmediate(existingRoots[index]);
                }
            }

            SceneManager.SetActiveScene(scene);

            Camera camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 1f;
            camera.backgroundColor = new Color(0.13f, 0.15f, 0.18f, 1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            Light2D keyLight = CreateLight("Key Light", Light2D.LightType.Point, 1, new Color(1f, 0.82f, 0.62f), 1.2f, new Vector3(-0.8f, 0.75f, -1f), 2.5f, 65f, 35f, 1.5f);
            

            GameObject previewObject = CreateSpriteObject(material);
            previewObject.name = "Half Cube";
            previewObject.GetComponent<HalfCubeLightLink>().SetLight(keyLight);

            if (!EditorSceneManager.SaveScene(scene, SCENE_PATH))
            {
                if (createdScene)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                throw new InvalidOperationException("The half-cube preview scene could not be saved.");
            }

            if (createdScene)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Light2D CreateLight(string name, Light2D.LightType lightType, int blendStyleIndex, Color color, float intensity, Vector3 position, float radius, float outerAngle, float innerAngle, float normalMapDistance)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.position = position;

            Light2D light = gameObject.AddComponent<Light2D>();
            light.lightType = lightType;
            light.blendStyleIndex = blendStyleIndex;
            light.color = color;
            light.intensity = intensity;
            SerializedObject serializedLight = new SerializedObject(light);
            SerializedProperty normalDistance = serializedLight.FindProperty("m_NormalMapDistance");
            if (normalDistance != null)
            {
                normalDistance.floatValue = normalMapDistance;
                serializedLight.ApplyModifiedProperties();
            }

            if (lightType == Light2D.LightType.Point && outerAngle < 360f)
            {
                SerializedProperty normalQuality = serializedLight.FindProperty("m_NormalMapQuality");
                if (normalQuality != null)
                {
                    normalQuality.intValue = (int)Light2D.NormalMapQuality.Accurate;
                    serializedLight.ApplyModifiedProperties();
                }
            }

            if (lightType == Light2D.LightType.Point)
            {
                light.pointLightOuterRadius = radius;
                light.pointLightOuterAngle = outerAngle;
                light.pointLightInnerAngle = innerAngle;
                light.shadowsEnabled = true;
                light.shadowIntensity = 0.55f;

                if (outerAngle < 360f)
                {
                    gameObject.transform.up = new Vector3(-position.x, -position.y, 0f).normalized;
                }
            }
            return light;
        }

        #endregion
    }
}
