using System;
using System.Collections.Generic;
using System.Reflection;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Gameplay;
using Crookedile.Gameplay.Battle;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Authors the Celebrity Glamour / IOU card set (docs/celebrity-glamour-iou.md) as CardData
    /// assets. Re-running overwrites the cards in place (ids and GUIDs survive). Numbers are
    /// placeholders: tune them on the assets, not here.
    /// </summary>
    public static class CelebrityGlamourIouBuilder
    {
        private const string Dir = "Assets/Data/Cards/Celebrity/GlamourIou";

        private static readonly (string name, int count)[] StarterDeck =
        {
            ("Hot Take", 3),
            ("No Comment", 3),
            ("Autograph", 3),
            ("Smile and Wave", 1),
        };

        [MenuItem("Crookedile/Celebrity/Build Glamour-IOU Cards")]
        public static void Build()
        {
            EnsureFolder("Assets/Data/Cards/Celebrity");
            EnsureFolder(Dir);
            const CardType P = CardType.Pressure, R = CardType.Rhetoric, Pol = CardType.Policy;
            const CardRarity B = CardRarity.Basic, E = CardRarity.Enhanced, Rare = CardRarity.Rare;
            const EffectContextValue Glam = EffectContextValue.CurrentGlamour;
            const EffectContextValue Debt = EffectContextValue.CurrentDebt;

            // Token first: Prepared Remarks, Borrow and Bailout reference it.
            var soundbite = Save("Soundbite", R, B, 0, new List<BattleEffect> { Opp(2), Make<ExhaustThisCardEffect>() }, generatedOnly: true);

            // Starter deck
            Save("Hot Take", P, B, 1, Opp(6));
            Save("No Comment", P, B, 1, Support(5));
            Save("Autograph", P, B, 1, Make<ReduceHostilityEffect>(("_amount", 2)));
            Save(
                "Smile and Wave", R, B, 1,
                new List<BattleEffect> { Support(5), GainGlamour(3) },
                upgraded: new List<BattleEffect> { Support(7), GainGlamour(4) }
            );

            // Glamour pole
            Save("Thumbs Up", R, B, 0, Support(3), GainGlamour(1));
            Save("Press Release", R, B, 1, Support(6), GainGlamour(2));
            Save("Prepared Remarks", R, B, 1, Support(4), AddHand(soundbite, 2));
            Save(
                "Photo Op", Pol, E, 1,
                new List<BattleEffect>(),
                passives: new List<BattlePassive> { OnSupportGained(GainGlamour(1)) }
            );
            Save("Behind the Podium", R, E, 1, Support(0, Glam));
            Save("Trending", R, Rare, 1, GainGlamour(0, Glam), Make<ExhaustThisCardEffect>());
            Save("Going Viral", R, Rare, 1, GainGlamour(0, EffectContextValue.CurrentSupport, 0.5f));
            Save("Standing Ovation", R, E, 2, Opp(0, Glam));
            Save("Fan Mail", R, E, 1, Make<DrawCardsEffect>(("_amount", 1)), Make<DrawIfGlamourEffect>());

            // IOU pole
            Save("Cash Advance", R, B, 1, new List<BattleEffect> { Make<BorrowEffect>(), AddHand(soundbite, 2) }, tags: "borrow");
            Save("Calling It In", R, E, 1, Opp(0, Debt, 4f));
            Save("Settle Up", R, E, 1, Make<ForgiveDebtEffect>(), Make<ExhaustThisCardEffect>());
            Save("Line of Credit", Pol, E, 1, Rule(DebtRule.LineOfCredit));
            Save("Open Tab", Pol, E, 1, Rule(DebtRule.OpenTab));
            Save("Bailout", Pol, Rare, 2, Rule(DebtRule.Bailout, soundbite, amount: 3));
            Save("Too Big to Fail", Pol, E, 2, Rule(DebtRule.TooBigToFail), Make<ExhaustThisCardEffect>());
            Save("Campaign Donors", Pol, E, 1, Support(0, EffectContextValue.DebtGainedThisTurn));
            Save("Overdraft", Pol, B, 1, Rule(DebtRule.Overdraft), Make<ExhaustThisCardEffect>());
            Save("Rain Check", Pol, B, 1, Rule(DebtRule.RainCheck), Make<ExhaustThisCardEffect>());
            Save("Fine Print", Pol, B, 0, Support(0, Debt), Make<ExhaustThisCardEffect>());

            AssetDatabase.SaveAssets();
            Debug.Log($"[Celebrity] Glamour-IOU cards built in {Dir}");
        }

        [MenuItem("Crookedile/Celebrity/Set Actor Starter Deck to Glamour-IOU")]
        public static void SetStarterDeck()
        {
            var db = OriginDatabase.Shared;
            if (db == null)
            {
                Debug.LogError("[Celebrity] No OriginDatabase found.");
                return;
            }
            var so = new SerializedObject(db);
            var entries = so.FindProperty("_entries");
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("Type").enumValueIndex != (int)OriginType.Actor)
                    continue;
                var deck = entry.FindPropertyRelative("StarterDeck");
                deck.ClearArray();
                foreach (var (name, count) in StarterDeck)
                {
                    var card = AssetDatabase.LoadAssetAtPath<CardData>($"{Dir}/{name}.asset");
                    if (card == null)
                    {
                        Debug.LogError($"[Celebrity] '{name}' missing: run Build Glamour-IOU Cards first.");
                        return;
                    }
                    deck.InsertArrayElementAtIndex(deck.arraySize);
                    var line = deck.GetArrayElementAtIndex(deck.arraySize - 1);
                    line.FindPropertyRelative("Card").objectReferenceValue = card;
                    line.FindPropertyRelative("Count").intValue = count;
                }
                so.ApplyModifiedProperties();
                Debug.Log("[Celebrity] Actor starter deck set to the Glamour-IOU 10.");
                return;
            }
            Debug.LogError("[Celebrity] No Actor entry in the OriginDatabase.");
        }

        [MenuItem("Crookedile/Celebrity/Self-Check Debt Rules")]
        public static void SelfCheck()
        {
            var ledger = new OpinionLedger(100, 50, () => { });
            var state = new CelebrityState();
            var player = new BattleStats(3);

            state.GainDebt(5);
            state.Settle(ledger, player, (_, __) => 0);
            // 3 energy absorbed, 2 unpaid -> 2 Opinion lost, Debt cleared.
            Check(player.CurrentActionPoints == 0 && ledger.CurrentOpinion == 48 && state.Debt == 0, "settle");

            player.RefreshActionPoints();
            state.LineOfCreditReduction = 1;
            state.GainDebt(2);
            state.GainDebt(2);
            Check(state.Debt == 3, "line of credit strips 1 once a turn");

            state.SettlementsDelayed = 1;
            state.Settle(ledger, player, (_, __) => 0);
            Check(state.Debt == 3 && player.CurrentActionPoints == 3, "rain check delays one settlement");

            state.DebtWaivers = 1;
            state.GainDebt(4); // 7 owed, 3 paid in energy, 4 would be damage
            state.Settle(ledger, player, (_, __) => 0);
            Check(player.CurrentActionPoints == 0 && ledger.CurrentOpinion == 48 && state.DebtWaivers == 0, "waiver eats the damage");
            Debug.Log("[Celebrity] Debt self-check passed.");
        }

        private static void Check(bool ok, string what)
        {
            if (!ok)
                throw new Exception($"[Celebrity] self-check failed: {what}");
        }

        #region Card + effect helpers

        private static CardData Save(
            string name, CardType type, CardRarity rarity, int cost, params BattleEffect[] effects
        ) => Save(name, type, rarity, cost, new List<BattleEffect>(effects));

        private static CardData Save(
            string name, CardType type, CardRarity rarity, int cost, List<BattleEffect> effects,
            List<BattleEffect> upgraded = null, List<BattlePassive> passives = null,
            bool generatedOnly = false, string tags = null
        )
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            var tagList = new List<string> { "actor", "glamouriou" };
            if (tags != null)
                tagList.Add(tags);
            var costRow = new CardCost();
            Set(costRow, "_costType", CostType.ActionPoints);
            Set(costRow, "_baseAmount", cost);

            Set(card, "_cardName", name);
            Set(card, "_cardType", type);
            Set(card, "_rarity", rarity);
            Set(card, "_costs", new List<CardCost> { costRow });
            Set(card, "_effects", effects);
            Set(card, "_passives", passives ?? new List<BattlePassive>());
            Set(card, "_upgradedEffects", upgraded ?? new List<BattleEffect>());
            Set(card, "_tags", tagList);
            Set(card, "_isGeneratedOnly", generatedOnly);

            string path = $"{Dir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (existing == null)
            {
                Set(card, "_id", Guid.NewGuid().ToString());
                AssetDatabase.CreateAsset(card, path);
                return card;
            }
            Set(card, "_id", Get<string>(existing, "_id"));
            EditorUtility.CopySerialized(card, existing);
            UnityEngine.Object.DestroyImmediate(card);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static BattleEffect Opp(int amount, EffectContextValue src = EffectContextValue.FixedAmount, float mult = 1f) =>
            Make<ApplyOpinionEffect>(("_amount", amount), ("_amountSource", src), ("_multiplier", mult));

        private static BattleEffect Support(int amount, EffectContextValue src = EffectContextValue.FixedAmount) =>
            Make<GainSupportEffect>(("_amount", amount), ("_amountSource", src));

        private static BattleEffect GainGlamour(int amount, EffectContextValue src = EffectContextValue.FixedAmount, float mult = 1f) =>
            Make<GainGlamourEffect>(("_amount", amount), ("_amountSource", src), ("_multiplier", mult));

        private static BattleEffect AddHand(CardData card, int count) =>
            Make<AddCardToHandEffect>(("_card", card), ("_amount", count));

        private static BattleEffect Rule(DebtRule rule, CardData soundbite = null, int amount = 1) =>
            Make<DebtRuleEffect>(("_rule", rule), ("_amount", amount), ("_soundbite", soundbite));

        private static BattlePassive OnSupportGained(params BattleEffect[] effects)
        {
            var passive = new BattlePassive();
            Set(passive, "_trigger", new SupportGainedTrigger());
            Set(passive, "_effects", new List<BattleEffect>(effects));
            return passive;
        }

        private static T Make<T>(params (string field, object value)[] fields)
            where T : new()
        {
            var obj = new T();
            foreach (var (field, value) in fields)
                Set(obj, field, value);
            return obj;
        }

        private static void Set(object target, string field, object value)
        {
            for (var t = target.GetType(); t != null; t = t.BaseType)
            {
                var f = t.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (f == null)
                    continue;
                f.SetValue(target, value);
                return;
            }
            throw new MissingFieldException(target.GetType().Name, field);
        }

        private static T Get<T>(object target, string field)
        {
            for (var t = target.GetType(); t != null; t = t.BaseType)
            {
                var f = t.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (f != null)
                    return (T)f.GetValue(target);
            }
            throw new MissingFieldException(target.GetType().Name, field);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'), System.IO.Path.GetFileName(path));
        }

        #endregion
    }
}
