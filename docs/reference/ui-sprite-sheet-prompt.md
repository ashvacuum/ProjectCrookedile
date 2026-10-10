# UI sprite sheet prompts

> **Kind:** Reference · **Status:** Draft · **Updated:** 2026-10-10
>
> **Summary:** Image prompts for the battle UI sprite sheets (frames, buttons, badges, bars, icons), built from the battle UI reference.
>
> **Source of truth:** this doc · **Related:** [`battle-ui-mock-prompt.md`](battle-ui-mock-prompt.md) · [`../ui-elements.md`](../ui-elements.md) · [`../art-bible.md`](../art-bible.md)

Attach `docs/design/battle-ui-reference.png` as the style reference for every sheet. Each sheet covers one group of
[`ui-elements.md`](../ui-elements.md); generating them separately keeps the shapes consistent within a sheet. Every
prompt is self-contained: paste one whole block per generation. Drop results beside this file as `ui-sheet-a.png` …
`ui-sheet-d.png`.

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

## Layout

Every sheet is a transparent PNG on an exact grid of equal cells. Each element is centred in its own cell and keeps
at least 16 px of empty space to the cell border, so cells never touch and Unity can slice them apart. Empty cells
stay fully transparent.

| Sheet | Canvas | Grid | Cell |
|---|---|---|---|
| A: frames and buttons | 2048 × 768 | 4 columns × 3 rows | 512 × 256 |
| B: round pieces | 1024 × 512 | 4 columns × 2 rows | 256 × 256 |
| C: bars and tags | 2048 × 512 | 2 columns × 4 rows | 1024 × 128 |
| D: icons | 512 × 384 | 4 columns × 3 rows | 128 × 128 |

---

## Sheet A: frames and buttons

> Create a game UI sprite sheet for Project Crookedile, matching the attached battle screen exactly in style: flat
> cel shading, thin clean line work, a dusty muted palette, dark walnut panels with a fine gold edge. This is a
> production asset sheet, not an illustration.
>
> **Canvas and grid.** The image is exactly 2048 × 768 pixels with a fully transparent background (alpha 0) everywhere
> outside the shapes: no background colour, no paper texture, no vignette, no shadow on the background. Divide it into
> an invisible grid of 4 columns × 3 rows of 512 × 256 pixel cells. Do not draw the grid. Centre each element in its
> cell and keep at least 16 pixels of empty transparent space between the element and its cell border. Nothing may
> cross into a neighbouring cell. Cells not listed stay completely empty and transparent.
>
> **Row 1, column 1: panel frame.** A rectangle 480 × 224 pixels with 12 pixel rounded corners. Fill flat dark walnut
> #1C1A15. A 2 pixel outer rim in dark brown #5C462E, then 6 pixels inside it a fine 3 pixel gold line #C99138 that
> follows the rounded corners. All detail stays within the outer 24 pixels; the centre is one flat colour so it can
> stretch.
>
> **Row 1, column 2: tray.** A rectangle 400 × 160 pixels with 10 pixel rounded corners, the same construction as the
> panel frame: flat #1C1A15 fill, 2 pixel #5C462E rim, a 2 pixel #C99138 gold line 5 pixels inside. Flat centre.
>
> **Row 1, columns 3 and 4:** empty.
>
> **Row 2: primary button, four states left to right.** Each is a rectangle 448 × 160 pixels with 10 pixel rounded
> corners and a 2 pixel dark outline #2A1E12. Column 1, normal: flat mustard fill #C78E33, a 3 pixel lighter band
> #E0AE55 along the inside of the top edge, a 3 pixel darker band #9C6C24 along the inside of the bottom edge. Column 2,
> hover: the same shape with fill #D6A040 and top band #EBC06A. Column 3, pressed: fill #B07C2A, the darker band on the
> top edge and the lighter band on the bottom, so it reads as pushed in. Column 4, disabled: fill desaturated grey
> mustard #8C8070, outline #4A4238, no light or dark bands. No text, no icon. All bands stay within the outer 20
> pixels; the centre is flat.
>
> **Row 3: secondary button, four states left to right.** Each is a rectangle 320 × 112 pixels with 8 pixel rounded
> corners, a 2 pixel near-black outline #120F0B and a fine 2 pixel gold line #C99138 inset 4 pixels. Column 1, normal:
> fill #2A2219. Column 2, hover: fill #362C20, gold line brighter #DDA94C. Column 3, pressed: fill #1C1712, gold line
> #A67830. Column 4, disabled: fill #2A2724, line dull #5C5248. No text, no icon. Flat centre; detail within the
> outer 16 pixels.
>
> **Style rules.** Front-on, perfectly straight edges, no perspective, no tilt. No drop shadow outside a shape, no
> glow, no lighting gradient beyond the bands described. No text, letters, numbers, logos or watermarks anywhere.
> Same corner radius and line weight for every state of a button. NOT photoreal, NOT glossy, NOT metallic, NOT
> leather, NOT neon, NOT painterly, NOT grunge, NOT 3D.

Feeds: Panel Frame, tray, Button / Primary, Button / Secondary.

## Sheet B: round pieces

> Create a game UI sprite sheet for Project Crookedile, matching the attached battle screen exactly in style: flat
> cel shading, thin clean line work, a dusty muted palette, dark walnut with a fine gold edge. This is a production
> asset sheet, not an illustration.
>
> **Canvas and grid.** The image is exactly 1024 × 512 pixels with a fully transparent background (alpha 0) everywhere
> outside the shapes: no background colour, no texture, no vignette. Divide it into an invisible grid of 4 columns × 2
> rows of 256 × 256 pixel cells. Do not draw the grid. Centre each element in its cell with at least 16 pixels of
> empty transparent space around it. Nothing crosses into a neighbouring cell. Cells not listed stay empty and
> transparent.
>
> **Row 1, column 1: counter.** A perfect circle 200 pixels across. Flat dark fill #1E1C18, a 3 pixel cream ring
> #D8CBB0 on the outer edge. Empty centre: no number, no text.
>
> **Row 1, column 2: chip.** A perfect circle 120 pixels across. Flat dark walnut fill #1C1A15, a 3 pixel gold ring
> #C99138 on the outer edge. Empty centre.
>
> **Row 1, column 3: Support badge.** A perfect circle 220 pixels across: a thick 16 pixel mustard ring #C78E33 with a
> 2 pixel dark outline #2A1E12 outside it, and a flat dark centre #232019. In the centre, a simple flat silhouette of
> three people standing shoulder to shoulder (one in front, two slightly behind), in mustard #C78E33, about half the
> badge's width. No text.
>
> **Row 1, column 4: Denial badge.** Identical in size and construction to the Support badge, but the ring is brick
> red #7C3527 and the three-person silhouette is brick red #B5463A.
>
> **Row 2, column 1: pip, filled.** A diamond (a square rotated 45 degrees) 120 × 120 pixels, flat mustard #C99138
> with a 2 pixel darker edge #8A6224.
>
> **Row 2, column 2: pip, empty.** The same diamond as an outline only: a 6 pixel mustard #C99138 line, transparent
> inside.
>
> **Row 2, column 3: status chip.** A rounded rectangle 160 × 80 pixels with 16 pixel rounded corners, flat pale cream
> #EEE6D4 with a 2 pixel edge #8C7E68. Empty: no dot, no number.
>
> **Row 2, column 4:** empty.
>
> **Style rules.** Front-on, perfectly round circles, no perspective. No drop shadow outside a shape, no glow, no
> gradient. No text, letters, numbers, logos or watermarks anywhere. NOT photoreal, NOT glossy, NOT metallic, NOT
> neon, NOT painterly, NOT 3D.

Feeds: Counter, Chip, the Support and Denial badges, the End Turn pips, the enemy status chip.

## Sheet C: bars and tags

> Create a game UI sprite sheet for Project Crookedile, matching the attached battle screen exactly in style: flat
> cel shading, thin clean line work, a dusty muted palette, dark walnut with a fine gold edge. This is a production
> asset sheet, not an illustration.
>
> **Canvas and grid.** The image is exactly 2048 × 512 pixels with a fully transparent background (alpha 0) everywhere
> outside the shapes: no background colour, no texture, no vignette. Divide it into an invisible grid of 2 columns × 4
> rows of 1024 × 128 pixel cells. Do not draw the grid. Centre each element in its cell with at least 16 pixels of
> empty transparent space around it. Nothing crosses into a neighbouring cell. Cells not listed stay empty and
> transparent.
>
> **Row 1, column 1: meter trough.** A long rectangle 960 × 88 pixels with 4 pixel rounded corners. Flat near-black fill
> #181812, a 3 pixel gold line #C99138 on the inside of the edge, a 2 pixel dark brown rim #5C462E outside it. Flat
> centre; detail within the outer 12 pixels.
>
> **Row 1, column 2: fill strip.** A long rectangle 960 × 80 pixels with square corners, one flat pale cream colour
> #F2EADB, no edge, no texture, no gradient. It will be tinted in the game.
>
> **Row 2, column 1: chevrons.** Three identical chevrons pointing right, side by side with a 8 pixel gap, each 56 pixels
> wide and 80 pixels tall with a 20 pixel thick stroke, flat cream #EEDEC7, no outline. Place them at the left of the
> cell.
>
> **Row 2, column 2: hostility track.** A thin rectangle 600 × 28 pixels with 4 pixel rounded corners, flat near-black
> fill #181812 and a 2 pixel dark brown edge #5C462E. Exactly at its horizontal centre, a 4 pixel wide cream tick
> #EEDEC7 that extends 6 pixels above and below the track.
>
> **Row 3, column 1: stance pill.** A pill shape (fully rounded ends) 240 × 56 pixels, flat pale cream #EEE6D4 with a 2
> pixel edge #8C7E68. Empty: no text. It will be tinted in the game.
>
> **Row 3, column 2: name plate.** A rectangle 360 × 64 pixels with 4 pixel rounded corners, light wood #917450 with
> two or three faint horizontal grain lines in #7E6444, a 2 pixel dark edge #3A2C1C, and a 2 pixel lighter band
> #A88A62 along the inside of the top edge. Empty: no text. Detail within the outer 16 pixels.
>
> **Row 4, column 1: intent box.** A rectangle 260 × 72 pixels with 6 pixel rounded corners, flat dark fill #2A2520,
> a fine 2 pixel gold line #C99138 inset 3 pixels. Empty: no icon, no text.
>
> **Row 4, column 2:** empty.
>
> **Style rules.** Front-on, perfectly straight edges, no perspective. No drop shadow outside a shape, no glow, no
> gradient beyond what is described. No text, letters, numbers, logos or watermarks anywhere. NOT photoreal, NOT
> glossy, NOT metallic, NOT neon, NOT painterly, NOT 3D.

Feeds: Bar (trough and fill), the Opinion chevrons, the Hostility Bar, Tag, the name plate, Intent Tag. The fill strip,
chevrons and pill are pale on purpose: Unity tints them (gold and dark Opinion fills, red chevrons, the green and red
hostility fills, Hostile, Neutral and Receptive tags) and flips the chevrons, so one sprite serves every colour and
direction.

## Sheet D: icons

> Create a game UI icon sheet for Project Crookedile, matching the attached battle screen in style: flat, bold, simple
> silhouettes with thin clean shapes. This is a production asset sheet, not an illustration.
>
> **Canvas and grid.** The image is exactly 512 × 384 pixels with a fully transparent background (alpha 0) everywhere
> outside the icons: no background colour, no backing circle or square, no texture. Divide it into an invisible grid
> of 4 columns × 3 rows of 128 × 128 pixel cells. Do not draw the grid. Centre each icon in its cell, fitting inside a
> 96 × 96 pixel area, so at least 16 pixels of transparent space surround it. Nothing crosses into a neighbouring cell.
>
> **Every icon** is a single flat colour, cream #EEDEC7, with no outline, no shading and no second colour, drawn with
> a consistent 10 pixel stroke weight and rounded stroke ends, so all icons look like one set.
>
> **Row 1, left to right:** an arrow pointing straight down; an arrow pointing straight up; a heater shield; two
> curved arrows chasing each other in a circle (convert).
>
> **Row 2, left to right:** three short horizontal dashes in a row (waits); four vertical bars of uneven height like a
> sound meter (riles); three people standing shoulder to shoulder (crowd); a rolled scroll (log).
>
> **Row 3, left to right:** three horizontal lines of equal length (menu); a round coin with a bold letter P cut out of
> its centre (Tithe Drive); then two empty cells.
>
> **Style rules.** No text other than the P on the coin, no logos or watermarks. NOT photoreal, NOT glossy, NOT
> emoji-style, NOT 3D, NOT painterly.

Feeds: Intent Tag icons, Chip icons, the LOG button. Icons are cream so Unity can tint them.

---

## Bringing a sheet into Unity

1. Save the PNG in `Assets/Art/UI/`.
2. Import: Texture Type **Sprite (2D and UI)**, Sprite Mode **Multiple**, Mesh Type **Full Rect**, Alpha Is
   Transparency on, no mipmaps, compression as [`art-bible.md`](../art-bible.md) §0.5.
3. Sprite Editor: **Slice → Automatic**. The transparent padding keeps every element separate, and automatic slicing
   trims each sprite to its shape, which 9-slice borders need. If a shape comes out in pieces, use **Grid By Cell
   Size** with the cell size from the Layout table and trim by hand. Name each sprite after its element
   (`button-primary-normal`, `badge-support`, …).
4. Set **9-slice borders** on everything that stretches, then use those sprites as Image Type **Sliced**:

   | Sprite | Border (left, right, top, bottom) |
   |---|---|
   | Panel frame | 24 all sides |
   | Tray | 20 all sides |
   | Primary button (all states) | 20 all sides |
   | Secondary button (all states) | 16 all sides |
   | Meter trough | 12 all sides |
   | Hostility track | 8 left and right, 0 top and bottom |
   | Stance pill | 28 left and right, 0 top and bottom |
   | Name plate | 16 all sides |
   | Intent box | 16 all sides |
   | Status chip | 16 all sides |

5. Wire the sprites into the element prefabs from [`ui-elements.md`](../ui-elements.md). Button states go in the
   Button's **Sprite Swap** transition.
