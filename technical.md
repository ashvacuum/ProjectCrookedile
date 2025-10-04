# Technical Implementation Notes

[🏠 Home](README.md) | [📖 Full Documentation](README.md#-game-design-documentation)

**Related:** [Game Overview](game_overview.md) | [All Design Docs](README.md#-game-design-documentation)

---

Development considerations, systems architecture, and implementation guidelines.

## Table of Contents

- [Tech Stack Recommendations](#tech-stack-recommendations)
- [System Architecture](#system-architecture)
- [Data Structures](#data-structures)
- [UI/UX Design Flow](#uiux-design-flow)
- [Animation & VFX](#animation--vfx)
- [Audio Design](#audio-design)
- [Localization](#localization)
- [Performance Optimization](#performance-optimization)
- [Testing Strategy](#testing-strategy)
- [Development Phases](#development-phases)
- [Monetization Strategy](#monetization-strategy)
- [Platform Strategy](#platform-strategy)
- [Accessibility Features](#accessibility-features)
- [Quality Assurance Checklist](#quality-assurance-checklist)

---

## Tech Stack Recommendations

### Engine Options

**Option 1: Unity (Recommended)**
- **Pros**: Excellent 2D tooling, large asset store, proven for card games
- **Cons**: Larger build size
- **Best for**: Full commercial release

**Option 2: Godot**
- **Pros**: Open source, lightweight, good 2D support
- **Cons**: Smaller community, fewer ready-made assets
- **Best for**: Indie development, smaller scope

**Option 3: Web-Based (Phaser + React)**
- **Pros**: Cross-platform automatically, easy distribution
- **Cons**: Performance limitations, offline play harder
- **Best for**: Browser-first release, rapid prototyping

### Programming Language
- **Unity**: C#
- **Godot**: GDScript or C#
- **Web**: TypeScript/JavaScript

---

## System Architecture

### Core Systems

#### 1. Game State Manager

```
GameState
├── CurrentDay (1-45)
├── Resources (₱, L, U, H, Support)
├── PlayerDeck (Card[])
├── PlayerOrigin (Strongman/Celebrity/ReligiousLeader/NepoB aby)
├── OwnedLocations (Location[])
├── NPCRelationships (Dictionary<NPC, int>)
├── ActiveEvents (Event[])
├── OpponentStates (Opponent[])
└── UnlockedContent (Achievement[], Card[], Location[])
```

#### 2. Card Battle System

**IMPLEMENTED** - Uses state machine pattern with EventBus communication

```
BattleManager (State Machine Orchestrator)
├── BattleStats (Player)
│   ├── Ego (true HP, 0 = defeat)
│   ├── Confidence (shield, must break before Ego damage)
│   ├── Action Points (energy to play cards, refreshes each turn)
│   └── Block (temporary damage reduction, clears each turn)
├── BattleStats (Opponent)
├── DeckManager (Player)
│   ├── Deck (draw pile)
│   ├── Hand (cards available to play)
│   ├── Discard (played/discarded cards)
│   └── Exhaust (removed from battle)
├── DeckManager (Opponent)
├── EffectResolver (resolves card effects)
└── State Machine
    ├── Initialize → Draw initial hands
    ├── TurnStart → Draw cards, refresh AP
    ├── PlayerTurn → Player plays cards (waits for EndTurnRequestedEvent)
    ├── OpponentTurn → AI plays cards
    ├── TurnEnd → Clear block, check victory
    └── BattleEnd → Calculate rewards
```

**Battle Resources** (per-battle, managed by BattleStats):
- **Ego** - True HP (varies by origin: 80-120)
- **Confidence** - Shield (varies by origin: 80-120)
- **Action Points** - Card cost (varies by origin: 3-4)
- **Block** - Temporary damage reduction

**Origin Battle Stats**:
- **Faith Leader**: 80 Ego, 120 Confidence, 3 AP (defensive tank)
- **Nepo Baby**: 100 Ego, 100 Confidence, 4 AP (flexible, extra AP)
- **Actor**: 120 Ego, 80 Confidence, 3 AP (glass cannon, fragile)

**Card Costs**:
- Cards ONLY cost Action Points in battle
- Free (0 AP), Fixed (1-3 AP), or X cost (all remaining AP)
- Dynamic costs: Can change per turn in hand (+1/-1 per turn)
- Runtime modifiers: Buffs/debuffs can affect costs

**EventBus Communication**:
- `BattleStartedEvent`, `BattleEndedEvent`
- `TurnStartedEvent`, `TurnEndedEvent`
- `CardPlayedEvent`, `CardDrawnEvent`, `CardDiscardedEvent`
- `EffectAppliedEvent`, `DamageDealtEvent`, `HealingAppliedEvent`
- `EndTurnRequestedEvent`, `PlayCardRequestedEvent` (player input)

#### 3. Map/Location System

```
MapManager
├── CurrentLocation (Location)
├── AvailableLocations (Location[])
├── LocationStates (Dictionary<Location, LocationState>)
├── TravelPaths (Graph structure)
├── FastTravelUnlocks (Location[])
└── RegionalModifiers (Region bonuses/penalties)
```

#### 4. Event System

```
EventManager
├── EventQueue (Event[])
├── EventHistory (CompletedEvent[])
├── TriggerConditions (check Heat, Day, U, etc.)
├── RandomEventPool (available events)
└── EventResolver (handle player choices)
```

#### 5. Resource Manager

```
ResourceManager
├── CampaignFunds (₱)
├── Lagay (L)
├── UtangNaLoob (U)
├── Heat (H)
├── Support (int)
├── OriginCurrency (Fear/Clout/Faith/Influence)
├── ResourceHistory (transaction log)
└── ResourceChangeNotifier (UI updates)
```

#### 6. NPC & Relationship System

```
RelationshipManager
├── NPCDatabase (NPC[])
├── RelationshipWeb (Graph<NPC, Relationship>)
├── UtangNaLoobLedger (Dictionary<NPC, int>)
├── QuestStates (Dictionary<NPC, QuestProgress>)
└── ReputationCalculator (location reputation)
```

#### 7. Opponent AI

```
OpponentAI
├── OpponentDeck (Card[])
├── Strategy (Aggressive/Defensive/Balanced)
├── LocationTargets (where they'll go)
├── DecisionMaker (choose cards, locations)
└── DifficultyScaling (gets harder over time)
```

#### 8. Meta Progression

```
ProgressionManager
├── PoliticalCapital (int)
├── UnlockedOrigins (Origin[])
├── LegacyPerks (Perk[])
├── UnlockedCards (Card[])
├── Achievements (Achievement[])
├── Statistics (career stats)
└── SaveData (persistent across runs)
```

---

## Data Structures

### Card Data

**IMPORTANT: Cards only cost Action Points in battle. Funds/Influence are meta resources used outside of battle (shops, events).**

```json
{
  "id": "intimidate_001",
  "name": "Intimidate",
  "type": "Attack",
  "rarity": "Common",
  "tier": "Basic",
  "cost": {
    "costType": "ActionPoints",
    "baseAmount": 1,
    "isXCost": false,
    "costChangePerTurn": 0,
    "minimumCost": 0,
    "maximumCost": 99
  },
  "effects": [
    {
      "effectContext": "Battle",
      "battleEffectType": "ConfidenceDamage",
      "targetType": "Opponent",
      "amount": 10
    }
  ],
  "description": "Deal 10 Confidence damage.",
  "flavorText": "A direct threat.",
  "upgradeId": "intimidate_001_plus"
}
```

#### Cost Types
- **Free** - `costType: "None"` - No AP required
- **Fixed Cost** - `baseAmount: 2` - Costs 2 AP
- **X Cost** - `isXCost: true` - Costs all remaining AP
- **Dynamic Cost** - `costChangePerTurn: -1` - Gets 1 AP cheaper each turn in hand

#### Cost Modifiers
- **Per-Turn Changes** - Cards can get cheaper/more expensive each turn held
  - `-1` = Gets 1 AP cheaper per turn (delayed power cards)
  - `+1` = Gets 1 AP more expensive per turn (urgency/momentum cards)
- **Runtime Modifiers** - Buffs/debuffs can affect card costs temporarily
- **Min/Max Limits** - Costs are clamped between `minimumCost` and `maximumCost`

#### Battle vs Campaign Effects
- **Battle Effects** - Apply during card battles (damage, block, draw cards, etc.)
  - `ConfidenceDamage`, `EgoDamage`, `GainBlock`, `DrawCards`, etc.
- **Campaign Effects** - Apply to meta-game state (gain funds, increase heat, unlock cards)
  - `GainFunds`, `GainHeat`, `AddCardToDeck`, `UnlockLocation`, etc.
- Cards can have both battle AND campaign effects

### Location Data

```json
{
  "id": "barangay_hall_001",
  "name": "Barangay Hall",
  "type": "Community",
  "description": "Local government center",
  "npcs": ["captain_001", "kagawad_001", "tanod_001"],
  "activities": [
    {
      "type": "battle",
      "opponent": "community_leader",
      "rewards": {
        "support": 500,
        "utangNaLoob": 3
      }
    },
    {
      "type": "event",
      "eventId": "town_hall_meeting"
    },
    {
      "type": "shop",
      "inventory": "basic_political"
    }
  ],
  "heatRisk": "low",
  "connections": ["palengke", "church", "basketball_court"],
  "regionBonus": {
    "type": "utangNaLoob_gain",
    "multiplier": 1.2
  }
}
```

### Event Data

```json
{
  "id": "troll_farm_offer",
  "name": "Troll Farm Offers Services",
  "trigger": {
    "type": "random",
    "dayRange": [10, 40],
    "chance": 0.15,
    "locations": ["any_social"]
  },
  "description": "A shady operator offers fake online support...",
  "choices": [
    {
      "id": "accept_full",
      "text": "Accept Full Package",
      "cost": {
        "campaignFunds": 500
      },
      "effects": [
        { "type": "support", "amount": 2000 },
        { "type": "heat", "amount": 20 }
      ],
      "consequences": [
        {
          "type": "random_event",
          "eventId": "troll_betrayal",
          "chance": 0.20
        }
      ]
    }
    // ... more choices
  ]
}
```

### NPC Data

```json
{
  "id": "captain_001",
  "name": "Kapitan Rodriguez",
  "title": "Barangay Captain",
  "location": "barangay_hall_001",
  "personality": "traditional",
  "relationships": [
    {
      "npcId": "kagawad_001",
      "type": "ally",
      "strength": 8
    },
    {
      "npcId": "rival_politician_001",
      "type": "enemy",
      "strength": 6
    }
  ],
  "questline": "barangay_leadership_quest",
  "rewards": {
    "utangNaLoob": 10,
    "support": 800,
    "card": "community_organizer"
  },
  "preferences": {
    "likesOrigins": ["Religious Leader", "Strongman"],
    "dislikesOrigins": ["Celebrity"],
    "likesActions": ["keep_promises", "community_service"],
    "dislikesActions": ["corruption", "violence"]
  }
}
```

---

## UI/UX Design Flow

### Main Menu Flow

```
Main Menu
├── New Run → Origin Selection → Campaign Map
├── Continue Run → Campaign Map
├── Progression → Meta Upgrade Screen
├── Achievements → Achievement Gallery
├── Statistics → Stats Dashboard
└── Settings → Options Menu
```

### Campaign Map Flow

```
Campaign Map
├── Location Select → Location Detail → Activity
├── Status Panel (always visible)
├── Event Popup → Event Resolution
└── End Day → Next Day Transition
```

### Card Battle Flow

```
Card Battle
├── Battle Start → Turn Loop → Battle End
├── Hand Display
├── Deck/Discard Counters
├── Confidence Bars
└── Effect Animations
```

### Event Screen Flow

```
Event Screen
├── Event Description
├── Choice Buttons
├── Consequence Preview (if unlocked perk)
└── Result Animation
```

---

## Key UI Elements

### Status Bar (Always Visible)

```
╔══════════════════════════════════════════╗
║ Day 23/45  ║  ₱4,500  ║  3L  ║  15U  ║
║ Support: 6,800/10,000  ║  Heat: 42 🔥  ║
╚══════════════════════════════════════════╝
```

### Card Display

```
┌─────────────────┐
│ INTIMIDATE      │ ← Card Name
├─────────────────┤
│   [ICON]        │ ← Card Art
│                 │
│ Attack          │ ← Type
│ Common          │ ← Rarity
├─────────────────┤
│ Deal 10 damage  │ ← Effect
│ +1 Heat         │
├─────────────────┤
│ Cost: Free      │ ← Cost
└─────────────────┘
```

### Location Card

```
┌───────────────────────┐
│ BARANGAY HALL         │
├───────────────────────┤
│  [Location Image]     │
│                       │
│ Community ● Low Heat  │
├───────────────────────┤
│ NPCs: 3               │
│ Activities: 3         │
│ Reputation: ♥♥♥♡♡     │
│                       │
│ [Visit] [View Details]│
└───────────────────────┘
```

---

## Color Coding

### Resource Colors

- **₱ (Campaign Funds)**: Green (#2ecc71)
- **L (Lagay)**: Red (#e74c3c)
- **U (Utang na Loob)**:
  - Positive: Blue (#3498db)
  - Negative: Dark Red (#c0392b)
- **H (Heat)**: 
  - 0-25: Gray (#95a5a6)
  - 26-50: Yellow (#f39c12)
  - 51-75: Orange (#e67e22)
  - 76-99: Red (#e74c3c)
  - 100+: Flashing Red (animated)
- **Support**: Progress bar with gradient (green to gold)

### Card Type Colors

- **Card Types**:
  - Attack: Red (#e74c3c)
  - Defense: Blue (#3498db)
  - Charm: Pink (#e91e63)
  - Leverage: Purple (#9b59b6)
  - Power: Gold (#f39c12)

### Card Rarity Colors

- **Card Rarity**:
  - Common: White/Gray
  - Uncommon: Green (#2ecc71)
  - Rare: Blue (#3498db)
  - Legendary: Gold (#f1c40f)

---

## Animation & VFX

### Essential Animations

#### Card Animations
- **Draw**: Card flies from deck to hand
- **Play**: Card moves from hand to center, flips, effect triggers
- **Discard**: Card flies to discard pile
- **Shuffle**: Deck glows, cards swirl briefly
- **Upgrade**: Card glows gold, transforms

#### Battle Effects
- **Damage**: Red flash on character, confidence bar decreases
- **Heal**: Green pulse, confidence bar increases
- **Buff**: Upward sparkles around character
- **Debuff**: Downward dark particles
- **Charm**: Pink hearts/stars
- **Block**: Blue shield appears

#### Resource Changes
- **₱ Gain**: Coins fly into counter, counter animates up
- **L Gain**: Red envelope appears, sneaky animation
- **U Gain**: Blue handshake icon, warm glow
- **H Gain**: Flame particles, red warning pulse
- **Support Gain**: Crowd cheering animation, bar fills

#### Transition Animations
- **Day Change**: Calendar page flip, sun rises
- **Location Travel**: Map path highlights, character icon moves
- **Event Trigger**: Newspaper headline appears
- **Victory/Defeat**: Confetti or gray overlay

### Performance Considerations
- **Optimize**: Particle systems (limit particles on low-end devices)
- **Toggleable**: Allow players to reduce/disable animations
- **Load times**: Keep animations under 1-2 seconds max
- **Skip option**: Allow ESC/click to skip most animations

---

## Audio Design

### Music Tracks

#### Main Themes (Looping)
1. **Main Menu Theme**: Hopeful, slightly satirical orchestral
2. **Map/Campaign Theme**: Upbeat Filipino folk fusion
3. **Battle Theme (Generic)**: Tense, strategic
4. **Battle Theme (Boss)**: Intense, dramatic
5. **Victory Theme**: Triumphant fanfare
6. **Defeat Theme**: Somber, reflective
7. **Event Theme**: Mysterious, questioning
8. **Scandal Theme**: Ominous, urgent

#### Origin-Specific Themes
- **Strongman**: Military drums, authoritarian
- **Celebrity**: Pop music, glamorous
- **Religious Leader**: Gospel/hymnal, reverent
- **Nepo Baby**: Classical, aristocratic

### Sound Effects

#### UI Sounds
- Button click (satisfying click)
- Button hover (subtle highlight sound)
- Card draw (swoosh)
- Card play (thump, varies by type)
- Resource gain (coins, notifications)
- Heat warning (sizzle, alarm for high Heat)
- Achievement unlock (celebratory chime)

#### Battle Sounds
- Attack cards: Impact, punch, boom
- Defense cards: Shield block, barrier
- Charm cards: Magical chime, sparkle
- Leverage cards: Mechanical, tactical
- Power cards: Dramatic orchestral hit
- Victory: Cheering crowd
- Defeat: Disappointed crowd murmur

#### Ambient Sounds
- **Map**: Birds, city ambiance
- **Locations**: 
  - Church: Choir, bells
  - Market: Crowd chatter, vendors
  - Cockpit: Roosters, crowd roar
  - Office: Keyboard typing, phones

### Voice Acting (Optional)
- **Narrator**: Key event narration
- **NPCs**: Brief voice clips for major characters
- **Player character**: Grunts, reactions (no full dialogue to allow projection)
- **Keep minimal**: Text-based dialogue with voice flavoring

---

## Localization

### Target Languages (Priority Order)

1. **English** (Primary)
   - International audience
   - Development language

2. **Filipino/Tagalog** (High Priority)
   - Native audience (most important)
   - Cultural authenticity
   - Many jokes only work in Filipino

3. **Spanish** (Medium Priority)
   - Large market
   - Some cultural overlap

4. **Japanese** (Medium Priority)
   - Strong indie game market

### Localization Challenges

#### Cultural Context
- Many Filipino political terms don't translate directly
- Humor and satire may not land in other cultures
- Regional references specific to Philippines

#### Solutions
- **Glossary**: In-game glossary for Filipino terms
- **Cultural notes**: Optional tooltips explaining context
- **Adaptation**: Localize humor, not just translate
- **Keep Filipino terms**: "Utang na Loob," "Lagay," etc. with explanations

#### Technical Implementation

```
LocalizationManager
├── CurrentLanguage (string)
├── StringDatabase (Dictionary<string, Dictionary<string, string>>)
├── LoadLanguage(string languageCode)
├── GetString(string key, string fallback)
└── GetFormattedString(string key, params object[] args)
```

### Text Storage Example
```json
{
  "en": {
    "card_intimidate_name": "Intimidate",
    "card_intimidate_desc": "Deal {0} confidence damage. +{1} Heat.",
    "location_barangay_hall_name": "Barangay Hall",
    "event_troll_farm_title": "Troll Farm Offers Services"
  },
  "fil": {
    "card_intimidate_name": "Takutin",
    "card_intimidate_desc": "Gumawa ng {0} pinsala sa kumpiyansa. +{1} Init.",
    "location_barangay_hall_name": "Barangay Hall",
    "event_troll_farm_title": "Alok ng Troll Farm"
  }
}
```

---

## Performance Optimization

### Target Specifications

#### Minimum Specs

CPU: Dual-core 2.0 GHz
RAM: 4 GB
GPU: Integrated graphics (Intel HD 4000 or equivalent)
Storage: 2 GB
OS: Windows 10, macOS 10.14, Ubuntu 18.04

#### Recommended Specs

CPU: Quad-core 2.5 GHz
RAM: 8 GB
GPU: Dedicated graphics (GTX 750 or equivalent)
Storage: 2 GB SSD
OS: Windows 11, macOS 12, Ubuntu 20.04

### Optimization Strategies

#### Asset Optimization

Textures: Use texture atlases, compress appropriately
Audio: MP3/OGG for music, WAV for short SFX
Sprites: Sprite sheets for animations
Fonts: Limit font families, use bitmap fonts for small text

#### Code Optimization

Object pooling: Reuse card objects instead of instantiating
Lazy loading: Load location data only when needed
Caching: Cache frequently accessed data (card stats, etc.)
Async operations: Load assets asynchronously to prevent freezing

#### Memory Management

Unload unused assets: Clear memory after location changes
Texture streaming: Load high-res textures only when needed
Garbage collection: Minimize allocations in hot paths (battle loops)

---

## Testing Strategy

### Test Types

#### Unit Tests

Card effects: Each card effect works correctly
Resource calculations: ₱, L, U, H, Support math accurate
Event triggers: Conditions trigger correctly
AI decisions: Opponent AI makes valid moves

#### Integration Tests

Battle flow: Full battle from start to finish
Campaign flow: Full 45-day run simulation
Save/Load: Data persists correctly
Event chains: Multi-part events resolve properly

#### Playtesting

Balance testing: Is game too easy/hard?
Fun factor: Are players engaged?
Clarity: Do players understand mechanics?
Bug hunting: Find edge cases

### Test Scenarios

#### Critical Path Tests

Full victory run: Play through entire campaign, win
Full defeat run: Play through, intentionally lose
Scandal survival: Trigger scandal at 100H, survive
All endings: Achieve each ending type
All origins: Win with each origin

#### Edge Case Tests

Maximum resources: What happens at ₱999,999?
Minimum resources: What happens at negative U?
Heat overflow: What if Heat goes above 100 during event?
Support overflow: What happens above 30,000?
Empty deck: What if player runs out of cards?

#### Regression Tests

After each update: Run automated test suite
Before release: Full manual playthrough
Beta testing: Community testing period

---

## Development Phases

### Phase 1: Prototype (3-4 months)

**Goals:** Prove core loop is fun

**Deliverables:**

Basic card battle system
2 origins (Strongman, Celebrity)
30 cards total
5 locations
Basic map navigation
Resource management (₱, L, U, H)
Simple event system
One complete 45-day run possible

**Success Criteria:**

Battle system feels good
Deck building is engaging
Players understand resources
Core loop is replayable

### Phase 2: Alpha (4-6 months)

**Goals:** Complete core content

**Deliverables:**

All 4 origins fully implemented
100+ cards
20+ locations
Full event system (50+ events)
NPC relationship system
Project kickback system
Opponent AI
Multiple endings
Basic UI polish

**Success Criteria:**

All origins feel distinct
Content variety sufficient for replayability
Balance is reasonable
No major bugs

### Phase 3: Beta (3-4 months)

**Goals:** Polish and balance

**Deliverables:**

Meta progression system
Achievement system
Statistics tracking
Card upgrades and fusion
Location questlines
Full event variety
Tutorial system
UI/UX polish
Audio implementation
Localization (English + Filipino)

**Success Criteria:**

Game is fun for 50+ hours
Balance is good across origins
Meta progression feels rewarding
UI is clear and attractive

### Phase 4: Release Candidate (2-3 months)

**Goals:** Bug fixing and final polish

**Deliverables:**

All content complete
Full testing pass
Performance optimization
Final balance adjustments
Marketing materials
Platform-specific builds
Documentation

**Success Criteria:**

No critical bugs
Performance targets met
Ready for public release

### Phase 5: Post-Launch Support (Ongoing)

**Goals:** Maintain and expand

**Deliverables:**

Bug fixes
Balance patches
New content (cards, events, origins)
Regional expansions (DLC)
Community features
Quality of life improvements

---

## Monetization Strategy

### Base Game Pricing

#### Option 1: Premium

Price: $14.99 - $19.99
Model: Pay once, own forever
Pros: Clean, no predatory practices
Cons: Higher barrier to entry

#### Option 2: Free-to-Play (NOT RECOMMENDED for this game)

Price: Free
Model: Cosmetic microtransactions
Pros: Larger audience
Cons: Can feel exploitative, harder to balance

**Recommendation:** Premium pricing with fair DLC model

### DLC Strategy

#### Expansion Packs ($9.99 - $14.99 each)

Visayas Expansion: New region, 2 origins, 50 cards, 15 locations
Mindanao Expansion: New region, 2 origins, 50 cards, 15 locations
Presidential Campaign: Extended mode, new mechanics
Origin Pack: 3 new origins (The Technocrat, The Activist, The Warlord)

#### Cosmetic DLC ($2.99 - $4.99)

Card back designs
UI themes
Character portraits
Location skins

#### Support DLC ("Tip Jar") ($4.99)

Optional support for developers
Includes exclusive cosmetics
All proceeds to development team

---

## Platform Strategy

### Primary Platforms

PC (Steam) - Primary market
Mac - Via Steam
Linux - Via Steam

### Secondary Platforms

Nintendo Switch - Great for turn-based games
iOS/Android - Mobile port (later)
PlayStation/Xbox - Console ports (if successful)

### Platform-Specific Considerations

#### Steam Features

Steam Achievements
Steam Cloud saves
Steam Workshop (user-generated content)
Trading cards (optional revenue)
Leaderboards

#### Switch Features

Touch screen support
Joy-Con controls
Portable mode optimization
Nintendo Online integration

#### Mobile Features

Touch-optimized UI
Portrait or landscape mode
Offline play essential
Careful with battery usage
No predatory monetization

---

## Accessibility Features

### Essential Accessibility

#### Visual

Colorblind modes: Alternative color palettes
High contrast mode: Increased readability
Text scaling: Adjustable font sizes
Screen reader support: Read UI elements aloud

#### Audio

Subtitles: All dialogue and narration
Visual cues: Replace audio cues with visual indicators
Volume controls: Separate music, SFX, voice volumes

#### Motor

Remappable controls: Customize all inputs
One-handed mode: Can play with one hand
Reduced animation: Skip/reduce animations
Auto-play options: Assist modes for difficult sections

#### Cognitive

Tutorial system: Comprehensive, replayable
Glossary: In-game reference for all terms
Difficulty options: Multiple difficulty levels
Undo button: Allow taking back moves (in single-player)

---

## Community Features

### In-Game Features

#### Leaderboards

Fastest victories
Highest Support totals
Cleanest victories (lowest Heat)
Daily/Weekly challenges

#### Sharing

Share deck builds (export/import codes)
Share run summaries (Twitter, Discord integration)
Screenshot gallery of victories

#### User-Generated Content (Future)

Custom card designs (balanced by developers)
Custom events (moderated)
Custom campaigns (Steam Workshop)

### External Community

#### Social Media

Twitter: Announcements, patch notes, community highlights
Discord: Community hub, support, feedback
Reddit: Subreddit for discussions
YouTube: Trailers, dev diaries, gameplay

#### Content Creators

Press keys: Provide keys to reviewers, streamers
Creator program: Support fan content
Tournaments: Sponsored competitive events (if multiplayer)

---

## Analytics & Telemetry

### Data Collection (With User Consent)

#### Gameplay Metrics

Win/loss rates by origin
Card usage statistics
Popular deck archetypes
Average Heat levels
Resource spending patterns
Location visit frequency
Event choice distributions

#### Technical Metrics

Crash reports
Performance data (FPS, load times)
Hardware specifications
Platform distribution

#### Retention Metrics

Daily/Weekly active users
Session length
Runs completed
Drop-off points (where players quit)

### Privacy Considerations

Opt-in: Make telemetry optional
Anonymous: No personally identifiable information
Transparent: Explain what data is collected
GDPR compliant: Follow data protection regulations

### Using Analytics

Balance patches: Identify overpowered/underpowered cards
Difficulty tuning: Find where players struggle
Content priorities: What content players engage with most
Bug identification: Common crash points

---

## Development Tools & Workflow

### Version Control

Git: Standard version control
GitHub/GitLab: Repository hosting, issue tracking
Branching strategy:

main: Stable release builds
develop: Active development
feature/: Individual features
hotfix/: Urgent bug fixes

### Project Management

Trello/Jira: Task tracking
Agile sprints: 2-week development cycles
Daily standups: Team sync (if team)
Weekly reviews: Progress assessment

### Build Pipeline

Automated builds: CI/CD for all platforms
Testing automation: Run test suite on each commit
Beta channels: Steam beta branch for testing
Release checklist: Ensure all steps before release

### Documentation

Design docs: Keep updated (like this document)
Code documentation: Comment complex systems
API documentation: For modding support
Player-facing docs: Manual, FAQ, troubleshooting

---

## Risk Mitigation

### Technical Risks

**Risk:** Performance issues on low-end hardware

**Mitigation:** Target minimum specs conservatively, optimize early

**Risk:** Save corruption

**Mitigation:** Multiple backup saves, robust save system, version checking

**Risk:** Balance issues

**Mitigation:** Extensive playtesting, analytics, rapid patching

### Business Risks

**Risk:** Low sales/visibility

**Mitigation:** Strong marketing, press outreach, demo release, content creator partnerships

**Risk:** Negative reception to satire

**Mitigation:** Clear content warnings, emphasize fictional nature, avoid real politician names

**Risk:** Controversy over political content

**Mitigation:** Balance satire (criticize all sides equally), focus on systems not individuals

### Content Risks

**Risk:** Cultural insensitivity

**Mitigation:** Filipino cultural consultants, sensitivity readers, community feedback

**Risk:** Content becomes dated

**Mitigation:** Timeless satire of political archetypes rather than current events

**Risk:** Scope creep

**Mitigation:** Strict feature prioritization, MVP mindset, post-launch content plan

---

## Quality Assurance Checklist

### Pre-Release QA

#### Functionality

 All 4 origins playable start to finish
 All cards functional
 All locations accessible
 All events trigger correctly
 All endings achievable
 Save/Load works correctly
 No critical bugs

#### Balance

 All origins have ~45-55% win rate
 No obviously overpowered cards
 Resources feel meaningful
 Difficulty curve appropriate
 Late game is challenging but fair

#### Polish

 All UI elements functional
 All text proofread
 All animations smooth
 All audio implemented
 Performance targets met
 Accessibility features working

#### Platforms

 Windows build tested
 Mac build tested
 Linux build tested
 Steam integration working
 Achievements unlocking correctly

#### Localization

 English text complete and polished
 Filipino text complete and accurate
 All text displays correctly (no overflow)
 Cultural terms explained

---

## Launch Checklist

### Pre-Launch (1 Month Before)

 Store page live (Steam)
 Press kit prepared
 Review keys sent to media
 Social media campaign started
 Community Discord opened
 Demo available (optional)
 Trailer released

### Launch Week

 Final build uploaded
 All platforms tested
 Day 1 patch ready (if needed)
 Support channels monitored
 Marketing push (social media, ads)
 Streamer outreach

### Post-Launch (First Week)

 Monitor crash reports
 Gather community feedback
 Hot fixes for critical bugs
 Respond to reviews
 Plan first content update
 Thank you message to community

---

## Success Metrics

### Launch Targets (First Month)

Sales: 10,000 units (breakeven)
Reviews: 80%+ positive on Steam
Engagement: Average 15+ hours played
Retention: 40%+ return within week
Community: 1,000+ Discord members

### Long-Term Targets (First Year)

Sales: 100,000 units
Reviews: 85%+ positive (Very Positive on Steam)
Engagement: Average 30+ hours played
DLC: 2 expansions released
Community: 10,000+ Discord members, active modding scene

---

## Contingency Plans

### If Launch Fails

Pivot to free demo + paid full game
Heavy discounts during sales
Free content updates to rebuild goodwill
Focus on community building
Consider early access model for DLC

### If Launch Succeeds Beyond Expectations

Hire additional developers for faster content
Expand to more platforms quickly
Accelerate DLC timeline
Consider multiplayer features
Plan sequel or spin-offs

---

## Implementation Priorities

Technical implementation should prioritize:
1. **Core gameplay feel** - Card battles must be satisfying
2. **Performance** - Run smoothly on minimum specs
3. **Replayability** - Meta progression and variety
4. **Polish** - Professional presentation

---

## Final Notes
This technical document provides a roadmap for implementation. Adjust based on:

Team size and expertise
Budget constraints
Timeline requirements
Platform priorities
Market feedback

**Remember:** Ship a small, polished game rather than a large, buggy one. Start with the core loop (card battles + map navigation + resources), make it fun, then expand.

---

**Navigation:** [🏠 Home](README.md) | [Game Overview](game_overview.md) | [All Design Docs](README.md#-game-design-documentation)