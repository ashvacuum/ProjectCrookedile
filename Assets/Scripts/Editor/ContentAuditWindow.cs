using System;
using System.Collections.Generic;
using System.Linq;
using Crookedile.Data;
using Crookedile.Data.Audio;
using Crookedile.Data.Battle;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Gameplay.Battle;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Content Hub — one window that browses ALL game content (cards, statuses, effects, enemies,
    /// intents, origins, audio/VFX, relics, reward config) and audits completeness. A Summary tab
    /// shows per-category health; each content tab lists every entry with a status badge and
    /// click-to-select. Read-only. Add an <see cref="IContentProvider"/> to <see cref="BuildProviders"/>
    /// and it shows up as a new tab automatically.
    ///
    /// Menu: Crookedile → Content Hub.
    /// </summary>
    public class ContentAuditWindow : EditorWindow
    {
        public enum Severity
        {
            Ok,
            Info,
            Warning,
            Error,
        }

        public readonly struct AuditIssue
        {
            public readonly Severity Severity;
            public readonly string Message;

            public AuditIssue(Severity severity, string message)
            {
                Severity = severity;
                Message = message;
            }
        }

        /// <summary>One entry in a category — a content item plus any problems found with it.</summary>
        public readonly struct Row
        {
            public readonly string Label;
            public readonly string Detail;
            public readonly UnityEngine.Object Context;
            public readonly List<AuditIssue> Issues;

            public Row(string label, string detail, UnityEngine.Object context, List<AuditIssue> issues)
            {
                Label = label;
                Detail = detail;
                Context = context;
                Issues = issues ?? new List<AuditIssue>();
            }

            public Severity Worst =>
                Issues.Count == 0 ? Severity.Ok : Issues.Max(i => i.Severity);
        }

        public interface IContentProvider
        {
            string Category { get; }
            IEnumerable<Row> Rows();
        }

        private List<(string category, List<Row> rows)> _data;
        private int _tab;
        private Vector2 _scroll;
        private bool _problemsOnly;

        [MenuItem("Crookedile/Content Hub")]
        public static void ShowWindow()
        {
            var win = GetWindow<ContentAuditWindow>("Content Hub");
            win.minSize = new Vector2(560, 400);
            win.Show();
        }

        private static List<IContentProvider> BuildProviders() =>
            new List<IContentProvider>
            {
                new ReadinessProvider(),
                new CardsProvider(),
                new StatusesProvider(),
                new StatusParityProvider(),
                new EffectsProvider(),
                new EnemiesProvider(),
                new EnemyMovesProvider(),
                new IntentsProvider(),
                new OriginsProvider(),
                new AudioVfxProvider(),
                new RelicsProvider(),
                new RewardProvider(),
            };

        private void Refresh()
        {
            _data = new List<(string, List<Row>)>();
            foreach (var p in BuildProviders())
            {
                List<Row> rows;
                try
                {
                    rows = p.Rows().ToList();
                }
                catch (Exception e)
                {
                    rows = new List<Row>
                    {
                        new Row("(provider error)", e.Message, null,
                            new List<AuditIssue> { new AuditIssue(Severity.Error, e.Message) }),
                    };
                }
                _data.Add((p.Category, rows));
            }
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    Refresh();
                GUILayout.FlexibleSpace();
                _problemsOnly = GUILayout.Toggle(_problemsOnly, "Problems only", EditorStyles.toolbarButton);
            }

            if (_data == null)
            {
                EditorGUILayout.HelpBox("Press Refresh to scan all content.", MessageType.Info);
                return;
            }

            // Tab bar: Summary + one per category.
            var tabs = new List<string> { "Summary" };
            tabs.AddRange(_data.Select(d => d.category));
            _tab = GUILayout.Toolbar(Mathf.Clamp(_tab, 0, tabs.Count - 1), tabs.ToArray());

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_tab == 0)
                DrawSummary();
            else
                DrawCategory(_data[_tab - 1]);
            EditorGUILayout.EndScrollView();
        }

        private void DrawSummary()
        {
            foreach (var (category, rows) in _data)
            {
                int errors = rows.Count(r => r.Worst == Severity.Error);
                int warns = rows.Count(r => r.Worst == Severity.Warning);
                string status = errors == 0 && warns == 0 ? "OK" : $"{errors} err, {warns} warn";
                var prev = GUI.color;
                GUI.color = errors > 0 ? Red : warns > 0 ? Amber : Color.white;
                EditorGUILayout.LabelField($"{category}", $"{rows.Count} entries  —  {status}");
                GUI.color = prev;
            }
        }

        private void DrawCategory((string category, List<Row> rows) data)
        {
            EditorGUILayout.LabelField($"{data.category}  ({data.rows.Count} entries)", EditorStyles.boldLabel);
            foreach (var row in data.rows)
            {
                if (_problemsOnly && row.Worst == Severity.Ok)
                    continue;

                using (new EditorGUILayout.HorizontalScope())
                {
                    var prev = GUI.color;
                    GUI.color = ColorFor(row.Worst);
                    EditorGUILayout.LabelField($"{Glyph(row.Worst)} {row.Label}", GUILayout.Width(240));
                    GUI.color = prev;
                    EditorGUILayout.LabelField(row.Detail, EditorStyles.miniLabel);
                    if (row.Context != null && GUILayout.Button("Select", GUILayout.Width(56)))
                    {
                        Selection.activeObject = row.Context;
                        EditorGUIUtility.PingObject(row.Context);
                    }
                }

                foreach (var issue in row.Issues)
                {
                    var prev = GUI.color;
                    GUI.color = ColorFor(issue.Severity);
                    EditorGUILayout.LabelField($"      • {issue.Message}", EditorStyles.wordWrappedMiniLabel);
                    GUI.color = prev;
                }
            }
        }

        private static readonly Color Red = new Color(1f, 0.6f, 0.6f);
        private static readonly Color Amber = new Color(1f, 0.85f, 0.5f);

        private static Color ColorFor(Severity s) =>
            s switch
            {
                Severity.Error => Red,
                Severity.Warning => Amber,
                _ => Color.white,
            };

        private static string Glyph(Severity s) =>
            s switch
            {
                Severity.Error => "x",
                Severity.Warning => "!",
                Severity.Info => "i",
                _ => "+",
            };

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private static List<T> LoadAll<T>() where T : UnityEngine.Object
        {
            return AssetDatabase
                .FindAssets("t:" + typeof(T).Name)
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(o => o != null)
                .ToList();
        }

        private static T LoadFirst<T>() where T : UnityEngine.Object => LoadAll<T>().FirstOrDefault();

        // -----------------------------------------------------------------
        // Providers
        // -----------------------------------------------------------------

        /// <summary>
        /// Asset-level readiness for a playable battle: each origin has a starter deck and a wired
        /// passive, and the project has enemies and at least one encounter. (Scene wiring — the
        /// BattleManager's passive array / intent theme / overlay text slots / BattleTestStarter
        /// session — can't be asset-scanned; verify those in the scene.)
        /// </summary>
        private sealed class ReadinessProvider : IContentProvider
        {
            public string Category => "Readiness";

            public IEnumerable<Row> Rows()
            {
                var cards = LoadAll<CardData>();
                var passives = LoadAll<OriginPassive>();

                foreach (OriginType origin in Enum.GetValues(typeof(OriginType)))
                {
                    string tag = origin.ToString().ToLowerInvariant();
                    int deck = cards.Count(c =>
                        c.IsStarterCard
                        && (c.Tags == null || c.Tags.Count == 0 || c.HasTag(tag) || c.HasTag("universal"))
                    );
                    var deckIssues = new List<AuditIssue>();
                    if (deck == 0)
                        deckIssues.Add(new AuditIssue(Severity.Error, "No starter cards (run the deck generator / tag cards)."));
                    else if (deck < 5)
                        deckIssues.Add(new AuditIssue(Severity.Warning, "Fewer than 5 starter cards — thin deck."));
                    yield return new Row($"{origin} starter deck", $"{deck} card(s)", null, deckIssues);

                    var p = passives.FirstOrDefault(x => x.Origin == origin);
                    var passIssues = new List<AuditIssue>();
                    if (p == null)
                        passIssues.Add(new AuditIssue(Severity.Warning, "No OriginPassive asset for this origin."));
                    else if (p.Passives == null || p.Passives.Count == 0)
                        passIssues.Add(new AuditIssue(Severity.Warning, "OriginPassive has no passives wired."));
                    yield return new Row($"{origin} passive", p != null ? p.name : "(none)", p, passIssues);
                }

                int enemies = LoadAll<EnemyData>().Count(e => e.Moves != null && e.Moves.Count > 0);
                var enemyIssues = new List<AuditIssue>();
                if (enemies == 0)
                    enemyIssues.Add(new AuditIssue(Severity.Error, "No enemies with moves — nothing to fight."));
                yield return new Row("Enemies with moves", $"{enemies}", null, enemyIssues);

                var sessions = LoadAll<BattleSession>();
                int playable = sessions.Count(s =>
                    s.rounds != null && s.rounds.Any(r => r != null && r.enemies != null && r.enemies.Count > 0)
                );
                var sessionIssues = new List<AuditIssue>();
                if (sessions.Count == 0)
                    sessionIssues.Add(new AuditIssue(Severity.Info, "No BattleSession — BattleTestStarter must use its own enemies list."));
                else if (playable == 0)
                    sessionIssues.Add(new AuditIssue(Severity.Warning, "BattleSession(s) exist but no round has enemies."));
                yield return new Row("Encounters (BattleSession)", $"{playable}/{sessions.Count} playable", null, sessionIssues);
            }
        }

        private sealed class CardsProvider : IContentProvider
        {
            public string Category => "Cards";

            public IEnumerable<Row> Rows()
            {
                foreach (var card in LoadAll<CardData>().OrderBy(c => c.name))
                {
                    var issues = new List<AuditIssue>();
                    bool junk = card.CardType == CardType.Status || card.CardType == CardType.Scandal;
                    if (!junk && (card.Effects == null || card.Effects.Count == 0))
                        issues.Add(new AuditIssue(Severity.Error, "No effects."));
                    if (card.IsInDevelopment)
                        issues.Add(new AuditIssue(Severity.Warning, "No artwork (in development)."));
                    if (card.NeedsConfiguration)
                        issues.Add(new AuditIssue(Severity.Warning, "Has leftover configuration notes."));
                    if (card.Costs == null || card.Costs.Count == 0)
                        issues.Add(new AuditIssue(Severity.Info, "No cost entry."));
                    yield return new Row(card.name, $"{card.CardType} / {card.Rarity}", card, issues);
                }
            }
        }

        private sealed class StatusesProvider : IContentProvider
        {
            public string Category => "Statuses";

            public IEnumerable<Row> Rows()
            {
                var map = LoadFirst<StatusEffectIconMapSO>();
                foreach (StatusEffectType type in Enum.GetValues(typeof(StatusEffectType)))
                {
                    var issues = new List<AuditIssue>();
                    if (string.IsNullOrWhiteSpace(new StatusEffect(type, 1).Description))
                        issues.Add(new AuditIssue(Severity.Error, "No description in StatusEffect switch."));
                    string detail = "no icon map";
                    if (map == null)
                        issues.Add(new AuditIssue(Severity.Warning, "No StatusEffectIconMap asset (run the seeder)."));
                    else if (!map.TryGet(type, out var icon, out _, out var name, out _))
                        issues.Add(new AuditIssue(Severity.Warning, "No icon-map entry (run the seeder)."));
                    else
                    {
                        detail = string.IsNullOrEmpty(name) ? "(no name)" : name;
                        if (icon == null)
                            issues.Add(new AuditIssue(Severity.Warning, "No icon."));
                        if (string.IsNullOrEmpty(name))
                            issues.Add(new AuditIssue(Severity.Info, "No display name."));
                    }
                    yield return new Row(type.ToString(), detail, map, issues);
                }
            }
        }

        /// <summary>
        /// Status migration parity: every legacy StatusEffectType enum value maps to a StatusBehavior
        /// (by id), and flags behaviors with no enum (new statuses). When this tab is all-green the
        /// new system is a faithful drop-in for the old — the green light to cut over and drop the enum.
        /// </summary>
        private sealed class StatusParityProvider : IContentProvider
        {
            public string Category => "Status parity";

            public IEnumerable<Row> Rows()
            {
                foreach (StatusEffectType type in Enum.GetValues(typeof(StatusEffectType)))
                {
                    var b = StatusBridge.ToBehavior(type);
                    var issues = new List<AuditIssue>();
                    if (b == null)
                        issues.Add(new AuditIssue(Severity.Error,
                            $"No StatusBehavior with Id '{type.ToString().ToLowerInvariant()}'."));
                    yield return new Row(type.ToString(), b != null ? b.GetType().Name : "— missing —", null, issues);
                }

                foreach (var behavior in StatusRegistry.All)
                {
                    if (!StatusBridge.TryToEnum(behavior, out _))
                        yield return new Row(
                            behavior.Id,
                            behavior.GetType().Name,
                            null,
                            new List<AuditIssue>
                            {
                                new AuditIssue(Severity.Info, "New status (no enum) — fine once the enum is dropped."),
                            });
                }
            }
        }

        private sealed class EffectsProvider : IContentProvider
        {
            public string Category => "Effects";

            public IEnumerable<Row> Rows()
            {
                foreach (var info in BattleEffectCatalog.All())
                {
                    var issues = new List<AuditIssue>();
                    if (!info.Serializable)
                        issues.Add(new AuditIssue(Severity.Warning, "Not [Serializable] — hidden from the picker."));
                    else if (string.IsNullOrWhiteSpace(info.Description))
                        issues.Add(new AuditIssue(Severity.Info, "Empty GetDescription."));
                    yield return new Row(info.DisplayName, info.Type.Name, null, issues);
                }
            }
        }

        private sealed class EnemiesProvider : IContentProvider
        {
            public string Category => "Enemies";

            public IEnumerable<Row> Rows()
            {
                foreach (var enemy in LoadAll<EnemyData>().OrderBy(e => e.name))
                {
                    var issues = new List<AuditIssue>();
                    if (string.IsNullOrWhiteSpace(enemy.EnemyName) || enemy.EnemyName == "Unknown Enemy")
                        issues.Add(new AuditIssue(Severity.Warning, "No display name."));
                    if (enemy.Portrait == null)
                        issues.Add(new AuditIssue(Severity.Warning, "No portrait."));
                    int moves = enemy.Moves?.Count ?? 0;
                    if (moves == 0)
                        issues.Add(new AuditIssue(Severity.Error, "No moves."));
                    yield return new Row(enemy.EnemyName ?? enemy.name, $"{moves} move(s)", enemy, issues);
                }
            }
        }

        private sealed class EnemyMovesProvider : IContentProvider
        {
            public string Category => "Enemy moves";

            public IEnumerable<Row> Rows()
            {
                foreach (var move in LoadAll<EnemyMoveData>().OrderBy(m => m.name))
                {
                    var issues = new List<AuditIssue>();
                    if (string.IsNullOrWhiteSpace(move.IntentDescription))
                        issues.Add(new AuditIssue(Severity.Warning, "No intent description."));
                    bool needsEffects =
                        move.MoveType != EnemyMoveType.Idle && move.MoveType != EnemyMoveType.SummonMinion;
                    if (needsEffects && (move.Effects == null || move.Effects.Count == 0))
                        issues.Add(new AuditIssue(Severity.Warning, "No effects."));
                    if (move.MoveType == EnemyMoveType.SummonMinion && move.MinionToSummon == null)
                        issues.Add(new AuditIssue(Severity.Error, "Summon move has no minion."));
                    yield return new Row(move.name, move.MoveType.ToString(), move, issues);
                }
            }
        }

        private sealed class IntentsProvider : IContentProvider
        {
            public string Category => "Intents";

            public IEnumerable<Row> Rows()
            {
                var theme = LoadFirst<EnemyIntentTheme>();
                foreach (EnemyMoveType type in Enum.GetValues(typeof(EnemyMoveType)))
                {
                    var issues = new List<AuditIssue>();
                    if (theme == null)
                        issues.Add(new AuditIssue(Severity.Warning, "No EnemyIntentTheme asset."));
                    else if (theme.GetVisual(type).icon == null)
                        issues.Add(new AuditIssue(Severity.Warning, "No icon in theme."));
                    yield return new Row(type.ToString(), "", theme, issues);
                }
            }
        }

        private sealed class OriginsProvider : IContentProvider
        {
            public string Category => "Origins";

            public IEnumerable<Row> Rows()
            {
                var db = LoadFirst<OriginDatabase>();
                foreach (OriginType origin in Enum.GetValues(typeof(OriginType)))
                {
                    var issues = new List<AuditIssue>();
                    string detail = "no DB";
                    if (db == null)
                        issues.Add(new AuditIssue(Severity.Warning, "No OriginDatabase asset (run the generator)."));
                    else if (!db.TryGet(origin, out var e))
                        issues.Add(new AuditIssue(Severity.Error, "No database entry."));
                    else
                    {
                        detail = $"{e.DisplayName} / {e.Resource}";
                        if (string.IsNullOrWhiteSpace(e.DisplayName))
                            issues.Add(new AuditIssue(Severity.Warning, "No display name."));
                        if (e.Passive == null)
                            issues.Add(new AuditIssue(Severity.Warning, "No starter passive linked."));
                    }
                    yield return new Row(origin.ToString(), detail, db, issues);
                }
            }
        }

        private sealed class AudioVfxProvider : IContentProvider
        {
            public string Category => "Audio / VFX";

            public IEnumerable<Row> Rows()
            {
                var map = LoadFirst<BattleSoundMap>();
                foreach (BattleAudioTrigger trigger in Enum.GetValues(typeof(BattleAudioTrigger)))
                {
                    var issues = new List<AuditIssue>();
                    string detail = "unmapped";
                    if (map == null)
                        issues.Add(new AuditIssue(Severity.Error, "No BattleSoundMap asset."));
                    else if (!map.TryGet(trigger, out var entry))
                        issues.Add(new AuditIssue(Severity.Warning, "Unmapped — no sound or VFX."));
                    else
                    {
                        detail = $"{(entry.Sound != null ? "sfx" : "—")} / {(entry.Visual != null ? "vfx" : "—")}";
                        if (entry.Sound == null && entry.Visual == null)
                            issues.Add(new AuditIssue(Severity.Info, "Mapped but empty."));
                    }
                    yield return new Row(trigger.ToString(), detail, map, issues);
                }
            }
        }

        private sealed class RelicsProvider : IContentProvider
        {
            public string Category => "Relics";

            public IEnumerable<Row> Rows()
            {
                var relics = LoadAll<RelicData>();
                if (relics.Count == 0)
                {
                    yield return new Row("(none)", "no relics authored yet (future layer)", null, new List<AuditIssue>());
                    yield break;
                }
                foreach (var relic in relics.OrderBy(r => r.name))
                {
                    var issues = new List<AuditIssue>();
                    if (string.IsNullOrWhiteSpace(relic.RelicName))
                        issues.Add(new AuditIssue(Severity.Warning, "No display name."));
                    if (relic.Icon == null)
                        issues.Add(new AuditIssue(Severity.Info, "No icon."));
                    if (relic.Passives == null || relic.Passives.Count == 0)
                        issues.Add(new AuditIssue(Severity.Warning, "No passives (does nothing)."));
                    yield return new Row(relic.RelicName ?? relic.name, relic.Rarity.ToString(), relic, issues);
                }
            }
        }

        private sealed class RewardProvider : IContentProvider
        {
            public string Category => "Reward config";

            public IEnumerable<Row> Rows()
            {
                var configs = LoadAll<RewardConfig>();
                if (configs.Count == 0)
                {
                    yield return new Row("(none)", "rewards use hardcoded weights in CardDatabase", null,
                        new List<AuditIssue> { new AuditIssue(Severity.Info, "No RewardConfig asset.") });
                    yield break;
                }
                foreach (var cfg in configs)
                {
                    var issues = new List<AuditIssue>();
                    if (!cfg.IsValid)
                        issues.Add(new AuditIssue(Severity.Error, "Weights sum to 0 or offer count < 1."));
                    yield return new Row(cfg.name,
                        $"B{cfg.BasicWeight}/E{cfg.EnhancedWeight}/R{cfg.RareWeight}  x{cfg.DefaultOfferCount}",
                        cfg, issues);
                }
            }
        }
    }
}
