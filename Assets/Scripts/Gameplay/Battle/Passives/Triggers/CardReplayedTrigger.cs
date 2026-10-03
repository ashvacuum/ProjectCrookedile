using System;
using Crookedile.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Fires when a card replays (resolves again without being played). Optionally filters by
    /// card type and by which extra play it was: 1 = the card's second play (a double play),
    /// 2 = its third.
    /// </summary>
    [Serializable]
    public class CardReplayedTrigger : PassiveTriggerBase
    {
        [Tooltip("Enable to restrict this trigger to cards of a specific type.")]
        [SerializeField]
        private bool _filterByType = false;

        [ShowIf("_filterByType")]
        [Tooltip("Only fire when a card of this type replays.")]
        [SerializeField]
        private CardType _filterType = CardType.Rhetoric;

        [Tooltip(
            "Only fire on this extra play: 1 = a card's second play (a double play), 2 = its "
                + "third. 0 = any replay."
        )]
        [MinValue(0)]
        [SerializeField]
        private int _replayNumber = 0;

        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<CardReplayedEvent>())
                return false;
            var e = ctx.As<CardReplayedEvent>();
            if (e.Card == null)
                return false;
            if (_filterByType && e.Card.CardType != _filterType)
                return false;
            return _replayNumber <= 0 || e.ReplayNumber == _replayNumber;
        }

        public override Type EventType => typeof(CardReplayedEvent);

        public override string TriggerLabel
        {
            get
            {
                string card = _filterByType ? $"a {_filterType} card" : "a card";
                return _replayNumber switch
                {
                    1 => $"When {card} is played twice",
                    2 => $"When {card} is played a third time",
                    0 => $"When {card} replays",
                    _ => $"When {card} replays (extra play #{_replayNumber})",
                };
            }
        }
    }
}
