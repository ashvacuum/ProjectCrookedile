# Resource Management

[<à Home](README.md) | [=Ö Full Documentation](README.md#-game-design-documentation)

**Related:** [Game Overview](game_overview.md) | [Origins](origins.md) | [Cards](cards.md) | [Events](events.md)

---

Currency and stat management systems for the political card game.

## Table of Contents

- [Primary Resources](#primary-resources)
- [Origin-Specific Resources](#origin-specific-resources)
- [Resource Conversion](#resource-conversion)
- [Resource Management Strategy](#resource-management-strategy)
- [Economic Systems](#economic-systems)

---

## Primary Resources

### Campaign Funds (±)
**Symbol:** ± (Peso sign)
**Color:** Green (#2ecc71)
**Starting Amount:** Varies by origin (500-5000)

**Uses:**
- Purchase cards from shops
- Pay for events and services
- Fund rallies and advertisements
- Bribe officials (generates Lagay)
- Upgrade cards

**Earning:**
- Project kickbacks (20-50% of budget)
- Fundraising events
- Business endorsements
- Origin-specific abilities (e.g., Religious Leader tithes)
- Selling unwanted cards

**Strategy:**
- Nepo Baby starts richest, excels at ± strategies
- Strongman and Religious Leader start poor
- Balance spending vs. saving for emergencies

---

### Lagay (L)
**Symbol:** L (for "Lagay" - Filipino slang for bribery/grease money)
**Color:** Red (#e74c3c)
**Starting Amount:** Varies by origin (0-10)

**Uses:**
- Bribe officials to avoid consequences
- Silence journalists
- Buy favors from NPCs
- Auto-win difficult battles
- Remove Heat (expensive)
- Trigger special corrupt events

**Earning:**
- Project kickbacks (corruption)
- Shady locations (Cockpit, Alak bar)
- Convert ± at poor exchange rate (±300 = 1L)
- Special events
- Betraying allies

**Consequences:**
- Using Lagay generates Heat (+5-15H per use)
- Can trigger scandal events
- NPCs may discover and lose trust (-U)
- Religious Leader CANNOT use Lagay (instant +80H)

**Strategy:**
- Nepo Baby starts with most Lagay
- Powerful but risky
- Save for critical moments
- Can backfire spectacularly

---

### Utang na Loob (U)
**Symbol:** U
**Color:** Blue (positive, #3498db) / Dark Red (negative, #c0392b)
**Starting Amount:** Varies by origin (-5 to +10)
**Range:** Can go negative

**Concept:**
Filipino cultural concept of reciprocal obligation and debt of gratitude.

**Positive Utang na Loob (Respect & Loyalty):**
- NPCs remember your good deeds
- Unlock special events and favors
- Discounts at shops
- Emergency assistance
- Coalition building

**Negative Utang na Loob (Betrayal & Resentment):**
- NPCs turn against you
- Scandals and sabotage
- Higher costs
- Lost opportunities
- Active opposition

**Earning Positive U:**
- Help NPCs with quests (+3 to +15)
- Keep promises (+5)
- Community service (+8)
- Fair project management (+5)
- Religious Leader bonus (all Charm cards +2U)

**Losing U (Going Negative):**
- Break promises (-10)
- Betray NPCs (-15)
- Corrupt projects (-5)
- Violence against community (-20)
- Ignore requests for help (-2)

**Special Mechanics:**
- At +30U: Unlock relationship web bonuses
- At +50U: "Beloved" status (passive Support gains)
- At -10U: "Betrayed" event triggers
- At -25U: "Blood is Thicker" crisis event
- Nepo Baby cannot earn U naturally (capped at 0)

---

### Heat (H)
**Symbol:** H =%
**Color:** Variable by level
- 0-25: Gray (#95a5a6)
- 26-50: Yellow (#f39c12)
- 51-75: Orange (#e67e22)
- 76-99: Red (#e74c3c)
- 100+: Flashing Red (animated)

**Starting Amount:** Varies by origin (10-40)
**Critical Threshold:** 100 (triggers scandal)

**Concept:**
Media attention and scandal risk. Higher Heat = more scrutiny.

**Causes of Heat:**
- Using Lagay (+5-15H)
- Violence and intimidation (+10-30H)
- Corrupt projects (+20-50H)
- Scandal events (+30-80H)
- Controversial choices (varies)

**Reducing Heat:**
- Visit churches (±200, -15H, once per church)
- PR management (±1500, -15H)
- Lay low / Rest (-10H per rest)
- Clean governance (-5H per clean project)
- Celebrity "PR team" ability (-20H with Clout)

**Heat Consequences:**
- **26-50H:** Journalist encounters increase
- **51-75H:** Random scandal events (20% chance/day)
- **76-99H:** Assassination risk at dangerous locations
- **100H:** SCANDAL EXPLOSION event (survivable but devastating)

**Strategy:**
- Keep below 50H for safety
- Plan Heat reductions before major corruption
- Use churches strategically
- Strongman starts at 30H (high risk, high reward)

---

### Support Points
**Symbol:** Support / Votes
**Color:** Progress bar (green to gold gradient)
**Starting Amount:** Varies by origin (500-1500)
**Victory Threshold:** 10,000

**Concept:**
Your political support and vote count. Primary win condition.

**Earning Support:**
- Win card battles (+500-2000)
- Complete events (+200-3000)
- Control locations (+50-200 per day passive)
- Rallies and campaigns (+1000-2500)
- Project completion (+200-1000)
- NPC endorsements (+500-1500)
- Media appearances (+800-2000)

**Losing Support:**
- Scandals (-1000-5000)
- Broken promises (-500)
- Unpopular decisions (-200-800)
- Opponent actions (varies)
- Betrayals (-1000-2000)

**Support Multipliers:**
- Origin bonuses (Celebrity x2 from social media)
- Location ownership (+50% in owned areas)
- High Utang na Loob (+100 per day at 50+U)

---

## Origin-Specific Resources

### Fear (Strongman)
**Starting Amount:** 75
**Color:** Dark red/black
**Range:** 0-200

**Earning:**
- Intimidation tactics (+10-20)
- Violent victories (+15)
- Police/military events (+20-40)

**Spending:**
- Skip opponent turn (20 Fear)
- Deal instant damage (35 Fear)
- Block scandal (50 Fear)
- Auto-win battle (75 Fear)

**Effects:**
- High Fear (75-100): Easier victories, +30% Heat
- Low Fear (0-39): Lose origin bonuses

---

### Clout (Celebrity)
**Starting Amount:** 50
**Color:** Pink/purple
**Range:** 0-300

**Earning:**
- Charm victories (+10)
- Viral events (+20-50)
- Photo-ops (+15)
- Media appearances (+25)
- Daily social media (+5)

**Spending:**
- Boost card (25 Clout)
- Cancel Heat gain (40 Clout)
- Convert to Support (60 Clout = 6000 Support)
- Special endorsement (100 Clout)
- Scandal recovery (150 Clout)

**Effects:**
- High Clout: Media protection, viral bonuses
- Low Clout: Lose star power

---

### Faith (Religious Leader)
**Starting Amount:** 100
**Color:** White/gold
**Range:** 0-150

**Earning:**
- Win with Sermon cards (+15)
- Prayer services (+20)
- Community service (+10)
- Keep promises (+25)
- Stay clean (+ 5/day under 30H)

**Spending:**
- Reduce Heat (30 Faith = -20H)
- Convert opponent (50 Faith)
- Survive scandal (70 Faith - consumes all)
- Perform miracle (100 Faith)

**Effects:**
- High Faith (75-100): +30% card effects
- Low Faith (0-39): Followers question you, lose perks

---

### Influence (Nepo Baby)
**Starting Amount:** 100
**Color:** Purple/royal
**Range:** 0-200

**Earning:**
- Use family connections (+10)
- Maintain dynasty image (+5-15)
- Control family locations (+20)

**Losing:**
- Public failures (-20)
- Scandals (-30)
- Break family traditions (-30)

**Spending:**
- Auto-win battle (20 Influence)
- Remove all Heat (40 Influence, once/run)
- Inherit legendary card (60 Influence)
- Emergency family meeting (80 Influence)

**Effects:**
- High Influence (75-100): Full dynasty support
- Low Influence (0-39): Family withdraws support

---

## Resource Conversion

### Official Conversions
- **± to Lagay:** ±300 = 1L (poor rate, +5H)
- **± to Support:** ±1 = 2 Support (via Helicopter Money card)
- **Clout to Support:** 1 Clout = 100 Support (Celebrity)
- **Faith to ±:** Special events (Religious Leader)

### Hidden Conversions
- **Support to ±:** Fundraiser action (10% of Support ’ ±)
- **Utang na Loob to Support:** 1U = 50 Support (at end, passive)
- **Heat penalty:** Every 1H above 50 = -1 Support at election

---

## Resource Management Strategy

### Early Game (Days 1-15)
**Priority:** Build foundation
- Focus on gaining Support (+500-1000/day)
- Keep Heat below 40
- Build positive Utang na Loob (+10-20)
- Moderate ± spending

### Mid Game (Days 16-30)
**Priority:** Expand and dominate
- Control key locations
- Build origin-specific resource (Fear/Clout/Faith/Influence)
- Strategic use of Lagay (if needed)
- Prepare for COMELEC deadline (5000 Support minimum)

### Late Game (Days 31-45)
**Priority:** Final push to victory
- Massive Support gains
- Manage Heat carefully (avoid scandal)
- Use saved resources for critical battles
- Secure 10,000 Support by Day 45

---

## Economic Systems

### Project Kickbacks
Projects offer opportunities for corruption:
- **Clean:** +±200, +1000 Support, 0H
- **20% Kickback:** +±800, +800 Support, +15H
- **50% Kickback:** +±2000, +400 Support, +35H, gain Lagay

### Campaign Finance Loop
1. Earn Support through actions
2. Convert some Support to ± (Fundraiser)
3. Use ± to buy cards and services
4. Use cards to win battles and gain more Support
5. Repeat strategically

### Corruption Spiral
  **Warning:** Lagay use can spiral out of control
1. Use Lagay ’ Gain advantage
2. Gain Heat from Lagay use
3. Need more Lagay to fix Heat problems
4. More Lagay = More Heat
5. Scandal at 100H

**Mitigation:**
- Use churches to reduce Heat
- Balance corruption with clean actions
- Plan Heat management

---

## Resource Quick Reference

| Resource | Symbol | Start Range | Victory Impact | Risk Level |
|----------|--------|-------------|----------------|------------|
| Campaign Funds | ± | 500-5000 | Indirect | Low |
| Lagay | L | 0-10 | Indirect | High |
| Utang na Loob | U | -5 to +10 | +50 Support/U | Medium |
| Heat | H | 10-40 | -1 Support/H >50 | High |
| Support | - | 500-1500 | DIRECT (10k to win) | Critical |
| Fear | =J | 75 | Indirect | Medium |
| Clout | ( | 50 | Convertible to Support | Low |
| Faith | =O | 100 | Indirect | Low |
| Influence | =Q | 100 | Indirect | Medium |

---

**Navigation:** [<à Home](README.md) | [Game Overview](game_overview.md) | [Origins](origins.md) | [Cards](cards.md)
