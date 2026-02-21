# 2D Card System — Unity Setup Guide

The 3D card system (Card3DView, CardHandManager, CardInputHandler, CardPrefabSetup, BattleCardHandBridge) has been removed. The `CardButton` component is now the single card view — it handles artwork, frames, hover, affordability dimming, and MMFeedbacks.

This guide walks through creating the `CardButton` prefab and wiring up the `BattleUI` scene.

---

## What Was Removed

| File | Status |
|------|--------|
| `Card3DView.cs` | ✅ Deleted |
| `CardHandManager.cs` | ✅ Deleted |
| `CardInputHandler.cs` | ✅ Deleted |
| `CardPrefabSetup.cs` | ✅ Deleted |
| `BattleCardHandBridge.cs` | ✅ Deleted |
| `Card.prefab` (3D) | ✅ Deleted |

**Nothing in battle logic changed.** BattleManager, DeckManager, EffectResolver, BattleStats, BattleEvents — all untouched.

---

## Step 1: Create the CardButton Prefab

### Hierarchy Structure

In the **Project** window, right-click → `Create Empty` to start, or build it in the scene and drag to prefabs.

```
CardButton (root)
├── [CardButton.cs]  ← your script
├── [CanvasGroup]    ← for affordability dimming (alpha)
│
├── Background       (Image — solid card background, e.g. dark gray)
│
├── TypeFrame        (Image — type border, set by CardVisualSettings)
│
├── Artwork          (Image — card illustration sprite)
│
├── RarityOverlay    (Image — rarity glow/badge, optional)
│
├── Header           (empty RectTransform — top of card)
│   ├── CardName     (TMP_Text)
│   └── CostBadge    (empty)
│       └── CostText (TMP_Text — shows "1" "2" "Free")
│
├── Description      (TMP_Text — card effect text)
│
├── FlavorText       (TMP_Text — italic, smaller, bottom)
│
└── TypeStrip        (Image — thin colored strip, top or bottom edge)
```

### Recommended RectTransform Sizes

| Element | Recommended Size | Notes |
|---------|-----------------|-------|
| CardButton root | 120 × 170 px | ~2:3 portrait ratio |
| Artwork | 110 × 80 px | Upper portion |
| TypeFrame | 120 × 170 px | Full card, Image Type = Sliced |
| RarityOverlay | 120 × 170 px | Full card, additive blend |
| CardName | 110 × 20 px | Below artwork |
| CostText | 24 × 24 px | Top-left corner |
| Description | 110 × 50 px | Middle |
| FlavorText | 110 × 25 px | Bottom, italic |
| TypeStrip | 120 × 6 px | Bottom edge |

---

## Step 2: Wire Up CardButton Inspector Fields

Select the `CardButton` root and assign in the Inspector:

### Card Art
| Field | Assign |
|-------|--------|
| `Artwork Image` | The `Artwork` Image child |
| `Type Frame Image` | The `TypeFrame` Image child |
| `Rarity Overlay Image` | The `RarityOverlay` Image child (can leave null) |

### Card Text
| Field | Assign |
|-------|--------|
| `Card Name Text` | `Header/CardName` TMP_Text |
| `Card Cost Text` | `Header/CostBadge/CostText` TMP_Text |
| `Card Description Text` | `Description` TMP_Text |
| `Flavor Text` | `FlavorText` TMP_Text (can leave null) |

### Card Type Strip
| Field | Assign |
|-------|--------|
| `Card Type Strip` | `TypeStrip` Image |

### Visual Settings
| Field | Assign |
|-------|--------|
| `Visual Settings` | Drag in your `CardVisualSettings` ScriptableObject |

> **If you don't have art yet:** Leave `Visual Settings` null. Type colors (green/red/purple) on the strip will still work with the default colors.

### Hover Behaviour
| Field | Default | Notes |
|-------|---------|-------|
| `Hover Scale` | 1.12 | 12% bigger on hover |
| `Hover Lerp Speed` | 12 | Animation smoothness |
| `Hover Lift Pixels` | 20 | Upward movement in px |

### Feedbacks (all optional — skip for now if no MMFeedbacks configured)
| Field | What It Plays On |
|-------|-----------------|
| `Draw Feedback` | Card enters hand |
| `Hover Enter Feedback` | Mouse enters card |
| `Hover Exit Feedback` | Mouse leaves card |
| `Select Feedback` | Card clicked |
| `Discard Feedback` | Card leaves hand |

Leave these null for now — the card works without them.

---

## Step 3: Configure the Layout Container

In your **Battle scene**, the hand container needs a layout component. `BattleUI` already has `cardButtonContainer` — configure it:

### Option A: Horizontal Layout (Simplest)

On `cardButtonContainer`:
- Add `Horizontal Layout Group`
  - Child Alignment: Lower Center
  - Spacing: −20 (negative spacing overlaps cards slightly, like a real hand)
  - Child Force Expand Width: ❌ OFF
  - Child Force Expand Height: ❌ OFF
- Add `Content Size Fitter`
  - Horizontal Fit: Preferred Size

This gives a simple hand that grows left-to-right. Good for MVP.

### Option B: Fixed-Width Fan (Better Feel)

On `cardButtonContainer`:
- Fixed RectTransform width (e.g. 700px)
- **No** layout group
- Cards are positioned by script in an arc

> For now, **use Option A**. Fan layout is a Phase C polish item.

---

## Step 4: Wire Up BattleUI

`BattleUI.cs` already works. Just confirm the Inspector references:

| Field | Assign |
|-------|--------|
| `Card Button Container` | The hand container RectTransform |
| `Card Button Prefab` | The `CardButton` prefab you just made |
| (all stat text fields) | Same as before |
| (victory/defeat panels) | Same as before |

> The old `BattleCardHandBridge` field is gone — remove it from any scene objects if it's still referenced.

---

## Step 5: Test It

1. Open your battle test scene
2. Press Play
3. Click the "Start Battle" button (or whatever your test initiator is)
4. Cards should appear in the hand container as 2D UI buttons
5. Hover over a card — it should scale up and lift
6. Click a card — it should fire `PlayCardRequestedEvent`
7. Cards you can't afford (not enough AP) should be dimmed

---

## Prefab Variants (Future)

Once the base `CardButton` prefab works, create prefab variants for:

| Variant | Purpose |
|---------|---------|
| `CardButton_Diplomacy` | Pre-assigned green frame, used for shop display |
| `CardButton_Reward` | Larger size for post-battle card choice screen |
| `CardButton_Deck` | Small thumbnail for deck review UI |

---

## What's Still Needed After This

| Item | Priority |
|------|---------|
| Card art sprites assigned to CardData assets | High — looks like placeholder until done |
| CardVisualSettings filled with frame sprites | High — frames won't show without sprites |
| Starter deck CardData assets (30 cards) | Critical — battles need real content |
| OpponentAI | Critical — opponents can't play cards yet |

---

## Troubleshooting

**Cards don't appear in hand**
→ Check that `cardButtonContainer` and `cardButtonPrefab` are assigned in BattleUI Inspector.

**Cards appear but artwork is blank**
→ CardData assets don't have artwork sprites assigned yet. Expected — assign sprites later.

**Cards are dimmed even though you have AP**
→ Check that `BattleManager.PlayerStats.CurrentActionPoints` is returning the right value at the time `UpdateHandDisplay()` runs.

**Hover doesn't animate**
→ Confirm CardButton has a `CanvasGroup` component on the root. The hover works on the RectTransform — no physics needed.

**Old BattleCardHandBridge missing script errors in scene**
→ Find any GameObjects in your scene with the missing script and remove the component. The Bridge is gone and no longer needed.

**`Card.prefab` missing reference in scene**
→ Remove any field references to the old Card prefab. The 3D card prefab is deleted.
