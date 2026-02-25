using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Cards;

namespace Crookedile.Editor
{
    /// <summary>
    /// Unity Editor tool to auto-generate all 30 starter cards as ScriptableObjects.
    /// Right-click in Project window → Crookedile → Generate All Starter Cards
    /// </summary>
    public static class StarterCardGenerator
    {
        [MenuItem("Assets/Crookedile/Generate All Starter Cards", false, 1)]
        public static void GenerateAllStarterCards()
        {
            string basePath = "Assets/Data/Cards/Starter";

            // Ensure folders exist
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder("Assets/Data/Cards"))
                AssetDatabase.CreateFolder("Assets/Data", "Cards");
            if (!AssetDatabase.IsValidFolder(basePath))
                AssetDatabase.CreateFolder("Assets/Data/Cards", "Starter");

            GenerateFaithLeaderCards(basePath);
            GenerateNepoBabyCards(basePath);
            GenerateActorCards(basePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("✅ Generated all 30 starter cards!");
        }

        #region Faith Leader Cards

        private static void GenerateFaithLeaderCards(string basePath)
        {
            string path = $"{basePath}/FaithLeader";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(basePath, "FaithLeader");

            // 1. Find Common Ground x4
            for (int i = 1; i <= 4; i++)
            {
                CreateCard(
                    path: $"{path}/FindCommonGround_{i}.asset",
                    name: "Find Common Ground",
                    type: CardType.Pressure,
                    rarity: CardRarity.Basic,
                    description: "Basic persuasion technique.",
                    cost: 1,
                    effects: new CardEffect[]
                    {
                        CreateDamageEffect(3)
                    }
                );
            }

            // 2. Blessing x2
            for (int i = 1; i <= 2; i++)
            {
                CreateCard(
                    path: $"{path}/Blessing_{i}.asset",
                    name: "Blessing",
                    type: CardType.Pressure,
                    rarity: CardRarity.Basic,
                    description: "Convert all Composure into a powerful burst of conviction.",
                    cost: 1,
                    effects: new CardEffect[]
                    {
                        CreateDamageEqualToComposureEffect(),
                        CreateConsumeAllComposureEffect()
                    }
                );
            }

            // 3. Accusation x2
            for (int i = 1; i <= 2; i++)
            {
                CreateCard(
                    path: $"{path}/Accusation_{i}.asset",
                    name: "Accusation",
                    type: CardType.Rhetoric,
                    rarity: CardRarity.Basic,
                    description: "Direct confrontation. Creates tension.",
                    cost: 1,
                    effects: new CardEffect[]
                    {
                        CreateDamageEffect(4),
                        CreateReduceHostilityEffect(1)
                    }
                );
            }

            // 4. Deflect x1
            CreateCard(
                path: $"{path}/Deflect.asset",
                name: "Deflect",
                type: CardType.Policy,
                rarity: CardRarity.Basic,
                description: "Redirect aggression into grace.",
                cost: 1,
                effects: new CardEffect[]
                {
                    CreateGainComposureEffect(3),
                    CreateReduceHostilityEffect(1)
                }
            );

            // 5. Gather Thoughts x1
            CreateCard(
                path: $"{path}/GatherThoughts.asset",
                name: "Gather Thoughts",
                type: CardType.Policy,
                rarity: CardRarity.Basic,
                description: "Center yourself and build inner strength.",
                cost: 1,
                effects: new CardEffect[]
                {
                    CreateGainComposureEffect(4)
                }
            );
        }

        #endregion

        #region Nepo Baby Cards

        private static void GenerateNepoBabyCards(string basePath)
        {
            string path = $"{basePath}/NepoBaby";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(basePath, "NepoBaby");

            // 1. Family Name x2
            for (int i = 1; i <= 2; i++)
            {
                CreateCard(
                    path: $"{path}/FamilyName_{i}.asset",
                    name: "Family Name",
                    type: CardType.Pressure,
                    rarity: CardRarity.Basic,
                    description: "Leverage your family's reputation.",
                    cost: 1,
                    effects: new CardEffect[]
                    {
                        CreateDamageEffect(3)
                    }
                );
            }

            // 2. Inherited Privilege x1
            CreateCard(
                path: $"{path}/InheritedPrivilege.asset",
                name: "Inherited Privilege",
                type: CardType.Pressure,
                rarity: CardRarity.Basic,
                description: "Your advantages open doors.",
                cost: 2,
                effects: new CardEffect[]
                {
                    CreateDamageEffect(5),
                    CreateDrawCardsEffect(1)
                }
            );

            // 3. Pull Strings x2
            for (int i = 1; i <= 2; i++)
            {
                CreateCard(
                    path: $"{path}/PullStrings_{i}.asset",
                    name: "Pull Strings",
                    type: CardType.Rhetoric,
                    rarity: CardRarity.Basic,
                    description: "Use connections to apply pressure.",
                    cost: 1,
                    effects: new CardEffect[]
                    {
                        CreateDamageEffect(4),
                        CreateReduceHostilityEffect(1)
                    }
                );
            }

            // 4. Call in Favor x2
            for (int i = 1; i <= 2; i++)
            {
                CreateCard(
                    path: $"{path}/CallInFavor_{i}.asset",
                    name: "Call in Favor",
                    type: CardType.Policy,
                    rarity: CardRarity.Basic,
                    description: "You know people.",
                    cost: 1,
                    effects: new CardEffect[]
                    {
                        CreateDrawCardsEffect(2)
                    }
                );
            }

            // 5. Backroom Deal x1
            CreateCard(
                path: $"{path}/BackroomDeal.asset",
                name: "Backroom Deal",
                type: CardType.Policy,
                rarity: CardRarity.Basic,
                description: "Negotiate for future advantage.",
                cost: 2,
                effects: new CardEffect[]
                {
                    CreateDrawCardsEffect(2),
                    CreateGainActionPointsNextTurnEffect(1)
                }
            );

            // 6. Dynasty Network x1
            CreateCard(
                path: $"{path}/DynastyNetwork.asset",
                name: "Dynasty Network",
                type: CardType.Policy,
                rarity: CardRarity.Basic,
                description: "Cycle through your connections.",
                cost: 1,
                effects: new CardEffect[]
                {
                    CreateDiscardCardsEffect(1),
                    CreateDrawCardsEffect(2)
                }
            );

            // 7. Trust Fund x1
            CreateCard(
                path: $"{path}/TrustFund.asset",
                name: "Trust Fund",
                type: CardType.Policy,
                rarity: CardRarity.Basic,
                description: "Money solves problems instantly.",
                cost: 0,
                effects: new CardEffect[]
                {
                    CreateGainComposureEffect(2),
                    CreateGainActionPointsEffect(1)
                }
            );
        }

        #endregion

        #region Actor Cards

        private static void GenerateActorCards(string basePath)
        {
            string path = $"{basePath}/Actor";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(basePath, "Actor");

            // 1. Charming Gambit x2
            for (int i = 1; i <= 2; i++)
            {
                CreateCard(
                    path: $"{path}/CharmingGambit_{i}.asset",
                    name: "Charming Gambit",
                    type: CardType.Pressure,
                    rarity: CardRarity.Basic,
                    description: "Charisma with a chance of deeper connection.",
                    cost: 1,
                    effects: new CardEffect[]
                    {
                        CreateDamageEffect(3),
                        CreateDrawCardsEffect(1) // TODO: Should be 50% chance
                    }
                );
            }

            // 2. All or Nothing x1
            CreateCard(
                path: $"{path}/AllOrNothing.asset",
                name: "All or Nothing",
                type: CardType.Rhetoric,
                rarity: CardRarity.Basic,
                description: "High risk, high reward aggression.",
                cost: 2,
                effects: new CardEffect[]
                {
                    CreateRandomDamageEffect(3, 9)
                }
            );

            // 3. Bold Accusation x2
            for (int i = 1; i <= 2; i++)
            {
                CreateCard(
                    path: $"{path}/BoldAccusation_{i}.asset",
                    name: "Bold Accusation",
                    type: CardType.Rhetoric,
                    rarity: CardRarity.Basic,
                    description: "Aggressive confrontation.",
                    cost: 1,
                    effects: new CardEffect[]
                    {
                        CreateDamageEffect(5),
                        CreateReduceHostilityEffect(2)
                    }
                );
            }

            // 4. Spotlight Hog x2
            for (int i = 1; i <= 2; i++)
            {
                CreateCard(
                    path: $"{path}/SpotlightHog_{i}.asset",
                    name: "Spotlight Hog",
                    type: CardType.Rhetoric,
                    rarity: CardRarity.Basic,
                    description: "All eyes on you - for better or worse.",
                    cost: 2,
                    effects: new CardEffect[]
                    {
                        CreateDamageEffect(6),
                        CreateGainComposureEffect(3),
                        CreateReduceHostilityEffect(2)
                    }
                );
            }

            // 5. High Stakes x1
            CreateCard(
                path: $"{path}/HighStakes.asset",
                name: "High Stakes",
                type: CardType.Policy,
                rarity: CardRarity.Rare,
                description: "All in.",
                cost: 0,
                effects: new CardEffect[]
                {
                    CreateDiscardCardsEffect(99), // Discard entire hand
                    CreateDrawCardsEffect(3)
                }
            );

            // 6. Ego Trip x1
            CreateCard(
                path: $"{path}/EgoTrip.asset",
                name: "Ego Trip",
                type: CardType.Policy,
                rarity: CardRarity.Basic,
                description: "Convert your bad reputation into confidence.",
                cost: 1,
                effects: new CardEffect[]
                {
                    CreateComposureEqualToHostilityEffect()
                }
            );

            // 7. Fan Favorite x1
            CreateCard(
                path: $"{path}/FanFavorite.asset",
                name: "Fan Favorite",
                type: CardType.Policy,
                rarity: CardRarity.Basic,
                description: "Trade popularity for damage reduction.",
                cost: 1,
                effects: new CardEffect[]
                {
                    CreateLoseComposureEffect(3),
                    CreateReduceHostilityEffect(3)
                }
            );
        }

        #endregion

        #region Card Creation Helpers

        private static void CreateCard(string path, string name, CardType type, CardRarity rarity, string description, int cost, CardEffect[] effects)
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();

            // Use reflection to set private serialized fields
            var nameField = typeof(CardData).GetField("_cardName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var typeField = typeof(CardData).GetField("_cardType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rarityField = typeof(CardData).GetField("_rarity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var descriptionField = typeof(CardData).GetField("_description", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var costsField = typeof(CardData).GetField("_costs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var effectsField = typeof(CardData).GetField("_effects", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            nameField?.SetValue(card, name);
            typeField?.SetValue(card, type);
            rarityField?.SetValue(card, rarity);
            descriptionField?.SetValue(card, description);

            List<CardCost> costs = new List<CardCost>();
            if (cost > 0)
            {
                costs.Add(new CardCost(CostType.ActionPoints, cost));
            }
            else
            {
                costs.Add(new CardCost(CostType.None, 0));
            }
            costsField?.SetValue(card, costs);

            effectsField?.SetValue(card, new List<CardEffect>(effects));

            AssetDatabase.CreateAsset(card, path);
        }

        #endregion

        #region Effect Creation Helpers

        private static CardEffect CreateDamageEffect(int amount)
        {
            return CreateEffect(EffectCategory.Damage, TargetType.Opponent, damageType: DamageType.FixedDamage, damageAmount: amount);
        }

        private static CardEffect CreateRandomDamageEffect(int min, int max)
        {
            return CreateEffect(EffectCategory.Damage, TargetType.Opponent, damageType: DamageType.RandomDamage, randomMin: min, randomMax: max);
        }

        private static CardEffect CreateDamageEqualToComposureEffect()
        {
            return CreateEffect(EffectCategory.Damage, TargetType.Opponent, damageType: DamageType.DamageEqualToComposure);
        }

        private static CardEffect CreateGainComposureEffect(int amount)
        {
            return CreateEffect(EffectCategory.Resource, TargetType.Self, resourceType: ResourceEffectType.GainComposure, resourceAmount: amount);
        }

        private static CardEffect CreateLoseComposureEffect(int amount)
        {
            return CreateEffect(EffectCategory.Resource, TargetType.Self, resourceType: ResourceEffectType.LoseComposure, resourceAmount: amount);
        }

        private static CardEffect CreateConsumeAllComposureEffect()
        {
            return CreateEffect(EffectCategory.Resource, TargetType.Self, resourceType: ResourceEffectType.ConsumeAllComposure);
        }

        private static CardEffect CreateComposureEqualToHostilityEffect()
        {
            return CreateEffect(EffectCategory.Resource, TargetType.Self, resourceType: ResourceEffectType.ComposureEqualToHostility);
        }

        private static CardEffect CreateReduceHostilityEffect(int amount)
        {
            return CreateEffect(EffectCategory.Resource, TargetType.Self, resourceType: ResourceEffectType.ReduceHostility, resourceAmount: amount);
        }

        private static CardEffect CreateGainActionPointsEffect(int amount)
        {
            return CreateEffect(EffectCategory.Resource, TargetType.Self, resourceType: ResourceEffectType.GainActionPoints, resourceAmount: amount);
        }

        private static CardEffect CreateGainActionPointsNextTurnEffect(int amount)
        {
            return CreateEffect(EffectCategory.Resource, TargetType.Self, resourceType: ResourceEffectType.GainActionPointsNextTurn, resourceAmount: amount);
        }

        private static CardEffect CreateDrawCardsEffect(int amount)
        {
            return CreateEffect(EffectCategory.CardManipulation, TargetType.Self, cardManipType: CardManipulationType.DrawCards, cardAmount: amount);
        }

        private static CardEffect CreateDiscardCardsEffect(int amount)
        {
            return CreateEffect(EffectCategory.CardManipulation, TargetType.Self, cardManipType: CardManipulationType.DiscardCards, cardAmount: amount);
        }

        private static CardEffect CreateEffect(
            EffectCategory category,
            TargetType target,
            DamageType damageType = DamageType.FixedDamage,
            int damageAmount = 0,
            int randomMin = 0,
            int randomMax = 0,
            ResourceEffectType resourceType = ResourceEffectType.GainComposure,
            int resourceAmount = 0,
            CardManipulationType cardManipType = CardManipulationType.DrawCards,
            int cardAmount = 0)
        {
            var effect = new CardEffect();
            var categoryField = typeof(CardEffect).GetField("_category", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetField = typeof(CardEffect).GetField("_target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            categoryField?.SetValue(effect, category);
            targetField?.SetValue(effect, target);

            if (category == EffectCategory.Damage)
            {
                var damageTypeField = typeof(CardEffect).GetField("_damageType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var damageAmountField = typeof(CardEffect).GetField("_damageAmount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var randomMinField = typeof(CardEffect).GetField("_randomDamageMin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var randomMaxField = typeof(CardEffect).GetField("_randomDamageMax", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                damageTypeField?.SetValue(effect, damageType);
                damageAmountField?.SetValue(effect, damageAmount);
                randomMinField?.SetValue(effect, randomMin);
                randomMaxField?.SetValue(effect, randomMax);
            }
            else if (category == EffectCategory.Resource)
            {
                var resourceTypeField = typeof(CardEffect).GetField("_resourceType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var resourceAmountField = typeof(CardEffect).GetField("_resourceAmount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                resourceTypeField?.SetValue(effect, resourceType);
                resourceAmountField?.SetValue(effect, resourceAmount);
            }
            else if (category == EffectCategory.CardManipulation)
            {
                var cardManipTypeField = typeof(CardEffect).GetField("_cardManipulationType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var cardAmountField = typeof(CardEffect).GetField("_cardAmount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                cardManipTypeField?.SetValue(effect, cardManipType);
                cardAmountField?.SetValue(effect, cardAmount);
            }

            return effect;
        }

        #endregion
    }
}
