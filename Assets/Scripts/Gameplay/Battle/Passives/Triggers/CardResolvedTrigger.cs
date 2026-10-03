using System;
using Crookedile.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Fires once a card the player played from hand has fully resolved and left the hand
    /// (into the discard, or the exhaust pile). Use it for effects that act on the played card
    /// afterwards, such as sending it back to hand.
    /// </summary>
    [Serializable]
    public class CardResolvedTrigger : PassiveTriggerBase
    {
        [Tooltip("Enable to restrict this trigger to cards of a specific type.")]
        [SerializeField]
        private bool _filterByType = false;

        [ShowIf("_filterByType")]
        [Tooltip("Only fire when a card of this type resolves.")]
        [SerializeField]
        private CardType _filterType = CardType.Pressure;

        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<CardPlayResolvedEvent>())
                return false;
            var card = ctx.As<CardPlayResolvedEvent>().Card;
            return card != null && (!_filterByType || card.CardType == _filterType);
        }

        public override Type EventType => typeof(CardPlayResolvedEvent);

        public override string TriggerLabel =>
            _filterByType ? $"After you play a {_filterType} card" : "After you play a card";
    }
}
