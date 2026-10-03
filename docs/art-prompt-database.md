# Crookedile — Art Prompt Database

> **Kind:** Art · **Status:** Stale · **Updated:** 2026-10-03
>
> **Summary:** Per-asset image prompts for cards, enemies and allies. Generated from 48 card assets on 2026-09-11; the card prompts are now kept in the card-art workbook.
>
> **Source of truth:** generated from [`Assets/Data/`](../Assets/Data/) · **Related:** [`art-bible.md`](art-bible.md) · [`crookedile-card-art-prompts.xlsx`](crookedile-card-art-prompts.xlsx)

*Generated from the live assets (2026-09-11): 48 card assets, 9 enemy assets, 6 ally assets.
Paste **§1 the style block** first, then the subject line from the table. Card art uses the icon style in §1a (updated 2026-10-02, see `docs/art-bible.md` §3); portraits keep the Odd Taxi block in §1b
(`docs/reference/style-mock-prompt.md` v4). Sizes are from `docs/art-bible.md` §0.5.*

**Read §6 before generating** — several entries are blocked on data that isn't authored yet.

---

## 1. The style block (paste before every prompt)

### 1a. Card illustrations (icon style)

One object, bold silhouette, flat colour blocks, one accent colour, plain background. Reference: Deadlock ability icons. Subject lines in §3 are written to this block.

```
bold stylized 2D game icon illustration, single clear focal object, strong readable silhouette,
flat cel-shaded color blocks with clean thick inked outline, limited palette of three colors plus
one saturated accent, high contrast against a plain solid background, simple geometric shapes,
minimal surface detail, no texture
SUBJECT: <paste subject line here>, plain solid neutral background, centered, front-on
```

*Midjourney tail:* `--v 8.1 --ar 5:7 --raw --hd --s 60 --c 0 --no text, letters, words, border, frame, watermark, shadow, gradient, texture, clutter`
*Gemini / ChatGPT tail:* `Render at 1500x2148 px, portrait, PNG.`
The same prompts, pre-assembled, are in `docs/crookedile-card-art-prompts.xlsx`.

### 1b. Enemy portraits (Odd Taxi block)

Paste this base block, then the portrait ending below it.

```
Flat 2D anime illustration in the style of the anime Odd Taxi: clean uniform thin linework,
limited flat cel shading, almost no gradients, subtle paper-grain texture over flat color.
Muted sophisticated palette — dusty mustard, muted teal, brick red, cream, faded olive, warm
greys — low saturation but warm; never grey-grimdark, never candy-bright. Even soft lighting,
no chiaroscuro, no black voids. Filipino political satire, deadpan and adult: the cast are
grounded anthropomorphic animals — naturalistic simplified animal heads on ordinary human
bodies in ordinary clothes (plain barongs, unremarkable suits, campaign shirts), calm restrained
expressions, understated body language. People who happen to be animals — not cartoon mascots,
not realistic beasts, not cute, not photoreal, not oil painting. The humor is in the restraint.
Portrait composition, subject in the upper-centre two-thirds, lower 45% quiet and uncluttered
(a textbox sits there). No text, no letters, no logos, no watermark, no UI frame.
```

*Midjourney tail:* `--ar 7:10 --style raw --s 150 --no text,letters,watermark,frame`
*Gemini / ChatGPT tail:* `Render at 1500x2148 px, portrait, PNG.`

Swap its last two sentences (composition + "No text...") for:

```
Square bust portrait, head-and-shoulders, facing camera from slightly below (they sit at a
raised committee panel). Plain flat muted background, no scenery. The role reads on the face.
```

*Midjourney tail:* `--ar 1:1 --style raw --s 150` · *target 1024x1024.*

### 1c. Ally / status / intent icons

```
Flat single-colour silhouette icon, pure white on transparent, no outline weight variation,
no gradient, no text. Must read clearly at 32 px. Centred with ~6% margin. Simple bold shape.
SUBJECT: <paste subject line here>
```

*Target 128x128 (ally icons 256x256 — see §5).*

---

## 2. Frame colour by card type (the artist reads this off the type column)

| Type | Frame colour | Motif |
|---|---|---|
| Pressure | green `#3FA86B` | calm laurel / handshake |
| Rhetoric | red `#C0392B` | sharp, jagged, megaphone |
| Policy | blue `#2C6FB5` | ledger / seal / document |
| Heckle | purple `#7D4CA8` | dashed / ephemeral border |
| Scandal | dark crimson `#6E1B2E` | torn tabloid / redacted bars |

---

## 3. Card illustration prompts — 48 cards

Columns: **card · type · AP · what it actually does (read off the asset) · subject line**. Subject lines are icon-style briefs (one object, one verb, one accent colour, see §1a); the old scene-style lines are superseded.
"↑" = the upgraded value.

### 3a. Faith Leader — Basic (10)

| Card | Type | AP | Mechanic | Subject line |
|---|---|---|---|---|
| Drown out the Haters | Pressure | 1 | Push meter 10 ↑15 on one enemy; only while a hostile is present | one megaphone with thick sound arcs pushing outward, one gold accent |
| Guilt | Pressure | 1 · starter | Apply 2 Guilt to one enemy | one heavy weight hanging from a thin chain, one gold accent |
| Pressure | Pressure | 1 · starter | Push meter 7 ↑10 + shift that enemy's stance 3 | one clenched fist pressing down onto a flat slab, one gold accent |
| Quiet Reflection | Rhetoric ⚑ | 0 | Delayed: gain 8 ↑10 Support | one closed eye above a still ripple ring, one gold accent |
| Shepherd's Joy | Rhetoric ⚑ | 1 · starter | This turn: gain 3 ↑5 Support each time an enemy turns receptive | one shepherd's crook with a single smiling sheep, one gold accent |
| **(unnamed)** ⚑ | Rhetoric | 1 | Apply 1 ↑2 Doubt to every hostile enemy | one closed mouth with a finger pressed to its lips, one gold accent |
| Sow Discord | Pressure | 1 ↑0 | Push meter 5 ↑7 + 1 Doubt on one enemy | one pointing finger above a single question mark, one gold accent |
| Stir the Flock | Rhetoric | 1 ↑0 · starter | Draw 2 + shift every enemy 2 toward receptive | one wooden spoon stirring a swirl of identical sheep, one gold accent |
| Support | Rhetoric ⚑ | 1 · starter | Gain 4 ↑6 Support | one open hand holding up a single gold star |
| Traitor to the Cause | Rhetoric | 1 · starter | Apply 2 Shame to one enemy (needs a hostile present) | one broken chain link with a dagger through it, one gold accent |

### 3b. Faith Leader — Enhanced (25)

| Card | Type | AP | Mechanic | Subject line |
|---|---|---|---|---|
| Absolve Sins | Pressure | 1 | Push meter 2 ↑5 + soften one enemy 5 ↓2 | one hand releasing a chain of broken links upward, one gold accent |
| Blessed are the Persecuted | Pressure | 1 | This turn: meter +5 every time any stance shifts | one thorn crown glowing with a halo, one gold accent |
| Call out the Wicked | Rhetoric | 2 ↑1 | Gain 1 Strength | one pointing finger aimed at a single dark silhouette, one gold accent |
| Congregation | Rhetoric ⚑ | 2 | Soften one enemy 3 ↑5 + delayed +2 AP | five identical silhouettes filing toward one gold doorway |
| Faith | Rhetoric | 3 ↑2 | Gain 3 Support + soften one enemy 2 | one steady flame inside a lantern, one gold accent |
| Fruits of Conversion | Policy | 2 ↑1 | This turn: meter +5 each time an enemy turns receptive | one fruit branch with a single gold fruit |
| God's Chosen | Rhetoric | 2 | Strip Jaded + apply 1 Fanatic to one enemy | one gold crown floating above a single raised hand |
| Gospel | Policy | 3 ↑2 | Convert every pacified enemy at once (consume Guilt/Shame/Doubt → Fanatic) | one open book radiating a single beam of light, one gold accent |
| Hallelujah | Rhetoric | 3 ↑2 | Gain 2 Strength per Fanatic in the room | two arms thrown up with gold light above |
| Have you no Shame? | Policy | 2 ↑1 | This turn: any stance shift also applies 1 Shame to that enemy | one pointing finger above a bowed head, one gold accent |
| Holier Than Thou ⚑ | Pressure | 3 ↑2 | Push meter 30 on one enemy | one raised chin and pointing nose above a halo ring, one gold accent |
| I am speaking | Pressure | 2 ↑1 | Silence every enemy 1 | one raised palm in front of a crossed-out mouth, one gold accent |
| Impenetrable Belief ⚑ | Pressure | 2 ↑1 | Delayed: gain 7 ↑10 Support | one thick round shield with a cross cut into its face, one gold accent |
| Insinuate | Rhetoric | 2 ↑1 | 2 Doubt + push meter 5 on one enemy | one raised eyebrow above a sideways glance, one gold accent |
| Name and Shame | Pressure | 2 ↑1 | Silence one enemy 1 + raise that enemy's hostility 2 | one pointing finger with a single speech bubble cracked in half, one gold accent |
| Persecution Complex | Rhetoric | 1 | This turn: meter +5 every time any stance shifts | one pointing finger turned back at its own chest, one gold accent |
| Praise ⚑ | Rhetoric | 1 | Soften 2 ↑4 (target currently resolves to self — see §6) | two hands clapping with a gold spark between them |
| Preach | Pressure | 1 | Draw 1 + push meter 8 on one enemy | one lectern with a megaphone fused on top, one gold accent |
| Quiet the Haters | Pressure | 2 ↑1 | Silence 1 random hostile per Fanatic in the room | one hand over a row of three silenced mouths, one gold accent |
| Retribution | Pressure | 1 | Push meter 5 on one enemy | one gavel striking down with impact lines, one gold accent |
| Sacrificial Lamb | Pressure | 1 | Push meter 10 + raise that enemy's hostility 10 ↓4 | one lamb on an altar, one gold glow |
| Sermon ⚑ | Pressure | 0 | Push meter 5 + raise every enemy's hostility 5 ↓2 | a single raised open palm thrust forward with three motion arcs, one gold accent |
| The Other Cheek | Pressure | 4 ↑3 | Push meter 15 + draw 3 | one cheek turned toward a stopped slapping hand, one gold accent |
| United Faith | Pressure | 2 | Push meter 20 ↑25 (needs a hostile present) | three identical hands clasped in a ring, one gold accent |
| Word is Law | Pressure | 2 | Push meter 25 + soften every enemy 3 + delayed +2 AP | one open book stamped by a single gavel, one gold accent |

### 3c. Faith Leader — Rare (3)

| Card | Type | AP | Mechanic | Subject line |
|---|---|---|---|---|
| The World Is Against Us | Rhetoric | 3 ↑2 | Apply 1 Fanatic to every receptive enemy | one small circle of figures facing out, shielded by a thick ring, one gold accent |
| Us vs Them | Policy | 3 ↑2 | This turn: applying a status strips Jaded off that enemy | one thick vertical line dividing two identical silhouettes, one gold accent |
| Zealotry | Policy | 3 ↑2 | This turn: applying a status also makes that enemy a Fanatic | one burning torch held aloft, one gold accent |

### 3d. Status cards — Heckle, purple frame (6)

*These clog the hand for a turn. Keep them sparse: the same icon style with a single purple accent and extra empty space.*

| Card | AP | Mechanic | Subject line |
|---|---|---|---|
| Deflated ⚑ | — | Unplayable; exhausts at end of turn (no other effect authored) | one half-deflated balloon sagging on its string, one purple accent |
| Disappointed | 1 | Unplayable; at end of turn draw 1 and exhaust | one flat downturned mouth on a plain circle, one purple accent |
| Emptiness | — | Playable; costs you 1 AP the moment it is drawn | one empty folding chair, one purple accent |
| Self Doubt | 0 | Unplayable; at end of turn apply 1 Rattled to yourself and exhaust | one cracked hand mirror, one purple accent |
| Self Pity | 1 | At end of turn nudge the meter 1 toward yourself, then exhaust | one lone stool with a loosened tie draped over it, one purple accent |
| Self Pity *(file: `Exile.asset`)* ⚑ | 1 | At end of turn raise every enemy's hostility 1, then exhaust | one lone stool with a loosened tie draped over it, one purple accent |

### 3e. Curses — Scandal, dark crimson frame (4)

*All unplayable, permanent deck pollution. Same icon style with a single crimson accent; the tabloid halftone texture from the old direction is dropped.*

| Card | Mechanic | Subject line |
|---|---|---|
| Crisis of Faith ⚑ | Exhausts at end of turn *(the described "lose 4 Support" is not authored — see §6)* | one cracked cross, one crimson accent |
| Doubt | Lose 2 meter at end of turn, every turn | one huge question mark stamped on a torn newspaper clipping, one crimson accent |
| False Accusations | Discards a random card the moment it is drawn | one forged paper with a crooked crimson seal |
| Scandal | Lose 3 meter at end of turn, every turn | one folded tabloid with two black redaction bars, one crimson accent |

---

## 4. Characters

### 4a. Player origins — 3 (card backs + any portrait use)

All three are the same crocodile (*buwaya*) — the wardrobe is the satire. Composition: back to camera at a podium for scene art; three-quarter bust for portraits.

| Origin | Subject line |
|---|---|
| Faith Leader | A composed crocodile politician in a plain white barong with a large wooden cross over it, sleeves pressed, a worn bible under one arm; calm, patient, unreadable |
| Nepo Baby | A young crocodile in an expensive slim-cut barong with a heavy gold family signet ring and a watch too big for him, bored posture, phone face-down on the lectern |
| Celebrity | A crocodile in designer sunglasses indoors, barong worn open over a tee, practised half-smile aimed slightly off-camera at where the photographers are |

**Card backs** (`1500x2148`, 4 needed — default + one per origin): Faith Leader = an embossed religious seal; Nepo Baby = a dynastic family crest; Celebrity = a monogram-and-star; default = the crocodile silhouette. Flat, symmetrical, single motif, muted two-tone.

### 4b. Enemy portraits — 9 built (`1024x1024` square bust)

Animal casting is locked in style-mock v4 for the first seven.

| Enemy | Stance | Behaviour | Subject line |
|---|---|---|---|
| Loyal Partisan | hostile (2, range −7…5) | The baseline attacker | A stocky bulldog *(tuta)* in a plain screen-printed campaign shirt, flat tired stare, one fist resting on the desk — loyal out of habit, not passion |
| Spin Doctor | neutral | Shields the meter (Denial) | A lean snake *(ahas)* in an unremarkable grey suit, half-lidded eyes, earpiece, phone face-down, entirely unbothered |
| Heckler | neutral | Silences you | A rooster mid-jeer, beak open, permanent smirk, leaning back with one arm hooked over the chair back |
| Firebrand | hostile | Rallies the row | A wiry fighting cock *(sabungero)* in a sleeveless shirt, coiled forward but calm-faced, about to stand |
| The Bishop | hostile, Hardened | Absolves allies of your statuses | A heavy-set carabao in modest vestments and a plain pectoral cross, serene, immovable, hands folded on the desk |
| Swing Voter | receptive | Turncoats if provoked | A soft-spoken chameleon in an ordinary polo, eyes drifting to one side, hopeful and wary at once |
| The Fixer | neutral | Summons muscle | A watchful monitor lizard *(bayawak)* holding a phone low under the desk, eyes up on you the whole time |
| Curious Student ⚑ | — *(no casting yet)* | — | **Proposed:** a young maya (sparrow) in a university lanyard and plain shirt, notebook open, actually listening — the only face in the row that isn't performing |
| The Incumbent ⚑ | hostile (15, range 10…60) · final boss | Rigged room; no moves authored | **Proposed:** an old, heavy crocodile in a perfectly tailored barong with a sash, three decades of campaign posters behind him; your own species thirty years further in — flat, patient, utterly unimpressed |

### 4c. Roster note

`docs/enemy-design-bible.md` §4 specifies a **different, animal-named roster** (Askal, Maya, Carabao, Parrot, Rat, Cat Tita, Musang, Butiki, Bayawak, Ahas, Tandang, Peacock, Agila, Uwak, Gagamba) across 15 encounters. The nine assets above are the older generic set. Docs are canonical, so **do not commission the nine as final** until the roster question is settled — otherwise you pay for portraits the encounter tables never call. See §6.

---

## 5. Ally icons — 6 built

Allies are passive relics, not row bodies. No size is specced in the art bible; **suggest 256x256**, flat single-colour silhouette (icon block §1c), so they read in a relic tray at ~48 px.

| Ally | Rarity | Mechanic | Subject line |
|---|---|---|---|
| The Beef | Basic | Every 3rd time an enemy's hostility rises, gain 3 Support | Two crossed forearms braced against each other, shoulders squared |
| Crowd Control | Basic | When an enemy turns receptive (first 3 turns), meter +2 | A rope-barrier stanchion with the rope looped neatly, a crowd shape behind it |
| Devil's Advocate | Enhanced | When the room tips receptive, re-radicalise a random hostile by 10 | A single raised hand in a room of lowered ones |
| Fixer's Cousin | Enhanced | Every 5th card played, one card becomes free | A folded envelope of cash passed under a table edge |
| Hail Mary | Rare | Once per battle, while the meter is critically low, meter +10 | A thrown basketball frozen at the top of its arc, the hoop far off at the edge of frame |
| **(unnamed)** ⚑ | Basic | *Empty asset — no name, no id, no passive* | — blocked, see §6 |

---

## 6. Blocked / flagged before you spend money on art

Ordered by how much art it wastes.

1. **The enemy roster is two different sets.** The bible's 15 encounters use animal-named enemies; the nine built assets are the old generic set. Settle this first — it decides whether you commission 9 portraits or ~15.
2. **`New Ally 3.asset` is fully empty** — no name, no id, no passive. Nothing to illustrate.
3. **Two assets both name themselves "Self Pity"** (`Self Pity.asset` and `Exile.asset`), with different effects. One is presumably Exile. Fix before art, or you get two identical illustrations.
4. **Three "Enhanced" cards are authored as Basic rarity** — Holier Than Thou, Impenetrable Belief, Sermon. They sit in the Enhanced folder but carry `_rarity: 0`, so they'd get the plain overlay. Decide which is true.
5. **Frame colour looks scrambled on the defensive cards.** Support, Quiet Reflection, Shepherd's Joy and Congregation all gain Support or soften enemies but are typed **Rhetoric (red = aggressive)**. If the type is wrong, the frame colour is wrong — and frame colour is the player's fastest read on a hand.
6. **`Praise` targets `AdjacentAllies`**, which resolves to *self* on a player card — the card currently does nothing to the neighbours its name implies.
7. **`Crisis of Faith`'s description promises "lose 4 Support"** but the asset only authors an exhaust. Description and effect disagree.
8. **`Deflated` has no effect at all** beyond exhausting itself.
9. **`The Incumbent` has no portrait, no moves, and no animal casting.** It's the final boss; it needs a design pass before an art brief.
10. **`docs/design/crookedile-style-v4-oddtaxi.png` — the reference of record — is not in the repo** (art bible §0.1 flags this too). Two PNGs sit in `docs/design/` under GUID filenames. Rename the right one before handing this doc to anyone, or every prompt here loses its visual anchor.

---

## 7. Quantities, at a glance

| Batch | Count | Size |
|---|---|---|
| Card illustrations | 48 (46 unblocked) | 1500 × 2148 |
| Type frames | 5 | 1500 × 2148, transparent art window |
| Rarity overlays | 3 | 1500 × 2148, mostly transparent |
| Card backs | 4 | 1500 × 2148 |
| Enemy portraits | 9 (7 cast, 2 proposed) | 1024 × 1024 |
| Ally icons | 6 (5 unblocked) | 256 × 256 |
| Status icons | 29 | 128 × 128 |
| Intent icons | 10 | 128 × 128 |
| Resource icons | 3 | 128 × 128 |
