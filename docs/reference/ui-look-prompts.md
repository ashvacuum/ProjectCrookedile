# UI look prompts

> **Kind:** Reference · **Status:** Stale · **Updated:** 2026-11-07
>
> **Summary:** Image prompts for the battle screen: three UI directions in the locked style, and three alternative art styles to test against it, and the favoured hand-drawn satire blend in three weights.
>
> **Source of truth:** this doc · **Related:** [`battle-ui-mock-prompt.md`](battle-ui-mock-prompt.md) · [`style-mock-prompt.md`](style-mock-prompt.md) · [`ui-sprite-sheet-prompt.md`](ui-sprite-sheet-prompt.md) · [`art-bible.md`](../art-bible.md)

**Decision (2026-11-07):** the existing reference image, `docs/design/battle-ui-reference.png`, still reads best.
It is a detailed ink illustration, not the flat look these prompts assume; the art bible (section 0.1) now describes it.
These explorations are kept for reference; new screens are built as edits of that image (`battle-ui-mock-prompt.md`).

Each prompt draws the same battle screen in the locked *Odd Taxi* style; only what the interface is made of
changes. Generate each at 16:9 (1920×1080). Optionally attach `docs/design/battle-ui-reference.png` as a
composition reference. Drop results beside this file as `ui-look-a-1.png`, `ui-look-b-1.png` and so on, then
note what worked under the matching direction below.

The shared scene block is repeated in each prompt so every prompt can be pasted alone.

---

## A. Live broadcast

The hearing is being televised. The UI is a TV news graphics package: the Opinion Meter is a live poll
graphic, names are lower-thirds, intents are captions. Satire through the medium.

```text
A 16:9 video game battle screen, rendered in the style of the anime Odd Taxi: flat 2D, clean thin uniform
linework, limited flat cel shading, a muted warm palette of dusty mustard, muted teal, brick red, cream,
faded olive and warm greys, subtle paper grain, even soft lighting.

Scene: a Filipino legislative committee hearing. A composed crocodile politician in a plain barong stands
with his back to camera at a wooden podium with a microphone, lower centre. Facing him, three grounded,
deadpan anthropomorphic animal officials sit behind a raised wooden panel: a radio commentator, a barangay
tanod, a swing voter. Campaign tarpaulins on the back wall.

The interface is a live TV news broadcast graphics package laid over the hearing, flat and clean:
- Top centre, a live-poll graphic titled "OPINION 62": one horizontal bar, gold fill from the left, a
  thin tick at 60 labelled "WIN LINE", a round teal badge "SUPPORT 8" on the left end and a round brick
  badge "DENIAL 4" on the right end. A small red "LIVE" bug and "TURN 4 · JUDGMENT IN 6" in the top right
  corner like a broadcast clock.
- Each official gets a lower-third name strap on the panel edge with their name, a coloured stance tag
  (HOSTILE brick, NEUTRAL grey, RECEPTIVE olive), a thin signed meter, and a small "GUILT 2/3" chip.
  Above each head, a caption box with one word and a number: "CONDEMN 6", "REBUKE +4", "MURMUR".
- Above the player's hand, a news ticker strip: "GUILT IN THE ROOM 3 · CONVERTED THIS TURN 0".
- Bottom left, three round counters like broadcast score bugs: ENERGY 2/3, DRAW 9, DISCARD 4.
- Along the bottom edge, four playing cards fanned, red-framed, each with a cost circle, a title in caps
  (CONFESSION, HEAVY CONSCIENCE, ALTAR CALL, FIRE AND BRIMSTONE), a small flat illustration and one line
  of text.
- Bottom right, a large mustard "END TURN" button with three small diamond pips above it.

One condensed sans-serif for all labels. The broadcast graphics are flat colour blocks with crisp edges,
no gradients, no glow. Faces stay uncovered.
NOT oil painting, NOT photoreal, NOT neon, NOT 3D, NOT bright slapstick.
```

**Notes:**

---

## B. Campaign tarpaulin

The UI is campaign print: tarpaulin banners, sticker chips, rubber stamps, starburst callouts. Loud
election-season graphics, but still flat and muted.

```text
A 16:9 video game battle screen, rendered in the style of the anime Odd Taxi: flat 2D, clean thin uniform
linework, limited flat cel shading, a muted warm palette of dusty mustard, muted teal, brick red, cream,
faded olive and warm greys, subtle paper grain, even soft lighting.

Scene: a Filipino legislative committee hearing. A composed crocodile politician in a plain barong stands
with his back to camera at a wooden podium with a microphone, lower centre. Facing him, three grounded,
deadpan anthropomorphic animal officials sit behind a raised wooden panel: a radio commentator, a barangay
tanod, a swing voter. Campaign tarpaulins on the back wall.

The interface is made of Filipino election-campaign print material, flat and slightly worn:
- Top centre, the Opinion Meter is a printed tarpaulin banner strung across the top with rope ties at the
  ends, reading "OPINION 62" in heavy poster type, a gold fill bar printed on it with a stamped tick at 60
  marked "WIN LINE". Two round campaign-button badges pinned at its ends: a teal "SUPPORT 8" and a brick
  "DENIAL 4". Top right, a printed sticker "TURN 4 · JUDGMENT IN 6".
- Each official has a small printed name placard on the panel edge, a rubber-stamped stance tag (HOSTILE in
  brick ink, NEUTRAL in grey ink, RECEPTIVE in olive ink), a printed signed meter, and a round sticker
  "GUILT 2/3". Above each head, a starburst callout with one word and a number: "CONDEMN 6", "REBUKE +4",
  "MURMUR".
- Above the player's hand, a strip of campaign stickers: "GUILT IN THE ROOM 3", "CONVERTED THIS TURN 0".
- Bottom left, three campaign-button counters: ENERGY 2/3, DRAW 9, DISCARD 4.
- Along the bottom edge, four playing cards fanned like printed handbills, red-framed, each with a cost
  circle, a title in caps (CONFESSION, HEAVY CONSCIENCE, ALTAR CALL, FIRE AND BRIMSTONE), a small flat
  illustration and one line of text.
- Bottom right, a large mustard "END TURN" button shaped like a tarpaulin patch with grommets, three small
  diamond pips above it.

Heavy condensed poster type for labels, halftone dots in the larger fills, slightly misregistered print
edges. Faces stay uncovered.
NOT oil painting, NOT photoreal, NOT glossy, NOT 3D, NOT bright slapstick.
```

**Notes:**

---

## C. Official dossier

The UI is government paperwork: manila folders, typed labels, rubber stamps, carbon-copy forms, a
receipt-paper log. Dry, bureaucratic, deadpan, closest to the tone of the satire.

```text
A 16:9 video game battle screen, rendered in the style of the anime Odd Taxi: flat 2D, clean thin uniform
linework, limited flat cel shading, a muted warm palette of dusty mustard, muted teal, brick red, cream,
faded olive and warm greys, subtle paper grain, even soft lighting.

Scene: a Filipino legislative committee hearing. A composed crocodile politician in a plain barong stands
with his back to camera at a wooden podium with a microphone, lower centre. Facing him, three grounded,
deadpan anthropomorphic animal officials sit behind a raised wooden panel: a radio commentator, a barangay
tanod, a swing voter. Campaign tarpaulins on the back wall.

The interface is made of government paperwork, flat and orderly:
- Top centre, the Opinion Meter is a strip of ruled form paper clipped to a board, typed header "OPINION
  62", a gold-shaded bar filled in like a form field, a hand-drawn tick at 60 labelled "WIN LINE". At its
  ends, two round rubber-stamp impressions: teal "SUPPORT 8" and brick "DENIAL 4". Top right, a
  typewritten docket tab "TURN 4 · JUDGMENT IN 6".
- Each official has a manila folder tab on the panel edge with their name typed on it, a rubber-stamped
  stance (HOSTILE in brick ink, NEUTRAL in grey, RECEPTIVE in olive), a thin ruled signed meter, and a small
  paper tag "GUILT 2/3" held by a paper clip. Above each head, a typed index-card note with one word and a
  number: "CONDEMN 6", "REBUKE +4", "MURMUR".
- Above the player's hand, a thin carbon-copy strip: "GUILT IN THE ROOM 3 · CONVERTED THIS TURN 0".
- Bottom left, three round inspection-stamp counters: ENERGY 2/3, DRAW 9, DISCARD 4.
- Along the bottom edge, four playing cards fanned like filed index cards, red-tabbed, each with a cost
  circle, a typed title in caps (CONFESSION, HEAVY CONSCIENCE, ALTAR CALL, FIRE AND BRIMSTONE), a small flat
  illustration and one line of typed text.
- Bottom right, a large mustard "END TURN" button shaped like an APPROVED stamp pad, three small diamond
  pips above it.

Typewriter type for small labels, one condensed sans-serif for numbers. Faces stay uncovered.
NOT oil painting, NOT photoreal, NOT grimy, NOT 3D, NOT bright slapstick.
```

**Notes:**

---

# Alternative art styles (exploration)

The art bible locks the *Odd Taxi* look. These three test other base styles for the same screen, so the lock
can be confirmed or replaced on evidence. The UI in each is deliberately plain (thin dark panels, one
condensed sans-serif) so the comparison is about the art, not the interface. Save results as
`ui-style-d-1.png` and so on.

## D. Komiks ink

Filipino komiks and pulp-print tradition: bold brush-ink lines, flat spot colour, halftone shading on
newsprint. Satirical-cartoon energy, sharper and more caricatured than the flat look.

```text
A 16:9 video game battle screen in a Filipino komiks pulp-print style: bold confident brush-ink outlines
with varied line weight, flat spot colours in a limited four-colour print palette (mustard yellow, brick
red, teal, black on off-white newsprint), halftone dot shading, slight print misregistration, crosshatched
shadows. Satirical political-cartoon caricature: big expressive heads, exaggerated but readable.

Scene: a Filipino legislative committee hearing. A crocodile politician in a plain barong stands with his
back to camera at a wooden podium with a microphone, lower centre. Facing him, three anthropomorphic animal
officials sit behind a raised wooden panel: a radio commentator, a barangay tanod, a swing voter. Campaign
tarpaulins on the back wall.

Plain game interface, thin dark panels and one condensed sans-serif: top centre an Opinion bar labelled
"OPINION 62" with a tick at 60, a teal "SUPPORT 8" badge on the left end and a brick "DENIAL 4" badge on
the right; top right "TURN 4 · JUDGMENT IN 6". Above each official a small intent tag ("CONDEMN 6",
"REBUKE +4", "MURMUR"), and under each a name plate, a stance tag (HOSTILE, NEUTRAL, RECEPTIVE) and a "GUILT
2/3" chip. Bottom left three round counters: ENERGY 2/3, DRAW 9, DISCARD 4. Along the bottom, four fanned
cards with cost circles and titles (CONFESSION, HEAVY CONSCIENCE, ALTAR CALL, FIRE AND BRIMSTONE). Bottom
right a large mustard "END TURN" button.

NOT photoreal, NOT 3D, NOT smooth digital gradients, NOT anime.
```

**Notes:**

## E. Cut-paper diorama

The hearing as a layered paper diorama: cut card stock, soft drop shadows between layers, visible paper
texture and slightly uneven edges. Warm, handmade, a little absurd.

```text
A 16:9 video game battle screen as a handmade cut-paper diorama: every shape cut from coloured card stock
in layered planes, soft realistic drop shadows between layers, visible paper fibre texture, slightly uneven
hand-cut edges, shallow depth of field. Palette of dusty mustard, muted teal, brick red, cream and olive
card stock under soft warm studio light.

Scene: a Filipino legislative committee hearing built as a paper diorama. A crocodile politician in a plain
barong, cut from paper, stands with his back to camera at a paper podium with a microphone, lower centre.
Facing him, three paper anthropomorphic animal officials sit behind a raised paper panel: a radio
commentator, a barangay tanod, a swing voter. Tiny paper campaign tarpaulins on the back wall.

Plain game interface, also cut from paper and layered on top: top centre an Opinion bar labelled "OPINION
62" with a tick at 60, a teal "SUPPORT 8" badge on the left end and a brick "DENIAL 4" badge on the right;
top right "TURN 4 · JUDGMENT IN 6". Above each official a small intent tag ("CONDEMN 6", "REBUKE +4",
"MURMUR"), and under each a name plate, a stance tag (HOSTILE, NEUTRAL, RECEPTIVE) and a "GUILT 2/3" chip.
Bottom left three round counters: ENERGY 2/3, DRAW 9, DISCARD 4. Along the bottom, four fanned paper cards
with cost circles and titles (CONFESSION, HEAVY CONSCIENCE, ALTAR CALL, FIRE AND BRIMSTONE). Bottom right a
large mustard "END TURN" button. One condensed sans-serif printed on the paper.

NOT photoreal people, NOT plastic, NOT glossy 3D render, NOT flat vector.
```

**Notes:**

## F. Painted movie-poster

Hand-painted Filipino cinema and campaign billboard art: bold gouache brushwork, dramatic poses, saturated
retro colour, hand-lettered titles. Grand, theatrical, a little over the top.

```text
A 16:9 video game battle screen in the style of hand-painted vintage Filipino cinema billboards and campaign
posters: bold opaque gouache brushwork, visible strokes, saturated retro colour (warm yellows, deep reds,
teal, cream), strong rim light, dramatic theatrical poses, hand-lettered titles with drop shadows.

Scene: a Filipino legislative committee hearing staged like a movie billboard. A crocodile politician in a
plain barong stands with his back to camera at a wooden podium with a microphone, lower centre, lit from the
front. Facing him, three anthropomorphic animal officials sit behind a raised wooden panel: a radio
commentator, a barangay tanod, a swing voter, each striking a theatrical expression. Campaign tarpaulins on
the back wall.

Plain game interface, thin dark panels and one condensed sans-serif: top centre an Opinion bar labelled
"OPINION 62" with a tick at 60, a teal "SUPPORT 8" badge on the left end and a brick "DENIAL 4" badge on
the right; top right "TURN 4 · JUDGMENT IN 6". Above each official a small intent tag ("CONDEMN 6",
"REBUKE +4", "MURMUR"), and under each a name plate, a stance tag (HOSTILE, NEUTRAL, RECEPTIVE) and a "GUILT
2/3" chip. Bottom left three round counters: ENERGY 2/3, DRAW 9, DISCARD 4. Along the bottom, four fanned
cards with cost circles and titles (CONFESSION, HEAVY CONSCIENCE, ALTAR CALL, FIRE AND BRIMSTONE). Bottom
right a large mustard "END TURN" button.

NOT photoreal, NOT 3D, NOT flat vector, NOT anime.
```

**Notes:**

---

# G. Hand-drawn satire (the favoured blend)

The blend picked on 2026-11-07: the hand-drawn, comic-acted cast and painted backgrounds of a Klei-style
negotiation card game, with the rounded, appealing anthro satire of a Filipino election game. The prompts
describe those qualities instead of naming the games, so results are our own. Three weights of the same
mix; save results as `ui-style-g1-1.png` and so on.

**Keep it ours:** Political Animals (Squeaky Wheel, 2016) also has a crocodile candidate in a Filipino
election. Our crocodile should not match theirs: a plain barong, a heavier, older build, and a deadpan face,
not a cute mascot.

**The background is the crowd:** the officials sit in front of a packed public gallery, never a bare wall.
The crowd is the audience the Opinion Meter measures, so it should always be visible behind them.

## G1. Even blend

```text
A 16:9 video game battle screen, hand-drawn 2D: confident thick ink outlines with varied weight, characters
drawn like an animated cartoon with strong expressive acting and clear silhouettes, set against softly
painted atmospheric backgrounds with visible brushwork. Rounded, appealing anthropomorphic animals with a
sly satirical edge: likeable on the surface, shady underneath. Warm lighting, rich but slightly muted
colour: mustard, teal, brick red, cream, olive.

Scene: a Filipino legislative committee hearing. A heavy-set older crocodile politician in a plain barong
stands with his back to camera at a wooden podium with a microphone, lower centre, one hand raised
mid-speech. Facing him, three anthropomorphic animal officials sit behind a raised wooden panel, each acting
clearly: a radio commentator leaning in to accuse, a barangay tanod with arms crossed, a swing voter
glancing sideways. Behind and above the panel, filling the whole background, a packed public gallery of
anthropomorphic animal citizens in tiered seats: fans waving homemade signs, skeptics with folded arms,
someone filming on a phone, painted looser and softer than the officials so they read as a crowd, not a wall.

Game interface, clean and slightly illustrated, one bold condensed sans-serif: top centre an Opinion bar
labelled "OPINION 62" with a tick at 60, a teal "SUPPORT 8" badge on the left end and a brick "DENIAL 4"
badge on the right; top right "TURN 4 · JUDGMENT IN 6". Above each official a small intent tag ("CONDEMN
6", "REBUKE +4", "MURMUR"), and under each a name plate, a stance tag (HOSTILE, NEUTRAL, RECEPTIVE) and a
"GUILT 2/3" chip. Above the hand, a small tray: "GUILT IN THE ROOM 3". Bottom left three round counters:
ENERGY 2/3, DRAW 9, DISCARD 4. Along the bottom, four fanned cards with ink-outlined frames, cost circles,
titles in caps (CONFESSION, HEAVY CONSCIENCE, ALTAR CALL, FIRE AND BRIMSTONE) and small hand-drawn
illustrations. Bottom right a large mustard "END TURN" button.

NOT photoreal, NOT 3D, NOT anime, NOT flat vector, NOT grimdark.
```

**Notes:**

## G2. Leaning painterly and moody

```text
A 16:9 video game battle screen, hand-drawn 2D: thick expressive ink outlines on the characters, drawn like
a classic animated feature with strong acting, set against richly painted, atmospheric backgrounds with
deep shadows, haze and a warm key light from tall windows. Anthropomorphic animals with weight and age in
their faces, satirical and a little sinister. Dusky palette: deep teal, burnt mustard, brick red, cream
highlights.

Scene: a Filipino legislative committee hearing late in the afternoon, light slanting through dusty air. A
heavy-set older crocodile politician in a plain barong stands with his back to camera at a wooden podium
with a microphone, lower centre. Facing him, three anthropomorphic animal officials sit behind a raised
wooden panel: a radio commentator leaning in to accuse, a barangay tanod with arms crossed, a swing voter
glancing sideways. Behind and above the panel, filling the whole background, a packed public gallery of
anthropomorphic animal citizens in tiered seats, half-lit by the slanting light: murmuring faces, raised
signs, a few heads turned away, painted looser and darker than the officials so they read as a crowd, not a
wall.

Game interface, dark panels with thin warm-gold edges, one bold condensed sans-serif: top centre an Opinion
bar labelled "OPINION 62" with a tick at 60, a teal "SUPPORT 8" badge on the left end and a brick "DENIAL 4"
badge on the right; top right "TURN 4 · JUDGMENT IN 6". Above each official a small intent tag ("CONDEMN
6", "REBUKE +4", "MURMUR"), and under each a name plate, a stance tag (HOSTILE, NEUTRAL, RECEPTIVE) and a
"GUILT 2/3" chip. Above the hand, a small tray: "GUILT IN THE ROOM 3". Bottom left three round counters:
ENERGY 2/3, DRAW 9, DISCARD 4. Along the bottom, four fanned cards with ink-outlined frames, cost circles,
titles in caps (CONFESSION, HEAVY CONSCIENCE, ALTAR CALL, FIRE AND BRIMSTONE) and small painted
illustrations. Bottom right a large mustard "END TURN" button.

NOT photoreal, NOT 3D, NOT anime, NOT neon, NOT pure black voids.
```

**Notes:**

## G3. Leaning cute and bright

```text
A 16:9 video game battle screen, hand-drawn 2D: clean rounded ink outlines, soft simple shapes, characters
drawn like a friendly cartoon with big readable expressions, set against lightly painted backgrounds in
bright daylight. Cute, chunky anthropomorphic animals doing shady politics: adorable on the surface, the
satire in the details (a wad of cash peeking from a pocket, a too-wide smile). Cheerful warm palette:
sunny mustard, fresh teal, tomato red, cream, leaf green.

Scene: a Filipino legislative committee hearing at midday. A heavy-set older crocodile politician in a plain
barong stands with his back to camera at a wooden podium with a microphone, lower centre. Facing him, three
anthropomorphic animal officials sit behind a raised wooden panel: a radio commentator leaning in to
accuse, a barangay tanod with arms crossed, a swing voter glancing sideways. Behind and above
the panel, filling the whole background, a packed and noisy public gallery of cute anthropomorphic animal
citizens in tiered seats: waving flags and homemade signs, cheering, booing, one asleep, drawn simpler and
lighter than the officials so they read as a crowd, not a wall.

Game interface, rounded and friendly, cream panels with thick dark outlines, one bold rounded sans-serif:
top centre an Opinion bar labelled "OPINION 62" with a tick at 60, a teal "SUPPORT 8" badge on the left end
and a red "DENIAL 4" badge on the right; top right "TURN 4 · JUDGMENT IN 6". Above each official a small
intent tag ("CONDEMN 6", "REBUKE +4", "MURMUR"), and under each a name plate, a stance tag (HOSTILE,
NEUTRAL, RECEPTIVE) and a "GUILT 2/3" chip. Above the hand, a small tray: "GUILT IN THE ROOM 3". Bottom left
three round counters: ENERGY 2/3, DRAW 9, DISCARD 4. Along the bottom, four fanned cards with rounded
frames, cost circles, titles in caps (CONFESSION, HEAVY CONSCIENCE, ALTAR CALL, FIRE AND BRIMSTONE) and small
cute illustrations. Bottom right a large mustard "END TURN" button.

NOT photoreal, NOT 3D, NOT anime, NOT chibi, NOT grimdark.
```

**Notes:**
