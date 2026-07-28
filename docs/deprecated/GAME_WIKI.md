> [!WARNING]
> **Deprecated — no longer in use.** Snapshot of a codebase state that no longer exists.
> Kept because the ideas may still be worth mining; the specifics are not.
> **Superseded by:** [`readme.md`](../../readme.md), [`docs/roadmap.md`](../roadmap.md)
> Index: [`docs/deprecated/README.md`](README.md)  ·  Back to [`readme.md`](../../readme.md)

---

> [!WARNING]
> **Pre-redesign document — may be inaccurate (flagged 2026-06).** Crookedile underwent a major combat + class redesign. Canonical design now lives in [`docs/core-design.md`](docs/core-design.md) and [`docs/crookedile-starter-decks.md`](docs/crookedile-starter-decks.md); current code/architecture is summarized in [`readme.md`](readme.md). Treat specifics below as historical until reconciled.

# CROOKEDILE — Game Wiki

> **Philippine Political Card Game** · Roguelike Deck-Builder · Absurdist Satire

This is the single source of truth for all game design and systems knowledge. For implementation status and architecture analysis, see [SYSTEMS_STUDY.md](SYSTEMS_STUDY.md).

---

## Table of Contents

1. [The Game at a Glance](#1-the-game-at-a-glance)
2. [Core Game Loop](#2-core-game-loop)
3. [Resources](#3-resources)
4. [Card System](#4-card-system)
5. [Battle System](#5-battle-system)
6. [Origins (Classes)](#6-origins-classes)
7. [The Campaign Layer](#7-the-campaign-layer)
8. [Meta Progression](#8-meta-progression)
9. [Design Pillars](#9-design-pillars)

---

## 1. The Game at a Glance

| | |
|---|---|
| **Genre** | Roguelike Deck-Builder + Social Navigation |
| **Tone** | Absurdist Political Satire |
| **Setting** | Philippines — 45 days before an election |
| **Win Condition** | 10,000 Support Points by Day 45, below 100 Heat |
| **Inspiration** | Griftlands (battle system), Slay the Spire (map), Potionomics (town map) |

You play as one of four flawed political archetypes — Strongman, Celebrity, Religious Leader, or Nepo Baby — navigating a procedurally generated campaign through card battles (political negotiations), location visits, and random events.

**Design Pillars:**
1. **Meaningful Choices** — Every decision has downstream consequences
2. **Dark Humor** — Serious topics treated with satirical edge
3. **Replayability** — Roguelike structure ensures variety
4. **Cultural Authenticity** — Grounded in Philippine political reality (exaggerated)
5. **Strategic Depth** — Multiple viable paths to victory

> *Content Warning: Satirical content about political violence, corruption, religious manipulation, and class inequality. All characters are fictional parody.*

---

## 2. Core Game Loop

### The 45-Day Campaign

Each run is a **45-day countdown to Election Day**. Every location visit costs 1 day. Days are structured in three phases:

```
MORNING          →   AFTERNOON        →   EVENING (Auto)
Visit Location       Visit Location        Random Event Roll
  or                   or                  News Cycle
Rest (-10 Heat)      Card Workshop         Opponent AI Actions
                       or
                     Fundraiser
```

### Victory Conditions

| Ending | Requirements | Reward |
|--------|-------------|--------|
| **Squeaky Clean Champion** | < 20 Heat + > 12,000 Support | "Reformist" party unlocked |
| **Necessary Evil Victor** | 20–60 Heat + > 10,000 Support | Standard progression |
| **Trapo Triumphant** | 60–99 Heat + > 10,000 Support | "Shameless" perk unlocked |
| **Pyrrhic Victory** | Survived scandal + won + < 0 Utang na Loob | Special cards unlocked |

### Loss Conditions

1. **Heat reaches 100** → Scandal event (may be survivable with resources)
2. **Support < 5,000 by Day 45** → Eliminated in primary
3. **Assassinated** → Failed to defend in special encounter
4. **COMELEC Disqualification** → Specific event chain failure

### Milestone Events

| Day | Event |
|-----|-------|
| 7, 14, 21, 28, 35, 42 | TV Debates — high-stakes card battles, broadcast nationwide |
| 30 | COMELEC Filing Deadline — need 5,000 Support or pay ₱5,000 bypass |
| 45 | Election Day — final Support tally, victory speeches |

---

## 3. Resources

Crookedile has **two tiers of resources**: primary resources shared by all origins, and origin-specific resources that define each playstyle.

### Primary Resources

#### Campaign Funds (₱)
The primary currency. Used for buying cards at shops, paying for events, funding rallies, bribing officials (generating Lagay), and upgrading cards.

**Earn by:** Project kickbacks, fundraising events, business endorsements, origin abilities (e.g., tithes)
**Danger:** Spending too fast leaves you unable to react to emergencies

| Origin | Starting ₱ |
|--------|-----------|
| Strongman | ₱1,000 |
| Religious Leader | ₱500 |
| Celebrity | ₱3,000 |
| Nepo Baby | ₱5,000 |

---

#### Lagay (L)
Filipino slang for bribery/grease money. Powerful but dangerous.

**Uses:** Bribe officials, silence journalists, auto-win battles, remove Heat, trigger special corrupt events
**Earn by:** Project kickbacks (corruption), shady locations, ₱300 = 1L conversion (+5H)
**Consequences:** Every use generates +5–15H, can trigger scandal events, NPCs may discover it

> **Religious Leader CANNOT use Lagay** — attempting it causes instant +80H

---

#### Utang na Loob (U)
The core social currency of Filipino culture — reciprocal obligation and debt of gratitude. Can go **negative**.

**Positive U:** NPCs remember good deeds → discounts, emergency help, coalition building, passive Support
**Negative U:** NPCs turn against you → sabotage, higher costs, active opposition

| Threshold | Effect |
|-----------|--------|
| +50U | "Beloved" status — passive Support gains |
| +30U | Relationship web bonuses unlocked |
| −10U | "Betrayed" event triggers |
| −25U | "Blood is Thicker" crisis event |

> **Nepo Baby cannot earn U naturally** — hard capped at 0 until exceptional actions unlock it

---

#### Heat (H)
Media scrutiny and scandal risk. Starts at 10–40 depending on origin. **Critical threshold: 100.**

| Heat Level | Color | Effect |
|-----------|-------|--------|
| 0–25 | Gray | Safe |
| 26–50 | Yellow | Journalist encounter frequency increases |
| 51–75 | Orange | 20% chance/day of random scandal event |
| 76–99 | Red | Assassination risk at dangerous locations |
| 100+ | Flashing | SCANDAL EXPLOSION — devastating but survivable |

**Reduce Heat by:** Churches (₱200, −15H), PR management (₱1,500, −15H), Rest (−10H), clean governance (−5H/project)

---

#### Support Points
The **primary win condition**. Need 10,000 by Day 45.

**Earn by:** Winning card battles (+500–2,000), events (+200–3,000), owning locations (+50–200/day passive), rallies (+1,000–2,500), NPC endorsements (+500–1,500)
**Multipliers:** Celebrity x2 from social media; owned locations +50%; High U +100/day at 50+U
**Lose by:** Scandals (−1,000–5,000), broken promises (−500), betrayals (−1,000–2,000)

**Hidden conversion:** Every 1H above 50 = −1 Support at election day

---

### Origin-Specific Resources

Each origin has a **secondary meter** that powers their unique identity:

| Resource | Origin | Range | High Effect | Low Effect |
|----------|--------|-------|-------------|------------|
| **Fear** | Strongman | 0–200 | Enemies surrender easier, +30% Heat gen | Lose origin advantages |
| **Clout** | Celebrity | 0–300 | Media protection, viral bonuses | Lose star power |
| **Faith** | Religious Leader | 0–150 | +30% card effects, congregation unwavering | Followers question you |
| **Influence** | Nepo Baby | 0–200 | Full dynasty support, access all resources | Family withdraws support |

**Spending secondary resources:**

| Resource | 20 pts | 35–40 pts | 50–60 pts | 75–80 pts | 100+ pts |
|----------|--------|-----------|-----------|-----------|----------|
| Fear | Skip opponent turn | Instant −30% Resolve | Block one scandal | Auto-win battle (1×/run) | — |
| Clout | Boost card +50% | Cancel Heat gain | Convert to Support (1:100) | — | Scandal recovery (1×/run) |
| Faith | Reduce −20H | — | Convert opponent | Survive scandal | Perform "miracle" |
| Influence | Auto-win battle | Remove all Heat (1×/run) | Inherit legendary card | Emergency family meeting | — |

### Resource Conversions

| From | To | Rate | Side Effect |
|------|----|------|-------------|
| ₱300 | 1L | 1:1 | +5H |
| ₱1 | 2 Support | via Helicopter Money | +25H (ostentatious) |
| 1 Clout | 100 Support | Celebrity only | — |
| Support | ₱ | Fundraiser: 10% ratio | — |
| 1U (at end) | 50 Support | Passive end-game | — |
| 1H above 50 | −1 Support | Election day penalty | — |

---

## 4. Card System

### Card Types

All cards belong to one of three types, inspired by Griftlands' negotiation system:

#### Diplomacy (Green)
Peaceful persuasion. Sustainable damage, builds Composure, creates allies.
- Low/no Hostility generation
- Examples: *Find Common Ground* (3 damage), *Blessing* (burst via Composure), *Family Name*

#### Hostility (Red)
Aggressive pressure. Higher damage but builds Hostility — a self-inflicted debuff that makes the opponent hit harder.
- Risk/reward: hit harder, take more damage
- Examples: *Accusation* (4 damage, +1H), *Bold Accusation* (5 damage, +2H), *Spotlight Hog* (6 damage, +3 Composure, +2H)

#### Manipulate (Purple)
Utility cards for resource advantage, card draw, and tactical plays.
- No direct damage — enables combos and big turns
- Examples: *Call in Favor* (draw 2), *Gather Thoughts* (+4 Composure), *Trust Fund* (+2 Composure, +1 AP)

### Card Rarity

| Rarity | Visual | Description |
|--------|--------|-------------|
| Common | White/Gray | Foundation cards, lower power, reliable |
| Uncommon | Green | Enhanced effects, situational power |
| Rare | Blue | Powerful, often origin-specific |
| Legendary | Gold | Game-changing, very rare, high risk/reward |

### Battle Costs

In battle, **cards only cost Action Points (AP)**. Meta resources (₱, Heat, Influence) are campaign-layer only.

| Cost | Type |
|------|------|
| 0 AP | Free — always playable, usually with downside |
| 1 AP | Standard — 3–4 damage, draw 2, +3 Composure |
| 2 AP | Strong — 6+ damage, draw 3+, multi-effect |
| 3 AP | Rare — game-changing effect |

### Starter Decks (10 cards each)

#### Faith Leader — "The Peacemaker"
**Passive:** "Divine Grace" — Start battle with +1 card draw (6 instead of 5)
**Composition:** 6 Diplomacy, 2 Hostility, 2 Manipulate
**Playstyle:** Build Composure → burst with Blessing

| # | Card | Cost | Effect |
|---|------|------|--------|
| ×4 | Find Common Ground | 1 AP | Deal 3 Resolve damage |
| ×2 | Blessing | 1 AP | Deal damage = Composure, consume all Composure |
| ×2 | Accusation | 1 AP | Deal 4 damage, gain 1 Hostility |
| ×1 | Deflect | 1 AP | Gain 3 Composure, reduce Hostility by 1 |
| ×1 | Gather Thoughts | 1 AP | Gain 4 Composure |

#### Nepo Baby — "The Operator"
**Passive:** "Family Connections" — Start with 4 AP instead of 3
**Composition:** 3 Diplomacy, 2 Hostility, 5 Manipulate
**Playstyle:** Card/AP advantage engine

| # | Card | Cost | Effect |
|---|------|------|--------|
| ×2 | Family Name | 1 AP | Deal 3 Resolve damage |
| ×1 | Inherited Privilege | 2 AP | Deal 5 damage, draw 1 card |
| ×2 | Pull Strings | 1 AP | Deal 4 damage, gain 1 Hostility |
| ×2 | Call in Favor | 1 AP | Draw 2 cards |
| ×1 | Backroom Deal | 2 AP | Draw 2 cards, gain 1 AP next turn |
| ×1 | Dynasty Network | 1 AP | Discard 1, draw 2 |
| ×1 | Trust Fund | 0 AP | Gain 2 Composure, gain 1 AP this turn |

#### Actor (Celebrity) — "The Risk Taker"
**Passive:** "Stage Presence" — First card each turn costs 1 less AP
**Composition:** 3 Diplomacy, 4 Hostility, 3 Manipulate
**Playstyle:** High Hostility → convert to Composure via Ego Trip

| # | Card | Cost | Effect |
|---|------|------|--------|
| ×2 | Charming Gambit | 1 AP | Deal 3 damage, 50% chance: draw 1 card |
| ×1 | All or Nothing | 2 AP | Deal 3–9 damage (random) |
| ×2 | Bold Accusation | 1 AP | Deal 5 damage, gain 2 Hostility |
| ×2 | Spotlight Hog | 2 AP | Deal 6 damage, +3 Composure, +2 Hostility |
| ×1 | High Stakes | 0 AP | Discard hand, draw 3 cards |
| ×1 | Ego Trip | 1 AP | Gain Composure = Hostility (don't reduce Hostility) |
| ×1 | Fan Favorite | 1 AP | Lose 3 Composure, reduce Hostility by 3 |

### Card Acquisition (During Runs)

- **Battle Rewards** — Win negotiations: choose 1 of 3 random cards
- **Ally Cards** — Allies give their signature card to your deck
- **Shop Purchases** — Buy with ₱ at location shops
- **Event Rewards** — Special cards from story events
- **Quest Completion** — Unique cards from NPC questlines
- **Card Removal** — Rest sites let you remove 1 card per rest (alternate: upgrade 1 card)

### Upgrade System

Every card has a **"+" version** with enhanced effects:

| Original | Upgraded |
|----------|---------|
| Find Common Ground: 3 damage | Find Common Ground+: 5 damage |
| Accusation: 4 damage, 1 Hostility | Accusation+: 6 damage, 1 Hostility |
| Blessing: deal Composure → consume | Blessing+: deal Composure → don't consume |

**Upgrade opportunities:** Rest sites (free), Shops (cost ₱)

### Deck Size
- **Starting:** 10 cards
- **Minimum:** 10 cards
- **Maximum:** 30 cards
- **Optimal:** 12–18 cards (consistency vs. variety)

---

## 5. Battle System

Battles represent **political negotiations and debates** — you must break your opponent's will through Diplomacy, Hostility, or Manipulation.

### Three Battle Resources

#### Resolve (The "HP")
- Both combatants start with ~20 Resolve
- Reduce opponent's Resolve to 0 = Victory
- Your Resolve reaches 0 = Defeat
- Damaged by cards, healed by certain cards

#### Composure (Offensive Buff)
- You build Composure stacks with cards
- Each stack adds **+1 damage** to your next attack
- Stacks consumed when dealing damage
- Faith Leader specialty: Spend ALL Composure for burst damage (Blessing)
- Actor specialty: Convert Hostility → Composure (Ego Trip)

#### Hostility (Self-Inflicted Debuff)
- Gained from playing red (Hostility) cards
- **Formula:** Incoming damage × (1 + Hostility × 0.5)
- Example: 3 Hostility = opponent deals 2.5× damage to you
- Risk/reward: hit harder, take much more damage
- Can be reduced with Manipulate cards (Fan Favorite, Deflect)

### Battle Turn Flow

```
START OF BATTLE
  └─ Initialize both combatants (origin-specific stats)
  └─ Draw initial hands (Player: 5 cards, Faith Leader: 6)
  └─ Apply origin passives

EACH TURN:
  TurnStart
    ├─ Switch active player (Player ↔ Opponent)
    ├─ Refresh AP (3 default, 4 for Nepo Baby)
    ├─ Draw 1 card
    ├─ Apply Actor passive (first card −1 AP)
    └─ Publish TurnStartedEvent

  PlayerTurn (or OpponentTurn)
    ├─ Play cards (consume AP, resolve effects)
    └─ End Turn (publish EndTurnRequestedEvent)

  TurnEnd
    ├─ Apply Hostility damage to player
    ├─ Clear turn-based buffs
    ├─ Check victory conditions
    └─ Loop or proceed to BattleEnd

END OF BATTLE
  ├─ Low Hostility used → Diplomatic victory → Opponent becomes ALLY
  └─ High Hostility used → Aggressive victory → Opponent becomes ENEMY
                           (Better rewards, more ₱/Influence, +Heat)
```

### Playing a Card
1. Pay Action Point cost
2. Resolve damage (base + Composure stacks consumed)
3. Apply secondary effects (Composure gain, Hostility gain, card draw)
4. Move card to Discard pile
5. Update UI via EventBus

### Status Effects Reference

**Debuffs:**
| Effect | Description |
|--------|-------------|
| Weakened | Target deals X less damage (decreases per turn) |
| Vulnerable | Target takes 50% more damage |
| Exposed | Next attack deals double damage (removed on trigger) |
| Frail | Target gains 25% less Composure |
| Entangled | Target's cards cost +1 AP |
| Scandal | Target takes X damage at end of each turn |
| Confused | Random card costs +1 AP each turn |
| Silenced | Cannot play Manipulate cards |

**Buffs:**
| Effect | Description |
|--------|-------------|
| Strength | Deal X more damage |
| Plated | Reduce incoming damage by X |
| Intangible | Take only 1 damage from attacks |
| Thorns | Deal X damage back when attacked |
| Dexterity | Gain X more Composure per card |
| Focus | Cards cost X less AP (this turn only) |
| Regeneration | Heal X Resolve at end of turn |
| Energized | Draw X extra cards next turn |
| Ritual | Gain X Composure at start of turn |
| Momentum | Gain X damage per card played this turn |
| Echo | Next card is played twice |

### Combo Examples

**Faith Leader — Build & Burst:**
```
Turn 1: Gather Thoughts     → +4 Composure
Turn 2: Deflect             → +3 Composure (total 7), −1 Hostility
Turn 3: Blessing            → Deal 7 damage, consume all Composure
```

**Actor — Hostility Conversion:**
```
Turn 1: Bold Accusation     → 5 damage, +2 Hostility
Turn 2: Spotlight Hog       → 6 damage, +3 Composure, +2 Hostility (4 total)
Turn 3: Ego Trip            → Gain 4 Composure from Hostility (now 7 Composure)
Turn 4: Any attack          → +7 bonus damage from Composure
```

**Nepo Baby — Card Engine:**
```
Turn 1: Call in Favor       → Draw 2
Turn 2: Dynasty Network     → Discard 1, Draw 2
Turn 3: Backroom Deal       → Draw 2, +1 AP next turn
Result: Drew 6 extra cards, bank +1 AP for next turn
```

### Ally / Enemy System

Every battle victory creates a lasting relationship:

| Outcome | Condition | What Happens |
|---------|-----------|--------------|
| **Ally** | Low Hostility used | Opponent gives you their signature card, provides passive bonuses in future battles |
| **Enemy** | High Hostility used | Opponent opposes you in future battles, adds Arguments to opponent's side, blocks map paths, generates Heat |

**Strategic consideration:**
- Early game: Create allies (build deck, get bonuses)
- Late game: Can afford enemies (strong deck, need bigger rewards)
- Actor handles high Hostility better than other origins

### Opponent Types

| Type | Battle Style | Win Reward |
|------|-------------|------------|
| Rival Politicians | Full card decks, represent factions | Gain their support base |
| Journalists | "Investigation battles," exposé mechanics | Prevent story publication |
| Community Leaders | Persuasion battles, Charm-focused | Utang na Loob + Support |
| Fixers/Operators | Negotiation for resources, Leverage-heavy | Access to special services |

---

## 6. Origins (Classes)

Four playable character classes, each with unique starting stats, secondary resource, signature cards, perks, and drawbacks.

### Difficulty Ranking
1. **Nepo Baby** ⭐ (Easy) — Money solves most problems
2. **Celebrity** ⭐⭐ (Medium) — Strong early, fragile to scandals
3. **Religious Leader** ⭐⭐⭐ (Medium-Hard) — Restricted but loyal
4. **Strongman** ⭐⭐⭐⭐ (Hard) — High Heat, constant journalist pressure

---

### THE STRONGMAN ("Mano Dura")
*"Discipline. Order. Fear."* — Former police/military officer.

**Starting Stats:** ₱1,000 | 3L | 0U | 30H | 500 Support

**Secondary Resource: Fear (0–200)**
- High Fear (75–100): Enemies surrender easier, +30% Heat generation
- Low Fear (0–39): Lose all origin advantages

**Key Perks:**
- Attack cards deal 20% more Confidence damage
- Accumulate Heat 30% slower from violent actions
- Base Support never drops below 500 (even during scandals)
- Once per run: call in "security forces" (auto-win battle, +40H)

**Key Drawbacks:**
- Charm cards 25% less effective
- Churches distrust you (−2U on entry)
- Journalists MORE aggressive (+1 encounter frequency)
- International community events give negative modifiers

**Signature Cards:**

| Card | Cost | Effect |
|------|------|--------|
| Nanlaban | 50 Fear | Permanently remove one enemy card from battle; +50H, international condemnation |
| Ride-in-Tandem | 30 Fear | Opponent skips next turn; +30H, journalist risk |
| Human Rights? More Like Human Wrongs! | 10 Fear | Deflect criticism (only works vs specific demographics) |

**Best locations:** Police Station (exclusive), Veterans' Hall (exclusive), Barangay Halls, Cockpit Arena

---

### THE CELEBRITY ("Artista")
*"They know my face. They'll vote for my smile."* — Actor/influencer leveraging fame.

**Starting Stats:** ₱3,000 | 0L | 5U | 10H | 1,500 Support

**Secondary Resource: Clout (0–300)**
- Earn by: Charm victories (+10), viral events (+20–50), media appearances (+25)
- Spend: Boost card (+50%), cancel Heat gain, convert to Support (1:100)

**Key Perks:**
- All Charm cards 40% more effective
- Support gain x2 from social media events
- Heat accumulates 20% slower
- Every 5 days: automatic random media event (usually positive)

**Key Drawbacks:**
- Attack cards 30% less effective
- Academics dismiss you (−3U at universities)
- Scandals hit 2× harder (fallen idol effect)
- Certain immoral choices are locked

**Signature Cards:**

| Card | Cost | Effect |
|------|------|--------|
| Iyak Mo 'Yan | 20 Clout | Dismiss criticism, gain Support from fans; double damage vs journalists |
| Asawa Ko, Ikaw? | 15 Clout | Deflect attack by creating romantic drama distraction |
| Comeback Movie | 80 Clout | Stage massive PR comeback (+2,000 Support); once per run |
| Woke Tweet | 30 Clout | Appeal to youth/progressive voters; backfires if Heat is high |

**Best locations:** TV Studio (exclusive), Fashion Show/Gala (exclusive), SM-style Malls, Basketball Courts

---

### THE RELIGIOUS LEADER ("Anointed One")
*"God has chosen me to lead His flock."* — Preacher using spiritual authority for political power.

**Starting Stats:** ₱500 | 0L | 10U | 15H | 800 Support

**Secondary Resource: Faith (0–150)**
- Earn by: Sermon victories (+15), prayer services (+20), keeping promises (+25)
- Spend: Reduce Heat (30 Faith = −20H), convert opponent (50 Faith), survive scandal (70 Faith — consumes ALL)

**Key Perks:**
- All Charm cards also generate +2U
- Defense cards 30% more effective
- Automatic +100 Support per day from tithes
- Faith Shield: First scandal forgiven automatically (−50H)

**Key Drawbacks:**
- **CANNOT use Lagay** — attempting it = instant +80H
- Certain immoral cards locked
- Scandal Heat penalty TRIPLED (fallen preacher effect)
- Secular/progressive NPCs heavily distrust you

**Signature Cards:**

| Card | Cost | Effect |
|------|------|--------|
| Seed Faith Donation | 20 Faith | Followers donate ₱500–2,000; +5H per use |
| Excommunicate | 40 Faith | Spiritually condemn opponent; double damage in churches |
| Prosperity Gospel | 50 Faith | Convert ALL ₱ to Support (1:1); +20H |
| Speaking in Tongues | 60 Faith | Shuffle all decks, random beneficial effect; unpredictable |
| Holy Water Blessing | 25 Faith | Immune to next Attack card |

**Best locations:** Megachurch HQ (exclusive), Prayer Mountain (exclusive), All Churches, Rural Barangays

---

### THE NEPO BABY ("Anak ng...")
*"It's my turn. My family built this province."* — Child of a political dynasty.

**Starting Stats:** ₱5,000 | 10L | −5U | 40H | 1,000 Support

**Secondary Resource: Influence (0–200)**
- Starts with 100 Influence; Earn from family connections (+10/use)
- Lose from public failures (−20), scandals (−30), breaking traditions (−30)

**Key Perks:**
- Start with 3 random locations already "owned" (guaranteed Support)
- All shop prices reduced 30%
- Call in dynasty favors 3× per run (auto-solve problems)
- Untouchable: First 50H doesn't trigger journalist encounters

**Key Drawbacks:**
- Starts with negative Utang na Loob (public resentment)
- "Eat the Rich" protests can randomly trigger
- All scandals +50% Heat gain (privilege amplifier)
- Cannot earn Utang na Loob naturally (capped at 0 until exceptional actions)

**Signature Cards:**

| Card | Cost | Effect |
|------|------|--------|
| Do You Know Who I Am? | 15 Influence | Intimidate via connections; scales with current ₱ |
| Helicopter Money | ₱2,000 | Convert ₱ to Support (1:2 ratio); +25H |
| Political Butterfly | 50 Influence | Change party affiliation mid-run, access new deck archetype |
| Assassinate Rival | 50 Lagay | Permanently remove opponent; +100H, murder investigation |
| Martyr Complex | 60 Influence | Convert Heat to Support if scandal hits; can backfire |
| Bobo Pero Mayaman | 30 Influence | Lean into incompetence — becomes endearing meme, gain Support |

**Best locations:** Family Compound (exclusive), Country Club (exclusive), Office Buildings, Haciendas

---

### Origin Synergies & Rivalries

**Natural Alliances:**
- Strongman + Religious Leader: Traditional values coalition
- Celebrity + Nepo Baby: Modern elite (compete for same demographics)

**Natural Rivalries:**
- Strongman vs Celebrity: "Tough guy" vs "Pretty face" narrative
- Religious Leader vs Nepo Baby: "Holy" vs "Unholy privilege"

### Origin Variants (Unlock Through Achievements)

| Variant | Cost (PC) | How to Unlock | Key Change |
|---------|-----------|---------------|------------|
| Reformed Strongman | 150 PC | Win as Strongman with <30H | Starting Heat 15, can use Charm; Fear gen −50% |
| Method Actor | 150 PC | Win as Celebrity without using Clout for scandals | Attack cards +20%; Star Power reduced |
| Fallen Prophet | 150 PC | Win as Religious Leader after surviving scandal | Can use Lagay (5H only); Charm −20% |
| Self-Made Heir | 150 PC | Win as Nepo Baby with net positive U | Can earn U naturally; Dynasty perks −50% |

---

## 7. The Campaign Layer

### Map Navigation

The campaign unfolds on a procedurally generated map similar to Slay the Spire — nodes represent locations where you can battle, rest, shop, or encounter events. Travel costs days.

**Node Types:**
- **Battle** — Card negotiation vs an NPC (rival, journalist, fixer, etc.)
- **Event** — Random or scripted story encounter (choice-based)
- **Shop** — Spend ₱ on cards, upgrades, Heat reduction
- **Rest** — Reduce Heat by 10, shuffle deck, restore Confidence

### Locations

Locations are grouped into four types:

| Type | Examples | Typical Effect |
|------|---------|----------------|
| **Community** | Barangay Halls, Churches, Carinderias | Utang na Loob, Support from locals |
| **Social** | Malls, Basketball Courts, Karaoke Bars | Celebrity appeal, Clout, broad Support |
| **Institutional** | Police Station, COMELEC, Banks | Strongman/Nepo perks, legal protection |
| **Economic** | Markets, Haciendas, Ports, Offices | ₱, Lagay opportunities, business networks |

**Origin-Exclusive Locations:**
- Strongman: Police Station, Veterans' Hall
- Celebrity: TV Studio, Fashion Show/Gala
- Religious Leader: Megachurch HQ, Prayer Mountain
- Nepo Baby: Family Compound, Country Club

**Location Ownership:** Winning battles or completing quests in a location lets you "own" it — generating passive Support each day. Nepo Baby starts with 3 owned locations.

### Regional Factions

Different regions respond to origins differently:

| Region | Likes | Dislikes | Key Locations |
|--------|-------|---------|---------------|
| Tagalog Urban Elite | Education, progressive policies | Strongman tactics, corruption | Malls, universities, offices |
| Visayan Business Class | Pragmatism, economic growth | Excessive spending, instability | Markets, ports, trade centers |
| Mindanao Regional Bloc | Autonomy, regional respect | Imperial Manila attitudes | Farms, community halls |
| Rural Agricultural | Tradition, patron-client relationships | Elitism, broken promises | Haciendas, barangay halls, churches |

### NPC Relationship Web (Palakasan System)

Every NPC exists in a web of relationships:

| Relationship Type | Strength | Examples |
|-------------------|----------|---------|
| Family/Clan | Strongest | Blood ties, dynasty networks |
| Compadre | Strong | Godparent relationships |
| Fraternity | Medium | Brotherhood/sorority ties |
| Business | Medium | Economic partnerships |
| Political | Variable | Party affiliations |

**Consequences:** Helping one NPC may anger their enemies. Betraying one affects their entire network. Building Utang na Loob with key figures unlocks access to their entire group.

### Events

**Milestone Events (Scripted):**
- TV Debates (every 7 days) — major Support swings
- COMELEC Filing Deadline (Day 30) — 5,000 Support minimum
- Election Day (Day 45) — final tally

**Random Events (Evening Roll):**
- Heat-triggered scandal system
- Utang na Loob relationship events
- Absurdist comedy events (Troll farms, viral dances, faith healers)
- Weather and opponent AI actions
- Project-based events with corruption mechanics

**Heat-Triggered Escalation:**
- 51–75H: 20% chance/day of scandal event
- 76–99H: Assassination risk at dangerous locations
- 100H: SCANDAL EXPLOSION (devastating, may be survivable)

### Economic Loops

**Project Kickbacks (key decision point):**
| Option | ₱ Gain | Support | Heat | Notes |
|--------|--------|---------|------|-------|
| Clean | +₱200 | +1,000 | 0H | Safe |
| 20% Kickback | +₱800 | +800 | +15H | Moderate risk |
| 50% Kickback | +₱2,000 | +400 | +35H | Gain Lagay too |

**Campaign Finance Loop:**
```
Earn Support → Convert some to ₱ (Fundraiser) → Buy cards
→ Win battles with better deck → Earn more Support → Repeat
```

**Corruption Spiral Warning:**
```
Use Lagay → Get advantage → Gain Heat → Need more Lagay to fix Heat
→ More Lagay = More Heat → Scandal at 100H
```

---

## 8. Meta Progression

### Political Capital (PC) — Meta Currency

PC persists across runs and is spent on permanent unlocks.

**Earning PC:**

| Source | Amount |
|--------|--------|
| Squeaky Clean Champion victory | 500 PC |
| Necessary Evil Victor | 300 PC |
| Trapo Triumphant | 200 PC |
| Pyrrhic Victory | 150 PC |
| Failed run (reached Day 45) | 150 PC |
| Failed run (reached Day 30) | 100 PC |
| Failed run (reached Day 15) | 75 PC |
| Failed run (before Day 15) | 50 PC |

Achievements add 25–500 PC each. Total available across all achievements: ~3,000+ PC.

**Spending PC (Selected highlights):**

| Unlock | Cost | Effect |
|--------|------|--------|
| Origin variants | 150 PC each | Modified playthroughs of existing origins |
| New Origins (Technocrat, Activist, Warlord) | 300–500 PC | New full origins |
| Legacy Perks (starting bonuses, combat boosts) | 50–250 PC | Permanent advantages |
| Legacy Cards (added to card pool) | 25–200 PC | New cards available per run |
| Shortcut/QoL Upgrades | 50–100 PC | Map shortcuts, event rerolls |

### Achievement System

80+ achievements across categories:
- **Campaign:** Win with each origin, reach all 4 endings, speed runs
- **Challenge:** Win with <20H total, never use Lagay, 80+ Utang na Loob
- **Collection:** Cards, locations, NPC questlines
- **Combat:** Flawless victories, specific combo chains, pacifist runs
- **Resource:** Wealth milestones, Heat survival, corruption depths
- **Origin-Specific:** Max Fear/Clout/Faith/Influence, exclusive feats

### Difficulty Modifiers (Post-First Victory)

**Easy (−50% PC):** Safety Net (100H threshold), Popular Mandate (+2000 Start Support), Media Darling (−50% Heat accumulation)

**Hard (+50% PC):** Hostile Media (+50% Heat), Strong Opposition, Economic Crisis (+50% all prices), Short Campaign (35 days)

**Extreme (×2 PC):** Perfect Run (no Lagay), Speed Demon (25 days), Maximum Heat Start (start at 60H)

### New Game+ Modes

| Mode | Requirement | Effect |
|------|------------|--------|
| NG+ | First victory | Carry legacy perks; stronger opponents; +50% PC |
| NG++ | 5 victories | Elite opponents; origin questlines; hardest difficulty; +100% PC |
| Endless | — | Campaign continues past Day 45; hold office; survival mode |

### Prestige System

After 100% completion: Reset all unlocks (keep achievements), gain Prestige Level, unlock cosmetics (card backs, location themes, portraits, UI skins). Prestige bonuses: +10% PC per level, up to +100% at Prestige 10.

---

## 9. Design Pillars

### Balancing Philosophy

**The Lagay Loop:** Bribery is always available but always escalating. Every Lagay use generates Heat, which requires more resources to suppress, which may require more Lagay. The game is designed so corruption is viable but never "safe."

**Ally vs Enemy:** The battle system's diplomacy/hostility outcome creates a persistent consequence graph. Being aggressive early creates enemies who appear in later battles. Being diplomatic creates allies who strengthen your deck. This choice should feel meaningful throughout the entire run.

**Heat as Pressure:** Heat is the primary meta-tension. Every corrupt or aggressive action raises it. The game escalates from 0 to 100 — at various thresholds, different consequences activate. Players should always feel Heat creeping and need to manage it deliberately.

**Origin Identity:** Each origin should feel fundamentally different to play. Strongman is a Heat-management puzzle (start high, stay below 100). Celebrity is a clout-farming game. Religious Leader is a restrictive challenge run (no Lagay, triple scandal penalty). Nepo Baby is an economic efficiency puzzle starting with advantages and social liability.

### Tone

The game is **absurdist satire** — the mechanics exaggerate real political dysfunction for comedic effect. Cards have dark humor names. Events treat serious topics (political violence, corruption, poverty) with satirical distance. The goal is for players to recognize real patterns while laughing at the absurdity.

All content is fictional. No real-world politicians are depicted.

---

*Crookedile — Version 0.1 — Design Phase*

*For implementation details, architecture decisions, and current build status, see [SYSTEMS_STUDY.md](SYSTEMS_STUDY.md).*
