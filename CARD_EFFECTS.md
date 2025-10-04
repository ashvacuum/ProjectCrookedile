# Card Effect Library

Complete reference of all battle effects that cards can perform in Crookedile. Use this as a design guide when creating new cards.

[← Home](README.md) | [Cards](cards.md) | [Battle System](BATTLE_SYSTEM.md)

---

## Table of Contents

- [Core Damage & Healing](#core-damage--healing)
- [Composure System](#composure-system)
- [Hostility System](#hostility-system)
- [Action Resources](#action-resources)
- [Status Effects - Debuffs](#status-effects---debuffs)
- [Status Effects - Buffs](#status-effects---buffs)
- [Status Effects - Special](#status-effects---special)
- [Card Manipulation](#card-manipulation)
- [Effect Combinations](#effect-combinations)
- [Design Guidelines](#design-guidelines)

---

## Core Damage & Healing

### Resolve Damage
| Effect | Description | Usage Example |
|--------|-------------|---------------|
| **ResolveDamage** | Deal X Resolve damage to target | Find Common Ground: 3 damage |
| **RandomDamage** | Deal X-Y random Resolve damage | All or Nothing: 3-9 damage |
| **ResolveDamageEqualToComposure** | Deal damage = current Composure | Blessing: Deal all Composure as damage |

### Resolve Healing
| Effect | Description | Usage Example |
|--------|-------------|---------------|
| **ResolveHeal** | Restore X Resolve to self | Meditation: Restore 5 Resolve |

---

## Composure System

**Composure** is an offensive buff - each stack adds +1 damage to attacks.

### Composure Gain
| Effect | Description | Usage Example |
|--------|-------------|---------------|
| **GainComposure** | Gain X Composure stacks | Gather Thoughts: +4 Composure |
| **ComposureEqualToHostility** | Gain Composure = current Hostility | Ego Trip: Convert all Hostility to Composure |

### Composure Loss
| Effect | Description | Usage Example |
|--------|-------------|---------------|
| **LoseComposure** | Lose X Composure stacks | Fan Favorite: Lose 3 Composure |
| **ConsumeAllComposure** | Remove all Composure | Blessing: Consume all (paired with damage) |

---

## Hostility System

**Hostility** is a self-inflicted debuff - opponent deals more damage based on formula: `1 + (Hostility × 0.5)`

### Hostility Gain
| Effect | Description | Usage Example |
|--------|-------------|---------------|
| **GainHostility** | Gain X Hostility | Accusation: +1 Hostility |

### Hostility Reduction
| Effect | Description | Usage Example |
|--------|-------------|---------------|
| **ReduceHostility** | Reduce X Hostility stacks | Fan Favorite: Reduce 3 Hostility |

---

## Action Resources

### Action Points
| Effect | Description | Usage Example |
|--------|-------------|---------------|
| **GainActionPoints** | Gain X AP this turn | Trust Fund: +1 AP this turn |
| **GainActionPointsNextTurn** | Gain X AP next turn | Backroom Deal: +1 AP next turn |

### Card Draw/Discard
| Effect | Description | Usage Example |
|--------|-------------|---------------|
| **DrawCards** | Draw X cards from deck | Call in Favor: Draw 2 cards |
| **DiscardCards** | Discard X cards from hand | Dynasty Network: Discard 1 |

---

## Status Effects - Debuffs

### Damage Modifiers
| Effect | Duration | Description | Balance Value |
|--------|----------|-------------|---------------|
| **ApplyWeakened** | DecreasePerTurn | Target deals X less damage | 2-3 stacks |
| **ApplyVulnerable** | DecreasePerTurn | Target takes 50% more damage | 1-2 stacks |
| **ApplyExposed** | RemoveEndOfTurn | Next attack deals double damage | 1 stack only |

### Resource Modifiers
| Effect | Duration | Description | Balance Value |
|--------|----------|-------------|---------------|
| **ApplyFrail** | DecreasePerTurn | Target gains 25% less Composure | 2-3 stacks |
| **ApplyEntangled** | DecreasePerTurn | Target's cards cost +1 AP | 1-2 stacks |

### Damage Over Time
| Effect | Duration | Description | Balance Value |
|--------|----------|-------------|---------------|
| **ApplyScandal** | DecreasePerTurn | Target takes X damage at end of turn | 3-5 damage/turn |

### Special Debuffs
| Effect | Duration | Description | Balance Value |
|--------|----------|-------------|---------------|
| **ApplyConfused** | DecreasePerTurn | Random card costs +1 AP each turn | 2-3 turns |
| **ApplySilenced** | DecreasePerTurn | Cannot play Manipulate cards | 1-2 turns |

---

## Status Effects - Buffs

### Damage Modifiers
| Effect | Duration | Description | Balance Value |
|--------|----------|-------------|---------------|
| **ApplyStrength** | DecreasePerTurn | Deal X more damage | 2-4 stacks |
| **ApplyPlated** | DecreasePerTurn | Reduce incoming damage by X | 3-5 armor |
| **ApplyIntangible** | DecreasePerTurn | Take only 1 damage from attacks | 1-2 stacks |
| **ApplyThorns** | DecreasePerTurn | Deal X damage back when attacked | 2-3 damage |

### Resource Modifiers
| Effect | Duration | Description | Balance Value |
|--------|----------|-------------|---------------|
| **ApplyDexterity** | DecreasePerTurn | Gain X more Composure per card | 1-2 stacks |
| **ApplyFocus** | RemoveEndOfTurn | Cards cost X less AP (this turn only) | 1-2 AP reduction |

### Regeneration
| Effect | Duration | Description | Balance Value |
|--------|----------|-------------|---------------|
| **ApplyRegeneration** | DecreasePerTurn | Heal X Resolve at end of turn | 2-3 heal/turn |

### Special Buffs
| Effect | Duration | Description | Balance Value |
|--------|----------|-------------|---------------|
| **ApplyEnergized** | RemoveEndOfTurn | Draw X extra cards next turn | 1-2 cards |
| **ApplyBlock** | RemoveEndOfTurn | Temporary damage reduction | 5-8 block |

---

## Status Effects - Special

### Turn-Based Effects
| Effect | Duration | Description | Balance Value |
|--------|----------|-------------|---------------|
| **ApplyRitual** | DecreasePerTurn | Gain X Composure at start of turn | 2-3 Composure/turn |
| **ApplyMomentum** | RemoveEndOfTurn | Gain X damage per card played this turn | 1-2 damage/card |

### Card Manipulation
| Effect | Duration | Description | Balance Value |
|--------|----------|-------------|---------------|
| **ApplyEcho** | RemoveEndOfTurn | Next card is played twice | 1 stack only |

---

## Card Manipulation

### Exhaust
| Effect | Description | Usage Example |
|--------|-------------|---------------|
| **ExhaustCard** | Remove this card from deck until end of battle | Desperation: Powerful effect, then exhaust |

---

## Effect Combinations

### Composure Build → Burst (Faith Leader Strategy)
```
Turn 1: Gather Thoughts (+4 Composure)
Turn 2: Deflect (+3 Composure, total 7)
Turn 3: Blessing (Deal 7 damage, consume all Composure)
```

### Hostility → Composure Conversion (Actor Strategy)
```
Turn 1: Bold Accusation (5 damage, +2 Hostility)
Turn 2: Spotlight Hog (6 damage, +3 Composure, +2 Hostility = 4 total)
Turn 3: Ego Trip (Gain 4 Composure from Hostility, now 7 Composure + 4 Hostility)
```

### Card Advantage Engine (Nepo Baby Strategy)
```
Turn 1: Call in Favor (Draw 2)
Turn 2: Dynasty Network (Discard 1, Draw 2)
Turn 3: Backroom Deal (Draw 2, +1 AP next turn)
Result: Drew 6 cards, +1 AP next turn
```

---

## Design Guidelines

### Card Type Specializations

#### Diplomacy Cards (Green)
**Focus:** Sustainable damage, relationship building
**Effects:** ResolveDamage, GainComposure, ApplyBuff
**Hostility:** None or minimal

**Examples:**
- Find Common Ground: 3 damage
- Blessing: Deal Composure damage
- Deflect: +3 Composure, -1 Hostility

#### Hostility Cards (Red)
**Focus:** High burst damage, aggressive tactics
**Effects:** ResolveDamage (high), GainHostility, ApplyDebuff
**Risk/Reward:** More damage for more Hostility

**Examples:**
- Accusation: 4 damage, +1 Hostility
- Bold Accusation: 5 damage, +2 Hostility
- Spotlight Hog: 6 damage, +3 Composure, +2 Hostility

#### Manipulate Cards (Purple)
**Focus:** Resource advantage, utility
**Effects:** DrawCards, GainActionPoints, ApplyBuff/Debuff
**Strategy:** Enable combos and big turns

**Examples:**
- Call in Favor: Draw 2
- Backroom Deal: Draw 2, +1 AP next turn
- Trust Fund: +2 Composure, +1 AP this turn

---

## Balancing Guidelines

### Damage Values (per 1 AP)
- **Basic (Common):** 3-4 damage
- **Enhanced (Uncommon):** 5-6 damage
- **Rare:** 7+ damage or special mechanics

### Composure Values (per 1 AP)
- **Basic:** 3-4 Composure
- **Enhanced:** 5-6 Composure
- **Rare:** 7+ Composure or scaling effects

### Hostility Cost Conversion
- **+1 Hostility** ≈ **+2 damage value**
- Example: Accusation (4 damage, +1 Hostility) ≈ 6 damage worth of value

### Card Draw/AP Gain
- **Draw 1 card** ≈ **0.5-1 AP value**
- **+1 AP this turn** ≈ **1.5 AP value**
- **+1 AP next turn** ≈ **1 AP value**

### Status Effect Stacks
- **Debuffs (Weakened, Vulnerable):** 2-3 stacks per 1 AP
- **Buffs (Strength, Dexterity):** 2-3 stacks per 1 AP
- **Special (Intangible, Echo):** 1 stack per 2-3 AP

### Card Costs
- **0 AP:** Weak effect or downside (discard hand, lose Composure)
- **1 AP:** Standard effect (3-4 damage, draw 2, +3 Composure)
- **2 AP:** Strong effect (6+ damage, draw 3+, multiple effects)
- **3 AP:** Rare, game-changing effect

---

## Card Design Templates

### Basic Attack Card
```
Name: [Card Name]
Type: Diplomacy/Hostility/Manipulate
Cost: 1 AP
Effects:
  - ResolveDamage: 3 to Opponent
```

### Build & Burst Card (Faith Leader)
```
Name: Blessing
Type: Diplomacy
Cost: 1 AP
Effects:
  - ResolveDamageEqualToComposure: to Opponent
  - ConsumeAllComposure
```

### Risk/Reward Card (Actor)
```
Name: All or Nothing
Type: Hostility
Cost: 2 AP
Effects:
  - RandomDamage: 3-9 to Opponent
```

### Resource Engine Card (Nepo Baby)
```
Name: Backroom Deal
Type: Manipulate
Cost: 2 AP
Effects:
  - DrawCards: 2
  - GainActionPointsNextTurn: 1
```

### Status Effect Application
```
Name: Intimidate
Type: Hostility
Cost: 1 AP
Effects:
  - ApplyWeakened: 2 stacks to Opponent (DecreasePerTurn)
  - GainHostility: 1
```

---

**Navigation:** [← Cards](cards.md) | [Battle System](BATTLE_SYSTEM.md) | [Card Acquisition](CARD_ACQUISITION.md)
