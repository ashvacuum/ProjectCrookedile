using System;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// Class-wide multipliers on what the player's cards produce. Applied to every player-side
    /// Sway and Support gain resolved through the shared effect helpers, on top of
    /// each card's own numbers. Identity (1, 1) for a class that sets none.
    /// </summary>
    [Serializable]
    public struct ClassModifiers
    {
        [Tooltip(
            "Multiplies every Sway (Opinion push) the player's cards deal. 1 = the shared baseline."
        )]
        [Min(0f)]
        public float SwayMultiplier;

        [Tooltip(
            "Multiplies every Support the player's cards grant. 1 = the shared baseline."
        )]
        [Min(0f)]
        public float ComposureMultiplier;

        /// <summary>No change: both multipliers at 1.</summary>
        public static ClassModifiers Identity =>
            new ClassModifiers { SwayMultiplier = 1f, ComposureMultiplier = 1f };

        /// <summary>Applies <see cref="SwayMultiplier"/>, rounding to the nearest whole point.</summary>
        public int ModifySway(int amount) => Scale(amount, SwayMultiplier);

        /// <summary>Applies <see cref="ComposureMultiplier"/>, rounding to the nearest whole point.</summary>
        public int ModifyComposure(int amount) => Scale(amount, ComposureMultiplier);

        private static int Scale(int amount, float multiplier) =>
            amount <= 0 ? amount : Mathf.RoundToInt(amount * multiplier);
    }
}
