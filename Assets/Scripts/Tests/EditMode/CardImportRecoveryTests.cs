using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Crookedile.Data.Cards;
using Crookedile.Editor.Database;
using Crookedile.EditorTools;
using Crookedile.Gameplay.Battle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Crookedile.Tests
{
    public class CardImportRecoveryTests
    {
        private static readonly string[] CardsWithNewEffects =
        {
            "Charm Offensive",
            "Second Take",
            "Signature Catchphrase",
            "Paid-Off Promises",
            "Overpromise",
            "Never Meet Your Heroes",
            "Media Training",
            "Line of Credit",
            "Open Tab",
            "Bailout",
            "Too Big to Fail",
            "Rain Check",
            "Payment Holiday",
            "Overdraft",
            "NepoBaby/Enhanced/Legacy Admission",
            "NepoBaby/Enhanced/Smooth Operator",
            "NepoBaby/Enhanced/Executive Privilege",
        };

        [UnityTest]
        public IEnumerator DatabaseDrawsEffectListContents(
            [ValueSource(nameof(CardsWithNewEffects))] string cardName
        )
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("This rendering check requires a graphics-enabled Unity editor.");

            var configType = System.Type.GetType(
                "Sirenix.OdinInspector.Editor.GeneralDrawerConfig, Sirenix.OdinInspector.Editor"
            );
            var config = configType
                .GetProperty(
                    "Instance",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy
                )
                .GetValue(null);
            var toolkitSetting = configType.GetProperty("EnableUIToolkitSupport");
            bool originalSetting = (bool)toolkitSetting.GetValue(config);
            toolkitSetting.SetValue(config, true);
            var window = ScriptableObject.CreateInstance<DatabaseWindow>();
            try
            {
                window.position = new Rect(0, 0, 1200, 900);
                window.Show();
                var tabs = (System.Collections.Generic.List<ContentTab>)
                    typeof(DatabaseWindow)
                        .GetField("_tabs", BindingFlags.NonPublic | BindingFlags.Instance)
                        .GetValue(window);
                var tab = (CardsTab)tabs[0];
                tab.Reload();
                var card = AssetDatabase.LoadAssetAtPath<CardData>(
                    cardName.StartsWith("NepoBaby/")
                        ? $"Assets/Data/Cards/{cardName}.asset"
                        : $"Assets/Data/Cards/Celebrity/GlamourIou/{cardName}.asset"
                );
                typeof(ContentTab<CardData>)
                    .GetMethod("SelectLater", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(tab, new object[] { card });
                for (int i = 0; i < 5; i++)
                {
                    window.Repaint();
                    yield return null;
                }
                var inspector = (UnityEditor.Editor)
                    typeof(ContentTab<CardData>)
                        .GetField("_inspector", BindingFlags.NonPublic | BindingFlags.Instance)
                        .GetValue(tab);
                Assert.IsNotNull(inspector);
                var tree = inspector.GetType().GetProperty("Tree").GetValue(inspector);
                var enumerate = tree.GetType()
                    .GetMethod("EnumerateTree", new[] { typeof(bool), typeof(bool) });
                var properties = ((IEnumerable)enumerate.Invoke(tree, new object[] { true, false }))
                    .Cast<object>()
                    .ToArray();
                var effectProperties = new System.Collections.Generic.List<object>();
                foreach (var property in properties)
                {
                    var state = property.GetType().GetProperty("State").GetValue(property);
                    state.GetType().GetProperty("Expanded").SetValue(state, true);
                    var entry = property.GetType().GetProperty("ValueEntry").GetValue(property);
                    if (
                        entry != null
                        && entry.GetType().GetProperty("WeakSmartValue").GetValue(entry)
                            is BattleEffect
                    )
                        effectProperties.Add(property);
                }
                Assert.IsNotEmpty(
                    effectProperties,
                    "The card must expose its base, upgraded, or passive effects."
                );
                for (int i = 0; i < 20; i++)
                {
                    window.Repaint();
                    yield return null;
                }
                foreach (var property in effectProperties)
                {
                    var rect = (Rect)
                        property.GetType().GetProperty("LastDrawnValueRect").GetValue(property);
                    var path = (string)property.GetType().GetProperty("Path").GetValue(property);
                    Assert.IsFalse(
                        float.IsNaN(rect.y),
                        path + " must have a valid layout position."
                    );
                    Assert.Greater(rect.height, 0, path + " must have a visible layout height.");
                }
                Assert.IsTrue(
                    (bool)toolkitSetting.GetValue(config),
                    "Database drawing must restore Odin's UI Toolkit preference."
                );
            }
            finally
            {
                window.Close();
                Object.DestroyImmediate(window);
                toolkitSetting.SetValue(config, originalSetting);
            }
        }

        [Test]
        public void EncoreAndCelebrityCardsHaveEditableEffectObjects()
        {
            var encore = AssetDatabase.LoadAssetAtPath<CardData>(
                "Assets/Data/Cards/NepoBaby/Enhanced/Encore.asset"
            );
            Assert.AreEqual(3, encore.Effects.Count);
            Assert.IsInstanceOf<ReplayCardEffect>(encore.Effects[0]);
            Assert.IsInstanceOf<RaiseAllOpponentsHostilityEffect>(encore.Effects[1]);
            Assert.IsInstanceOf<ExhaustThisCardEffect>(encore.Effects[2]);
            foreach (
                var guid in AssetDatabase.FindAssets(
                    "t:CardData",
                    new[] { "Assets/Data/Cards/Celebrity", "Assets/Data/Cards/NepoBaby" }
                )
            )
            {
                var card = AssetDatabase.LoadAssetAtPath<CardData>(
                    AssetDatabase.GUIDToAssetPath(guid)
                );
                Assert.IsFalse(
                    UnityEditor.SerializationUtility.HasManagedReferencesWithMissingTypes(card),
                    card.name
                );
                Assert.IsTrue(
                    card.Effects.Concat(card.UpgradedEffects).All(effect => effect != null),
                    card.name
                );
            }
        }

        [Test]
        public void CardInspectorUsesOdinForEditablePolymorphicEffects()
        {
            var card = AssetDatabase.LoadAssetAtPath<CardData>(
                "Assets/Data/Cards/Celebrity/GlamourIou/Second Take.asset"
            );
            var inspector = UnityEditor.Editor.CreateEditor(card);
            try
            {
                Assert.IsInstanceOf<CardDataEditor>(inspector);
                var effects = inspector.serializedObject.FindProperty("_effects");
                Assert.AreEqual(1, effects.arraySize);
                Assert.IsInstanceOf<BorrowCardSelectionEffect>(
                    effects.GetArrayElementAtIndex(0).managedReferenceValue
                );
            }
            finally
            {
                Object.DestroyImmediate(inspector);
            }
        }

        [Test]
        public void DatabaseInspectorAddsAndPersistsEffectsAndRebuildsOnReload()
        {
            string folder = "CardInspectorTest_" + System.Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder);
            string path = $"Assets/{folder}/EditableCard.asset";
            var tab = new CardsTab();
            try
            {
                var source = AssetDatabase.LoadAssetAtPath<CardData>(
                    "Assets/Data/Cards/Celebrity/GlamourIou/Second Take.asset"
                );
                var card = Object.Instantiate(source);
                card.name = "EditableCard";
                AssetDatabase.CreateAsset(card, path);
                var getInspector = typeof(ContentTab<CardData>).GetMethod(
                    "GetInspector",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                var inspector = (UnityEditor.Editor)getInspector.Invoke(tab, new object[] { card });
                Assert.AreEqual(
                    "Sirenix.OdinInspector.Editor.OdinEditor",
                    inspector.GetType().FullName
                );
                inspector.serializedObject.Update();
                var effects = inspector.serializedObject.FindProperty("_effects");
                effects.arraySize = 2;
                effects.GetArrayElementAtIndex(1).managedReferenceValue =
                    new ExhaustThisCardEffect();
                inspector.serializedObject.ApplyModifiedProperties();
                AssetDatabase.SaveAssetIfDirty(card);
                AssetDatabase.ImportAsset(
                    path,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport
                );
                card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                Assert.AreEqual(2, card.Effects.Count);
                Assert.IsInstanceOf<ExhaustThisCardEffect>(card.Effects[1]);
                tab.Reload();
                Assert.IsTrue(inspector == null, "Reload must destroy the cached inspector.");
                var rebuilt = (UnityEditor.Editor)getInspector.Invoke(tab, new object[] { card });
                Assert.AreEqual(
                    "Sirenix.OdinInspector.Editor.OdinEditor",
                    rebuilt.GetType().FullName
                );
                Assert.AreEqual(2, rebuilt.serializedObject.FindProperty("_effects").arraySize);
            }
            finally
            {
                tab.OnDisable();
                AssetDatabase.DeleteAsset("Assets/" + folder);
            }
        }

        [Test]
        public void ReimportRestoresTypesAfterSourceCompilationRecovers()
        {
            string folder = "CardImportTest_" + System.Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder);
            string path = $"Assets/{folder}/ImportRecovery.asset";
            bool previousLogSetting = LogAssert.ignoreFailingMessages;
            try
            {
                var source = AssetDatabase.LoadAssetAtPath<CardData>(
                    "Assets/Data/Cards/NepoBaby/Enhanced/Encore.asset"
                );
                var card = Object.Instantiate(source);
                card.name = "ImportRecovery";
                AssetDatabase.CreateAsset(card, path);
                AssetDatabase.SaveAssetIfDirty(card);
                string valid = File.ReadAllText(path);
                string broken = valid.Replace(
                    "class: ReplayCardEffect,",
                    "class: MissingReplayCardEffect,"
                );
                Assert.AreNotEqual(valid, broken);
                LogAssert.ignoreFailingMessages = true;
                File.WriteAllText(path, broken);
                AssetDatabase.ImportAsset(
                    path,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport
                );
                card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                Assert.IsTrue(
                    UnityEditor.SerializationUtility.HasManagedReferencesWithMissingTypes(card)
                );
                File.WriteAllText(path, valid);
                typeof(DatabaseAutoRefresh)
                    .GetMethod("ReimportStaleCards", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, null);
                card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                Assert.IsFalse(
                    UnityEditor.SerializationUtility.HasManagedReferencesWithMissingTypes(card)
                );
                Assert.IsInstanceOf<ReplayCardEffect>(card.Effects[0]);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousLogSetting;
                AssetDatabase.DeleteAsset("Assets/" + folder);
            }
        }
    }
}
