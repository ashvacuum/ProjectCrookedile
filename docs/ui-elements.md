# UI Elements Build List

> **Kind:** Tracking · **Status:** Living · **Updated:** 2026-10-09
>
> **Summary:** The battle UI broken into reusable prefabs, smallest first, as a checklist to build against.
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

## Cleanup

- [ ] Delete the player's `Resolve` block (`Hp bar`, `ComposureImage`, `ResolveText`) from `Player Info Panel`.
  HP and composure are gone; no script references it.

## Open questions

- **LOG and Media Training:** Secondary buttons, or their own small icon-button style?
