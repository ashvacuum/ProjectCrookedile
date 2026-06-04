namespace Crookedile.Data
{
    /// <summary>
    /// Card types for political negotiation.
    /// Pressure = persuasion / de-escalation, Rhetoric = aggressive framing,
    /// Policy = policy positions with a left/center/right lean that shifts demographics.
    /// </summary>
    public enum CardType
    {
        Pressure, // Green  - Persuasion, de-escalation, relationship building
        Rhetoric, // Red    - Aggressive framing, attacks, pressure tactics
        Policy, // Blue   - Policy positions; lean shifts all enemy hostility by demographic
        Status, // Purple - Temporary effect cards; some are unplayable
        Curse, // Dark   - Always unplayable; negative cards forced into the deck
    }

    /// <summary>
    /// How a card-manipulation effect picks which card(s) it acts on.
    /// (Ordinals are serialized on effect assets — do not reorder.)
    /// </summary>
    public enum CardSelectionMode
    {
        PlayerChoice, // 0 — prompt the player via CardChoiceRequestedEvent
        RandomAny, // 1 — pick randomly from the whole pool
        RandomByType, // 2 — filter by a CardType, then pick randomly
    }

    /// <summary>
    /// Card rarity determines acquisition chance, power level, and visual frame.
    /// </summary>
    public enum CardRarity
    {
        Basic, // Basic cards from shops and starter decks
        Enhanced, // Enhanced effects, moderate acquisition difficulty
        Rare, // Powerful effects, harder to acquire
    }

    /// <summary>
    /// Player origin types. Each has unique starting deck and passive abilities.
    /// Simplified to 3 distinct archetypes.
    /// </summary>
    public enum OriginType
    {
        FaithLeader, // Religious leader - defensive, Confidence-focused
        NepoBaby, // Nepo baby - resource manipulation, Influence-focused
        Actor, // Celebrity/Actor - charismatic, versatile
    }

    /// <summary>
    /// Campaign/Overworld resources that persist across battles.
    /// Simplified to 3 core resources for cleaner gameplay.
    /// </summary>
    public enum CampaignResourceType
    {
        Funds, // ₱ - Campaign funds for buying cards, upgrades, etc.
        Heat, // H - Scandal meter (0-100), lose if too high
        Influence, // Political influence/power, win condition
    }

    /// <summary>
    /// Battle-specific effect types for in-combat card effects.
    /// Separates battle mechanics from campaign/overworld effects.
    /// </summary>
    public enum BattleEffectType
    {
        // Core Damage/Healing
        ResolveDamage, // Damage opponent's Resolve (HP)
        ResolveHeal, // Restore your Resolve (HP)
        RandomDamage, // Deal random damage (Actor All or Nothing)

        // Shield — Support (player) / Denial (enemy)
        GainShield, // Gain Shield stacks
        LoseShield, // Lose Shield stacks
        RaiseOpinionEqualToShield, // Raise Opinion = your Shield (Faith Leader Blessing)
        ConsumeAllShield, // Remove all Shield stacks

        // Hostility (Self-Inflicted Debuff)
        GainHostility, // Gain Hostility (opponent deals more damage)
        ReduceHostility, // Reduce Hostility stacks
        ShieldEqualToHostility, // Gain Shield = number of Hostile enemies (Actor Ego Trip)

        // Resource Types
        GainActionPoints, // Gain extra action points this turn
        GainActionPointsNextTurn, // Gain AP next turn (Nepo Baby Backroom Deal)
        DrawCards, // Draw cards from deck
        DiscardCards, // Discard cards from hand (self or opponent)

        // Status Effects - Debuffs
        ApplyWeakened, // Deal X less damage
        ApplyVulnerable, // Take 50% more damage
        ApplyFrail, // Gain 25% less Shield
        ApplyEntangled, // Cards cost +1 AP
        ApplyExposed, // Next attack deals double damage
        ApplyScandal, // Take X damage at end of turn
        ApplyConfused, // Random card costs +1 AP each turn
        ApplySilenced, // Cannot play Policy cards

        // Status Effects - Buffs
        ApplyStrength, // Deal X more damage
        ApplyDexterity, // Gain X more Shield per card
        ApplyFocus, // Cards cost X less AP (this turn only)
        ApplyEnergized, // Draw X extra cards next turn
        ApplyPlated, // Reduce incoming damage by X
        ApplyRegeneration, // Heal X Resolve at end of turn
        ApplyIntangible, // Take only 1 damage from attacks
        ApplyThorns, // Deal X damage back when attacked

        // Status Effects - Special
        ApplyBlock, // Temporary damage reduction
        ApplyRitual, // Gain X Shield at start of turn
        ApplyMomentum, // Gain X damage per card played this turn
        ApplyEcho, // Next card is played twice

        // Special
        ExhaustCard, // Remove card from deck until end of battle
    }

    /// <summary>
    /// Campaign/Overworld effect types for post-battle rewards and events.
    /// These affect the player's campaign state, not battle state.
    /// </summary>
    public enum CampaignEffectType
    {
        // Resource Gains/Losses (3 core resources only)
        GainFunds, // Gain ₱
        LoseFunds, // Lose ₱
        GainHeat, // Increase Heat (scandal)
        LoseHeat, // Decrease Heat
        GainInfluence, // Gain Influence (political power)
        LoseInfluence, // Lose Influence

        // Card Collection
        AddCardToDeck, // Permanently add card to deck
        RemoveCardFromDeck, // Permanently remove card from deck
        UpgradeCard, // Upgrade a card to + version
        TransformCard, // Transform one card into another

        // Progression
        UnlockLocation, // Unlock new map location
        UnlockCard, // Unlock card for future acquisition
        TriggerEvent, // Trigger a specific event
        AdvanceDay, // Skip to next day
    }

    public enum TargetType
    {
        Self,
        Opponent, // Single focused enemy (player) or the player (enemy)
        All, // Player + ALL living enemies
        Random, // Random single opponent
        AllOpponents, // Player card → all living enemies | Enemy card → the player
        AllAllies, // Enemy card → all living enemies  | Player card → self
        Adjacent, // Focused enemy + immediate row neighbours (player); the player (enemy)
        AllHostile, // Every living enemy with Hostility > 0 (the dissenters)
        AllReceptive, // Every living enemy with Hostility < 0 (the supporters)
    }

    /// <summary>
    /// Cost types for playing cards.
    /// NOTE: In battle, cards ONLY cost Action Points (or are free).
    /// Funds/Influence are meta resources used outside battle (shops, events, etc.)
    /// </summary>
    public enum CostType
    {
        None, // Free to play
        ActionPoints, // Battle resource - energy to play cards (THE ONLY CARD COST IN BATTLE)
    }

    /// <summary>
    /// Battle-specific resources that exist only during card battles.
    /// Griftlands-inspired negotiation resources.
    /// </summary>
    public enum BattleResourceType
    {
        Resolve, // HP - Both player and opponent have this (reduce to 0 = win/lose)
        Shield, // Support (player) / Denial (enemy) — temporary buffer against opinion changes
        Hostility, // Self-inflicted debuff - Opponent deals more damage based on this
        ActionPoints, // Energy to play cards each turn (3-4 depending on origin)
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
        Victory,
    }

    public enum BattlePhase
    {
        Setup,
        PlayerTurn,
        OpponentTurn,
        Victory,
        Defeat,
    }

    /// <summary>
    /// Political lean of a Policy card.
    /// Determines which demographics become more or less hostile when the card is played.
    /// None = not a Policy card (or Policy card with no lean effect).
    /// </summary>
    public enum PolicyLean
    {
        Left,
        Center,
        Right,
        None, // Default for Pressure/Rhetoric cards — no hostility shift applied
    }

    /// <summary>
    /// Socioeconomic class of an enemy demographic.
    /// Used for card targeting and thematic identification.
    /// </summary>
    public enum DemographicClass
    {
        Upper,
        Middle,
        Lower,
    }

    /// <summary>
    /// Political values of an enemy demographic.
    /// Determines how they react to Policy cards based on lean alignment.
    /// </summary>
    public enum DemographicValues
    {
        Progressive,
        Moderate,
        Traditional,
    }

    /// <summary>
    /// Determines where a BattleEffect reads its numeric amount at runtime.
    /// Used by passive and card effects to mirror values accumulated during resolution
    /// (e.g. heal for the damage you just dealt = LastDamageDealt).
    /// </summary>
    public enum EffectContextValue
    {
        FixedAmount, // 0 — use the inspector-authored value (shows fixed amount fields)
        LastDamageDealt, // 1 — ctx.LastDamageDealt  — e.g. lifesteal
        LastHealAmount, // 2 — ctx.LastHealAmount
        LastSupportGained, // 3 — ctx.LastSupportGained
        LastSupportLost, // 4 — ctx.LastSupportLost — e.g. bonus pressure equal to Support spent
        CurrentSupport, // 5 — session CurrentSupport at time of trigger
        CurrentHostility, // 6 — focused target.CurrentHostility at time of trigger
        None, // 7 — return 0; hides fixed amount fields (use when amount is irrelevant)
        HostileEnemyCount, // 8 — number of living enemies with Hostility > 0
        ReceptiveEnemyCount, // 9 — number of living enemies with Hostility < 0
    }
}
