using System;
using Crookedile.Data.Campaign;
using Crookedile.Utilities;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    public static class ContentAssetNamingChecks
    {
        [MenuItem("Crookedile/Run Asset Naming Checks")]
        public static void Run()
        {
            CheckNames();
            string folder = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/ContentAssetNamingChecks"
            );
            AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(folder));
            try
            {
                var district = ScriptableObject.CreateInstance<DistrictData>();
                AssetDatabase.CreateAsset(district, folder + "/Original.asset");
                Require(
                    !string.IsNullOrEmpty(ContentAssetNaming.Inspect(district).Error),
                    "District filename fallback must not hide an empty authored name."
                );
                SetName(district, "Named District");

                var network = ScriptableObject.CreateInstance<CampaignTravelData>();
                using (var serialized = new SerializedObject(network))
                {
                    serialized.FindProperty("_headquarters").objectReferenceValue = district;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                AssetDatabase.CreateAsset(network, folder + "/Reference.asset");
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(district));
                Require(
                    string.IsNullOrEmpty(ContentAssetNaming.Rename(district)),
                    "Valid rename failed."
                );
                Require(
                    AssetDatabase.GetAssetPath(district) == folder + "/Named District.asset",
                    "Rename must use the authored name in the same folder."
                );
                Require(
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(district)) == guid,
                    "Rename must preserve the asset GUID."
                );
                AssetDatabase.ImportAsset(
                    folder + "/Reference.asset",
                    ImportAssetOptions.ForceUpdate
                );
                network = AssetDatabase.LoadAssetAtPath<CampaignTravelData>(
                    folder + "/Reference.asset"
                );
                Require(
                    network.Headquarters == district,
                    "Serialized references must survive rename."
                );
                Require(
                    !ContentAssetNaming.Inspect(district).CanRename,
                    "Matching names must be a no-op."
                );

                SetName(district, "named district");
                Require(
                    string.IsNullOrEmpty(ContentAssetNaming.Rename(district)),
                    "Case-only rename failed."
                );
                Require(
                    AssetDatabase.GetAssetPath(district) == folder + "/named district.asset",
                    "Filename casing must match the authored name."
                );

                var other = ScriptableObject.CreateInstance<DistrictData>();
                AssetDatabase.CreateAsset(other, folder + "/Other.asset");
                SetName(other, "NAMED DISTRICT");
                Require(
                    !ContentAssetNaming.Inspect(other).CanRename,
                    "Existing filenames must block case-insensitive collisions."
                );
                SetName(district, "Shared Target");
                SetName(other, "Shared Target");
                Require(
                    !string.IsNullOrEmpty(ContentAssetNaming.Rename(district)),
                    "Two authored names targeting the same new filename must be blocked."
                );
                Require(
                    AssetDatabase.GetAssetPath(other) == folder + "/Other.asset",
                    "Collision handling must not overwrite another asset."
                );
                GameLogger.LogInfo("Content", "Asset naming checks passed.");
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        public static void CheckNames()
        {
            foreach (
                string invalid in new[]
                {
                    null,
                    "",
                    " \t",
                    "Unknown Enemy",
                    "New Zone",
                    "New Ally",
                    " leading",
                    "trailing ",
                    "dot.",
                    "bad/name",
                    "bad\\name",
                    "bad:name",
                    "bad?name",
                    "bad\nname",
                    "CON",
                    "con.txt",
                    "LPT1",
                    "COM9.notes",
                    "COM¹",
                }
            )
            {
                Require(
                    !string.IsNullOrEmpty(ContentAssetNaming.ValidateName(invalid)),
                    $"Invalid name accepted: {invalid}"
                );
            }

            foreach (
                string valid in new[]
                {
                    "People's Choice",
                    "Debate +",
                    "Brgy. Fiesta",
                    "Ñame",
                    "COM10",
                }
            )
            {
                Require(
                    ContentAssetNaming.ValidateName(valid) == null,
                    $"Valid name rejected: {valid}"
                );
            }
        }

        private static void SetName(DistrictData asset, string value)
        {
            using var serialized = new SerializedObject(asset);
            serialized.FindProperty("_displayName").stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
