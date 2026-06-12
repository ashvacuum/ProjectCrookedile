using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Content Hub provider that audits serialized UnityEngine.Object references on
    /// Crookedile components — null [SerializeField] refs surface here BEFORE they become
    /// runtime NullReferences. Scans every prefab under Assets/ plus all currently open
    /// scenes, and reports one row per component instance that has missing references.
    ///
    /// Severity heuristic: a field whose tooltip mentions "optional", "fallback", or
    /// "when ... absent" reports as Info; everything else as Warning. UI work rule of
    /// thumb: refresh this tab after moving serialized fields between components.
    /// </summary>
    public class UIRefsAuditProvider : ContentAuditWindow.IContentProvider
    {
        public string Category => "UI refs";

        public IEnumerable<ContentAuditWindow.Row> Rows()
        {
            var rows = new List<ContentAuditWindow.Row>();

            // --- Prefabs (project-wide, Assets/ only — Packages are not ours to audit).
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;
                AuditHierarchy(prefab.transform, $"prefab {System.IO.Path.GetFileName(path)}", rows);
            }

            // --- Open scenes (whatever the user has loaded right now).
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded)
                    continue;
                foreach (var root in scene.GetRootGameObjects())
                    AuditHierarchy(root.transform, $"scene {scene.name}", rows);
            }

            if (rows.Count == 0)
                rows.Add(
                    new ContentAuditWindow.Row(
                        "All serialized references assigned",
                        "No Crookedile component with a missing [SerializeField] reference was found.",
                        null,
                        null
                    )
                );

            return rows;
        }

        private static void AuditHierarchy(
            Transform root,
            string source,
            List<ContentAuditWindow.Row> rows
        )
        {
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null)
                    continue; // missing script — CardsProvider-style checks cover assets; skip here
                var type = mb.GetType();
                if (type.Namespace == null || !type.Namespace.StartsWith("Crookedile"))
                    continue;

                var issues = AuditComponent(mb, type);
                if (issues.Count == 0)
                    continue;

                rows.Add(
                    new ContentAuditWindow.Row(
                        $"{type.Name} on '{mb.gameObject.name}'",
                        source,
                        mb,
                        issues
                    )
                );
            }
        }

        private static List<ContentAuditWindow.AuditIssue> AuditComponent(
            MonoBehaviour mb,
            Type type
        )
        {
            var issues = new List<ContentAuditWindow.AuditIssue>();

            for (Type t = type; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
            {
                foreach (
                    var field in t.GetFields(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly
                    )
                )
                {
                    if (!typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                        continue;
                    bool serialized =
                        field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
                    if (!serialized || field.GetCustomAttribute<NonSerializedAttribute>() != null)
                        continue;

                    var value = field.GetValue(mb) as UnityEngine.Object;
                    if (value != null)
                        continue; // assigned (Unity fake-null compares true against null)

                    string tooltip = field.GetCustomAttribute<TooltipAttribute>()?.tooltip ?? "";
                    bool optional =
                        tooltip.IndexOf("optional", StringComparison.OrdinalIgnoreCase) >= 0
                        || tooltip.IndexOf("fallback", StringComparison.OrdinalIgnoreCase) >= 0
                        || tooltip.IndexOf("absent", StringComparison.OrdinalIgnoreCase) >= 0;

                    issues.Add(
                        new ContentAuditWindow.AuditIssue(
                            optional
                                ? ContentAuditWindow.Severity.Info
                                : ContentAuditWindow.Severity.Warning,
                            $"{NiceFieldName(field.Name)} is not assigned"
                                + (optional ? " (tooltip marks it optional)" : "")
                        )
                    );
                }
            }

            return issues;
        }

        private static string NiceFieldName(string raw) =>
            ObjectNames.NicifyVariableName(raw.TrimStart('_'));
    }
}
