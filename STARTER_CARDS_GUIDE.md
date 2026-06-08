> [!WARNING]
> **Pre-redesign document — may be inaccurate (flagged 2026-06).** Crookedile underwent a major combat + class redesign. Canonical design now lives in [`docs/core-design.md`](docs/core-design.md) and [`docs/crookedile-starter-decks.md`](docs/crookedile-starter-decks.md); current code/architecture is summarized in [`readme.md`](readme.md). Treat specifics below as historical until reconciled.

# Starter Cards Creation Guide

**Last Updated:** 2025-10-08

Complete guide to creating all 30 starter cards (10 per origin) as ScriptableObjects in Unity.

---

## 📋 Quick Reference: All Starter Cards

### Faith Leader - "The Peacemaker" (10 cards)
| # | Card Name | Cost | Type | Effect |
|---|-----------|------|------|--------|
| 1-4 | Find Common Ground | 1 AP | Diplomacy | Deal 3 Resolve damage |
| 5-6 | Blessing | 1 AP | Diplomacy | Deal damage = Composure, consume all Composure |
| 7-8 | Accusation | 1 AP | Hostility | Deal 4 damage, gain 1 Hostility |
| 9 | Deflect | 1 AP | Manipulate | Gain 3 Composure, reduce Hostility by 1 |
| 10 | Gather Thoughts | 1 AP | Manipulate | Gain 4 Composure |

### Nepo Baby - "The Operator" (10 cards)
| # | Card Name | Cost | Type | Effect |
|---|-----------|------|------|--------|
| 1-2 | Family Name | 1 AP | Diplomacy | Deal 3 Resolve damage |
| 3 | Inherited Privilege | 2 AP | Diplomacy | Deal 5 damage, draw 1 card |
| 4-5 | Pull Strings | 1 AP | Hostility | Deal 4 damage, gain 1 Hostility |
| 6-7 | Call in Favor | 1 AP | Manipulate | Draw 2 cards |
| 8 | Backroom Deal | 2 AP | Manipulate | Draw 2 cards, gain 1 AP next turn |
| 9 | Dynasty Network | 1 AP | Manipulate | Discard 1 card, draw 2 cards |
| 10 | Trust Fund | 0 AP | Manipulate | Gain 2 Composure, gain 1 AP this turn |

### Actor - "The Risk Taker" (10 cards)
| # | Card Name | Cost | Type | Effect |
|---|-----------|------|------|--------|
| 1-2 | Charming Gambit | 1 AP | Diplomacy | Deal 3 damage, 50% chance: draw 1 card |
| 3 | All or Nothing | 2 AP | Hostility | Deal 3-9 damage (random) |
| 4-5 | Bold Accusation | 1 AP | Hostility | Deal 5 damage, gain 2 Hostility |
| 6-7 | Spotlight Hog | 2 AP | Hostility | Deal 6 damage, gain 3 Composure, gain 2 Hostility |
| 8 | High Stakes | 0 AP | Manipulate | Discard hand, draw 3 cards |
| 9 | Ego Trip | 1 AP | Manipulate | Gain Composure = Hostility (don't reduce Hostility this turn) |
| 10 | Fan Favorite | 1 AP | Manipulate | Lose 3 Composure, reduce Hostility by 3 |

**Total:** 30 unique cards (some shared between origins)

---

## 🎨 Unity Setup

### Step 1: Create Folder Structure
```
Assets/
└── Data/
    └── Cards/
        └── Starter/
            ├── FaithLeader/
            ├── NepoBaby/
            └── Actor/
```

### Step 2: Create CardData ScriptableObjects

In Unity:
1. Right-click in Project window
2. `Create → Crookedile → Card Data` (if you have a menu)
3. **OR** manually: Right-click → `Create → ScriptableObject → CardData`

---

## 📝 Detailed Card Configurations

### Faith Leader Cards

#### 1. Find Common Ground (x4)
```
Name: Find Common Ground
Card Type: Diplomacy
Rarity: Common
Description: "Basic persuasion technique."

Costs:
  - Type: ActionPoints
  - Amount: 1

Effects:
  Effect 1:
    - Category: Damage
    - Target: Opponent
    - Damage Type: FixedDamage
    - Damage Amount: 3
```

#### 2. Blessing (x2)
```
Name: Blessing
Card Type: Diplomacy
Rarity: Common
Description: "Convert all Composure into a powerful burst of conviction."

Costs:
  - Type: ActionPoints
  - Amount: 1

Effects:
  Effect 1:
    - Category: Damage
    - Target: Opponent
    - Damage Type: DamageEqualToComposure

  Effect 2:
    - Category: Resource
    - Target: Self
    - Resource Type: ConsumeAllComposure
```

#### 3. Accusation (x2)
```
Name: Accusation
Card Type: Hostility
Rarity: Common
Description: "Direct confrontation. Creates tension."

Costs:
  - Type: ActionPoints
  - Amount: 1

Effects:
  Effect 1:
    - Category: Damage
    - Target: Opponent
    - Damage Type: FixedDamage
    - Damage Amount: 4

  Effect 2:
    - Category: Resource
    - Target: Self
    - Resource Type: GainHostility
    - Resource Amount: 1
```

#### 4. Deflect (x1)
```
Name: Deflect
Card Type: Manipulate
Rarity: Common
Description: "Redirect aggression into grace."

Costs:
  - Type: ActionPoints
  - Amount: 1

Effects:
  Effect 1:
    - Category: Resource
    - Target: Self
    - Resource Type: GainComposure
    - Resource Amount: 3

  Effect 2:
    - Category: Resource
    - Target: Self
    - Resource Type: ReduceHostility
    - Resource Amount: 1
```

#### 5. Gather Thoughts (x1)
```
Name: Gather Thoughts
Card Type: Manipulate
Rarity: Common
Description: "Center yourself and build inner strength."

Costs:
  - Type: ActionPoints
  - Amount: 1

Effects:
  Effect 1:
    - Category: Resource
    - Target: Self
    - Resource Type: GainComposure
    - Resource Amount: 4
```

---

### Nepo Baby Cards

#### 6. Family Name (x2)
```
Name: Family Name
Card Type: Diplomacy
Rarity: Common
Description: "Leverage your family's reputation."

Costs:
  - Type: ActionPoints
  - Amount: 1

Effects:
  Effect 1:
    - Category: Damage
    - Target: Opponent
    - Damage Type: FixedDamage
    - Damage Amount: 3
```

#### 7. Inherited Privilege (x1)
```
Name: Inherited Privilege
Card Type: Diplomacy
Rarity: Uncommon
Description: "Your advantages open doors."

Costs:
  - Type: ActionPoints
  - Amount: 2

Effects:
  Effect 1:
    - Category: Damage
    - Target: Opponent
    - Damage Type: FixedDamage
    - Damage Amount: 5

  Effect 2:
    - Category: CardManipulation
    - Target: Self
    - Card Manipulation Type: DrawCards
    - Card Amount: 1
```

#### 8. Pull Strings (x2)
```
Name: Pull Strings
Card Type: Hostility
Rarity: Common
Description: "Use connections to apply pressure."

Costs:
  - Type: ActionPoints
  - Amount: 1

Effects:
  Effect 1:
    - Category: Damage
    - Target: Opponent
    - Damage Type: FixedDamage
    - Damage Amount: 4

  Effect 2:
    - Category: Resource
    - Target: Self
    - Resource Type: GainHostility
    - Resource Amount: 1
```

#### 9. Call in Favor (x2)
```
Name: Call in Favor
Card Type: Manipulate
Rarity: Common
Description: "You know people."

Costs:
  - Type: ActionPoints
  - Amount: 1

Effects:
  Effect 1:
    - Category: CardManipulation
    - Target: Self
    - Card Manipulation Type: DrawCards
    - Card Amount: 2
```

#### 10. Backroom Deal (x1)
```
Name: Backroom Deal
Card Type: Manipulate
Rarity: Uncommon
Description: "Negotiate for future advantage."

Costs:
  - Type: ActionPoints
  - Amount: 2

Effects:
  Effect 1:
    - Category: CardManipulation
    - Target: Self
    - Card Manipulation Type: DrawCards
    - Card Amount: 2

  Effect 2:
    - Category: Resource
    - Target: Self
    - Resource Type: GainActionPointsNextTurn
    - Resource Amount: 1
```

#### 11. Dynasty Network (x1)
```
Name: Dynasty Network
Card Type: Manipulate
Rarity: Common
Description: "Cycle through your connections."

Costs:
  - Type: ActionPoints
  - Amount: 1

Effects:
  Effect 1:
    - Category: CardManipulation
    - Target: Self
    - Card Manipulation Type: DiscardCards
    - Card Amount: 1

  Effect 2:
    - Category: CardManipulation
    - Target: Self
    - Card Manipulation Type: DrawCards
    - Card Amount: 2
```

#### 12. Trust Fund (x1)
```
Name: Trust Fund
Card Type: Manipulate
Rarity: Common
Description: "Money solves problems instantly."

Costs:
  - Type: None (Free)

Effects:
  Effect 1:
    - Category: Resource
    - Target: Self
    - Resource Type: GainComposure
    - Resource Amount: 2

  Effect 2:
    - Category: Resource
    - Target: Self
    - Resource Type: GainActionPoints
    - Resource Amount: 1
```

---

### Actor Cards

#### 13. Charming Gambit (x2)
```
Name: Charming Gambit
Card Type: Diplomacy
Rarity: Common
Description: "Charisma with a chance of deeper connection."

Costs:
  - Type: ActionPoints
  - Amount: 1

Effects:
  Effect 1:
    - Category: Damage
    - Target: Opponent
    - Damage Type: FixedDamage
    - Damage Amount: 3

  Effect 2: (TODO: Conditional 50% chance - may need special implementation)
    - Category: CardManipulation
    - Target: Self
    - Card Manipulation Type: DrawCards
    - Card Amount: 1
    - Condition: 50% probability
```
**Note:** Conditional effects may need custom implementation. For now, could make it always draw 1.

#### 14. All or Nothing (x1)
```
Name: All or Nothing
Card Type: Hostility
Rarity: Uncommon
Description: "High risk, high reward aggression."

Costs:
  - Type: ActionPoints
  - Amount: 2

Effects:
  Effect 1:
    - Category: Damage
    - Target: Opponent
    - Damage Type: RandomDamage
    - Random Damage Min: 3
    - Random Damage Max: 9
```

#### 15. Bold Accusation (x2)
```
Name: Bold Accusation
Card Type: Hostility
Rarity: Common
Description: "Aggressive confrontation."

Costs:
  - Type: ActionPoints
  - Amount: 1

Effects:
  Effect 1:
    - Category: Damage
    - Target: Opponent
    - Damage Type: FixedDamage
    - Damage Amount: 5

  Effect 2:
    - Category: Resource
    - Target: Self
    - Resource Type: GainHostility
    - Resource Amount: 2
```

#### 16. Spotlight Hog (x2)
```
Name: Spotlight Hog
Card Type: Hostility
Rarity: Uncommon
Description: "All eyes on you - for better or worse."

Costs:
  - Type: ActionPoints
  - Amount: 2

Effects:
  Effect 1:
    - Category: Damage
    - Target: Opponent
    - Damage Type: FixedDamage
    - Damage Amount: 6

  Effect 2:
    - Category: Resource
    - Target: Self
    - Resource Type: GainComposure
    - Resource Amount: 3

  Effect 3:
    - Category: Resource
    - Target: Self
    - Resource Type: GainHostility
    - Resource Amount: 2
```

#### 17. High Stakes (x1)
```
Name: High Stakes
Card Type: Manipulate
Rarity: Rare
Description: "All in."

Costs:
  - Type: None (Free)

Effects:
  Effect 1: (TODO: "Discard hand" needs special implementation)
    - Category: CardManipulation
    - Target: Self
    - Card Manipulation Type: DiscardCards
    - Card Amount: 99 (or entire hand)

  Effect 2:
    - Category: CardManipulation
    - Target: Self
    - Card Manipulation Type: DrawCards
    - Card Amount: 3
```
**Note:** "Discard entire hand" may need custom handling.

#### 18. Ego Trip (x1)
```
Name: Ego Trip
Card Type: Manipulate
Rarity: Uncommon
Description: "Convert your bad reputation into confidence."

Costs:
  - Type: ActionPoints
  - Amount: 1

Effects:
  Effect 1:
    - Category: Resource
    - Target: Self
    - Resource Type: ComposureEqualToHostility
```
**Note:** Actor passive "don't reduce Hostility" may need special flag.

#### 19. Fan Favorite (x1)
```
Name: Fan Favorite
Card Type: Manipulate
Rarity: Common
Description: "Trade popularity for damage reduction."

Costs:
  - Type: ActionPoints
  - Amount: 1

Effects:
  Effect 1:
    - Category: Resource
    - Target: Self
    - Resource Type: LoseComposure
    - Resource Amount: 3

  Effect 2:
    - Category: Resource
    - Target: Self
    - Resource Type: ReduceHostility
    - Resource Amount: 3
```

---

## 🏗️ Creating Starter Deck ScriptableObjects

After creating all CardData ScriptableObjects, create deck container assets:

### Option A: List-Based Starter Decks

Create 3 ScriptableObjects that hold lists of CardData:

```csharp
// Create this as StarterDeckData.cs if needed
[CreateAssetMenu(fileName = "StarterDeck", menuName = "Crookedile/Starter Deck")]
public class StarterDeckData : ScriptableObject
{
    public OriginType origin;
    public List<CardData> cards;
}
```

Then create 3 assets:
- `FaithLeader_StarterDeck.asset` (10 cards)
- `NepoBaby_StarterDeck.asset` (10 cards)
- `Actor_StarterDeck.asset` (10 cards)

### Option B: Direct Reference in BattleSetup

In your BattleTestStarter script, manually drag all 10 cards for each origin into the inspector.

---

## 🎯 Quick Creation Checklist

For each card:
- [ ] Create CardData ScriptableObject
- [ ] Set Card Name
- [ ] Set Card Type (Diplomacy/Hostility/Manipulate)
- [ ] Set Rarity (Common/Uncommon/Rare)
- [ ] Set Description
- [ ] Add Cost (Action Points)
- [ ] Add all Effects with correct categories
- [ ] Verify effect values match table above

---

## 📦 Simplified Card Creation Order

### Start with Shared Cards (Used by Multiple Origins)
1. **Find Common Ground** (Faith Leader & shared concept)
2. **Accusation** (Faith Leader, similar to Pull Strings)

### Then Origin-Specific

**Faith Leader (5 unique cards):**
- Blessing
- Deflect
- Gather Thoughts
- Find Common Ground x4
- Accusation x2

**Nepo Baby (7 unique cards):**
- Family Name x2
- Inherited Privilege
- Pull Strings x2
- Call in Favor x2
- Backroom Deal
- Dynasty Network
- Trust Fund

**Actor (7 unique cards):**
- Charming Gambit x2
- All or Nothing
- Bold Accusation x2
- Spotlight Hog x2
- High Stakes
- Ego Trip
- Fan Favorite

---

## 🚀 Testing Your Cards

After creating cards:

1. Create StarterDeckData assets or add to BattleTestStarter
2. Assign to BattleSetup in scene
3. Run battle scene
4. Verify cards appear in hand
5. Test each card plays correctly
6. Check effects apply as expected

---

## 💡 Tips

- **Start simple:** Create 3 basic damage cards first, test, then add complexity
- **Copy/paste:** Duplicate similar cards and modify (speeds up creation)
- **Test incrementally:** Don't create all 30 at once - do 5, test, repeat
- **Use descriptions:** Copy exact text from tables above

---

## 🐛 Special Cases to Handle Later

Some cards have mechanics not fully implemented yet:

- **Charming Gambit:** 50% probability (may always draw for now)
- **High Stakes:** Discard entire hand (use high number like 99)
- **Ego Trip:** "Don't reduce Hostility" flag (may need special implementation)

These can start with simplified versions and be enhanced later.

