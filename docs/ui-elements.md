# UI Elements Build List

> **Kind:** Tracking · **Status:** Living · **Updated:** 2026-11-21
>
> **Summary:** The battle UI broken into reusable prefabs, smallest first, plus the mock's unbuilt features, the screens outside battle and the build order.
>
> **Source of truth:** this doc · **Related:** [`art-bible.md`](art-bible.md) · [`ui-vfx.md`](ui-vfx.md)

*Target layout: [`design/battle-ui-reference.png`](design/battle-ui-reference.png). Each element is one prefab, so a
style change is made once and reaches every use. Build top to bottom: every level is assembled only from the levels
above it.*

**Rules**

- Style variations are **prefab variants** of one base (Secondary button is a variant of Primary), so restyling the
  base restyles them all.
- Text style is not a prefab. Labels, numbers and titles use **TMP style sheet** styles (next to the existing
  `Keyword` style), so one edit restyles every text.
- A finished element replaces every hand-built copy listed under "Replaces". Tick it only when no copy is left.
- Prefabs are look only. Behaviour stays on the panel scripts, which all subclass `UIPanel` (battle panels
  `BattlePanel`) and wire themselves, so swapping a hand-built copy for a prefab needs no binding code. Checked
  2026-11-21 after the UIPanel rework: no element below is built yet.

---

## 1. Basic pieces

- [ ] **Button / Primary**: large mustard button with a label.
  Replaces `End Turn Button` (`Player Info Panel`). Used by: End Turn.
- [ ] **Button / Secondary** (variant of Primary): small button.
  Replaces `Confirm` (`CardChoicePanel`, `BattleResultPanel`), `Continue Btn`, `Cancel Button`, `Close Btn`
  (`CardZonePanel`), and the Media Training button `BattleUI` builds in code when none is assigned.
  Used by: every popup, LOG, Media Training.
- [ ] **Counter**: round frame, a number, a small label underneath.
  Replaces `Deck`, `Discard`, `Exhaust` (hand-made button + text each) and the `AP` text.
  Used by: Energy, Draw, Discard, Exhaust (bottom left).
- [ ] **Chip**: icon with a number.
  Replaces `StatusEffectUI`. Used by: status chips, the class resource tray, carried items (top left).
- [ ] **Tag**: coloured pill with text.
  Replaces `Stance Badge`, the enemy `Name` text. Used by: stance tag (HOSTILE / NEUTRAL / RECEPTIVE), name plate.
- [ ] **Bar**: background, fill, optional text.
  Replaces the bars in `OpinionBarContainer`. Used by: Opinion bar, both halves of the Hostility Bar.
- [ ] **Panel Frame**: 9-sliced dark walnut with a fine gold edge.
  Replaces the background Image of every panel. Used by: every panel and popup.

## 2. Groups

- [ ] **Intent Tag**: icon plus one word or number. Replaces `EnemyIntent` (`BaseEnemyPrefab`).
- [ ] **Status Row**: layout group of Chips, at most two shown. Replaces `Status Effect Panel` (player) and
  `StatusEffectPanel` (enemy).
- [ ] **Hostility Bar**: two Bars meeting at a centre zero mark. The receptive fill (green) grows left, the hostile
  fill (red) grows right (`EnemySlotUI._receptiveFill` / `_hostileFill`). Replaces the enemy's `Resolve` block,
  whose `HP Bar` and `ComposureBar` are those two fills under their old names.
- [ ] **Opinion Meter**: Bar, round Support badge (left), round Denial badge (right), "OPINION 62" readout and the
  win/lose line. Replaces `OpinionMeter` (`Enemy Panel`).
- [ ] **Scroll List**: scroll view with its scrollbars. Replaces the copies in `CardChoicePanel`, `CardZonePanel`
  and `Discard`.
- [ ] **Popup Shell**: Panel Frame, title, Scroll List, Secondary buttons. `CardChoicePanel`, `CardZonePanel`,
  `Discard` and `BattleResultPanel` become variants of it.

## 3. Screen sections

- [ ] **Enemy Slot** (`BaseEnemyPrefab`): Intent Tag, portrait, name Tag, stance Tag, Hostility Bar, Status Row.
- [ ] **Top Bar**: carried-item Chips, Opinion Meter, "TURN 4 · JUDGMENT IN 8" readout (`Base Phase Panel`), LOG
  button. LOG shows and hides `BattleLogPanel`: the scrolling battle record, each card play or enemy action with
  its outcomes indented under it. Nothing opens the panel yet.
- [ ] **Player HUD**: resource tray of Chips above the hand, the bottom-left Counters, End Turn with its three
  diamond pips.
- **Card**: already split into per-type variants; no work.

---

## 4. Mock features with no code yet

Sections 1–3 restyle what the battle already does. These four appear in the mock but nothing in the game feeds them;
each waits on its open question below.

- [ ] **Carried allies**: the "CARRYING" row of ally Chips in the Top Bar, each with a tooltip (`RunState.Allies`).
- [ ] **Class resource tray**: the Chips above the hand ("Congregation", "Tithe Drive" in the mock).
- [ ] **End Turn pips**: the three diamonds beside End Turn.
- [ ] **Opinion chevrons**: the arrow run on the Opinion Meter where Support meets Denial.

## 5. Outside the battle

- [ ] **Campaign scene**: give `campaign.unity` a `UIRouter` and rebuild its HUD and popups (events, ally pick, card
  reward) from the same elements. Replaces `CampaignFlow`'s IMGUI header readout.
- [ ] **Class select** and the **first-fight "How it works" overlay**: built from Popup Shell, Buttons and Panel
  Frame.

---

## Build order

Code can run ahead of art: every element is built with greybox sprites, and the 9-slice kit and icons drop into the
base prefabs later (art-bible §0.5 for sizes).

1. **Basic pieces** (§1) and the TMP text styles. Move `CardZonePanel` onto `UIView` at the same time, so every
   battle popup goes through `UIRouter` (dimmer, ESC, input blocking) before Popup Shell wraps them.
2. **Groups** (§2), then make the four popups Popup Shell variants.
3. **Screen sections** (§3) swapped into `main.unity`. LOG opens a uGUI `BattleLogPanel`; its IMGUI debug draw goes.
4. **Mock features** (§4), once their questions are answered.
5. **Steam Deck layout**: check every section at 1280×800 (16:10). Hand and target navigation already exist
   (`BattleGamepadInput`); what's left is layout and focus visuals.
6. **Outside the battle** (§5).

## Cleanup

- [x] Delete the player's `Resolve` block (`Hp bar`, `ComposureImage`, `ResolveText`) from `Player Info Panel`.
  HP and composure are gone; no script references it.
- [ ] Replace the IMGUI draws (`BattleLogPanel`, `CampaignFlow`): IMGUI receives no input under the Input System,
  so neither can be clicked.

## Open questions

- **LOG and Media Training:** Secondary buttons, or their own small icon-button style?
- **End Turn pips:** what do they count? Energy already reads "3/3" under the Energy counter.
- **Opinion chevrons:** this turn's projected push from enemy intents, or momentum?
- **Resource tray:** what each class shows. Faith Leader's mock shows Congregation and Tithe Drive: Policies in play,
  or class counters? What do Nepo Baby and Celebrity show?
- **Campaign scene:** part of this UI push, or its own track after the battle UI?
