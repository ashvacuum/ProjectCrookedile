using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Crookedile.Data;
using Crookedile.Data.Boss;
using Crookedile.Data.Campaign;
using Crookedile.Data.Enemy;
using Crookedile.Gameplay.Battle;
using Crookedile.UI.Battle;
using Opsive.BehaviorDesigner.Runtime;
using Opsive.BehaviorDesigner.Runtime.Tasks.Composites;
using Opsive.BehaviorDesigner.Runtime.Tasks.Events;
using Opsive.GraphDesigner.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Crookedile.EditorTools
{
    public static class BossAuthoring
    {
        public const string EXAMPLE_FOLDER = "Assets/Data/Bosses/Debate Prototype";
        public const string EXAMPLE_ENCOUNTER = EXAMPLE_FOLDER + "/Debate Prototype.asset";
        public const string PODIUM_PREFAB = "Assets/Prefabs/UI/BossPodium.prefab";

        public static void CreateExample()
        {
            if (AssetDatabase.LoadAssetAtPath<BattleEncounterData>(EXAMPLE_ENCOUNTER) != null)
            {
                EditorGUIUtility.PingObject(
                    AssetDatabase.LoadAssetAtPath<BattleEncounterData>(EXAMPLE_ENCOUNTER)
                );
                return;
            }

            Directory.CreateDirectory(EXAMPLE_FOLDER);
            AssetDatabase.Refresh();
            var rallyEffect = new RaiseTargetHostilityEffect();
            SetField(rallyEffect, "_target", TargetType.AllAllies);
            var swayEffect = new RaiseTargetHostilityEffect();
            SetField(swayEffect, "_target", TargetType.RandomReceptive);
            SetField(swayEffect, "_amount", 6);
            var wardEffect = new ApplyStatusBehaviorEffect();
            SetField(wardEffect, "_target", TargetType.RandomHostile);
            SetField(wardEffect, "_behavior", new WardedStatus());
            SetField(wardEffect, "_stacks", 1);
            SetField(wardEffect, "_duration", StatusDurationType.Permanent);
            var shieldEffect = new GainSupportEffect();
            SetField(shieldEffect, "_amount", 2);
            var pushEffect = new ApplyOpinionEffect();
            SetField(pushEffect, "_amount", 5);

            var rally = CreateMove(
                "Rally the Base",
                EnemyMoveType.RileOthers,
                "Audience gains 2 Hostility.",
                rallyEffect
            );
            var sway = CreateMove(
                "Take Back the Microphone",
                EnemyMoveType.RileOthers,
                "A revealed receptive audience member gains 6 Hostility.",
                swayEffect
            );
            var ward = CreateMove(
                "Protect the Spokesperson",
                EnemyMoveType.Ward,
                "A revealed hostile audience member gains 1 Ward.",
                wardEffect
            );
            var shield = CreateMove(
                "Stonewall",
                EnemyMoveType.DefendOpinion,
                "Gain 2 Denial.",
                shieldEffect
            );
            var push = CreateMove(
                "Closing Argument",
                EnemyMoveType.Attack,
                "Push Opinion down by 5 through Support.",
                pushEffect
            );

            var boss = ScriptableObject.CreateInstance<BossData>();
            SetField(boss, "_displayName", "The Rival Candidate");
            SetField(
                boss,
                "_bundles",
                new List<BossMoveBundle>
                {
                    Bundle("Opening statements", 0, rally, push),
                    Bundle("Win them back", 1, sway, ward, push),
                    Bundle("Protect the lead", 1, shield, ward, push),
                }
            );
            AssetDatabase.CreateAsset(boss, EXAMPLE_FOLDER + "/Rival Candidate.asset");
            var graph = CreatePlanningTree();
            SetField(boss, "_behavior", graph);
            EditorUtility.SetDirty(boss);

            var audience = AssetDatabase
                .FindAssets("t:EnemyData")
                .Select(g =>
                    AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(g))
                )
                .Where(e => e != null && e.Moves.Count > 0)
                .OrderBy(e => e.name)
                .ToList();
            var preferred = audience
                .Where(e =>
                    e.name.Contains("Askal") || e.name.Contains("Maya") || e.name.Contains("Tanod")
                )
                .ToList();
            if (preferred.Count == 0)
                preferred = audience;
            if (preferred.Count == 0)
                throw new InvalidOperationException(
                    "Create audience enemy assets before the sample debate."
                );

            var session = ScriptableObject.CreateInstance<BattleSession>();
            var round = new BattleSession.BattleRound
            {
                label = "Debate stage",
                boss = boss,
                maxTurns = 8,
            };
            for (int i = 0; i < 4; i++)
                round.enemies.Add(preferred[i % preferred.Count]);
            session.rounds.Add(round);
            AssetDatabase.CreateAsset(session, EXAMPLE_FOLDER + "/Debate Session.asset");
            var encounter = ScriptableObject.CreateInstance<BattleEncounterData>();
            SetField(encounter, "_session", session);
            SetField(encounter, "_isBoss", true);
            SetField(encounter, "_displayName", "The Debate Stage");
            SetField(
                encounter,
                "_blurb",
                "A rival candidate contests your audience. Read their full plan before answering."
            );
            AssetDatabase.CreateAsset(encounter, EXAMPLE_ENCOUNTER);
            SetField(encounter, "_id", AssetDatabase.AssetPathToGUID(EXAMPLE_ENCOUNTER));
            EditorUtility.SetDirty(encounter);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(encounter);
        }

        private static EnemyMoveData CreateMove(
            string name,
            EnemyMoveType type,
            string description,
            BattleEffect effect
        )
        {
            var move = ScriptableObject.CreateInstance<EnemyMoveData>();
            SetField(move, "_moveName", name);
            SetField(move, "_moveType", type);
            SetField(move, "_intentDescription", description);
            SetField(move, "_effects", new List<BattleEffect> { effect });
            AssetDatabase.CreateAsset(move, EXAMPLE_FOLDER + "/" + name + ".asset");
            return move;
        }

        private static BossMoveBundle Bundle(
            string name,
            int cooldown,
            params EnemyMoveData[] moves
        )
        {
            var bundle = new BossMoveBundle();
            SetField(bundle, "_name", name);
            SetField(bundle, "_cooldownTurns", cooldown);
            SetField(bundle, "_moves", moves.ToList());
            return bundle;
        }

        private static Subtree CreatePlanningTree()
        {
            var graph =
                ScriptableObject.CreateInstance<Opsive.BehaviorDesigner.Runtime.Wrappers.Subtree>();
            ITreeLogicNode[] nodes =
            {
                new Selector(),
                new Sequence(),
                new CheckBossBoard { Metric = BossBoardMetric.OpinionPercent, Threshold = 75 },
                new ChooseBossBundle { BundleName = "Protect the lead" },
                new Sequence(),
                new CheckBossBoard { Metric = BossBoardMetric.ReceptiveAudience, Threshold = 2 },
                new ChooseBossBundle { BundleName = "Win them back" },
                new ChooseBossBundle { BundleName = "Opening statements" },
            };
            ushort[] parents = { ushort.MaxValue, 0, 1, 1, 0, 4, 4, 0 };
            ushort[] siblings =
            {
                ushort.MaxValue,
                4,
                3,
                ushort.MaxValue,
                7,
                6,
                ushort.MaxValue,
                ushort.MaxValue,
            };
            graph.LogicNodeProperties = new LogicNodeProperties[nodes.Length];
            for (ushort i = 0; i < nodes.Length; i++)
            {
                ((IRuntimeNode)nodes[i]).Index = i;
                nodes[i].ParentIndex = parents[i];
                nodes[i].SiblingIndex = siblings[i];
                graph.LogicNodeProperties[i] = new LogicNodeProperties(
                    Guid.NewGuid(),
                    new Vector2(
                        i == 0 ? 0 : (i < 4 ? -260 : 120),
                        i == 0 ? 0
                            : i == 1 || i == 4 || i == 7 ? 130
                            : 260
                    ),
                    220,
                    new LogicNodeProperties.NodeData
                    {
                        ParentIndex = parents[i],
                        SiblingIndex = siblings[i],
                        IsParent = nodes[i] is IParentNode,
                    },
                    "",
                    null
                );
            }
            graph.TreeLogicNodes = nodes;
            graph.EventNodes = new IEventNode[] { new Start { ConnectedIndex = 0 } };
            graph.EventNodeProperties = new[]
            {
                new NodeProperties(Guid.NewGuid(), new Vector2(0, -120)),
            };
            graph.Serialize();
            AssetDatabase.CreateAsset(graph, EXAMPLE_FOLDER + "/Rival Planning.asset");
            return graph;
        }

        public static void DuplicateBoss(BossData source)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Independent boss variant",
                source.name + " Variant",
                "asset",
                "Choose a folder for this boss and its independent moves."
            );
            if (string.IsNullOrEmpty(path))
                return;

            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), path);
            var copy = AssetDatabase.LoadAssetAtPath<BossData>(path);
            var moves = new Dictionary<EnemyMoveData, EnemyMoveData>();
            foreach (var bundle in copy.Bundles)
            {
                var localMoves = new List<EnemyMoveData>();
                foreach (var move in bundle.Moves)
                {
                    if (!moves.TryGetValue(move, out var local))
                    {
                        string movePath = AssetDatabase.GenerateUniqueAssetPath(
                            Path.GetDirectoryName(path)
                                + "/"
                                + copy.name
                                + " "
                                + move.name
                                + ".asset"
                        );
                        AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(move), movePath);
                        local = AssetDatabase.LoadAssetAtPath<EnemyMoveData>(movePath);
                        moves.Add(move, local);
                    }
                    localMoves.Add(local);
                }
                SetField(bundle, "_moves", localMoves);
            }
            if (source.Behavior != null)
            {
                string treePath = AssetDatabase.GenerateUniqueAssetPath(
                    Path.GetDirectoryName(path) + "/" + copy.name + " Planning.asset"
                );
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source.Behavior), treePath);
                SetField(copy, "_behavior", AssetDatabase.LoadAssetAtPath<Subtree>(treePath));
            }
            EditorUtility.SetDirty(copy);
            AssetDatabase.SaveAssets();
            Selection.activeObject = copy;
        }

        public static void CreatePodium()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PODIUM_PREFAB) != null)
                return;

            var root = new GameObject("Boss Podium", typeof(RectTransform), typeof(BossPanel));
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = new Vector2(0.35f, 0.82f);
            rootRect.anchorMax = new Vector2(0.98f, 0.98f);
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            var content = new GameObject("Podium", typeof(RectTransform), typeof(Image));
            Stretch(content, root.transform);
            content.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.13f, 0.95f);
            var name = Text("Rival", content.transform, 20);
            Rect(name.rectTransform, new Vector2(0.1f, 0.72f), Vector2.one);
            var portrait = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portrait.transform.SetParent(content.transform, false);
            Rect((RectTransform)portrait.transform, Vector2.zero, new Vector2(0.09f, 1));
            portrait.GetComponent<Image>().preserveAspect = true;
            var intents = new EnemyIntentDisplay[3];
            var labels = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                var slot = new GameObject(
                    "Intent " + (i + 1),
                    typeof(RectTransform),
                    typeof(EnemyIntentDisplay)
                );
                slot.transform.SetParent(content.transform, false);
                Rect(
                    (RectTransform)slot.transform,
                    new Vector2(0.1f + i * 0.3f, 0),
                    new Vector2(0.39f + i * 0.3f, 0.72f)
                );
                labels[i] = Text("Order and target", slot.transform, 12);
                Rect(labels[i].rectTransform, new Vector2(0, 0.7f), Vector2.one);
                var badge = new GameObject("Move", typeof(RectTransform), typeof(Image));
                badge.transform.SetParent(slot.transform, false);
                Rect((RectTransform)badge.transform, Vector2.zero, new Vector2(1, 0.68f));
                badge.GetComponent<Image>().color = new Color(0.17f, 0.2f, 0.24f, 1);
                var title = Text("Move name", badge.transform, 15);
                Rect(title.rectTransform, new Vector2(0, 0.5f), Vector2.one);
                var description = Text("Effect", badge.transform, 12);
                Rect(description.rectTransform, Vector2.zero, new Vector2(1, 0.5f));
                intents[i] = slot.GetComponent<EnemyIntentDisplay>();
                SetField(intents[i], "intentPanel", badge);
                SetField(intents[i], "intentNameText", title);
                SetField(intents[i], "intentDescText", description);
                SetField(intents[i], "_showMoveName", true);
                SetField(intents[i], "_showFullDescription", true);
                SetField(intents[i], "_bobAmplitude", 0f);
            }
            var panel = root.GetComponent<BossPanel>();
            SetField(panel, "_content", content);
            SetField(panel, "_name", name);
            SetField(panel, "_portrait", portrait.GetComponent<Image>());
            SetField(panel, "_intents", intents);
            SetField(panel, "_orderLabels", labels);
            PrefabUtility.SaveAsPrefabAsset(root, PODIUM_PREFAB);
            Object.DestroyImmediate(root);
        }

        private static TMP_Text Text(string name, Transform parent, float size)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(GameObject child, Transform parent)
        {
            child.transform.SetParent(parent, false);
            Rect((RectTransform)child.transform, Vector2.zero, Vector2.one);
        }

        private static void Rect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = new Vector2(4, 4);
            rect.offsetMax = new Vector2(-4, -4);
        }

        public static void CreateExampleAndWireScene()
        {
            CreateExample();
            CreatePodium();
            var scene = EditorSceneManager.OpenScene(
                "Assets/Scenes/main.unity",
                OpenSceneMode.Single
            );
            var roots = scene.GetRootGameObjects();
            var ui = roots.SelectMany(r => r.GetComponentsInChildren<BattleUI>(true)).First();
            var canvas = roots
                .SelectMany(r => r.GetComponentsInChildren<Canvas>(true))
                .First(c => c.isRootCanvas);
            var existing = canvas.GetComponentInChildren<BossPanel>(true);
            var panel =
                existing != null
                    ? existing
                    : (
                        (GameObject)
                            PrefabUtility.InstantiatePrefab(
                                AssetDatabase.LoadAssetAtPath<GameObject>(PODIUM_PREFAB),
                                canvas.transform
                            )
                    ).GetComponent<BossPanel>();
            var serializedUI = new SerializedObject(ui);
            serializedUI.FindProperty("bossPanel").objectReferenceValue = panel;
            serializedUI.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetField(object owner, string name, object value)
        {
            for (var type = owner.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(
                    name,
                    System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.NonPublic
                );
                if (field == null)
                    continue;

                field.SetValue(owner, value);
                return;
            }
            throw new MissingFieldException(owner.GetType().Name, name);
        }
    }
}
