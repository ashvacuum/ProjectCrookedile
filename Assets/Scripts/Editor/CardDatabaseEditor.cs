using Crookedile.Data;
using UnityEditor;
using UnityEngine;
using Crookedile.Data.Cards;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using Sirenix.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Crookedile.Editor
{
    [CustomEditor(typeof(CardDatabase))]
    public class CardDatabaseEditor : OdinEditor
    {
        private CardDatabase database;
        private Vector2 scrollPosition;
        private List<CardData> filteredCards;
        private SortMode currentSortMode = SortMode.Name;
        private bool sortDescending = false;
        private string searchFilter = "";
        private CardType?   filterByType = null;
        private CardRarity? filterByRarity = null;
        private OriginType? filterByOrigin = null;
        private bool        filterStarterOnly = false;

        // View mode
        private ViewMode viewMode = ViewMode.Statistics;

        private enum SortMode
        {
            Name,
            HighestDamage,
            HighestComposure,
            MostEffects,
            CheapestCost,
            Type,
            Rarity
        }

        private enum ViewMode
        {
            Statistics,
            CardBrowser,
            NeedsSetup
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            database = (CardDatabase)target;
            RefreshFilteredCards();
        }

        public override void OnInspectorGUI()
        {
            if (database == null) return;

            DrawHeader();
            DrawViewModeSelector();

            EditorGUILayout.Space(10);

            switch (viewMode)
            {
                case ViewMode.Statistics:
                    DrawStatisticsView();
                    break;
                case ViewMode.CardBrowser:
                    DrawCardBrowserView();
                    break;
                case ViewMode.NeedsSetup:
                    DrawNeedsSetupView();
                    break;
            }

            EditorGUILayout.Space(10);
            DrawDefaultInspector();
        }

        #region Header

        private void DrawHeader()
        {
            SirenixEditorGUI.BeginBox();
            SirenixEditorGUI.BeginBoxHeader();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Card Database Manager", SirenixGUIStyles.BoldTitle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{database.Count} Cards", SirenixGUIStyles.BoldLabel);
            GUILayout.EndHorizontal();

            SirenixEditorGUI.EndBoxHeader();

            // Action buttons
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Refresh Database", GUILayout.Height(35)))
            {
                database.RefreshDatabase();
                RefreshFilteredCards();
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("Validate Cards", GUILayout.Height(35)))
            {
                ValidateCardDatabase(database);
                GUI.FocusControl(null);
            }

            GUILayout.EndHorizontal();

            SirenixEditorGUI.EndBox();
        }

        private void DrawViewModeSelector()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Toggle(viewMode == ViewMode.Statistics, "Statistics", SirenixGUIStyles.Button, GUILayout.Width(120), GUILayout.Height(25)))
            {
                viewMode = ViewMode.Statistics;
            }

            if (GUILayout.Toggle(viewMode == ViewMode.CardBrowser, "Card Browser", SirenixGUIStyles.Button, GUILayout.Width(120), GUILayout.Height(25)))
            {
                viewMode = ViewMode.CardBrowser;
                RefreshFilteredCards();
            }

            int needsSetupCount = database.GetAll().Count(c => c.NeedsConfiguration);
            string needsSetupLabel = needsSetupCount > 0
                ? $"Needs Setup ({needsSetupCount})"
                : "Needs Setup";
            if (GUILayout.Toggle(viewMode == ViewMode.NeedsSetup, needsSetupLabel,
                SirenixGUIStyles.Button, GUILayout.Width(160), GUILayout.Height(25)))
            {
                viewMode = ViewMode.NeedsSetup;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Statistics View

        private void DrawStatisticsView()
        {
            SirenixEditorGUI.BeginBox();
            SirenixEditorGUI.Title("Database Statistics", "", TextAlignment.Left, true);

            // Card Type Breakdown
            EditorGUILayout.Space(5);
            DrawStatSection("Card Types", () =>
            {
                foreach (CardType type in System.Enum.GetValues(typeof(CardType)))
                {
                    int count = database.GetByType(type).Count;
                    float percentage = database.Count > 0 ? (count / (float)database.Count) * 100f : 0f;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{type}:", GUILayout.Width(120));
                    DrawProgressBar(count, database.Count, $"{count} ({percentage:F1}%)");
                    GUILayout.EndHorizontal();
                }
            });

            EditorGUILayout.Space(10);

            // Rarity Breakdown
            DrawStatSection("Rarity Distribution", () =>
            {
                foreach (CardRarity rarity in System.Enum.GetValues(typeof(CardRarity)))
                {
                    int count = database.GetByRarity(rarity).Count;
                    float percentage = database.Count > 0 ? (count / (float)database.Count) * 100f : 0f;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{rarity}:", GUILayout.Width(120));
                    DrawProgressBar(count, database.Count, $"{count} ({percentage:F1}%)");
                    GUILayout.EndHorizontal();
                }
            });

            EditorGUILayout.Space(10);

            // Top Cards
            DrawTopCards();

            SirenixEditorGUI.EndBox();
        }

        private void DrawStatSection(string title, System.Action content)
        {
            SirenixEditorGUI.BeginVerticalList();
            GUILayout.Label(title, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            content();
            EditorGUI.indentLevel--;
            SirenixEditorGUI.EndVerticalList();
        }

        private void DrawProgressBar(int current, int max, string label)
        {
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
            float fillAmount = max > 0 ? (current / (float)max) : 0f;

            // Background
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.5f));

            // Fill
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * fillAmount, rect.height);
            EditorGUI.DrawRect(fillRect, new Color(0.3f, 0.7f, 1f, 0.6f));

            // Border
            SirenixEditorGUI.DrawBorders(rect, 1);

            // Label
            GUI.Label(rect, label, SirenixGUIStyles.CenteredWhiteMiniLabel);
        }

        private void DrawTopCards()
        {
            var allCards = database.GetAll();
            if (allCards.Count == 0) return;

            SirenixEditorGUI.BeginVerticalList();
            GUILayout.Label("Notable Cards", EditorStyles.boldLabel);

            // Highest Damage Card
            var highestDamageCard = allCards
                .OrderByDescending(c => GetMaxDamage(c))
                .FirstOrDefault();
            if (highestDamageCard != null && GetMaxDamage(highestDamageCard) > 0)
            {
                DrawNotableCard("Highest Damage", highestDamageCard, $"{GetMaxDamage(highestDamageCard)} damage");
            }

            // Most Composure Gain
            var mostComposureCard = allCards
                .OrderByDescending(c => GetMaxComposureGain(c))
                .FirstOrDefault();
            if (mostComposureCard != null && GetMaxComposureGain(mostComposureCard) > 0)
            {
                DrawNotableCard("Highest Composure", mostComposureCard, $"+{GetMaxComposureGain(mostComposureCard)} composure");
            }

            // Most Effects
            var mostEffectsCard = allCards
                .OrderByDescending(c => c.Effects?.Count ?? 0)
                .FirstOrDefault();
            if (mostEffectsCard != null && (mostEffectsCard.Effects?.Count ?? 0) > 0)
            {
                DrawNotableCard("Most Effects", mostEffectsCard, $"{mostEffectsCard.Effects.Count} effects");
            }

            // Cheapest Card
            var cheapestCard = allCards
                .Where(c => c.Costs != null && c.Costs.Count > 0)
                .OrderBy(c => GetMinCost(c))
                .FirstOrDefault();
            if (cheapestCard != null)
            {
                DrawNotableCard("Cheapest Card", cheapestCard, $"{GetMinCost(cheapestCard)} AP");
            }

            SirenixEditorGUI.EndVerticalList();
        }

        private void DrawNotableCard(string category, CardData card, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label($"{category}:", GUILayout.Width(140));

            if (GUILayout.Button(card.CardName, SirenixGUIStyles.MiniButton))
            {
                Selection.activeObject = card;
                EditorGUIUtility.PingObject(card);
            }

            GUILayout.Label($"({value})", SirenixGUIStyles.RightAlignedGreyMiniLabel, GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Card Browser View

        private void DrawCardBrowserView()
        {
            SirenixEditorGUI.BeginBox();

            DrawFilters();
            DrawSortControls();

            EditorGUILayout.Space(5);
            SirenixEditorGUI.HorizontalLineSeparator();
            EditorGUILayout.Space(5);

            DrawCardList();

            SirenixEditorGUI.EndBox();
        }

        private void DrawFilters()
        {
            SirenixEditorGUI.BeginVerticalList();
            GUILayout.Label("Filters", EditorStyles.boldLabel);

            // Search filter
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(60));
            string newSearch = EditorGUILayout.TextField(searchFilter);
            if (newSearch != searchFilter)
            {
                searchFilter = newSearch;
                RefreshFilteredCards();
            }

            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                searchFilter     = "";
                filterByType     = null;
                filterByRarity   = null;
                filterByOrigin   = null;
                filterStarterOnly = false;
                RefreshFilteredCards();
                GUI.FocusControl(null);
            }
            GUILayout.EndHorizontal();

            // Type filter
            GUILayout.BeginHorizontal();
            GUILayout.Label("Type:", GUILayout.Width(60));

            if (GUILayout.Toggle(filterByType == null, "All", SirenixGUIStyles.MiniButton))
            {
                if (filterByType != null)
                {
                    filterByType = null;
                    RefreshFilteredCards();
                }
            }

            foreach (CardType type in System.Enum.GetValues(typeof(CardType)))
            {
                if (GUILayout.Toggle(filterByType == type, type.ToString(), SirenixGUIStyles.MiniButton))
                {
                    if (filterByType != type)
                    {
                        filterByType = type;
                        RefreshFilteredCards();
                    }
                }
            }
            GUILayout.EndHorizontal();

            // Rarity filter
            GUILayout.BeginHorizontal();
            GUILayout.Label("Rarity:", GUILayout.Width(60));

            if (GUILayout.Toggle(filterByRarity == null, "All", SirenixGUIStyles.MiniButton))
            {
                if (filterByRarity != null)
                {
                    filterByRarity = null;
                    RefreshFilteredCards();
                }
            }

            foreach (CardRarity rarity in System.Enum.GetValues(typeof(CardRarity)))
            {
                if (GUILayout.Toggle(filterByRarity == rarity, rarity.ToString(), SirenixGUIStyles.MiniButton))
                {
                    if (filterByRarity != rarity)
                    {
                        filterByRarity = rarity;
                        RefreshFilteredCards();
                    }
                }
            }
            GUILayout.EndHorizontal();

            // Class (OriginType) filter
            GUILayout.BeginHorizontal();
            GUILayout.Label("Class:", GUILayout.Width(60));

            if (GUILayout.Toggle(filterByOrigin == null, "All", SirenixGUIStyles.MiniButton))
            {
                if (filterByOrigin != null)
                {
                    filterByOrigin = null;
                    RefreshFilteredCards();
                }
            }

            foreach (OriginType origin in System.Enum.GetValues(typeof(OriginType)))
            {
                if (GUILayout.Toggle(filterByOrigin == origin, origin.ToString(), SirenixGUIStyles.MiniButton))
                {
                    if (filterByOrigin != origin)
                    {
                        filterByOrigin = origin;
                        RefreshFilteredCards();
                    }
                }
            }
            GUILayout.EndHorizontal();

            // Starter card filter
            GUILayout.BeginHorizontal();
            GUILayout.Label("Starter:", GUILayout.Width(60));
            bool newStarterOnly = GUILayout.Toggle(filterStarterOnly, "Starter Only", SirenixGUIStyles.MiniButton, GUILayout.Width(90));
            if (newStarterOnly != filterStarterOnly)
            {
                filterStarterOnly = newStarterOnly;
                RefreshFilteredCards();
            }
            GUILayout.EndHorizontal();

            SirenixEditorGUI.EndVerticalList();
        }

        private void DrawSortControls()
        {
            EditorGUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Sort By:", GUILayout.Width(60));

            foreach (SortMode mode in System.Enum.GetValues(typeof(SortMode)))
            {
                bool isActive = currentSortMode == mode;
                string label = mode.ToString().Replace("Highest", "").Replace("Most", "").Replace("Cheapest", "");

                if (GUILayout.Toggle(isActive, label, SirenixGUIStyles.MiniButton))
                {
                    if (currentSortMode == mode)
                    {
                        sortDescending = !sortDescending;
                    }
                    else
                    {
                        currentSortMode = mode;
                        sortDescending = (mode == SortMode.HighestDamage || mode == SortMode.HighestComposure || mode == SortMode.MostEffects);
                    }
                    RefreshFilteredCards();
                }
            }

            GUILayout.FlexibleSpace();

            string orderIcon = sortDescending ? "↓" : "↑";
            if (GUILayout.Button(orderIcon, GUILayout.Width(30)))
            {
                sortDescending = !sortDescending;
                RefreshFilteredCards();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawCardList()
        {
            if (filteredCards == null || filteredCards.Count == 0)
            {
                SirenixEditorGUI.MessageBox("No cards found matching filters.", MessageType.Info);
                return;
            }

            GUILayout.Label($"Showing {filteredCards.Count} card(s)", SirenixGUIStyles.CenteredGreyMiniLabel);
            EditorGUILayout.Space(5);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(400));

            for (int i = 0; i < filteredCards.Count; i++)
            {
                var card = filteredCards[i];
                if (card == null) continue;

                DrawCardEntry(card, i);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCardEntry(CardData card, int index)
        {
            bool isOdd = index % 2 == 1;
            Color bgColor = isOdd ? new Color(0.25f, 0.25f, 0.25f, 0.3f) : new Color(0.2f, 0.2f, 0.2f, 0.2f);

            // Pre-calculate values to avoid conditional GUI calls
            int cost = GetMinCost(card);
            int effectCount = card.Effects?.Count ?? 0;
            int damage = GetMaxDamage(card);
            int composure = GetMaxComposureGain(card);

            // Use a simple rect approach instead of nested Begin/End
            Rect lineRect = EditorGUILayout.GetControlRect(GUILayout.Height(24));

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(lineRect, bgColor);
            }

            Rect contentRect = new Rect(lineRect.x + 5, lineRect.y + 2, lineRect.width - 10, lineRect.height - 4);

            float xOffset = contentRect.x;

            // Card name button
            Rect nameRect = new Rect(xOffset, contentRect.y, 150, 20);
            if (GUI.Button(nameRect, card.GetDisplayName(), SirenixGUIStyles.LeftAlignedWhiteMiniLabel))
            {
                Selection.activeObject = card;
                EditorGUIUtility.PingObject(card);
            }
            xOffset += 155;

            // Type badge
            DrawBadgeAtPosition(new Rect(xOffset, contentRect.y, 80, 18), card.CardType.ToString(), GetTypeColor(card.CardType));
            xOffset += 85;

            // Rarity badge
            DrawBadgeAtPosition(new Rect(xOffset, contentRect.y, 70, 18), card.Rarity.ToString(), GetRarityColor(card.Rarity));
            xOffset += 75;

            // Cost
            GUI.Label(new Rect(xOffset, contentRect.y, 50, 18), $"{cost} AP", SirenixGUIStyles.CenteredGreyMiniLabel);
            xOffset += 55;

            // Effects count
            GUI.Label(new Rect(xOffset, contentRect.y, 50, 18), $"{effectCount} FX", SirenixGUIStyles.CenteredGreyMiniLabel);
            xOffset += 55;

            // Damage
            string damageText = damage > 0 ? $"{damage} DMG" : "";
            GUI.Label(new Rect(xOffset, contentRect.y, 60, 18), damageText, SirenixGUIStyles.RightAlignedGreyMiniLabel);
            xOffset += 65;

            // Composure
            string composureText = composure > 0 ? $"+{composure} CMP" : "";
            GUI.Label(new Rect(xOffset, contentRect.y, 70, 18), composureText, SirenixGUIStyles.RightAlignedGreyMiniLabel);
        }

        private void DrawBadgeAtPosition(Rect rect, string label, Color color)
        {
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, color);
                SirenixEditorGUI.DrawBorders(rect, 1);
            }
            GUI.Label(rect, label, SirenixGUIStyles.CenteredWhiteMiniLabel);
        }

        #endregion

        #region Filtering and Sorting

        private void RefreshFilteredCards()
        {
            if (database == null) return;

            filteredCards = database.GetAll();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchFilter))
            {
                filteredCards = filteredCards.Where(c =>
                    c.CardName.IndexOf(searchFilter, System.StringComparison.OrdinalIgnoreCase) >= 0
                ).ToList();
            }

            if (filterByType.HasValue)
            {
                filteredCards = filteredCards.Where(c => c.CardType == filterByType.Value).ToList();
            }

            if (filterByRarity.HasValue)
            {
                filteredCards = filteredCards.Where(c => c.Rarity == filterByRarity.Value).ToList();
            }

            if (filterByOrigin.HasValue)
            {
                string originTag = filterByOrigin.Value.ToString().ToLower();
                filteredCards = filteredCards
                    .Where(c => c.HasTag(originTag) || c.HasTag("universal"))
                    .ToList();
            }

            if (filterStarterOnly)
            {
                filteredCards = filteredCards.Where(c => c.IsStarterCard).ToList();
            }

            // Apply sorting
            filteredCards = SortCards(filteredCards);
        }

        private List<CardData> SortCards(List<CardData> cards)
        {
            IEnumerable<CardData> sorted = currentSortMode switch
            {
                SortMode.Name => cards.OrderBy(c => c.CardName),
                SortMode.HighestDamage => cards.OrderBy(c => GetMaxDamage(c)),
                SortMode.HighestComposure => cards.OrderBy(c => GetMaxComposureGain(c)),
                SortMode.MostEffects => cards.OrderBy(c => c.Effects?.Count ?? 0),
                SortMode.CheapestCost => cards.OrderBy(c => GetMinCost(c)),
                SortMode.Type => cards.OrderBy(c => c.CardType),
                SortMode.Rarity => cards.OrderBy(c => c.Rarity),
                _ => cards.OrderBy(c => c.CardName)
            };

            if (sortDescending)
            {
                sorted = sorted.Reverse();
            }

            return sorted.ToList();
        }

        #endregion

        #region Helper Methods

        private int GetMaxDamage(CardData card)
        {
            if (card?.Effects == null) return 0;

            int maxDamage = 0;
            foreach (var effect in card.Effects)
            {
                if (effect.Category == EffectCategory.Damage)
                {
                    if (effect.DamageType == DamageType.FixedDamage)
                    {
                        maxDamage = Mathf.Max(maxDamage, effect.DamageAmount);
                    }
                    else if (effect.DamageType == DamageType.RandomDamage)
                    {
                        maxDamage = Mathf.Max(maxDamage, effect.RandomDamageMax);
                    }
                }
            }
            return maxDamage;
        }

        private int GetMaxComposureGain(CardData card)
        {
            if (card?.Effects == null) return 0;

            int totalComposure = 0;
            foreach (var effect in card.Effects)
            {
                if (effect.Category == EffectCategory.Resource && effect.ResourceType == ResourceEffectType.GainComposure)
                {
                    totalComposure += effect.ResourceAmount;
                }
            }
            return totalComposure;
        }

        private int GetMinCost(CardData card)
        {
            if (card?.Costs == null || card.Costs.Count == 0) return 0;

            int minCost = int.MaxValue;
            foreach (var cost in card.Costs)
            {
                if (cost.CostType == CostType.ActionPoints)
                {
                    minCost = Mathf.Min(minCost, cost.BaseAmount);
                }
            }
            return minCost == int.MaxValue ? 0 : minCost;
        }

        private Color GetTypeColor(CardType type)
        {
            return type switch
            {
                CardType.Pressure => new Color(0.3f, 0.8f, 0.3f),
                CardType.Rhetoric => new Color(0.9f, 0.3f, 0.3f),
                CardType.Policy   => new Color(0.3f, 0.5f, 0.9f),
                CardType.Status   => new Color(0.6f, 0.3f, 0.85f),
                CardType.Curse    => new Color(0.4f, 0.1f, 0.1f),
                _                 => Color.grey
            };
        }

        private Color GetRarityColor(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Basic => new Color(0.6f, 0.6f, 0.6f),
                CardRarity.Enhanced => new Color(0.4f, 0.7f, 1f),
                CardRarity.Rare => new Color(0.9f, 0.7f, 0.2f),
                _ => Color.grey
            };
        }

        #endregion

        #region Needs Setup View

        private void DrawNeedsSetupView()
        {
            var incompleteCards = database.GetAll()
                .Where(c => c.NeedsConfiguration)
                .OrderBy(c => c.CardName)
                .ToList();

            SirenixEditorGUI.BeginBox();

            // ── Header ──────────────────────────────────────────────────────
            SirenixEditorGUI.BeginBoxHeader();
            GUILayout.BeginHorizontal();

            if (incompleteCards.Count > 0)
            {
                var labelStyle = new GUIStyle(SirenixGUIStyles.BoldTitle);
                labelStyle.normal.textColor = new Color(1f, 0.65f, 0f); // orange
                GUILayout.Label($"⚠  {incompleteCards.Count} card(s) need Inspector setup", labelStyle);
            }
            else
            {
                var labelStyle = new GUIStyle(SirenixGUIStyles.BoldTitle);
                labelStyle.normal.textColor = new Color(0.4f, 0.85f, 0.4f); // green
                GUILayout.Label("✓  All cards are fully configured", labelStyle);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();          // close header horizontal
            SirenixEditorGUI.EndBoxHeader();

            EditorGUILayout.Space(5);

            // ── Card List ────────────────────────────────────────────────────
            if (incompleteCards.Count == 0)
            {
                EditorGUILayout.Space(10);
                GUILayout.Label("Nothing left to configure. Nice work!", SirenixGUIStyles.CenteredGreyMiniLabel);
                EditorGUILayout.Space(10);
                SirenixEditorGUI.EndBox();
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition,
                GUILayout.MaxHeight(500));

            foreach (var card in incompleteCards)
            {
                SirenixEditorGUI.BeginVerticalList();
                GUILayout.BeginHorizontal();

                // Card name + type pill
                GUILayout.BeginVertical();
                GUILayout.Label(card.CardName, EditorStyles.boldLabel);
                GUILayout.Label($"{card.CardType}  ·  {card.Rarity}", SirenixGUIStyles.LeftAlignedGreyLabel);
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                // Select button
                if (GUILayout.Button("Select", GUILayout.Width(60), GUILayout.Height(30)))
                {
                    Selection.activeObject = card;
                    EditorGUIUtility.PingObject(card);
                }

                GUILayout.EndHorizontal();

                // Configuration notes
                var notesStyle = new GUIStyle(EditorStyles.helpBox) { wordWrap = true };
                GUILayout.Label(card.ConfigurationNotes, notesStyle);

                SirenixEditorGUI.EndVerticalList();
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.EndScrollView();

            SirenixEditorGUI.EndBox();
        }

        #endregion

        #region Validation

        private void ValidateCardDatabase(CardDatabase database)
        {
            var allCards = database.GetAll();
            int issueCount = 0;
            System.Text.StringBuilder report = new System.Text.StringBuilder();

            report.AppendLine("Card Database Validation Report");
            report.AppendLine("================================\n");

            foreach (var card in allCards)
            {
                bool hasIssues = false;
                System.Text.StringBuilder cardIssues = new System.Text.StringBuilder();

                // Check for empty card name
                if (string.IsNullOrWhiteSpace(card.CardName))
                {
                    cardIssues.AppendLine("  - Card name is empty");
                    hasIssues = true;
                }

                // Check for empty costs
                if (card.Costs == null || card.Costs.Count == 0)
                {
                    cardIssues.AppendLine("  - No costs defined");
                    hasIssues = true;
                }

                // Check for empty effects
                if (card.Effects == null || card.Effects.Count == 0)
                {
                    cardIssues.AppendLine("  - No effects defined");
                    hasIssues = true;
                }

                if (hasIssues)
                {
                    issueCount++;
                    string displayName = string.IsNullOrWhiteSpace(card.CardName) ?
                        $"[Unnamed Card - {card.ID.Substring(0, 8)}]" :
                        card.CardName;
                    report.AppendLine($"Card: {displayName}");
                    report.Append(cardIssues.ToString());
                    report.AppendLine();
                }
            }

            if (issueCount == 0)
            {
                report.AppendLine("All cards passed validation!");
                Debug.Log(report.ToString());
                EditorUtility.DisplayDialog("Card Validation",
                    $"All {allCards.Count} cards passed validation!",
                    "OK");
            }
            else
            {
                report.Insert(0, $"Found {issueCount} card(s) with issues out of {allCards.Count} total cards.\n\n");
                Debug.LogWarning(report.ToString());
                EditorUtility.DisplayDialog("Card Validation",
                    $"Found {issueCount} card(s) with issues.\nCheck the Console for details.",
                    "OK");
            }
        }

        #endregion
    }
}
