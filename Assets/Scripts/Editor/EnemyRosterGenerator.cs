using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Crookedile.Data;
using Crookedile.Data.Enemy;
using Crookedile.Gameplay.Battle;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// One-click generator for the prototype enemy roster (per docs/core-design.md §5).
    /// Builds 7 enemies covering the role x stance matrix, plus their move assets, in the current
    /// serialization format (fields set via live private fields so Unity serializes the
    /// [SerializeReference] effect/status lists correctly).
    ///
    /// Menu: Crookedile -> Generate -> Enemy Roster. Re-runnable (overwrites the Prototype/ folder).
    /// All numbers are placeholders -- tune in play. Portraits are left null; assign in the Inspector.
    ///
    /// The roster (role / stance / what it teaches):
    ///   1. Loyal Partisan   - Aggressive / Hostile    - the baseline villain (break through to climb)
    ///   2. Spin Doctor      - Defensive  / Neutral     - gains Denial; out-pace the shield
    ///   3. Heckler          - Disruptive / Neutral     - Silences you (no Rhetoric) -> stalls Faith Leader's stackers
    ///   4. Firebrand        - Amplifier  / Hostile     - Rallies: raises other enemies' hostility
    ///   5. The Bishop       - Protector  / Hostile     - Hardened (can't convert) + Cleanses your stacks off allies = FL hard counter
    ///   6. Swing Voter      - Passive    / Receptive   - the ally you can lose; teaches the Turncoat cascade
    ///   7. The Fixer        - Summoner   / Neutral      - summons a Partisan when none alive; tests board control
    /// </summary>
    public static class EnemyRosterGenerator
    {
        private const string Folder = "Assets/Resources/Enemies/Prototype";

        [MenuItem("Crookedile/Generate/Enemy Roster")]
        public static void Generate()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();

            // 1. Loyal Partisan -- the basic hostile attacker.
            var partisan = MakeEnemy(
                "Loyal Partisan",
                startingHostility: 2,
                maxHostility: 5,
                minHostility: -2,
                startingStatuses: null,
                aggressive: new[]
                {
                    Move("Partisan Condemn", EnemyMoveType.Attack, "Condemns: -6 Opinion", Condemn(6)),
                    Move("Partisan Tirade", EnemyMoveType.Attack, "Tirade: -4 Opinion", Condemn(4)),
                },
                neutral: new[]
                {
                    Move("Partisan Grumble", EnemyMoveType.Attack, "Mutters: -2 Opinion", Condemn(2)),
                },
                receptive: new[] { Idle("Partisan Waver", "Holds their tongue") }
            );

            // 2. Spin Doctor -- defensive; raises Denial to block the player's pushes.
            MakeEnemy(
                "Spin Doctor",
                startingHostility: 0,
                maxHostility: 4,
                minHostility: -2,
                startingStatuses: null,
                aggressive: new[]
                {
                    Move("Spin Smear", EnemyMoveType.DebuffAttack, "Smears: -4 Opinion, +4 Denial", Condemn(4), Denial(4)),
                },
                neutral: new[]
                {
                    Move("Spin Story", EnemyMoveType.DefendOpinion, "Spins the story: +6 Denial", Denial(6)),
                    Move("Spin Deflect", EnemyMoveType.Attack, "Deflects: -3 Opinion", Condemn(3)),
                },
                receptive: new[] { Idle("Spin Concede", "Concedes the point") }
            );

            // 3. Heckler -- disruptive; Silences the player (no Rhetoric) -> directly stalls Faith Leader stackers.
            MakeEnemy(
                "Heckler",
                startingHostility: 0,
                maxHostility: 4,
                minHostility: -2,
                startingStatuses: null,
                aggressive: new[]
                {
                    Move("Heckle Shout Down", EnemyMoveType.DebuffAttack, "Shouts you down: Silence + -3 Opinion", Silence(1), Condemn(3)),
                },
                neutral: new[]
                {
                    Move("Heckle Jeer", EnemyMoveType.Debuff, "Heckles: you can't play Rhetoric next turn", Silence(1)),
                },
                receptive: new[] { Idle("Heckle Quiet", "Falls quiet") }
            );

            // 4. Firebrand -- amplifier; rallies the room (raises other enemies' hostility).
            MakeEnemy(
                "Firebrand",
                startingHostility: 2,
                maxHostility: 6,
                minHostility: -1,
                startingStatuses: null,
                aggressive: new[]
                {
                    Move("Firebrand Rally", EnemyMoveType.RileOthers, "Rallies: +2 Hostility to other enemies", Rile(2)),
                    Move("Firebrand Incite", EnemyMoveType.Attack, "Incites: -4 Opinion", Condemn(4)),
                },
                neutral: new[]
                {
                    Move("Firebrand Provoke", EnemyMoveType.RileOthers, "Provokes: +1 Hostility to other enemies", Rile(1)),
                },
                receptive: new[] { Idle("Firebrand Sulk", "Sulks quietly") }
            );

            // 5. The Bishop -- protector + Hardened; Cleanses your pacify stacks off allies. Faith Leader hard counter.
            MakeEnemy(
                "The Bishop",
                startingHostility: 1,
                maxHostility: 5,
                minHostility: 0, // Hardened anyway, but clamp out the receptive range for clarity
                startingStatuses: new List<StartingStatusEntry> { Starting(new HardenedStatus(), 1, StatusDurationType.Permanent) },
                aggressive: new[]
                {
                    Move("Bishop Absolve", EnemyMoveType.Buff, "Absolves allies: removes your statuses from them", CleanseAllies()),
                    Move("Bishop Sermonize", EnemyMoveType.Attack, "Sermonizes: -4 Opinion", Condemn(4)),
                },
                neutral: new[]
                {
                    Move("Bishop Bless", EnemyMoveType.Buff, "Blesses allies: removes your statuses from them", CleanseAllies()),
                },
                receptive: null
            );

            // 6. Swing Voter -- starts receptive; teaches the Turncoat cascade when riled hostile.
            MakeEnemy(
                "Swing Voter",
                startingHostility: -2,
                maxHostility: 4,
                minHostility: -4,
                startingStatuses: null,
                aggressive: new[]
                {
                    // After a Turncoat flip the cascade amplifies this -- they "knew your strategy".
                    Move("Swing Betrayed", EnemyMoveType.Attack, "Feels betrayed: -5 Opinion", Condemn(5)),
                },
                neutral: new[]
                {
                    Move("Swing Reconsider", EnemyMoveType.Attack, "Reconsiders: -2 Opinion", Condemn(2)),
                },
                receptive: new[] { Idle("Swing Nod", "Nods along (could turn on you if provoked)") }
            );

            // 7. The Fixer -- summoner; calls in a Loyal Partisan when none are alive.
            MakeEnemy(
                "The Fixer",
                startingHostility: 0,
                maxHostility: 4,
                minHostility: -2,
                startingStatuses: null,
                aggressive: new[]
                {
                    Move("Fixer Pull Strings", EnemyMoveType.Attack, "Pulls strings: -4 Opinion", Condemn(4)),
                },
                neutral: new[]
                {
                    SummonMove("Fixer Call Muscle", "Calls in a Loyal Partisan", partisan, 1),
                    Move("Fixer Arrange", EnemyMoveType.Attack, "Arranges: -3 Opinion", Condemn(3)),
                },
                receptive: new[] { Idle("Fixer Wait", "Waits and watches") }
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[EnemyRosterGenerator] Generated 7 enemies + moves in {Folder}");
        }

        // --- Assembly -----------------------------------------------------------

        private static EnemyData MakeEnemy(
            string name,
            int startingHostility,
            int maxHostility,
            int minHostility,
            List<StartingStatusEntry> startingStatuses,
            EnemyMoveData[] aggressive,
            EnemyMoveData[] neutral,
            EnemyMoveData[] receptive
        )
        {
            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            SetField(enemy, "_enemyName", name);
            SetField(enemy, "_startingHostility", startingHostility);
            SetField(enemy, "_maxHostility", maxHostility);
            SetField(enemy, "_minHostility", minHostility);
            SetField(enemy, "_movePattern", EnemyMovePattern.Sequential);
            SetField(enemy, "_startingEffects", startingStatuses ?? new List<StartingStatusEntry>());
            SetField(enemy, "_aggressiveMoves", new List<EnemyMoveData>(aggressive ?? Array.Empty<EnemyMoveData>()));
            SetField(enemy, "_neutralMoves", new List<EnemyMoveData>(neutral ?? Array.Empty<EnemyMoveData>()));
            SetField(enemy, "_receptiveMoves", new List<EnemyMoveData>(receptive ?? Array.Empty<EnemyMoveData>()));

            string path = $"{Folder}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(enemy, path);
            return enemy;
        }

        private static EnemyMoveData Move(
            string name,
            EnemyMoveType type,
            string intent,
            params BattleEffect[] effects
        )
        {
            var move = ScriptableObject.CreateInstance<EnemyMoveData>();
            SetField(move, "_moveName", name);
            SetField(move, "_moveType", type);
            SetField(move, "_intentDescription", intent);
            SetField(move, "_effects", new List<BattleEffect>(effects));

            string path = $"{Folder}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(move, path);
            return move;
        }

        private static EnemyMoveData Idle(string name, string intent) =>
            Move(name, EnemyMoveType.Idle, intent); // no effects

        private static EnemyMoveData SummonMove(string name, string intent, EnemyData minion, int count)
        {
            var move = ScriptableObject.CreateInstance<EnemyMoveData>();
            SetField(move, "_moveName", name);
            SetField(move, "_moveType", EnemyMoveType.SummonMinion);
            SetField(move, "_intentDescription", intent);
            SetField(move, "_effects", new List<BattleEffect>());
            SetField(move, "_minionToSummon", minion);
            SetField(move, "_minionCount", count);
            SetField(move, "_condition", EnemyMoveCondition.OnlyIfNoMinionsAlive);

            string path = $"{Folder}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(move, path);
            return move;
        }

        // --- Effect builders ----------------------------------------------------

        private static BattleEffect Condemn(int amount)
        {
            var e = new ApplyPressureEffect();
            SetField(e, "_amount", amount);
            SetField(e, "_amountSource", EffectContextValue.FixedAmount);
            return e; // enemy caster -> pressures the player (absorbed by Support)
        }

        private static BattleEffect Denial(int amount)
        {
            var e = new GainBufferShieldEffect();
            SetField(e, "_amount", amount);
            SetField(e, "_amountSource", EffectContextValue.FixedAmount);
            return e; // enemy caster -> gains Denial
        }

        private static BattleEffect Rile(int amount)
        {
            var e = new RaiseAlliesHostilityEffect();
            SetField(e, "_amount", amount);
            return e;
        }

        private static BattleEffect Silence(int stacks)
        {
            var e = new ApplyStatusBehaviorEffect();
            SetField(e, "_target", TargetType.Opponent); // enemy -> player
            SetField(e, "_behavior", new SilencedStatus());
            SetField(e, "_stacks", stacks);
            SetField(e, "_duration", StatusDurationType.DecreasePerTurn);
            return e;
        }

        private static BattleEffect CleanseAllies()
        {
            var e = new CleanseStatusEffect();
            SetField(e, "_target", TargetType.AllAllies); // enemy -> all living enemies
            SetField(e, "_mode", CleanseStatusEffect.CleanseMode.AllDebuffs);
            return e;
        }

        private static StartingStatusEntry Starting(StatusBehavior behavior, int stacks, StatusDurationType duration)
        {
            var entry = new StartingStatusEntry();
            SetField(entry, "_behavior", behavior);
            SetField(entry, "_stacks", stacks);
            SetField(entry, "_duration", duration);
            return entry;
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
