using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Crookedile.Data;
using Crookedile.Data.Battle;
using Crookedile.Data.Cards;
using Crookedile.Gameplay.Battle;
using Crookedile.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Crookedile.Editor.Database
{
    /// <summary>Every <see cref="CardData"/>: how it reads, what it costs and does, and what's wrong with it.</summary>
    public sealed class CardsTab : ContentTab<CardData>
    {
        private const string CardsRoot = "Assets/Data/Cards/";
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private CardType? _type;
        private CardRarity? _rarity;
        private HostilityKind? _hostility;
        private CardFantasy? _fantasy;
        private string _class;
        private bool _starterOnly;
        private bool _noArtOnly;

        private List<string> _classes = new List<string>();
        private HashSet<string> _duplicateNames = new HashSet<string>();
        private StatusEffectIconMapSO _iconMap;
        private readonly Dictionary<CardData, CardHostility> _hostilityCache = new Dictionary<CardData, CardHostility>();
        private readonly Dictionary<CardData, string> _classOf = new Dictionary<CardData, string>();

        public override string Title => "Cards";

        protected override string DisplayName(CardData card) =>
            string.IsNullOrWhiteSpace(card.CardName) ? $"({card.name})" : card.CardName;

        protected override string SearchText(CardData card) =>
            string.Join(" ", card.Tags) + " " + SafeDescription(card);

        protected override IEnumerable<Column> BuildColumns()
        {
            yield return new Column("Card", 170, null, c => DisplayName(c));
            yield return new Column("Class", 78, ClassOf, c => ClassOf(c));
            yield return new Column("Type", 70, c => c.CardType.ToString(), c => c.CardType, c => TypeColor(c.CardType));
            yield return new Column("Rarity", 66, c => c.Rarity.ToString(), c => c.Rarity, c => RarityColor(c.Rarity));
            yield return new Column("Cost", 38, CostLabel, c => CostOf(c));
            yield return new Column("Sway", 42, c => Blank(MaxSway(c)), c => MaxSway(c));
            yield return new Column("Supp.", 42, c => Blank(Support(c)), c => Support(c));
            yield return new Column("Hostility", 92, c => HostilityOf(c).Label(), c => HostilityOf(c).Kind, HostilityColor);
            yield return new Column("Fantasy", 52, FantasyInitials, c => (int)c.Fantasies);
            yield return new Column("Flags", 52, Flags, c => Flags(c));
        }

        protected override IEnumerable<TabAction> Actions()
        {
            yield return new TabAction("Refresh databases", DatabaseAutoRefresh.RefreshAll);
            yield return new TabAction("Export CSV", CardCsvExporter.Export);
        }

        protected override void OnReloaded(IReadOnlyList<CardData> all)
        {
            _hostilityCache.Clear();
            _classOf.Clear();
            _classes = all.Select(ClassOf).Distinct().OrderBy(c => c).ToList();
            _duplicateNames = new HashSet<string>(
                all.GroupBy(c => c.CardName).Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key)).Select(g => g.Key)
            );
            string guid = AssetDatabase.FindAssets("t:" + nameof(StatusEffectIconMapSO)).FirstOrDefault();
            _iconMap = guid == null ? null : AssetDatabase.LoadAssetAtPath<StatusEffectIconMapSO>(AssetDatabase.GUIDToAssetPath(guid));
        }

        // ---- Filters --------------------------------------------------------------------

        protected override bool DrawFilters()
        {
            bool changed = false;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Class", EditorStyles.miniLabel, GUILayout.Width(62));
                if (GUILayout.Toggle(_class == null, "All", EditorStyles.miniButtonLeft) && _class != null)
                {
                    _class = null;
                    changed = true;
                }
                foreach (var cls in _classes)
                    if (GUILayout.Toggle(_class == cls, cls, EditorStyles.miniButtonMid) && _class != cls)
                    {
                        _class = cls;
                        changed = true;
                    }
                GUILayout.Space(12);
                changed |= Toggle(ref _starterOnly, "Starter only");
                changed |= Toggle(ref _noArtOnly, "No art only");
                GUILayout.FlexibleSpace();
            }
            changed |= EnumFilter("Type", ref _type);
            changed |= EnumFilter("Rarity", ref _rarity);
            changed |= EnumFilter("Hostility", ref _hostility);
            changed |= EnumFilter("Fantasy", ref _fantasy, f => f == CardFantasy.None ? "Unassigned" : f.ToString());
            return changed;
        }

        protected override bool PassesFilters(CardData card) =>
            (_class == null || ClassOf(card) == _class)
            && (!_type.HasValue || card.CardType == _type.Value)
            && (!_rarity.HasValue || card.Rarity == _rarity.Value)
            && (!_hostility.HasValue || HostilityOf(card).Kind == _hostility.Value)
            && (!_fantasy.HasValue || (_fantasy.Value == CardFantasy.None ? card.Fantasies == CardFantasy.None : card.Fantasies.HasFlag(_fantasy.Value)))
            && (!_starterOnly || card.IsStarterCard)
            && (!_noArtOnly || card.IsInDevelopment);

        // ---- Audit ----------------------------------------------------------------------

        protected override IEnumerable<Issue> Audit(CardData card)
        {
            bool hasEffects = card.Effects != null && card.Effects.Count > 0;
            bool hasPassives = card.Passives != null && card.Passives.Count > 0;

            if (string.IsNullOrWhiteSpace(card.CardName))
                yield return Issue.Error("No card name.");
            else if (_duplicateNames.Contains(card.CardName))
                yield return Issue.Error($"Another card is also named '{card.CardName}'.");
            if (!card.IsJunk && !card.IsUnplayable && !hasEffects && !hasPassives)
                yield return Issue.Error("No effects or passives: does nothing when played.");

            if (card.Effects != null)
                for (int i = 0; i < card.Effects.Count; i++)
                {
                    var effect = card.Effects[i];
                    if (effect == null)
                        yield return Issue.Error($"Effect [{i}] is empty (no type picked).");
                    else
                        foreach (var problem in effect.GetConfigurationIssues())
                            yield return Issue.Warning($"{effect.GetType().Name}: {problem}");
                }

            if (card.Passives != null)
                for (int i = 0; i < card.Passives.Count; i++)
                {
                    var passive = card.Passives[i];
                    if (passive == null)
                    {
                        yield return Issue.Error($"Passive [{i}] is empty.");
                        continue;
                    }
                    if (passive.Trigger == null)
                        yield return Issue.Error($"Passive [{i}] has no trigger, so it never fires.");
                    if (passive.Effects == null || passive.Effects.Count == 0)
                        yield return Issue.Warning($"Passive '{passive.Name}' has no effects.");
                    else
                        foreach (var effect in passive.Effects)
                            if (effect == null)
                                yield return Issue.Error($"Passive '{passive.Name}' has an empty effect.");
                            else
                                foreach (var problem in effect.GetConfigurationIssues())
                                    yield return Issue.Warning($"Passive '{passive.Name}' {effect.GetType().Name}: {problem}");
                }

            foreach (var status in StatusesUsed(card))
            {
                if (_iconMap == null || !_iconMap.TryGet(status.Id, out var icon, out _, out var name, out _))
                    yield return Issue.Warning($"Uses status '{status.DisplayName}', which has no icon-map entry (seed it from the Statuses data).");
                else
                {
                    if (icon == null)
                        yield return Issue.Warning($"Uses status '{status.DisplayName}', which has no icon.");
                    if (string.IsNullOrEmpty(name))
                        yield return Issue.Info($"Uses status '{status.DisplayName}', which has no display name.");
                }
            }

            if (card.IsInDevelopment && !card.IsGeneratedOnly)
                yield return Issue.Warning("No artwork, so it is never offered as a reward.");
            if (card.NeedsConfiguration)
                yield return Issue.Warning("Has leftover configuration notes.");
            if (!card.IsJunk && (card.Costs == null || card.Costs.Count == 0))
                yield return Issue.Info("No cost entry.");
            if (!card.IsJunk && !card.IsUpgraded && !card.CanUpgrade)
                yield return Issue.Info("No upgrade authored.");
            if (card.IsUnlockable && card.UnlockCondition == null)
                yield return Issue.Info("Unlockable with no condition: only an event grant unlocks it.");
        }

        // ---- Appearance -----------------------------------------------------------------

        protected override void DrawPreview(CardData card)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                Picture(card.Artwork, 150);
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField($"{DisplayName(card)}   ·   {CostLabel(card)}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"{card.CardType} · {card.Rarity} · {ClassOf(card)}{(card.IsStarterCard ? " · starter" : "")}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(SafeDescription(card), EditorStyles.wordWrappedLabel);
                    if (card.CanUpgrade)
                        EditorGUILayout.LabelField("Upgraded: " + SafeDescription(card, upgraded: true), EditorStyles.wordWrappedMiniLabel);
                    if (!string.IsNullOrEmpty(card.FlavorText))
                        EditorGUILayout.LabelField(card.FlavorText, ItalicMini);

                    var hostility = HostilityOf(card);
                    string line = string.Join("   ", new[]
                    {
                        hostility.Kind != HostilityKind.None ? "Hostility: " + hostility.Label() : null,
                        card.Fantasies != CardFantasy.None ? "Fantasy: " + card.Fantasies : null,
                        card.Tags.Count > 0 ? "Tags: " + string.Join(", ", card.Tags) : null,
                    }.Where(s => s != null));
                    if (line.Length > 0)
                        EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);

                    var statuses = StatusesUsed(card).ToList();
                    if (statuses.Count > 0)
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Statuses:", EditorStyles.miniLabel, GUILayout.Width(52));
                            foreach (var status in statuses)
                            {
                                Sprite icon = null;
                                _iconMap?.TryGet(status.Id, out icon, out _);
                                var content = new GUIContent(status.DisplayName, icon != null ? AssetPreview.GetAssetPreview(icon) : null);
                                GUILayout.Label(content, EditorStyles.miniLabel, GUILayout.Height(18));
                            }
                            GUILayout.FlexibleSpace();
                        }
                }
            }
            if (card.NeedsConfiguration)
                EditorGUILayout.HelpBox(card.ConfigurationNotes, MessageType.Info);
        }

        protected override void DrawSummary(IReadOnlyList<CardData> all)
        {
            EditorGUILayout.LabelField($"{all.Count} cards. Select one to see and edit it.", EditorStyles.miniLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("By class", EditorStyles.boldLabel);
            foreach (var group in all.GroupBy(ClassOf).OrderBy(g => g.Key))
                Bar(group.Key, group.Count(), all.Count);
            EditorGUILayout.LabelField("By type", EditorStyles.boldLabel);
            foreach (CardType type in Enum.GetValues(typeof(CardType)))
                Bar(type.ToString(), all.Count(c => c.CardType == type), all.Count);
            EditorGUILayout.LabelField("By rarity", EditorStyles.boldLabel);
            foreach (CardRarity rarity in Enum.GetValues(typeof(CardRarity)))
                Bar(rarity.ToString(), all.Count(c => c.Rarity == rarity), all.Count);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("State", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"No artwork (never offered): {all.Count(c => c.IsInDevelopment && !c.IsGeneratedOnly)}");
            EditorGUILayout.LabelField($"Leftover configuration notes: {all.Count(c => c.NeedsConfiguration)}");
            EditorGUILayout.LabelField($"Unlockable: {all.Count(c => c.IsUnlockable)}");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Notable", EditorStyles.boldLabel);
            Notable("Biggest Sway", all.OrderByDescending(MaxSway).FirstOrDefault(), c => $"{MaxSway(c)} Sway");
            Notable("Most Support", all.OrderByDescending(Support).FirstOrDefault(), c => $"+{Support(c)} Support");
            Notable("Most effects", all.OrderByDescending(c => c.Effects?.Count ?? 0).FirstOrDefault(), c => $"{c.Effects?.Count ?? 0} effects");
        }

        private void Notable(string label, CardData card, Func<CardData, string> detail)
        {
            if (card == null)
                return;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(110));
                if (GUILayout.Button(DisplayName(card), EditorStyles.linkLabel))
                    SelectLater(card);
                GUILayout.Label(detail(card), EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
            }
        }

        // ---- Card facts -----------------------------------------------------------------

        private CardHostility HostilityOf(CardData card)
        {
            if (!_hostilityCache.TryGetValue(card, out var result))
                _hostilityCache[card] = result = CardHostility.Of(card);
            return result;
        }

        /// <summary>The folder under Data/Cards: the class or pool (FaithLeader, NepoBaby, Status, ...).</summary>
        private string ClassOf(CardData card)
        {
            if (!_classOf.TryGetValue(card, out var cls))
                _classOf[card] = cls = FolderOf(card);
            return cls;
        }

        private static string FolderOf(CardData card)
        {
            string path = AssetDatabase.GetAssetPath(card);
            if (!path.StartsWith(CardsRoot))
                return "(other)";
            string rest = path.Substring(CardsRoot.Length);
            int slash = rest.IndexOf('/');
            return slash > 0 ? rest.Substring(0, slash) : "(root)";
        }

        private static int CostOf(CardData card)
        {
            var costs = card.GetCosts(false);
            if (costs == null || costs.Count == 0)
                return 0;
            return costs.Where(c => c.CostType == CostType.ActionPoints).Select(c => c.BaseAmount).DefaultIfEmpty(0).Min();
        }

        private static string CostLabel(CardData card)
        {
            var costs = card.GetCosts(false);
            if (costs != null && costs.Any(c => c.IsXCost))
                return "X";
            return card.IsUnplayable ? "—" : CostOf(card).ToString();
        }

        private static int MaxSway(CardData card)
        {
            int best = 0;
            if (card.Effects == null)
                return 0;
            foreach (var effect in card.Effects)
            {
                var preview = effect?.GetDamagePreview();
                if (preview.HasValue)
                    best = Mathf.Max(best, preview.Value.Type == DamagePreviewType.Random ? preview.Value.MaxAmount : preview.Value.Amount);
            }
            return best;
        }

        private static int Support(CardData card) =>
            card.Effects?.OfType<GainSupportEffect>().Sum(e => e.PreviewSupportAmount) ?? 0;

        private static string FantasyInitials(CardData card) =>
            string.Join(" ", Enum.GetValues(typeof(CardFantasy)).Cast<CardFantasy>()
                .Where(f => f != CardFantasy.None && card.Fantasies.HasFlag(f))
                .Select(f => f.ToString().Substring(0, 1)));

        private static string Flags(CardData card) =>
            (card.IsStarterCard ? "S" : "") + (card.IsUnlockable ? "U" : "") + (card.IsGeneratedOnly ? "G" : "") + (card.IsUpgraded ? "+" : "");

        private static string Blank(int value) => value == 0 ? "" : value.ToString();

        private static string SafeDescription(CardData card, bool upgraded = false)
        {
            try
            {
                return card.GetDescription(upgraded);
            }
            catch (Exception e)
            {
                return $"(description failed: {e.Message})";
            }
        }

        /// <summary>
        /// Every status a card's effects and passives name (applied, cleansed or checked), found
        /// by walking the effect graph, so a new wrapper effect needs no change here.
        /// ponytail: finds statuses held as a <see cref="StatusBehavior"/> field only. An effect
        /// that grants a status by itself (Glamour gain) isn't seen; give it a field if it matters.
        /// </summary>
        private static IEnumerable<StatusBehavior> StatusesUsed(CardData card)
        {
            var found = new Dictionary<string, StatusBehavior>();
            var seen = new HashSet<object>();
            void Walk(object node)
            {
                switch (node)
                {
                    case null:
                    case string:
                        return;
                    case StatusBehavior status:
                        found[status.Id] = status;
                        return;
                    case IEnumerable list:
                        foreach (var item in list)
                            Walk(item);
                        return;
                }
                if (!(node is BattleEffect || node is BattlePassive) || !seen.Add(node))
                    return;
                foreach (var field in node.GetType().GetFields(Fields))
                {
                    var type = field.FieldType;
                    if (typeof(StatusBehavior).IsAssignableFrom(type)
                        || typeof(BattleEffect).IsAssignableFrom(type)
                        || typeof(BattlePassive).IsAssignableFrom(type)
                        || (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string)))
                        Walk(field.GetValue(node));
                }
            }
            Walk(card.Effects);
            Walk(card.Passives);
            Walk(card.UpgradedEffects);
            Walk(card.UpgradedPassives);
            return found.Values.OrderBy(s => s.DisplayName);
        }

        // ---- Colours --------------------------------------------------------------------

        private static Color? TypeColor(CardType type) =>
            type switch
            {
                CardType.Pressure => new Color(0.25f, 0.66f, 0.42f),
                CardType.Rhetoric => new Color(0.75f, 0.22f, 0.17f),
                CardType.Policy => new Color(0.17f, 0.44f, 0.71f),
                CardType.Heckle => new Color(0.49f, 0.3f, 0.66f),
                _ => new Color(0.43f, 0.11f, 0.18f),
            };

        private static Color? RarityColor(CardRarity rarity) =>
            rarity switch
            {
                CardRarity.Rare => new Color(0.75f, 0.58f, 0.15f),
                CardRarity.Enhanced => new Color(0.45f, 0.5f, 0.58f),
                _ => new Color(0.35f, 0.35f, 0.35f),
            };

        private Color? HostilityColor(CardData card) =>
            HostilityOf(card).Kind switch
            {
                HostilityKind.Aggravates => new Color(0.65f, 0.2f, 0.15f),
                HostilityKind.Pacifies => new Color(0.2f, 0.55f, 0.3f),
                HostilityKind.None => (Color?)null,
                _ => new Color(0.7f, 0.5f, 0.1f),
            };

        private static bool Toggle(ref bool value, string label)
        {
            bool next = GUILayout.Toggle(value, label, EditorStyles.miniButton);
            if (next == value)
                return false;
            value = next;
            return true;
        }

        private static GUIStyle _italicMini;

        private static GUIStyle ItalicMini =>
            _italicMini ??= new GUIStyle(EditorStyles.wordWrappedMiniLabel) { fontStyle = FontStyle.Italic };
    }
}
