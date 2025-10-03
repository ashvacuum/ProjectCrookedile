# Roguelike & Meta Progression

[🏠 Home](README.md) | [📖 Full Documentation](README.md#-game-design-documentation)

**Related:** [Game Overview](game_overview.md) | [Origins](origins.md) | [Cards](cards.md) | [Events](events.md)

---

Systems for replayability, unlocks, and long-term progression across multiple runs.

## Table of Contents

- [Run Structure](#run-structure)
- [Political Capital (Meta Currency)](#political-capital-meta-currency)
- [Difficulty Modifiers](#difficulty-modifiers)
- [Achievement System](#achievement-system)
- [Statistics Tracking](#statistics-tracking)
- [New Game+ Modes](#new-game-modes)
- [Daily/Weekly Challenges](#dailyweekly-challenges)
- [Prestige System](#prestige-system)
- [Content Roadmap](#content-roadmap-future-updates)

---

## Run Structure

### Single Run
- **Duration**: 45 days (in-game time)
- **Goal**: Reach 10,000 Support by Day 45 without exceeding 100 Heat
- **Save System**: One save per run, deleted on completion
- **Permadeath**: Failed runs are over (but reward Political Capital)

### Run Seeding
- **Procedural Generation**: Each run randomizes:
  - Map layout (location positions)
  - NPC placements and relationships
  - Event triggers and timing
  - Opponent strategies and decks
  - Project opportunities
  - Shop inventory

- **Fixed Elements**:
  - Origin stats and starting decks
  - Core location types
  - Card effects and mechanics
  - Major milestone events (Days 7, 14, 21, 28, 35, 42, 45)

---

## Political Capital (Meta Currency)

### Earning Political Capital (PC)

**From Completed Runs:**
- **Victory**: 100-500 PC (based on ending type)
  - Squeaky Clean Champion: 500 PC
  - Necessary Evil Victor: 300 PC
  - Trapo Triumphant: 200 PC
  - Pyrrhic Victory: 150 PC

- **Defeat**: 50-150 PC (based on how far you got)
  - Reached Day 45: 150 PC
  - Reached Day 30: 100 PC
  - Reached Day 15: 75 PC
  - Before Day 15: 50 PC

**From Achievements:**
- **First Victory**: +100 PC
- **Victory with Each Origin**: +50 PC each
- **All Endings Unlocked**: +200 PC
- **Speed Run (30 days)**: +150 PC
- **Clean Run (Heat <30 entire run)**: +100 PC
- **Corrupt Run (Used >100L total)**: +75 PC
- **Beloved (60+ Utang na Loob)**: +100 PC
- **Feared (150+ Fear, Strongman)**: +100 PC
- **Influential (200+ Clout, Celebrity)**: +100 PC
- **Faithful

### Earning Political Capital (PC) (continued)

**From Achievements (continued):**
- **Faithful (150+ Faith, Religious Leader)**: +100 PC
- **Dynasty Secured (200+ Influence, Nepo Baby)**: +100 PC
- **Perfect Debate Record**: +75 PC
- **Never Used Lagay**: +125 PC
- **Survived Scandal**: +50 PC
- **All Locations Discovered**: +100 PC
- **All Cards Collected**: +300 PC
- **Complete All NPC Questlines**: +200 PC

**Total PC Available**: ~3,000+ PC across all achievements

---

### Spending Political Capital

#### Origin Unlocks

**Reformed Strongman** (150 PC)
- **Variant**: Strongman who reformed
- **Starting Heat**: 15 (instead of 30)
- **Perk**: Can use Charm cards effectively
- **Drawback**: Fear generation -50%
- **Unlock Condition**: Win as Strongman with <30 Heat

**Method Actor** (150 PC)
- **Variant**: Celebrity focused on substance
- **Starting Clout**: 0 (instead of bonus)
- **Perk**: Attack cards +20% effective
- **Drawback**: Star Power reduced
- **Unlock Condition**: Win as Celebrity without using Clout for scandals

**Fallen Prophet** (150 PC)
- **Variant**: Religious Leader who survived scandal
- **Starting Faith**: 50 (instead of 100)
- **Perk**: Can use Lagay (only 5H penalty instead of 80H)
- **Drawback**: Charm cards -20% effective
- **Unlock Condition**: Win as Religious Leader after surviving scandal

**Self-Made Heir** (150 PC)
- **Variant**: Nepo Baby proving themselves
- **Starting U**: +5 (instead of -5)
- **Perk**: Can earn Utang na Loob normally (no cap)
- **Drawback**: Dynasty perks reduced by 50%
- **Unlock Condition**: Win as Nepo Baby with net positive Utang na Loob

**The Technocrat** (300 PC)
- **New Origin**: Data-driven policy wonk
- **Focus**: Analytics and efficiency
- **Unique Mechanic**: "Data Points" - collect information to optimize strategies
- **Unlock Condition**: Complete 10 runs total

**The Activist** (300 PC)
- **New Origin**: Grassroots organizer
- **Focus**: Community building and protests
- **Unique Mechanic**: "Movement" - build coalition of supporters
- **Unlock Condition**: Win with 80+ Utang na Loob

**The Warlord** (500 PC)
- **New Origin**: Regional strongman with private army
- **Focus**: Territory control and intimidation
- **Unique Mechanic**: "Territory" - control map regions for bonuses
- **Unlock Condition**: Win as Strongman with 200+ Fear

---

#### Legacy Perks (Permanent Bonuses)

**Starting Bonuses:**

**"Campaign Veteran"** (50 PC)
- All origins start with +₱500
- Repeatable (up to 3 times, +₱500 each)

**"Connected"** (75 PC)
- All origins start with +3U
- Repeatable (up to 2 times, +3U each)

**"Media Savvy"** (100 PC)
- All origins start with -5H
- One-time purchase

**"Lucky Break"** (100 PC)
- Start each run with 1 random Rare card
- One-time purchase

**Resource Generation:**

**"Fundraising Network"** (150 PC)
- Passive income: +₱50 per day
- One-time purchase

**"Loyal Base"** (150 PC)
- Passive income: +50 Support per day
- One-time purchase

**"Information Broker"** (100 PC)
- Reveal opponent's next 3 moves at start
- One-time purchase

**Combat Bonuses:**

**"Debate Training"** (100 PC)
- All Charm cards +10% effective
- One-time purchase

**"Thick Skin"** (100 PC)
- All Defense cards +10% effective
- One-time purchase

**"Silver Tongue"** (100 PC)
- Win any Charm check once per run (auto-success token)
- One-time purchase

**"Strategic Mind"** (150 PC)
- Draw +1 card per turn in battles
- One-time purchase

**Utility Perks:**

**"Heat Resistant"** (200 PC)
- Heat accumulation -20% globally
- One-time purchase

**"Quick Learner"** (100 PC)
- Card upgrades cost -30%
- One-time purchase

**"Master Networker"** (150 PC)
- Utang na Loob gains +50%
- One-time purchase

**"Crisis Manager"** (250 PC)
- Survive one scandal automatically per run
- One-time purchase

---

#### Card Unlocks

**Legacy Cards** (Available in all future runs after purchase)

**Common Legacy Cards** (25 PC each):
- "Political Instinct" (Leverage) - See next event type
- "Grassroots Support" (Charm) - Gain Support equal to Utang na Loob × 50
- "Crisis Response" (Defense) - Block all damage, gain +5U

**Uncommon Legacy Cards** (50 PC each):
- "Dark Horse Momentum" (Power) - Each turn, gain +100 Support
- "Coalition Builder" (Charm) - Make all NPCs in location friendly
- "Emergency Funds" (Leverage) - Gain ₱1000 (once per run)

**Rare Legacy Cards** (100 PC each):
- "People Power" (Power) - Convert all Utang na Loob to Support (1:200 ratio)
- "The Fixer" (Leverage) - Remove any negative event
- "Kingmaker" (Charm) - Auto-win current battle (once per run)

**Legendary Legacy Cards** (200 PC each):
- "Second Chance" (Power) - If you would lose, reset to Day 1 with all current resources (once per run)
- "Revolution" (Power) - Gain Support equal to current Heat × 100, reset Heat to 0
- "Mandate of Heaven" (Power) - Win election regardless of Support (requires 0H, once ever)

---

#### Shortcut Unlocks

**Map Shortcuts** (50 PC each):
- Unlock permanent fast travel between specific location pairs
- Reduces travel time penalty
- Up to 5 shortcuts can be purchased

**Starting Location Choice** (100 PC):
- Choose your starting location each run
- Default: Random Community location
- One-time purchase

**Reroll Shop** (75 PC):
- Reroll shop inventory once per day (free action)
- One-time purchase

**Event Reroll** (100 PC):
- Reroll one random event per run
- One-time purchase

---

#### Story/Lore Unlocks

**NPC Backstories** (25 PC each):
- Unlock detailed backstory for each major NPC
- 20+ NPCs to unlock
- Reveals optimal strategies for winning them over

**Rival Dossiers** (50 PC each):
- Unlock opponent AI strategies and weaknesses
- 10+ rivals to unlock
- Shows their deck composition and tactics

**World Codex** (Free, unlocks through play):
- Lore about Philippine political culture
- Satirical commentary
- Historical references (fictionalized)

**Ending Gallery** (Free):
- View all endings you've achieved
- Replay ending cinematics
- Track completion percentage

---

## Difficulty Modifiers

### Unlocked After First Victory

**Easy Modifiers** (Reduce Political Capital earned by 50%):
- **"Safety Net"**: Heat doesn't trigger scandal until 120
- **"Popular Mandate"**: Start with 2000 Support
- **"Rich Backer"**: Start with +₱3000
- **"Media Darling"**: Heat accumulation -50%

**Normal** (Standard Political Capital):
- Default difficulty
- No modifiers

**Hard Modifiers** (Increase Political Capital earned by 50%):
- **"Hostile Media"**: Heat accumulation +50%
- **"Strong Opposition"**: Opponents are more aggressive
- **"Economic Crisis"**: All prices +50%
- **"Short Campaign"**: Only 35 days instead of 45

**Extreme Modifiers** (Double Political Capital):
- **"Perfect Run"**: Cannot use Lagay at all
- **"Speed Demon"**: Must win in 25 days
- **"Maximum Heat Start"**: Begin at 60H
- **"No Kickbacks"**: All project kickbacks forbidden

**Custom Difficulty**:
- Mix and match modifiers
- PC multiplier calculated based on difficulty
- Achievements disabled if too easy

---

## Achievement System

### Campaign Achievements

**Victory Achievements:**
- **"First Blood"**: Win first election (100 PC)
- **"Strongman Victory"**: Win as Strongman (50 PC)
- **"Celebrity Victory"**: Win as Celebrity (50 PC)
- **"Religious Victory"**: Win as Religious Leader (50 PC)
- **"Dynasty Victory"**: Win as Nepo Baby (50 PC)
- **"All Origins Victorious"**: Win with all 4 origins (200 PC)

**Ending Achievements:**
- **"Squeaky Clean"**: Achieve Clean Champion ending (100 PC)
- **"Necessary Evil"**: Achieve Necessary Evil ending (50 PC)
- **"Shameless"**: Achieve Trapo Triumphant ending (75 PC)
- **"Alone at the Top"**: Achieve Pyrrhic Victory ending (75 PC)
- **"All Endings"**: Unlock all victory types (200 PC)

**Speed Achievements:**
- **"Speedrunner"**: Win in 30 days or less (150 PC)
- **"Lightning Campaign"**: Win in 25 days or less (250 PC)
- **"Blitzkrieg"**: Win in 20 days or less (400 PC)

**Challenge Achievements:**
- **"Paragon"**: Win with Heat <20 entire run (100 PC)
- **"Incorruptible"**: Win without using Lagay (125 PC)
- **"Beloved Leader"**: Win with 80+ Utang na Loob (100 PC)
- **"Iron Will"**: Win despite scandal (50 PC)
- **"Comeback Kid"**: Win after being below 3000 Support on Day 35 (75 PC)

---

### Collection Achievements

**Card Achievements:**
- **"Card Collector"**: Collect 50 unique cards (50 PC)
- **"Card Master"**: Collect 100 unique cards (100 PC)
- **"Complete Collection"**: Collect all cards (300 PC)
- **"Deck Builder"**: Win with 10 different deck archetypes (150 PC)

**Location Achievements:**
- **"Explorer"**: Visit all locations in one run (75 PC)
- **"Territory Control"**: Own 10 locations simultaneously (100 PC)
- **"Questmaster"**: Complete all location questlines (200 PC)

**NPC Achievements:**
- **"Networker"**: Achieve +10U with 15 different NPCs (100 PC)
- **"Friend to All"**: Achieve +20U with 10 NPCs in one run (150 PC)
- **"Palakasan Master"**: Unlock all relationship webs (100 PC)

---

### Combat Achievements

**Battle Achievements:**
- **"Flawless Victory"**: Win battle without taking damage (25 PC)
- **"Debate Champion"**: Win all 6 TV debates in one run (75 PC)
- **"Giant Slayer"**: Defeat opponent 20+ levels higher (50 PC)
- **"Pacifist"**: Win without using Attack cards (100 PC)
- **"Aggressive"**: Win using only Attack cards (75 PC)

**Combo Achievements:**
- **"Synergy"**: Execute 10-card combo (50 PC)
- **"Combo Master"**: Execute 20-card combo (100 PC)
- **"Perfect Chain"**: Win battle with unbroken card synergy chain (75 PC)

---

### Resource Achievements

**Wealth Achievements:**
- **"Millionaire"**: Accumulate ₱10,000 (50 PC)
- **"Tycoon"**: Accumulate ₱25,000 (100 PC)
- **"War Chest"**: End run with ₱15,000 (75 PC)

**Corruption Achievements:**
- **"Dirty Hands"**: Use 50L in one run (50 PC)
- **"Trapo Master"**: Use 100L in one run (100 PC)
- **"Kingpin"**: Accumulate 200L total across all runs (150 PC)

**Support Achievements:**
- **"Popular"**: Reach 15,000 Support (50 PC)
- **"Landslide"**: Reach 20,000 Support (100 PC)
- **"Unanimous"**: Reach 30,000 Support (200 PC)

**Heat Achievements:**
- **"Playing with Fire"**: Operate at 80-99H for 10 days (75 PC)
- **"Phoenix"**: Survive scandal and still win (100 PC)
- **"Scandal Magnet"**: Survive 3 scandals in one run (150 PC)

---

### Origin-Specific Achievements

**Strongman:**
- **"Maximum Fear"**: Reach 200 Fear (100 PC)
- **"Iron Fist"**: Win without using Charm cards (75 PC)
- **"Military Coup"**: Control both Police Station and Veterans' Hall (50 PC)

**Celebrity:**
- **"Viral Sensation"**: Reach 300 Clout (100 PC)
- **"Media Empire"**: Own TV Studio, Radio Station, and Mall (75 PC)
- **"Comeback Story"**: Use "Comeback Movie" successfully (50 PC)

**Religious Leader:**
- **"Saint"**: Reach 150 Faith (100 PC)
- **"Miracle Worker"**: Complete Prayer Mountain retreat 3 times (75 PC)
- **"Megachurch Mogul"**: Own 5+ churches (50 PC)

**Nepo Baby:**
- **"Dynasty Restored"**: Reach 200 Influence (100 PC)
- **"Rags to Riches"**: Start at -5U, end at +50U (150 PC)
- **"Family Business"**: Use all 3 dynasty favors in one run (50 PC)

---

### Hidden Achievements

**Secret Achievements** (Not shown until unlocked):
- **"The Truth"**: Discover secret ending (300 PC)
- **"Breaking the Fourth Wall"**: Find easter egg (50 PC)
- **"Perfect Run"**: Win with 0H, 10000+ Support, 80+U (500 PC)
- **"Nightmare Mode"**: Win on Extreme with all hard modifiers (1000 PC)
- **"Legacy Builder"**: Play 100 runs (250 PC)

---

## Statistics Tracking

### Per-Run Stats
- Total Support earned
- Highest Heat reached
- Total ₱ earned/spent
- Total Lagay earned/spent
- Utang na Loob peak/trough
- Cards played
- Battles won/lost
- Locations visited
- NPCs befriended/betrayed
- Days survived

### Career Stats (All Runs)
- Total runs attempted
- Total victories
- Win rate by origin
- Fastest victory time
- Total PC earned
- Total cards unlocked
- Total achievements
- Favorite deck archetype (tracked by AI)
- Most used card
- Most visited location

### Leaderboards (Optional Online Feature)
- Fastest victory times
- Highest Support totals
- Cleanest victories (lowest Heat)
- Most corrupt victories (highest Lagay)
- Highest difficulty victories

---

## New Game+ Modes

### Unlocked After First Victory

**NG+ Mode**:
- Carry over legacy perks
- Opponents are stronger
- New exclusive cards available
- Harder event checks
- +50% PC rewards

**NG++ Mode**:
- Requires 5 victories
- Elite opponents with legendary decks
- New origin-specific questlines
- Hardest difficulty
- +100% PC rewards

**Endless Mode**:
- Campaign continues past Day 45
- Hold office and defend against challenges
- Survival mode (how long can you last?)
- Scandals don't end game, but penalties increase
- Special rewards for every 10 days survived

---

## Daily/Weekly Challenges

### Daily Challenge
- **Pre-set Run**: Fixed seed, origin, and modifiers
- **Leaderboard**: Compete for highest score
- **Scoring**: Based on Support, low Heat, high U, efficiency
- **Reward**: 50 PC (participation), 100 PC (top 10%)

### Weekly Challenge
- **Themed Runs**: Special modifiers or restrictions
- **Examples**:
  - "No Violence Week": Cannot use Attack cards
  - "Charm Offensive": Only Charm cards allowed
  - "Corruption Fest": Must use 50L minimum
  - "Clean Sweep": Heat must stay <15
- **Reward**: 200 PC (completion), special card unlock

---

## Prestige System

### After 100% Completion

**Prestige Reset**:
- Reset all unlocks (except achievements)
- Gain Prestige Level
- Unlock cosmetic rewards:
  - Card back designs
  - Location themes
  - Character portraits
  - UI skins

**Prestige Bonuses**:
- **Level 1**: +10% PC gains
- **Level 2**: +20% PC gains
- **Level 3**: +30% PC gains
- **Max Prestige (10)**: +100% PC gains, golden title

---

## Content Roadmap (Future Updates)

### Planned Expansions

**Regional Expansion: Visayas**
- New map region
- New locations (Sinulog festival, Beach resorts)
- New NPCs and factions
- New cards themed around Visayan culture

**Regional Expansion: Mindanao**
- New map region
- New locations (Mosques, Peace zones)
- Unique political dynamics
- New origin: "The Datu" (regional leader)

**Presidential Campaign DLC**
- Longer campaign (90 days)
- National scale (multiple provinces)
- International relations layer
- New victory conditions

**Multiplayer Mode**
- Versus battles (2-4 players)
- Shared map competition
- Simultaneous campaigns
- Direct sabotage mechanics

---

## Meta Progression Philosophy

### Design Goals
1. **Respect Player Time**: Every run should feel rewarding
2. **Meaningful Unlocks**: Each purchase makes future runs different
3. **Skill Expression**: Meta progression helps, but skill matters most
4. **Replayability**: Hundreds of runs worth of content
5. **Fair Monetization**: All content earnable through play (no pay-to-win)

### Balance Principles
- Legacy perks should be helpful, not overpowered (max ~30% advantage)
- Difficulty modifiers should challenge, not frustrate
- PC earning should be fair (average 200-300 PC per hour)
- Unlocks should open strategies, not trivialize game

---

*Meta progression transforms each run into progress toward long-term mastery.*