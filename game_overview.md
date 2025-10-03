# Game Overview

[🏠 Home](README.md) | [📖 Full Documentation](README.md#-game-design-documentation)

**Related:** [Origins](origins.md) | [Resources](resources.md) | [Cards](cards.md) | [Locations](locations.md) | [Events](events.md)

---

## Table of Contents

- [Core Game Loop](#core-game-loop)
- [Victory Conditions](#victory-conditions)
- [Loss Conditions](#loss-conditions)
- [Card Battle System](#card-battle-system)
- [Special Events](#special-events)
- [Regional Factions](#regional-factions)
- [Palakasan System](#palakasan-system-connections)
- [Difficulty Scaling](#difficulty-scaling)

---

## Core Game Loop

### Time System
- **45 Days until Election Day**
- Each location visit = 1 day
- Days are structured in three phases

### Daily Structure

#### Morning (Pick One)
- **Visit Location** → Trigger battle/event/shop
- **Rest** → Reduce Heat by 10, shuffle deck, restore confidence

#### Afternoon (Pick One)
- **Second Location Visit** → Another battle/event/shop
- **Card Workshop** → Upgrade or combine cards
- **Fundraiser** → Convert Support to Campaign Funds

#### Evening (Automatic)
- Random event roll
- News cycle (see rankings, Heat level)
- Opponent actions (AI rivals also playing)

---

## Victory Conditions

### 1. Squeaky Clean Champion
- **Requirements:** < 20 Heat, >12,000 Support
- **Unlocks:** "Reformist" party for future runs
- **Ending:** Best possible outcome

### 2. Necessary Evil Victor
- **Requirements:** 20-60 Heat, >10,000 Support
- **Unlocks:** Standard progression
- **Ending:** Won but compromised

### 3. Trapo Triumphant
- **Requirements:** 60-99 Heat, >10,000 Support
- **Unlocks:** "Shameless" perk
- **Ending:** Corrupt but victorious

### 4. Pyrrhic Victory
- **Requirements:** Survived scandal, won with <0 Utang na Loob
- **Unlocks:** Special cards
- **Ending:** Won but destroyed all relationships

---

## Loss Conditions

1. **Heat Reaches 100** → Scandal event (may be survivable with resources)
2. **Support < 5,000 by Day 45** → Eliminated in primary
3. **Assassinated** → Failed to defend in special encounter
4. **COMELEC Disqualification** → Specific event chain failure

---

## Card Battle System

### Battle Mechanics
- **Confidence System:** Attack opponent's political confidence (not HP)
- **Turn-based:** Play cards from hand each turn
- **Deck Building:** Acquire cards throughout the run
- **Victory:** Reduce opponent confidence to 0

### Opponent Types

#### Rival Politicians
- Full card battles with complete decks
- Represent different political factions
- Win = Gain their support base

#### Journalists
- "Investigation battles" using exposé mechanics
- Try to uncover your scandals
- Win = Prevent story publication

#### Community Leaders
- Persuasion battles for endorsements
- Often use Charm-based strategies
- Win = Gain Utang na Loob and Support

#### Fixers/Operators
- Negotiation battles for resources
- Use Leverage cards heavily
- Win = Gain access to special services

---

## Special Events

### Milestone Events

#### Day 7, 14, 21, 28, 35, 42: TV Debates
- Major card battles against rivals
- High stakes (large Support swings)
- Broadcast to all regions

#### Day 30: COMELEC Filing Deadline
- Must have at least 5,000 Support
- Failure = Immediate disqualification
- Can pay ₱5,000 to bypass if close

#### Day 45: Election Day
- Final tally of all Support
- Victory speeches based on ending type

---

## Regional Factions

Different areas respond differently to your origin and tactics:

### Tagalog Urban Elite
- **Preferences:** Education, progressive policies
- **Dislikes:** Strongman tactics, corruption
- **Key Locations:** Malls, universities, offices

### Visayan Business Class
- **Preferences:** Pragmatism, economic growth
- **Dislikes:** Excessive spending, instability
- **Key Locations:** Markets, ports, trade centers

### Mindanao Regional Bloc
- **Preferences:** Autonomy, regional respect
- **Dislikes:** Imperial Manila attitudes
- **Key Locations:** Farms, community halls

### Rural Agricultural Communities
- **Preferences:** Tradition, patron-client relationships
- **Dislikes:** Elitism, broken promises
- **Key Locations:** Haciendas, barangay halls, churches

---

## Palakasan System (Connections)

Every NPC exists in a web of relationships:

### Relationship Types
- **Family/Clan** - Blood ties (strongest)
- **Compadre** - Godparent relationships
- **Fraternity** - Brotherhood/sorority ties
- **Business** - Economic partnerships
- **Political** - Party affiliations

### Consequences
- Helping one NPC may anger their enemies
- Betraying one affects their entire network
- Building Utang na Loob with key figures unlocks groups

### Tracking
- Visual relationship web in UI
- Color coding: Green (ally), Yellow (neutral), Red (enemy)
- Shows cascading effects of choices

---

## Difficulty Scaling

### Origin Difficulty (Easiest → Hardest)
1. **Nepo Baby** - Money solves most problems
2. **Celebrity** - Popularity protects you
3. **Religious Leader** - Limited options, loyal base
4. **Strongman** - Constant Heat and journalist aggro

### Additional Modifiers (Post-Game Unlocks)
- **Honest Run** - Cannot use Lagay at all
- **Speed Run** - Win in 30 days instead of 45
- **Maximum Heat** - Start at 50 Heat
- **No Kickbacks** - Refuse all corrupt projects

---

## Win/Loss Statistics

Track across all runs:
- Total runs attempted
- Best Support score
- Lowest Heat victory
- Most Utang na Loob accumulated
- Fastest victory
- Each ending achieved
- Cards unlocked
- Locations discovered