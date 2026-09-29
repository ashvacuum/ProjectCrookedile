using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Campaign;
using Crookedile.Data.Cards;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.UI.Campaign
{
    /// <summary>Drives campaign visits and encounter chains; RunState preserves the clock and district across scene loads.</summary>
    [Debuggable("Campaign", LogLevel.Info)]
    public class CampaignFlow : MonoBehaviour
    {
        #region Inspector
        [Header("Content")]
        [Tooltip("Pool the day's locations are drawn from.")]
        [SerializeField]
        private EncounterPoolData _pool;

        [Header("Debug run (used only when entering this scene with no active run)")]
        [SerializeField]
        private OriginType _debugOrigin = OriginType.FaithLeader;

        [Tooltip("Locations offered per day.")]
        [Min(1)]
        [SerializeField]
        private int _locationsPerDay = 3;

        [Tooltip("Hours available each day.")]
        [Min(1)]
        [SerializeField]
        private int _maxHours = RunState.DEFAULT_MAX_HOURS;

        [Tooltip("Campaign seed. 0 = random each run.")]
        [SerializeField]
        private int _debugSeed;

        #endregion

        #region Runtime state
        /// <summary>Non-null while an event overlay is open. The map stays visible behind it.</summary>
        private EventEncounterData _openEvent;

        /// <summary>Result text of the option just chosen, shown before returning to the map.</summary>
        private string _pendingResultText;

        private Vector2 _scroll;

        #endregion

        #region Lifecycle
        private void Start()
        {
            EnsureRunState();
            RunState.Current.ConfigureTravel(_pool != null ? _pool.Travel : null);
            ResolveChainOrRefresh();
        }

        /// <summary>
        /// Creates a debug run when the scene is entered directly (pressing Play here), mirroring
        /// <c>BattleTestStarter</c>. A run arriving from a battle already has one and is left alone.
        /// </summary>
        private void EnsureRunState()
        {
            if (RunState.Current != null)
                return;

            var db = Resources.Load<CardDatabase>("Databases/CardDatabase");
            List<CardData> deck =
                db != null ? db.GetStarterDeck(_debugOrigin) : new List<CardData>();
            if (deck.Count == 0)
                GameLogger.LogWarning(
                    "Campaign",
                    "Starter deck came back empty — check CardDatabase is populated (Refresh Database).",
                    this
                );

            RunState.Create(
                _debugOrigin,
                deck,
                battleQueue: null,
                isCampaignRun: true,
                maxHours: _maxHours,
                seed: _debugSeed
            );
            RunState.Current.ConfigureTravel(_pool != null ? _pool.Travel : null);
            GameLogger.LogInfo(
                "Campaign",
                $"Debug campaign run created — origin {_debugOrigin}, seed {RunState.Current.Seed}, {deck.Count} cards.",
                this
            );
        }

        #endregion

        #region Flow
        /// <summary>
        /// Entry point on every return to the map. If the encounter just resolved chained
        /// forward, resolve that instead of re-rendering — this is what makes battle → event →
        /// battle sequences work without a multi-round BattleSession.
        /// </summary>
        private void ResolveChainOrRefresh()
        {
            var state = RunState.Current;
            if (state?.NextEncounter != null)
            {
                var chained = state.NextEncounter;
                state.ClearNextEncounter();
                GameLogger.LogInfo("Campaign", $"Chaining into '{chained.name}'.", this);
                Enter(chained, chargeHours: false); // the chain is a consequence, not a choice
                return;
            }
            RefreshLocations();
        }

        /// <summary>
        /// The single rebuild entry point. Called on load, after an event closes, and after a
        /// day ends. Draws once per day and stores the result on <see cref="RunState"/>, so
        /// returning from a battle restores the same map rather than re-rolling it.
        /// </summary>
        private void RefreshLocations()
        {
            var state = RunState.Current;
            if (state == null || _pool == null)
                return;
            if (state.TodaysLocationsDay == state.Day)
                return;

            state.SetTodaysLocations(
                state.Day,
                _pool.DrawForDay(
                    state.Day,
                    _locationsPerDay,
                    state.Seed,
                    state.VisitedLocationIds,
                    state // evaluates dependency gates and weight boosts
                )
            );

            GameLogger.LogInfo(
                "Campaign",
                $"Day {state.Day}: drew {state.TodaysLocations.Count} location(s) from '{_pool.name}'.",
                this
            );
        }

        /// <summary>
        /// Commits to an encounter: marks it visited, spends Hours, and dispatches on its type.
        /// Battles leave the scene; events open a panel over the map.
        ///
        /// <paramref name="chargeHours"/> only governs the time cost. "It happened" is a separate
        /// question from "it cost time": a chained encounter is free but still visited, or
        /// OncePerRun entries reached by a chain get redrawn on a later day and
        /// HasVisitedEncounter requirements gated on them never fire.
        /// </summary>
        private void Enter(EncounterData encounter, bool chargeHours = true)
        {
            var state = RunState.Current;
            if (state == null || encounter == null)
                return;

            if (!(encounter is EventEncounterData) && !(encounter is BattleEncounterData))
            {
                GameLogger.LogWarning(
                    "Campaign",
                    $"Unsupported encounter '{encounter.name}'.",
                    this
                );
                return;
            }

            if (encounter is BattleEncounterData invalidBattle && invalidBattle.Session == null)
            {
                GameLogger.LogWarning(
                    "Campaign",
                    $"'{encounter.name}' has no battle session.",
                    this
                );
                return;
            }

            if (chargeHours)
            {
                if (!state.TryVisit(encounter))
                {
                    GameLogger.LogWarning(
                        "Campaign",
                        $"Cannot enter '{encounter.name}': {state.PlanVisit(encounter).BlockedReason}",
                        this
                    );
                    return;
                }
            }
            else
            {
                state.MarkVisited(encounter.ID);
                state.RemoveTodaysLocation(encounter);
            }

            switch (encounter)
            {
                case BattleEncounterData battle:
                    StartBattle(battle);
                    break;

                case EventEncounterData evt:
                    _openEvent = evt;
                    _pendingResultText = null;
                    break;

                default:
                    GameLogger.LogWarning(
                        "Campaign",
                        $"'{encounter.name}' is a {encounter.GetType().Name}, which has no handler yet — skipping.",
                        this
                    );
                    ResolveChainOrRefresh();
                    break;
            }
        }

        private void StartBattle(BattleEncounterData battle)
        {
            var state = RunState.Current;
            if (battle.Session == null)
            {
                GameLogger.LogWarning(
                    "Campaign",
                    $"Battle encounter '{battle.name}' has no BattleSession — cannot start it.",
                    this
                );
                return;
            }

            state.StartEncounter(battle.Session.BuildBattleQueue());
            state.SetPendingBattle(battle);
            GameLogger.LogInfo("Campaign", $"Entering battle '{battle.name}'.", this);
            SceneLoader.Instance?.LoadScene("main");
        }

        /// <summary>Applies a chosen option, then holds on its result text before closing.</summary>
        private void ChooseOption(EventOption option)
        {
            // Re-checked here, not just in the view: an outcome applied from a locked option is
            // silent and unrecoverable, and the view is the easy thing to get wrong later.
            if (!option.IsAvailable(RunState.Current))
            {
                GameLogger.LogWarning(
                    "Campaign",
                    $"Blocked locked option '{option.Label}' — needs {option.DescribeRequirements()}.",
                    this
                );
                return;
            }

            option.Apply(RunState.Current);
            _pendingResultText = string.IsNullOrEmpty(option.ResultText)
                ? "(no result text authored)"
                : option.ResultText;
            GameLogger.LogInfo(
                "Campaign",
                $"Chose '{option.Label}' — {option.DescribeOutcomes()}",
                this
            );
        }

        /// <summary>
        /// Closes the event overlay. If the chosen option carried a <c>GoToEncounterOutcome</c>
        /// it already wrote <c>RunState.NextEncounter</c>, which
        /// <see cref="ResolveChainOrRefresh"/> picks up; otherwise this returns to the map.
        /// </summary>
        private void CloseEvent()
        {
            _openEvent = null;
            _pendingResultText = null;
            ResolveChainOrRefresh();
        }

        private void EndDay()
        {
            RunState.Current?.AdvanceDay();
            GameLogger.LogInfo("Campaign", $"Day advanced to {RunState.Current?.Day}.", this);
            RefreshLocations();
        }

        #endregion

        #region Debug view
        // ponytail: IMGUI playtest view; replace with isometric map hotspots when the production UI is ready.
        private void OnGUI()
        {
            var state = RunState.Current;
            GUILayout.BeginArea(new Rect(20f, 20f, Screen.width - 40f, Screen.height - 40f));

            if (state == null)
            {
                GUILayout.Label("Run ended.", GUI.skin.box);
                if (GUILayout.Button("Start a new run", GUILayout.Height(30f)))
                {
                    EnsureRunState();
                    RefreshLocations();
                }
                GUILayout.EndArea();
                return;
            }

            GUILayout.Label(
                $"Day {state.Day}   {CampaignTravelData.FormatTime(state.ClockMinute)}   "
                    + $"Time left {state.MinutesRemaining / 60}h {state.MinutesRemaining % 60}m   "
                    + $"District: {(state.CurrentDistrict != null ? state.CurrentDistrict.DisplayName : "Local")}   "
                    + $"Funds {state.Funds}   Credibility {state.Credibility}   "
                    + $"Deck {state.Deck.Count}   Allies {state.Allies.Count}   Seed {state.Seed}",
                GUI.skin.box
            );

            _scroll = GUILayout.BeginScrollView(_scroll);

            // A card pick owed to the player takes priority over everything: it was raised by an
            // outcome that has already applied, so the map/event underneath is mid-resolution.
            if (state.PendingCardChoice != null)
                DrawCardChoice(state.PendingCardChoice);
            else if (_openEvent != null)
                DrawEvent();
            else
                DrawMap(state);

            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void DrawMap(RunState state)
        {
            if (_pool == null)
            {
                GUILayout.Label("No EncounterPool assigned on CampaignFlow.");
                return;
            }

            // The campaign runs to the pool's last day; past it the run is over. Without this
            // the player rolls into day 8 with nothing eligible and loops forever.
            if (state.Day > _pool.Days)
            {
                GUILayout.Label(
                    $"Campaign complete — survived all {_pool.Days} days.\n"
                        + $"Funds {state.Funds}   Credibility {state.Credibility}   "
                        + $"Deck {state.Deck.Count}   Allies {state.Allies.Count}",
                    GUI.skin.box
                );
                if (GUILayout.Button("Start a new run", GUILayout.Height(30f)))
                {
                    RunState.Clear();
                    EnsureRunState();
                    RefreshLocations();
                }
                return;
            }

            GUILayout.Space(8f);
            if (state.TodaysLocations.Count == 0)
                GUILayout.Label("Nothing on offer today — end the day to move on.");

            // Indexed, not foreach: Enter() removes the chosen location from this very list, and
            // a foreach would throw on the next MoveNext. The early return also stops drawing a
            // list that no longer matches what just happened.
            for (int i = 0; i < state.TodaysLocations.Count; i++)
            {
                var loc = state.TodaysLocations[i];
                if (loc == null)
                    continue;

                var plan = state.PlanVisit(loc);
                GUI.enabled = plan.CanEnter;

                string label = string.IsNullOrEmpty(loc.DisplayName) ? loc.name : loc.DisplayName;
                string kind = loc is BattleEncounterData ? "Battle" : "Event";
                string district = loc.District != null ? loc.District.DisplayName : "Local";
                string cost = plan.CanEnter
                    ? $"Travel {plan.TravelMinutes}m + wait {plan.WaitMinutes}m + event {plan.EncounterMinutes}m"
                    : plan.BlockedReason;

                bool clicked = GUILayout.Button(
                    $"[{kind}] {label} / {district}   ({cost})",
                    GUILayout.Height(34f)
                );

                if (!string.IsNullOrEmpty(loc.Blurb))
                    GUILayout.Label($"    {loc.Blurb}");

                GUI.enabled = true;
                GUILayout.Label(
                    $"    Opens {CampaignTravelData.FormatTime(loc.OpeningMinute)} | "
                        + $"Enter before {CampaignTravelData.FormatTime(loc.ClosingMinute)}"
                );
                if (plan.ArrivalMinute > 0)
                {
                    string traffic =
                        plan.TrafficMultiplier >= 2f ? "Heavy"
                        : plan.TrafficMultiplier > 1f ? "Moderate"
                        : "Clear";
                    GUILayout.Label(
                        $"    Traffic: {traffic} ({plan.TrafficMultiplier:0.##}x) | "
                            + $"Arrive {CampaignTravelData.FormatTime(plan.ArrivalMinute)} | "
                            + $"Start {CampaignTravelData.FormatTime(plan.StartMinute)} | "
                            + $"Finish {CampaignTravelData.FormatTime(plan.FinishMinute)}"
                    );
                    Rect meter = GUILayoutUtility.GetRect(120f, 6f, GUILayout.Width(120f));
                    GUI.Box(meter, GUIContent.none);
                    meter.width *= Mathf.Clamp01((plan.TrafficMultiplier - 1f) / 4f);
                    GUI.DrawTexture(meter, Texture2D.whiteTexture);
                    if (plan.TravelMinutesSaved > 0 || plan.EncounterMinutesSaved > 0)
                    {
                        GUILayout.Label(
                            $"    Allies save {plan.TravelMinutesSaved}m travel and {plan.EncounterMinutesSaved}m encounter time."
                        );
                    }
                }
                GUILayout.Space(4f);

                if (clicked)
                {
                    Enter(loc);
                    return;
                }
            }

            GUILayout.Space(12f);

            GUI.enabled = state.MinutesRemaining >= 15;
            if (GUILayout.Button("Wait 15 minutes", GUILayout.Height(30f)))
            {
                state.TrySpendMinutes(15);
            }
            GUI.enabled = true;

            // On a boss day there is no ending the day — ending it IS facing the boss. Without
            // this the finale is skippable: End Day rolls you to the next day and, on the last
            // one, straight to "campaign complete" without the fight ever happening.
            var boss = UnresolvedBoss(state);
            if (boss != null)
            {
                string bossName = string.IsNullOrEmpty(boss.DisplayName)
                    ? boss.name
                    : boss.DisplayName;
                // Always enabled, like HQ was: running out of Hours must not strand the run.
                if (
                    GUILayout.Button(
                        $"Face {bossName} (mandatory finale, no travel or time cost)",
                        GUILayout.Height(30f)
                    )
                )
                    Enter(boss, chargeHours: false);
                return;
            }

            // HQ is always enabled at 0 cost so the day can be ended even at 0 Hours.
            if (GUILayout.Button("End the day (HQ)", GUILayout.Height(30f)))
                EndDay();
        }

        /// <summary>
        /// The boss still on offer today, or null. Drives the End Day → Face the boss swap.
        /// </summary>
        private static EncounterData UnresolvedBoss(RunState state)
        {
            foreach (var loc in state.TodaysLocations)
                if (loc is BattleEncounterData battle && battle.IsBoss)
                    return loc;
            return null;
        }

        /// <summary>
        /// Draws the deck picker an outcome raised (upgrade/remove a chosen card). No cancel
        /// button by design: <see cref="RunState.RequestCardChoice"/> refuses to open on an empty
        /// candidate list, so every picker shown has an answer, and choices are consequences of a
        /// pick the player already made.
        /// </summary>
        private void DrawCardChoice(RunState.CardChoice choice)
        {
            GUILayout.Label(choice.Prompt, GUI.skin.box);
            GUILayout.Space(8f);

            for (int i = 0; i < choice.Candidates.Count; i++)
            {
                var card = choice.Candidates[i];
                if (card == null)
                    continue;

                if (GUILayout.Button($"{card.CardName}   ({card.CardType})", GUILayout.Height(30f)))
                {
                    RunState.Current.ResolveCardChoice(card);
                    GameLogger.LogInfo("Campaign", $"Card choice: '{card.CardName}'.", this);
                    return; // the list is gone now — stop drawing against it
                }

                if (!string.IsNullOrEmpty(card.Description))
                    GUILayout.Label($"    {card.Description}");

                GUILayout.Space(2f);
            }
        }

        private void DrawEvent()
        {
            GUILayout.Label(_openEvent.DisplayName ?? _openEvent.name, GUI.skin.box);
            GUILayout.Space(6f);
            GUILayout.Label(_openEvent.Body);
            GUILayout.Space(12f);

            if (_pendingResultText != null)
            {
                GUILayout.Label(_pendingResultText, GUI.skin.box);
                GUILayout.Space(8f);
                if (GUILayout.Button("Continue", GUILayout.Height(30f)))
                    CloseEvent();
                return;
            }

            if (_openEvent.Options.Count == 0)
            {
                GUILayout.Label("(no options authored)");
                if (GUILayout.Button("Leave", GUILayout.Height(30f)))
                    CloseEvent();
                return;
            }

            var state = RunState.Current;
            foreach (var option in _openEvent.Options)
            {
                if (option == null)
                    continue;

                // Locked options stay visible and disabled, with the reason attached. Hiding
                // them would make a gated event read as a shorter event.
                bool available = option.IsAvailable(state);
                string outcomes = option.DescribeOutcomes();

                string label = option.Label;
                if (!available)
                    label += $"   [needs {option.DescribeRequirements()}]";
                else if (!string.IsNullOrEmpty(outcomes))
                    label += $"   —   {outcomes}";

                GUI.enabled = available;
                bool clicked = GUILayout.Button(label, GUILayout.Height(34f));
                GUI.enabled = true;

                if (clicked)
                {
                    ChooseOption(option);
                    return; // stop iterating: the option list is about to be replaced by result text
                }
            }
        }

        #endregion
    }
}
