using Crookedile.Managers;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Crookedile.Editor
{
    /// <summary>
    /// Editor window for quick access to cheat commands during development.
    /// Accessible via menu: Crookedile > Cheats Window
    /// </summary>
    public class CheatsWindow : EditorWindow
    {
        private Vector2 _scrollPosition;

        [MenuItem("Crookedile/Cheats Window")]
        public static void ShowWindow()
        {
            CheatsWindow window = GetWindow<CheatsWindow>("Cheats");
            window.minSize = new Vector2(300, 400);
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                SirenixEditorGUI.MessageBox("Enter Play Mode to use cheats.", MessageType.Info);
                return;
            }

            if (CheatsManager.Instance == null)
            {
                SirenixEditorGUI.MessageBox(
                    "CheatsManager not found in scene.",
                    MessageType.Warning
                );
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawGeneralCheats();
            DrawResourceCheats();
            DrawCardCheats();
            DrawTimeCheats();
            DrawBattleCheats();
            DrawHotkeys();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>Opens a titled Sirenix box; pair with <see cref="EndSection"/>.</summary>
        private static void BeginSection(string title)
        {
            SirenixEditorGUI.BeginBox();
            SirenixEditorGUI.Title(title, "", TextAlignment.Left, true);
        }

        private static void EndSection()
        {
            SirenixEditorGUI.EndBox();
            GUILayout.Space(4);
        }

        private void DrawHeader()
        {
            SirenixEditorGUI.Title("Crookedile Cheats", "Play-mode only", TextAlignment.Left, true);
#if CHEATS_ENABLED
            SirenixEditorGUI.MessageBox("CHEATS_ENABLED build flag is set.", MessageType.Info);
#else
            SirenixEditorGUI.MessageBox(
                "CHEATS_ENABLED flag is not set. Use menu: Crookedile > Toggle Cheats Build.",
                MessageType.Warning
            );
#endif
            GUILayout.Space(4);
        }

        private void DrawGeneralCheats()
        {
            BeginSection("General");
            if (GUILayout.Button("Toggle God Mode", GUILayout.Height(28)))
                CheatsManager.Instance.ToggleGodMode();
            if (GUILayout.Button("Toggle Unlimited Resources", GUILayout.Height(28)))
                CheatsManager.Instance.ToggleUnlimitedResources();
            EndSection();
        }

        private void DrawResourceCheats()
        {
            BeginSection("Resources");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Give Max Resources"))
                CheatsManager.Instance.GiveMaxResources();
            if (GUILayout.Button("Clear Heat"))
                CheatsManager.Instance.ClearHeat();
            EditorGUILayout.EndHorizontal();
            EndSection();
        }

        private void DrawCardCheats()
        {
            BeginSection("Cards");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Draw 5 Cards"))
                CheatsManager.Instance.DrawCards(5);
            if (GUILayout.Button("Refresh Hand"))
                CheatsManager.Instance.RefreshHand();
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Unlock All Cards", GUILayout.Height(28)))
                CheatsManager.Instance.UnlockAllCards();
            EndSection();
        }

        private void DrawTimeCheats()
        {
            BeginSection("Time");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("0.5x"))
                CheatsManager.Instance.SetTimeScale(0.5f);
            if (GUILayout.Button("1x"))
                CheatsManager.Instance.SetTimeScale(1f);
            if (GUILayout.Button("2x"))
                CheatsManager.Instance.SetTimeScale(2f);
            if (GUILayout.Button("5x"))
                CheatsManager.Instance.SetTimeScale(5f);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Skip Day"))
                CheatsManager.Instance.SkipDay();
            EndSection();
        }

        private void DrawBattleCheats()
        {
            BeginSection("Battle");
            if (GUILayout.Button("Win Current Battle", GUILayout.Height(28)))
                CheatsManager.Instance.WinBattle();
            if (GUILayout.Button("Set Opponent Confidence to 1"))
                CheatsManager.Instance.SetOpponentConfidence(1);
            EndSection();
        }

        private void DrawHotkeys()
        {
            BeginSection("Hotkeys");
            SirenixEditorGUI.MessageBox(
                "F1 - Toggle Cheat Panel\n"
                    + "F2 - Toggle God Mode\n"
                    + "F3 - Toggle Unlimited Resources\n"
                    + "F4 - Give Resources\n"
                    + "+ - Speed Up Time\n"
                    + "- - Slow Down Time\n"
                    + "0 - Reset Time",
                MessageType.Info
            );
            EndSection();
        }
    }
}
