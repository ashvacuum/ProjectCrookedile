using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Stacks are remaining enemy turns; hostility crossing consumes them before the Sway payoff.</summary>
    [Serializable]
    public sealed class PoisonedPerceptionStatus : StatusBehavior
    {
        public int HostilityPerTurn { get; } = 2;
        public int SwayOnHostile { get; } = 10;

        public PoisonedPerceptionStatus() { }

        public PoisonedPerceptionStatus(int hostilityPerTurn, int swayOnHostile)
        {
            HostilityPerTurn = Math.Max(1, hostilityPerTurn);
            SwayOnHostile = Math.Max(1, swayOnHostile);
        }

        public override string Id => "poisoned_perception";
        public override string DisplayName => "Poisoned Perception";
        public override bool IsDebuff => true;

        public override string Describe(int stacks) =>
            $"Gain {HostilityPerTurn} Hostility at the start of each enemy turn for {stacks} turns. Becoming hostile removes this and deals {SwayOnHostile} Sway.";
    }
}
