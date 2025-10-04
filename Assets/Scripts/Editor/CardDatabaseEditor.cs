using Crookedile.Data;
using UnityEditor;
using UnityEngine;
using Crookedile.Data.Cards;

namespace Crookedile.Editor
{
    [CustomEditor(typeof(CardDatabase))]
    public class CardDatabaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            CardDatabase database = (CardDatabase)target;

            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox($"Total Cards: {database.Count}", MessageType.Info);

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Refresh Database", GUILayout.Height(40)))
            {
                database.RefreshDatabase();
            }

            EditorGUILayout.Space(10);

            // Quick stats
            EditorGUILayout.LabelField("Database Statistics", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            foreach (CardType type in System.Enum.GetValues(typeof(CardType)))
            {
                int count = database.GetByType(type).Count;
                EditorGUILayout.LabelField($"{type}: {count}");
            }

            EditorGUILayout.Space(5);

            foreach (CardRarity rarity in System.Enum.GetValues(typeof(CardRarity)))
            {
                int count = database.GetByRarity(rarity).Count;
                EditorGUILayout.LabelField($"{rarity}: {count}");
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            DrawDefaultInspector();
        }
    }
}
