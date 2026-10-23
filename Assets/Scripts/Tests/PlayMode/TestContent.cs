using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Gameplay.Battle;
using NUnit.Framework;
using UnityEngine;

namespace Crookedile.Tests
{
    /// <summary>
    /// Builds cards and effects in code, so a test checks one mechanic with the numbers it states
    /// rather than whatever a card asset is tuned to today. Card assets are covered by
    /// CardSmokeTests; everything else should build what it needs here.
    /// </summary>
    internal static class TestContent
    {
        /// <summary>A new <typeparamref name="T"/> with the given private serialized fields set.</summary>
        public static T Make<T>(params (string field, object value)[] fields)
            where T : new()
        {
            var item = new T();
            foreach (var (field, value) in fields)
                Set(item, field, value);
            return item;
        }

        /// <summary>
        /// A card owned by <paramref name="owned"/> (destroy it in TearDown). Energy 0 = free,
        /// -1 = X cost.
        /// </summary>
        public static CardData Card(
            List<Object> owned,
            string name,
            CardType type,
            int energy,
            params BattleEffect[] effects
        )
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            owned.Add(card);
            card.name = name;
            // Asset cards take their id from the file GUID; an in-memory card needs its own so
            // id-keyed lookups (token swaps, saves) work. It is never saved, so a fresh one is safe.
            Set(card, "_id", "test-" + System.Guid.NewGuid().ToString("N"));
            Set(card, "_cardName", name);
            Set(card, "_cardType", type);
            Set(
                card,
                "_costs",
                energy == 0
                    ? new List<CardCost>()
                    : new List<CardCost>
                    {
                        new(CostType.ActionPoints, Mathf.Max(0, energy), isXCost: energy < 0),
                    }
            );
            Set(card, "_effects", effects.ToList());
            return card;
        }

        /// <summary>A Policy card whose passives switch on when it is played.</summary>
        public static CardData Policy(
            List<Object> owned,
            string name,
            int energy,
            params BattlePassive[] passives
        )
        {
            var card = Card(owned, name, CardType.Policy, energy);
            Set(card, "_passives", passives.ToList());
            return card;
        }

        /// <summary>Sets a private field declared on the object's type or any base type.</summary>
        public static void Set(object target, string field, object value)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var info = type.GetField(
                    field,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
                if (info == null)
                    continue;
                info.SetValue(target, value);
                return;
            }
            Assert.Fail($"{target.GetType().Name} has no field {field}");
        }
    }
}
