# CORRUPTION TYCOON - FINAL MASTER DESIGN DOCUMENT

> *A satirical political management game set in the Philippines, combining roguelike deckbuilding with simplified relationship mechanics and project management*

## 📋 TABLE OF CONTENTS

- [🎯 Game Overview](#-game-overview)
- [🃏 Simplified Card System](#-simplified-card-system)
- [📊 Core Resources & Victory](#-core-resources--victory)
- [⏰ Project Timeline System](#-project-timeline-system)
- [👥 Simplified Relationship Network](#-simplified-relationship-network)
- [📈 Impunity & Corruption Progression](#-impunity--corruption-progression)
- [🗺️ Interactive Town Map](#️-interactive-town-map)
- [🌍 Philippine Seasons & Culture](#-philippine-seasons--culture)
- [👤 Player Classes](#-player-classes)
- [⚔️ Enemy Initiative System](#️-enemy-initiative-system)
- [💻 Technical Implementation](#-technical-implementation)

---

## 🎯 GAME OVERVIEW

### **Core Concept**
A political corruption simulator where players spend one year as a Filipino politician building power, managing relationships, and surviving until the local election. Players must balance public approval, wealth accumulation, and avoiding investigation while making moral choices about corruption.

### **Victory Condition**
**Single Goal**: Win the local mayoral election at the end of one year (3 seasons)
- **Timeline**: Summer → Rainy Season → Christmas Season → Election Day
- **Success**: Survive with sufficient approval and votes to win
- **Player Story**: HOW you win (clean vs corrupt) defines your narrative

### **Core Gameplay Loop**
```
Turn Structure (30-60 seconds):
1. PLANNING: Review resources, check relationships, assess threats
2. ACTION: Draw cards, play using energy, make project decisions
3. RESOLUTION: Apply effects, random events, relationship changes

Session Length: 45-60 minutes per complete campaign
```

---

## 🃏 SIMPLIFIED CARD SYSTEM

### **Universal 5-Card Type System**
*Every card works in every situation with context-sensitive effectiveness*

#### **💝 CHARM CARDS** (Pink - Win hearts and minds)
```
PURPOSE: Building positive relationships and goodwill
EXAMPLES: Kind Words, Small Gift, Cultural Respect, Shared Meal
EFFECTS: Build rapport, increase satisfaction, reduce suspicion
STRATEGY: Sustainable relationship building, low risk but sometimes costly
```

#### **🛡️ DEFENSE CARDS** (Blue - Protect yourself)
```
PURPOSE: Avoiding consequences and maintaining plausible deniability
EXAMPLES: Plausible Deniability, Legal Technicality, Emergency Meeting
EFFECTS: Reduce heat, protect approval, buy time during crises
STRATEGY: Reactive survival tools, damage control
```

#### **⚔️ ATTACK CARDS** (Red - Apply pressure)
```
PURPOSE: Forcing others to comply through various means
EXAMPLES: Regulatory Pressure, Media Leak, Economic Squeeze
EFFECTS: Force compliance, weaken opponents, create leverage
STRATEGY: High risk/high reward, can backfire spectacularly
```

#### **🎯 LEVERAGE CARDS** (Yellow - Seize opportunities) *UNLOCKABLE*
```
PURPOSE: Exploiting advantages and information for maximum benefit
EXAMPLES: Perfect Timing, Information Advantage, Coalition Building
EFFECTS: Maximize gains, turn disadvantages into advantages
UNLOCK: Through completing projects, building relationships, surviving crises
```

#### **💥 POWER CARDS** (Purple - Game changers) *UNLOCKABLE*
```
PURPOSE: Nuclear options that fundamentally alter the political landscape
EXAMPLES: Nuclear Option, Political Alliance, Emergency Powers
EFFECTS: Permanently change campaign dynamics, massive impact
UNLOCK: Major achievements, max relationships, high impunity levels
```

### **Card Progression**
```
STARTER DECK (14 cards):
├── 6 Charm Cards (basic politeness and respect)
├── 4 Defense Cards (basic self-protection)  
├── 4 Attack Cards (basic pressure tactics)
└── Leverage/Power cards unlocked through gameplay

ACQUISITION METHODS:
├── Relationship-based: Each ally teaches their specialty
├── Achievement-based: Major accomplishments unlock power cards
├── Intel-based: Purchasing information unlocks targeted cards
└── Progression: Basic → Enhanced → Powerful → Ultimate versions
```

---

## 📊 CORE RESOURCES & VICTORY

### **Primary Resources**
```
BUDGET: 200/turn (government allocation)
├── Used for: Project funding, bribes, gifts
├── Management: Limited supply, choose projects carefully

APPROVAL: 30-100 (your "HP")
├── Lose Condition: Below 30 = career over
├── Win Condition: Sufficient approval for election victory
├── Sources: Projects, community relations, PR events

WEALTH: 0+ (hidden accumulation)
├── Victory Goal: Accumulate through corruption (optional)
├── Uses: Bribes, gifts, eliminating enemies, luxury
├── Sources: Project skimming, ghost employees, criminal schemes

HEAT: 0-100 (investigation pressure)
├── Crisis Threshold: 75+ triggers major investigations
├── Sources: Corruption, scandals, enemy actions
├── Management: Defense cards, ally protection, time

ENERGY: 3-5/turn (card play limitation)
├── Function: Limits cards played per turn
├── Strategy: Choose most effective cards for situation

IMPUNITY: 0-100 (corruption immunity)
├── Function: Unlocks criminal relationships and extreme actions
├── Sources: Police control, judicial corruption, cover-up success
├── Thresholds: 40/60/80/95+ unlock increasingly dangerous allies
```

### **Victory Requirements**
```
ELECTION SUCCESS FACTORS:
├── Approval Level: Minimum 40+ for realistic victory chance
├── Community Support: Project completions and relationship bonuses
├── Opposition Strength: Reduced through neutralization or alliance
├── Campaign Resources: Wealth helps but isn't required
└── Scandal Survival: Heat management crucial for final weeks
```

---

## ⏰ PROJECT TIMELINE SYSTEM

### **Variable Project Durations**
```
QUICK PROJECTS (1 Week):
├── Examples: Pothole repairs, streetlight installation, park cleanup
├── Cost: 25-150 budget
├── Approval: +5 to +15
├── Strategy: Emergency boosts, election week final impressions
├── Risk: Low heat, minimal investigation attention

STANDARD PROJECTS (2 Weeks):
├── Examples: Road resurfacing, school renovation, small bridge
├── Cost: 300-500 budget  
├── Approval: +15 to +30
├── Strategy: Balanced risk/reward, steady campaign building
├── Risk: Moderate heat, standard oversight

MAJOR PROJECTS (4 Weeks):
├── Examples: Highway extension, hospital wing, sports complex
├── Cost: 700-1200 budget
├── Approval: +30 to +50
├── Strategy: High impact but long vulnerability window
├── Risk: High heat, federal attention possible

MEGA PROJECTS (6-8 Weeks):
├── Examples: Airport terminal, university campus, medical center
├── Cost: 1500-3500 budget
├── Approval: +50 to +100
├── Strategy: Legacy-defining gambles
├── Risk: Extreme heat, international attention, opposition mobilization
```

### **Project Management**
```
SKIMMING SYSTEM:
├── Any project can be skimmed for personal wealth
├── Skim Level: Player choice (Clean/Light/Moderate/Heavy)
├── Trade-off: More skim = less approval + more heat
├── Authenticity: Even streetlights can be skimmed (realistic)

SEASONAL CONSIDERATIONS:
├── Summer: Ideal construction season, all projects normal timeline
├── Rainy Season: Outdoor projects +1-2 weeks delay
├── Christmas: Rush completions for election ceremonies

CRISIS POTENTIAL:
├── Week 1: Planning problems (permits, opposition, budget)
├── Week 2-3: Construction issues (strikes, materials, weather)
├── Week 4+: Federal attention, quality scandals, cost overruns
├── Abandonment: Incomplete projects = massive penalties
```

---

## 👥 SIMPLIFIED RELATIONSHIP NETWORK

### **Relationship Mechanics**
```
CORE SYSTEM:
├── Meet Character → Build Rapport (0-100)
├── High Rapport → Powerful Passive Bonus (like Slay the Spire relic)
├── Betrayal → Devastating Curse until character eliminated
├── Simple Management: No complex dialogue, just strategic partnerships
```

### **Government Sector Characters**
```
SECRETARY CARMEN:
├── Bonus: "Administrative Efficiency" - All government actions cost -1 energy
├── Penalty: "Bureaucratic Nightmare" - All permits cost +50% budget

DEPUTY MAYOR SANTOS:
├── Bonus: "Official Authority" - Can use Attack cards as Defense cards
├── Penalty: "Internal Opposition" - All Attack cards cost +1 energy

CITY ATTORNEY RIVERA:
├── Bonus: "Legal Protection" - First legal crisis per turn auto-resolves
├── Penalty: "Legal Warfare" - Random legal crisis every 3 turns

POLICE COMMISSIONER TORRES: (High Risk/High Reward)
├── Bonus: "Above the Law" - Immune to investigation events
├── Penalty: "Federal Task Force" - +5 Heat per turn, FBI investigation
```

### **Business Sector Characters**
```
BANK MANAGER ALEX:
├── Bonus: "Financial Empire" - +25% wealth from all sources
├── Penalty: "Frozen Assets" - -50% wealth from all sources

CONTRACTOR DON RICARDO:
├── Bonus: "Construction Empire" - Projects cost -50% budget, +1 week speed
├── Penalty: "Contractor Blacklist" - Projects cost +100% budget, +1 week delay

ACCOUNTANT JENNIFER:
├── Bonus: "Creative Accounting" - Can hide wealth from investigations
├── Penalty: "Audit Trail" - All financial schemes generate +2 Heat

ENGINEER PATRICIA:
├── Bonus: "Quality Assurance" - All projects generate +5 approval
├── Penalty: "Structural Problems" - All projects generate -5 approval
```

### **Media Sector Characters**
```
TV JOURNALIST RIVER:
├── Bonus: "Media Empire" - +2 Approval per turn, preview enemy actions
├── Penalty: "Hostile Press" - -2 Approval per turn, exposé articles

RADIO HOST CARLOS:
├── Bonus: "Voice of the People" - All PR cards have double effect
├── Penalty: "Talk Show Target" - All PR cards have half effect

NEWSPAPER EDITOR RAMON:
├── Bonus: "Editorial Support" - Convert crises into positive stories
├── Penalty: "Front Page Scandal" - Random damaging articles
```

### **Community Sector Characters**
```
PRIEST SAGE:
├── Bonus: "Divine Authority" - All Charm cards become Power cards
├── Penalty: "Lost Blessing" - All Charm cards cost +1 energy

SCHOOL PRINCIPAL RODRIGUEZ:
├── Bonus: "Educational Excellence" - School projects generate double approval
├── Penalty: "Academic Scandal" - School projects generate zero approval

TEACHER SARAH:
├── Bonus: "Community Respect" - +1 Approval per turn from parent support
├── Penalty: "Parent Complaints" - -1 Approval per turn from anger
```

---

## 📈 IMPUNITY & CORRUPTION PROGRESSION

### **Impunity Meter (0-100)**
```
CORRUPTION THRESHOLDS:
├── 0-39: "Clean Politician" - Basic governance only
├── 40-59: "Connected Official" - Minor criminal contacts available
├── 60-79: "Corrupt Authority" - Serious criminal partnerships
├── 80-94: "Criminal Partner" - Major crime empire connections
├── 95-100: "Shadow Emperor" - Ultimate criminal power (hitman access)

IMPUNITY SOURCES:
├── Police Relationships: +2 per corrupt officer
├── Judicial Connections: +3 per compromised judge
├── Cover-up Success: +5 per major scandal survived
├── Evidence Destruction: +3 per investigation shutdown
├── Witness Silencing: +10 per person "convinced"
```

### **Criminal Network (Impunity-Gated)**
```
TIER 1 CRIMINALS (40+ Impunity):
├── Bar Owner Tony: Information hub vs loose lips
├── Gambling Den Manager Rosa: +2 wealth/turn vs police raids

TIER 2 CRIMINALS (60+ Impunity):
├── Smuggler Captain Reyes: +5 wealth/turn vs federal attention
├── Loan Shark Eddie: Force cooperation vs violence escalation

TIER 3 CRIMINALS (80+ Impunity):
├── Drug Lord Santos: +10 wealth/turn vs DEA investigation
├── Arms Dealer Garcia: Attack cards become Power cards vs ATF raids

TIER 4 CRIMINALS (95+ Impunity):
├── The Cleaner (Hitman): Eliminate any character vs assassination attempts
├── Crime Family Don: Immunity to betrayals vs military intervention
```

### **Moral Event Horizons**
```
CORRUPTION ESCALATION:
├── 40+ Impunity: "I know people who can help" - First criminal contact
├── 60+ Impunity: "Sometimes pressure isn't enough" - Violence threshold
├── 80+ Impunity: "We run this city now" - Crime empire partnership
├── 95+ Impunity: "Some problems require permanent solutions" - Murder threshold
```

---

## 🗺️ INTERACTIVE TOWN MAP

### **Town Layout**
```
LIVING POLITICAL DOMAIN:
                    🏔️ MOUNTAIN DISTRICT 🏔️
                         │ (Development Zone)
                    🏗️──🎿──🏘️──⛰️
                    │         │    │
         🌾 RURAL   🏭──🏘️──🏘️──🏘️──🌊 WATERFRONT
         DISTRICT   │    │    │    │    │  DISTRICT
              │     🏪──🏠──⭐──🏠──🏫──🏊 (Tourism/Port)
              │     │    │    │    │    │
         🚜──🌾──🏗️──🏘️──🏛️──🏘️──⛪──🌊
              │         │    │         │
              🚛────HIGHWAY─🏛️────🏢──🚢
                         │
                   FEDERAL DISTRICT

⭐ = Your Office (expandable headquarters)
🏛️ = Government Buildings (encounters)
🏠 = Residential Areas (approval sources)
🏪 = Business District (wealth opportunities)
🏫 = Public Services (project locations)
⛪ = Community Centers (relationship building)
🏗️ = Active Construction Projects
```

### **Office Expansion System**
```
HEADQUARTERS EVOLUTION:
├── Floor 1 (Starting): Basic operations, 3 staff, store 500 wealth
├── Floor 2 (1000 wealth): Administrative expansion, 6 staff, ghost employees (+50/turn)
├── Floor 3 (2500 wealth): Executive suite, 10 staff, advanced operations (+150/turn)
├── Basement (5000 wealth): Shadow operations, unlimited storage, max corruption (+300/turn)

GHOST EMPLOYEE SYSTEM:
├── Create fake government positions for passive wealth income
├── Risk: Each ghost employee increases heat generation
├── Management: Balance income vs investigation attention
├── Crisis: Convert ghosts to "consultants" during audits
```

### **Dynamic Visual Systems**
```
LIVING WORLD INDICATORS:
├── Office Growth: Small building → imposing fortress
├── District Approval: Campaign signs vs protest graffiti
├── Heat Visualization: Clean town → police state atmosphere
├── Project Progress: Blueprint → construction → ribbon cutting
├── Enemy Activity: FBI vans, investigation teams, protests
├── Resource Flows: Money streams from buildings to office
├── Threat Warnings: Red borders, alert icons for crises
```

---

## 🌍 PHILIPPINE SEASONS & CULTURE

### **Three Season System**
```
SUMMER SEASON (Tag-init - Months 1-4):
├── Weather: Bright, dusty, intense heat
├── Politics: Peak construction season, outdoor rallies
├── Challenges: Water shortages, power outages, heat complaints
├── Opportunities: Major infrastructure projects, emergency contracts
├── Projects: Normal timeline, ideal construction conditions

RAINY SEASON (Tag-ulan - Months 5-10):
├── Weather: Gray skies, flooding, muddy conditions
├── Politics: Emergency response, flood control critical
├── Challenges: Construction delays, transportation problems
├── Opportunities: Emergency fund allocation, disaster relief skimming
├── Projects: Outdoor work +1-2 weeks delay

CHRISTMAS SEASON (Tag-lamig - Months 11-12):
├── Weather: Cool, clear, festive atmosphere with parol decorations
├── Politics: Gift-giving season, charity events, campaign finale
├── Challenges: Year-end budget pressure, holiday expectations
├── Opportunities: Christmas bonuses, goodwill events, final project openings
├── Projects: Rush to complete before election
```

### **Cultural Integration**
```
AUTHENTIC FILIPINO ELEMENTS:
├── Utang na Loob: Favor-trading system with all characters
├── Bayanihan: Community cooperation during crises (rainy season)
├── Pakikipagkunware: Maintaining face while political maneuvering
├── Religious Influence: Church relationships crucial for community approval
├── Family Politics: Extended family networks affect relationships
├── Regional Identity: Different districts with distinct cultural expectations
```

---

## 👤 PLAYER CLASSES

### **Starting Character Options**
```
NEPO BABY (Easy Mode):
├── Starting: Approval 80, Wealth 200, Heat Resistance +1
├── Special: "Legacy Protection" - First approval drop below 30 becomes 35
├── Playstyle: Protected from consequences, passive income focus

BUSINESSMAN (Medium):
├── Starting: Approval 65, Wealth 300, Budget Efficiency +10%
├── Special: "Market Savvy" - +1 wealth per 10 approval over 50
├── Playstyle: Economic efficiency, infrastructure investment focus

ACTOR/SINGER (Medium-Hard):
├── Starting: Approval 90, Wealth 100, PR Effectiveness +50%
├── Special: "Star Power" - Approval gains/losses amplified by 25%
├── Playstyle: High-risk approval swings, media manipulation

NOBODY (Hard Mode):
├── Starting: Approval 45, Wealth 50, Heat Generation +1
├── Special: "People's Champion" - Clean projects give bonus approval
├── Playstyle: Grassroots appeal, reform vs corruption tension

RELIGIOUS LEADER (Medium-Easy):
├── Starting: Approval 75, Wealth 100, Divine Favor resource
├── Special: "Moral Authority" - Divine Favor system replaces heat mechanics
├── Playstyle: Moral authority manipulation, congregation-based schemes
```

---

## ⚔️ ENEMY INITIATIVE SYSTEM

### **Dynamic Opposition**
```
THREAT ESCALATION BY HEAT LEVEL:
├── 0-20 Heat: Local complaints, minor media attention
├── 21-40 Heat: Investigative journalism, reform group organizing
├── 41-60 Heat: Police investigations, federal attention
├── 61-80 Heat: Coordinated opposition, federal task forces
├── 81-100 Heat: International intervention, revolution potential

ENEMY-INITIATED ENCOUNTERS:
├── Investigative campaigns by hostile journalists
├── Federal investigation sequences (multi-week pressure)
├── Reform coalition building and electoral challenges
├── Opposition coordination timed for maximum damage
├── Crisis exploitation when you're most vulnerable
```

### **Enemy Adaptation**
```
OPPONENTS LEARN FROM YOUR TACTICS:
├── Heavy Charm Use → Bring charm-resistant personalities
├── Heavy Attack Use → Increase security and legal protection
├── Heavy Defense Use → Try different angles of attack
├── Leverage Reliance → Research your own vulnerabilities
├── Power Card Use → Escalate to matching force levels
```

---

## 💻 TECHNICAL IMPLEMENTATION

### **Development Phases**
```
PHASE 1 (Weeks 1-2): Core Foundation
├── 5-card system with 14 starter cards
├── Basic resource management (Budget, Approval, Wealth, Heat)
├── Simple minimap with office + 6 key locations
├── Basic relationship system (5 core characters)
├── Project system (2 quick, 2 standard projects)

PHASE 2 (Weeks 3-4): Content Expansion
├── Full card database (50+ cards across all types)
├── Complete character network (20+ characters)
├── Full project variety (all duration types)
├── Office expansion system
├── Impunity meter and criminal unlocks

PHASE 3 (Weeks 5-6): Advanced Systems
├── Enemy initiative and adaptation
├── Seasonal effects and cultural events
├── Complete minimap with all districts
├── Dynamic election finale
├── Multiple ending narratives

PHASE 4 (Weeks 7-8): Polish & Balance
├── Playtesting and balance adjustments
├── Audio and visual effects
├── Tutorial and onboarding
├── Achievement and progression systems
├── Performance optimization
```

### **Unity Architecture**
```
CORE SYSTEMS:
├── GameManager: Overall game state and turn flow
├── ResourceManager: Budget, Approval, Wealth, Heat, Impunity tracking
├── CardManager: Deck building, hand management, card effects
├── RelationshipManager: Character bonuses/curses, rapport tracking
├── ProjectManager: Infrastructure timeline and completion system
├── MapManager: Interactive town, office expansion, visual updates
├── SeasonManager: Weather effects, cultural events, project impacts
├── EnemyManager: Opposition AI, investigation systems, threat escalation
```

---

## 🎯 DESIGN PHILOSOPHY

### **Core Principles**
1. **Authentic Tension**: Every decision creates genuine moral/strategic dilemma
2. **Cascading Consequences**: Small early choices meaningfully impact late game
3. **Satirical Commentary**: Corruption mechanics naturally reveal political realities
4. **Strategic Mastery**: Simple systems with emergent complexity
5. **Cultural Authenticity**: Genuine Philippine political and cultural setting

### **Replayability Drivers**
```
MULTIPLE VIABLE STRATEGIES:
├── "Clean Governance": Minimal corruption, high approval focus
├── "Business Empire": Wealth accumulation through infrastructure
├── "Criminal Network": High impunity, underground alliances
├── "Media Mogul": Information control and narrative dominance
├── "Political Machine": Balanced corruption with relationship focus

NARRATIVE VARIETY:
├── Same victory condition (win election) with different moral journeys
├── Character relationship combinations create unique storylines
├── Seasonal events and random crises ensure no two campaigns identical
├── Enemy adaptation means strategies must evolve
├── Multiple character classes with distinct gameplay approaches
```

---

**"The question isn't whether you'll win the election - it's how corrupted you'll become in the process."**

*Ready for development with complete, cohesive design that balances strategic depth with accessible mechanics.*