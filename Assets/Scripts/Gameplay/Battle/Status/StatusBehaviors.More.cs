using System;

namespace Crookedile.Gameplay.Battle
{
    // Phase 2 — the remaining statuses. Hooks mirror the old StatusEffectManager switches; statuses
    // whose logic isn't a simple aggregate (turn ticks, skip chances, thresholds, card-replay) carry
    // the metadata here and are read by type in the manager / BattleManager.

    // ---- Debuffs ----------------------------------------------------------

    /// <summary>Takes 50% more pressure.</summary>
    [Serializable]
    public sealed class VulnerableStatus : StatusBehavior
    {
        public override string Id => "vulnerable";
        public override string DisplayName => "Vulnerable";
        public override bool IsDebuff => true;
        public override float ModifyIncomingPressure(float p, int stacks, int attackerHostility) =>
            stacks > 0 ? p * 1.5f : p;
        public override string Describe(int stacks) => "Takes 50% more pressure.";
    }

    /// <summary>Gains 25% less Support.</summary>
    [Serializable]
    public sealed class FrailStatus : StatusBehavior
    {
        public override string Id => "frail";
        public override string DisplayName => "Frail";
        public override bool IsDebuff => true;
        public override int ModifySupportGained(int support, int stacks) =>
            stacks > 0 ? (int)Math.Round(support * 0.75f) : support;
        public override string Describe(int stacks) => "Gains 25% less Support.";
    }

    /// <summary>All cards cost +1 AP.</summary>
    [Serializable]
    public sealed class EntangledStatus : StatusBehavior
    {
        public override string Id => "entangled";
        public override string DisplayName => "Entangled";
        public override bool IsDebuff => true;
        public override int ModifyCardCost(int cost, int stacks) => stacks > 0 ? cost + 1 : cost;
        public override string Describe(int stacks) => "Cards cost +1 AP.";
    }

    /// <summary>Next attack against this target deals double, then it fades.</summary>
    [Serializable]
    public sealed class ExposedStatus : StatusBehavior
    {
        public override string Id => "exposed";
        public override string DisplayName => "Exposed";
        public override bool IsDebuff => true;
        public override bool ConsumedOnIncomingHit => true;
        public override float ModifyIncomingPressure(float p, int stacks, int attackerHostility) =>
            stacks > 0 ? p * 2f : p;
        public override string Describe(int stacks) => "Next attack against it deals double.";
    }

    /// <summary>Reputation bleed — take X pressure at end of turn (read at turn end by BattleManager).</summary>
    [Serializable]
    public sealed class SmearStatus : StatusBehavior
    {
        public override string Id => "smear";
        public override string DisplayName => "Smear";
        public override bool IsDebuff => true;
        public override string Describe(int stacks) => $"Take {stacks} pressure at end of turn.";
    }

    /// <summary>Effect values are randomised each turn (handled by BattleManager).</summary>
    [Serializable]
    public sealed class ConfusedStatus : StatusBehavior
    {
        public override string Id => "confused";
        public override string DisplayName => "Confused";
        public override bool IsDebuff => true;
        public override string Describe(int stacks) => "Effect values are randomised each turn.";
    }

    /// <summary>Silenced — player: can't play Rhetoric; enemy: skips its action (read by type).</summary>
    [Serializable]
    public sealed class SilencedStatus : StatusBehavior
    {
        public override string Id => "silenced";
        public override string DisplayName => "Silenced";
        public override bool IsDebuff => true;
        public override string Describe(int stacks) => "Silenced — no voice this turn.";
    }

    /// <summary>Skips its next action (read by type).</summary>
    [Serializable]
    public sealed class StunnedStatus : StatusBehavior
    {
        public override string Id => "stunned";
        public override string DisplayName => "Stunned";
        public override bool IsDebuff => true;
        public override string Describe(int stacks) => "Skips its next action.";
    }

    /// <summary>Takes bonus pressure equal to attacker Hostility per stack.</summary>
    [Serializable]
    public sealed class RattledStatus : StatusBehavior
    {
        public override string Id => "rattled";
        public override string DisplayName => "Rattled";
        public override bool IsDebuff => true;
        public override float ModifyIncomingPressure(float p, int stacks, int attackerHostility) =>
            p + attackerHostility * stacks;
        public override string Describe(int stacks) =>
            $"Takes bonus pressure = attacker Hostility x {stacks}.";
    }

    /// <summary>Pacify status — soft chance to hold back its action per stack (skip read by type).</summary>
    [Serializable]
    public sealed class DoubtStatus : StatusBehavior
    {
        public override string Id => "doubt";
        public override string DisplayName => "Doubt";
        public override bool IsDebuff => true;
        public override StatusCategory Category => StatusCategory.Pacify;
        public override bool CountsTowardPacify => true;
        public override string Describe(int stacks) =>
            $"Pacify: ~{stacks * 25}% chance to hold back. Counts toward conversion.";
    }

    /// <summary>Threshold status — raises this enemy's pacify cost by 1 per stack (read by type).</summary>
    [Serializable]
    public sealed class JadedStatus : StatusBehavior
    {
        public override string Id => "jaded";
        public override string DisplayName => "Jaded";
        public override bool IsDebuff => false;
        public override StatusCategory Category => StatusCategory.Threshold;
        public override string Describe(int stacks) => $"Pacify cost +{stacks}. Permanent.";
    }

    // ---- Buffs ------------------------------------------------------------

    /// <summary>Deals X more pressure per stack.</summary>
    [Serializable]
    public sealed class StrengthStatus : StatusBehavior
    {
        public override string Id => "strength";
        public override string DisplayName => "Strength";
        public override bool IsDebuff => false;
        public override float ModifyOutgoingPressure(float p, int stacks) => p + stacks;
        public override string Describe(int stacks) => $"Deals {stacks} more pressure.";
    }

    /// <summary>Gains X more Support per card.</summary>
    [Serializable]
    public sealed class DexterityStatus : StatusBehavior
    {
        public override string Id => "dexterity";
        public override string DisplayName => "Dexterity";
        public override bool IsDebuff => false;
        public override int ModifySupportGained(int support, int stacks) => support + stacks;
        public override string Describe(int stacks) => $"Gains {stacks} more Support per card.";
    }

    /// <summary>Cards cost X less AP (this turn).</summary>
    [Serializable]
    public sealed class FocusStatus : StatusBehavior
    {
        public override string Id => "focus";
        public override string DisplayName => "Focus";
        public override bool IsDebuff => false;
        public override int ModifyCardCost(int cost, int stacks) => cost - stacks;
        public override string Describe(int stacks) => $"Cards cost {stacks} less AP this turn.";
    }

    /// <summary>Cards cost X less AP this turn.</summary>
    [Serializable]
    public sealed class EnergizedStatus : StatusBehavior
    {
        public override string Id => "energized";
        public override string DisplayName => "Energized";
        public override bool IsDebuff => false;
        public override int ModifyCardCost(int cost, int stacks) => cost - stacks;
        public override string Describe(int stacks) => $"Cards cost {stacks} less AP this turn.";
    }

    /// <summary>Reduces incoming pressure by X.</summary>
    [Serializable]
    public sealed class PlatedStatus : StatusBehavior
    {
        public override string Id => "plated";
        public override string DisplayName => "Plated";
        public override bool IsDebuff => false;
        public override float ModifyIncomingPressure(float p, int stacks, int attackerHostility) =>
            p - stacks;
        public override string Describe(int stacks) => $"Reduces incoming pressure by {stacks}.";
    }

    /// <summary>Raise Opinion by X at end of turn (read at turn end by BattleManager).</summary>
    [Serializable]
    public sealed class RegenerationStatus : StatusBehavior
    {
        public override string Id => "regeneration";
        public override string DisplayName => "Regeneration";
        public override bool IsDebuff => false;
        public override string Describe(int stacks) => $"Raise Opinion by {stacks} at end of turn.";
    }

    /// <summary>Take only 1 from attacks, then it fades.</summary>
    [Serializable]
    public sealed class IntangibleStatus : StatusBehavior
    {
        public override string Id => "intangible";
        public override string DisplayName => "Intangible";
        public override bool IsDebuff => false;
        public override bool ConsumedOnIncomingHit => true;
        public override bool IncomingOverride => true;
        public override float ModifyIncomingPressure(float p, int stacks, int attackerHostility) =>
            stacks > 0 ? 1f : p;
        public override string Describe(int stacks) => "Takes only 1 from attacks.";
    }

    /// <summary>Reflect X pressure to the meter when hit (read by type for the reflection).</summary>
    [Serializable]
    public sealed class ThornsStatus : StatusBehavior
    {
        public override string Id => "thorns";
        public override string DisplayName => "Thorns";
        public override bool IsDebuff => false;
        public override string Describe(int stacks) => $"Reflect {stacks} pressure when hit.";
    }

    // ---- Special ----------------------------------------------------------

    /// <summary>Deal X to a random enemy per card played this turn (read by type by BattleManager).</summary>
    [Serializable]
    public sealed class MomentumStatus : StatusBehavior
    {
        public override string Id => "momentum";
        public override string DisplayName => "Momentum";
        public override bool IsDebuff => false;
        public override StatusCategory Category => StatusCategory.Special;
        public override string Describe(int stacks) =>
            $"Deal {stacks} to a random enemy per card played this turn.";
    }

    /// <summary>The next card played is resolved twice (read by type by BattleManager).</summary>
    [Serializable]
    public sealed class EchoStatus : StatusBehavior
    {
        public override string Id => "echo";
        public override string DisplayName => "Echo";
        public override bool IsDebuff => false;
        public override StatusCategory Category => StatusCategory.Special;
        public override string Describe(int stacks) => "The next card played is resolved twice.";
    }

    /// <summary>Freshly betrayed — deals +X bonus pressure per stack, fading over a turn or two.</summary>
    [Serializable]
    public sealed class TurncoatStatus : StatusBehavior
    {
        public override string Id => "turncoat";
        public override string DisplayName => "Turncoat";
        public override bool IsDebuff => false;
        public override StatusCategory Category => StatusCategory.Special;
        public override float ModifyOutgoingPressure(float p, int stacks) => p + stacks;
        public override string Describe(int stacks) => $"Freshly betrayed: deals +{stacks} pressure.";
    }
}
