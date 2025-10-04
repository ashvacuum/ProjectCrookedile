namespace Crookedile.Data
{
    /// <summary>
    /// Card types following Griftlands negotiation model.
    /// Diplomacy = peaceful persuasion, Hostility = aggressive tactics, Manipulate = utility/resources.
    /// </summary>
    public enum CardType
    {
        Diplomacy,   // Green - Peaceful persuasion, build relationships
        Hostility,   // Red - Aggressive tactics, threats, pressure
        Manipulate   // Purple - Utility, card draw, resource manipulation
    }

    /// <summary>
    /// Card rarity determines acquisition chance, power level, and visual frame.
    /// </summary>
    public enum CardRarity
    {
        Basic,     // Basic cards from shops and starter decks
        Enhanced,   // Enhanced effects, moderate acquisition difficulty
        Rare,       // Powerful effects, harder to acquire
        Legendary   // Game-changing abilities, very rare
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
        // Core Damage/Healing
        ResolveDamage,                  // Damage opponent's Resolve (HP)
        ResolveHeal,                    // Restore your Resolve (HP)
        RandomDamage,                   // Deal random damage (Actor All or Nothing)

        // Composure (Offensive Buff)
        GainComposure,                  // Build Composure stacks (+damage)
        LoseComposure,                  // Lose Composure stacks
        ResolveDamageEqualToComposure,  // Deal damage = Composure (Faith Leader Blessing)
        ConsumeAllComposure,            // Remove all Composure stacks

        // Hostility (Self-Inflicted Debuff)
        GainHostility,                  // Gain Hostility (opponent deals more damage)
        ReduceHostility,                // Reduce Hostility stacks
        ComposureEqualToHostility,      // Gain Composure = Hostility (Actor Ego Trip)

        // Resource Types
        GainActionPoints,               // Gain extra action points this turn
        GainActionPointsNextTurn,       // Gain AP next turn (Nepo Baby Backroom Deal)
        DrawCards,                      // Draw cards from deck
        DiscardCards,                   // Discard cards from hand (self or opponent)

        // Status Effects - Debuffs
        ApplyWeakened,                  // Deal X less damage
        ApplyVulnerable,                // Take 50% more damage
        ApplyFrail,                     // Gain 25% less Composure
        ApplyEntangled,                 // Cards cost +1 AP
        ApplyExposed,                   // Next attack deals double damage
        ApplyScandal,                   // Take X damage at end of turn
        ApplyConfused,                  // Random card costs +1 AP each turn
        ApplySilenced,                  // Cannot play Manipulate cards

        // Status Effects - Buffs
        ApplyStrength,                  // Deal X more damage
        ApplyDexterity,                 // Gain X more Composure per card
        ApplyFocus,                     // Cards cost X less AP (this turn only)
        ApplyEnergized,                 // Draw X extra cards next turn
        ApplyPlated,                    // Reduce incoming damage by X
        ApplyRegeneration,              // Heal X Resolve at end of turn
        ApplyIntangible,                // Take only 1 damage from attacks
        ApplyThorns,                    // Deal X damage back when attacked

        // Status Effects - Special
        ApplyBlock,                     // Temporary damage reduction
        ApplyRitual,                    // Gain X Composure at start of turn
        ApplyMomentum,                  // Gain X damage per card played this turn
        ApplyEcho,                      // Next card is played twice

        // Special
        ExhaustCard                     // Remove card from deck until end of battle
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

    /// <summary>
    /// Cost types for playing cards.
    /// NOTE: In battle, cards ONLY cost Action Points (or are free).
    /// Funds/Influence are meta resources used outside battle (shops, events, etc.)
    /// </summary>
    public enum CostType
    {
        None,           // Free to play
        ActionPoints    // Battle resource - energy to play cards (THE ONLY CARD COST IN BATTLE)
    }

    /// <summary>
    /// Battle-specific resources that exist only during card battles.
    /// Griftlands-inspired negotiation resources.
    /// </summary>
    public enum BattleResourceType
    {
        Resolve,        // HP - Both player and opponent have this (reduce to 0 = win/lose)
        Composure,      // Offensive buff - Each stack = +1 damage (consumed on attack)
        Hostility,      // Self-inflicted debuff - Opponent deals more damage based on this
        ActionPoints    // Energy to play cards each turn (3-4 depending on origin)
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
