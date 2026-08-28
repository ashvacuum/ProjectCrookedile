using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Crookedile.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Crookedile.UI
{
    /// <summary>
    /// In-game developer console (uGUI). Press ` (backquote) to toggle.
    ///
    /// One input line drives everything:
    ///   cheats  — any [CheatCommand] method, auto-discovered  (type 'help')
    ///   debug   — 'logs', 'log &lt;category&gt; &lt;level|off&gt;', 'loglevel', 'logsolo', 'logall'
    ///
    /// Colour coding: log level tints the message, each category gets a stable hue,
    /// and the level buttons along the top light up when that level is shown.
    ///
    /// Builds its own canvas at runtime, so there is nothing to wire up in a scene.
    /// </summary>
    [Debuggable("Console", LogLevel.Info)]
    public class DevConsole : MonoBehaviour
    {
        #region Bootstrap
#if CHEATS_ENABLED || UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
                return;
            var go = new GameObject("[DevConsole]");
            DontDestroyOnLoad(go);
            go.AddComponent<DevConsole>();
        }
#endif

        public static DevConsole Instance { get; private set; }

        public bool IsVisible => _root != null && _root.activeSelf;

        #endregion

        #region Colours
        private static readonly Color BgColor = new Color(0.04f, 0.05f, 0.07f, 0.93f);
        private static readonly Color FieldColor = new Color(0.10f, 0.12f, 0.16f, 1f);
        private static readonly Color MutedColor = new Color(0.45f, 0.50f, 0.58f, 1f);

        private static Color LevelColor(LogLevel level) =>
            level switch
            {
                LogLevel.Error => new Color(1f, 0.35f, 0.35f),
                LogLevel.Warning => new Color(1f, 0.78f, 0.35f),
                LogLevel.Verbose => new Color(0.55f, 0.60f, 0.68f),
                LogLevel.None => new Color(0.55f, 0.85f, 1f), // console's own output
                _ => new Color(0.87f, 0.90f, 0.94f),
            };

        // Stable per-category hue so "Battle" is always the same colour across runs.
        private static Color CategoryColor(string category)
        {
            if (string.IsNullOrEmpty(category))
                return MutedColor;
            int hash = 0;
            foreach (char c in category)
                hash = hash * 31 + c;
            return Color.HSVToRGB(Mathf.Abs(hash % 360) / 360f, 0.45f, 1f);
        }

        private static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        #endregion

        #region State
        private const int MaxEntries = 200;
        private const int MaxHistory = 50;

        private readonly List<LogEntry> _entries = new List<LogEntry>();

        private readonly HashSet<LogLevel> _shownLevels = new HashSet<LogLevel>
        {
            LogLevel.Error,
            LogLevel.Warning,
            LogLevel.Info,
            LogLevel.Verbose,
        };

        private string _categoryFilter = "";

        private readonly List<string> _history = new List<string>();
        private int _historyIndex = -1;

        private readonly Dictionary<string, Command> _commands = new Dictionary<string, Command>(
            StringComparer.OrdinalIgnoreCase
        );

        private GameObject _root;
        private TMP_InputField _input;
        private TextMeshProUGUI _output;
        private TextMeshProUGUI _hint;
        private ScrollRect _scroll;
        private readonly List<(LogLevel Level, Button Button)> _levelButtons =
            new List<(LogLevel, Button)>();

        #endregion

        #region Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildUI();
            DiscoverCommands();

            foreach (var e in GameLogger.Entries)
                _entries.Add(e);
            GameLogger.OnLogAdded += OnLogAdded;

            Print("Dev console ready — type 'help' for cheats, 'logs' for debug categories.");
            SetVisible(false);
        }

        private void OnDestroy()
        {
            GameLogger.OnLogAdded -= OnLogAdded;
            if (Instance == this)
                Instance = null;
        }

        private void OnLogAdded(LogEntry entry)
        {
            if (_entries.Count >= MaxEntries)
                _entries.RemoveAt(0);
            _entries.Add(entry);
            if (IsVisible)
                Redraw();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.backquoteKey.wasPressedThisFrame)
            {
                SetVisible(!IsVisible);
                return;
            }

            if (!IsVisible)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
                SetVisible(false);
            else if (keyboard.upArrowKey.wasPressedThisFrame)
                NavigateHistory(1);
            else if (keyboard.downArrowKey.wasPressedThisFrame)
                NavigateHistory(-1);
            else if (keyboard.tabKey.wasPressedThisFrame)
                Autocomplete();
        }

        private void SetVisible(bool visible)
        {
            _root.SetActive(visible);
            if (!visible)
                return;

            Redraw();
            _input.text = "";
            _historyIndex = -1;
            _input.ActivateInputField();
        }

        #endregion

        #region Rendering
        private void Redraw()
        {
            var sb = new StringBuilder();
            foreach (var e in _entries)
            {
                if (e.Level != LogLevel.None && !_shownLevels.Contains(e.Level))
                    continue;
                if (
                    !string.IsNullOrEmpty(_categoryFilter)
                    && e.Category.IndexOf(_categoryFilter, StringComparison.OrdinalIgnoreCase) < 0
                )
                    continue;

                if (!string.IsNullOrEmpty(e.Category))
                    sb.Append($"<color=#{Hex(CategoryColor(e.Category))}>[{e.Category}]</color> ");
                sb.AppendLine($"<color=#{Hex(LevelColor(e.Level))}>{e.Message}</color>");
            }

            _output.text = sb.ToString();

            foreach (var (level, button) in _levelButtons)
            {
                bool on = _shownLevels.Contains(level);
                button.targetGraphic.color = on
                    ? LevelColor(level) * 0.45f
                    : new Color(0.13f, 0.14f, 0.17f);
                button.GetComponentInChildren<TextMeshProUGUI>().color = on
                    ? LevelColor(level)
                    : MutedColor;
            }

            string filter = string.IsNullOrEmpty(_categoryFilter)
                ? ""
                : $"   filter:<color=#{Hex(CategoryColor(_categoryFilter))}>{_categoryFilter}</color>";
            string solo = string.IsNullOrEmpty(GameLogger.CurrentSolo)
                ? ""
                : $"   solo:<color=#{Hex(CategoryColor(GameLogger.CurrentSolo))}>{GameLogger.CurrentSolo}</color>";
            _hint.text = $"Tab completes  •  Up/Down history  •  Esc closes{filter}{solo}";

            // ponytail: rebuild the whole string per entry — 200 lines is nothing.
            // Swap to per-line children if the buffer ever grows past a few thousand.
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 0f;
        }

        private void Print(string message, LogLevel level = LogLevel.None)
        {
            OnLogAdded(new LogEntry(Time.time, "", message, level));
        }

        #endregion

        #region Command execution
        private void Submit(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return;

            _input.text = "";
            _input.ActivateInputField();
            _historyIndex = -1;
            _history.Insert(0, raw);
            if (_history.Count > MaxHistory)
                _history.RemoveAt(_history.Count - 1);

            Print($"> {raw}");

            string[] parts = raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string name = parts[0];

            if (name.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                ShowHelp();
                return;
            }
            if (name.Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                _entries.Clear();
                Redraw();
                return;
            }

            if (!_commands.TryGetValue(name, out var cmd))
            {
                Print($"Unknown command '{name}'. Type 'help'.", LogLevel.Error);
                return;
            }

            try
            {
                object[] args = ParseArgs(parts.Skip(1).ToArray(), cmd.Parameters);
                object result = cmd.Method.Invoke(cmd.Target, args);
                if (result != null)
                    Print(result.ToString());
            }
            catch (Exception e)
            {
                Print($"{name}: {e.InnerException?.Message ?? e.Message}", LogLevel.Error);
            }
        }

        private void ShowHelp()
        {
            foreach (
                var group in _commands
                    .Values.GroupBy(c => string.IsNullOrEmpty(c.Category) ? "General" : c.Category)
                    .OrderBy(g => g.Key)
            )
            {
                Print($"<b><color=#{Hex(CategoryColor(group.Key))}>{group.Key}</color></b>");
                foreach (var c in group.OrderBy(c => c.Name))
                    Print($"  <b>{c.Name}</b> {Signature(c)} <color=#{Hex(MutedColor)}>{c.Description}</color>");
            }
        }

        private static string Signature(Command c) =>
            string.Join(
                " ",
                c.Parameters.Select(p =>
                    p.HasDefaultValue ? $"[{p.Name}]" : $"&lt;{p.Name}&gt;"
                )
            );

        private static object[] ParseArgs(string[] args, ParameterInfo[] parameters)
        {
            var result = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                Type type = parameters[i].ParameterType;

                if (i >= args.Length)
                {
                    if (!parameters[i].HasDefaultValue)
                        throw new Exception($"missing argument '{parameters[i].Name}'");
                    result[i] = parameters[i].DefaultValue;
                    continue;
                }

                string arg = args[i];
                if (type == typeof(string))
                    result[i] = arg;
                else if (type == typeof(int))
                    result[i] = int.Parse(arg);
                else if (type == typeof(float))
                    result[i] = float.Parse(arg);
                else if (type == typeof(bool))
                    result[i] = bool.Parse(arg);
                else if (type.IsEnum)
                    result[i] = Enum.Parse(type, arg, true);
                else
                    throw new Exception($"unsupported parameter type {type.Name}");
            }
            return result;
        }

        #endregion

        #region Input helpers
        private void NavigateHistory(int dir)
        {
            if (_history.Count == 0)
                return;
            _historyIndex = Mathf.Clamp(_historyIndex + dir, -1, _history.Count - 1);
            _input.text = _historyIndex >= 0 ? _history[_historyIndex] : "";
            _input.caretPosition = _input.text.Length;
        }

        private void Autocomplete()
        {
            string prefix = (_input.text ?? "").Split(' ')[0];
            if (string.IsNullOrEmpty(prefix))
                return;

            var matches = _commands
                .Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(k => k)
                .ToList();

            if (matches.Count == 0)
                return;

            if (matches.Count == 1)
            {
                _input.text = matches[0] + " ";
                _input.caretPosition = _input.text.Length;
                return;
            }

            Print(string.Join("   ", matches));
        }

        #endregion

        #region Debug commands
        [CheatCommand("logs", "List debug categories and their levels", Category = "Debug")]
        private string CmdLogs()
        {
            foreach (string category in GameLogger.KnownCategories.OrderBy(c => c))
            {
                var (enabled, level) = GameLogger.GetCategory(category);
                string state = enabled ? level.ToString() : "off";
                Print(
                    $"  <color=#{Hex(CategoryColor(category))}>{category}</color> "
                        + $"<color=#{Hex(enabled ? LevelColor(level) : MutedColor)}>{state}</color>"
                );
            }
            return $"global: {GameLogger.GlobalLevel}";
        }

        [CheatCommand("log", "Set a category's level: log Battle verbose | log Battle off", Category = "Debug")]
        private string CmdLog(string category, string level)
        {
            if (level.Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                GameLogger.SetCategory(category, false);
                return $"{category} muted";
            }
            var parsed = (LogLevel)Enum.Parse(typeof(LogLevel), level, true);
            GameLogger.SetCategory(category, true, parsed);
            return $"{category} → {parsed}";
        }

        [CheatCommand("loglevel", "Set the global log level", Category = "Debug")]
        private string CmdLogLevel(string level)
        {
            var parsed = (LogLevel)Enum.Parse(typeof(LogLevel), level, true);
            GameLogger.SetGlobalLevel(parsed);
            return $"global → {parsed}";
        }

        [CheatCommand("filter", "Only show logs whose category contains this text (blank clears)", Category = "Debug")]
        private string CmdFilter(string text = "")
        {
            _categoryFilter = text;
            Redraw();
            return string.IsNullOrEmpty(text) ? "filter cleared" : $"filter → {text}";
        }

        #endregion

        #region Command discovery
        private class Command
        {
            public string Name;
            public string Description;
            public string Category;
            public MethodInfo Method;
            public object Target;
            public ParameterInfo[] Parameters;
        }

        private void DiscoverCommands()
        {
            const BindingFlags flags =
                BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.Instance
                | BindingFlags.Static;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch
                {
                    continue; // unloadable assembly
                }

                foreach (var type in types)
                foreach (var method in type.GetMethods(flags))
                {
                    var attr = method.GetCustomAttribute<CheatCommandAttribute>();
                    if (attr == null)
                        continue;

                    object target = null;
                    if (!method.IsStatic)
                    {
                        target = type == GetType()
                            ? this
                            : FindFirstObjectByType(type, FindObjectsInactive.Include);
                        if (target == null)
                            continue; // no live instance to call
                    }

                    _commands[attr.Command] = new Command
                    {
                        Name = attr.Command,
                        Description = attr.Description,
                        Category = attr.Category,
                        Method = method,
                        Target = target,
                        Parameters = method.GetParameters(),
                    };
                }
            }

            GameLogger.LogInfo("Console", $"Discovered {_commands.Count} commands");
        }

        #endregion

        #region UI construction
        private void BuildUI()
        {
            if (EventSystem.current == null)
            {
                var es = new GameObject(
                    "[DevConsole EventSystem]",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule)
                );
                DontDestroyOnLoad(es);
            }

            var canvasGO = new GameObject(
                "Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _root = Panel("Console", canvasGO.transform, BgColor);
            var rootRect = (RectTransform)_root.transform;
            rootRect.anchorMin = new Vector2(0.02f, 0.45f);
            rootRect.anchorMax = new Vector2(0.98f, 0.98f);
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

            var layout = _root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 6f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildLevelBar(_root.transform);
            BuildOutput(_root.transform);

            _hint = Label("Hint", _root.transform, MutedColor, 16f);
            _hint.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

            BuildInput(_root.transform);
        }

        private void BuildLevelBar(Transform parent)
        {
            var bar = new GameObject("Levels", typeof(RectTransform));
            bar.transform.SetParent(parent, false);
            var group = bar.AddComponent<HorizontalLayoutGroup>();
            group.spacing = 6f;
            group.childControlWidth = group.childControlHeight = true;
            group.childForceExpandWidth = false;
            bar.AddComponent<LayoutElement>().preferredHeight = 30f;

            foreach (
                var level in new[]
                {
                    LogLevel.Error,
                    LogLevel.Warning,
                    LogLevel.Info,
                    LogLevel.Verbose,
                }
            )
            {
                var buttonGO = Panel(level.ToString(), bar.transform, Color.black);
                var button = buttonGO.AddComponent<Button>();
                button.targetGraphic = buttonGO.GetComponent<Image>();
                buttonGO.AddComponent<LayoutElement>().preferredWidth = 100f;

                var label = Label("Text", buttonGO.transform, LevelColor(level), 16f);
                Stretch(label.rectTransform);
                label.alignment = TextAlignmentOptions.Center;
                label.text = level.ToString();

                LogLevel captured = level;
                button.onClick.AddListener(() =>
                {
                    if (!_shownLevels.Remove(captured))
                        _shownLevels.Add(captured);
                    Redraw();
                });

                _levelButtons.Add((level, button));
            }
        }

        private void BuildOutput(Transform parent)
        {
            var viewport = Panel("Output", parent, new Color(0f, 0f, 0f, 0.35f));
            viewport.AddComponent<RectMask2D>();
            var element = viewport.AddComponent<LayoutElement>();
            element.flexibleHeight = 1f;
            element.preferredHeight = 400f;

            _scroll = viewport.AddComponent<ScrollRect>();
            _scroll.horizontal = false;
            _scroll.scrollSensitivity = 30f;
            _scroll.movementType = ScrollRect.MovementType.Clamped;

            _output = Label("Text", viewport.transform, Color.white, 18f);
            var rect = _output.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(8f, 0f);
            rect.offsetMax = new Vector2(-8f, 0f);
            _output.alignment = TextAlignmentOptions.TopLeft;
            _output.richText = true;

            var fitter = _output.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scroll.viewport = (RectTransform)viewport.transform;
            _scroll.content = rect;
        }

        private void BuildInput(Transform parent)
        {
            var fieldGO = Panel("Input", parent, FieldColor);
            fieldGO.AddComponent<LayoutElement>().preferredHeight = 34f;

            var textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(fieldGO.transform, false);
            var areaRect = (RectTransform)textArea.transform;
            Stretch(areaRect);
            areaRect.offsetMin = new Vector2(8f, 2f);
            areaRect.offsetMax = new Vector2(-8f, -2f);

            var text = Label("Text", textArea.transform, Color.white, 18f);
            Stretch(text.rectTransform);
            text.alignment = TextAlignmentOptions.Left;
            text.richText = false;

            _input = fieldGO.AddComponent<TMP_InputField>();
            _input.textViewport = areaRect;
            _input.textComponent = text;
            _input.lineType = TMP_InputField.LineType.SingleLine;
            _input.caretColor = new Color(0.55f, 0.85f, 1f);
            _input.customCaretColor = true;
            _input.restoreOriginalTextOnEscape = false;
            // Swallow the toggle key and tab so they never land in the field as text.
            _input.onValidateInput = (text, index, c) => c == '`' || c == '\t' ? '\0' : c;
            _input.onSubmit.AddListener(Submit);
        }

        private static GameObject Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static TextMeshProUGUI Label(
            string name,
            Transform parent,
            Color color,
            float size
        )
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.color = color;
            label.fontSize = size;
            return label;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        #endregion
    }
}
