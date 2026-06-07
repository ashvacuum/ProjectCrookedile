using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Gameplay.Battle;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// One-click generator for the Faith Leader starter deck (per docs/crookedile-starter-decks.md).
    /// Builds the 7 distinct card assets in the current serialization format (effects set via the
    /// live private fields, so Unity serializes the [SerializeReference] lists correctly).
    ///
    /// Menu: Crookedile → Generate → Faith Leader Starter Deck. Re-runnable (overwrites the assets in
    /// the Starter/ folder). All numbers are placeholders — tune in play.
    ///
    /// NOTE: this only creates the CARD assets. Wire them into the starter deck list, and populate the
    /// existing FaithLeaderPassive/NepoBabyPassive assets, separately (see the chat instructions).
    /// </summary>
    public static class FaithLeaderDeckGenerator
    {
        private const string Folder = "Assets/Resources/Cards/FaithLeader/Starter";

        [MenuItem("Crookedile/Generate/Faith Leader Starter Deck")]
        public static void Generate()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh(); // register the folder before creating assets in it

            // Basics
            MakeCard("Rebuke", CardType.Rhetoric, 1, CardRarity.Basic, DealDamage(6, TargetType.Opponent));
            MakeCard("Pray", CardType.Pressure, 1, CardRarity.Basic, GainShield(5), Draw(1));
            MakeCard("Call Out Sin", CardType.Rhetoric, 1, CardRarity.Basic,
                RaiseHostility(3, TargetType.RandomReceptive));

            // Identity — pacify stackers (Permanent so they persist toward the 3-stack threshold)
            MakeCard("Guilt Trip", CardType.Pressure, 1, CardRarity.Basic,
                ApplyStatus(TargetType.Opponent, StatusEffectType.Guilt, 1, StatusDurationType.Permanent));
            MakeCard("Name and Shame", CardType.Pressure, 1, CardRarity.Basic,
                ApplyStatus(TargetType.Opponent, StatusEffectType.Shame, 1, StatusDurationType.Permanent));
            MakeCard("Sow Doubt", CardType.Pressure, 1, CardRarity.Basic,
                ApplyStatus(TargetType.Opponent, StatusEffectType.Doubt, 1, StatusDurationType.Permanent));

            // Identity — harvest (scales with conversions this turn; 1:1 for now, tune/multiplier later)
            MakeCard("Sermon", CardType.Pressure, 2, CardRarity.Basic,
                DealDamageSourced(EffectContextValue.ConversionsThisTurn, TargetType.Opponent));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[FaithLeaderDeckGenerator] Generated 7 starter cards in {Folder}");
        }

        // --- Card assembly ------------------------------------------------------

        private static void MakeCard(
            string name,
            CardType type,
            int energyCost,
            CardRarity rarity,
            params BattleEffect[] effects
        )
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            SetField(card, "_id", Guid.NewGuid().ToString());
            SetField(card, "_cardName", name);
            SetField(card, "_cardType", type);
            SetField(card, "_rarity", rarity);
            SetField(card, "_costs", new List<CardCost> { new CardCost(CostType.ActionPoints, energyCost) });
            SetField(card, "_effects", new List<BattleEffect>(effects));
            SetField(card, "_isStarterCard", true);
            SetField(card, "_tags", new List<string> { "faithleader" });

            string path = $"{Folder}/{name}.asset";
            AssetDatabase.DeleteAsset(path); // overwrite cleanly on re-run
            AssetDatabase.CreateAsset(card, path);
        }

        // --- Effect builders ----------------------------------------------------

        private static BattleEffect DealDamage(int amount, TargetType target)
        {
            var e = new ApplyPressureEffect();
            SetField(e, "_amount", amount);
            SetField(e, "_amountSource", EffectContextValue.FixedAmount);
            SetField(e, "_target", target);
            return e;
        }

        private static BattleEffect DealDamageSourced(EffectContextValue source, TargetType target)
        {
            var e = new ApplyPressureEffect();
            SetField(e, "_amountSource", source);
            SetField(e, "_target", target);
            return e;
        }

        private static BattleEffect GainShield(int amount)
        {
            var e = new GainBufferEffect();
            SetField(e, "_amount", amount);
            SetField(e, "_amountSource", EffectContextValue.FixedAmount);
            return e;
        }

        private static BattleEffect Draw(int amount)
        {
            var e = new DrawCardsEffect();
            SetField(e, "_amount", amount);
            return e;
        }

        private static BattleEffect RaiseHostility(int amount, TargetType target)
        {
            var e = new RaiseTargetHostilityEffect();
            SetField(e, "_amount", amount);
            SetField(e, "_target", target);
            return e;
        }

        private static BattleEffect ApplyStatus(
            TargetType target,
            StatusEffectType status,
            int stacks,
            StatusDurationType duration
        )
        {
            // Behavior-first: resolve the enum to a fresh StatusBehavior instance via the bridge.
            var behavior = Activator.CreateInstance(StatusBridge.ToBehavior(status).GetType());
            var e = new ApplyStatusBehaviorEffect();
            SetField(e, "_target", target);
            SetField(e, "_behavior", behavior);
            SetField(e, "_stacks", stacks);
            SetField(e, "_duration", duration);
            return e;
        }

        // --- Reflection helper (walks the type hierarchy for private serialized fields) ---

        private static void SetField(object obj, string fieldName, object value)
        {
            Type t = obj.GetType();
            while (t != null)
            {
                FieldInfo f = t.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
                if (f != null)
                {
                    f.SetValue(obj, value);
                    return;
                }
                t = t.BaseType;
            }
            throw new Exception($"Field '{fieldName}' not found on {obj.GetType().Name}");
        }
    }
}
