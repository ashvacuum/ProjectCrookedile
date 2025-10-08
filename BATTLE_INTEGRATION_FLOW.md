# Battle System Integration Flow

Visual guide showing how all the battle components work together.

---

## 🔄 System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        BATTLE SCENE                          │
└─────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              │               │               │
         ┌────▼────┐    ┌────▼────┐    ┌────▼─────┐
         │ Battle  │    │   3D    │    │   UI     │
         │ Manager │    │  Cards  │    │  Overlay │
         └────┬────┘    └────┬────┘    └────┬─────┘
              │              │              │
              └──────────────┼──────────────┘
                             │
                      ┌──────▼──────┐
                      │  EventBus   │
                      │ (Observer)  │
                      └─────────────┘
```

---

## 📊 Data Flow Diagram

### Battle Start Flow
```
BattleTestStarter.Start()
    │
    ├─> Creates BattleSetup (deck, origins, stats)
    │
    ├─> Initializes BattleCardHandBridge(battleManager)
    │
    ├─> Initializes BattleStatsOverlay(battleManager)
    │
    └─> battleManager.StartBattle(setup)
            │
            ├─> Publishes BattleStartedEvent
            │       │
            │       ├─> BattleCardHandBridge receives → syncs hand
            │       └─> BattleStatsOverlay receives → updates UI
            │
            └─> Transitions to Initialize state
                    │
                    └─> Draws initial hands → TurnStart
```

### Card Play Flow
```
Player clicks 3D card
    │
    ▼
CardInputHandler.HandlePointerUp()
    │
    ▼
CardHandManager.PlayCard(card)
    │
    ├─> Finds card index in hand
    │
    └─> Publishes PlayCardRequestedEvent
            │
            ▼
        BattleManager.OnPlayCardRequested()
            │
            ├─> Validates can play (AP cost)
            │
            ├─> Pays costs (AP)
            │
            ├─> DeckManager.PlayCardAtIndex() → moves card to discard
            │
            ├─> Publishes CardPlayedEvent
            │       │
            │       ├─> BattleCardHandBridge receives → removes 3D card
            │       └─> BattleStatsOverlay receives → updates stats
            │
            └─> EffectResolver.ResolveCardEffects()
                    │
                    ├─> Applies damage (with status modifiers)
                    ├─> Applies Composure/Hostility changes
                    ├─> Applies status effects
                    └─> Triggers card draw/discard effects
```

### Turn Flow
```
Player Turn
    │
    ├─> Publishes TurnStartedEvent(isPlayerTurn: true)
    │       │
    │       ├─> BattleCardHandBridge → shows hand
    │       └─> BattleStatsOverlay → enables End Turn button
    │
    ├─> Player plays cards or clicks End Turn
    │
    └─> Publishes EndTurnRequestedEvent
            │
            ▼
        BattleManager transitions to TurnEnd
            │
            ├─> StatusEffects.OnTurnEnd() → applies effects
            ├─> Checks victory conditions
            └─> Transitions to Opponent Turn
                    │
                    └─> Publishes TurnStartedEvent(isPlayerTurn: false)
                            │
                            ├─> BattleCardHandBridge → clears hand
                            ├─> BattleStatsOverlay → disables End Turn
                            └─> Opponent AI plays (or auto-passes)
```

---

## 🔌 Component Dependencies

### BattleManager
**Depends on:**
- `BattleStats` (player & opponent)
- `DeckManager` (player & opponent)
- `EffectResolver`
- `StatusEffectManager` (via EffectResolver)

**Creates Events:**
- `BattleStartedEvent`
- `TurnStartedEvent`
- `TurnEndedEvent`
- `BattleEndedEvent`

**Listens to Events:**
- `PlayCardRequestedEvent`
- `EndTurnRequestedEvent`

---

### CardHandManager (3D Cards)
**Depends on:**
- `Card3DView` prefab
- `CardInputHandler`

**Creates Events:**
- `PlayCardRequestedEvent` (when card clicked)

**Listens to Events:**
- None (controlled by BattleCardHandBridge)

---

### BattleCardHandBridge
**Depends on:**
- `CardHandManager`
- `BattleManager`

**Creates Events:**
- None

**Listens to Events:**
- `BattleStartedEvent` → sync hand
- `TurnStartedEvent` → show/hide hand
- `CardPlayedEvent` → remove card from 3D hand

---

### BattleStatsOverlay (UI)
**Depends on:**
- `BattleManager`
- UI Text fields (TMP)

**Creates Events:**
- `EndTurnRequestedEvent` (when button clicked)

**Listens to Events:**
- `BattleStartedEvent` → update stats
- `TurnStartedEvent` → update turn info
- `TurnEndedEvent` → update stats
- `CardPlayedEvent` → update stats
- `BattleEndedEvent` → show victory/defeat

---

### EffectResolver
**Depends on:**
- `BattleStats` (player & opponent)
- `DeckManager` (player & opponent)
- `StatusEffectManager` (player & opponent)

**Creates Events:**
- None (modifies stats directly)

**Called by:**
- `BattleManager.PlayCard()`

---

## 🎯 Initialization Order

**Critical:** Components must be initialized in this order:

1. ✅ **BattleManager** instantiated
2. ✅ **BattleCardHandBridge.Initialize(battleManager)**
3. ✅ **BattleStatsOverlay.Initialize(battleManager)**
4. ✅ **BattleManager.StartBattle(setup)**

Example from BattleTestStarter:
```csharp
void Start()
{
    // 1. BattleManager already exists in scene

    // 2. Initialize bridges
    cardHandBridge.Initialize(battleManager);
    statsOverlay.Initialize(battleManager);

    // 3. Create setup data
    BattleSetup setup = new BattleSetup { ... };

    // 4. Start battle
    battleManager.StartBattle(setup);
}
```

---

## 🧩 Key Integration Points

### 1. EventBus Communication
All systems communicate via EventBus:
- ✅ **Decoupled** - Systems don't directly reference each other
- ✅ **Flexible** - Easy to add new listeners
- ✅ **Testable** - Can mock events

### 2. DeckManager ↔ CardHandManager
- **Logical state** (DeckManager) separate from **visual state** (CardHandManager)
- **BattleCardHandBridge** syncs them via events
- DeckManager is source of truth for card positions

### 3. BattleStats ↔ UI Display
- BattleStats holds actual values
- BattleStatsOverlay reads and displays them
- Updates happen on events (not every frame)

### 4. Effect Resolution Pipeline
```
Card Played
    │
    ▼
Pay Costs (with status modifiers)
    │
    ▼
EffectResolver.ResolveCardEffects()
    │
    ├─> StatusEffectManager.ModifyDamageDealt()
    │
    ├─> StatusEffectManager.ModifyDamageTaken()
    │
    ├─> BattleStats.DamageResolve()
    │
    └─> StatusEffectManager.ModifyComposureGained()
```

---

## 🚨 Common Integration Issues

### Issue: Cards don't appear in hand
**Cause:** BattleCardHandBridge not initialized before StartBattle()
**Fix:** Call `cardHandBridge.Initialize(battleManager)` in Start()

### Issue: UI doesn't update
**Cause:** BattleStatsOverlay not subscribed to events
**Fix:** Ensure OnEnable() is called (component must be enabled)

### Issue: Card clicks don't work
**Cause:** CardInputHandler missing camera reference
**Fix:** Assign Main Camera in inspector

### Issue: Stats show as 0/0
**Cause:** BattleStatsOverlay.Initialize() not called
**Fix:** Call `statsOverlay.Initialize(battleManager)` before StartBattle()

---

## 📋 Integration Checklist

Use this when setting up a new battle scene:

- [ ] BattleManager GameObject exists
- [ ] 3D Card Hand setup with all 3 components
- [ ] UI Canvas with BattleStatsOverlay
- [ ] All inspector references assigned
- [ ] BattleTestStarter script calls Initialize() in correct order
- [ ] Test deck has CardData ScriptableObjects
- [ ] OriginStats ScriptableObject exists
- [ ] Card3DView prefab has collider
- [ ] EventBus is working (check console)
- [ ] Scene plays without errors

---

## 🎓 Learning Resources

- **EventBus Pattern:** `Assets/Scripts/Core/EventBus.cs`
- **Battle State Machine:** `Assets/Scripts/Gameplay/Battle/BattleManager.cs`
- **Effect Resolution:** `Assets/Scripts/Gameplay/Battle/EffectResolver.cs`
- **Full Setup Guide:** `BATTLE_UI_SETUP.md`
- **Task Tracker:** `BATTLE_SYSTEM_TASKS.md`

