using System.Collections.Generic;
using System.Linq;
using Crookedile.Data.Campaign;
using Crookedile.EditorTools;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Crookedile.Editor.Database
{
    /// <summary>
    /// Every <see cref="EncounterData"/> (events and battles): where and when it can be entered,
    /// what it costs, and what its options do. Scheduling across days lives in the Encounter
    /// Designer, which the toolbar opens.
    /// </summary>
    public sealed class EncountersTab : ContentTab<EncounterData>
    {
        private enum Kind
        {
            Event,
            Battle,
            Boss,
        }

        private Kind? _kind;
        private Dictionary<Object, List<Issue>> _issues;
        private Crookedile.Data.OriginType _testOrigin;
        private int _testSeed = 1000;

        public override string Title => "Encounters";

        protected override string DisplayName(EncounterData encounter) =>
            string.IsNullOrWhiteSpace(encounter.DisplayName) ? $"({encounter.name})" : encounter.DisplayName;

        protected override string SearchText(EncounterData encounter) =>
            encounter.name + " " + encounter.Blurb + " " + (encounter is EventEncounterData e ? e.Body : "");

        protected override void OnReloaded(IReadOnlyList<EncounterData> all) =>
            _issues = ProviderAudit.ByAsset(new ContentChecks.CampaignEncountersProvider());

        protected override IEnumerable<Column> BuildColumns()
        {
            yield return new Column("Encounter", 170, null, e => DisplayName(e));
            yield return new Column("Kind", 54, e => KindOf(e).ToString(), e => KindOf(e), e => KindOf(e) == Kind.Event ? new Color(0.25f, 0.45f, 0.6f) : new Color(0.6f, 0.25f, 0.2f));
            yield return new Column("Time", 50, e => Duration(e.DurationMinutes), e => e.DurationMinutes);
            yield return new Column("District", 90, e => e.District != null ? e.District.name : "local", e => e.District != null ? e.District.name : "");
            yield return new Column("Window", 84, Window, e => e.OpeningMinute);
            yield return new Column("Weight", 48, e => e.DropWeight.ToString("0.##"), e => e.DropWeight);
            yield return new Column("Options", 52, e => e is EventEncounterData ev ? ev.Options.Count.ToString() : "", e => e is EventEncounterData ev ? ev.Options.Count : -1);
        }

        protected override bool DrawFilters() => EnumFilter("Kind", ref _kind);

        protected override bool PassesFilters(EncounterData encounter) => !_kind.HasValue || KindOf(encounter) == _kind.Value;

        protected override IEnumerable<Issue> Audit(EncounterData encounter) => ProviderAudit.For(_issues, encounter);

        protected override IEnumerable<TabAction> Actions()
        {
            yield return new TabAction("Open Encounter Designer", () => EditorApplication.ExecuteMenuItem("Crookedile/Encounter Designer"));
        }

        protected override void DrawPreview(EncounterData encounter)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"{DisplayName(encounter)}   ·   {KindOf(encounter)}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    $"{Duration(encounter.DurationMinutes)} · {(encounter.District != null ? encounter.District.name : "local")} · {Window(encounter)} · weight {encounter.DropWeight:0.##}",
                    EditorStyles.miniLabel
                );
                if (!string.IsNullOrWhiteSpace(encounter.Blurb))
                    EditorGUILayout.LabelField(encounter.Blurb, EditorStyles.wordWrappedLabel);

                if (encounter is EventEncounterData ev)
                {
                    if (!string.IsNullOrWhiteSpace(ev.Body))
                        EditorGUILayout.LabelField(ev.Body, EditorStyles.wordWrappedMiniLabel);
                    foreach (var option in ev.Options)
                    {
                        if (option == null)
                            continue;
                        EditorGUILayout.LabelField("▸ " + (string.IsNullOrWhiteSpace(option.Label) ? "(no label)" : option.Label), EditorStyles.boldLabel);
                        foreach (var req in option.Requirements.Where(r => r != null))
                            EditorGUILayout.LabelField("    needs: " + req.EditorSafeDescription(), EditorStyles.wordWrappedMiniLabel);
                        foreach (var outcome in option.Outcomes.Where(o => o != null))
                            EditorGUILayout.LabelField("    → " + outcome.EditorSafeDescription(), EditorStyles.wordWrappedMiniLabel);
                    }
                }
                else if (encounter is BattleEncounterData battle)
                {
                    _testOrigin = (Crookedile.Data.OriginType)
                        EditorGUILayout.EnumPopup("Playtest origin", _testOrigin);
                    _testSeed = EditorGUILayout.IntField("Playtest seed", _testSeed);
                    if (GUILayout.Button("Play selected encounter with Scored bot"))
                    {
                        var config = new Crookedile.EditorTools.Playtest.PlaytestConfig
                        {
                            RunBattles = true,
                            RunCampaigns = false,
                            SeedsPerMatchup = 1,
                            BaseSeed = _testSeed,
                            EncounterPath = AssetDatabase.GetAssetPath(battle),
                            Origins = new List<string> { _testOrigin.ToString() },
                            BattleBots = new List<string> { "Scored" },
                        };
                        Later(() => Crookedile.EditorTools.Playtest.PlaytestRunner.Launch(config));
                    }
                    var rounds = battle.Session != null ? battle.Session.rounds : null;
                    if (rounds == null || rounds.Count == 0)
                        EditorGUILayout.LabelField("No battle session.", EditorStyles.miniLabel);
                    else
                        foreach (var round in rounds)
                            EditorGUILayout.LabelField(
                                $"{round.Scenario}: {string.Join(", ", round.enemies.Where(x => x != null).Select(x => x.EnemyName))}   ({round.maxTurns} turns)",
                                EditorStyles.wordWrappedMiniLabel
                            );
                }
            }
        }

        protected override void DrawSummary(IReadOnlyList<EncounterData> all)
        {
            EditorGUILayout.LabelField($"{all.Count} encounters. Day scheduling is in the Encounter Designer.", EditorStyles.miniLabel);
            EditorGUILayout.Space();
            foreach (Kind kind in System.Enum.GetValues(typeof(Kind)))
                Bar(kind.ToString(), all.Count(e => KindOf(e) == kind), all.Count);
            EditorGUILayout.Space();
            foreach (var group in all.GroupBy(e => e.District != null ? e.District.name : "local").OrderBy(g => g.Key))
                Bar(group.Key, group.Count(), all.Count);
        }

        private static Kind KindOf(EncounterData encounter) =>
            encounter is BattleEncounterData battle ? (battle.IsBoss ? Kind.Boss : Kind.Battle) : Kind.Event;

        private static string Duration(int minutes) => minutes % 60 == 0 ? $"{minutes / 60}h" : $"{minutes / 60}h{minutes % 60:00}";

        private static string Window(EncounterData encounter) =>
            encounter.OpeningMinute == 0 && encounter.ClosingMinute == 0
                ? "all day"
                : $"{Clock(encounter.OpeningMinute)}–{Clock(encounter.ClosingMinute == 0 ? 1440 : encounter.ClosingMinute)}";

        private static string Clock(int minute) => $"{minute / 60:00}:{minute % 60:00}";
    }
}
