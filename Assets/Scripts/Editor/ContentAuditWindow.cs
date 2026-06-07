using System;
using System.Collections.Generic;
using System.Linq;
using Crookedile.Data;
using Crookedile.Data.Audio;
using Crookedile.Data.Enemy;
using Crookedile.Gameplay.Battle;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Content-completeness dashboard. Runs a registry of read-only checks that verify every
    /// enum-driven piece of content is actually authored (statuses described, audio/VFX triggers
    /// mapped, etc.) and surfaces what's missing. Mutates nothing.
    ///
    /// Menu: Crookedile → Content Audit. Designed to grow — add an <see cref="IContentCheck"/> to
    /// <see cref="BuildChecks"/> and it shows up automatically.
    /// </summary>
    public class ContentAuditWindow : EditorWindow
    {
        public enum Severity
        {
            Error,
            Warning,
            Info,
        }

        public readonly struct AuditIssue
        {
            public readonly Severity Severity;
            public readonly string Message;
            public readonly UnityEngine.Object Context;

            public AuditIssue(Severity severity, string message, UnityEngine.Object context = null)
            {
                Severity = severity;
                Message = message;
                Context = context;
            }
        }

        public interface IContentCheck
        {
            string Category { get; }
            IEnumerable<AuditIssue> Run();
        }

        private List<(string category, List<AuditIssue> issues)> _results;
        private Vector2 _scroll;

        [MenuItem("Crookedile/Content Audit")]
        public static void ShowWindow()
        {
            var win = GetWindow<ContentAuditWindow>("Content Audit");
            win.minSize = new Vector2(480, 320);
            win.Show();
        }

        private static List<IContentCheck> BuildChecks() =>
            new List<IContentCheck>
            {
                new StatusCheck(),
                new StatusDatabaseCheck(),
                new AudioVfxCheck(),
                new IntentThemeCheck(),
                new EnemyCheck(),
            };

        private void RunAudit()
        {
            _results = new List<(string, List<AuditIssue>)>();
            foreach (var check in BuildChecks())
            {
                List<AuditIssue> issues;
                try
                {
                    issues = check.Run().ToList();
                }
                catch (Exception e)
                {
                    issues = new List<AuditIssue>
                    {
                        new AuditIssue(Severity.Error, $"Check threw: {e.Message}"),
                    };
                }
                _results.Add((check.Category, issues));
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Run Audit", GUILayout.Height(28)))
                RunAudit();

            if (_results == null)
            {
                EditorGUILayout.HelpBox("Press Run Audit to scan content.", MessageType.Info);
                return;
            }

            int totalErrors = _results.Sum(r => r.issues.Count(i => i.Severity == Severity.Error));
            int totalWarnings = _results.Sum(r =>
                r.issues.Count(i => i.Severity == Severity.Warning)
            );
            EditorGUILayout.LabelField(
                $"{totalErrors} error(s), {totalWarnings} warning(s) across {_results.Count} categories.",
                EditorStyles.boldLabel
            );

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var (category, issues) in _results)
            {
                int errs = issues.Count(i => i.Severity == Severity.Error);
                int warns = issues.Count(i => i.Severity == Severity.Warning);
                string suffix = issues.Count == 0 ? "OK" : $"{errs} err, {warns} warn";
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField($"{category}  —  {suffix}", EditorStyles.boldLabel);

                foreach (var issue in issues)
                {
                    var prev = GUI.color;
                    GUI.color = issue.Severity switch
                    {
                        Severity.Error => new Color(1f, 0.6f, 0.6f),
                        Severity.Warning => new Color(1f, 0.85f, 0.5f),
                        _ => Color.white,
                    };
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(
                            $"[{issue.Severity}] {issue.Message}",
                            EditorStyles.wordWrappedLabel
                        );
                        if (
                            issue.Context != null
                            && GUILayout.Button("Select", GUILayout.Width(60))
                        )
                        {
                            Selection.activeObject = issue.Context;
                            EditorGUIUtility.PingObject(issue.Context);
                        }
                    }
                    GUI.color = prev;
                }
            }
            EditorGUILayout.EndScrollView();
        }

        // ---------------------------------------------------------------------
        // Checks
        // ---------------------------------------------------------------------

        /// <summary>
        /// Validates every <see cref="StatusEffectType"/> has a non-empty description, and flags the
        /// absence of any central status visual data (icons/colors/sfx/vfx) — there is no status
        /// database yet, so a status added to the enum can be wholly invisible to the player.
        /// </summary>
        private sealed class StatusCheck : IContentCheck
        {
            public string Category => "Statuses";

            public IEnumerable<AuditIssue> Run()
            {
                foreach (StatusEffectType type in Enum.GetValues(typeof(StatusEffectType)))
                {
                    string desc = new StatusEffect(type, 1).Description;
                    if (string.IsNullOrWhiteSpace(desc))
                        yield return new AuditIssue(
                            Severity.Error,
                            $"Status '{type}' has no description (add a case in StatusEffect.GetEffectDescription)."
                        );
                }
            }
        }

        /// <summary>
        /// Validates the <see cref="StatusEffectDatabase"/> has a complete entry for every status:
        /// present, with an icon and a color. Run "Crookedile → Generate → Status Effect Database"
        /// to seed it, then fill in the visuals.
        /// </summary>
        private sealed class StatusDatabaseCheck : IContentCheck
        {
            public string Category => "Status database";

            public IEnumerable<AuditIssue> Run()
            {
                string[] guids = AssetDatabase.FindAssets("t:StatusEffectDatabase");
                if (guids.Length == 0)
                {
                    yield return new AuditIssue(
                        Severity.Warning,
                        "No StatusEffectDatabase asset — statuses have no icon/color/SFX/VFX. "
                            + "Run Crookedile → Generate → Status Effect Database."
                    );
                    yield break;
                }

                var db = AssetDatabase.LoadAssetAtPath<StatusEffectDatabase>(
                    AssetDatabase.GUIDToAssetPath(guids[0])
                );

                foreach (StatusEffectType type in Enum.GetValues(typeof(StatusEffectType)))
                {
                    if (!db.TryGet(type, out var entry))
                    {
                        yield return new AuditIssue(
                            Severity.Error,
                            $"Status '{type}' has no database entry (re-run the generator to sync).",
                            db
                        );
                        continue;
                    }
                    if (entry.Icon == null)
                        yield return new AuditIssue(
                            Severity.Warning,
                            $"Status '{type}' has no icon.",
                            db
                        );
                    if (entry.Color.a <= 0f)
                        yield return new AuditIssue(
                            Severity.Info,
                            $"Status '{type}' has no color set.",
                            db
                        );
                }
            }
        }

        /// <summary>
        /// Validates every <see cref="BattleAudioTrigger"/> is mapped in a <see cref="BattleSoundMap"/>
        /// and that the entry has at least an audio event or a visual. Unmapped triggers play no
        /// feedback at all.
        /// </summary>
        private sealed class AudioVfxCheck : IContentCheck
        {
            public string Category => "Audio / VFX triggers";

            public IEnumerable<AuditIssue> Run()
            {
                string[] guids = AssetDatabase.FindAssets("t:BattleSoundMap");
                if (guids.Length == 0)
                {
                    yield return new AuditIssue(
                        Severity.Error,
                        "No BattleSoundMap asset found — no battle audio/VFX is wired."
                    );
                    yield break;
                }

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var map = AssetDatabase.LoadAssetAtPath<BattleSoundMap>(path);
                    if (map == null)
                        continue;

                    foreach (BattleAudioTrigger trigger in Enum.GetValues(typeof(BattleAudioTrigger)))
                    {
                        if (!map.TryGet(trigger, out var entry))
                        {
                            yield return new AuditIssue(
                                Severity.Warning,
                                $"[{map.name}] trigger '{trigger}' is unmapped — no sound or VFX.",
                                map
                            );
                        }
                        else if (entry.Sound == null && entry.Visual == null)
                        {
                            yield return new AuditIssue(
                                Severity.Info,
                                $"[{map.name}] trigger '{trigger}' is mapped but has neither sound nor VFX.",
                                map
                            );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates every <see cref="EnemyMoveType"/> has an icon in an <see cref="EnemyIntentTheme"/>,
        /// so the intent badge isn't blank for some move types.
        /// </summary>
        private sealed class IntentThemeCheck : IContentCheck
        {
            public string Category => "Intent theme";

            public IEnumerable<AuditIssue> Run()
            {
                string[] guids = AssetDatabase.FindAssets("t:EnemyIntentTheme");
                if (guids.Length == 0)
                {
                    yield return new AuditIssue(
                        Severity.Warning,
                        "No EnemyIntentTheme asset — intent badges have no icons/colors."
                    );
                    yield break;
                }

                foreach (string guid in guids)
                {
                    var theme = AssetDatabase.LoadAssetAtPath<EnemyIntentTheme>(
                        AssetDatabase.GUIDToAssetPath(guid)
                    );
                    if (theme == null)
                        continue;
                    foreach (EnemyMoveType type in Enum.GetValues(typeof(EnemyMoveType)))
                    {
                        var (icon, _) = theme.GetVisual(type);
                        if (icon == null)
                            yield return new AuditIssue(
                                Severity.Warning,
                                $"[{theme.name}] intent '{type}' has no icon.",
                                theme
                            );
                    }
                }
            }
        }

        /// <summary>
        /// Validates enemy assets: each <see cref="EnemyData"/> has a name, portrait and at least one
        /// move; each <see cref="EnemyMoveData"/> has an intent description, effects (where applicable),
        /// and a minion assigned for SummonMinion moves.
        /// </summary>
        private sealed class EnemyCheck : IContentCheck
        {
            public string Category => "Enemies";

            public IEnumerable<AuditIssue> Run()
            {
                foreach (string guid in AssetDatabase.FindAssets("t:EnemyData"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                    if (enemy == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(enemy.EnemyName) || enemy.EnemyName == "Unknown Enemy")
                        yield return new AuditIssue(Severity.Warning, $"Enemy '{enemy.name}' has no display name.", enemy);
                    if (enemy.Portrait == null)
                        yield return new AuditIssue(Severity.Warning, $"Enemy '{enemy.name}' has no portrait.", enemy);
                    if (enemy.Moves == null || enemy.Moves.Count == 0)
                        yield return new AuditIssue(Severity.Error, $"Enemy '{enemy.name}' has no moves.", enemy);
                    else
                        for (int i = 0; i < enemy.Moves.Count; i++)
                            if (enemy.Moves[i] == null)
                                yield return new AuditIssue(Severity.Error, $"Enemy '{enemy.name}' move [{i}] is unassigned.", enemy);
                }

                foreach (string guid in AssetDatabase.FindAssets("t:EnemyMoveData"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var move = AssetDatabase.LoadAssetAtPath<EnemyMoveData>(path);
                    if (move == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(move.IntentDescription))
                        yield return new AuditIssue(Severity.Warning, $"Move '{move.name}' has no intent description.", move);

                    bool needsEffects =
                        move.MoveType != EnemyMoveType.Idle
                        && move.MoveType != EnemyMoveType.SummonMinion;
                    if (needsEffects && (move.Effects == null || move.Effects.Count == 0))
                        yield return new AuditIssue(Severity.Warning, $"Move '{move.name}' ({move.MoveType}) has no effects.", move);

                    if (move.MoveType == EnemyMoveType.SummonMinion && move.MinionToSummon == null)
                        yield return new AuditIssue(Severity.Error, $"Summon move '{move.name}' has no minion assigned.", move);
                }
            }
        }
    }
}
