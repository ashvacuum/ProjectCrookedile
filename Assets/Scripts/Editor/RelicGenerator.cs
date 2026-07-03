using System;
using System.Collections.Generic;
using System.Reflection;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Gameplay.Battle;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Generates the prototype relic set + the RelicDatabase asset so the relic runtime
    /// layer (RunState.Relics → PassiveResolver) has content to prove the pipeline.
    ///
    /// Same reflection pattern as EnemyRosterGenerator: sets live private serialized fields,
    /// re-runnable (deletes + recreates each asset). Icons are NOT set — author in Inspector.
    ///
    /// Menu: Crookedile → Generate → Relic Set
    /// Debug: Crookedile → Debug → Grant All Relics To Run (play mode, needs an active run)
    /// </summary>
    public static class RelicGenerator
    {
        private const string Folder = "Assets/Resources/Relics";
        private const string DatabasePath = Folder + "/RelicDatabase.asset";

        [MenuItem("Crookedile/Generate/Relic Set")]
        public static void Generate()
        {
            System.IO.Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();

            var relics = new List<RelicData>
            {
                // Basic — small, always-on openers.
                Relic(
                    "Campaign Pin",
                    "Start each debate with an extra card in hand.",
                    CardRarity.Basic,
                    Passive(
                        "Campaign Pin",
                        new BattleStartTrigger(),
                        oneShot: false,
                        Effect<DrawCardsEffect>(("_amount", 1))
                    )
                ),
                Relic(
                    "Teleprompter",
                    "Start each debate with 3 Support.",
                    CardRarity.Basic,
                    Passive(
                        "Teleprompter",
                        new BattleStartTrigger(),
                        oneShot: false,
                        Effect<GainBufferShieldEffect>(("_amount", 3))
                    )
                ),
                Relic(
                    "Epal Tarpaulin",
                    "Your face on every waiting shed. Start each debate with +2 Opinion.",
                    CardRarity.Basic,
                    Passive(
                        "Epal Tarpaulin",
                        new BattleStartTrigger(),
                        oneShot: false,
                        Effect<RaiseOpinionEffect>(("_amount", 2))
                    )
                ),
                // Enhanced — a felt spike, once per battle.
                Relic(
                    "Fixer's Rolodex",
                    "Gain 1 extra Action Point on your first turn each debate.",
                    CardRarity.Enhanced,
                    Passive(
                        "Fixer's Rolodex",
                        new TurnStartTrigger(),
                        oneShot: true,
                        Effect<GainActionPointsEffect>(("_amount", 1))
                    )
                ),
                // Rare — an engine, every turn.
                Relic(
                    "Golden Rooster",
                    "Gain 1 Support at the start of every turn.",
                    CardRarity.Rare,
                    Passive(
                        "Golden Rooster",
                        new TurnStartTrigger(),
                        oneShot: false,
                        Effect<GainBufferShieldEffect>(("_amount", 1))
                    )
                ),
            };

            var db = ScriptableObject.CreateInstance<RelicDatabase>();
            SetField(db, "_relics", relics);
            AssetDatabase.DeleteAsset(DatabasePath);
            AssetDatabase.CreateAsset(db, DatabasePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RelicGenerator] Generated {relics.Count} relics + database in {Folder}");
        }

        /// <summary>Grants every relic in the database to the active run — playtest shortcut.</summary>
        [MenuItem("Crookedile/Debug/Grant All Relics To Run")]
        public static void GrantAllRelics()
        {
            if (!Application.isPlaying || RunState.Current == null)
            {
                Debug.LogWarning("[RelicGenerator] Needs play mode with an active run.");
                return;
            }

            var db = AssetDatabase.LoadAssetAtPath<RelicDatabase>(DatabasePath);
            if (db == null)
            {
                Debug.LogWarning("[RelicGenerator] No RelicDatabase — run Generate first.");
                return;
            }

            foreach (var relic in db.Relics)
                RunState.Current.AddRelic(relic);
            Debug.Log(
                $"[RelicGenerator] Run now holds {RunState.Current.Relics.Count} relic(s) — takes effect next battle."
            );
        }

        // --- Assembly -----------------------------------------------------------

        private static RelicData Relic(
            string name,
            string description,
            CardRarity rarity,
            BattlePassive passive
        )
        {
            var relic = ScriptableObject.CreateInstance<RelicData>();
            SetField(relic, "_id", name.ToLowerInvariant().Replace(" ", "-").Replace("'", ""));
            SetField(relic, "_relicName", name);
            SetField(relic, "_description", description);
            SetField(relic, "_rarity", rarity);
            SetField(relic, "_passives", new List<BattlePassive> { passive });

            string path = $"{Folder}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(relic, path);
            return relic;
        }

        private static BattlePassive Passive(
            string name,
            PassiveTriggerBase trigger,
            bool oneShot,
            params BattleEffect[] effects
        )
        {
            var passive = new BattlePassive();
            SetField(passive, "_name", name);
            SetField(passive, "_trigger", trigger);
            SetField(passive, "_oneShot", oneShot);
            SetField(passive, "_effects", new List<BattleEffect>(effects));
            return passive;
        }

        private static BattleEffect Effect<T>(params (string field, object value)[] fields)
            where T : BattleEffect, new()
        {
            var effect = new T();
            foreach (var (field, value) in fields)
                SetField(effect, field, value);
            return effect;
        }

        // --- Reflection helper (walks the type hierarchy for private serialized fields) ---

        private static void SetField(object obj, string fieldName, object value)
        {
            Type t = obj.GetType();
            while (t != null)
            {
                FieldInfo f = t.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
                if (f != null)
                {
                    f.SetValue(obj, value);
                    return;
                }
                t = t.BaseType;
            }
            throw new Exception($"Field '{fieldName}' not found on {obj.GetType().Name}");
        }
    }
}
