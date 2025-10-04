# Battle System Documentation

## Overview

The battle system is a **political negotiation card game** inspired by Griftlands, where players use Action Points to play cards that reduce opponent's Resolve, build Composure for damage, and manage Hostility risk. Battles are **conversations/debates** where you convince opponents through Diplomacy, Hostility, or Manipulation.

---

## Core Concept: Political Negotiation Battles

**Theme:** Card battles represent political debates, negotiations, and confrontations where you must break down your opponent's will to accept your position.

**Win Condition:** Reduce opponent's Resolve to 0

**Lose Condition:** Your Resolve reaches 0

**Three Card Types (Griftlands-inspired):**
- **Diplomacy (Green)** - Peaceful persuasion, build relationships
- **Hostility (Red)** - Threats, aggression, pressure tactics
- **Manipulate (Purple)** - Utility, card advantage, resource manipulation

---

## Battle Resources

### 1. Resolve (HP)
- **Both you and opponent have Resolve** (~20 starting)
- Represents willingness to negotiate/resist
- Reduce to 0 = Victory/Defeat
- Damaged by card effects
- Can be healed by certain cards

### 2. Composure (Offensive Buff)
- **You build Composure** with cards
- **Each stack = +1 damage** on your next attack
- Stacks consumed when dealing damage
- Faith Leader can spend ALL Composure for burst damage (Blessing card)
- Actor can convert Hostility → Composure (Ego Trip card)

### 3. Hostility (Self-Inflicted Debuff/Risk)
- **You gain Hostility** from playing aggressive red cards
- **Higher Hostility = Opponent deals MORE damage to your Resolve**
- Risk/reward mechanic: Hit harder but take more damage
- **Self-inflicted only** - opponent doesn't manipulate your Hostility
- Can be reduced with certain Manipulate cards
- Actor can convert Hostility → Composure

**Hostility Damage Formula:**
- Opponent's base damage × (1 + Hostility × 0.5)
- Example: 3 Hostility = opponent deals 2.5× damage

---

## Three Origins (Classes)

### Faith Leader - "The Peacemaker"
- **Passive**: "Divine Grace" - Start each negotiation with +1 card draw
- **Playstyle**: Composure combo specialist (build → burst with Blessing)
- **Deck**: 6 Diplomacy, 2 Hostility, 2 Manipulate

**Starter Deck (10 cards):**
1-4. Find Common Ground (1 AP) x4 - Deal 3 Resolve damage
5-6. Blessing (1 AP) x2 - Deal damage = Composure, consume all Composure
7-8. Accusation (1 AP) x2 - Deal 4 damage, gain 1 Hostility
9. Deflect (1 AP) x1 - Gain 3 Composure, reduce Hostility by 1
10. Gather Thoughts (1 AP) x1 - Gain 4 Composure

### Nepo Baby - "The Operator"
- **Passive**: "Family Connections" - Start with 4 AP instead of 3
- **Playstyle**: Card/AP advantage engine
- **Deck**: 3 Diplomacy, 2 Hostility, 5 Manipulate

**Starter Deck (10 cards):**
1-2. Family Name (1 AP) x2 - Deal 3 Resolve damage
3. Inherited Privilege (2 AP) x1 - Deal 5 damage, draw 1 card
4-5. Pull Strings (1 AP) x2 - Deal 4 damage, gain 1 Hostility
6-7. Call in Favor (1 AP) x2 - Draw 2 cards
8. Backroom Deal (2 AP) x1 - Draw 2 cards, gain 1 AP next turn
9. Dynasty Network (1 AP) x1 - Discard 1, draw 2
10. Trust Fund (0 AP) x1 - Gain 2 Composure, gain 1 AP this turn

### Actor - "The Risk Taker"
- **Passive**: "Stage Presence" - First card each turn costs 1 less AP
- **Playstyle**: Risk/reward specialist, Hostility → Composure conversion
- **Deck**: 3 Diplomacy, 4 Hostility, 3 Manipulate

**Starter Deck (10 cards):**
1-2. Charming Gambit (1 AP) x2 - Deal 3 damage, 50% chance: draw 1 card
3. All or Nothing (2 AP) x1 - Deal 3-9 damage (random)
4-5. Bold Accusation (1 AP) x2 - Deal 5 damage, gain 2 Hostility
6-7. Spotlight Hog (2 AP) x2 - Deal 6 damage, gain 3 Composure, gain 2 Hostility
8. High Stakes (0 AP) x1 - Discard hand, draw 3 cards
9. Ego Trip (1 AP) x1 - Gain Composure = Hostility, don't reduce Hostility
10. Fan Favorite (1 AP) x1 - Lose 3 Composure, reduce Hostility by 3

---

## Campaign Integration (Griftlands-inspired)

### Map Traversal
- Slay the Spire-style node map
- Travel between locations (cities, provinces)
- Node types: Battles, Events, Rest sites, Shops
- Limited time before election day

### Ally/Enemy Network
- Every negotiation victory creates **ally** or **enemy**
- **Allies** give signature cards + passive bonuses
- **Enemies** appear in future battles to oppose you
- Network affects available paths and battle difficulty

**Ally Benefits:**
- Add their signature card to your deck
- Passive bonuses (e.g., "Start with +1 card draw if Manila Mayor is ally")
- Can be "summoned" during negotiation for powerful effect

**Enemy Consequences:**
- Enemies send opposition in future battles
- Add Arguments/Effects to opponent's side
- Build Heat (scandal meter)
- Block certain map paths

### Battle Outcomes
- **Diplomatic Victory** (low Hostility used): Ally, low rewards, maintain relationship
- **Aggressive Victory** (high Hostility used): Better rewards, enemy created, +Heat

---

## Core Systems Implemented

### 1. BattleManager (State Machine)
- **Location:** `Assets/Scripts/Gameplay/Battle/BattleManager.cs`
- **Pattern:** State machine with EventBus communication
- **Not a Singleton:** Instantiated per-battle
- **Responsibilities:**
  - Orchestrates battle flow through states
  - Manages player/opponent stats and decks
  - Handles card playing and turn management
  - Checks victory conditions

**Battle States:**
1. **Initialize** - Draw initial hands
2. **TurnStart** - Draw cards, refresh Action Points
3. **PlayerTurn** - Wait for player input (EndTurnRequestedEvent)
4. **OpponentTurn** - AI plays cards
5. **TurnEnd** - Apply Hostility damage, check victory
6. **BattleEnd** - Calculate rewards based on Hostility used

### 2. BattleStats (NEEDS UPDATE)
- **Location:** `Assets/Scripts/Gameplay/BattleStats.cs`
- **Manages per-combatant resources:**
  - **Resolve** - HP (0 = defeat)
  - **Composure** - Offensive buff (stacks add damage)
  - **Hostility** - Self-inflicted debuff (opponent deals more damage)
  - **Action Points** - Energy to play cards (refreshes each turn)

**Status Effects:**
- **Composure**: +1 damage per stack (consumed on attack)
- **Hostility**: Multiplies incoming damage (1 + Hostility × 0.5)
- **Vulnerable**: +50% damage taken (debuff)
- **Weakened**: -2 damage dealt (debuff)

### 3. DeckManager
- **Location:** `Assets/Scripts/Gameplay/Battle/DeckManager.cs`
- **Manages card zones:**
  - **Deck** - Draw pile (10 cards starting)
  - **Hand** - Cards available to play
  - **Discard** - Played/discarded cards
  - **Exhaust** - Removed from battle
- **Features:**
  - Auto-shuffle when deck empties
  - Max hand size (10 cards)
  - Card zone queries and statistics

### 4. EffectResolver (NEEDS UPDATE)
- **Location:** `Assets/Scripts/Gameplay/Battle/EffectResolver.cs`
- **Resolves card effects:**
  - Resolve damage
  - Composure gain/loss
  - Hostility gain/reduction
  - Card draw/discard
  - Action Point manipulation
  - Buffs/Debuffs (Vulnerable, Weakened)

### 5. OriginStats (NEEDS UPDATE)
- **Location:** `Assets/Scripts/Data/OriginStats.cs`
- **ScriptableObject:** Configure origin-specific battle stats
- **Origin Battle Stats:**
  - **Faith Leader**: 20 Resolve, 3 AP, +1 card draw passive
  - **Nepo Baby**: 20 Resolve, 4 AP (extra AP passive)
  - **Actor**: 20 Resolve, 3 AP, first card -1 AP passive

### 6. BattleEvents
- **Location:** `Assets/Scripts/Gameplay/Battle/BattleEvents.cs`
- **EventBus communication:**
  - Battle lifecycle: `BattleStartedEvent`, `BattleEndedEvent`
  - Turn events: `TurnStartedEvent`, `TurnEndedEvent`
  - Card events: `CardPlayedEvent`, `CardDrawnEvent`, `CardDiscardedEvent`
  - Effect events: `ComposureGainedEvent`, `HostilityGainedEvent`, `ResolveChangedEvent`
  - Player input: `EndTurnRequestedEvent`, `PlayCardRequestedEvent`

---

## Card Cost System

### Cards ONLY Cost Action Points in Battle
- **Funds/Influence/Heat** are meta resources (shops, events, campaign)
- **No other costs in battle** - just Action Points

### Cost Types

#### 1. Free (0 AP)
```csharp
CardCost.Free()
```
- No cost, always playable

#### 2. Fixed Cost (1-2 AP)
```csharp
new CardCost(CostType.ActionPoints, 1)
```
- Costs specific amount
- Most starter cards are 1-2 AP

#### 3. X Cost (REMOVED - Not needed for negotiation)
- Simplified system: Fixed costs only

#### 4. Dynamic Cost (REMOVED - Not needed for negotiation)
- Simplified system: Fixed costs only

---

## Battle Flow

### Start of Battle
1. `BattleManager.StartBattle(BattleSetup)` called
2. Initialize combatant stats (origin-specific)
3. Create DeckManagers with player/opponent 10-card decks
4. Create EffectResolver
5. Apply origin passives (Faith Leader +1 draw, etc.)
6. Publish `BattleStartedEvent`
7. Transition to **Initialize** state

### Initialize State
1. Draw initial hands (5 cards for player, 4 for opponent)
2. Faith Leader draws 6 cards (passive)
3. Transition to **TurnStart** state

### Turn Cycle
1. **TurnStart**
   - Increment turn counter, switch active player
   - Refresh Action Points to max (3-4 depending on origin)
   - Draw 1 card
   - Apply Actor passive (first card -1 AP this turn)
   - Publish `TurnStartedEvent`
   - Transition to PlayerTurn or OpponentTurn

2. **PlayerTurn / OpponentTurn**
   - Player: Wait for `PlayCardRequestedEvent` or `EndTurnRequestedEvent`
   - Opponent: AI plays cards (TODO: OpponentAI)

3. **TurnEnd**
   - Apply Hostility damage to player (if any)
   - Clear turn-based buffs
   - Publish `TurnEndedEvent`
   - Check victory conditions
   - If battle over: Transition to **BattleEnd**
   - Else: Transition to **TurnStart**

### Playing a Card
1. User publishes `PlayCardRequestedEvent`
2. BattleManager validates card can be played:
   - Check `cost.CanAfford(currentActionPoints)`
3. Pay costs:
   - `stats.SpendActionPoints(cost)`
4. Apply card effects:
   - Resolve damage (modified by Composure)
   - Composure gain/loss
   - Hostility gain
   - Card draw/discard
5. Move card from hand to discard
6. Publish `CardPlayedEvent`
7. Resolve effects via `EffectResolver`

### End of Battle
1. Check victory conditions:
   - Player Resolve <= 0: Defeat
   - Opponent Resolve <= 0: Victory
2. Calculate outcome based on Hostility used:
   - Low Hostility: Diplomatic victory (ally, maintain relationship)
   - High Hostility: Aggressive victory (enemy, higher rewards, +Heat)
3. Create `BattleResult` with stats and relationship changes
4. Publish `BattleEndedEvent`
5. Update campaign state (allies, enemies, Heat, rewards)

---

## Systems Still Needed

### 1. OpponentAI (Critical)
- Decision making for opponent turns
- Card selection logic based on Resolve/Composure/Hostility
- Target selection
- Difficulty scaling

### 2. Ally/Enemy System (Important)
- Track allies and enemies created through battles
- Signature card system (allies give cards)
- Passive bonus system (ally bonuses in battle)
- Enemy interference system (enemies add opposition)

### 3. BattleRewardManager (Important)
- Post-battle rewards based on Hostility used
- Diplomatic victory rewards (ally, moderate funds/influence)
- Aggressive victory rewards (enemy, high funds/influence, +Heat)
- Card reward selection UI

### 4. Map/Campaign System (Important)
- Slay the Spire-style node map
- Location system with battles, events, shops, rest
- Path selection and unlocking
- Time/turn limit before election

### 5. StatusEffectManager (Nice to have)
- Buff/Debuff tracking beyond Composure/Hostility
- Vulnerable, Weakened effects
- Status effect duration and stacking
- Turn-based tick/expiry

### 6. CombatAnimationController (Polish)
- Card play animations
- Resolve/Composure/Hostility VFX
- State transition animations

---

## Architecture Principles

### SOLID Compliance
- **Single Responsibility:** Each system has one clear job
- **Dependency Injection:** BattleManager doesn't use singletons
- **EventBus Communication:** Loose coupling between systems
- **State Machine:** Clear separation of battle phases

### Type-Safe Logging
- All logging uses `GameLogger.LogInfo<T>()`
- No string literals for component names
- Auto-registers categories from type names

### No Singleton Abuse
- BattleManager is NOT a singleton
- Only global services are singletons (AudioManager, SaveManager, etc.)
- Battle systems are instantiated per-battle

---

## Example Card Implementations

### Diplomacy Card (Find Common Ground)
```csharp
cost: new CardCost(CostType.ActionPoints, 1)
effects: [
  new CardEffect(BattleEffectType.ResolveDamage, TargetType.Opponent, 3)
]
```

### Hostility Card (Accusation)
```csharp
cost: new CardCost(CostType.ActionPoints, 1)
effects: [
  new CardEffect(BattleEffectType.ResolveDamage, TargetType.Opponent, 4),
  new CardEffect(BattleEffectType.GainHostility, TargetType.Self, 1)
]
```

### Manipulate Card (Gather Thoughts)
```csharp
cost: new CardCost(CostType.ActionPoints, 1)
effects: [
  new CardEffect(BattleEffectType.GainComposure, TargetType.Self, 4)
]
```

### Combo Card (Blessing - Faith Leader)
```csharp
cost: new CardCost(CostType.ActionPoints, 1)
effects: [
  new CardEffect(BattleEffectType.ResolveDamageEqualToComposure, TargetType.Opponent, 0),
  new CardEffect(BattleEffectType.ConsumeAllComposure, TargetType.Self, 0)
]
```

---

## Testing Checklist

- [ ] Battle initialization with 3 origins
- [ ] Turn flow (start → player → opponent → end)
- [ ] Card playing and AP cost validation
- [ ] Resolve damage calculation
- [ ] Composure gain and damage bonus
- [ ] Hostility gain and incoming damage multiplier
- [ ] Origin passives (Faith Leader +1 draw, Nepo Baby 4 AP, Actor -1 AP first card)
- [ ] Victory/defeat conditions (Resolve = 0)
- [ ] EventBus notifications
- [ ] Deck shuffling when empty (10 card decks)
- [ ] Max hand size enforcement
- [ ] Exhaust pile functionality
- [ ] Ally/Enemy creation based on Hostility
- [ ] Campaign reward calculation

---

**Status:** Redesigned with Griftlands-inspired negotiation system. Core battle loop needs updating for Resolve/Composure/Hostility. Needs OpponentAI, Ally/Enemy system, and Campaign integration.
