using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Crookedile.Data;
using Crookedile.Data.Campaign;
using Crookedile.Data.Cards;
using Crookedile.Gameplay.Battle;
using Crookedile.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Crookedile.EditorTools.Playtest
{
    /// <summary>What to run. Survives the domain reload into Play Mode as JSON in SessionState.</summary>
    [Serializable]
    public class PlaytestConfig
    {
        public bool RunBattles = true;
        public bool RunCampaigns = true;
        public int SeedsPerMatchup = 5;
        public int RunsPerCampaignBot = 10;
        public int LocationsPerDay = 3;
        public string PoolPath;
        public List<string> Origins = new() { "FaithLeader", "NepoBaby", "Actor" };
        public List<string> BattleBots = new() { "Random", "Greedy", "Tempo", "Hoarder", "Scored" };
        public List<string> CampaignBots = new() { "RandomRun", "GreedyRun" };
        public int BaseSeed = 1000;
    }

    /// <summary>
    /// Plays every configured battle and campaign run back to back inside Play Mode, then writes
    /// the report and leaves Play Mode. Runs on a CoroutineHost in an empty scene.
    /// </summary>
    public static class PlaytestRunner
    {
        private const string PendingKey = "Crookedile.Playtest.Pending";
        private const string ConfigKey = "Crookedile.Playtest.Config";

        public static void Launch(PlaytestConfig config)
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Playtest Bot", "Leave Play Mode first.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            SessionState.SetString(ConfigKey, JsonUtility.ToJson(config));
            SessionState.SetBool(PendingKey, true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false))
                    return;
                SessionState.SetBool(PendingKey, false);
                var config = JsonUtility.FromJson<PlaytestConfig>(SessionState.GetString(ConfigKey, "{}"));
                CoroutineHost.Run("PlaytestRunner", Execute(config));
            };
        }

        private static IEnumerator Execute(PlaytestConfig config)
        {
            var started = DateTime.Now;
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
            Application.runInBackground = true;
            var previousLevel = GameLogger.GlobalLevel;
            GameLogger.SetGlobalLevel(LogLevel.Warning);

            var cards = Resources.Load<CardDatabase>("Databases/CardDatabase");
            var passives = Find<OriginPassive>().ToArray();
            var pool = string.IsNullOrEmpty(config.PoolPath)
                ? Find<EncounterPoolData>().FirstOrDefault()
                : AssetDatabase.LoadAssetAtPath<EncounterPoolData>(config.PoolPath);
            var origins = config.Origins.Select(o => (OriginType)Enum.Parse(typeof(OriginType), o)).ToList();
            var battleBots = AllBattleBots().Where(b => config.BattleBots.Contains(b.Name)).ToList();
            var campaignBots = AllCampaignBots().Where(b => config.CampaignBots.Contains(b.Name)).ToList();

            var battles = new List<BattleRecord>();
            var campaigns = new List<CampaignRecord>();
            bool cancelled = false;

            if (cards == null)
            {
                Debug.LogError("[Playtest] No CardDatabase at Resources/Databases/CardDatabase.");
                cancelled = true;
            }

            // ---- Standalone battles: every encounter × origin × bot × seed, starter decks ----
            if (!cancelled && config.RunBattles)
            {
                var encounters = Find<BattleEncounterData>()
                    .Where(e => e.Session != null && e.Session.RoundCount > 0)
                    .OrderBy(e => e.name)
                    .ToList();
                int total = encounters.Count * origins.Count * battleBots.Count * config.SeedsPerMatchup, done = 0;
                foreach (var encounter in encounters)
                foreach (var origin in origins)
                foreach (var bot in battleBots)
                for (int s = 0; s < config.SeedsPerMatchup && !cancelled; s++)
                {
                    cancelled = EditorUtility.DisplayCancelableProgressBar(
                        "Playtest Bot — battles",
                        $"{encounter.name} · {origin} · {bot.Name} · seed {s + 1}  ({done}/{total})",
                        (float)done / Math.Max(1, total)
                    );
                    RunState.Clear(); // no leftover allies or banked hostility between fights
                    var round = encounter.Session.GetRound(0);
                    var setup = new BattleSetup
                    {
                        playerOrigin = origin,
                        originDatabase = OriginDatabase.Shared,
                        playerDeck = cards.GetStarterDeck(origin),
                        enemies = round.enemies.Where(e => e != null).ToList(),
                        maxTurns = round.maxTurns > 0 ? round.maxTurns : (int?)null,
                        startingOpinion = round.startingOpinion,
                        maxOpinion = round.maxOpinion > 0 ? round.maxOpinion : 100,
                    };
                    var rec = new BattleRecord
                    {
                        Bot = bot.Name,
                        Origin = origin.ToString(),
                        Encounter = encounter.name,
                        Seed = config.BaseSeed + s,
                    };
                    battles.Add(rec);
                    if (setup.enemies.Count > 0)
                        yield return Safe(BattleHarness.Run(setup, bot, passives, rec.Seed, rec), ex => rec.Errors.Add("Harness crashed: " + Describe(ex)));
                    else
                        rec.Errors.Add("Round 0 has no enemies.");
                    done++;
                }
            }

            // ---- Whole campaign runs: campaign bot × origin × run ----
            if (!cancelled && config.RunCampaigns)
            {
                if (pool == null)
                    Debug.LogError("[Playtest] No EncounterPoolData found — skipping campaign runs.");
                else
                {
                    int total = campaignBots.Count * origins.Count * config.RunsPerCampaignBot, done = 0;
                    foreach (var bot in campaignBots)
                    foreach (var origin in origins)
                    for (int r = 0; r < config.RunsPerCampaignBot && !cancelled; r++)
                    {
                        cancelled = EditorUtility.DisplayCancelableProgressBar(
                            "Playtest Bot — campaign runs",
                            $"{bot.Name} · {origin} · run {r + 1}  ({done}/{total})",
                            (float)done / Math.Max(1, total)
                        );
                        var rec = new CampaignRecord
                        {
                            Id = $"{bot.Name}-{origin}-{r + 1}",
                            Bot = bot.Name,
                            Origin = origin.ToString(),
                            Seed = config.BaseSeed + 7919 * (r + 1),
                        };
                        campaigns.Add(rec);
                        yield return Safe(
                            CampaignHarness.Run(pool, cards, origin, bot, passives, rec.Seed, config.LocationsPerDay, rec, battles),
                            ex =>
                            {
                                rec.EndReason = "Error";
                                rec.Errors.Add("Harness crashed: " + Describe(ex));
                                RunState.Clear();
                            }
                        );
                        done++;
                    }
                }
            }

            EditorUtility.ClearProgressBar();
            GameLogger.SetGlobalLevel(previousLevel);
            RunState.Clear();

            string folder = Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? ".",
                "Playtests",
                DateTime.Now.ToString("yyyyMMdd-HHmmss") + (cancelled ? "-partial" : "")
            );
            var scored = battleBots.OfType<ScoredBot>().FirstOrDefault();
            string notes = scored != null && config.RunBattles ? $"Scored bot learned from: {scored.Table.Source}." : null;
            string report = PlaytestReport.Write(folder, battles, campaigns, pool, DateTime.Now - started, notes);
            Debug.Log($"[Playtest] {battles.Count} battles, {campaigns.Count} runs → {report}");
            EditorApplication.ExitPlaymode();
            EditorUtility.RevealInFinder(report);
        }

        /// <summary>
        /// Runs <paramref name="routine"/> (and every coroutine it yields) by hand, so an exception
        /// anywhere inside is caught and reported instead of silently killing the whole playtest.
        /// On failure every open iterator is disposed, which runs their <c>finally</c> cleanup.
        /// </summary>
        public static IEnumerator Safe(IEnumerator routine, Action<Exception> onError)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(routine);
            while (stack.Count > 0)
            {
                var top = stack.Peek();
                bool moved;
                object current = null;
                try
                {
                    moved = top.MoveNext();
                    if (moved)
                        current = top.Current;
                }
                catch (Exception ex)
                {
                    onError(ex);
                    while (stack.Count > 0)
                        (stack.Pop() as IDisposable)?.Dispose();
                    yield break;
                }
                if (!moved)
                    stack.Pop();
                else if (current is IEnumerator nested)
                    stack.Push(nested);
                else
                    yield return current;
            }
        }

        private static string Describe(Exception ex)
        {
            var frame = ex.StackTrace?.Split('\n').FirstOrDefault(l => l.Contains("Crookedile"))?.Trim();
            return $"{ex.GetType().Name}: {ex.Message}" + (frame != null ? $" — {frame}" : "");
        }

        public static IEnumerable<IBattleBot> AllBattleBots() =>
            new IBattleBot[] { new RandomBot(), new GreedyBot(), new TempoBot(), new HoarderBot(), new ScoredBot() };

        public static IEnumerable<ICampaignBot> AllCampaignBots() =>
            new ICampaignBot[] { new RandomCampaignBot(), new GreedyCampaignBot() };

        private static IEnumerable<T> Find<T>()
            where T : UnityEngine.Object =>
            AssetDatabase
                .FindAssets("t:" + typeof(T).Name)
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null);
    }

    /// <summary>Crookedile → Playtest Bot. Pick what to run; everything else is automatic.</summary>
    public class PlaytestBotWindow : EditorWindow
    {
        private PlaytestConfig _config = new();
        private EncounterPoolData _pool;

        [MenuItem("Crookedile/Playtest Bot")]
        private static void Open() => GetWindow<PlaytestBotWindow>("Playtest Bot");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Plays real battles and whole campaign runs with scripted bots, then writes "
                    + "Playtests/<time>/report.md ranking what looks broken. Runs in Play Mode in an "
                    + "empty scene; your open scene is offered for saving first. Cancel from the progress bar.",
                MessageType.Info
            );

            _config.RunBattles = EditorGUILayout.ToggleLeft("Standalone battles (every encounter × origin × bot)", _config.RunBattles);
            using (new EditorGUI.DisabledScope(!_config.RunBattles))
                _config.SeedsPerMatchup = EditorGUILayout.IntSlider("  Seeds per matchup", _config.SeedsPerMatchup, 1, 50);

            _config.RunCampaigns = EditorGUILayout.ToggleLeft("Whole campaign runs", _config.RunCampaigns);
            using (new EditorGUI.DisabledScope(!_config.RunCampaigns))
            {
                _config.RunsPerCampaignBot = EditorGUILayout.IntSlider("  Runs per bot × origin", _config.RunsPerCampaignBot, 1, 200);
                _config.LocationsPerDay = EditorGUILayout.IntSlider("  Locations per day", _config.LocationsPerDay, 1, 8);
                _pool = (EncounterPoolData)EditorGUILayout.ObjectField("  Pool", _pool, typeof(EncounterPoolData), false);
            }

            EditorGUILayout.Space();
            Toggles("Origins", Enum.GetNames(typeof(OriginType)), _config.Origins);
            Toggles("Battle bots", PlaytestRunner.AllBattleBots().Select(b => b.Name), _config.BattleBots);
            Toggles("Campaign bots", PlaytestRunner.AllCampaignBots().Select(b => b.Name), _config.CampaignBots);
            _config.BaseSeed = EditorGUILayout.IntField("Base seed", _config.BaseSeed);

            EditorGUILayout.Space();
            if (GUILayout.Button("Run playtest", GUILayout.Height(32)))
            {
                _config.PoolPath = _pool != null ? AssetDatabase.GetAssetPath(_pool) : null;
                PlaytestRunner.Launch(_config);
            }
        }

        private static void Toggles(string label, IEnumerable<string> names, List<string> selected)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
                foreach (var name in names)
                {
                    bool on = EditorGUILayout.ToggleLeft(name, selected.Contains(name), GUILayout.Width(110));
                    if (on && !selected.Contains(name))
                        selected.Add(name);
                    else if (!on)
                        selected.Remove(name);
                }
        }
    }
}
