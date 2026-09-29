# Crookedile — Art Prompt Database

*Generated from the live assets (2026-09-11): 48 card assets, 9 enemy assets, 6 ally assets.
Paste **§1 the style block** first, then the subject line from the table. Style is locked to
`docs/reference/style-mock-prompt.md` v4 (Odd Taxi); sizes are from `docs/art-bible.md` §0.5.*

**Read §6 before generating** — several entries are blocked on data that isn't authored yet.

---

## 1. The style block (paste before every prompt)

### 1a. Card illustrations

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
SUBJECT: <paste subject line here>
```

*Midjourney tail:* `--ar 7:10 --style raw --s 150 --no text,letters,watermark,frame`
*Gemini / ChatGPT tail:* `Render at 1500x2148 px, portrait, PNG.`

### 1b. Enemy portraits

Same block, but swap the last two sentences for:

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

Columns: **card · type · AP · what it actually does (read off the asset) · subject line**.
"↑" = the upgraded value.

### 3a. Faith Leader — Basic (10)

| Card | Type | AP | Mechanic | Subject line |
|---|---|---|---|---|
| Drown out the Haters | Pressure | 1 | Push meter 10 ↑15 on one enemy; only while a hostile is present | A preacher leaning hard into a pulpit microphone, mouth open mid-word; two hecklers in the front row drowned out, their shouts swallowed by the sound |
| Guilt | Pressure | 1 · starter | Apply 2 Guilt to one enemy | A parishioner head bowed, shoulders sagging under an invisible weight, hands slack in their lap; the preacher's shadow falls across them |
| Pressure | Pressure | 1 · starter | Push meter 7 ↑10 + shift that enemy's stance 3 | A preacher's hand resting a little too firmly on a seated man's shoulder, smiling; the man not smiling back |
| Quiet Reflection | Rhetoric ⚑ | 0 | Delayed: gain 8 ↑10 Support | An empty pew, a single candle, a folded barong laid over the backrest; nobody in the room |
| Shepherd's Joy | Rhetoric ⚑ | 1 · starter | This turn: gain 3 ↑5 Support each time an enemy turns receptive | A preacher's face breaking into a small genuine smile as one person in the crowd finally nods along |
| **(unnamed)** ⚑ | Rhetoric | 1 | Apply 1 ↑2 Doubt to every hostile enemy | A wide silent crowd, arms folded, nobody speaking; one raised placard lowered halfway |
| Sow Discord | Pressure | 1 ↑0 | Push meter 5 ↑7 + 1 Doubt on one enemy | A whispered aside behind a cupped hand, a question mark of doubt curling over the listener's ear |
| Stir the Flock | Rhetoric | 1 ↑0 · starter | Draw 2 + shift every enemy 2 toward receptive | A preacher raising both arms, the congregation half-rising from their seats in a ragged wave |
| Support | Rhetoric ⚑ | 1 · starter | Gain 4 ↑6 Support | A row of hands clasped together on a pew rail, a sampaguita garland looped over the knuckles |
| Traitor to the Cause | Rhetoric | 1 · starter | Apply 2 Shame to one enemy (needs a hostile present) | A man in a campaign shirt turning his face away from a pointing finger, collar pulled up; the room watching him, not the speaker |

### 3b. Faith Leader — Enhanced (25)

| Card | Type | AP | Mechanic | Subject line |
|---|---|---|---|---|
| Absolve Sins | Pressure | 1 | Push meter 2 ↑5 + soften one enemy 5 ↓2 | A hand laid flat on a bowed forehead; the recipient's clenched jaw loosening |
| Blessed are the Persecuted | Pressure | 1 | This turn: meter +5 every time any stance shifts | A preacher standing serenely in a drizzle of thrown paper and tabloid pages, eyes closed, arms open |
| Call out the Wicked | Rhetoric | 2 ↑1 | Gain 1 Strength | A finger levelled at one specific face in the crowd; the accused recoiling, everyone else turning to look |
| Congregation | Rhetoric ⚑ | 2 | Soften one enemy 3 ↑5 + delayed +2 AP | A flock filing in through double doors by candlelight, the pews filling row by row |
| Faith | Rhetoric | 3 ↑2 | Gain 3 Support + soften one enemy 2 | A worn rosary wound tight around a fist resting on a lectern |
| Fruits of Conversion | Policy | 2 ↑1 | This turn: meter +5 each time an enemy turns receptive | A basket of mangoes and rice set at the foot of a pulpit, hands reaching in to take some |
| God's Chosen | Rhetoric | 2 | Strip Jaded + apply 1 Fanatic to one enemy | One ordinary man in the crowd lit slightly brighter than everyone around him, eyes wide, standing while the rest sit |
| Gospel | Policy | 3 ↑2 | Convert every pacified enemy at once (consume Guilt/Shame/Doubt → Fanatic) | An open holy book on a lectern radiating flat light, pages turning by themselves, the whole front row rising |
| Hallelujah | Rhetoric | 3 ↑2 | Gain 2 Strength per Fanatic in the room | Several people in a crowd with both arms thrown up at once, deadpan faces, perfectly synchronised |
| Have you no Shame? | Policy | 2 ↑1 | This turn: any stance shift also applies 1 Shame to that enemy | A preacher pausing mid-sentence, eyebrow raised, head tilted, letting the silence do the work |
| Holier Than Thou ⚑ | Pressure | 3 ↑2 | Push meter 30 on one enemy | A preacher on a raised marble step looking calmly down at someone on floor level, chin level — not smug, just higher |
| I am speaking | Pressure | 2 ↑1 | Silence every enemy 1 | A flat open palm raised toward the panel; every mouth in the row closed at once |
| Impenetrable Belief ⚑ | Pressure | 2 ↑1 | Delayed: gain 7 ↑10 Support | A heavy carved church door, shut, viewed straight on; a thrown tabloid lying at its base |
| Insinuate | Rhetoric | 2 ↑1 | 2 Doubt + push meter 5 on one enemy | A preacher speaking to the room while his eyes are on one seated man; that man's confidence visibly draining |
| Name and Shame | Pressure | 2 ↑1 | Silence one enemy 1 + raise that enemy's hostility 2 | A person shielding their face from a semicircle of phones and a boom mic, a name card still on the desk in front of them |
| Persecution Complex | Rhetoric | 1 | This turn: meter +5 every time any stance shifts | A preacher pointing at himself, wounded expression, in front of a wall of ordinary unbothered faces |
| Praise ⚑ | Rhetoric | 1 | Soften 2 ↑4 (target currently resolves to self — see §6) | A preacher warmly clasping the hand of the man seated beside the one he means to praise — the wrong man |
| Preach | Pressure | 1 | Draw 1 + push meter 8 on one enemy | A lectern fused with a megaphone, words rendered as a flat visible pressure wave pushing the front row back |
| Quiet the Haters | Pressure | 2 ↑1 | Silence 1 random hostile per Fanatic in the room | A converted follower turning around in their seat to shush the heckler behind them |
| Retribution | Pressure | 1 | Push meter 5 on one enemy | A gavel-hard fist coming down on a lectern edge; a hymnal jumping |
| Sacrificial Lamb | Pressure | 1 | Push meter 10 + raise that enemy's hostility 10 ↓4 | A preacher gently steering one man forward to the front of the room to take the blame, hand between his shoulder blades |
| Sermon ⚑ | Pressure | 0 | Push meter 5 + raise every enemy's hostility 5 ↓2 | Pulpit wide shot from behind the speaker: a rapt crowd, flat even light from high windows, a few folded arms at the back |
| The Other Cheek | Pressure | 4 ↑3 | Push meter 15 + draw 3 | A man taking a thrown egg on the cheek without flinching, still speaking, the crowd going quiet |
| United Faith | Pressure | 2 | Push meter 20 ↑25 (needs a hostile present) | An entire congregation standing in one flat unbroken block of shoulders, seen from the pulpit |
| Word is Law | Pressure | 2 | Push meter 25 + soften every enemy 3 + delayed +2 AP | A church seal stamped onto a government document, the ink still wet |

### 3c. Faith Leader — Rare (3)

| Card | Type | AP | Mechanic | Subject line |
|---|---|---|---|---|
| The World Is Against Us | Rhetoric | 3 ↑2 | Apply 1 Fanatic to every receptive enemy | A tight circle of followers back-to-back facing outward, calm faces, a wall of tabloid headlines and camera flashes pressing in around them |
| Us vs Them | Policy | 3 ↑2 | This turn: applying a status strips Jaded off that enemy | A room divided by a painted line down the middle of the floor, two blocks of people, nobody crossing |
| Zealotry | Policy | 3 ↑2 | This turn: applying a status also makes that enemy a Fanatic | A single figure with arms outstretched in sacrificial pose, the crowd around them lit and inflamed, the figure themselves calm |

### 3d. Status cards — Heckle, purple frame (6)

*These clog the hand for a turn. Art should feel thin and temporary — more empty space than the real cards.*

| Card | AP | Mechanic | Subject line |
|---|---|---|---|
| Deflated ⚑ | — | Unplayable; exhausts at end of turn (no other effect authored) | A sagging campaign balloon caught on a plastic chair, half deflated |
| Disappointed | 1 | Unplayable; at end of turn draw 1 and exhaust | An elderly woman in the third row looking at the speaker with flat, quiet disappointment |
| Emptiness | — | Playable; costs you 1 AP the moment it is drawn | A wide empty folding-chair hall after an event, one chair knocked over |
| Self Doubt | 0 | Unplayable; at end of turn apply 1 Rattled to yourself and exhaust | A crocodile politician catching his own reflection in a dark window and not liking it |
| Self Pity | 1 | At end of turn nudge the meter 1 toward yourself, then exhaust | A crocodile politician sitting alone backstage on a plastic stool, tie loosened, staring at nothing |
| Self Pity *(file: `Exile.asset`)* ⚑ | 1 | At end of turn raise every enemy's hostility 1, then exhaust | A figure walking out of a hall alone while the room behind him turns to watch him go |

### 3e. Curses — Scandal, dark crimson frame (4)

*All unplayable, permanent deck pollution. Treatment: torn tabloid, halftone print dots, redaction bars — the one place a rougher texture is allowed.*

| Card | Mechanic | Subject line |
|---|---|---|
| Crisis of Faith ⚑ | Exhausts at end of turn *(the described "lose 4 Support" is not authored — see §6)* | A cracked plaster saint statue in a side chapel, one hand broken off on the floor |
| Doubt | Lose 2 meter at end of turn, every turn | A tabloid front page with the candidate's face and a single enormous question mark over it |
| False Accusations | Discards a random card the moment it is drawn | A fabricated document held up to a bank of cameras, the type too blurry to read |
| Scandal | Lose 3 meter at end of turn, every turn | A folded tabloid on a jeepney seat, a grainy long-lens photo above the fold, redaction bars over two faces |

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
2. **`Silent Majority.asset` has an empty `_cardName`.** Frame, art and text all key off a name it doesn't have.
3. **`New Ally 3.asset` is fully empty** — no name, no id, no passive. Nothing to illustrate.
4. **Two assets both name themselves "Self Pity"** (`Self Pity.asset` and `Exile.asset`), with different effects. One is presumably Exile. Fix before art, or you get two identical illustrations.
5. **Three "Enhanced" cards are authored as Basic rarity** — Holier Than Thou, Impenetrable Belief, Sermon. They sit in the Enhanced folder but carry `_rarity: 0`, so they'd get the plain overlay. Decide which is true.
6. **Frame colour looks scrambled on the defensive cards.** Support, Quiet Reflection, Shepherd's Joy and Congregation all gain Support or soften enemies but are typed **Rhetoric (red = aggressive)**. If the type is wrong, the frame colour is wrong — and frame colour is the player's fastest read on a hand.
7. **`Praise` targets `AdjacentAllies`**, which resolves to *self* on a player card — the card currently does nothing to the neighbours its name implies.
8. **`Crisis of Faith`'s description promises "lose 4 Support"** but the asset only authors an exhaust. Description and effect disagree.
9. **`Deflated` has no effect at all** beyond exhausting itself.
10. **`The Incumbent` has no portrait, no moves, and no animal casting.** It's the final boss; it needs a design pass before an art brief.
11. **`docs/design/crookedile-style-v4-oddtaxi.png` — the reference of record — is not in the repo** (art bible §0.1 flags this too). Two PNGs sit in `docs/design/` under GUID filenames. Rename the right one before handing this doc to anyone, or every prompt here loses its visual anchor.
12. **The art bible §3 card briefs are stale.** They brief 15 cards (Rebuke, Pray, Call Out Sin, Excommunicate, Absolution, Martyrdom, Revelation…) that **do not exist as assets**. The 48 above are what's actually built.

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
