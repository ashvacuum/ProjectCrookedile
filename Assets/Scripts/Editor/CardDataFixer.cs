#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Crookedile.Data;
using Crookedile.Data.Cards;

namespace Crookedile.Editor
{
    /// <summary>
    /// One-click tool to stamp all 19 starter card assets with:
    ///   • IsStarterCard = true
    ///   • Correct origin tag  (used by CardDatabase.GetStarterDeck)
    ///   • Mechanical description text
    ///
    /// Open via: Tools → Crookedile → Fix Starter Card Data
    /// </summary>
    public class CardDataFixer : EditorWindow
    {
        #region Card Metadata
        // Keyed by asset name (matches filename in Resources/Cards/).
        // originTag must match OriginType.ToString().ToLower() used in CardDatabase.GetStarterDeck().

        private static readonly Dictionary<string, CardMeta> CardMetadata = new()
        {
        #endregion

            #region Faith Leader
            ["Find Common Ground"] = new(
                "faithleader",
                "Apply 3 pressure.",
                "Sometimes all it takes is a smile."
            ),

            ["Blessing"] = new(
                "faithleader",
                "Raise Opinion equal to your Support. Consume all Support.",
                "The congregation holds its breath."
            ),

            ["Accusation"] = new(
                "faithleader",
                "Apply 4 pressure. Gain 1 Hostility.",
                "Righteous anger, barely contained."
            ),

            ["Deflect"] = new(
                "faithleader",
                "Gain 3 Support. Reduce Hostility by 1.",
                "Grace under fire."
            ),

            ["Gather Thoughts"] = new(
                "faithleader",
                "Gain 4 Support.",
                "A moment of quiet before the storm."
            ),

            #endregion

            #region Nepo Baby
            ["Family Name"] = new("nepobaby", "Apply 3 pressure.", "Do you know who my father is?"),

            ["Inherited Privelege"] = new(
                "nepobaby", // asset typo — keep as-is
                "Apply 5 pressure. Draw 1 card.",
                "Some doors open themselves."
            ),

            ["Pull Strings"] = new(
                "nepobaby",
                "Apply 4 pressure. Gain 1 Hostility.",
                "Everyone has a price. Yours is just lower."
            ),

            ["Call In Favor"] = new(
                "nepobaby",
                "Draw 2 cards.",
                "The account was always in the black."
            ),

            ["Backroom Deal"] = new(
                "nepobaby",
                "Draw 2 cards. Gain 1 Action Point next turn.",
                "Nothing illegal about a private meeting."
            ),

            ["Dynasty Network"] = new(
                "nepobaby",
                "Discard 1 card. Draw 2 cards.",
                "One call, a hundred doors."
            ),

            ["Trust Fund"] = new(
                "nepobaby",
                "Gain 2 Support. Gain 1 Action Point this turn.",
                "Family always provides."
            ),

            #endregion

            #region Actor
            ["Charming Gambit"] = new(
                "actor",
                "Apply 3 pressure. 50% chance: Draw 1 card.",
                "High risk, higher cheekbones."
            ),

            ["All or Nothing"] = new(
                "actor",
                "Apply 3–9 pressure (random).",
                "Every performance is a gamble."
            ),

            ["Bold Accusation"] = new(
                "actor",
                "Apply 5 pressure. Gain 2 Hostility.",
                "Critics said it was too much. It worked."
            ),

            ["Spotlight Hog"] = new(
                "actor",
                "Apply 6 pressure. Gain 3 Support. Gain 2 Hostility.",
                "They can't look away. Neither can you."
            ),

            ["High Stakes"] = new(
                "actor",
                "Discard your hand. Draw 3 cards.",
                "Burn it down and start over."
            ),

            ["Ego Trip"] = new(
                "actor",
                "Gain Support equal to your Hostility. (Hostility is not reduced.)",
                "Turn the wounds into weapons."
            ),

            ["Fan Favorite"] = new(
                "actor",
                "Lose 3 Support. Reduce Hostility by 3.",
                "They love you. Remind yourself of that."
            ),
        };

            #endregion

        #region Editor Window
        [MenuItem("Tools/Crookedile/Fix Starter Card Data")]
        public static void ShowWindow()
        {
            GetWindow<CardDataFixer>("Card Data Fixer");
        }

        private Vector2 _scroll;

        private void OnGUI()
        {
            GUILayout.Label("Starter Card Data Fixer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This tool stamps all 19 starter card assets with:\n"
                    + "  • IsStarterCard = true\n"
                    + "  • Origin tag (faithleader / nepobaby / actor)\n"
                    + "  • Flavor text\n\n"
                    + "Note: Mechanical descriptions are now auto-generated from card effects at runtime.\n\n"
                    + "Cards are loaded from Assets/Resources/Cards/.\n"
                    + "Safe to run multiple times — idempotent.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            if (GUILayout.Button("Fix All Starter Cards", GUILayout.Height(40)))
                FixAllCards();

            EditorGUILayout.Space();

            if (GUILayout.Button("Log Card Status (dry run)", GUILayout.Height(30)))
                LogCardStatus();
        }

        #endregion

        #region Fix Logic
        private static void FixAllCards()
        {
            int fixed_count = 0;
            int missing = 0;

            foreach (var kvp in CardMetadata)
            {
                string assetName = kvp.Key;
                CardMeta meta = kvp.Value;

                string path = $"Assets/Resources/Cards/{assetName}.asset";
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);

                if (card == null)
                {
                    Debug.LogWarning($"[CardDataFixer] Not found at '{path}' — skipping.");
                    missing++;
                    continue;
                }

                bool changed = false;

                // IsStarterCard
                if (!card.IsStarterCard)
                {
                    SetField(card, "_isStarterCard", true);
                    changed = true;
                }

                // Description is now auto-generated from card effects — no longer stamped here.

                // FlavorText
                if (card.FlavorText != meta.flavorText)
                {
                    SetField(card, "_flavorText", meta.flavorText);
                    changed = true;
                }

                // Tags — add origin tag if missing
                var so = new SerializedObject(card);
                var tagsProp = so.FindProperty("_tags");
                bool hasTag = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue == meta.originTag)
                    {
                        hasTag = true;
                        break;
                    }
                }

                if (!hasTag)
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue =
                        meta.originTag;
                    so.ApplyModifiedProperties();
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(card);
                    fixed_count++;
                    Debug.Log($"[CardDataFixer] Fixed: {assetName} ({meta.originTag})");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CardDataFixer] Done. Fixed {fixed_count} cards. Missing: {missing}.");
            EditorUtility.DisplayDialog(
                "Card Data Fixer",
                $"Fixed {fixed_count} card(s).\nMissing: {missing} (check Console).",
                "OK"
            );
        }

        private static void LogCardStatus()
        {
            Debug.Log("[CardDataFixer] === Card Status Report ===");
            foreach (var kvp in CardMetadata)
            {
                string path = $"Assets/Resources/Cards/{kvp.Key}.asset";
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);

                if (card == null)
                {
                    Debug.LogWarning($"  MISSING: {kvp.Key}");
                    continue;
                }

                string status =
                    $"  {kvp.Key}: "
                    + $"IsStarter={card.IsStarterCard}, "
                    + $"Tags=[{string.Join(",", card.Tags)}], "
                    + $"Artwork={(card.Artwork == null ? "MISSING" : "OK")}";
                Debug.Log(status);
            }
        }

        #endregion

        #region Helpers
        /// <summary>
        /// Sets a private serialized field on a ScriptableObject using SerializedObject.
        /// </summary>
        private static void SetField(ScriptableObject obj, string fieldName, object value)
        {
            var so = new SerializedObject(obj);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[CardDataFixer] Field '{fieldName}' not found on {obj.name}");
                return;
            }

            switch (value)
            {
                case bool b:
                    prop.boolValue = b;
                    break;
                case string s:
                    prop.stringValue = s;
                    break;
                case int i:
                    prop.intValue = i;
                    break;
            }

            so.ApplyModifiedProperties();
        }

        #endregion

        #region Data
        private readonly struct CardMeta
        {
            public readonly string originTag;
            public readonly string description;
            public readonly string flavorText;

            public CardMeta(string originTag, string description, string flavorText)
            {
                this.originTag = originTag;
                this.description = description;
                this.flavorText = flavorText;
            }
        }
    }
}
        #endregion
#endif
