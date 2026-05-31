#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Gameplay.Battle;

namespace Crookedile.Editor
{
    /// <summary>
    /// One-time migration tool that converts all <see cref="CardData"/> and
    /// <see cref="EnemyMoveData"/> assets from the legacy <c>List&lt;CardEffect&gt;</c>
    /// format to the new polymorphic <c>[SerializeReference] List&lt;BattleEffect&gt;</c> system.
    ///
    /// Run via: <b>Crookedile → Tools → Migrate Effects to New System</b>
    ///
    /// After migration is verified, remove the legacy <c>_effects</c> field from
    /// <see cref="CardData"/> and <see cref="EnemyMoveData"/> and delete this tool.
    /// </summary>
    public static class EffectMigrationTool
    {
        [MenuItem("Crookedile/Tools/Migrate Effects to New System")]
        public static void MigrateAll()
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Migrate Effects",
                "This will convert all CardData and EnemyMoveData assets from the legacy "
                    + "CardEffect list to the new BattleEffect system.\n\n"
                    + "Assets that already have NewEffects populated will be SKIPPED.\n\n"
                    + "Proceed?",
                "Migrate",
                "Cancel"
            );

            if (!confirm)
                return;

            int cardsMigrated = 0;
            int movesMigrated = 0;
            int cardsSkipped = 0;
            int movesSkipped = 0;
            var errors = new List<string>();

            #region Migrate CardData assets
            string[] cardGuids = AssetDatabase.FindAssets("t:CardData");
            foreach (string guid in cardGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card == null)
                    continue;

                if (card.NewEffects != null && card.NewEffects.Count > 0)
                {
                    cardsSkipped++;
                    continue;
                }
                if (card.Effects == null || card.Effects.Count == 0)
                {
                    cardsSkipped++;
                    continue;
                }

                try
                {
                    var serialized = new SerializedObject(card);
                    var newEffectsProp = serialized.FindProperty("_newEffects");
                    newEffectsProp.ClearArray();

                    var converted = ConvertEffectList(card.Effects, errors, path);
                    for (int i = 0; i < converted.Count; i++)
                    {
                        newEffectsProp.InsertArrayElementAtIndex(i);
                        newEffectsProp.GetArrayElementAtIndex(i).managedReferenceValue = converted[
                            i
                        ];
                    }

                    serialized.ApplyModifiedProperties();
                    EditorUtility.SetDirty(card);
                    cardsMigrated++;
                }
                catch (Exception ex)
                {
                    errors.Add($"[CardData] {path}: {ex.Message}");
                }
            }

            #endregion

            #region Migrate EnemyMoveData assets
            string[] moveGuids = AssetDatabase.FindAssets("t:EnemyMoveData");
            foreach (string guid in moveGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EnemyMoveData move = AssetDatabase.LoadAssetAtPath<EnemyMoveData>(path);
                if (move == null)
                    continue;

                if (move.NewEffects != null && move.NewEffects.Count > 0)
                {
                    movesSkipped++;
                    continue;
                }
                if (move.Effects == null || move.Effects.Count == 0)
                {
                    movesSkipped++;
                    continue;
                }

                try
                {
                    var serialized = new SerializedObject(move);
                    var newEffectsProp = serialized.FindProperty("_newEffects");
                    newEffectsProp.ClearArray();

                    // EnemyMoveData.Effects returns IReadOnlyList — convert to list for the helper
                    var legacyList = new List<CardEffect>(move.Effects);
                    var converted = ConvertEffectList(legacyList, errors, path);
                    for (int i = 0; i < converted.Count; i++)
                    {
                        newEffectsProp.InsertArrayElementAtIndex(i);
                        newEffectsProp.GetArrayElementAtIndex(i).managedReferenceValue = converted[
                            i
                        ];
                    }

                    serialized.ApplyModifiedProperties();
                    EditorUtility.SetDirty(move);
                    movesMigrated++;
                }
                catch (Exception ex)
                {
                    errors.Add($"[EnemyMoveData] {path}: {ex.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            #endregion

            #region Report
            string report =
                $"Migration complete.\n\n"
                + $"CardData:      {cardsMigrated} migrated, {cardsSkipped} skipped\n"
                + $"EnemyMoveData: {movesMigrated} migrated, {movesSkipped} skipped\n";

            if (errors.Count > 0)
                report += $"\n{errors.Count} error(s):\n" + string.Join("\n", errors);

            Debug.Log("[EffectMigrationTool] " + report);
            EditorUtility.DisplayDialog("Migration Complete", report, "OK");
        }

            #endregion

        #region Conversion helpers
        private static List<BattleEffect> ConvertEffectList(
            List<CardEffect> legacy,
            List<string> errors,
            string assetPath
        )
        {
            var result = new List<BattleEffect>();
            foreach (var le in legacy)
            {
                if (le == null)
                    continue;
                try
                {
                    var converted = Convert(le, errors, assetPath);
                    if (converted != null)
                        result.Add(converted);
                }
                catch (Exception ex)
                {
                    errors.Add($"{assetPath}: failed to convert '{le.EffectName}' — {ex.Message}");
                }
            }
            return result;
        }

        private static BattleEffect Convert(CardEffect le, List<string> errors, string assetPath)
        {
            switch (le.Category)
            {
                case EffectCategory.Damage:
                    return ConvertDamage(le);
                case EffectCategory.Resource:
                    return ConvertResource(le);
                case EffectCategory.StatusEffect:
                    return ConvertStatus(le);

                case EffectCategory.CardManipulation:
                    return ConvertCardManipulation(le, errors, assetPath);

                default:
                    errors.Add($"{assetPath}: unknown EffectCategory '{le.Category}'");
                    return null;
            }
        }

        #endregion

        #region Damage
        private static BattleEffect ConvertDamage(CardEffect le)
        {
            switch (le.DamageType)
            {
                case DamageType.FixedDamage:
                    return Make<DealDamageEffect>(e =>
                    {
                        SetField(e, "_target", le.Target);
                        SetField(e, "_amount", le.DamageAmount);
                        SetField(e, "_amountSource", le.AmountSource);
                    });

                case DamageType.RandomDamage:
                    return Make<DealRandomDamageEffect>(e =>
                    {
                        SetField(e, "_target", le.Target);
                        SetField(e, "_minDamage", le.RandomDamageMin);
                        SetField(e, "_maxDamage", le.RandomDamageMax);
                    });

                case DamageType.DamageEqualToShield:
                    return Make<RaiseOpinionEqualToShieldEffect>(e =>
                        SetField(e, "_target", le.Target)
                    );

                default:
                    return null;
            }
        }

        #endregion

        #region Resource
        private static BattleEffect ConvertResource(CardEffect le)
        {
            switch (le.ResourceType)
            {
                case ResourceEffectType.GainShield:
                    return Make<GainShieldEffect>(e =>
                    {
                        SetField(e, "_amount", le.ResourceAmount);
                        SetField(e, "_amountSource", le.AmountSource);
                    });

                case ResourceEffectType.LoseShield:
                    return Make<LoseShieldEffect>(e => SetField(e, "_amount", le.ResourceAmount));

                case ResourceEffectType.ConsumeAllShield:
                    return Make<ConsumeAllShieldEffect>(_ => { });

                case ResourceEffectType.ShieldEqualToHostility:
                    return Make<ShieldEqualToHostilityEffect>(_ => { });

                case ResourceEffectType.ReduceHostility:
                    return Make<ReduceHostilityEffect>(e =>
                        SetField(e, "_amount", le.ResourceAmount)
                    );

                case ResourceEffectType.RaiseTargetHostility:
                    return Make<RaiseTargetHostilityEffect>(e =>
                        SetField(e, "_amount", le.ResourceAmount)
                    );

                case ResourceEffectType.GainActionPoints:
                    return Make<GainActionPointsEffect>(e =>
                        SetField(e, "_amount", le.ResourceAmount)
                    );

                case ResourceEffectType.GainActionPointsNextTurn:
                    return Make<GainActionPointsNextTurnEffect>(e =>
                        SetField(e, "_amount", le.ResourceAmount)
                    );

                case ResourceEffectType.HealResolve:
                    return Make<HealResolveEffect>(e =>
                    {
                        SetField(e, "_amount", le.ResourceAmount);
                        SetField(e, "_amountSource", le.AmountSource);
                    });

                default:
                    return null;
            }
        }

        #endregion

        #region Status
        private static BattleEffect ConvertStatus(CardEffect le) =>
            Make<ApplyStatusEffect>(e =>
            {
                SetField(e, "_target", le.Target);
                SetField(e, "_statusType", le.StatusEffectType);
                SetField(e, "_stacks", le.StatusStacks);
                SetField(e, "_duration", le.StatusDuration);
            });

        #endregion

        #region CardManipulation
        private static BattleEffect ConvertCardManipulation(
            CardEffect le,
            List<string> errors,
            string assetPath
        )
        {
            switch (le.CardManipulationType)
            {
                case CardManipulationType.DrawCards:
                    return Make<DrawCardsEffect>(e => SetField(e, "_amount", le.CardAmount));

                case CardManipulationType.DiscardCards:
                    return Make<DiscardCardsEffect>(e => SetField(e, "_amount", le.CardAmount));

                case CardManipulationType.DiscardHand:
                    return Make<DiscardHandEffect>(e =>
                        SetField(e, "_reclaimAmount", le.DiscardDrawAmount)
                    );

                case CardManipulationType.ExhaustThisCard:
                    return Make<ExhaustThisCardEffect>(_ => { });

                case CardManipulationType.ChooseToDiscard:
                    return Make<ChooseToDiscardEffect>(e =>
                    {
                        SetField(e, "_amount", le.CardAmount);
                        SetField(e, "_selectionMode", le.SelectionMode);
                        SetField(e, "_filterType", le.FilterCardType);
                    });

                case CardManipulationType.ChooseFromDiscardToHand:
                    return Make<ChooseFromDiscardToHandEffect>(e =>
                        SetField(e, "_amount", le.CardAmount)
                    );

                case CardManipulationType.ChooseFromDiscardToDeck:
                    return Make<ChooseFromDiscardToDeckEffect>(e =>
                        SetField(e, "_amount", le.CardAmount)
                    );

                case CardManipulationType.AddCardToDeck:
                    return Make<AddCardToDeckEffect>(e =>
                    {
                        SetField(e, "_card", le.CardToAdd);
                        SetField(e, "_amount", le.CardAmount);
                    });

                case CardManipulationType.AddCardToHand:
                    return Make<AddCardToHandEffect>(e =>
                    {
                        SetField(e, "_card", le.CardToAdd);
                        SetField(e, "_amount", le.CardAmount);
                    });

                case CardManipulationType.UpgradeCardThisBattle:
                    return Make<UpgradeCardThisBattleEffect>(e =>
                    {
                        SetField(e, "_selectionMode", le.SelectionMode);
                        SetField(e, "_filterType", le.FilterCardType);
                    });

                case CardManipulationType.UpgradeAllCardsInHand:
                    return Make<UpgradeAllCardsInHandEffect>(_ => { });

                case CardManipulationType.MakeCardRetain:
                    return Make<MakeCardRetainEffect>(e =>
                    {
                        SetField(e, "_selectionMode", le.SelectionMode);
                        SetField(e, "_filterType", le.FilterCardType);
                    });

                case CardManipulationType.MakeAllCardsRetain:
                    return Make<MakeAllCardsRetainEffect>(_ => { });

                case CardManipulationType.ReduceCardCost:
                    return Make<ReduceCardCostEffect>(e =>
                    {
                        SetField(e, "_costReduction", le.CostReduction);
                        SetField(e, "_selectionMode", le.SelectionMode);
                        SetField(e, "_filterType", le.FilterCardType);
                    });

                case CardManipulationType.MakeCardFree:
                    return Make<MakeCardFreeEffect>(e =>
                    {
                        SetField(e, "_selectionMode", le.SelectionMode);
                        SetField(e, "_filterType", le.FilterCardType);
                    });

                case CardManipulationType.ChanceRoll:
                {
                    // Convert nested effects recursively
                    var nestedLegacy =
                        le.ChanceEffects != null
                            ? new List<CardEffect>(le.ChanceEffects)
                            : new List<CardEffect>();
                    var nestedNew = ConvertEffectList(nestedLegacy, errors, assetPath);

                    return Make<ChanceRollEffect>(e =>
                    {
                        SetField(e, "_chancePercent", le.ChancePercent);
                        SetField(e, "_effects", nestedNew);
                    });
                }

                default:
                    errors.Add(
                        $"{assetPath}: unknown CardManipulationType '{le.CardManipulationType}'"
                    );
                    return null;
            }
        }

        #endregion

        #region Reflection helpers
        private static T Make<T>(Action<T> configure)
            where T : BattleEffect, new()
        {
            var instance = new T();
            configure(instance);
            return instance;
        }

        private static void SetField(object obj, string fieldName, object value)
        {
            FieldInfo field = obj.GetType()
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (field != null)
                field.SetValue(obj, value);
            else
                Debug.LogWarning(
                    $"[EffectMigrationTool] Field '{fieldName}' not found on {obj.GetType().Name}"
                );
        }
    }
}
        #endregion
#endif
