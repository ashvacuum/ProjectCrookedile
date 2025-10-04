namespace Crookedile.Data
{
    /// <summary>
    /// Card types following Slay the Spire model.
    /// Attack = damage, Skill = utility/defense, Power = passive effects.
    /// </summary>
    public enum CardType
    {
        Attack,     // Deals damage to Confidence or Ego
        Skill,      // Utility: block, draw, healing, manipulation
        Power       // Big effects, usually expensive or risky
    }

    /// <summary>
    /// Card rarity determines acquisition chance and power level.
    /// </summary>
    public enum CardRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    /// <summary>
    /// Card frame/tier following Slay the Spire model.
    /// Each tier is separate - Basic upgrades to Basic+, Enhanced to Enhanced+, etc.
    /// Higher tiers are acquired through gameplay (beating opponents, crises, thresholds).
    /// </summary>
    public enum CardTier
    {
        Basic,      // Common cards from shops and basic rewards
        Enhanced,   // Gained from beating normal opponents
        Rare        // Gained from crises, thresholds, special events
    }

    /// <summary>
    /// Player origin types. Each has unique starting deck and passive abilities.
    /// Simplified to 3 distinct archetypes.
    /// </summary>
    public enum OriginType
    {
        FaithLeader,    // Religious leader - defensive, Confidence-focused
        NepoBaby,       // Nepo baby - resource manipulation, Influence-focused
        Actor           // Celebrity/Actor - charismatic, versatile
    }

    /// <summary>
    /// Campaign/Overworld resources that persist across battles.
    /// Simplified to 3 core resources for cleaner gameplay.
    /// </summary>
    public enum CampaignResourceType
    {
        Funds,      // ₱ - Campaign funds for buying cards, upgrades, etc.
        Heat,       // H - Scandal meter (0-100), lose if too high
        Influence   // Political influence/power, win condition
    }

    /// <summary>
    /// Battle-specific effect types for in-combat card effects.
    /// Separates battle mechanics from campaign/overworld effects.
    /// </summary>
    public enum BattleEffectType
    {
        // Damage Types
        ConfidenceDamage,       // Damage opponent's Confidence (shield breaking)
        EgoDamage,              // Damage opponent's Ego (amplified when Confidence is low)

        // Healing Types
        ConfidenceRestore,      // Restore your Confidence (rebuild shield)
        EgoRestore,             // Restore your Ego (direct healing)

        // Resource Types
        GainActionPoints,       // Gain extra action points this turn
        DrawCards,              // Draw cards from deck
        DiscardCards,           // Discard cards from hand (self or opponent)

        // Defense Types
        GainBlock,              // Temporary damage reduction this turn

        // Status Effects
        ApplyBuff,              // Positive status effect (increases stats)
        ApplyDebuff,            // Negative status effect (decreases stats)

        // Special
        DestroyCard,            // Remove a card from play permanently
        ExhaustCard             // Remove card from deck until end of battle
    }

    /// <summary>
    /// Campaign/Overworld effect types for post-battle rewards and events.
    /// These affect the player's campaign state, not battle state.
    /// </summary>
    public enum CampaignEffectType
    {
        // Resource Gains/Losses (3 core resources only)
        GainFunds,              // Gain ₱
        LoseFunds,              // Lose ₱
        GainHeat,               // Increase Heat (scandal)
        LoseHeat,               // Decrease Heat
        GainInfluence,          // Gain Influence (political power)
        LoseInfluence,          // Lose Influence

        // Card Collection
        AddCardToDeck,          // Permanently add card to deck
        RemoveCardFromDeck,     // Permanently remove card from deck
        UpgradeCard,            // Upgrade a card to + version
        TransformCard,          // Transform one card into another

        // Progression
        UnlockLocation,         // Unlock new map location
        UnlockCard,             // Unlock card for future acquisition
        TriggerEvent,           // Trigger a specific event
        AdvanceDay              // Skip to next day
    }

    public enum TargetType
    {
        Self,
        Opponent,
        All,
        Random
    }

    public enum CostType
    {
        None,
        ActionPoints,       // Battle resource - energy to play cards
        Funds,              // Campaign resource ₱
        Influence           // Campaign resource - political power/favors
    }

    /// <summary>
    /// Battle-specific resources that exist only during card battles.
    /// </summary>
    public enum BattleResourceType
    {
        Ego,            // True HP - lose battle if this reaches 0
        Confidence,     // Shield/multiplier - protects Ego, affects Ego damage vulnerability
        ActionPoints,   // Energy to play cards each turn
        Block           // Temporary damage reduction (expires end of turn)
    }

    public enum GamePhase
    {
        MainMenu,
        CharacterSelect,
        Campaign,
        Battle,
        Event,
        Shop,
        GameOver,
        Victory
    }

    public enum BattlePhase
    {
        Setup,
        PlayerTurn,
        OpponentTurn,
        Victory,
        Defeat
    }
}
