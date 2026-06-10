using System.Linq;
using Crookedile.Data;
using Crookedile.Gameplay.Battle;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Play-mode live battle inspector — a read-only dashboard of the active <see cref="BattleManager"/>:
    /// opinion / Support / Denial, the archetype resources (Patronage, Attention), action points,
    /// deck pile counts, active player statuses, and every enemy's hostility / intent / statuses.
    /// Richer than the in-game BattleStatsOverlay and a natural companion to the Cheats window.
    ///
    /// Menu: Crookedile → Battle Inspector. Repaints itself each editor tick while in Play mode.
    /// </summary>
    public class BattleInspectorWindow : EditorWindow
    {
        private Vector2 _scroll;

        [MenuItem("Crookedile/Battle Inspector")]
        public static void ShowWindow()
        {
            var win = GetWindow<BattleInspectorWindow>("Battle Inspector");
            win.minSize = new Vector2(360, 420);
            win.Show();
        }

        private void OnEnable() => EditorApplication.update += OnEditorTick;

        private void OnDisable() => EditorApplication.update -= OnEditorTick;

        // Keep the live values fresh without the user mousing over the window.
        private void OnEditorTick()
        {
            if (Application.isPlaying)
                Repaint();
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                SirenixEditorGUI.MessageBox(
                    "Enter Play Mode and start a battle to inspect live state.",
                    MessageType.Info
                );
                return;
            }

            var bm = Object.FindObjectOfType<BattleManager>();
            if (bm == null)
            {
                SirenixEditorGUI.MessageBox(
                    "No BattleManager in the scene yet.",
                    MessageType.Warning
                );
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawHeader(bm);
            DrawPlayer(bm);
            DrawPlayerStatuses(bm);
            DrawEnemies(bm);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawHeader(BattleManager bm)
        {
            SirenixEditorGUI.Title(
                $"{bm.PlayerOrigin}",
                $"{bm.CurrentState}   •   turn {bm.CurrentTurn}   •   "
                    + (bm.IsPlayerTurn ? "player's turn" : "enemy's turn"),
                TextAlignment.Left,
                true
            );
        }

        private static void DrawPlayer(BattleManager bm)
        {
            SirenixEditorGUI.BeginBox();
            SirenixEditorGUI.Title("Player", "", TextAlignment.Left, false);

            Field("Opinion", $"{bm.CurrentOpinion} / {bm.MaxOpinion}  ({bm.OpinionPercentage:P0})");
            Field("Support / Denial", $"{bm.CurrentSupport}  /  {bm.CurrentDenial}");
            Field("Patronage", bm.CurrentPatronage.ToString());
            Field("Attention", bm.CurrentAttention.ToString());

            var stats = bm.PlayerStats;
            if (stats != null)
                Field(
                    "Action Points",
                    $"{stats.CurrentActionPoints} / {stats.MaxActionPoints}"
                        + (stats.ActionPointsNextTurn > 0 ? $"  (+{stats.ActionPointsNextTurn} next)" : "")
                );
            Field("Conversions this turn", bm.ConversionsThisTurn.ToString());
            Field("Turns elapsed", $"{bm.PlayerTurnsElapsed} / {bm.MaxTurns}");

            var deck = bm.PlayerDeck;
            if (deck != null)
                Field(
                    "Piles",
                    $"hand {deck.HandCount}  •  draw {deck.DeckCount}  •  discard {deck.DiscardCount}  •  exhaust {deck.ExhaustCount}"
                );

            SirenixEditorGUI.EndBox();
        }

        private static void DrawPlayerStatuses(BattleManager bm)
        {
            var mgr = bm.PlayerStatusEffects;
            SirenixEditorGUI.BeginBox();
            SirenixEditorGUI.Title("Player statuses", "", TextAlignment.Left, false);
            if (mgr == null)
            {
                EditorGUILayout.LabelField("(no status manager)", EditorStyles.miniLabel);
            }
            else
            {
                var all = mgr.GetDebuffs().Concat(mgr.GetBuffs()).ToList();
                if (all.Count == 0)
                    EditorGUILayout.LabelField("(none)", EditorStyles.miniLabel);
                foreach (var s in all)
                    Field(s.DisplayName, $"x{s.Stacks}");
            }
            SirenixEditorGUI.EndBox();
        }

        private static void DrawEnemies(BattleManager bm)
        {
            var enemies = bm.Enemies;
            SirenixEditorGUI.Title(
                "Enemies",
                $"{enemies?.Count ?? 0}   •   focused #{bm.FocusedEnemyIndex}",
                TextAlignment.Left,
                false
            );

            if (enemies == null || enemies.Count == 0)
            {
                SirenixEditorGUI.MessageBox("No enemies.", MessageType.Info);
                return;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null)
                    continue;

                SirenixEditorGUI.BeginBox();
                SirenixEditorGUI.BeginBoxHeader();
                EditorGUILayout.BeginHorizontal();
                string name = e.EnemyData != null ? e.EnemyData.EnemyName : $"Enemy {i}";
                GUILayout.Label(
                    $"#{i} {name}" + (i == bm.FocusedEnemyIndex ? "  (focused)" : ""),
                    EditorStyles.boldLabel
                );
                GUILayout.FlexibleSpace();
                if (e.IsDefeated)
                    GUILayout.Label("converted", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
                SirenixEditorGUI.EndBoxHeader();

                var st = e.Stats;
                if (st != null)
                {
                    string tag =
                        st.IsHostile ? "hostile"
                        : st.IsReceptive ? "receptive"
                        : "neutral";
                    Field("Hostility", $"{st.CurrentHostility}  ({tag})");
                    string flags =
                        (st.IsHardened ? "Hardened " : "") + (st.IsFanatic ? "Fanatic" : "");
                    if (!string.IsNullOrWhiteSpace(flags))
                        Field("Flags", flags.Trim());
                }

                var intent = e.CurrentIntent;
                Field(
                    "Intent",
                    intent != null
                        ? $"{intent.MoveType}"
                            + (
                                string.IsNullOrWhiteSpace(intent.IntentDescription)
                                    ? ""
                                    : $" — {intent.IntentDescription}"
                            )
                        : "(none)"
                );

                if (e.StatusEffects != null)
                {
                    var statuses = e
                        .StatusEffects.GetDebuffs()
                        .Concat(e.StatusEffects.GetBuffs())
                        .ToList();
                    if (statuses.Count > 0)
                        Field(
                            "Statuses",
                            string.Join("  ", statuses.Select(s => $"{s.DisplayName} x{s.Stacks}"))
                        );
                }

                SirenixEditorGUI.EndBox();
                GUILayout.Space(3);
            }
        }

        private static void Field(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(150));
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }
    }
}
