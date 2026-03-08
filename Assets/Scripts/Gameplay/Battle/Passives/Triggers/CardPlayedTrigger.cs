using System;
using Crookedile.Data;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Data.Cards;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Fires when the player plays a card from hand.
    /// Optionally filters to a specific <see cref="CardType"/> (e.g. only Pressure cards).
    /// </summary>
    [Serializable]
    public class CardPlayedTrigger : PassiveTriggerBase
    {
        [Tooltip("Enable to restrict this trigger to cards of a specific type.")]
        [SerializeField] private bool _filterByType = false;

        [ShowIf("_filterByType")]
        [Tooltip("Only fire when a card of this type is played.")]
        [SerializeField] private CardType _filterType = CardType.Pressure;

        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<CardPlayedEvent>()) return false;
            var e = ctx.As<CardPlayedEvent>();
            if (!e.IsPlayer) return false;
            if (_filterByType && e.Card != null && e.Card.CardType != _filterType) return false;
            return true;
        }

        public override string TriggerLabel =>
            _filterByType ? $"When you play a {_filterType} card" : "When you play a card";
    }
}
