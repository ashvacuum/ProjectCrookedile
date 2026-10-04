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
        };

        [UnityTest]
        public IEnumerator DatabaseDrawsEffectListContents(
            [ValueSource(nameof(CardsWithNewEffects))] string cardName
        )
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("This rendering check requires a graphics-enabled Unity editor.");

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
                    $"Assets/Data/Cards/Celebrity/GlamourIou/{cardName}.asset"
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
                var effects = tree.GetType()
                    .GetMethod("GetPropertyAtPath", new[] { typeof(string) })
                    .Invoke(tree, new object[] { "_effects" });
                Assert.IsNotNull(effects);
                var children = effects.GetType().GetProperty("Children").GetValue(effects);
                Assert.AreEqual(
                    card.Effects.Count,
                    children.GetType().GetProperty("Count").GetValue(children)
                );
                var rect = (Rect)
                    effects.GetType().GetProperty("LastDrawnValueRect").GetValue(effects);
                Assert.Greater(
                    rect.height,
                    0,
                    "Effects must actually be drawn in the Database pane."
                );
                var state = effects.GetType().GetProperty("State").GetValue(effects);
                state.GetType().GetProperty("Expanded").SetValue(state, true);
                for (int i = 0; i < 20; i++)
                {
                    window.Repaint();
                    yield return null;
                }
                var child = children
                    .GetType()
                    .GetProperty("Item", new[] { typeof(int) })
                    .GetValue(children, new object[] { 0 });
                var childRect = (Rect)
                    child.GetType().GetProperty("LastDrawnValueRect").GetValue(child);
                Assert.IsFalse(
                    float.IsNaN(childRect.y),
                    "Effect entries must have a valid layout position."
                );
                Assert.Greater(
                    childRect.height,
                    0,
                    "Effect entries must have a visible layout height."
                );
                Assert.Greater(
                    (int)child.GetType().GetProperty("DrawCount").GetValue(child),
                    0,
                    "The expanded list must draw its first effect entry."
                );
            }
            finally
            {
                window.Close();
                Object.DestroyImmediate(window);
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
                    new[] { "Assets/Data/Cards/Celebrity" }
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
                Assert.IsTrue(card.Effects.All(effect => effect != null), card.name);
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
