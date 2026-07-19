using System;
using System.Collections.Generic;
using System.Linq;
using Crookedile.Data;
using Crookedile.Data.Audio;
using Crookedile.Data.Battle;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Data.Localization;
using Crookedile.Data.VFX;
using Crookedile.Gameplay.Battle;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Content Hub — one Odin window that browses ALL game content (cards, statuses, effects,
    /// enemies, intents, origins, audio/VFX, relics, reward config) and audits completeness. A
    /// searchable sidebar lists a Summary plus every category (with a warning/error icon); the
    /// editor pane draws each entry as a box with its issues as message boxes and click-to-select.
    /// Read-only. Add an <see cref="IContentProvider"/> to <see cref="BuildProviders"/> and it shows
    /// up as a new sidebar entry automatically.
    ///
    /// Menu: Crookedile → Content Hub.
    /// </summary>
    public class ContentAuditWindow : OdinMenuEditorWindow
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

            /// <summary>Optional sprite shown as a thumbnail at the left of the row.</summary>
            public readonly Sprite Thumbnail;

            public Row(
                string label,
                string detail,
                UnityEngine.Object context,
                List<AuditIssue> issues,
                Sprite thumbnail = null
            )
            {
                Label = label;
                Detail = detail;
                Context = context;
                Issues = issues ?? new List<AuditIssue>();
                Thumbnail = thumbnail;
            }

            public Severity Worst => Issues.Count == 0 ? Severity.Ok : Issues.Max(i => i.Severity);
        }

        public interface IContentProvider
        {
            string Category { get; }
            IEnumerable<Row> Rows();
        }

        private List<(string category, List<Row> rows)> _data;
        private bool _problemsOnly;

        [MenuItem("Crookedile/Content Hub")]
        public static void ShowWindow()
        {
            var win = GetWindow<ContentAuditWindow>("Content Hub");
            win.minSize = new Vector2(720, 480);
            win.Show();
        }

        private static List<IContentProvider> BuildProviders() =>
            new List<IContentProvider>
            {
                new ReadinessProvider(),
                new CardsProvider(),
                new StatusesProvider(),
                new EffectsProvider(),
                new EnemiesProvider(),
                new EnemyMovesProvider(),
                new EncountersProvider(),
                new SharedArtProvider(),
                new CardVisualsProvider(),
                new IntentsProvider(),
                new OriginsProvider(),
                new OriginPassivesProvider(),
                new AudioVfxProvider(),
                new AudioVfxEventsProvider(),
                new LocalizationProvider(),
                new RelicsProvider(),
                new RewardProvider(),
                new UIRefsAuditProvider(),
            };

        /// <summary>Re-scans every provider into <see cref="_data"/>. Cheap; safe to call on demand.</summary>
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
                        new Row(
                            "(provider error)",
                            e.Message,
                            null,
                            new List<AuditIssue> { new AuditIssue(Severity.Error, e.Message) }
                        ),
                    };
                }
                _data.Add((p.Category, rows));
            }
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            if (_data == null)
                Refresh();

            var tree = new OdinMenuTree(false);
            tree.Config.DrawSearchToolbar = true;
            tree.DefaultMenuStyle.IconSize = 18f;

            tree.Add("Summary", new SummaryView(this));

            foreach (var (category, rows) in _data)
            {
                int errors = rows.Count(r => r.Worst == Severity.Error);
                int warns = rows.Count(r => r.Worst == Severity.Warning);
                foreach (var item in tree.Add(category, new CategoryView(this, category, rows)))
                {
                    item.Icon =
                        errors > 0 ? EditorIcons.UnityErrorIcon
                        : warns > 0 ? EditorIcons.UnityWarningIcon
                        : null;
                }
            }
            return tree;
        }

        /// <summary>Top toolbar over the editor pane: Refresh + the "problems only" filter.</summary>
        protected override void OnBeginDrawEditors()
        {
            float toolbarHeight = MenuTree?.Config.SearchToolbarHeight ?? 22f;
            SirenixEditorGUI.BeginHorizontalToolbar(toolbarHeight);
            if (SirenixEditorGUI.ToolbarButton("Refresh"))
            {
                Refresh();
                ForceMenuTreeRebuild();
            }
            GUILayout.FlexibleSpace();
            _problemsOnly = SirenixEditorGUI.ToolbarToggle(_problemsOnly, "Problems only");
            SirenixEditorGUI.EndHorizontalToolbar();
        }

        private static readonly Color Red = new Color(1f, 0.5f, 0.5f);
        private static readonly Color Amber = new Color(1f, 0.82f, 0.45f);
        private static readonly Color Green = new Color(0.55f, 0.85f, 0.55f);

        private static Color ColorFor(Severity s) =>
            s switch
            {
                Severity.Error => Red,
                Severity.Warning => Amber,
                _ => Color.white,
            };

        private static MessageType MessageTypeFor(Severity s) =>
            s switch
            {
                Severity.Error => MessageType.Error,
                Severity.Warning => MessageType.Warning,
                Severity.Info => MessageType.Info,
                _ => MessageType.None,
            };

        // -----------------------------------------------------------------
        // Menu views — one drawable per sidebar entry. [OnInspectorGUI] lets Odin render
        // custom IMGUI inside its themed editor pane.
        // -----------------------------------------------------------------

        /// <summary>Landing page: per-category health, click a row to jump to that category.</summary>
        private sealed class SummaryView
        {
            private readonly ContentAuditWindow _owner;

            public SummaryView(ContentAuditWindow owner) => _owner = owner;

            [OnInspectorGUI]
            private void Draw()
            {
                SirenixEditorGUI.Title(
                    "Content Hub",
                    "Per-category health — click a row to open it",
                    TextAlignment.Left,
                    true
                );

                foreach (var (category, rows) in _owner._data)
                {
                    int errors = rows.Count(r => r.Worst == Severity.Error);
                    int warns = rows.Count(r => r.Worst == Severity.Warning);

                    SirenixEditorGUI.BeginBox();
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(category, EditorStyles.label, GUILayout.Width(170)))
                        _owner
                            .MenuTree.EnumerateTree()
                            .FirstOrDefault(i => i.Name == category)
                            ?.Select();
                    GUILayout.Label($"{rows.Count} entries", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();

                    var prev = GUI.color;
                    if (errors == 0 && warns == 0)
                    {
                        GUI.color = Green;
                        GUILayout.Label("OK", EditorStyles.boldLabel);
                    }
                    else
                    {
                        GUI.color = errors > 0 ? Red : Amber;
                        GUILayout.Label($"{errors} err   {warns} warn", EditorStyles.boldLabel);
                    }
                    GUI.color = prev;
                    EditorGUILayout.EndHorizontal();
                    SirenixEditorGUI.EndBox();
                }
            }
        }

        /// <summary>One category's rows, each an Odin box with its issues as message boxes.</summary>
        private sealed class CategoryView
        {
            private readonly ContentAuditWindow _owner;
            private readonly string _category;
            private readonly List<Row> _rows;

            public CategoryView(ContentAuditWindow owner, string category, List<Row> rows)
            {
                _owner = owner;
                _category = category;
                _rows = rows;
            }

            [OnInspectorGUI]
            private void Draw()
            {
                SirenixEditorGUI.Title(
                    _category,
                    $"{_rows.Count} entries",
                    TextAlignment.Left,
                    true
                );

                int shown = 0;
                foreach (var row in _rows)
                {
                    if (_owner._problemsOnly && row.Worst == Severity.Ok)
                        continue;
                    shown++;
                    DrawRow(row);
                }

                if (shown == 0)
                    SirenixEditorGUI.MessageBox(
                        _owner._problemsOnly
                            ? "No problems in this category."
                            : "Nothing to show.",
                        MessageType.Info
                    );
            }

            private static void DrawRow(Row row)
            {
                SirenixEditorGUI.BeginBox();

                SirenixEditorGUI.BeginBoxHeader();
                EditorGUILayout.BeginHorizontal();
                if (row.Thumbnail != null)
                    DrawSpriteThumb(row.Thumbnail, 28f);

                var prev = GUI.color;
                GUI.color = ColorFor(row.Worst);
                GUILayout.Label(row.Label, EditorStyles.boldLabel);
                GUI.color = prev;

                GUILayout.FlexibleSpace();
                if (!string.IsNullOrEmpty(row.Detail))
                    GUILayout.Label(row.Detail, EditorStyles.miniLabel);
                if (
                    row.Context != null
                    && GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(56))
                )
                {
                    Selection.activeObject = row.Context;
                    EditorGUIUtility.PingObject(row.Context);
                }
                EditorGUILayout.EndHorizontal();
                SirenixEditorGUI.EndBoxHeader();

                foreach (var issue in row.Issues)
                    SirenixEditorGUI.MessageBox(issue.Message, MessageTypeFor(issue.Severity));

                SirenixEditorGUI.EndBox();
            }
        }

        /// <summary>
        /// Draws a sprite at a fixed square size, honoring its atlas/sliced sub-rect so packed or
        /// sheet-sliced sprites show the right region (not the whole texture).
        /// </summary>
        private static void DrawSpriteThumb(Sprite sprite, float size)
        {
            var rect = GUILayoutUtility.GetRect(
                size,
                size,
                GUILayout.Width(size),
                GUILayout.Height(size)
            );
            if (Event.current.type != EventType.Repaint || sprite == null)
                return;
            var tex = sprite.texture;
            if (tex == null)
                return;
            var r = sprite.textureRect;
            var coords = new Rect(
                r.x / tex.width,
                r.y / tex.height,
                r.width / tex.width,
                r.height / tex.height
            );
            GUI.DrawTextureWithTexCoords(rect, tex, coords);
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private static List<T> LoadAll<T>()
            where T : UnityEngine.Object
        {
            return AssetDatabase
                .FindAssets("t:" + typeof(T).Name)
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(o => o != null)
                .ToList();
        }

        private static T LoadFirst<T>()
            where T : UnityEngine.Object => LoadAll<T>().FirstOrDefault();

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
                        && (
                            c.Tags == null
                            || c.Tags.Count == 0
                            || c.HasTag(tag)
                            || c.HasTag("universal")
                        )
                    );
                    var deckIssues = new List<AuditIssue>();
                    if (deck == 0)
                        deckIssues.Add(
                            new AuditIssue(
                                Severity.Error,
                                "No starter cards (run the deck generator / tag cards)."
                            )
                        );
                    else if (deck < 5)
                        deckIssues.Add(
                            new AuditIssue(
                                Severity.Warning,
                                "Fewer than 5 starter cards — thin deck."
                            )
                        );
                    yield return new Row(
                        $"{origin} starter deck",
                        $"{deck} card(s)",
                        null,
                        deckIssues
                    );

                    var p = passives.FirstOrDefault(x => x.Origin == origin);
                    var passIssues = new List<AuditIssue>();
                    if (p == null)
                        passIssues.Add(
                            new AuditIssue(
                                Severity.Warning,
                                "No OriginPassive asset for this origin."
                            )
                        );
                    else if (p.Passives == null || p.Passives.Count == 0)
                        passIssues.Add(
                            new AuditIssue(Severity.Warning, "OriginPassive has no passives wired.")
                        );
                    yield return new Row(
                        $"{origin} passive",
                        p != null ? p.name : "(none)",
                        p,
                        passIssues
                    );
                }

                int enemies = LoadAll<EnemyData>().Count(e => e.Moves != null && e.Moves.Count > 0);
                var enemyIssues = new List<AuditIssue>();
                if (enemies == 0)
                    enemyIssues.Add(
                        new AuditIssue(Severity.Error, "No enemies with moves — nothing to fight.")
                    );
                yield return new Row("Enemies with moves", $"{enemies}", null, enemyIssues);

                var sessions = LoadAll<BattleSession>();
                int playable = sessions.Count(s =>
                    s.rounds != null
                    && s.rounds.Any(r => r != null && r.enemies != null && r.enemies.Count > 0)
                );
                var sessionIssues = new List<AuditIssue>();
                if (sessions.Count == 0)
                    sessionIssues.Add(
                        new AuditIssue(
                            Severity.Info,
                            "No BattleSession — BattleTestStarter must use its own enemies list."
                        )
                    );
                else if (playable == 0)
                    sessionIssues.Add(
                        new AuditIssue(
                            Severity.Warning,
                            "BattleSession(s) exist but no round has enemies."
                        )
                    );
                yield return new Row(
                    "Encounters (BattleSession)",
                    $"{playable}/{sessions.Count} playable",
                    null,
                    sessionIssues
                );
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
                    bool junk =
                        card.CardType == CardType.Heckle || card.CardType == CardType.Scandal;
                    bool hasEffects = card.Effects != null && card.Effects.Count > 0;
                    bool hasPassives = card.Passives != null && card.Passives.Count > 0;
                    if (!junk && !hasEffects && !hasPassives)
                        issues.Add(new AuditIssue(Severity.Error, "No effects or passives."));
                    if (card.IsInDevelopment)
                        issues.Add(
                            new AuditIssue(Severity.Warning, "No artwork (in development).")
                        );
                    if (card.NeedsConfiguration)
                        issues.Add(
                            new AuditIssue(Severity.Warning, "Has leftover configuration notes.")
                        );
                    if (card.Costs == null || card.Costs.Count == 0)
                        issues.Add(new AuditIssue(Severity.Info, "No cost entry."));
                    yield return new Row(
                        card.name,
                        $"{card.CardType} / {card.Rarity}",
                        card,
                        issues,
                        card.Artwork
                    );
                }
            }
        }

        private sealed class StatusesProvider : IContentProvider
        {
            public string Category => "Statuses";

            public IEnumerable<Row> Rows()
            {
                var map = LoadFirst<StatusEffectIconMapSO>();
                foreach (StatusBehavior behavior in StatusRegistry.All.OrderBy(b => b.Id))
                {
                    var issues = new List<AuditIssue>();
                    if (string.IsNullOrWhiteSpace(behavior.Describe(1)))
                        issues.Add(new AuditIssue(Severity.Error, "Empty Describe()."));
                    string detail = "no icon map";
                    if (map == null)
                        issues.Add(
                            new AuditIssue(
                                Severity.Warning,
                                "No StatusEffectIconMap asset (run the seeder)."
                            )
                        );
                    else if (!map.TryGet(behavior.Id, out var icon, out _, out var name, out _))
                        issues.Add(
                            new AuditIssue(Severity.Warning, "No icon-map entry (run the seeder).")
                        );
                    else
                    {
                        detail = string.IsNullOrEmpty(name) ? "(no name)" : name;
                        if (icon == null)
                            issues.Add(new AuditIssue(Severity.Warning, "No icon."));
                        if (string.IsNullOrEmpty(name))
                            issues.Add(new AuditIssue(Severity.Info, "No display name."));
                    }
                    yield return new Row(behavior.DisplayName, detail, map, issues);
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
                        issues.Add(
                            new AuditIssue(
                                Severity.Warning,
                                "Not [Serializable] — hidden from the picker."
                            )
                        );
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
                    if (
                        string.IsNullOrWhiteSpace(enemy.EnemyName)
                        || enemy.EnemyName == "Unknown Enemy"
                    )
                        issues.Add(new AuditIssue(Severity.Warning, "No display name."));
                    if (enemy.Portrait == null)
                        issues.Add(new AuditIssue(Severity.Warning, "No portrait."));
                    int moves = enemy.Moves?.Count ?? 0;
                    if (moves == 0)
                        issues.Add(new AuditIssue(Severity.Error, "No moves."));
                    yield return new Row(
                        enemy.EnemyName ?? enemy.name,
                        $"{moves} move(s)",
                        enemy,
                        issues,
                        enemy.Portrait
                    );
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
                        move.MoveType != EnemyMoveType.Idle
                        && move.MoveType != EnemyMoveType.SummonMinion;
                    if (needsEffects && (move.Effects == null || move.Effects.Count == 0))
                        issues.Add(new AuditIssue(Severity.Warning, "No effects."));
                    if (move.MoveType == EnemyMoveType.SummonMinion && move.MinionToSummon == null)
                        issues.Add(new AuditIssue(Severity.Error, "Summon move has no minion."));
                    yield return new Row(move.name, move.MoveType.ToString(), move, issues);
                }
            }
        }

        /// <summary>
        /// Cross-content art duplication: collects every art reference across the project
        /// (card artwork, enemy portraits, status icons, intent icons, relic icons) and flags any
        /// sprite asset used by more than one item. Sharing art is sometimes intentional (a
        /// placeholder, or a deliberately reused icon) and sometimes a copy-paste mistake — this tab
        /// surfaces every case so you can decide. All-green means every flagged item has unique art.
        /// </summary>
        private sealed class SharedArtProvider : IContentProvider
        {
            public string Category => "Shared art";

            // One art reference: which sprite, on which content item, and a human label for it.
            private readonly struct ArtRef
            {
                public readonly Sprite Sprite;
                public readonly string Label;
                public readonly UnityEngine.Object Context;

                public ArtRef(Sprite sprite, string label, UnityEngine.Object context)
                {
                    Sprite = sprite;
                    Label = label;
                    Context = context;
                }
            }

            private static IEnumerable<ArtRef> Collect()
            {
                foreach (var card in LoadAll<CardData>())
                    if (card.Artwork != null)
                        yield return new ArtRef(card.Artwork, $"Card: {card.name}", card);

                foreach (var enemy in LoadAll<EnemyData>())
                    if (enemy.Portrait != null)
                        yield return new ArtRef(
                            enemy.Portrait,
                            $"Enemy: {enemy.EnemyName ?? enemy.name}",
                            enemy
                        );

                foreach (var relic in LoadAll<RelicData>())
                    if (relic.Icon != null)
                        yield return new ArtRef(
                            relic.Icon,
                            $"Relic: {relic.RelicName ?? relic.name}",
                            relic
                        );

                var iconMap = LoadFirst<StatusEffectIconMapSO>();
                if (iconMap != null)
                    foreach (StatusBehavior behavior in StatusRegistry.All)
                        if (iconMap.TryGet(behavior.Id, out var icon, out _) && icon != null)
                            yield return new ArtRef(
                                icon,
                                $"Status: {behavior.DisplayName}",
                                iconMap
                            );

                var theme = LoadFirst<EnemyIntentTheme>();
                if (theme != null)
                    foreach (EnemyMoveType type in Enum.GetValues(typeof(EnemyMoveType)))
                    {
                        var icon = theme.GetVisual(type).icon;
                        if (icon != null)
                            yield return new ArtRef(icon, $"Intent: {type}", theme);
                    }
            }

            public IEnumerable<Row> Rows()
            {
                // Group by the underlying sprite asset; only sprites shared by 2+ items matter.
                var groups = Collect()
                    .GroupBy(r => r.Sprite)
                    .Where(g => g.Count() > 1)
                    .OrderByDescending(g => g.Count());

                bool any = false;
                foreach (var group in groups)
                {
                    any = true;
                    var members = group.ToList();
                    string sprite = group.Key != null ? group.Key.name : "(missing)";
                    foreach (var m in members)
                    {
                        var others = members
                            .Where(o => o.Context != m.Context || o.Label != m.Label)
                            .Select(o => o.Label);
                        yield return new Row(
                            m.Label,
                            $"art '{sprite}'  —  shared by {members.Count}",
                            m.Context,
                            new List<AuditIssue>
                            {
                                new AuditIssue(
                                    Severity.Warning,
                                    $"Shares art '{sprite}' with: {string.Join(", ", others)}"
                                ),
                            },
                            group.Key
                        );
                    }
                }

                if (!any)
                    yield return new Row(
                        "(none)",
                        "every item has unique art",
                        null,
                        new List<AuditIssue>()
                    );
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
                        issues.Add(
                            new AuditIssue(
                                Severity.Warning,
                                "No OriginDatabase asset (run the generator)."
                            )
                        );
                    else if (!db.TryGet(origin, out var e))
                        issues.Add(new AuditIssue(Severity.Error, "No database entry."));
                    else
                    {
                        detail = $"{e.DisplayName} / {e.Resource}";
                        if (string.IsNullOrWhiteSpace(e.DisplayName))
                            issues.Add(new AuditIssue(Severity.Warning, "No display name."));
                        if (e.Passive == null)
                            issues.Add(
                                new AuditIssue(Severity.Warning, "No starter passive linked.")
                            );
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
                        detail =
                            $"{(entry.Sound != null ? "sfx" : "—")} / {(entry.Visual != null ? "vfx" : "—")}";
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
                    yield return new Row(
                        "(none)",
                        "no relics authored yet — run Crookedile → Generate → Relic Set",
                        null,
                        new List<AuditIssue>()
                    );
                    yield break;
                }

                var databases = LoadAll<RelicDatabase>();
                var seenIds = new Dictionary<string, string>();
                foreach (var relic in relics.OrderBy(r => r.name))
                {
                    var issues = new List<AuditIssue>();
                    if (string.IsNullOrWhiteSpace(relic.RelicName))
                        issues.Add(new AuditIssue(Severity.Warning, "No display name."));
                    if (relic.Icon == null)
                        issues.Add(new AuditIssue(Severity.Info, "No icon."));

                    // Id must be unique — RelicDatabase indexes by it (last one wins silently).
                    if (string.IsNullOrEmpty(relic.Id))
                        issues.Add(new AuditIssue(Severity.Error, "Empty id."));
                    else if (seenIds.TryGetValue(relic.Id, out var other))
                        issues.Add(
                            new AuditIssue(Severity.Error, $"Duplicate id (also on {other}).")
                        );
                    else
                        seenIds[relic.Id] = relic.name;

                    // Unregistered relics are invisible to acquisition (boss/event offers).
                    if (!databases.Any(db => db.Relics != null && db.Relics.Contains(relic)))
                        issues.Add(
                            new AuditIssue(
                                Severity.Warning,
                                "Not in any RelicDatabase — acquisition can't offer it."
                            )
                        );

                    if (relic.Passives == null || relic.Passives.Count == 0)
                    {
                        issues.Add(new AuditIssue(Severity.Warning, "No passives (does nothing)."));
                    }
                    else
                    {
                        // A passive without a trigger is never bucketed by PassiveResolver;
                        // without effects it fires into nothing. Both are silent at runtime.
                        foreach (var bp in relic.Passives)
                        {
                            if (bp == null)
                                continue;
                            if (bp.Trigger == null)
                                issues.Add(
                                    new AuditIssue(
                                        Severity.Warning,
                                        $"Passive '{bp.Name}' has no trigger (never fires)."
                                    )
                                );
                            if (bp.Effects == null || bp.Effects.Count == 0)
                                issues.Add(
                                    new AuditIssue(
                                        Severity.Warning,
                                        $"Passive '{bp.Name}' has no effects."
                                    )
                                );
                        }
                    }

                    yield return new Row(
                        relic.RelicName ?? relic.name,
                        relic.Rarity.ToString(),
                        relic,
                        issues,
                        relic.Icon
                    );
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
                    yield return new Row(
                        "(none)",
                        "rewards use hardcoded weights in CardDatabase",
                        null,
                        new List<AuditIssue>
                        {
                            new AuditIssue(Severity.Info, "No RewardConfig asset."),
                        }
                    );
                    yield break;
                }
                foreach (var cfg in configs)
                {
                    var issues = new List<AuditIssue>();
                    if (!cfg.IsValid)
                        issues.Add(
                            new AuditIssue(Severity.Error, "Weights sum to 0 or offer count < 1.")
                        );
                    yield return new Row(
                        cfg.name,
                        $"B{cfg.BasicWeight}/E{cfg.EnhancedWeight}/R{cfg.RareWeight}  x{cfg.DefaultOfferCount}",
                        cfg,
                        issues
                    );
                }
            }
        }

        /// <summary>
        /// Encounters: every BattleSession and its rounds. Flags empty sessions, rounds with no
        /// enemies, null enemy slots, and rounds over the 5-enemy display cap.
        /// </summary>
        private sealed class EncountersProvider : IContentProvider
        {
            public string Category => "Encounters";

            public IEnumerable<Row> Rows()
            {
                var sessions = LoadAll<BattleSession>();
                if (sessions.Count == 0)
                {
                    yield return new Row(
                        "(none)",
                        "no BattleSession assets — BattleTestStarter uses its own enemies list",
                        null,
                        new List<AuditIssue> { new AuditIssue(Severity.Info, "No encounters.") }
                    );
                    yield break;
                }

                foreach (var session in sessions.OrderBy(s => s.name))
                {
                    var issues = new List<AuditIssue>();
                    int rounds = session.RoundCount;
                    if (rounds == 0)
                        issues.Add(new AuditIssue(Severity.Error, "No rounds."));

                    for (int i = 0; i < rounds; i++)
                    {
                        var round = session.GetRound(i);
                        int enemies = round?.enemies?.Count ?? 0;
                        string label = string.IsNullOrWhiteSpace(round?.label)
                            ? $"Round {i + 1}"
                            : round.label;
                        if (enemies == 0)
                            issues.Add(
                                new AuditIssue(Severity.Error, $"{label}: no enemies.")
                            );
                        else
                        {
                            if (round.enemies.Any(e => e == null))
                                issues.Add(
                                    new AuditIssue(Severity.Warning, $"{label}: empty enemy slot.")
                                );
                            if (enemies > 5)
                                issues.Add(
                                    new AuditIssue(
                                        Severity.Warning,
                                        $"{label}: {enemies} enemies (display cap is 5)."
                                    )
                                );
                        }
                    }

                    yield return new Row(
                        session.name,
                        $"{rounds} round(s)",
                        session,
                        issues
                    );
                }
            }
        }

        /// <summary>
        /// Card visuals: the shared CardVisualSettings (every back/frame slot filled) and each
        /// CardVisualAtlas (texture set, mapping entries complete).
        /// </summary>
        private sealed class CardVisualsProvider : IContentProvider
        {
            public string Category => "Card visuals";

            public IEnumerable<Row> Rows()
            {
                var settings = LoadFirst<CardVisualSettings>();
                if (settings == null)
                {
                    yield return new Row(
                        "Card Visual Settings",
                        "(none)",
                        null,
                        new List<AuditIssue>
                        {
                            new AuditIssue(Severity.Warning, "No CardVisualSettings asset."),
                        }
                    );
                }
                else
                {
                    var issues = new List<AuditIssue>();
                    if (settings.DefaultCardBack == null)
                        issues.Add(new AuditIssue(Severity.Warning, "No default card back."));
                    foreach (
                        CardType type in new[]
                        {
                            CardType.Pressure,
                            CardType.Rhetoric,
                            CardType.Policy,
                            CardType.Heckle,
                            CardType.Scandal,
                        }
                    )
                        if (settings.GetFrameForType(type) == null)
                            issues.Add(
                                new AuditIssue(Severity.Info, $"No frame for {type} cards.")
                            );
                    foreach (
                        CardRarity rarity in new[]
                        {
                            CardRarity.Basic,
                            CardRarity.Enhanced,
                            CardRarity.Rare,
                        }
                    )
                        if (settings.GetFrameForRarity(rarity) == null)
                            issues.Add(
                                new AuditIssue(Severity.Info, $"No {rarity} rarity frame.")
                            );
                    yield return new Row(
                        "Card Visual Settings",
                        settings.name,
                        settings,
                        issues
                    );
                }

                foreach (var atlas in LoadAll<CardVisualAtlas>().OrderBy(a => a.name))
                {
                    var issues = new List<AuditIssue>();
                    if (atlas.AtlasTexture == null)
                        issues.Add(new AuditIssue(Severity.Warning, "No atlas texture assigned."));

                    var so = new SerializedObject(atlas);
                    var mappings = so.FindProperty("cardMappings");
                    int blankId = 0,
                        noRef = 0;
                    if (mappings != null)
                    {
                        for (int i = 0; i < mappings.arraySize; i++)
                        {
                            var entry = mappings.GetArrayElementAtIndex(i);
                            if (
                                string.IsNullOrWhiteSpace(
                                    entry.FindPropertyRelative("cardId")?.stringValue
                                )
                            )
                                blankId++;
                            if (
                                entry.FindPropertyRelative("cardReference")?.objectReferenceValue
                                == null
                            )
                                noRef++;
                        }
                    }
                    if (blankId > 0)
                        issues.Add(
                            new AuditIssue(Severity.Warning, $"{blankId} mapping(s) with blank cardId.")
                        );
                    if (noRef > 0)
                        issues.Add(
                            new AuditIssue(Severity.Info, $"{noRef} mapping(s) with no CardData reference.")
                        );

                    int count = mappings?.arraySize ?? 0;
                    yield return new Row(atlas.name, $"{count} mapping(s)", atlas, issues);
                }
            }
        }

        /// <summary>
        /// Origin passives: validates each OriginPassive asset is wired (has passives, each with a
        /// trigger and at least one effect), and surfaces the known Faith Leader timing caveat.
        /// </summary>
        private sealed class OriginPassivesProvider : IContentProvider
        {
            public string Category => "Origin passives";

            public IEnumerable<Row> Rows()
            {
                var passives = LoadAll<OriginPassive>();
                if (passives.Count == 0)
                {
                    yield return new Row(
                        "(none)",
                        "no OriginPassive assets",
                        null,
                        new List<AuditIssue> { new AuditIssue(Severity.Warning, "None found.") }
                    );
                    yield break;
                }

                foreach (var op in passives.OrderBy(p => p.Origin))
                {
                    var issues = new List<AuditIssue>();
                    var list = op.Passives;
                    if (list == null || list.Count == 0)
                        issues.Add(
                            new AuditIssue(Severity.Error, "No passives wired (does nothing).")
                        );
                    else
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            var bp = list[i];
                            if (bp == null)
                            {
                                issues.Add(
                                    new AuditIssue(Severity.Warning, $"Passive #{i + 1} is null.")
                                );
                                continue;
                            }
                            if (bp.Trigger == null)
                                issues.Add(
                                    new AuditIssue(
                                        Severity.Warning,
                                        $"Passive #{i + 1} has no trigger."
                                    )
                                );
                            if (bp.Effects == null || bp.Effects.Count == 0)
                                issues.Add(
                                    new AuditIssue(
                                        Severity.Warning,
                                        $"Passive #{i + 1} has no effects."
                                    )
                                );
                            // Known caveat: SupportGainedTrigger fires on ANY Support increase, so a
                            // turn-1 receptive/Ritual Support gain can consume a "first time" passive
                            // before the intended Support card.
                            if (
                                bp.Trigger != null
                                && bp.Trigger.GetType().Name == "SupportGainedTrigger"
                            )
                                issues.Add(
                                    new AuditIssue(
                                        Severity.Info,
                                        "SupportGainedTrigger fires on any Support increase — verify a "
                                            + "turn-1 Support gain can't consume a first-time passive early."
                                    )
                                );
                        }
                    }

                    yield return new Row(
                        string.IsNullOrWhiteSpace(op.PassiveName) ? op.name : op.PassiveName,
                        $"{op.Origin} — {list?.Count ?? 0} passive(s)",
                        op,
                        issues,
                        op.Icon
                    );
                }
            }
        }

        /// <summary>
        /// Audio/VFX event assets (distinct from the trigger-coverage check): AudioEvent and
        /// AudioClipData with no clip, and VFXEvent set to None (a no-op).
        /// </summary>
        private sealed class AudioVfxEventsProvider : IContentProvider
        {
            public string Category => "Audio / VFX events";

            public IEnumerable<Row> Rows()
            {
                foreach (var evt in LoadAll<AudioEvent>().OrderBy(a => a.name))
                {
                    var issues = new List<AuditIssue>();
                    var clip = new SerializedObject(evt)
                        .FindProperty("_clip")
                        ?.objectReferenceValue;
                    if (clip == null)
                        issues.Add(new AuditIssue(Severity.Warning, "No clip (no-op)."));
                    yield return new Row(evt.name, "AudioEvent", evt, issues);
                }

                foreach (var clip in LoadAll<AudioClipData>().OrderBy(a => a.name))
                {
                    var issues = new List<AuditIssue>();
                    if (clip.Clip == null)
                        issues.Add(new AuditIssue(Severity.Warning, "No clip."));
                    if (string.IsNullOrWhiteSpace(clip.ClipName))
                        issues.Add(new AuditIssue(Severity.Info, "No lookup name."));
                    yield return new Row(
                        string.IsNullOrWhiteSpace(clip.ClipName) ? clip.name : clip.ClipName,
                        "AudioClipData",
                        clip,
                        issues
                    );
                }

                foreach (var vfx in LoadAll<VFXEvent>().OrderBy(v => v.name))
                {
                    var issues = new List<AuditIssue>();
                    if (string.IsNullOrEmpty(vfx.AnimationStateName))
                        issues.Add(
                            new AuditIssue(Severity.Info, "Animation state is None (no visual).")
                        );
                    yield return new Row(vfx.name, "VFXEvent", vfx, issues);
                }
            }
        }

        /// <summary>
        /// Localization: every entry in the LocalizationData table — flags blank keys, duplicate
        /// keys, and missing English / Tagalog text.
        /// </summary>
        private sealed class LocalizationProvider : IContentProvider
        {
            public string Category => "Localization";

            public IEnumerable<Row> Rows()
            {
                var data = LoadFirst<LocalizationData>();
                if (data == null)
                {
                    yield return new Row(
                        "(none)",
                        "no LocalizationData asset",
                        null,
                        new List<AuditIssue> { new AuditIssue(Severity.Info, "None found.") }
                    );
                    yield break;
                }

                var strings = new SerializedObject(data).FindProperty("_strings");
                int count = strings?.arraySize ?? 0;
                var seen = new HashSet<string>();

                for (int i = 0; i < count; i++)
                {
                    var entry = strings.GetArrayElementAtIndex(i);
                    string key = entry.FindPropertyRelative("_key")?.stringValue ?? "";
                    string english = entry.FindPropertyRelative("_english")?.stringValue ?? "";
                    string tagalog = entry.FindPropertyRelative("_tagalog")?.stringValue ?? "";

                    var issues = new List<AuditIssue>();
                    if (string.IsNullOrWhiteSpace(key))
                        issues.Add(new AuditIssue(Severity.Error, "Blank key."));
                    else if (!seen.Add(key))
                        issues.Add(new AuditIssue(Severity.Error, "Duplicate key."));
                    if (string.IsNullOrWhiteSpace(english))
                        issues.Add(new AuditIssue(Severity.Warning, "No English text."));
                    if (string.IsNullOrWhiteSpace(tagalog))
                        issues.Add(new AuditIssue(Severity.Info, "No Tagalog text."));

                    yield return new Row(
                        string.IsNullOrWhiteSpace(key) ? $"(entry {i})" : key,
                        english.Length > 40 ? english.Substring(0, 40) + "…" : english,
                        data,
                        issues
                    );
                }

                if (count == 0)
                    yield return new Row(
                        data.name,
                        "no strings",
                        data,
                        new List<AuditIssue> { new AuditIssue(Severity.Info, "Empty table.") }
                    );
            }
        }
    }
}
