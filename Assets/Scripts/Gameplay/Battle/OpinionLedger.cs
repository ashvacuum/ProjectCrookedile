using System;
using Crookedile.Core;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Single owner of the shared battle resources: the Opinion Meter and the two session
    /// shields (Support absorbs opinion drops, Denial absorbs opinion rises).
    ///
    /// This is the <b>command</b> side of opinion changes. Effects call <see cref="ApplyPressure"/>
    /// (or <see cref="RaiseDirect"/>) directly rather than publishing a gameplay event and hoping a
    /// subscriber mutates state. The EventBus is then used purely for <i>notification</i>
    /// (<see cref="DamageDealtEvent"/>, <see cref="OpinionChangedEvent"/>, etc.) so UI and passives
    /// can react. Centralising mutation here removes the re-entrancy bug class that came from
    /// absorbing in one place and re-publishing in another.
    /// </summary>
    public class OpinionLedger
    {
        private int _opinion;
        private readonly int _maxOpinion;

        // Session shields — decay to 0 at turn start before Ritual refills them.
        private int _support; // absorbs opinion drops (enemy attacks)
        private int _denial; // absorbs opinion rises (player cards)

        // Invoked whenever opinion reaches the maximum, so BattleManager can end the battle.
        private readonly Action _onOpinionMaxed;

        // Invoked whenever opinion hits 0 (the loss condition) — symmetric with _onOpinionMaxed.
        private readonly Action _onOpinionZeroed;

        // True while the room is an echo chamber (all enemies receptive). Supplied by BattleManager,
        // evaluated live so halving always reflects the current room state.
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

        #region Pressure pipeline (command)

        /// <summary>
        /// Applies incoming opinion pressure. Enemy attacks (<paramref name="toPlayer"/> = true)
        /// pass through Support before lowering opinion; player pressure passes through Denial
        /// before raising it. Publishes <see cref="DamageDealtEvent"/> AFTER resolution so the
        /// notification can carry the honest outcome: raw amount, shield-absorbed portion, and
        /// the delta the meter actually moved (post echo-halving and 0/max clamping).
        /// </summary>
        public void ApplyPressure(
            int pressure,
            bool toPlayer,
            string attackerName,
            int sourceEnemyIndex,
            int targetEnemyIndex
        )
        {
            if (pressure <= 0)
                return;

            int remaining = toPlayer
                ? AbsorbThroughSupport(pressure)
                : AbsorbThroughDenial(pressure);
            int applied = toPlayer ? Lower(remaining) : Raise(remaining);

            EventBus.Publish(
                new DamageDealtEvent
                {
                    Amount = pressure,
                    Absorbed = pressure - remaining,
                    Applied = applied,
                    IsToPlayer = toPlayer,
                    AttackerName = attackerName,
                    SourceEnemyIndex = sourceEnemyIndex,
                    TargetEnemyIndex = targetEnemyIndex,
                }
            );
        }

        /// <summary>
        /// Raises the Opinion Meter directly, bypassing the Denial buffer.
        /// Used by rallying/heal effects, Regeneration, and the pacify conversion burst.
        /// NOTE (deliberate): echo-chamber halving STILL applies — "direct" bypasses the
        /// Denial shield only, not the room-state penalty.
        /// </summary>
        public void RaiseDirect(int amount) => Raise(amount);

        /// <summary>
        /// Echo-chamber decay: bleeds opinion toward 0 while the whole room is receptive.
        /// Bypasses shields — it is ambient sentiment loss, not an attack.
        /// </summary>
        public void DecayOpinion(int amount) => Lower(amount);

        #endregion

        #region Opinion mutation

        /// <summary>Raises opinion; returns the delta actually applied (post halving/clamp).</summary>
        private int Raise(int amount)
        {
            if (amount <= 0)
                return 0;
            // Echo chamber: converting the whole room is inefficient — gains are halved.
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

        #region Session shields (Support / Denial)

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

        /// <summary>
        /// Drains up to <paramref name="amount"/> Support without routing anything to the meter.
        /// Used by "lose Support" effects. Returns the amount actually removed.
        /// </summary>
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
        /// A shield decays at the start of its OWNER'S turn, so it lives through the opponent's
        /// full turn: Support banked on your turn absorbs enemy attacks and expires when your
        /// next turn starts; Denial banked on the enemy turn blocks your cards and expires when
        /// the enemy's next turn starts. (Ritual refills right after the wipe.)
        /// </summary>
        public void DecayShields(bool isPlayerTurn)
        {
            if (isPlayerTurn)
                ConsumeAllSupport();
            else
                ConsumeAllDenial();
        }

        private int AbsorbThroughSupport(int pressure)
        {
            if (pressure <= 0 || _support <= 0)
                return pressure;
            int absorbed = Mathf.Min(pressure, _support);
            int old = _support;
            _support -= absorbed;
            EventBus.Publish(new SupportChangedEvent { OldValue = old, NewValue = _support });
            return pressure - absorbed;
        }

        private int AbsorbThroughDenial(int pressure)
        {
            if (pressure <= 0 || _denial <= 0)
                return pressure;
            int absorbed = Mathf.Min(pressure, _denial);
            int old = _denial;
            _denial -= absorbed;
            EventBus.Publish(new DenialChangedEvent { OldValue = old, NewValue = _denial });
            return pressure - absorbed;
        }

        private void ConsumeAllSupport()
        {
            if (_support <= 0)
                return;
            int old = _support;
            _support = 0;
            // IsDecay: ambient turn-start expiry, not an attack — feedback layers skip the
            // "shield lost" sting for it.
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
