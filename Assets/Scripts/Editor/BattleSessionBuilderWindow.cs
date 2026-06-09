using System.Linq;
using Crookedile.Data;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Encounter builder — an Odin menu window listing every <see cref="BattleSession"/> in the
    /// project down the side and editing the selected one inline (its rounds list draws through the
    /// asset's normal Odin inspector). Create / duplicate / delete sessions from the toolbar so you
    /// never have to hunt for the assets in the Project window.
    ///
    /// Menu: Crookedile → Battle Session Builder.
    /// </summary>
    public class BattleSessionBuilderWindow : OdinMenuEditorWindow
    {
        [MenuItem("Crookedile/Battle Session Builder")]
        public static void ShowWindow()
        {
            var win = GetWindow<BattleSessionBuilderWindow>("Battle Sessions");
            win.minSize = new Vector2(620, 460);
            win.Show();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree(false);
            tree.Config.DrawSearchToolbar = true;

            var sessions = AssetDatabase
                .FindAssets("t:" + nameof(BattleSession))
                .Select(g => AssetDatabase.LoadAssetAtPath<BattleSession>(
                    AssetDatabase.GUIDToAssetPath(g)
                ))
                .Where(s => s != null)
                .OrderBy(s => s.name);

            foreach (var session in sessions)
                tree.Add($"{session.name}  ({session.RoundCount})", session);

            return tree;
        }

        protected override void OnBeginDrawEditors()
        {
            var selected = MenuTree?.Selection?.SelectedValue as BattleSession;

            float h = MenuTree?.Config.SearchToolbarHeight ?? 22f;
            SirenixEditorGUI.BeginHorizontalToolbar(h);

            if (SirenixEditorGUI.ToolbarButton("New Session"))
                CreateSession();

            using (new EditorGUI.DisabledScope(selected == null))
            {
                if (SirenixEditorGUI.ToolbarButton("Duplicate"))
                    Duplicate(selected);
                if (SirenixEditorGUI.ToolbarButton("Delete"))
                    Delete(selected);
            }

            GUILayout.FlexibleSpace();
            SirenixEditorGUI.EndHorizontalToolbar();
        }

        private void CreateSession()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "New Battle Session",
                "BattleSession",
                "asset",
                "Choose where to save the new BattleSession."
            );
            if (string.IsNullOrEmpty(path))
                return;

            var asset = CreateInstance<BattleSession>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            ForceMenuTreeRebuild();
            TrySelect(asset);
        }

        private void Duplicate(BattleSession source)
        {
            string srcPath = AssetDatabase.GetAssetPath(source);
            string newPath = AssetDatabase.GenerateUniqueAssetPath(srcPath);
            if (AssetDatabase.CopyAsset(srcPath, newPath))
            {
                AssetDatabase.SaveAssets();
                ForceMenuTreeRebuild();
                TrySelect(AssetDatabase.LoadAssetAtPath<BattleSession>(newPath));
            }
        }

        private void Delete(BattleSession session)
        {
            if (
                !EditorUtility.DisplayDialog(
                    "Delete Battle Session",
                    $"Delete '{session.name}'? This cannot be undone.",
                    "Delete",
                    "Cancel"
                )
            )
                return;
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(session));
            AssetDatabase.SaveAssets();
            ForceMenuTreeRebuild();
        }

        private void TrySelect(BattleSession asset)
        {
            if (asset == null)
                return;
            var item = MenuTree?.EnumerateTree().FirstOrDefault(i => i.Value as BattleSession == asset);
            item?.Select();
        }
    }
}
