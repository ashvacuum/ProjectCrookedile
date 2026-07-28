using System.IO;
using System.Linq;
using Crookedile.Data;
using Crookedile.Data.Campaign;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Generates a small playable encounter pool so the campaign loop can be tested before any
    /// real content exists. Everything it writes lands in <c>Assets/Data/Encounters/Sample/</c>
    /// and is safe to delete.
    ///
    /// Exists because authoring these by hand means a lot of inspector clicking — every outcome
    /// is a <c>[SerializeReference]</c> type-picker choice — and that friction sits between you
    /// and the first end-to-end run.
    ///
    /// Menu: Crookedile → Campaign → Create Sample Content.
    /// </summary>
    public static class SampleCampaignContent
    {
        private const string Dir = "Assets/Data/Encounters/Sample";

        [MenuItem("Crookedile/Campaign/Create Sample Content")]
        public static void Create()
        {
            Directory.CreateDirectory(Dir);

            var bribe = CreateEvent(
                "Sample_TheFixer",
                "The Fixer",
                "A man in a barong leans against your van.",
                "He doesn't introduce himself. \"Your permits cleared this morning,\" he says, "
                    + "\"which is funny, because you never filed any.\" He waits.",
                options: new[]
                {
                    Option(
                        "Thank him and pay",
                        "He pockets it without counting. Somebody, somewhere, is now owed a favour.",
                        Funds(-40),
                        Credibility(10)
                    ),
                    Option(
                        "Refuse loudly, where people can hear",
                        "The crowd likes it. The Fixer doesn't.",
                        Credibility(25),
                        Funds(-10)
                    ),
                    Option(
                        "Take the permits and say nothing",
                        "You feel the weight of it in your chest all afternoon.",
                        Credibility(-15),
                        Funds(20)
                    ),
                }
            );

            var vigil = CreateEvent(
                "Sample_Vigil",
                "The Vigil",
                "Candles outside the parish hall.",
                "Forty people have been here since dawn. They are not here for you, but they "
                    + "have noticed you, and someone has already produced a microphone.",
                options: new[]
                {
                    Option(
                        "Speak",
                        "You say the thing you meant to say. It lands.",
                        Credibility(20)
                    ),
                    Option(
                        "Kneel and stay quiet",
                        "No one reports on it. Everyone who was there remembers.",
                        Credibility(10),
                        Funds(15)
                    ),
                }
            );

            var battle = CreateBattleEncounter("Sample_Hecklers", "Hecklers at the Plaza");
            var boss = CreateBattleEncounter("Sample_Boss", "Election Night", isBoss: true);

            var pool = CreatePool(
                "Sample_EncounterPool",
                (bribe, 1, 6, true, false),
                (vigil, 1, 6, true, false),
                (battle, 1, 6, false, false), // repeatable, so a short run never goes empty
                (boss, 7, 7, true, true) // day 7 only, and guaranteed — the demo's finale
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = pool;
            EditorGUIUtility.PingObject(pool);

            Debug.Log(
                $"[SampleCampaignContent] Wrote sample encounters to {Dir}.\n"
                    + "Assign Sample_EncounterPool to CampaignFlow's Pool field and press Play."
            );
        }

        #region Builders
        private static EventEncounterData CreateEvent(
            string assetName,
            string displayName,
            string blurb,
            string body,
            EventOption[] options
        )
        {
            var asset = ScriptableObject.CreateInstance<EventEncounterData>();
            AssetDatabase.CreateAsset(asset, $"{Dir}/{assetName}.asset");

            var so = new SerializedObject(asset);
            InitEncounterFields(so, displayName, blurb, hourCost: 1);
            so.FindProperty("_body").stringValue = body;

            var list = so.FindProperty("_options");
            list.arraySize = options.Length;
            for (int i = 0; i < options.Length; i++)
                WriteOption(list.GetArrayElementAtIndex(i), options[i]);

            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static BattleEncounterData CreateBattleEncounter(
            string assetName,
            string displayName,
            bool isBoss = false
        )
        {
            var asset = ScriptableObject.CreateInstance<BattleEncounterData>();
            AssetDatabase.CreateAsset(asset, $"{Dir}/{assetName}.asset");

            var so = new SerializedObject(asset);
            InitEncounterFields(
                so,
                displayName,
                isBoss
                    ? "Everything you've done so far is on the ballot."
                    : "Somebody paid them to be here. Probably.",
                hourCost: 1
            );
            // Drives CampaignFlow's End Day → Face the boss swap; without it the finale is
            // skippable.
            so.FindProperty("_isBoss").boolValue = isBoss;

            // Reuse whatever BattleSession already exists rather than authoring one — this is a
            // loop test, not a combat test. Left null (with a warning) if none is found.
            string sessionGuid = AssetDatabase.FindAssets("t:BattleSession").FirstOrDefault();
            if (sessionGuid != null)
            {
                var session = AssetDatabase.LoadAssetAtPath<BattleSession>(
                    AssetDatabase.GUIDToAssetPath(sessionGuid)
                );
                so.FindProperty("_session").objectReferenceValue = session;
            }
            else
            {
                Debug.LogWarning(
                    "[SampleCampaignContent] No BattleSession asset found — the sample battle "
                        + "encounter has no session and will refuse to start. Create one via "
                        + "Crookedile/Battle Session and assign it."
                );
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static EncounterPoolData CreatePool(
            string assetName,
            params (
                EncounterData encounter,
                int firstDay,
                int lastDay,
                bool oncePerRun,
                bool guaranteed
            )[] entries
        )
        {
            var asset = ScriptableObject.CreateInstance<EncounterPoolData>();
            AssetDatabase.CreateAsset(asset, $"{Dir}/{assetName}.asset");

            var so = new SerializedObject(asset);
            so.FindProperty("_days").intValue = 7;

            var list = so.FindProperty("_entries");
            list.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("_encounter").objectReferenceValue = entries[i].encounter;
                e.FindPropertyRelative("_firstDay").intValue = entries[i].firstDay;
                e.FindPropertyRelative("_lastDay").intValue = entries[i].lastDay;
                // Inherit each encounter's own DropWeight rather than overriding per row.
                e.FindPropertyRelative("_weight").floatValue = EncounterPoolEntry.InheritWeight;
                e.FindPropertyRelative("_oncePerRun").boolValue = entries[i].oncePerRun;
                e.FindPropertyRelative("_guaranteed").boolValue = entries[i].guaranteed;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        /// <summary>Fills the fields every encounter shares, including the GUID id.</summary>
        private static void InitEncounterFields(
            SerializedObject so,
            string displayName,
            string blurb,
            int hourCost
        )
        {
            // Set explicitly rather than relying on OnValidate firing at import time.
            so.FindProperty("_id").stringValue = System.Guid.NewGuid().ToString();
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_blurb").stringValue = blurb;
            so.FindProperty("_hourCost").intValue = hourCost;
            so.FindProperty("_dropWeight").floatValue = 1f;
        }

        private static void WriteOption(SerializedProperty prop, EventOption option)
        {
            prop.FindPropertyRelative("_label").stringValue = option.Label;
            prop.FindPropertyRelative("_resultText").stringValue = option.ResultText;

            var outcomes = prop.FindPropertyRelative("_outcomes");
            outcomes.arraySize = option.Outcomes.Count;
            for (int i = 0; i < option.Outcomes.Count; i++)
            {
                // managedReferenceValue is how a [SerializeReference] slot gets a concrete type
                // assigned from code — plain objectReferenceValue does not apply here.
                outcomes.GetArrayElementAtIndex(i).managedReferenceValue = option.Outcomes[i];
            }
        }

        #endregion

        #region Authoring shorthand
        // These build throwaway instances purely to carry values into WriteOption; the assets
        // store serialized copies, not these objects.
        private static EventOption Option(
            string label,
            string resultText,
            params RunOutcome[] outcomes
        )
        {
            // EventOption's fields are private; reflection is simpler than a SerializedObject
            // dance for an object that only ever carries values into WriteOption.
            var option = new EventOption();
            SetPrivate(option, "_label", label);
            SetPrivate(option, "_resultText", resultText);
            SetPrivate(option, "_outcomes", outcomes.ToList());
            return option;
        }

        private static RunOutcome Funds(int amount)
        {
            var o = new AdjustFundsOutcome();
            SetPrivate(o, "_amount", amount);
            return o;
        }

        private static RunOutcome Credibility(int amount)
        {
            var o = new AdjustCredibilityOutcome();
            SetPrivate(o, "_amount", amount);
            return o;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            var f = target
                .GetType()
                .GetField(
                    field,
                    System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.NonPublic
                );
            if (f == null)
            {
                Debug.LogError(
                    $"[SampleCampaignContent] No field '{field}' on {target.GetType().Name}."
                );
                return;
            }
            f.SetValue(target, value);
        }

        #endregion
    }
}
