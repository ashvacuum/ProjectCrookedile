using System;

namespace Crookedile.Gameplay.Battle
{
    // Foundation slice — representative behaviors covering each shape (debuff, pacify-outgoing,
    // pacify-denial, hostility flags, a manager-read buff). The rest are generated in the next phase.

    /// <summary>Deals X less Opinion per stack.</summary>
    [Serializable]
    public sealed class WeakenedStatus : StatusBehavior
    {
        public override string Id => "weakened";
        public override string DisplayName => "Weakened";
        public override bool IsDebuff => true;
        public override float ModifyOutgoingOpinion(float p, int stacks) => p - stacks;
        public override string Describe(int stacks) => $"Deals {stacks} less Opinion.";
    }

    /// <summary>Pacify status — blunts the enemy's push: deals X less Opinion per stack.</summary>
    [Serializable]
    public sealed class GuiltStatus : StatusBehavior
    {
        public override string Id => "guilt";
        public override string DisplayName => "Guilt";
        public override bool IsDebuff => true;
        public override StatusCategory Category => StatusCategory.Pacify;
        public override bool CountsTowardPacify => true;
        public override float ModifyOutgoingOpinion(float p, int stacks) => p - stacks;
        public override string Describe(int stacks) =>
            $"Pacify: deals {stacks} less Opinion. Counts toward conversion.";
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

    /// <summary>
    /// Your follower while it lasts: can't be riled, takes no action against you, and copies
    /// your attacks and defends (<see cref="FanaticFollowers"/>). When it runs out the follower is
    /// disillusioned: back to neutral, plus Jaded (FaithLeaderConfig.JadedOnDisillusion).
    /// </summary>
    [Serializable]
    public sealed class FanaticStatus : StatusBehavior
    {
        public override string Id => "fanatic";
        public override string DisplayName => "Fanatic";
        public override bool IsDebuff => false;
        public override StatusCategory Category => StatusCategory.HostilityFlag;
        public override bool BlocksHostilityGain => true;

        public override string Describe(int stacks) =>
            "Follows your lead: pushes when you attack, adds Support when you defend. Won't act against you.";

        public override void OnDepleted(BattleStats owner, StatusEffectManager statuses)
        {
            owner?.SetHostility(0);
            int jaded = Crookedile.Data.FaithLeaderConfig.Current.JadedOnDisillusion;
            if (jaded > 0)
                statuses?.ApplyStatus(
                    StatusRegistry.Get<JadedStatus>(),
                    jaded,
                    StatusDurationType.Permanent
                );
        }
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
