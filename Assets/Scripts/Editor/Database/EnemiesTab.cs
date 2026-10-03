using System;
using System.Collections.Generic;
using System.Linq;
using Crookedile.Data.Enemy;
using Crookedile.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Crookedile.Editor.Database
{
    /// <summary>Every <see cref="EnemyData"/>: its hostility line, its moves per stance, and what's wrong with it.</summary>
    public sealed class EnemiesTab : ContentTab<EnemyData>
    {
        private static readonly EnemyStance[] Stances = { EnemyStance.Aggressive, EnemyStance.Neutral, EnemyStance.Receptive };

        private EnemyMovePattern? _pattern;
        private bool _passivesOnly;
        private bool _summonersOnly;

        public override string Title => "Enemies";

        protected override string DisplayName(EnemyData enemy) =>
            string.IsNullOrWhiteSpace(enemy.EnemyName) ? $"({enemy.name})" : enemy.EnemyName;

        protected override string SearchText(EnemyData enemy) =>
            enemy.name + " " + string.Join(" ", enemy.Moves.Where(m => m != null).Select(m => m.MoveName));

        protected override IEnumerable<Column> BuildColumns()
        {
            yield return new Column("Enemy", 150, null, e => DisplayName(e));
            yield return new Column("Start", 40, e => e.StartingHostility.ToString(), e => e.StartingHostility);
            yield return new Column("Range", 62, e => $"{e.MinHostility}…{e.MaxHostility}", e => e.MaxHostility - e.MinHostility);
            yield return new Column("Zone", 38, e => e.NeutralZone.ToString(), e => e.NeutralZone);
            yield return new Column("Moves A/N/R", 78, MoveCounts, e => e.Moves.Count);
            yield return new Column("Passives", 56, e => Blank(e.Passives.Count), e => e.Passives.Count);
            yield return new Column("Start FX", 56, e => Blank(e.StartingEffects.Count), e => e.StartingEffects.Count);
            yield return new Column("Pattern", 96, e => e.MovePattern.ToString(), e => e.MovePattern);
        }

        protected override IEnumerable<TabAction> Actions()
        {
            yield return new TabAction("Refresh databases", DatabaseAutoRefresh.RefreshAll);
        }

        // ---- Filters --------------------------------------------------------------------

        protected override bool DrawFilters()
        {
            bool changed = EnumFilter("Pattern", ref _pattern);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Show", EditorStyles.miniLabel, GUILayout.Width(62));
                changed |= Toggle(ref _passivesOnly, "With passives");
                changed |= Toggle(ref _summonersOnly, "Summoners");
                GUILayout.FlexibleSpace();
            }
            return changed;
        }

        protected override bool PassesFilters(EnemyData enemy) =>
            (!_pattern.HasValue || enemy.MovePattern == _pattern.Value)
            && (!_passivesOnly || enemy.Passives.Count > 0)
            && (!_summonersOnly || enemy.Moves.Any(m => m != null && m.MoveType == EnemyMoveType.SummonMinion));

        // ---- Audit ----------------------------------------------------------------------

        protected override IEnumerable<Issue> Audit(EnemyData enemy)
        {
            if (string.IsNullOrWhiteSpace(enemy.EnemyName) || enemy.EnemyName == "Unknown Enemy")
                yield return Issue.Warning("No display name.");
            if (enemy.Portrait == null)
                yield return Issue.Warning("No portrait.");
            if (enemy.StartingHostility < enemy.MinHostility || enemy.StartingHostility > enemy.MaxHostility)
                yield return Issue.Warning($"Starts at {enemy.StartingHostility}, outside its range {enemy.MinHostility}…{enemy.MaxHostility}.");

            var all = enemy.Moves;
            if (all.Count == 0)
            {
                yield return Issue.Error("No moves: it can never act.");
                yield break;
            }

            foreach (var stance in Stances)
            {
                var moves = enemy.GetMovesForStance(stance);
                for (int i = 0; i < moves.Count; i++)
                    if (moves[i] == null)
                        yield return Issue.Error($"{stance} move [{i}] is empty.");
                if (moves.Count == 0 && CanReach(enemy, stance))
                    yield return Issue.Info($"Can turn {stance} but has no {stance} moves; it falls back to its other moves.");
            }

            foreach (var move in all.Where(m => m != null).Distinct())
                foreach (var issue in MoveIssues(move))
                    yield return new Issue(issue.Level, $"Move '{MoveLabel(move)}': {issue.Text}");
        }

        private static IEnumerable<Issue> MoveIssues(EnemyMoveData move)
        {
            if (string.IsNullOrWhiteSpace(move.MoveName))
                yield return Issue.Info("no move name.");
            if (string.IsNullOrWhiteSpace(move.IntentDescription))
                yield return Issue.Warning("no intent description, so the player can't read it.");
            bool needsEffects = move.MoveType != EnemyMoveType.Idle && move.MoveType != EnemyMoveType.SummonMinion;
            if (needsEffects && (move.Effects == null || move.Effects.Count == 0))
                yield return Issue.Warning("no effects, so it does nothing.");
            if (move.Effects != null)
                foreach (var effect in move.Effects)
                    if (effect == null)
                        yield return Issue.Error("an effect slot is empty.");
                    else
                        foreach (var problem in effect.GetConfigurationIssues())
                            yield return Issue.Warning($"{effect.GetType().Name}: {problem}");
            if (move.MoveType == EnemyMoveType.SummonMinion && move.MinionToSummon == null)
                yield return Issue.Error("summon move with no minion set.");
        }

        // ---- Appearance -----------------------------------------------------------------

        protected override void DrawPreview(EnemyData enemy)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                Picture(enemy.Portrait, 110);
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(DisplayName(enemy), EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(
                        $"Hostility starts {enemy.StartingHostility}, range {enemy.MinHostility}…{enemy.MaxHostility}, neutral within ±{enemy.NeutralZone}",
                        EditorStyles.wordWrappedMiniLabel
                    );
                    DrawHostilityLine(enemy);
                    if (enemy.Passives.Count > 0)
                        EditorGUILayout.LabelField($"Passives: {enemy.Passives.Count}", EditorStyles.miniLabel);
                    if (enemy.StartingEffects.Count > 0)
                        EditorGUILayout.LabelField(
                            "Starts with: " + string.Join(", ", enemy.StartingEffects.Where(s => s?.Behavior != null).Select(s => $"{s.Behavior.DisplayName} ×{s.Stacks}")),
                            EditorStyles.wordWrappedMiniLabel
                        );
                }
            }

            foreach (var stance in Stances)
            {
                var moves = enemy.GetMovesForStance(stance);
                EditorGUILayout.LabelField($"{stance} ({moves.Count})", EditorStyles.boldLabel);
                foreach (var move in moves)
                {
                    if (move == null)
                    {
                        EditorGUILayout.LabelField("   (empty slot)", EditorStyles.miniLabel);
                        continue;
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(MoveLabel(move), EditorStyles.linkLabel, GUILayout.Width(150)))
                        {
                            Selection.activeObject = move;
                            EditorGUIUtility.PingObject(move);
                        }
                        GUILayout.Label(move.MoveType.ToString(), EditorStyles.miniLabel, GUILayout.Width(90));
                        GUILayout.Label(move.IntentDescription ?? "", EditorStyles.wordWrappedMiniLabel);
                    }
                }
            }
        }

        /// <summary>The hostility line from min to max, with the neutral zone and the start marked.</summary>
        private static void DrawHostilityLine(EnemyData enemy)
        {
            Rect r = GUILayoutUtility.GetRect(200, 14, GUILayout.ExpandWidth(true));
            int min = enemy.MinHostility, max = enemy.MaxHostility;
            if (max <= min)
                return;
            float X(int value) => r.x + r.width * (value - min) / (float)(max - min);
            EditorGUI.DrawRect(new Rect(r.x, r.y + 4, X(-enemy.NeutralZone) - r.x, 6), new Color(0.2f, 0.55f, 0.3f, 0.8f));
            EditorGUI.DrawRect(new Rect(X(-enemy.NeutralZone), r.y + 4, X(enemy.NeutralZone) - X(-enemy.NeutralZone), 6), new Color(0.5f, 0.5f, 0.5f, 0.8f));
            EditorGUI.DrawRect(new Rect(X(enemy.NeutralZone), r.y + 4, r.xMax - X(enemy.NeutralZone), 6), new Color(0.65f, 0.2f, 0.15f, 0.8f));
            EditorGUI.DrawRect(new Rect(X(Mathf.Clamp(enemy.StartingHostility, min, max)) - 1, r.y, 3, 14), Color.white);
        }

        protected override void DrawSummary(IReadOnlyList<EnemyData> all)
        {
            EditorGUILayout.LabelField($"{all.Count} enemies. Select one to see and edit it.", EditorStyles.miniLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("By move pattern", EditorStyles.boldLabel);
            foreach (EnemyMovePattern pattern in Enum.GetValues(typeof(EnemyMovePattern)))
                Bar(pattern.ToString(), all.Count(e => e.MovePattern == pattern), all.Count);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("State", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"No portrait: {all.Count(e => e.Portrait == null)}");
            EditorGUILayout.LabelField($"With passives: {all.Count(e => e.Passives.Count > 0)}");
            EditorGUILayout.LabelField($"Summoners: {all.Count(e => e.Moves.Any(m => m != null && m.MoveType == EnemyMoveType.SummonMinion))}");
            EditorGUILayout.LabelField($"Moves in total: {all.Sum(e => e.Moves.Count)}");
        }

        // ---- Enemy facts ----------------------------------------------------------------

        private static bool CanReach(EnemyData enemy, EnemyStance stance) =>
            stance switch
            {
                EnemyStance.Aggressive => enemy.MaxHostility > enemy.NeutralZone,
                EnemyStance.Receptive => enemy.MinHostility < -enemy.NeutralZone,
                _ => true,
            };

        private static string MoveCounts(EnemyData enemy) =>
            string.Join("/", Stances.Select(s => enemy.GetMovesForStance(s).Count));

        private static string MoveLabel(EnemyMoveData move) =>
            string.IsNullOrWhiteSpace(move.MoveName) ? move.name : move.MoveName;

        private static string Blank(int value) => value == 0 ? "" : value.ToString();

        private static bool Toggle(ref bool value, string label)
        {
            bool next = GUILayout.Toggle(value, label, EditorStyles.miniButton);
            if (next == value)
                return false;
            value = next;
            return true;
        }
    }
}
