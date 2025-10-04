# Battle System Documentation

## Overview

The battle system is a turn-based card game where players use Action Points to play cards that deal Confidence/Ego damage, apply buffs/debuffs, and manipulate resources.

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
5. **TurnEnd** - Clear block, check victory
6. **BattleEnd** - Calculate rewards

### 2. BattleStats
- **Location:** `Assets/Scripts/Gameplay/BattleStats.cs`
- **Manages per-combatant resources:**
  - **Ego** - True HP (0 = defeat)
  - **Confidence** - Shield (must break before Ego damage)
  - **Action Points** - Energy to play cards (refreshes each turn)
  - **Block** - Temporary damage reduction (clears each turn)

**Ego Vulnerability System:**
- Low Confidence = Higher Ego damage taken
- Formula: `1.0 + (MissingConfidence / MaxConfidence)`
- Example: 75% missing Confidence = 1.75x Ego damage

### 3. DeckManager
- **Location:** `Assets/Scripts/Gameplay/Battle/DeckManager.cs`
- **Manages card zones:**
  - **Deck** - Draw pile
  - **Hand** - Cards available to play
  - **Discard** - Played/discarded cards
  - **Exhaust** - Removed from battle
- **Features:**
  - Auto-shuffle when deck empties
  - Max hand size (10 cards)
  - Card zone queries and statistics

### 4. EffectResolver
- **Location:** `Assets/Scripts/Gameplay/Battle/EffectResolver.cs`
- **Resolves card effects:**
  - Damage (Confidence/Ego)
  - Healing
  - Block gain
  - Card draw/discard
  - Action Point manipulation
  - Buffs/Debuffs (TODO)

### 5. OriginStats
- **Location:** `Assets/Scripts/Data/OriginStats.cs`
- **ScriptableObject:** Configure origin-specific battle stats
- **Origin Battle Stats:**
  - **Faith Leader**: 80 Ego, 120 Confidence, 3 AP (defensive tank)
  - **Nepo Baby**: 100 Ego, 100 Confidence, 4 AP (flexible, extra AP)
  - **Actor**: 120 Ego, 80 Confidence, 3 AP (glass cannon)

### 6. BattleEvents
- **Location:** `Assets/Scripts/Gameplay/Battle/BattleEvents.cs`
- **EventBus communication:**
  - Battle lifecycle: `BattleStartedEvent`, `BattleEndedEvent`
  - Turn events: `TurnStartedEvent`, `TurnEndedEvent`
  - Card events: `CardPlayedEvent`, `CardDrawnEvent`, `CardDiscardedEvent`
  - Effect events: `EffectAppliedEvent`, `DamageDealtEvent`, `HealingAppliedEvent`
  - Player input: `EndTurnRequestedEvent`, `PlayCardRequestedEvent`

---

## Card Cost System

### Cards ONLY Cost Action Points in Battle
- **Funds/Influence** are meta resources (shops, events)
- **Fear/Clout/Faith** are origin abilities (outside battle)

### Cost Types

#### 1. Free (0 AP)
```csharp
CardCost.Free()
```
- No cost, always playable

#### 2. Fixed Cost (1-3 AP)
```csharp
new CardCost(CostType.ActionPoints, 2)
```
- Costs specific amount

#### 3. X Cost
```csharp
CardCost.XCost()
```
- Costs ALL remaining Action Points
- Requires at least 1 AP

#### 4. Dynamic Cost
```csharp
new CardCost(CostType.ActionPoints, 5, costChangePerTurn: -1)
```
- `costChangePerTurn: -1` = Gets 1 AP cheaper each turn
- `costChangePerTurn: +1` = Gets 1 AP more expensive each turn

### Cost Modifiers
- **Per-Turn Changes:** Cards held in hand get cheaper/more expensive
- **Runtime Modifiers:** Buffs/debuffs can affect costs temporarily
- **Min/Max Limits:** Costs clamped between 0-99

### Cost Tracking
- `OnDrawn()` - Reset when drawn
- `OnTurnInHand()` - Called each turn to track time in hand
- `ApplyCostModifier(int)` - Apply temporary cost changes
- `GetActualCost(int)` - Returns final cost after all modifiers

---

## Battle Flow

### Start of Battle
1. `BattleManager.StartBattle(BattleSetup)` called
2. Initialize combatant stats (origin-specific)
3. Create DeckManagers with player/opponent decks
4. Create EffectResolver
5. Publish `BattleStartedEvent`
6. Transition to **Initialize** state

### Initialize State
1. Draw initial hands (5 cards each)
2. Transition to **TurnStart** state

### Turn Cycle
1. **TurnStart**
   - Increment turn counter, switch active player
   - Refresh Action Points to max
   - Draw cards (1 per turn)
   - Call `OnTurnInHand()` on all cards in hand
   - Publish `TurnStartedEvent`
   - Transition to PlayerTurn or OpponentTurn

2. **PlayerTurn / OpponentTurn**
   - Player: Wait for `PlayCardRequestedEvent` or `EndTurnRequestedEvent`
   - Opponent: AI plays cards (TODO: OpponentAI)

3. **TurnEnd**
   - Clear Block (temporary damage reduction)
   - Publish `TurnEndedEvent`
   - Check victory conditions
   - If battle over: Transition to **BattleEnd**
   - Else: Transition to **TurnStart**

### Playing a Card
1. User publishes `PlayCardRequestedEvent`
2. BattleManager validates card can be played:
   - Check `cost.CanAfford(currentActionPoints)`
3. Pay costs:
   - `cost.GetActualCost()` for dynamic/X costs
   - `stats.SpendActionPoints(cost)`
4. Move card from hand to discard
5. Publish `CardPlayedEvent`
6. Resolve effects via `EffectResolver`

### End of Battle
1. Check victory conditions:
   - Player Ego <= 0: Defeat
   - Opponent Ego <= 0: Victory
2. Create `BattleResult` with stats
3. Publish `BattleEndedEvent`
4. TODO: Calculate rewards, update campaign state

---

## Systems Still Needed

### 1. OpponentAI (Critical)
- Decision making for opponent turns
- Card selection logic
- Target selection
- Difficulty scaling

### 2. StatusEffectManager (Important)
- Buff/Debuff tracking and application
- Status effect duration and stacking
- Turn-based tick/expiry

### 3. BattleRewardManager (Important)
- Post-battle rewards (cards, funds, influence)
- Reward selection UI
- Card upgrade opportunities

### 4. OriginPassiveManager (Nice to have)
- Origin-specific passive abilities
- Fear/Clout/Faith/Influence tracking
- Trigger conditions and bonuses

### 5. CombatAnimationController (Polish)
- Card play animations
- Damage/heal VFX
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

## Example Card Designs

### Basic Attack (1 AP)
```csharp
cost: new CardCost(CostType.ActionPoints, 1)
effects: [
  new CardEffect(BattleEffectType.ConfidenceDamage, TargetType.Opponent, 10)
]
```

### All-Out Attack (X AP)
```csharp
cost: CardCost.XCost()
effects: [
  new CardEffect(BattleEffectType.ConfidenceDamage, TargetType.Opponent, X) // X = AP spent
]
```

### Delayed Nuke (5 AP, -1 per turn)
```csharp
cost: new CardCost(CostType.ActionPoints, 5, costChangePerTurn: -1)
effects: [
  new CardEffect(BattleEffectType.EgoDamage, TargetType.Opponent, 50)
]
// Turn 1: 5 AP (expensive!)
// Turn 2: 4 AP
// Turn 3: 3 AP (hold it to make it cheaper!)
```

### Momentum Strike (1 AP, +1 per turn)
```csharp
cost: new CardCost(CostType.ActionPoints, 1, costChangePerTurn: 1)
effects: [
  new CardEffect(BattleEffectType.ConfidenceDamage, TargetType.Opponent, 15)
]
// Turn 1: 1 AP (cheap!)
// Turn 2: 2 AP
// Turn 3: 3 AP (play it fast or it gets expensive!)
```

---

## Testing Checklist

- [ ] Battle initialization with different origins
- [ ] Turn flow (start → player → opponent → end)
- [ ] Card playing and cost validation
- [ ] Free, fixed, and X cost cards
- [ ] Dynamic cost changes per turn
- [ ] Confidence damage → Ego damage flow
- [ ] Block damage reduction
- [ ] Victory/defeat conditions
- [ ] EventBus notifications
- [ ] Deck shuffling when empty
- [ ] Max hand size enforcement
- [ ] Exhaust pile functionality

---

**Status:** Core battle loop implemented and functional. Needs OpponentAI, StatusEffects, and Rewards to be complete.
