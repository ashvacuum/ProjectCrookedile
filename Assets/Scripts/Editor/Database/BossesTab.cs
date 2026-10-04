using System.Collections.Generic;
using Crookedile.Data.Boss;
using UnityEditor;
using UnityEngine;

namespace Crookedile.Editor.Database
{
    public sealed class BossesTab : ContentTab<BossData>
    {
        public override string Title => "Bosses";

        protected override IEnumerable<Column> BuildColumns()
        {
            yield return new Column("Rival", 180, b => b.DisplayName, b => b.DisplayName);
            yield return new Column(
                "Bundles",
                60,
                b => b.Bundles.Count.ToString(),
                b => b.Bundles.Count
            );
            yield return new Column(
                "Tree",
                160,
                b => b.Behavior != null ? b.Behavior.name : "Missing",
                b => b.Behavior != null ? b.Behavior.name : ""
            );
        }

        protected override IEnumerable<Issue> Audit(BossData boss)
        {
            foreach (var issue in boss.GetConfigurationIssues())
                yield return Issue.Error(issue);
        }

        protected override void DrawPreview(BossData boss)
        {
            EditorGUILayout.LabelField(boss.DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Shared asset: editing this boss or its moves affects every session that references them. Duplicate the boss and moves for an independent variant.",
                MessageType.Info
            );
            if (GUILayout.Button("Duplicate boss, tree and moves for a local variant"))
                Later(() => Crookedile.EditorTools.BossAuthoring.DuplicateBoss(boss));
            foreach (var bundle in boss.Bundles)
            {
                if (bundle == null)
                    continue;

                EditorGUILayout.LabelField(
                    $"{bundle.Name} — cooldown {bundle.CooldownTurns}",
                    EditorStyles.boldLabel
                );
                for (int i = 0; i < bundle.Moves.Count; i++)
                    if (bundle.Moves[i] != null)
                        EditorGUILayout.LabelField(
                            $"{i + 1}. {bundle.Moves[i].MoveName}: {bundle.Moves[i].Description}",
                            EditorStyles.wordWrappedLabel
                        );
            }
        }

        protected override IEnumerable<TabAction> Actions()
        {
            yield return new TabAction(
                "Create sample debate",
                Crookedile.EditorTools.BossAuthoring.CreateExample
            );
        }
    }
}
