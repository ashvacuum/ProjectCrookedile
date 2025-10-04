# Card System

[← Home](README.md) | [≡ Full Documentation](README.md#-game-design-documentation)

**Related:** [Game Overview](game_overview.md) | [Origins](origins.md) | [Resources](resources.md) | [Locations](locations.md)

---

Complete card system breakdown for the political deck-building game inspired by Griftlands.

## Table of Contents

- [Card Types](#card-types)
- [Card Rarity](#card-rarity)
- [Card Mechanics](#card-mechanics)
- [Starter Decks](#starter-decks)
- [Card Acquisition](#card-acquisition)
- [Card Upgrade System](#card-upgrade-system)
- [Deck Building Strategy](#deck-building-strategy)

---

## Card Types

**Three card types inspired by Griftlands negotiation system:**

### Diplomacy Cards (Green)
Peaceful persuasion tactics that build relationships and avoid conflict.

**Mechanics:**
- Deal Resolve damage (the "HP" in negotiations)
- Build Composure (offensive buff)
- Low/no Hostility generation
- Focus on sustainable damage and alliances

**Examples:**
- **Find Common Ground** - Basic persuasion (3 Resolve damage)
- **Blessing** - Convert Composure to burst damage (Faith Leader)
- **Family Name** - Leverage family reputation (Nepo Baby)

### Hostility Cards (Red)
Aggressive tactics using threats, pressure, and confrontation.

**Mechanics:**
- Deal higher Resolve damage
- Build Hostility (self-inflicted debuff)
- Risk/reward: stronger effects but opponent hits harder
- Creates enemies when used heavily

**Examples:**
- **Accusation** - Direct attack (4 Resolve damage, gain 1 Hostility)
- **Bold Accusation** - Aggressive pressure (5 damage, gain 2 Hostility)
- **Spotlight Hog** - Vain aggression (6 damage, gain Composure, gain Hostility)

### Manipulate Cards (Purple)
Utility cards for resource advantage, card draw, and tactical plays.

**Mechanics:**
- Card draw/discard
- Action Point manipulation
- Composure building
- Hostility management
- Hand/deck manipulation

**Examples:**
- **Call in Favor** - Draw 2 cards
- **Gather Thoughts** - Gain 4 Composure
- **High Stakes** - Discard hand, draw 3 cards (Actor risk card)

---

## Card Rarity

### Common (White/Gray)
- Basic cards found frequently
- Foundation of most decks
- Lower power but reliable
- Most starter deck cards are Common

### Uncommon (Green)
- Enhanced effects or situational power
- Moderate acquisition difficulty
- Build-around potential

### Rare (Blue)
- Powerful effects with strategic depth
- Harder to acquire
- Often origin-specific

### Legendary (Gold)
- Game-changing abilities
- Very rare acquisitions
- Usually unique or origin-exclusive
- High risk/reward ratio

---

## Card Mechanics

### Battle Resources

**IMPORTANT: In battle, cards interact with three resources:**

#### Resolve (HP)
- Both you and opponent have Resolve (~20 starting)
- Reduce to 0 = Win/Lose
- Primary target of card damage
- Can be healed by certain cards

#### Composure (Offensive Buff)
- Build Composure with Manipulate/Diplomacy cards
- Each stack = +1 damage on next attack
- Consumed when dealing damage
- Faith Leader specialty: Convert all Composure → burst damage

#### Hostility (Self-Inflicted Debuff)
- Gained from playing aggressive red cards
- Higher Hostility = Opponent deals MORE damage to your Resolve
- Formula: Opponent damage × (1 + Hostility × 0.5)
- Actor specialty: Convert Hostility → Composure

### Cost Types

**In battle, cards ONLY cost Action Points (or are free).**

#### Battle Costs (Playing Cards)
- **Free (0 AP)** - No Action Points required
- **Fixed Cost (1-2 AP)** - Costs specific amount

#### Meta Resources (Outside Battle)
- **Funds** - Buy cards at shops, pay for events
- **Heat** - Scandal meter (lose condition)
- **Influence** - Political power (win condition)

### Card Effects

#### Battle Effects (In-Combat)
- **Resolve Damage** - Reduce opponent's Resolve (HP)
- **Composure Gain** - Build offensive buff stacks
- **Hostility Gain** - Self-inflicted risk (opponent deals more damage)
- **Hostility Reduction** - Manage risk
- **Card Draw** - Draw from deck
- **Card Discard** - Hand manipulation
- **Action Point Gain** - Extra energy

#### Campaign Effects (Post-Battle)
- **Gain/Lose Funds** - Economic impact
- **Gain/Lose Heat** - Scandal consequences
- **Gain/Lose Influence** - Political power shifts
- **Create Ally/Enemy** - Relationship changes based on Hostility used
- **Add/Remove Cards** - Deck building
- **Unlock Content** - New locations, cards, events

**Note:** Battle outcome determines relationship changes:
- Low Hostility = Diplomatic victory → Ally
- High Hostility = Aggressive victory → Enemy, better rewards, +Heat

### Special Mechanics

#### Combo Cards
- **Faith Leader's Blessing**: Deal damage = Composure, consume all Composure (build → burst)
- **Actor's Ego Trip**: Gain Composure = Hostility, don't reduce Hostility (convert risk → power)

#### Risk/Reward Cards (Actor specialty)
- **Random effects**: Deal 3-9 damage (All or Nothing)
- **Probability**: 50% chance to draw card (Charming Gambit)
- **High Stakes**: Discard hand, draw 3 cards

---

## Starter Decks

Each origin starts with **10 cards total**, differentiated by card type distribution and one unique passive ability.

### Faith Leader - "The Peacemaker" (10 cards)
- **Passive**: "Divine Grace" - Start each negotiation with +1 card draw (6 cards instead of 5)
- **Deck**: 6 Diplomacy, 2 Hostility, 2 Manipulate
- **Playstyle**: Composure combo specialist (build Composure → burst with Blessing)

**Cards:**
1-4. **Find Common Ground** (1 AP) x4 - Deal 3 Resolve damage
5-6. **Blessing** (1 AP) x2 - Deal damage = Composure, consume all Composure
7-8. **Accusation** (1 AP) x2 - Deal 4 damage, gain 1 Hostility
9. **Deflect** (1 AP) x1 - Gain 3 Composure, reduce Hostility by 1
10. **Gather Thoughts** (1 AP) x1 - Gain 4 Composure

### Nepo Baby - "The Operator" (10 cards)
- **Passive**: "Family Connections" - Start with 4 AP instead of 3
- **Deck**: 3 Diplomacy, 2 Hostility, 5 Manipulate
- **Playstyle**: Card/AP advantage engine

**Cards:**
1-2. **Family Name** (1 AP) x2 - Deal 3 Resolve damage
3. **Inherited Privilege** (2 AP) x1 - Deal 5 damage, draw 1 card
4-5. **Pull Strings** (1 AP) x2 - Deal 4 damage, gain 1 Hostility
6-7. **Call in Favor** (1 AP) x2 - Draw 2 cards
8. **Backroom Deal** (2 AP) x1 - Draw 2 cards, gain 1 AP next turn
9. **Dynasty Network** (1 AP) x1 - Discard 1, draw 2
10. **Trust Fund** (0 AP) x1 - Gain 2 Composure, gain 1 AP this turn

### Actor - "The Risk Taker" (10 cards)
- **Passive**: "Stage Presence" - First card each turn costs 1 less AP
- **Deck**: 3 Diplomacy, 4 Hostility, 3 Manipulate
- **Playstyle**: Risk/reward specialist, Hostility → Composure conversion

**Cards:**
1-2. **Charming Gambit** (1 AP) x2 - Deal 3 damage, 50% chance: draw 1 card
3. **All or Nothing** (2 AP) x1 - Deal 3-9 damage (random)
4-5. **Bold Accusation** (1 AP) x2 - Deal 5 damage, gain 2 Hostility
6-7. **Spotlight Hog** (2 AP) x2 - Deal 6 damage, gain 3 Composure, gain 2 Hostility
8. **High Stakes** (0 AP) x1 - Discard hand, draw 3 cards
9. **Ego Trip** (1 AP) x1 - Gain Composure = Hostility, don't reduce Hostility
10. **Fan Favorite** (1 AP) x1 - Lose 3 Composure, reduce Hostility by 3

---

## Card Acquisition

### During Runs (Griftlands-inspired)
- **Battle Rewards** - Win negotiations to choose from 3 random cards
- **Ally Cards** - Allies give their signature card to your deck
- **Shop Purchases** - Buy cards with Funds at locations
- **Event Rewards** - Special cards from events
- **Quest Completion** - Unique cards from NPC questlines

### Card Removal
- **Rest Sites** - Remove cards from deck (limit 1 per rest)
- **Special Events** - Remove unwanted cards for cost

---

## Card Upgrade System

### Standard Upgrades
- **+ Version** - Enhanced effect (e.g., Find Common Ground → Find Common Ground+)
- **Cost Reduction** - Lower AP requirements
- **Increased Effect** - More damage, more Composure, etc.
- **Added Effect** - Gain additional benefits

**Upgrade Examples:**
- Find Common Ground: 3 damage → 5 damage
- Blessing: Deal Composure damage → Deal Composure damage + don't consume
- Accusation: 4 damage, 1 Hostility → 6 damage, 1 Hostility

### Upgrade Opportunities
- **Rest Sites** - Upgrade 1 card per rest (alternative to removal)
- **Special Events** - Upgrade opportunities through story
- **Shops** - Pay Funds to upgrade cards

---

## Deck Building Strategy

### Archetypes

**Diplomatic Builder (Faith Leader)**
- Focus on Diplomacy cards (green)
- Build Composure → Blessing for burst
- Low Hostility = create allies
- Slower but safer victories
- **Best for:** Faith Leader

**Resource Engine (Nepo Baby)**
- Focus on Manipulate cards (purple)
- Card advantage and AP manipulation
- Flexible responses to any situation
- Extra AP enables big turns
- **Best for:** Nepo Baby

**Aggressive Gambler (Actor)**
- Focus on Hostility cards (red)
- High risk, high reward
- Convert Hostility → Composure
- Manage risk with Fan Favorite
- **Best for:** Actor

**Hybrid (Balanced)**
- Mix of all card types
- Adaptable to situations
- Moderate risk
- **Best for:** Any origin

### Deck Size Management
- **Starting:** 10 cards
- **Minimum:** 10 cards (can't go below)
- **Maximum:** 30 cards
- **Optimal:** 12-18 cards (consistency vs. variety)

### Ally/Enemy Network Strategy

**Creating Allies (Low Hostility):**
- Play mostly Diplomacy/Manipulate cards
- Avoid Hostility cards when possible
- Ally gives signature card
- Passive bonuses in future battles
- Lower immediate rewards

**Creating Enemies (High Hostility):**
- Play Hostility cards aggressively
- Higher Resolve damage, faster wins
- Enemy opposes you in future battles
- Better rewards (Funds, Influence)
- Gain Heat (scandal)

**Strategic Consideration:**
- Early game: Create allies (build deck, get bonuses)
- Late game: Can afford enemies (strong deck, need rewards)
- Actor can handle high Hostility (convert to Composure)

---

## Card Statistics

*See [Technical Notes](technical.md#card-data) for data structure and implementation details.*

*See [Battle System](BATTLE_SYSTEM.md) for complete battle mechanics and flow.*

---

**Navigation:** [← Home](README.md) | [Game Overview](game_overview.md) | [Origins](origins.md) | [Resources](resources.md)
