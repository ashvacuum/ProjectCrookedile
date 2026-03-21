using Crookedile.Data;
using Crookedile.Data.Enemy;
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
            NeedsSetup,
            LegacyEffects,
            InDevelopment,
            PassiveIssues,
            EnemyAudit
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
                case ViewMode.LegacyEffects:
                    DrawLegacyEffectsView();
                    break;
                case ViewMode.InDevelopment:
                    DrawInDevelopmentView();
                    break;
                case ViewMode.PassiveIssues:
                    DrawPassiveIssuesView();
                    break;
                case ViewMode.EnemyAudit:
                    DrawEnemyAuditView();
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
            // ── Row 1: Browse ──────────────────────────────────────────────────
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Toggle(viewMode == ViewMode.Statistics, "Statistics",
                    SirenixGUIStyles.Button, GUILayout.Width(120), GUILayout.Height(25)))
                viewMode = ViewMode.Statistics;

            if (GUILayout.Toggle(viewMode == ViewMode.CardBrowser, "Card Browser",
                    SirenixGUIStyles.Button, GUILayout.Width(120), GUILayout.Height(25)))
            {
                viewMode = ViewMode.CardBrowser;
                RefreshFilteredCards();
            }

            int needsSetupCount = database.GetAll().Count(c => c.NeedsConfiguration);
            string needsSetupLabel = needsSetupCount > 0 ? $"Needs Setup ({needsSetupCount})" : "Needs Setup";
            if (GUILayout.Toggle(viewMode == ViewMode.NeedsSetup, needsSetupLabel,
                    SirenixGUIStyles.Button, GUILayout.Width(150), GUILayout.Height(25)))
                viewMode = ViewMode.NeedsSetup;

            int legacyCount = database.GetAll().Count(c => c.UsesLegacyEffects);
            string legacyLabel = legacyCount > 0 ? $"Legacy Effects ({legacyCount})" : "Legacy Effects";
            if (GUILayout.Toggle(viewMode == ViewMode.LegacyEffects, legacyLabel,
                    SirenixGUIStyles.Button, GUILayout.Width(150), GUILayout.Height(25)))
                viewMode = ViewMode.LegacyEffects;

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            // ── Row 2: Quality / Audit ─────────────────────────────────────────
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            int devCount = database.GetAll().Count(c => c.IsInDevelopment);
            string devLabel = devCount > 0 ? $"In Development ({devCount})" : "In Development";
            if (GUILayout.Toggle(viewMode == ViewMode.InDevelopment, devLabel,
                    SirenixGUIStyles.Button, GUILayout.Width(150), GUILayout.Height(25)))
                viewMode = ViewMode.InDevelopment;

            int passiveIssueCount = database.GetAll().Count(c => GetCardPassiveIssues(c).Count > 0);
            string passiveLabel = passiveIssueCount > 0 ? $"Passive Issues ({passiveIssueCount})" : "Passive Issues";
            if (GUILayout.Toggle(viewMode == ViewMode.PassiveIssues, passiveLabel,
                    SirenixGUIStyles.Button, GUILayout.Width(150), GUILayout.Height(25)))
                viewMode = ViewMode.PassiveIssues;

            if (GUILayout.Toggle(viewMode == ViewMode.EnemyAudit, "Enemy Audit",
                    SirenixGUIStyles.Button, GUILayout.Width(120), GUILayout.Height(25)))
                viewMode = ViewMode.EnemyAudit;

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

        private void DrawLegacyEffectsView()
        {
            var legacyCards = database.GetAll()
                .Where(c => c.UsesLegacyEffects)
                .OrderBy(c => c.CardName)
                .ToList();

            SirenixEditorGUI.BeginBox();

            // ── Header ──────────────────────────────────────────────────────
            SirenixEditorGUI.BeginBoxHeader();
            GUILayout.BeginHorizontal();

            if (legacyCards.Count > 0)
            {
                var labelStyle = new GUIStyle(SirenixGUIStyles.BoldTitle);
                labelStyle.normal.textColor = new Color(1f, 0.65f, 0f); // orange
                GUILayout.Label($"⚠  {legacyCards.Count} card(s) still using legacy CardEffect system", labelStyle);
            }
            else
            {
                var labelStyle = new GUIStyle(SirenixGUIStyles.BoldTitle);
                labelStyle.normal.textColor = new Color(0.4f, 0.85f, 0.4f); // green
                GUILayout.Label("✓  All cards use the new BattleEffect system", labelStyle);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            SirenixEditorGUI.EndBoxHeader();

            EditorGUILayout.Space(5);

            // ── Empty state ──────────────────────────────────────────────────
            if (legacyCards.Count == 0)
            {
                EditorGUILayout.Space(10);
                GUILayout.Label("Migration complete. Nothing left to convert!", SirenixGUIStyles.CenteredGreyMiniLabel);
                EditorGUILayout.Space(10);
                SirenixEditorGUI.EndBox();
                return;
            }

            // ── Migration hint ───────────────────────────────────────────────
            EditorGUILayout.HelpBox(
                "Run  Crookedile → Tools → Migrate Effects to New System  to auto-convert these cards.",
                MessageType.Info);
            EditorGUILayout.Space(4);

            // ── Card List ────────────────────────────────────────────────────
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition,
                GUILayout.MaxHeight(500));

            foreach (var card in legacyCards)
            {
                SirenixEditorGUI.BeginVerticalList();
                GUILayout.BeginHorizontal();

                // Card name + type + effect count
                GUILayout.BeginVertical();
                GUILayout.Label(card.CardName, EditorStyles.boldLabel);
                GUILayout.Label(
                    $"{card.CardType}  ·  {card.Rarity}  ·  {card.Effects.Count} legacy effect(s)",
                    SirenixGUIStyles.LeftAlignedGreyLabel);
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                // Select button
                if (GUILayout.Button("Select", GUILayout.Width(60), GUILayout.Height(30)))
                {
                    Selection.activeObject = card;
                    EditorGUIUtility.PingObject(card);
                }

                GUILayout.EndHorizontal();
                SirenixEditorGUI.EndVerticalList();
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.EndScrollView();

            SirenixEditorGUI.EndBox();
        }

        private void DrawInDevelopmentView()
        {
            var devCards = database.GetAll()
                .Where(c => c.IsInDevelopment)
                .OrderBy(c => c.CardName)
                .ToList();

            SirenixEditorGUI.BeginBox();

            // ── Header ──────────────────────────────────────────────────────
            SirenixEditorGUI.BeginBoxHeader();
            GUILayout.BeginHorizontal();

            if (devCards.Count > 0)
            {
                var labelStyle = new GUIStyle(SirenixGUIStyles.BoldTitle);
                labelStyle.normal.textColor = new Color(1f, 0.65f, 0f); // orange
                GUILayout.Label($"⚠  {devCards.Count} card(s) have no artwork assigned", labelStyle);
            }
            else
            {
                var labelStyle = new GUIStyle(SirenixGUIStyles.BoldTitle);
                labelStyle.normal.textColor = new Color(0.4f, 0.85f, 0.4f); // green
                GUILayout.Label("✓  All cards have artwork assigned", labelStyle);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            SirenixEditorGUI.EndBoxHeader();

            EditorGUILayout.Space(5);

            // ── Empty state ──────────────────────────────────────────────────
            if (devCards.Count == 0)
            {
                EditorGUILayout.Space(10);
                GUILayout.Label("All cards have artwork and are ready for gameplay.", SirenixGUIStyles.CenteredGreyMiniLabel);
                EditorGUILayout.Space(10);
                SirenixEditorGUI.EndBox();
                return;
            }

            // ── Info note ────────────────────────────────────────────────────
            EditorGUILayout.HelpBox(
                "These cards are excluded from reward pools and card-choice panels until artwork is assigned.",
                MessageType.Info);
            EditorGUILayout.Space(4);

            // ── Card List ────────────────────────────────────────────────────
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition,
                GUILayout.MaxHeight(500));

            foreach (var card in devCards)
            {
                SirenixEditorGUI.BeginVerticalList();
                GUILayout.BeginHorizontal();

                // Card name + type + rarity
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
                SirenixEditorGUI.EndVerticalList();
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.EndScrollView();

            SirenixEditorGUI.EndBox();
        }

        #endregion

        #region Passive Issues View

        private void DrawPassiveIssuesView()
        {
            var cardsWithIssues = database.GetAll()
                .Select(c => (card: c, issues: GetCardPassiveIssues(c)))
                .Where(x => x.issues.Count > 0)
                .OrderBy(x => x.card.CardName)
                .ToList();

            SirenixEditorGUI.BeginBox();

            // ── Header ──────────────────────────────────────────────────────
            SirenixEditorGUI.BeginBoxHeader();
            GUILayout.BeginHorizontal();

            if (cardsWithIssues.Count > 0)
            {
                var labelStyle = new GUIStyle(SirenixGUIStyles.BoldTitle);
                labelStyle.normal.textColor = new Color(1f, 0.65f, 0f);
                GUILayout.Label($"⚠  {cardsWithIssues.Count} card(s) have passive or behavior issues", labelStyle);
            }
            else
            {
                var labelStyle = new GUIStyle(SirenixGUIStyles.BoldTitle);
                labelStyle.normal.textColor = new Color(0.4f, 0.85f, 0.4f);
                GUILayout.Label("✓  All card passives and effects are configured", labelStyle);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            SirenixEditorGUI.EndBoxHeader();

            EditorGUILayout.Space(5);

            if (cardsWithIssues.Count == 0)
            {
                EditorGUILayout.Space(10);
                GUILayout.Label("No passive or behavior issues found.", SirenixGUIStyles.CenteredGreyMiniLabel);
                EditorGUILayout.Space(10);
                SirenixEditorGUI.EndBox();
                return;
            }

            EditorGUILayout.HelpBox(
                "Cards listed here either have no behavior at all, or have passives that are misconfigured " +
                "(missing trigger, no effects). Unplayable cards (Curse / Status) with empty effects are expected.",
                MessageType.Info);
            EditorGUILayout.Space(4);

            // ── Card list ────────────────────────────────────────────────────
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(500));

            foreach (var (card, issues) in cardsWithIssues)
            {
                SirenixEditorGUI.BeginVerticalList();
                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();
                GUILayout.Label(card.CardName, EditorStyles.boldLabel);
                GUILayout.Label($"{card.CardType}  ·  {card.Rarity}", SirenixGUIStyles.LeftAlignedGreyLabel);
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Select", GUILayout.Width(60), GUILayout.Height(30)))
                {
                    Selection.activeObject = card;
                    EditorGUIUtility.PingObject(card);
                }

                GUILayout.EndHorizontal();

                foreach (var issue in issues)
                    GUILayout.Label($"• {issue}", EditorStyles.helpBox);

                SirenixEditorGUI.EndVerticalList();
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.EndScrollView();
            SirenixEditorGUI.EndBox();
        }

        /// <summary>
        /// Returns a list of human-readable issues for a card's passive and effect configuration.
        /// An empty list means the card is clean.
        /// </summary>
        private static List<string> GetCardPassiveIssues(CardData card)
        {
            var issues = new List<string>();

            bool hasNewEffects    = card.NewEffects != null && card.NewEffects.Count > 0;
            bool hasLegacyEffects = card.Effects    != null && card.Effects.Count    > 0;
            bool hasPassives      = card.Passives   != null && card.Passives.Count   > 0;

            // Card with no behavior at all (and can theoretically be played)
            if (!hasNewEffects && !hasLegacyEffects && !hasPassives && !card.IsUnplayable)
                issues.Add("No effects or passives — card has no behavior when played");

            // Per-passive checks
            if (card.Passives == null) return issues;

            for (int i = 0; i < card.Passives.Count; i++)
            {
                var passive = card.Passives[i];
                if (passive == null)
                {
                    issues.Add($"Passive [{i}]: null entry in list");
                    continue;
                }

                if (passive.Trigger == null)
                    issues.Add($"Passive '{passive.Name}': no trigger set — will never fire");

                if (passive.Effects == null || passive.Effects.Count == 0)
                    issues.Add($"Passive '{passive.Name}': has trigger but no effects — fires silently");
            }

            return issues;
        }

        #endregion

        #region Enemy Audit View

        private void DrawEnemyAuditView()
        {
            // Load all enemy and move assets from the project
            var enemies = AssetDatabase.FindAssets("t:EnemyData")
                .Select(g => AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(e => e != null)
                .OrderBy(e => e.EnemyName)
                .ToList();

            var moves = AssetDatabase.FindAssets("t:EnemyMoveData")
                .Select(g => AssetDatabase.LoadAssetAtPath<EnemyMoveData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m => m != null)
                .OrderBy(m => m.MoveName)
                .ToList();

            var enemiesWithIssues = enemies
                .Select(e => (enemy: e, issues: GetEnemyIssues(e)))
                .Where(x => x.issues.Count > 0)
                .ToList();

            var movesWithIssues = moves
                .Select(m => (move: m, issues: GetMoveIssues(m)))
                .Where(x => x.issues.Count > 0)
                .ToList();

            int totalIssues = enemiesWithIssues.Count + movesWithIssues.Count;

            SirenixEditorGUI.BeginBox();

            // ── Header ──────────────────────────────────────────────────────
            SirenixEditorGUI.BeginBoxHeader();
            GUILayout.BeginHorizontal();

            if (totalIssues > 0)
            {
                var labelStyle = new GUIStyle(SirenixGUIStyles.BoldTitle);
                labelStyle.normal.textColor = new Color(1f, 0.65f, 0f);
                GUILayout.Label(
                    $"⚠  {enemiesWithIssues.Count} enemy issue(s)  ·  {movesWithIssues.Count} move issue(s)",
                    labelStyle);
            }
            else
            {
                var labelStyle = new GUIStyle(SirenixGUIStyles.BoldTitle);
                labelStyle.normal.textColor = new Color(0.4f, 0.85f, 0.4f);
                GUILayout.Label($"✓  All {enemies.Count} enemies and {moves.Count} moves are ready", labelStyle);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"{enemies.Count} enemies  ·  {moves.Count} moves",
                SirenixGUIStyles.RightAlignedGreyMiniLabel);
            GUILayout.EndHorizontal();
            SirenixEditorGUI.EndBoxHeader();

            EditorGUILayout.Space(5);

            if (totalIssues == 0)
            {
                EditorGUILayout.Space(10);
                GUILayout.Label("All enemies and moves are ready for gameplay.",
                    SirenixGUIStyles.CenteredGreyMiniLabel);
                EditorGUILayout.Space(10);
                SirenixEditorGUI.EndBox();
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(500));

            // ── Enemy issues ─────────────────────────────────────────────────
            if (enemiesWithIssues.Count > 0)
            {
                GUILayout.Label("Enemies", EditorStyles.boldLabel);
                EditorGUILayout.Space(4);

                foreach (var (enemy, issues) in enemiesWithIssues)
                {
                    SirenixEditorGUI.BeginVerticalList();
                    GUILayout.BeginHorizontal();

                    GUILayout.BeginVertical();
                    GUILayout.Label(enemy.EnemyName, EditorStyles.boldLabel);
                    GUILayout.Label(
                        $"Resolve: {enemy.MaxResolve}  ·  Moves: {enemy.Moves?.Count ?? 0}",
                        SirenixGUIStyles.LeftAlignedGreyLabel);
                    GUILayout.EndVertical();

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Select", GUILayout.Width(60), GUILayout.Height(30)))
                    {
                        Selection.activeObject = enemy;
                        EditorGUIUtility.PingObject(enemy);
                    }

                    GUILayout.EndHorizontal();

                    foreach (var issue in issues)
                        GUILayout.Label($"• {issue}", EditorStyles.helpBox);

                    SirenixEditorGUI.EndVerticalList();
                    EditorGUILayout.Space(4);
                }

                EditorGUILayout.Space(8);
            }

            // ── Move issues ──────────────────────────────────────────────────
            if (movesWithIssues.Count > 0)
            {
                GUILayout.Label("Enemy Moves", EditorStyles.boldLabel);
                EditorGUILayout.Space(4);

                foreach (var (move, issues) in movesWithIssues)
                {
                    SirenixEditorGUI.BeginVerticalList();
                    GUILayout.BeginHorizontal();

                    string displayName = string.IsNullOrWhiteSpace(move.MoveName)
                        ? "[Unnamed Move]"
                        : move.MoveName;

                    GUILayout.BeginVertical();
                    GUILayout.Label(displayName, EditorStyles.boldLabel);
                    GUILayout.Label($"{move.MoveType}", SirenixGUIStyles.LeftAlignedGreyLabel);
                    GUILayout.EndVertical();

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Select", GUILayout.Width(60), GUILayout.Height(30)))
                    {
                        Selection.activeObject = move;
                        EditorGUIUtility.PingObject(move);
                    }

                    GUILayout.EndHorizontal();

                    foreach (var issue in issues)
                        GUILayout.Label($"• {issue}", EditorStyles.helpBox);

                    SirenixEditorGUI.EndVerticalList();
                    EditorGUILayout.Space(4);
                }
            }

            EditorGUILayout.EndScrollView();
            SirenixEditorGUI.EndBox();
        }

        /// <summary>Returns validation issues for an enemy asset.</summary>
        private static List<string> GetEnemyIssues(EnemyData enemy)
        {
            var issues = new List<string>();

            if (enemy.Portrait == null)
                issues.Add("Missing portrait — battle UI will show a broken image slot");

            if (enemy.Moves == null || enemy.Moves.Count == 0)
                issues.Add("No moves defined — enemy cannot act on their turn");
            else
            {
                for (int i = 0; i < enemy.Moves.Count; i++)
                    if (enemy.Moves[i] == null)
                        issues.Add($"Move slot [{i}] is null — will cause a NullReferenceException at runtime");
            }

            if (enemy.MaxResolve <= 0)
                issues.Add($"MaxResolve is {enemy.MaxResolve} — enemy will be instantly defeated");

            return issues;
        }

        /// <summary>Returns validation issues for an enemy move asset.</summary>
        private static List<string> GetMoveIssues(EnemyMoveData move)
        {
            var issues = new List<string>();

            if (string.IsNullOrWhiteSpace(move.MoveName))
                issues.Add("No move name — intent display will be blank in logs and debug UI");

            if (string.IsNullOrWhiteSpace(move.IntentDescription))
                issues.Add("No intent description — player cannot see what this move will do");

            bool hasNewEffects    = move.NewEffects != null && move.NewEffects.Count > 0;
            bool hasLegacyEffects = move.Effects    != null && move.Effects.Count    > 0;

            if (!hasNewEffects && !hasLegacyEffects && move.MoveType != EnemyMoveType.SummonMinion)
                issues.Add("No effects defined — move resolves but does nothing");

            if (move.MoveType == EnemyMoveType.SummonMinion && move.MinionToSummon == null)
                issues.Add("SummonMinion move has no MinionToSummon set — summon will silently fail");

            return issues;
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
