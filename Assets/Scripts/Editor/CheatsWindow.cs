using UnityEditor;
using UnityEngine;
using Crookedile.Managers;

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
                EditorGUILayout.HelpBox("Enter Play Mode to use cheats", MessageType.Info);
                return;
            }

            if (CheatsManager.Instance == null)
            {
                EditorGUILayout.HelpBox("CheatsManager not found in scene", MessageType.Warning);
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

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Crookedile Cheats", EditorStyles.boldLabel);
#if CHEATS_ENABLED
            EditorGUILayout.LabelField("CHEATS_ENABLED: True", EditorStyles.boldLabel);
#else
            EditorGUILayout.HelpBox("CHEATS_ENABLED flag is not set. Use menu: Crookedile > Toggle Cheats Build", MessageType.Warning);
#endif
            EditorGUILayout.Space(10);
        }

        private void DrawGeneralCheats()
        {
            EditorGUILayout.LabelField("General Cheats", EditorStyles.boldLabel);

            if (GUILayout.Button("Toggle God Mode", GUILayout.Height(30)))
            {
                CheatsManager.Instance.ToggleGodMode();
            }

            if (GUILayout.Button("Toggle Unlimited Resources", GUILayout.Height(30)))
            {
                CheatsManager.Instance.ToggleUnlimitedResources();
            }

            EditorGUILayout.Space(10);
        }

        private void DrawResourceCheats()
        {
            EditorGUILayout.LabelField("Resource Cheats", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Give Max Resources"))
            {
                CheatsManager.Instance.GiveMaxResources();
            }
            if (GUILayout.Button("Clear Heat"))
            {
                CheatsManager.Instance.ClearHeat();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
        }

        private void DrawCardCheats()
        {
            EditorGUILayout.LabelField("Card Cheats", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Draw 5 Cards"))
            {
                CheatsManager.Instance.DrawCards(5);
            }
            if (GUILayout.Button("Refresh Hand"))
            {
                CheatsManager.Instance.RefreshHand();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Unlock All Cards", GUILayout.Height(30)))
            {
                CheatsManager.Instance.UnlockAllCards();
            }

            EditorGUILayout.Space(10);
        }

        private void DrawTimeCheats()
        {
            EditorGUILayout.LabelField("Time Cheats", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("0.5x"))
            {
                CheatsManager.Instance.SetTimeScale(0.5f);
            }
            if (GUILayout.Button("1x"))
            {
                CheatsManager.Instance.SetTimeScale(1f);
            }
            if (GUILayout.Button("2x"))
            {
                CheatsManager.Instance.SetTimeScale(2f);
            }
            if (GUILayout.Button("5x"))
            {
                CheatsManager.Instance.SetTimeScale(5f);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Skip Day"))
            {
                CheatsManager.Instance.SkipDay();
            }

            EditorGUILayout.Space(10);
        }

        private void DrawBattleCheats()
        {
            EditorGUILayout.LabelField("Battle Cheats", EditorStyles.boldLabel);

            if (GUILayout.Button("Win Current Battle", GUILayout.Height(30)))
            {
                CheatsManager.Instance.WinBattle();
            }

            if (GUILayout.Button("Set Opponent Confidence to 1"))
            {
                CheatsManager.Instance.SetOpponentConfidence(1);
            }

            EditorGUILayout.Space(10);
        }

        private void DrawHotkeys()
        {
            EditorGUILayout.LabelField("Hotkeys", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "F1 - Toggle Cheat Panel\n" +
                "F2 - Toggle God Mode\n" +
                "F3 - Toggle Unlimited Resources\n" +
                "F4 - Give Resources\n" +
                "+ - Speed Up Time\n" +
                "- - Slow Down Time\n" +
                "0 - Reset Time",
                MessageType.Info
            );
        }
    }
}
