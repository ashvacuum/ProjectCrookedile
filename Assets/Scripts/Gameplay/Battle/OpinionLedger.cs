using System;
using Crookedile.Core;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Sole owner of the Opinion Meter and both session shields (Support absorbs drops, Denial
    /// absorbs rises) — the command side, so effects mutate through here and the EventBus only
    /// notifies, which is what keeps the old absorb-here-republish-there re-entrancy bugs out.
    /// </summary>
    public class OpinionLedger
    {
        private int _opinion;
        private readonly int _maxOpinion;

        // Both decay to 0 at turn start, before Ritual refills them.
        private int _support; // absorbs opinion drops (enemy attacks)
        private int _denial; // absorbs opinion rises (player cards)

        // Win condition — lets BattleManager end the battle.
        private readonly Action _onOpinionMaxed;

        // Loss condition, symmetric with _onOpinionMaxed.
        private readonly Action _onOpinionZeroed;

        // Live-evaluated so halving always reflects current room state, not a stale snapshot.
        private readonly Func<bool> _isEchoChamber;

        public OpinionLedger(
            int maxOpinion,
            int startingOpinion,
            Action onOpinionMaxed,
            Func<bool> isEchoChamber = null,
            Action onOpinionZeroed = null
        )
        {
            _maxOpinion = Mathf.Max(1, maxOpinion);
            _opinion = Mathf.Clamp(startingOpinion, 0, _maxOpinion);
            _onOpinionMaxed = onOpinionMaxed;
            _onOpinionZeroed = onOpinionZeroed;
            _isEchoChamber = isEchoChamber ?? (() => false);
        }

        #region Properties

        public int CurrentOpinion => _opinion;
        public int MaxOpinion => _maxOpinion;
        public float OpinionPercentage => (float)_opinion / _maxOpinion;
        public int CurrentSupport => _support;
        public int CurrentDenial => _denial;

        #endregion

        #region Opinion pipeline (command)

        /// <summary>
        /// Routes a shift through the matching shield (toPlayer through Support, else Denial) and
        /// publishes <see cref="DamageDealtEvent"/> only after resolving, so the event can carry the
        /// honest raw/absorbed/applied split rather than the requested amount.
        /// </summary>
        public void ApplyOpinionShift(
            int amount,
            bool toPlayer,
            string attackerName,
            int sourceEnemyIndex,
            int targetEnemyIndex
        )
        {
            if (amount <= 0)
                return;

            int remaining = toPlayer
                ? AbsorbThroughSupport(amount)
                : AbsorbThroughDenial(amount);
            int applied = toPlayer ? Lower(remaining) : Raise(remaining);

            EventBus.Publish(
                new DamageDealtEvent
                {
                    Amount = amount,
                    Absorbed = amount - remaining,
                    Applied = applied,
                    IsToPlayer = toPlayer,
                    AttackerName = attackerName,
                    SourceEnemyIndex = sourceEnemyIndex,
                    TargetEnemyIndex = targetEnemyIndex,
                }
            );
        }

        /// <summary>
        /// Raises opinion past the Denial shield (rally/Regeneration/pacify burst) — note "direct"
        /// deliberately still eats echo-chamber halving, which is a room penalty, not a shield.
        /// </summary>
        public void RaiseDirect(int amount) => Raise(amount);

        /// <summary>
        /// Bleeds opinion toward 0 while the room is an echo chamber, bypassing shields because
        /// it is ambient sentiment loss rather than an attack.
        /// </summary>
        public void DecayOpinion(int amount) => Lower(amount);

        #endregion

        #region Opinion mutation

        /// <summary>Raises opinion; returns the delta actually applied (post halving/clamp).</summary>
        private int Raise(int amount)
        {
            if (amount <= 0)
                return 0;
            // Converting an already-converted room is inefficient, so gains halve.
            if (_isEchoChamber())
                amount /= 2;
            if (amount <= 0)
                return 0;
            int old = _opinion;
            _opinion = Mathf.Min(_opinion + amount, _maxOpinion);
            if (_opinion == old)
                return 0;
            EventBus.Publish(
                new OpinionChangedEvent
                {
                    OldValue = old,
                    NewValue = _opinion,
                    MaxValue = _maxOpinion,
                    WasRaisedByPlayer = true,
                }
            );
            if (_opinion >= _maxOpinion)
                _onOpinionMaxed?.Invoke();
            return _opinion - old;
        }

        /// <summary>Lowers opinion; returns the delta actually applied (post clamp at 0).</summary>
        private int Lower(int amount)
        {
            if (amount <= 0)
                return 0;
            int old = _opinion;
            _opinion = Mathf.Max(_opinion - amount, 0);
            if (_opinion == old)
                return 0;
            EventBus.Publish(
                new OpinionChangedEvent
                {
                    OldValue = old,
                    NewValue = _opinion,
                    MaxValue = _maxOpinion,
                    WasRaisedByPlayer = false,
                }
            );
            if (_opinion <= 0)
                _onOpinionZeroed?.Invoke();
            return old - _opinion;
        }

        #endregion

        #region Support / Denial

        public void GainSupport(int amount)
        {
            if (amount <= 0)
                return;
            int old = _support;
            _support += amount;
            EventBus.Publish(new SupportChangedEvent { OldValue = old, NewValue = _support });
        }

        public void GainDenial(int amount)
        {
            if (amount <= 0)
                return;
            int old = _denial;
            _denial += amount;
            EventBus.Publish(new DenialChangedEvent { OldValue = old, NewValue = _denial });
        }

        /// <summary>Drains up to <paramref name="amount"/> Support without touching the meter, returning what was actually removed.</summary>
        public int SpendSupport(int amount)
        {
            if (amount <= 0 || _support <= 0)
                return 0;
            int spent = Mathf.Min(amount, _support);
            int old = _support;
            _support -= spent;
            EventBus.Publish(new SupportChangedEvent { OldValue = old, NewValue = _support });
            return spent;
        }

        /// <summary>
        /// Expires a shield at its OWNER'S turn start, not every half-turn, so each one survives
        /// the opponent's full turn and actually gets a chance to absorb something.
        /// </summary>
        public void DecaySupportAndDenial(bool isPlayerTurn)
        {
            if (isPlayerTurn)
                ConsumeAllSupport();
            else
                ConsumeAllDenial();
        }

        private int AbsorbThroughSupport(int amount)
        {
            if (amount <= 0 || _support <= 0)
                return amount;
            int absorbed = Mathf.Min(amount, _support);
            int old = _support;
            _support -= absorbed;
            EventBus.Publish(new SupportChangedEvent { OldValue = old, NewValue = _support });
            return amount - absorbed;
        }

        private int AbsorbThroughDenial(int amount)
        {
            if (amount <= 0 || _denial <= 0)
                return amount;
            int absorbed = Mathf.Min(amount, _denial);
            int old = _denial;
            _denial -= absorbed;
            EventBus.Publish(new DenialChangedEvent { OldValue = old, NewValue = _denial });
            return amount - absorbed;
        }

        private void ConsumeAllSupport()
        {
            if (_support <= 0)
                return;
            int old = _support;
            _support = 0;
            // IsDecay marks this as turn-start expiry so feedback skips the "Support lost" sting.
            EventBus.Publish(
                new SupportChangedEvent
                {
                    OldValue = old,
                    NewValue = 0,
                    IsDecay = true,
                }
            );
        }

        private void ConsumeAllDenial()
        {
            if (_denial <= 0)
                return;
            int old = _denial;
            _denial = 0;
            EventBus.Publish(
                new DenialChangedEvent
                {
                    OldValue = old,
                    NewValue = 0,
                    IsDecay = true,
                }
            );
        }

        #endregion
    }
}
