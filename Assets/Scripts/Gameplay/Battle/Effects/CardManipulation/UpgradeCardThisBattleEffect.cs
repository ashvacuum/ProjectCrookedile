using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Upgrades a card in hand for the rest of this battle.
    /// Supports player-choice, random-any, and random-by-type selection modes.
    /// Cards with no upgrade version are excluded from the candidate pool.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class UpgradeCardThisBattleEffect : BattleEffect
    {
        [Tooltip("How the card to upgrade is selected.")]
        [SerializeField] private CardSelectionMode _selectionMode = CardSelectionMode.PlayerChoice;

        [ShowIf("@_selectionMode == CardSelectionMode.RandomByType")]
        [Tooltip("Card type to filter for when using Random By Type.")]
        [SerializeField] private CardType _filterType = CardType.Pressure;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null) return;

            var upgradeable = new System.Collections.Generic.List<CardData>();
            foreach (var c in ctx.Deck.Hand)
                if (c != null && c.CanUpgrade) upgradeable.Add(c);

            if (upgradeable.Count == 0)
            {
                GameLogger.LogInfo<UpgradeCardThisBattleEffect>("No upgradeable cards in hand — no-op");
                return;
            }

            ResolveCardSelection(upgradeable, _selectionMode, _filterType,
                "Choose a card to Upgrade", 1,
                chosen =>
                {
                    if (chosen.Count == 0) return;
                    var upgraded = chosen[0].CreateUpgradedInstance();
                    ctx.Deck.SwapCardInHand(chosen[0], upgraded);
                });
        }

        public override string GetDescription()
        {
            string suffix = _selectionMode switch
            {
                CardSelectionMode.RandomAny    => "a random card",
                CardSelectionMode.RandomByType => $"a random {_filterType} card",
                _                              => "a card",
            };
            return $"Upgrade {suffix} this battle";
        }
    }
}
