using System;

namespace Crookedile.Gameplay.Battle
{
    // Foundation slice — representative behaviors covering each shape (debuff, pacify-outgoing,
    // pacify-denial, hostility flags, a manager-read buff). The rest are generated in the next phase.

    /// <summary>Deals X less pressure per stack.</summary>
    [Serializable]
    public sealed class WeakenedStatus : StatusBehavior
    {
        public override string Id => "weakened";
        public override string DisplayName => "Weakened";
        public override bool IsDebuff => true;
        public override float ModifyOutgoingPressure(float p, int stacks) => p - stacks;
        public override string Describe(int stacks) => $"Deals {stacks} less pressure.";
    }

    /// <summary>Pacify status — blunts the enemy's push: deals X less pressure per stack.</summary>
    [Serializable]
    public sealed class GuiltStatus : StatusBehavior
    {
        public override string Id => "guilt";
        public override string DisplayName => "Guilt";
        public override bool IsDebuff => true;
        public override StatusCategory Category => StatusCategory.Pacify;
        public override bool CountsTowardPacify => true;
        public override float ModifyOutgoingPressure(float p, int stacks) => p - stacks;
        public override string Describe(int stacks) =>
            $"Pacify: deals {stacks} less pressure. Counts toward conversion.";
    }

    /// <summary>Pacify status — drops the enemy's shield: gains X less Denial per stack.</summary>
    [Serializable]
    public sealed class ShameStatus : StatusBehavior
    {
        public override string Id => "shame";
        public override string DisplayName => "Shame";
        public override bool IsDebuff => true;
        public override StatusCategory Category => StatusCategory.Pacify;
        public override bool CountsTowardPacify => true;
        public override int ModifyDenialGained(int denial, int stacks) => Math.Max(0, denial - stacks);
        public override string Describe(int stacks) =>
            $"Pacify: gains {stacks} less Denial. Counts toward conversion.";
    }

    /// <summary>Won't listen to reason — hostility reductions are no-ops (the permanent villain).</summary>
    [Serializable]
    public sealed class HardenedStatus : StatusBehavior
    {
        public override string Id => "hardened";
        public override string DisplayName => "Hardened";
        public override bool IsDebuff => true;
        public override StatusCategory Category => StatusCategory.HostilityFlag;
        public override bool BlocksHostilityReduction => true;
        public override string Describe(int stacks) => "Can't be made receptive.";
    }

    /// <summary>Can't be riled — hostility gains are no-ops (the permanent loyalist).</summary>
    [Serializable]
    public sealed class FanaticStatus : StatusBehavior
    {
        public override string Id => "fanatic";
        public override string DisplayName => "Fanatic";
        public override bool IsDebuff => false;
        public override StatusCategory Category => StatusCategory.HostilityFlag;
        public override bool BlocksHostilityGain => true;
        public override string Describe(int stacks) => "Can't be riled up; can still be won over.";
    }

    /// <summary>Steadfast — resists hostility gains by X per stack (protects converts).</summary>
    [Serializable]
    public sealed class DevotionStatus : StatusBehavior
    {
        public override string Id => "devotion";
        public override string DisplayName => "Devotion";
        public override bool IsDebuff => false;
        public override StatusCategory Category => StatusCategory.HostilityFlag;
        public override int HostilityResistPerStack => 1;
        public override string Describe(int stacks) => $"Resists hostility gains by {stacks}.";
    }

    /// <summary>Gain X Support at the start of each turn (read by BattleManager at turn start).</summary>
    [Serializable]
    public sealed class RitualStatus : StatusBehavior
    {
        public override string Id => "ritual";
        public override string DisplayName => "Ritual";
        public override bool IsDebuff => false;
        public override StatusCategory Category => StatusCategory.Special;
        public override string Describe(int stacks) => $"Gain {stacks} Support at the start of each turn.";
    }
}
