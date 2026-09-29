using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Campaign;
using Crookedile.Data.Cards;
using Crookedile.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

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

        // ponytail: runtime-built UIDocument/PanelSettings — no scene/asset wiring needed, temporary
        // playtest UI. Replace with an authored PanelSettings + UXML when the production UI lands.
        private UIDocument _uiDocument;
        private VisualElement _root;
        private StyleSheet _styleSheet;

        #endregion

        #region Lifecycle
        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
                _uiDocument = gameObject.AddComponent<UIDocument>();
            if (_uiDocument.panelSettings == null)
                _uiDocument.panelSettings = CreateRuntimePanelSettings();
            _styleSheet = Resources.Load<StyleSheet>("UI/CampaignMap");
            if (_styleSheet == null)
                GameLogger.LogWarning(
                    "Campaign",
                    "Resources/UI/CampaignMap.uss failed to load — panel will render unstyled.",
                    this
                );
        }

        private void Start()
        {
            EnsureRunState();
            RunState.Current.ConfigureTravel(_pool != null ? _pool.Travel : null);
            ResolveChainOrRefresh();
            RefreshView();
        }

        private static PanelSettings CreateRuntimePanelSettings()
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.match = 0.5f;
            settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/DefaultRuntimeTheme");
            return settings;
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

        #region Debug HUD (IMGUI)
        // ponytail: thin IMGUI strip for at-a-glance run stats; the interactive view below is UI Toolkit.
        private void OnGUI()
        {
            var state = RunState.Current;
            GUILayout.BeginArea(new Rect(20f, 4f, Screen.width - 40f, 24f));
            GUILayout.Label(
                state == null
                    ? "Run ended."
                    : $"Day {state.Day}   {CampaignTravelData.FormatTime(state.ClockMinute)}   "
                        + $"Time left {state.MinutesRemaining / 60}h {state.MinutesRemaining % 60}m   "
                        + $"District: {(state.CurrentDistrict != null ? state.CurrentDistrict.DisplayName : "Local")}   "
                        + $"Funds {state.Funds}   Credibility {state.Credibility}   "
                        + $"Deck {state.Deck.Count}   Allies {state.Allies.Count}   Seed {state.Seed}",
                GUI.skin.box
            );
            GUILayout.EndArea();
        }

        #endregion

        #region Interactive view (UI Toolkit)
        // ponytail: temporary playtest UI. Replace with isometric map hotspots when the production UI is ready.
        private VisualElement _content;
        private VisualElement _tooltip;
        private Label _tooltipLabel;

        private void EnsureUIRoot()
        {
            if (_root != null)
                return;

            _root = _uiDocument.rootVisualElement;
            if (_styleSheet != null)
                _root.styleSheets.Add(_styleSheet);

            _content = new VisualElement();
            _content.AddToClassList("campaign-root");
            _root.Add(_content);

            _tooltipLabel = new Label();
            _tooltip = new VisualElement();
            _tooltip.AddToClassList("tooltip");
            _tooltip.style.display = DisplayStyle.None;
            _tooltip.pickingMode = PickingMode.Ignore;
            _tooltip.Add(_tooltipLabel);
            _root.Add(_tooltip);
        }

        private void ShowTooltip(VisualElement target, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            _tooltipLabel.text = text;
            _tooltip.style.display = DisplayStyle.Flex;
            Rect bound = target.worldBound;
            _tooltip.style.left = bound.xMax + 8f;
            _tooltip.style.top = bound.yMin;
        }

        private void HideTooltip()
        {
            _tooltip.style.display = DisplayStyle.None;
        }

        /// <summary>Shows <paramref name="text"/> next to <paramref name="target"/> on hover.</summary>
        private void AttachTooltip(VisualElement target, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            target.RegisterCallback<PointerEnterEvent>(_ => ShowTooltip(target, text));
            target.RegisterCallback<PointerLeaveEvent>(_ => HideTooltip());
        }

        private void RefreshView()
        {
            EnsureUIRoot();
            _content.Clear();
            HideTooltip();

            var state = RunState.Current;
            if (state == null)
            {
                _content.Add(BuildRunEndedPanel());
                return;
            }

            if (state.PendingCardChoice != null)
                _content.Add(BuildCardChoicePanel(state.PendingCardChoice));
            else if (_openEvent != null)
                _content.Add(BuildEventPanel());
            else
                _content.Add(BuildMapPanel(state));
        }

        private VisualElement BuildRunEndedPanel()
        {
            var panel = new VisualElement();
            panel.AddToClassList("panel");
            panel.Add(new Label("Run ended.") { });
            var button = new Button(() =>
            {
                EnsureRunState();
                RefreshLocations();
                RefreshView();
            })
            {
                text = "Start a new run",
            };
            button.AddToClassList("action-button");
            panel.Add(button);
            return panel;
        }

        private VisualElement BuildMapPanel(RunState state)
        {
            var panel = new VisualElement();
            panel.AddToClassList("panel");

            if (_pool == null)
            {
                panel.Add(new Label("No EncounterPool assigned on CampaignFlow."));
                return panel;
            }

            // The campaign runs to the pool's last day; past it the run is over. Without this
            // the player rolls into day 8 with nothing eligible and loops forever.
            if (state.Day > _pool.Days)
            {
                panel.Add(
                    new Label($"Campaign complete — survived all {_pool.Days} days.")
                    {
                        }
                );
                var summary = new Label(
                    $"Funds {state.Funds}   Credibility {state.Credibility}   "
                        + $"Deck {state.Deck.Count}   Allies {state.Allies.Count}"
                );
                summary.AddToClassList("meta-line");
                panel.Add(summary);

                var restartButton = new Button(() =>
                {
                    RunState.Clear();
                    EnsureRunState();
                    RefreshLocations();
                    RefreshView();
                })
                {
                    text = "Start a new run",
                };
                restartButton.AddToClassList("action-button");
                panel.Add(restartButton);
                return panel;
            }

            var scroll = new ScrollView();
            scroll.AddToClassList("scroll-view");
            panel.Add(scroll);

            if (state.TodaysLocations.Count == 0)
                scroll.Add(new Label("Nothing on offer today — end the day to move on."));

            foreach (var loc in state.TodaysLocations)
            {
                if (loc == null)
                    continue;
                scroll.Add(BuildLocationListItem(state, loc));
            }

            var waitButton = new Button(() =>
            {
                state.TrySpendMinutes(15);
                RefreshView();
            })
            {
                text = "Wait 15 minutes",
            };
            waitButton.AddToClassList("secondary-button");
            waitButton.SetEnabled(state.MinutesRemaining >= 15);
            panel.Add(waitButton);

            // On a boss day there is no ending the day — ending it IS facing the boss. Without
            // this the finale is skippable: End Day rolls you to the next day and, on the last
            // one, straight to "campaign complete" without the fight ever happening.
            var boss = UnresolvedBoss(state);
            if (boss != null)
            {
                string bossName = string.IsNullOrEmpty(boss.DisplayName) ? boss.name : boss.DisplayName;
                var bossButton = new Button(() =>
                {
                    Enter(boss, chargeHours: false);
                    RefreshView();
                })
                {
                    text = $"Face {bossName} (mandatory finale, no travel or time cost)",
                };
                bossButton.AddToClassList("action-button");
                panel.Add(bossButton);
                return panel;
            }

            // HQ is always enabled at 0 cost so the day can be ended even at 0 Hours.
            var endDayButton = new Button(() =>
            {
                EndDay();
                RefreshView();
            })
            {
                text = "End the day (HQ)",
            };
            endDayButton.AddToClassList("action-button");
            panel.Add(endDayButton);

            return panel;
        }

        /// <summary>
        /// One row: name, kind, and Hours cost only. The travel/wait/traffic breakdown that used
        /// to sit inline now lives in a hover tooltip (see <see cref="BuildLocationTooltip"/>) so
        /// the list itself stays scannable.
        /// </summary>
        private VisualElement BuildLocationListItem(RunState state, EncounterData loc)
        {
            var plan = state.PlanVisit(loc);
            string label = string.IsNullOrEmpty(loc.DisplayName) ? loc.name : loc.DisplayName;
            string kind = loc is BattleEncounterData ? "Battle" : "Event";

            var button = new Button(() =>
            {
                Enter(loc);
                RefreshView();
            })
            {
                text = $"[{kind}] {label}   ({loc.HourCost}h)",
            };
            button.AddToClassList("action-button");
            button.SetEnabled(plan.CanEnter);
            AttachTooltip(button, BuildLocationTooltip(loc, plan));

            return button;
        }

        private static string BuildLocationTooltip(EncounterData loc, CampaignVisitPlan plan)
        {
            var lines = new List<string>();
            if (!string.IsNullOrEmpty(loc.Blurb))
                lines.Add(loc.Blurb);

            lines.Add(
                $"Opens {CampaignTravelData.FormatTime(loc.OpeningMinute)} | "
                    + $"Enter before {CampaignTravelData.FormatTime(loc.ClosingMinute)}"
            );

            if (!plan.CanEnter)
            {
                lines.Add(plan.BlockedReason);
                return string.Join("\n", lines);
            }

            lines.Add(
                $"Travel {plan.TravelMinutes}m + wait {plan.WaitMinutes}m + event {plan.EncounterMinutes}m"
            );

            if (plan.ArrivalMinute > 0)
            {
                string traffic =
                    plan.TrafficMultiplier >= 2f ? "Heavy"
                    : plan.TrafficMultiplier > 1f ? "Moderate"
                    : "Clear";
                lines.Add(
                    $"Traffic: {traffic} ({plan.TrafficMultiplier:0.##}x) | "
                        + $"Arrive {CampaignTravelData.FormatTime(plan.ArrivalMinute)} | "
                        + $"Start {CampaignTravelData.FormatTime(plan.StartMinute)} | "
                        + $"Finish {CampaignTravelData.FormatTime(plan.FinishMinute)}"
                );

                if (plan.TravelMinutesSaved > 0 || plan.EncounterMinutesSaved > 0)
                    lines.Add(
                        $"Allies save {plan.TravelMinutesSaved}m travel and {plan.EncounterMinutesSaved}m encounter time."
                    );
            }

            return string.Join("\n", lines);
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
        private VisualElement BuildCardChoicePanel(RunState.CardChoice choice)
        {
            var panel = new VisualElement();
            panel.AddToClassList("panel");

            var title = new Label(choice.Prompt);
            title.AddToClassList("title");
            panel.Add(title);

            foreach (var card in choice.Candidates)
            {
                if (card == null)
                    continue;

                var button = new Button(() =>
                {
                    RunState.Current.ResolveCardChoice(card);
                    GameLogger.LogInfo("Campaign", $"Card choice: '{card.CardName}'.", this);
                    RefreshView();
                })
                {
                    text = $"{card.CardName}   ({card.CardType})",
                };
                button.AddToClassList("action-button");
                panel.Add(button);

                if (!string.IsNullOrEmpty(card.Description))
                {
                    var desc = new Label(card.Description);
                    desc.AddToClassList("blurb");
                    panel.Add(desc);
                }
            }

            return panel;
        }

        private VisualElement BuildEventPanel()
        {
            var panel = new VisualElement();
            panel.AddToClassList("panel");

            if (_openEvent.Image != null)
            {
                var image = new VisualElement();
                image.AddToClassList("event-image");
                image.style.backgroundImage = new StyleBackground(_openEvent.Image);
                panel.Add(image);
            }

            var title = new Label(_openEvent.DisplayName ?? _openEvent.name);
            title.AddToClassList("title");
            panel.Add(title);

            var body = new Label(_openEvent.Body);
            body.AddToClassList("blurb");
            panel.Add(body);

            if (_pendingResultText != null)
            {
                var result = new Label(_pendingResultText);
                result.AddToClassList("meta-line");
                panel.Add(result);

                var continueButton = new Button(() =>
                {
                    CloseEvent();
                    RefreshView();
                })
                {
                    text = "Continue",
                };
                continueButton.AddToClassList("action-button");
                panel.Add(continueButton);
                return panel;
            }

            if (_openEvent.Options.Count == 0)
            {
                panel.Add(new Label("(no options authored)"));
                var leaveButton = new Button(() =>
                {
                    CloseEvent();
                    RefreshView();
                })
                {
                    text = "Leave",
                };
                leaveButton.AddToClassList("action-button");
                panel.Add(leaveButton);
                return panel;
            }

            var state = RunState.Current;
            foreach (var option in _openEvent.Options)
            {
                if (option == null)
                    continue;

                // Locked options stay visible and disabled, with the reason in the tooltip.
                // Hiding them would make a gated event read as a shorter event.
                bool available = option.IsAvailable(state);
                string outcomes = option.DescribeOutcomes();

                var optionButton = new Button(() =>
                {
                    ChooseOption(option);
                    RefreshView();
                })
                {
                    text = option.Label,
                };
                optionButton.AddToClassList("action-button");
                optionButton.SetEnabled(available);
                AttachTooltip(
                    optionButton,
                    available ? outcomes : $"Needs {option.DescribeRequirements()}"
                );
                panel.Add(optionButton);
            }

            return panel;
        }

        #endregion
    }
}
