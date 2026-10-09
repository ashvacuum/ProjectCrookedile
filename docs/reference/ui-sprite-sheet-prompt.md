# UI sprite sheet prompts

> **Kind:** Reference · **Status:** Draft · **Updated:** 2026-10-09
>
> **Summary:** Image prompts for the battle UI sprite sheets (frames, buttons, badges, bars, icons), built from the battle UI reference.
>
> **Source of truth:** this doc · **Related:** [`battle-ui-mock-prompt.md`](battle-ui-mock-prompt.md) · [`../ui-elements.md`](../ui-elements.md) · [`../art-bible.md`](../art-bible.md)

Attach `docs/design/battle-ui-reference.png` as the style reference for every sheet. Each sheet covers one group of
[`ui-elements.md`](../ui-elements.md); generating them separately keeps the shapes consistent within a sheet. Drop
results beside this file as `ui-sheet-a.png` … `ui-sheet-d.png`.

These sheets replace `Assets/Art/UI_from_mock/`, which holds crops of an older mock with text baked in, no alpha
and no 9-slice; delete that folder once the sheets are in.

## Palette (sampled from the reference)

| Role | Hex |
|---|---|
| Panel base, dark walnut | `#1C1A15` |
| Panel wood, lighter | `#382C1E` |
| Fine gold edge | `#5C462E` (shadow) to `#C99138` (lit) |
| Mustard (End Turn, Opinion fill, Support) | `#C78E33` to `#D29938` |
| Brick (Hostile, Denial) | `#6A2915` to `#7C3527` |
| Olive (Neutral) | `#949675` |
| Teal (Receptive) | `#436265` |
| Cream (text, light chips) | `#EEDEC7` |
| Opinion empty | `#181812` |

## Rules for every sheet

Shared by all four prompts below; paste this paragraph at the end of each.

> Flat game UI sprite sheet in the style of the attached Project Crookedile battle screen: flat cel shading, thin
> clean lines, dusty muted palette, dark walnut with a fine gold edge. Front-on, no perspective, no drop shadow
> outside the shape, no lighting gradient beyond a subtle bevel. Every element sits alone on a flat solid magenta
> (#FF00FF) background with generous empty space around it, aligned to an even grid. No text, no letters, no
> numbers, no logos. NOT photoreal, NOT glossy, NOT skeuomorphic leather or metal, NOT neon, NOT painterly.

## Sheet A: frames and buttons

> A sheet of rectangular UI frames and buttons. Row 1: a large empty panel frame, dark walnut fill with a fine
> gold inner edge and slightly rounded corners; the same frame at a smaller size. Row 2: a wide mustard button with
> a thin dark outline, in four states left to right: normal, hover (slightly brighter), pressed (slightly darker,
> inset), disabled (desaturated grey-mustard). Row 3: a small dark walnut button with a fine gold edge in the same
> four states. Row 4: a small dark tray, walnut with a fine gold edge, wider than tall. Plain fills so the centre
> can stretch; all decoration stays within the outer 12 pixels of each shape.

Feeds: Panel Frame, Button / Primary, Button / Secondary, the resource tray.

## Sheet B: round pieces

> A sheet of round UI pieces, all the same diameter unless stated. Row 1: an empty round counter, cream fill with a
> thin dark ring; an empty round chip, dark walnut with a fine gold ring; the same chip at half size. Row 2: a large
> round badge with a thick mustard ring and a dark centre holding a simple gold crowd-of-people icon; the same badge
> with a brick-red ring and a brick crowd icon. Row 3: a diamond pip filled mustard, and the same diamond as an empty
> mustard outline.

Feeds: Counter, Chip, the Support and Denial badges, the End Turn pips.

## Sheet C: bars and tags

> A sheet of horizontal UI strips. Row 1: a long empty bar trough, near-black fill with a fine gold edge; a plain
> flat fill strip in pale cream the same height as the trough's inside. Row 2: a row of three chevrons pointing
> right in cream, and the same pointing left in brick red. Row 3: a short thin trough with a small vertical tick
> mark at its centre. Row 4: a small rounded pill tag in pale cream with a thin darker edge; a wider flat name plate,
> light wood with a thin dark edge; a small dark intent box with a fine gold edge, wider than tall.

Feeds: Bar, the Opinion chevrons, the Hostility Bar, Tag, the name plate, Intent Tag. The pill tag and fill strip are
pale on purpose: Unity tints them (Hostile, Neutral and Receptive tags, the green and red hostility fills), so one
sprite serves every colour.

## Sheet D: icons

> A sheet of small flat single-colour icons in cream, each in its own square cell with bold readable silhouettes:
> arrow pointing down, arrow pointing up, shield, two arrows curving into each other (convert), three short
> horizontal dashes (waits), vertical sound bars (riles), a crowd of three people, a scroll (log), a hamburger menu
> (three lines), a coin with a P (Tithe Drive).

Feeds: Intent Tag icons, Chip icons, the LOG button. Icons are cream so Unity can tint them.

## Bringing a sheet into Unity

1. Remove the magenta background (an image editor's colour-select works well with the flat fill); save as PNG with
   transparency in `Assets/Art/UI/`.
2. Import: Texture Type **Sprite (2D and UI)**, Sprite Mode **Multiple**, Mesh Type **Full Rect**, no mipmaps,
   compression as [`art-bible.md`](../art-bible.md) §0.5.
3. Sprite Editor: **Slice → Automatic**, then name each sprite after its element (`button-primary-normal`, …).
4. Set **9-slice borders** on everything that stretches (frames, buttons, troughs, tray, tags, name plate, intent
   box), then use them as Image Type **Sliced** so one sprite fits any size.
5. Wire the sprites into the element prefabs from [`ui-elements.md`](../ui-elements.md). Button states go in the
   Button's **Sprite Swap** transition.
