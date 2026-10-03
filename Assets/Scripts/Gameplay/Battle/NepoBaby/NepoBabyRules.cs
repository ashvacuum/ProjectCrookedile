using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Cards;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Nepo Baby's shared rules (docs/nepo-baby-class.md section 4). Tunable values live on
    /// <see cref="NepoBabyConfig"/>; this holds the logic every Burn, Pull and escalation shares.
    /// </summary>
    public static class NepoBabyRules
    {
        /// <summary>
        /// Can <paramref name="card"/> be burned (exhausted from hand as a cost)? Policies can't,
        /// anywhere, unless the config says otherwise. This is the single check every Burn uses.
        /// </summary>
        public static bool CanBurn(CardData card) =>
            card != null
            && (card.CardType != CardType.Policy || NepoBabyConfig.Current.PoliciesBurnable);

        /// <summary>
        /// The escalating scale for "each one costs more" Hostility (I Have All the Cards,
        /// Dynasty): 1, 1, 2, 3, 5, 8… for n = 1, 2, 3… Returns 0 for n below 1.
        /// </summary>
        public static int Fibonacci(int n)
        {
            if (n < 1)
                return 0;
            int a = 1,
                b = 1;
            for (int i = 2; i < n; i++)
            {
                int next = a + b;
                a = b;
                b = next;
            }
            return n <= 2 ? 1 : b;
        }
    }

    /// <summary>
    /// Nepo Baby's per-battle state: escalation counters for effects whose Hostility price
    /// grows each time they're used this battle. Reset at battle start.
    /// </summary>
    public class NepoBabyState
    {
        private readonly Dictionary<string, int> _escalations = new Dictionary<string, int>();

        /// <summary>Clears every escalation counter. Called at battle start.</summary>
        public void Reset() => _escalations.Clear();

        /// <summary>
        /// Counts one more use of <paramref name="key"/> this battle and returns how many uses
        /// that makes (1 for the first).
        /// </summary>
        public int NextEscalation(string key)
        {
            _escalations.TryGetValue(key, out int count);
            _escalations[key] = ++count;
            return count;
        }
    }
}
