using System;
using System.Collections.Generic;
using System.IO;
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
    /// Damage moves are authored. Moves that need systems which do not exist yet — the echo
    /// chamber's board-wide decay, Swing Voter's self-triggered flip, Pundit's board-majority
    /// scan — are created with the right name, type and intent but NO effects, so they read as
    /// unfinished rather than as working content that quietly does nothing.
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

            public Move(Stance stance, string name, EnemyMoveType type, string intent, int damage)
            {
                Stance = stance;
                Name = name;
                Type = type;
                Intent = intent;
                Damage = damage;
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
                        // Echo-chamber bait: healing the shared meter needs the board-wide system
                        // that does not exist yet.
                        new Move(Stance.Receptive, "Testimonial", EnemyMoveType.DefendOpinion,
                            "NEEDS AUTHORING — heals the shared Opinion meter", 0),
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
                        // Both scaling moves need the board-majority scan that does not exist.
                        new Move(Stance.Aggressive, "Amplify", EnemyMoveType.RileOthers,
                            "NEEDS AUTHORING — buffs all Hostile allies, scales with their count", 0),
                        new Move(Stance.Neutral, "Poll", EnemyMoveType.Idle,
                            "Takes the temperature of the room", 0),
                        new Move(Stance.Receptive, "Echo", EnemyMoveType.DefendOpinion,
                            "NEEDS AUTHORING — accelerates echo-chamber decay", 0),
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
                            "NEEDS AUTHORING — heavy multi-hit, scales with Receptive allies", 0),
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
                            "NEEDS AUTHORING — forces a state flip in his favour", 0),
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
                    + "Moves marked NEEDS AUTHORING have no effects — they need systems that do "
                    + "not exist yet. Delete this script once the roster is tuned."
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
                so.FindProperty("_effects")
                    .GetArrayElementAtIndex(0)
                    .FindPropertyRelative("_amount")
                    .intValue = move.Damage;
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
