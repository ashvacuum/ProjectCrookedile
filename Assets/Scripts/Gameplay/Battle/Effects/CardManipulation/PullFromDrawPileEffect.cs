using System;
using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Pull: the player takes chosen card(s) from the draw pile into hand, from the whole pile
    /// or from its top few. Pulling is paid in Hostility: each pull can raise it, flat or on the
    /// escalating Fibonacci scale (1, 1, 2, 3, 5…). Cards left unpicked keep their order.
    /// ponytail: "put the rest back in any order" keeps the existing order — the card-choice
    /// panel can't reorder; add a reorder step when the UI can.
    /// </summary>
    [Serializable]
    public class PullFromDrawPileEffect : BattleEffect
    {
        public enum PullSource
        {
            WholeDrawPile,
            TopOfDrawPile,
        }

        public enum HostilityScale
        {
            Flat, // every pull costs the same
            Fibonacci, // the nth pull of this play costs Hostility × Fib(n)
        }

        [Tooltip("Where the player picks from.")]
        [SerializeField]
        private PullSource _source = PullSource.WholeDrawPile;

        [ShowIf("_source", PullSource.TopOfDrawPile)]
        [MinValue(1)]
        [Tooltip("How many cards from the top the player looks at.")]
        [SerializeField]
        private int _lookAt = 5;

        [MinValue(1)]
        [Tooltip("How many cards to take (the most, when Up To is on).")]
        [SerializeField]
        private int _count = 1;

        [Tooltip("Up to: the player may take fewer, including none.")]
        [SerializeField]
        private bool _upTo = false;

        [MinValue(0)]
        [Tooltip("Only cards with at least this printed cost can be taken (0 = any).")]
        [SerializeField]
        private int _minimumPrintedCost = 0;

        [Tooltip("Pulled cards cost 0 for the rest of the turn.")]
        [SerializeField]
        private bool _freeThisTurn = false;

        [MinValue(0)]
        [Tooltip("Hostility raised per pull (scaled by Hostility Scale). 0 = pulling is free.")]
        [SerializeField]
        private int _hostilityPerPull = 0;

        [Tooltip("How the per-pull Hostility grows within one play.")]
        [SerializeField]
        private HostilityScale _hostilityScale = HostilityScale.Flat;

        [Tooltip("Whose Hostility each pull raises.")]
        [SerializeField]
        private TargetType _hostilityTarget = TargetType.AllOpponents;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null)
                return;

            IReadOnlyList<CardData> pool =
                _source == PullSource.TopOfDrawPile
                    ? ctx.Deck.PeekTop(_lookAt)
                    : ctx.Deck.DrawPile;
            var candidates = new List<CardData>();
            foreach (var card in pool)
                if (card != null && card.PrintedCost >= _minimumPrintedCost)
                    candidates.Add(card);

            ResolveCardSelection(
                candidates,
                CardSelectionMode.PlayerChoice,
                CardType.Pressure, // ignored by PlayerChoice
                ChoiceTitle(),
                _count,
                chosen =>
                {
                    int pulled = 0;
                    foreach (var card in chosen)
                    {
                        if (!ctx.Deck.PullFromDrawPile(card))
                            continue;
                        pulled++;
                        if (_freeThisTurn)
                            ctx.Deck.MakeCardFreeThisTurn(card);
                        int hostility =
                            _hostilityScale == HostilityScale.Fibonacci
                                ? _hostilityPerPull * NepoBabyRules.Fibonacci(pulled)
                                : _hostilityPerPull;
                        if (hostility > 0)
                            foreach (var (stats, _) in ctx.GetTargets(_hostilityTarget))
                                ctx.LastHostilityGained += stats.GainHostility(hostility);
                    }
                    GameLogger.LogInfo<PullFromDrawPileEffect>($"Pulled {pulled} card(s)");
                },
                allowFewer: _upTo
            );
        }

        private string ChoiceTitle()
        {
            string from =
                _source == PullSource.TopOfDrawPile ? $"the top {_lookAt}" : "your draw pile";
            string n = _upTo ? $"up to {_count}" : _count.ToString();
            return $"Take {n} from {from}";
        }

        public override string GetDescription()
        {
            string what = _count == 1 ? "a card" : $"{_count} cards";
            if (_upTo)
                what = _count >= 10 ? "any number of cards" : $"up to {_count} cards";
            if (_minimumPrintedCost > 0)
                what += $" costing {_minimumPrintedCost}+";
            string from =
                _source == PullSource.TopOfDrawPile
                    ? $"Look at the top {_lookAt} of your draw pile and take {what}"
                    : $"Take {what} from your draw pile";
            if (_freeThisTurn)
                from += ". It costs 0 this turn";
            if (_hostilityPerPull > 0)
            {
                string who = _hostilityTarget == TargetType.AllOpponents ? "all enemies" : $"{_hostilityTarget}";
                from +=
                    _hostilityScale == HostilityScale.Fibonacci
                        ? $". Each pull: Aggravate {who}, more each time (1, 1, 2, 3, 5…)"
                        : $". Each pull: Aggravate {_hostilityPerPull} ({who})";
            }
            return from;
        }
    }
}
