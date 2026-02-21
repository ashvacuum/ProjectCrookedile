# Battle System Implementation Tasks

**Last Updated:** 2026-02-21

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

## 📦 Phase 3: Battle Setup & Content

### Starter Deck Creation
- [x] **CardData assets exist** — all 19 unique cards in `Assets/Resources/Cards/`
  - [x] Faith Leader cards: Find Common Ground, Blessing, Accusation, Deflect, Gather Thoughts
  - [x] Nepo Baby cards: Family Name, Inherited Privelege, Pull Strings, Call In Favor, Backroom Deal, Dynasty Network, Trust Fund
  - [x] Actor cards: Charming Gambit, All or Nothing, Bold Accusation, Spotlight Hog, High Stakes, Ego Trip, Fan Favorite
- [x] **CardDatabase.asset** — all 19 cards registered
- [x] **BattleTestStarter.cs** — loads cards by name, builds decks, fires StartBattle() — no Unity Editor work needed to test
- [ ] **Run CardDataFixer** — `Tools → Crookedile → Fix Starter Card Data` to stamp IsStarterCard, origin tags, and descriptions on all 19 assets (one click in Unity Editor)
  - Note: typo in asset — "Inherited Privelege" (double e). Fix filename later after fixer runs.

### Origin Integration
- [x] **OriginStats.cs** — Faith Leader (20R/3AP), Nepo Baby (20R/4AP), Actor (20R/3AP) — stats confirmed
- [ ] Implement origin passive abilities in BattleManager
  - [ ] Faith Leader: Draw 6 cards instead of 5 at battle start
  - [ ] Nepo Baby: Start with 4 AP instead of 3 ← OriginStats sets this, but BattleManager ignores it currently
  - [ ] Actor: First card each turn costs -1 AP

### Battle Setup
- [x] **BattleSetup (inline class in BattleManager)** — accepts playerDeck, opponentDeck, originStats, origin types
- [x] **BattleTestStarter.cs** — replaces need for BattleSetupData ScriptableObject for testing

**Priority:** High — Cards exist, BattleTestStarter bypasses remaining gaps.

**Immediate next step:** Add BattleTestStarter to your test scene → Press Play → Battle runs.

---

## 🎨 Phase 4: Battle UI — 2D System (COMPLETED)

> **Note:** Switched from 3D card system to 2D sprite/Canvas system.
> Removed: Card3DView, CardHandManager, CardInputHandler, CardPrefabSetup, BattleCardHandBridge, Card.prefab
> See **[CARD_2D_SETUP.md](CARD_2D_SETUP.md)** for Unity Editor prefab setup instructions.

### Core Battle Display
- [x] **BattleUI.cs** - Main battle UI controller
  - [x] Display player Resolve/Composure/Hostility/AP
  - [x] Display opponent Resolve/Composure/Hostility/AP
  - [x] Show current turn number and phase
  - [x] End Turn button
  - [x] Connected to BattleManager via EventBus
  - [x] Passes current AP to cards on hand refresh
  - [x] Calls PlayDrawAnimation() on each card when hand is built
- [x] **CardButton.cs** - Full 2D card component (upgraded)
  - [x] Show card name, cost, description, flavor text
  - [x] Show card artwork (Image component)
  - [x] Show type frame (from CardVisualSettings)
  - [x] Show rarity overlay (from CardVisualSettings)
  - [x] Card type color strip (Diplomacy/Hostility/Manipulate)
  - [x] Click to play card (IPointerClickHandler)
  - [x] Hover scale + lift animation (IPointerEnterHandler/ExitHandler)
  - [x] Affordability dimming (CanvasGroup alpha, grays out if not enough AP)
  - [x] MMFeedbacks hooks (draw, hoverEnter, hoverExit, select, discard)
  - [x] RefreshVisuals(int ap) — updates affordability mid-turn without rebuilding
- [x] **Battle Log** - Text-based combat log
  - [x] Shows turn changes, cards played, battle events
  - [x] Auto-scrolls to latest entry

### Battle Flow UI
- [x] Victory/defeat panels
- [ ] CardButton prefab built in Unity Editor (see CARD_2D_SETUP.md)
- [ ] Battle start screen (optional for testing)

**Priority:** High - Needed to actually play battles.

**Status:** ✅ Code complete — Unity prefab setup remaining (see CARD_2D_SETUP.md)

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
- **Phase 2 (Opponent AI):** 🔥 0% Complete — **CURRENT CRITICAL BLOCKER**
- **Phase 3 (Battle Setup):** ✅ ~85% Complete — Cards exist + BattleTestStarter ready; only Editor one-click + origin passives remain
- **Phase 4 (Battle UI):** ✅ Code 100% Complete — Unity prefab setup remaining (CARD_2D_SETUP.md)
- **Phase 5 (Testing):** ⏳ 0% Complete (0/5 tasks)
- **Phase 6 (Meta-Game):** ⏳ 0% Complete (0/8 tasks)

**Overall Progress:** ~30/46 tasks complete (~65%)**

### Next Immediate Tasks
1. ✅ ~~EffectResolver integration~~ (DONE)
2. ✅ ~~Basic battle UI (2D system)~~ (DONE — CardButton.cs fully upgraded)
3. ✅ ~~Starter deck content~~ (DONE — 19 assets confirmed, BattleTestStarter.cs ready)
4. ✅ ~~CardDataFixer editor tool~~ (DONE — `Tools → Crookedile → Fix Starter Card Data`)
5. 🎨 **Unity prefab setup** — Build CardButton prefab per CARD_2D_SETUP.md; add BattleTestStarter to test scene; press Play
6. 🔥 **OpponentAI.cs** — Implement card evaluation + turn loop (blocking real battles)
7. ⚙️ **Origin passives** — Faith Leader +1 draw, Nepo Baby 4AP, Actor first card -1AP

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
- [ ] OpponentAI works (Phase 2) ← **only remaining code blocker**
- [x] All 3 starter decks created (Phase 3) ← BattleTestStarter bypasses tagging gap
- [x] Basic battle UI works (Phase 4) ← Code complete; prefab wiring is Unity Editor work
- [ ] Can start battle, play cards, see AI respond, win/lose

---

## Notes

- **Keep it simple:** Focus on getting battles playable first, polish later
- **Test as you go:** Use EffectResolverTest pattern for new systems
- **Origin passives:** Need special handling in BattleManager for unique abilities
- **UI can be ugly:** Text-based UI is fine for testing, make it pretty later
- **Meta-game is separate:** Don't get distracted by campaign features yet

