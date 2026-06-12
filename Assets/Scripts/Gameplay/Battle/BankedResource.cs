using System;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// A banked per-battle integer pool (Patronage, Attention): persists across turns,
    /// reset at battle start, floored at 0. Change notification goes through the callback
    /// so each resource keeps its own typed event on the bus.
    /// </summary>
    public class BankedResource
    {
        private int _value;
        private readonly Action<int, int> _onChanged; // (oldValue, newValue)

        public BankedResource(Action<int, int> onChanged) => _onChanged = onChanged;

        public int Current => _value;

        /// <summary>Banks the amount. No-op for non-positive amounts.</summary>
        public void Gain(int amount)
        {
            if (amount <= 0)
                return;
            Set(_value + amount);
        }

        /// <summary>Spends if affordable. Returns false (and spends nothing) if short.</summary>
        public bool Spend(int amount)
        {
            if (amount <= 0)
                return true;
            if (_value < amount)
                return false;
            Set(_value - amount);
            return true;
        }

        /// <summary>Empties the pool (battle start).</summary>
        public void Reset() => Set(0);

        private void Set(int value)
        {
            int old = _value;
            _value = Mathf.Max(0, value);
            if (_value != old)
                _onChanged?.Invoke(old, _value);
        }
    }
}
