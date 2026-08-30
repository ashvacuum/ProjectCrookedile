using System;
using System.Collections.Generic;
using System.IO;
using Crookedile.Data;
using Crookedile.Data.Enemy;
using Crookedile.Gameplay.Battle;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// One-shot builder for the eight enemies the Day 1-7 plan names but the project does not
    /// have. Run it once, check the results, then delete this file — it is scaffolding, and a
    /// generator that outlives its content becomes a second source of truth for numbers that
    /// have since been tuned by hand.
    ///
    /// Damage moves are authored, including the ones that scale with how much of the board
    /// shares a mood — Per X Source of Hostile/ReceptiveEnemyCount does that without new code.
    /// Defensive and shield moves carry no effects yet; their intent text says what to author.
    ///
    /// Menu: Crookedile → Generate → Day 1-7 Enemy Roster.
    /// </summary>
    public static class DayPlanRosterBuilder
    {
        private const string Root = "Assets/Data/Enemies";

        /// <summary>One move to author: which stance list it belongs to, and what it does.</summary>
        private readonly struct Move
        {
            public readonly Stance Stance;
            public readonly string Name;
            public readonly EnemyMoveType Type;
            public readonly string Intent;

            /// <summary>Opinion damage to the player. 0 means "needs authoring by hand".</summary>
            public readonly int Damage;

            /// <summary>
            /// Scales <see cref="Damage"/> by a board count — the mechanism behind every "scales
            /// with how many allies share my mood" move in the plan.
            /// </summary>
            public readonly EffectContextValue PerX;

            public Move(
                Stance stance,
                string name,
                EnemyMoveType type,
                string intent,
                int damage,
                EffectContextValue perX = EffectContextValue.None
            )
            {
                Stance = stance;
                Name = name;
                Type = type;
                Intent = intent;
                Damage = damage;
                PerX = perX;
            }
        }

        private enum Stance
        {
            Aggressive,
            Neutral,
            Receptive,
        }

        private readonly struct Enemy
        {
            public readonly string Name;
            public readonly int StartingHostility;
            public readonly int MaxHostility;
            public readonly int MinHostility;
            public readonly Move[] Moves;

            public Enemy(string name, int starting, int max, int min, Move[] moves)
            {
                Name = name;
                StartingHostility = starting;
                MaxHostility = max;
                MinHostility = min;
                Moves = moves;
            }
        }

        // Hostility ranges follow the existing roster: Spin Doctor starts at 0/max 4, Loyal
        // Partisan 2/5, Firebrand 2/15. Damage numbers match Partisan Grumble's 2 as the flat
        // baseline, so nothing here invents a scale the tuned content does not already use.
        private static Enemy[] Roster() =>
            new[]
            {
                new Enemy(
                    "Agitator",
                    2,
                    10,
                    -3,
                    new[]
                    {
                        new Move(Stance.Aggressive, "Rally", EnemyMoveType.RileOthers,
                            "Rallies the crowd: raises an ally's Hostility", 0),
                        new Move(Stance.Neutral, "Jab", EnemyMoveType.Attack,
                            "Jabs at your record", 2),
                        new Move(Stance.Receptive, "Placate", EnemyMoveType.Defend,
                            "Talks someone down", 0),
                    }
                ),
                new Enemy(
                    "Attacker",
                    2,
                    8,
                    -3,
                    new[]
                    {
                        new Move(Stance.Aggressive, "Haymaker", EnemyMoveType.Attack,
                            "Swings hard at your credibility", 5),
                        new Move(Stance.Neutral, "Strike", EnemyMoveType.Attack,
                            "Takes a shot at you", 3),
                        new Move(Stance.Receptive, "Reluctant Jab", EnemyMoveType.Attack,
                            "Half-hearted, and it shows", 1),
                    }
                ),
                new Enemy(
                    "Converter",
                    0,
                    6,
                    -5,
                    new[]
                    {
                        new Move(Stance.Aggressive, "Smear", EnemyMoveType.DebuffAttack,
                            "Smears you and digs in against persuasion", 2),
                        new Move(Stance.Neutral, "Sway", EnemyMoveType.Debuff,
                            "Works the room against you", 1),
                        // Echo-chamber bait: the more of them agree, the faster the meter slips.
                        new Move(Stance.Receptive, "Testimonial", EnemyMoveType.DefendOpinion,
                            "Vouches for you, and the room believes itself", 2,
                            EffectContextValue.ReceptiveEnemyCount),
                    }
                ),
                new Enemy(
                    "Bodyguard",
                    3,
                    8,
                    -2,
                    new[]
                    {
                        new Move(Stance.Aggressive, "Block", EnemyMoveType.Ward,
                            "Shields itself and an ally", 0),
                        new Move(Stance.Neutral, "Guard Up", EnemyMoveType.Defend,
                            "Shields itself", 0),
                        new Move(Stance.Receptive, "Stand Down", EnemyMoveType.Idle,
                            "Drops its guard entirely", 0),
                    }
                ),
                new Enemy(
                    "Loyalist",
                    3,
                    6,
                    -1, // Barely reachable: conversion-resistant by design.
                    new[]
                    {
                        new Move(Stance.Aggressive, "Dig In", EnemyMoveType.Defend,
                            "Digs in and hardens against persuasion", 0),
                        new Move(Stance.Neutral, "Hold Firm", EnemyMoveType.Defend,
                            "Holds the line", 0),
                        new Move(Stance.Receptive, "Grudging Nod", EnemyMoveType.Idle,
                            "Concedes a point, and hates it", 0),
                    }
                ),
                new Enemy(
                    "Pundit",
                    1,
                    6,
                    -4,
                    new[]
                    {
                        new Move(Stance.Aggressive, "Amplify", EnemyMoveType.RileOthers,
                            "Whips up the room — scales with how many are already angry", 2,
                            EffectContextValue.HostileEnemyCount),
                        new Move(Stance.Neutral, "Poll", EnemyMoveType.Idle,
                            "Takes the temperature of the room", 0),
                        new Move(Stance.Receptive, "Echo", EnemyMoveType.DefendOpinion,
                            "Amplifies the chorus — scales with how many agree", 2,
                            EffectContextValue.ReceptiveEnemyCount),
                    }
                ),
                new Enemy(
                    "Ardent Fan",
                    2,
                    12,
                    -3,
                    new[]
                    {
                        new Move(Stance.Aggressive, "Sulk", EnemyMoveType.Attack,
                            "Sulks, and hits softer for it", 1),
                        new Move(Stance.Neutral, "Strike", EnemyMoveType.Attack,
                            "Defends your honour, loudly", 3),
                        // Scales with the Receptive count on the board — same missing scan.
                        new Move(Stance.Receptive, "Outrage", EnemyMoveType.Attack,
                            "Furious on your behalf — worse the more of them agree with you", 2,
                            EffectContextValue.ReceptiveEnemyCount),
                    }
                ),
                new Enemy(
                    "The Incumbent",
                    4,
                    15,
                    -2,
                    new[]
                    {
                        new Move(Stance.Aggressive, "Photo Op", EnemyMoveType.Attack,
                            "Turns the room into a backdrop", 6),
                        new Move(Stance.Aggressive, "Smear Campaign", EnemyMoveType.Attack,
                            "Phase 3 — everything he has left, all at once", 9),
                        new Move(Stance.Aggressive, "Call In a Favor", EnemyMoveType.SummonMinion,
                            "Phase 2 — makes a call. Assign Loyalist as the minion.", 0),
                        new Move(Stance.Neutral, "Approval Rating", EnemyMoveType.RileOthers,
                            "Bends the room toward him — author with ShiftHostilityEffect on All Allies", 0),
                        new Move(Stance.Receptive, "Concede the Point", EnemyMoveType.Idle,
                            "Lets one land, and smiles", 0),
                    }
                ),
            };

        [MenuItem("Crookedile/Generate/Day 1-7 Enemy Roster")]
        private static void Generate()
        {
            var created = new List<string>();
            var skipped = new List<string>();

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var enemy in Roster())
                {
                    string folder = $"{Root}/{enemy.Name}";
                    if (AssetDatabase.LoadAssetAtPath<EnemyData>($"{folder}/{enemy.Name}.asset") != null)
                    {
                        // Never overwrite: re-running must not flatten hand-tuned numbers.
                        skipped.Add(enemy.Name);
                        continue;
                    }

                    Directory.CreateDirectory(folder);
                    Build(enemy, folder);
                    created.Add(enemy.Name);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[Day 1-7 roster] Created {created.Count}: {string.Join(", ", created)}\n"
                    + (skipped.Count > 0 ? $"Left alone (already exist): {string.Join(", ", skipped)}\n" : "")
                    + "Moves with no damage value carry no effects yet — see their intent text. "
                    + "Delete this script once the roster is tuned."
            );
        }

        private static void Build(Enemy enemy, string folder)
        {
            var aggressive = new List<EnemyMoveData>();
            var neutral = new List<EnemyMoveData>();
            var receptive = new List<EnemyMoveData>();

            foreach (var move in enemy.Moves)
            {
                var asset = BuildMove(move, $"{folder}/{enemy.Name} {move.Name}.asset");
                switch (move.Stance)
                {
                    case Stance.Aggressive:
                        aggressive.Add(asset);
                        break;
                    case Stance.Neutral:
                        neutral.Add(asset);
                        break;
                    default:
                        receptive.Add(asset);
                        break;
                }
            }

            var data = ScriptableObject.CreateInstance<EnemyData>();
            AssetDatabase.CreateAsset(data, $"{folder}/{enemy.Name}.asset");

            var so = new SerializedObject(data);
            so.FindProperty("_enemyName").stringValue = enemy.Name;
            so.FindProperty("_startingHostility").intValue = enemy.StartingHostility;
            so.FindProperty("_maxHostility").intValue = enemy.MaxHostility;
            so.FindProperty("_minHostility").intValue = enemy.MinHostility;
            Fill(so.FindProperty("_aggressiveMoves"), aggressive);
            Fill(so.FindProperty("_neutralMoves"), neutral);
            Fill(so.FindProperty("_receptiveMoves"), receptive);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static EnemyMoveData BuildMove(Move move, string path)
        {
            var asset = ScriptableObject.CreateInstance<EnemyMoveData>();
            AssetDatabase.CreateAsset(asset, path);

            var so = new SerializedObject(asset);
            so.FindProperty("_moveName").stringValue = move.Name;
            so.FindProperty("_moveType").enumValueIndex = (int)move.Type;
            so.FindProperty("_intentDescription").stringValue = move.Intent;

            var effects = so.FindProperty("_effects");
            effects.ClearArray();
            if (move.Damage > 0)
            {
                // ApplyOpinionEffect already targets Opponent by default, so only the amount
                // needs setting. Re-resolve first: the relative property does not exist until
                // the managed reference has been written.
                effects.arraySize = 1;
                effects.GetArrayElementAtIndex(0).managedReferenceValue = new ApplyOpinionEffect();
                so.ApplyModifiedPropertiesWithoutUndo();

                so = new SerializedObject(asset);
                var effect = so.FindProperty("_effects").GetArrayElementAtIndex(0);
                effect.FindPropertyRelative("_amount").intValue = move.Damage;

                // Per X Source turns the amount into "this much per matching enemy", which is
                // how every "scales with how many share my mood" move in the plan is expressed.
                if (move.PerX != EffectContextValue.None)
                    effect.FindPropertyRelative("_perXSource").enumValueIndex = (int)move.PerX;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static void Fill(SerializedProperty list, List<EnemyMoveData> moves)
        {
            list.ClearArray();
            list.arraySize = moves.Count;
            for (int i = 0; i < moves.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = moves[i];
        }
    }
}
