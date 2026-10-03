using System.Collections.Generic;
using System.Linq;
using Crookedile.Data;
using Crookedile.EditorTools;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Crookedile.Editor.Database
{
    /// <summary>
    /// Every <see cref="BattleSession"/>: a fight (or test gauntlet) as rounds of enemies.
    /// Campaign battles should wrap exactly one round; chain fights with an event instead.
    /// </summary>
    public sealed class BattleSessionsTab : ContentTab<BattleSession>
    {
        private Dictionary<Object, List<Issue>> _issues;

        public override string Title => "Battle sessions";

        protected override string SearchText(BattleSession session) =>
            string.Join(" ", session.rounds.SelectMany(r => r.enemies).Where(e => e != null).Select(e => e.EnemyName));

        protected override void OnReloaded(IReadOnlyList<BattleSession> all) =>
            _issues = ProviderAudit.ByAsset(new ContentChecks.EncountersProvider());

        protected override IEnumerable<Column> BuildColumns()
        {
            yield return new Column("Session", 180, null, s => s.name);
            yield return new Column("Rounds", 50, s => s.RoundCount.ToString(), s => s.RoundCount);
            yield return new Column("Enemies", 56, s => EnemyCount(s).ToString(), s => EnemyCount(s));
            yield return new Column("Turns", 46, s => s.rounds.Count > 0 ? s.rounds[0].maxTurns.ToString() : "", s => s.rounds.Count > 0 ? s.rounds[0].maxTurns : 0);
        }

        protected override IEnumerable<Issue> Audit(BattleSession session) => ProviderAudit.For(_issues, session);

        protected override IEnumerable<TabAction> Actions()
        {
            yield return new TabAction("New session", CreateSession);
        }

        protected override void DrawPreview(BattleSession session)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                for (int i = 0; i < session.rounds.Count; i++)
                {
                    var round = session.rounds[i];
                    EditorGUILayout.LabelField($"Round {i + 1}: {round.label}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(
                        string.Join(", ", round.enemies.Select(e => e != null ? e.EnemyName : "(empty)")),
                        EditorStyles.wordWrappedLabel
                    );
                    EditorGUILayout.LabelField(
                        $"{round.maxTurns} turns · meter {round.startingOpinion}/{round.maxOpinion}",
                        EditorStyles.miniLabel
                    );
                }
        }

        protected override void DrawSummary(IReadOnlyList<BattleSession> all)
        {
            EditorGUILayout.LabelField($"{all.Count} sessions.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"More than one round (test gauntlets): {all.Count(s => s.RoundCount > 1)}");
        }

        private static int EnemyCount(BattleSession session) => session.rounds.Sum(r => r.enemies.Count(e => e != null));

        private static void CreateSession()
        {
            string path = EditorUtility.SaveFilePanelInProject("New battle session", "New Battle Session", "asset", "Where to save the session?");
            if (string.IsNullOrEmpty(path))
                return;
            var session = ScriptableObject.CreateInstance<BattleSession>();
            session.rounds.Add(new BattleSession.BattleRound());
            AssetDatabase.CreateAsset(session, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(session);
        }
    }
}
