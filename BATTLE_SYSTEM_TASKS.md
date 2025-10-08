# Battle System Implementation Tasks

**Last Updated:** 2025-10-08

---

## 🎯 Goal: Playable Battle System

Create a functional card battle system where player vs opponent battles can be tested end-to-end.

---

## ✅ Phase 1: Core Systems (COMPLETED)

### Data Layer
- [x] **CardData.cs** - Card data structure with costs, effects, upgrades
- [x] **CardEffect.cs** - Effect system (Damage, Resource, CardManipulation, StatusEffect)
- [x] **CardCost.cs** - Cost system with runtime modifiers
- [x] **Enums.cs** - All battle enums (CardType, EffectCategory, TargetType, etc.)
- [x] **StatusEffect.cs** - Status effect data structure

### Battle Logic
- [x] **BattleStats.cs** - Resolve/Composure/Hostility/AP tracking
- [x] **DeckManager.cs** - Deck/Hand/Discard/Exhaust zones with shuffling
- [x] **StatusEffectManager.cs** - Apply/remove status effects, modifiers, turn triggers
- [x] **EffectResolver.cs** - Resolve card effects with status modifiers integrated
- [x] **BattleManager.cs** - State machine, turn flow, victory conditions
- [x] **BattleState.cs** - Battle state enum (Initialize, TurnStart, PlayerTurn, etc.)

### Integration
- [x] StatusEffectManager integrated into EffectResolver (damage/composure/cost modifiers)
- [x] BattleManager calls StatusEffectManager on turn start/end
- [x] Card cost modifiers applied in BattleManager (Focus, Entangled)
- [x] **EffectResolverTest.cs** - Comprehensive test suite for effect resolution

---

## 🔥 Phase 2: Opponent AI (CRITICAL - IN PROGRESS)

### OpponentAI System
- [ ] **OpponentAI.cs** - Basic AI decision-making system
  - [ ] Card evaluation logic (score cards based on situation)
  - [ ] Play affordable cards in priority order
  - [ ] Stop when out of AP or no valid moves
  - [ ] Log AI decisions for debugging
- [ ] Integrate OpponentAI into BattleManager.OpponentTurnState
- [ ] Test AI vs player in battle

**Priority:** **CRITICAL** - Without this, battles cannot be tested.

**Estimated Complexity:** Medium (2-3 hours)

---

## 📦 Phase 3: Battle Setup & Content (NEXT)

### Starter Deck Creation
- [ ] **StarterDeckData.cs** - ScriptableObject for origin starter decks
- [ ] Create CardData assets for Faith Leader starter cards (10 cards)
- [ ] Create CardData assets for Nepo Baby starter cards (10 cards)
- [ ] Create CardData assets for Actor starter cards (10 cards)

### Origin Integration
- [ ] **OriginStats.cs** - Update/verify origin-specific battle stats
  - [ ] Faith Leader: 20 Resolve, 3 AP, +1 card draw passive
  - [ ] Nepo Baby: 20 Resolve, 4 AP passive
  - [ ] Actor: 20 Resolve, 3 AP, first card -1 AP passive
- [ ] Implement origin passive abilities in BattleManager
  - [ ] Faith Leader: Draw 6 cards instead of 5 at battle start
  - [ ] Nepo Baby: Start with 4 AP instead of 3
  - [ ] Actor: First card each turn costs -1 AP

### Battle Setup
- [ ] **BattleSetupData.cs** - ScriptableObject for configuring battles
- [ ] Create test battle setups (player vs opponent with starter decks)

**Priority:** High - Needed to test battles with real content.

**Estimated Complexity:** Medium (3-4 hours)

---

## 🎨 Phase 4: Minimal Battle UI (COMPLETED)

### Core Battle Display
- [x] **BattleUI.cs** - Main battle UI controller
  - [x] Display player Resolve/Composure/Hostility/AP
  - [x] Display opponent Resolve/Composure/Hostility/AP
  - [x] Show current turn number and phase
  - [x] End Turn button
  - [x] Connected to BattleManager via EventBus
- [x] **CardButton.cs** - Display cards in hand as buttons
  - [x] Show card name, cost, description
  - [x] Click to play card
  - [x] Card type color coding (Diplomacy/Hostility/Manipulate)
- [x] **Battle Log** - Text-based combat log
  - [x] Shows turn changes, cards played, battle events
  - [x] Auto-scrolls to latest entry

### Battle Flow UI
- [x] Victory/defeat panels
- [ ] Battle start screen (optional for testing)
- [ ] Card play feedback animations (optional for testing)

**Priority:** High - Needed to actually play battles.

**Status:** ✅ Core UI Complete - Ready for Unity setup

---

## 🧪 Phase 5: Testing & Polish

### Battle Testing
- [ ] Create test scene with BattleManager
- [ ] Test all 3 origins vs each other
- [ ] Test all card effects work correctly
- [ ] Test all status effects work correctly
- [ ] Balance testing (are battles too fast/slow?)

### Bug Fixes & Polish
- [ ] Fix any bugs found during testing
- [ ] Improve AI decision-making if too dumb
- [ ] Add basic VFX/SFX for card plays (optional)
- [ ] Performance optimization if needed

**Priority:** Medium - Needed before moving to meta-game.

**Estimated Complexity:** Variable (depends on bugs found)

---

## 🎮 Phase 6: Meta-Game Systems (FUTURE)

### Battle Rewards
- [ ] **BattleRewardManager.cs** - Post-battle reward calculation
  - [ ] Calculate rewards based on Hostility used
  - [ ] Diplomatic victory (low Hostility): Ally created, moderate rewards
  - [ ] Aggressive victory (high Hostility): Enemy created, high rewards, +Heat
- [ ] **CardRewardUI.cs** - Card selection screen (choose 1 of 3)

### Ally/Enemy System
- [ ] **AllyData.cs** - Ally character data with signature cards
- [ ] **EnemyData.cs** - Enemy character data
- [ ] **RelationshipManager.cs** - Track allies/enemies
- [ ] Ally passive bonuses in battles
- [ ] Enemy interference in battles

### Campaign/Map System
- [ ] **MapManager.cs** - Slay the Spire-style node map
- [ ] **MapNode.cs** - Battle/Event/Shop/Rest nodes
- [ ] **CampaignManager.cs** - Campaign progression and win condition
- [ ] Time limit before election day

**Priority:** Low - Meta-game layer, do after battle system works.

**Estimated Complexity:** High (10-15 hours)

---

## 📊 Current Status

### Completion Summary
- **Phase 1 (Core Systems):** ✅ 100% Complete (15/15 tasks)
- **Phase 2 (Opponent AI):** ⏸️ Skipped for now (opponent auto-passes turn)
- **Phase 3 (Battle Setup):** ⏳ 0% Complete (0/8 tasks)
- **Phase 4 (Battle UI):** ✅ 100% Complete (7/7 core tasks)
- **Phase 5 (Testing):** ⏳ 0% Complete (0/5 tasks)
- **Phase 6 (Meta-Game):** ⏳ 0% Complete (0/8 tasks)

**Overall Progress:** 22/46 tasks complete (48%)

### Next Immediate Tasks
1. ✅ ~~EffectResolver integration~~ (DONE)
2. ✅ ~~Basic battle UI~~ (DONE)
3. 📦 **Starter deck content creation** (CURRENT - blocking playable battles)
4. 🎨 Unity scene setup with BattleUI prefab
5. 🔥 OpponentAI implementation (can test vs passing opponent first)

---

## 🎯 Definition of Done: Phase 2 (Opponent AI)

We can move to Phase 3 when:
- [ ] OpponentAI can play a full turn (select and play cards)
- [ ] AI ends turn when out of AP or no playable cards
- [ ] AI vs AI battle completes without errors
- [ ] AI makes reasonable decisions (doesn't play cards randomly)

---

## 🎯 Definition of Done: Playable Battle

We can test battles end-to-end when:
- [ ] OpponentAI works (Phase 2)
- [ ] All 3 starter decks created (Phase 3)
- [ ] Basic battle UI works (Phase 4)
- [ ] Can start battle, play cards, see AI respond, win/lose

---

## Notes

- **Keep it simple:** Focus on getting battles playable first, polish later
- **Test as you go:** Use EffectResolverTest pattern for new systems
- **Origin passives:** Need special handling in BattleManager for unique abilities
- **UI can be ugly:** Text-based UI is fine for testing, make it pretty later
- **Meta-game is separate:** Don't get distracted by campaign features yet

