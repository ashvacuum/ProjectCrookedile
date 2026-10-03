using System.Collections.Generic;
using System.Linq;
using Crookedile.Data.Battle;
using Crookedile.EditorTools;
using Crookedile.Gameplay.Battle;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Crookedile.Editor.Database
{
    /// <summary>
    /// Every status in the <see cref="StatusRegistry"/>: how it reads, its badge in the icon map,
    /// and which cards, enemies, moves, passives and allies use it. A status that content uses
    /// but the icon map can't draw shows up here first.
    /// </summary>
    public sealed class StatusesTab : ContentTab<StatusBehavior>
    {
        private StatusEffectIconMapSO _map;
        private ContentUsage _usage;

        public override string Title => "Statuses";

        protected override string DisplayName(StatusBehavior status) => status.DisplayName;

        protected override string SearchText(StatusBehavior status) => status.Id + " " + Describe(status);

        // The icon map holds every status's entry, so its inspector is where the icon is edited.
        protected override Object AssetOf(StatusBehavior status) => _map;

        protected override IEnumerable<StatusBehavior> Find() => StatusRegistry.All;

        protected override void OnReloaded(IReadOnlyList<StatusBehavior> all)
        {
            _map = FindAssets<StatusEffectIconMapSO>().FirstOrDefault();
            _usage = ContentUsage.Build();
        }

        protected override IEnumerable<Column> BuildColumns()
        {
            yield return new Column("Status", 140, null, s => s.DisplayName);
            yield return new Column("Id", 100, s => s.Id, s => s.Id);
            yield return new Column("Kind", 56, s => s.IsDebuff ? "Debuff" : "Buff", s => s.IsDebuff, s => s.IsDebuff ? new Color(0.6f, 0.25f, 0.2f) : new Color(0.25f, 0.5f, 0.3f));
            yield return new Column("Category", 96, s => s.Category.ToString(), s => s.Category);
            yield return new Column("Pacify", 46, s => s.CountsTowardPacify ? "yes" : "", s => s.CountsTowardPacify);
            yield return new Column("Icon", 40, s => HasIcon(s) ? "✓" : "—", s => HasIcon(s));
            yield return new Column("Used by", 56, s => UsedBy(s).Count == 0 ? "unused" : UsedBy(s).Count.ToString(), s => UsedBy(s).Count);
        }

        protected override IEnumerable<TabAction> Actions()
        {
            yield return new TabAction("Seed missing icon-map entries", StatusIconMapSeeder.Seed);
        }

        protected override IEnumerable<Issue> Audit(StatusBehavior status)
        {
            bool used = UsedBy(status).Count > 0;
            if (string.IsNullOrWhiteSpace(Describe(status)))
                yield return Issue.Error("Empty description.");
            if (_map == null)
            {
                yield return Issue.Warning("No StatusEffectIconMap asset exists.");
                yield break;
            }
            if (!_map.TryGet(status.Id, out var icon, out _, out var name, out _))
                yield return used
                    ? Issue.Warning("Used by content but has no icon-map entry: its badge can't draw. Seed missing entries.")
                    : Issue.Info("No icon-map entry.");
            else
            {
                if (icon == null)
                    yield return used ? Issue.Warning("Used by content but has no icon.") : Issue.Info("No icon.");
                if (string.IsNullOrEmpty(name))
                    yield return Issue.Info("No display name in the icon map.");
            }
            if (!used)
                yield return Issue.Info("Unused: no content asset applies or checks it.");
        }

        protected override void DrawPreview(StatusBehavior status)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                Sprite icon = null;
                Color color = Color.white;
                string name = "", description = "";
                _map?.TryGet(status.Id, out icon, out color, out name, out description);
                Picture(icon, 64);
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField($"{status.DisplayName}   ({status.Id})", EditorStyles.boldLabel);
                    Rect swatch = GUILayoutUtility.GetRect(40, 6, GUILayout.Width(40));
                    EditorGUI.DrawRect(swatch, color);
                    EditorGUILayout.LabelField(Describe(status), EditorStyles.wordWrappedLabel);
                    if (!string.IsNullOrEmpty(description))
                        EditorGUILayout.LabelField("Badge text: " + description, EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.LabelField(
                        $"{(status.IsDebuff ? "Debuff" : "Buff")} · {status.Category}{(status.CountsTowardPacify ? " · counts toward pacify" : "")}",
                        EditorStyles.miniLabel
                    );
                }
            }
            BuildingBlocksTab.UsedByList(UsedBy(status));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Icon map (edit this status's entry below)", EditorStyles.boldLabel);
        }

        protected override void DrawSummary(IReadOnlyList<StatusBehavior> all)
        {
            EditorGUILayout.LabelField($"{all.Count} statuses.", EditorStyles.miniLabel);
            EditorGUILayout.Space();
            Bar("Buffs", all.Count(s => !s.IsDebuff), all.Count);
            Bar("Debuffs", all.Count(s => s.IsDebuff), all.Count);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Used but missing an icon: {all.Count(s => UsedBy(s).Count > 0 && !HasIcon(s))}");
            EditorGUILayout.LabelField($"Unused: {all.Count(s => UsedBy(s).Count == 0)}");
        }

        private IReadOnlyList<Object> UsedBy(StatusBehavior status) =>
            _usage?.Of(status.GetType()) ?? new List<Object>();

        private bool HasIcon(StatusBehavior status) =>
            _map != null && _map.TryGet(status.Id, out var icon, out _) && icon != null;

        private static string Describe(StatusBehavior status) => ContentUsage.Safe(() => status.Describe(1));
    }
}
