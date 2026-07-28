> [!WARNING]
> **Deprecated — no longer in use.** Setup steps for a pre-redesign scene and script layout.
> Kept because the ideas may still be worth mining; the specifics are not.
> **Superseded by:** [`docs/core-design.md`](../core-design.md) and the code itself
> Index: [`docs/deprecated/README.md`](README.md)  ·  Back to [`readme.md`](../../readme.md)

---

> [!WARNING]
> **Pre-redesign document — may be inaccurate (flagged 2026-06).** Crookedile underwent a major combat + class redesign. Canonical design now lives in [`docs/core-design.md`](docs/core-design.md) and [`docs/crookedile-starter-decks.md`](docs/crookedile-starter-decks.md); current code/architecture is summarized in [`readme.md`](readme.md). Treat specifics below as historical until reconciled.

# Battle UI Setup Guide

**Last Updated:** 2025-10-08

Complete guide for setting up the battle UI in Unity using the 3D card system.

---

## 🎴 3D Card Battle System

The battle UI uses the existing 3D card visualization system integrated with BattleManager.

### Components Created

#### 1. **BattleCardHandBridge.cs**
- Syncs 3D CardHandManager with BattleManager's logical DeckManager
- Automatically draws/removes cards based on battle events
- Clears hand during opponent's turn

#### 2. **BattleStatsOverlay.cs**
- Canvas overlay showing battle stats
- Displays Resolve, Composure, Hostility, AP for both players
- End Turn button (enabled only during player turn)
- Victory/Defeat panels

#### 3. **CardHandManager.cs** (Updated)
- Added `PlayCard()` method that publishes `PlayCardRequestedEvent`
- Connects 3D card interactions to battle logic

#### 4. **CardInputHandler.cs** (Updated)
- Click card → calls `CardHandManager.PlayCard()`
- Card plays when clicked (no drag required for now)

---

## 🎬 Unity Scene Setup

### Hierarchy Structure

```
BattleScene
├── BattleManager (GameObject)
│   └── BattleManager (Component)
│
├── 3D Card Hand (GameObject)
│   ├── CardHandManager (Component)
│   ├── CardInputHandler (Component)
│   ├── BattleCardHandBridge (Component)
│   ├── Hand Transform (Empty GameObject)
│   ├── Deck Position (Empty GameObject)
│   └── Discard Position (Empty GameObject)
│
├── Battle UI Canvas (Canvas - Screen Space Overlay)
│   ├── BattleStatsOverlay (Component)
│   ├── Player Stats Panel
│   │   ├── Resolve Text (TMP)
│   │   ├── Composure Text (TMP)
│   │   ├── Hostility Text (TMP)
│   │   └── AP Text (TMP)
│   ├── Opponent Stats Panel
│   │   ├── Resolve Text (TMP)
│   │   ├── Composure Text (TMP)
│   │   ├── Hostility Text (TMP)
│   │   └── AP Text (TMP)
│   ├── Battle Info Panel
│   │   ├── Turn Info Text (TMP)
│   │   └── Phase Text (TMP)
│   ├── End Turn Button
│   ├── Victory Panel (Hidden by default)
│   └── Defeat Panel (Hidden by default)
│
├── Main Camera
└── Lights
```

---

## 📝 Step-by-Step Setup

### Step 1: Create Battle Scene
1. Create new scene: `Scenes/BattleTest.unity`
2. Add Main Camera (position for good view of cards)
3. Add directional light

### Step 2: Setup BattleManager
1. Create empty GameObject: `BattleManager`
2. Add `BattleManager` component
3. Configure settings:
   - Starting Hand Size: `5`
   - Cards Per Turn: `1`

### Step 3: Setup 3D Card Hand
1. Create empty GameObject: `3D Card Hand`
2. Add components:
   - `CardHandManager`
   - `CardInputHandler`
   - `BattleCardHandBridge`
3. Create child transforms:
   - `Hand Transform` (position: 0, 0, 0)
   - `Deck Position` (position: -5, 0, 0)
   - `Discard Position` (position: 5, 0, 0)
4. Configure `CardHandManager`:
   - Card Prefab: Assign Card3DView prefab
   - Hand Transform: Assign Hand Transform
   - Deck Position: Assign Deck Position
   - Discard Position: Assign Discard Position
   - Card Spacing: `2`
   - Card Arc Height: `0.5`
   - Card Rotation Angle: `5`
5. Configure `CardInputHandler`:
   - Hand Manager: Assign CardHandManager
   - Main Camera: Assign Main Camera
   - Card Layer Mask: Default (all layers)
6. Configure `BattleCardHandBridge`:
   - Card Hand Manager: Assign CardHandManager
   - Battle Manager: Leave empty (will be set at runtime)

### Step 4: Setup Battle UI Canvas
1. Create UI Canvas (Screen Space - Overlay)
2. Add `BattleStatsOverlay` component to Canvas
3. Create UI layout:

#### Player Stats Panel (Bottom Left)
```
Panel (Anchor: Bottom Left, Pivot: 0, 0)
├── HP Text: "HP: 20/20"
├── Composure Text: "Composure: 0"
├── Hostility Text: "Hostility: 0 (1.0x)"
└── AP Text: "AP: 3/3"
```

#### Opponent Stats Panel (Top Left)
```
Panel (Anchor: Top Left, Pivot: 0, 1)
├── HP Text: "HP: 20/20"
├── Composure Text: "Composure: 0"
├── Hostility Text: "Hostility: 0"
└── AP Text: "AP: 3/3"
```

#### Battle Info (Top Center)
```
Panel (Anchor: Top Center)
├── Turn Info Text: "Turn 1 - Your Turn"
└── Phase Text: "PlayerTurn"
```

#### Controls (Bottom Right)
```
Panel (Anchor: Bottom Right)
└── End Turn Button
    └── Text: "End Turn"
```

#### Result Panels (Center, Hidden)
```
Victory Panel (Initially disabled)
└── Text: "VICTORY!"

Defeat Panel (Initially disabled)
└── Text: "DEFEAT"
```

4. Assign all text references in `BattleStatsOverlay` inspector

### Step 5: Create Card Prefab (If Not Exists)
1. Create 3D card prefab with:
   - Quad mesh with card material
   - 3 TextMeshPro objects (name, cost, description)
   - `Card3DView` component
   - Collider for raycasting
2. Save as prefab: `Prefabs/Card3DView.prefab`

### Step 6: Create Test Starter Deck
Since we don't have actual cards yet, create a simple test:
1. Create ScriptableObject folder: `Assets/Data/Cards/Test/`
2. Create a test CardData:
   - Name: "Test Strike"
   - Type: Hostility
   - Cost: 1 AP
   - Effect: Deal 5 damage
3. Create 10 copies for testing

### Step 7: Create Battle Setup Script
Create a simple test script to start battles:

```csharp
using UnityEngine;
using Crookedile.Gameplay.Battle;
using Crookedile.Data;
using Crookedile.Data.Cards;
using System.Collections.Generic;

public class BattleTestStarter : MonoBehaviour
{
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private BattleCardHandBridge cardHandBridge;
    [SerializeField] private BattleStatsOverlay statsOverlay;
    [SerializeField] private List<CardData> testDeck;
    [SerializeField] private OriginStats originStats;

    void Start()
    {
        StartTestBattle();
    }

    void StartTestBattle()
    {
        BattleSetup setup = new BattleSetup
        {
            playerOrigin = OriginType.Actor,
            opponentOrigin = OriginType.FaithLeader,
            originStats = originStats,
            playerDeck = new List<CardData>(testDeck),
            opponentDeck = new List<CardData>(testDeck)
        };

        // Initialize components
        cardHandBridge.Initialize(battleManager);
        statsOverlay.Initialize(battleManager);

        // Start battle
        battleManager.StartBattle(setup);
    }
}
```

---

## ⚡ Quick Start Checklist

Before you can test battles, make sure you have:

- [ ] **BattleManager** GameObject in scene with BattleManager component
- [ ] **3D Card Hand** GameObject with CardHandManager, CardInputHandler, BattleCardHandBridge
- [ ] **Battle UI Canvas** with BattleStatsOverlay and all text fields assigned
- [ ] **Card3DView prefab** with collider and Card3DView component
- [ ] **Test deck** with at least 10 CardData ScriptableObjects
- [ ] **OriginStats** ScriptableObject with battle stats configured
- [ ] **BattleTestStarter** script on a GameObject to start the battle
- [ ] **Main Camera** positioned to see cards
- [ ] All component references assigned in inspector

---

## 🎮 How to Play

1. **Start Scene** → Battle initializes automatically
2. **Cards appear in 3D hand** during your turn
3. **Click card** to play it
4. **Watch stats update** on overlay
5. **Click "End Turn"** when done
6. **Opponent auto-passes** their turn (no AI yet)
7. **Battle ends** when either Resolve reaches 0

---

## 🔧 Current Limitations

### What Works
- ✅ 3D card visualization
- ✅ Click to play cards
- ✅ Stats display and updates
- ✅ Turn flow (player → opponent → player)
- ✅ Victory/defeat detection
- ✅ Card effects resolve correctly
- ✅ Status effects apply and display

### What's Missing
- ❌ Opponent AI (opponent just passes turn)
- ❌ Actual starter deck cards (need to create 30 cards)
- ❌ Origin passive abilities not implemented
- ❌ Card drag-and-drop (currently click to play)
- ❌ Animations/VFX for card effects
- ❌ Status effect visual indicators

---

## 🐛 Troubleshooting

### Cards don't appear
- Check BattleCardHandBridge is initialized with BattleManager
- Verify Card3DView prefab is assigned to CardHandManager
- Ensure test deck has cards assigned

### Cards can't be clicked
- Check CardInputHandler has Main Camera assigned
- Verify Card3DView prefab has a Collider
- Check Layer Mask in CardInputHandler includes card layer

### Stats don't update
- Verify BattleStatsOverlay is initialized with BattleManager
- Check all text references are assigned in inspector
- Ensure EventBus is working (check console for events)

### End Turn button doesn't work
- Check button has OnClick listener
- Verify BattleStatsOverlay is subscribed to events
- Make sure battle is in PlayerTurn state

---

## 🎯 Next Steps

1. **Create starter deck cards** (30 total - 10 per origin)
2. **Create OriginStats ScriptableObject** with battle stats
3. **Test battle flow** end-to-end
4. **Add opponent AI** (optional - can test without it)
5. **Implement origin passives** (Faith Leader +1 draw, etc.)
6. **Polish UI** layout and styling
7. **Add VFX/animations** for card effects

---

## 📚 Related Files

- **Battle Logic:** `Assets/Scripts/Gameplay/Battle/`
- **UI Components:** `Assets/Scripts/UI/` and `Assets/Scripts/UI/Battle/`
- **Card Data:** `Assets/Scripts/Data/Cards/`
- **Documentation:** `BATTLE_SYSTEM.md`, `BATTLE_SYSTEM_TASKS.md`

