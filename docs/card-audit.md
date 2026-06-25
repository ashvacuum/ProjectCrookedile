# Card Audit — fantasy fulfillment pass

*Evaluates every authored card in `Assets/Resources/Cards/` against the fantasies in `core-design.md` and `crookedile-starter-decks.md`. Goal: find the design decisions that fight the fantasy.*

CardType legend (from `Enums.cs`): **0 Pressure** (green, persuade/de-escalate) · **1 Rhetoric** (red, aggressive) · **2 Policy** (blue, lean) · **3 Heckle** (purple, temp/unplayable) · **4 Scandal** (dark, unplayable clog). Rarity: 0 basic / 1 uncommon / 2 rare.

---

## The headline problems (bad design decisions, ranked by damage to the fantasy)

### 1. The card text contradicts the #1 fantasy ("no fight, no HP, work a crowd")
Nearly every offensive card reads **"Deal X Resolve damage"** / "Deal 8 Damage." The core doc opens with *"You are not winning a fight… there are no enemy HP pools."* The single most-repeated word in the deck is "damage." Offenders: Accusation, All or Nothing, Bold Accusation, Charming Gambit, Ego Trip, Family Name, Find Common Ground, Inherited Privilege, Pull Strings, Spotlight Hog, Martyrdom. Vocabulary is also **inconsistent** — "Resolve," "Opinion," "Support," "Damage" all used for the same meter. Pick one verb set ("push the meter," "Support," "Opinion") and purge "damage/Resolve" everywhere.

### 2. Faith Leader's spine is essentially not built in cards
The locked engine is *stack Guilt/Shame/Doubt to 3 → convert → Fanatic burst → revert + Jaded.* In the actual cards:
- **Pacify-status appliers barely exist:** Guilt is on Judgement and Excommunicate; Shame only on Excommunicate; **Doubt has zero playable applier** (the only "Doubt" is an unplayable Scandal curse). There is no starter stacker pair, so the "any 3 = convert" lesson the starter deck is supposed to teach can't happen.
- **The convert payoff is one rare card.** `ConsumeStatusAndRaiseOpinion` appears only on **Absolution** (rare). The whole class can't reach its loop from the starter.
- **Sermon — the harvest/payoff card — has a NULL effect** (`class:` empty). It literally does nothing.
- **Pacify** (the card) just runs `ReduceHostilityEffect`. It is not the conversion mechanic its name implies; it's a hostility-down card wearing the engine's name.

### 3. Nepo Baby has no Patronage and no summon — its entire identity is missing
Locked identity: *burn a card → bank Patronage → summon bodies.* No card has a `Patronage` cost; no card runs `GeneratePatronageEffect` or `SummonBodyEffect` (both exist in code, authored on zero cards). The seven nepobaby cards (Backroom Deal, Call In Favor, Dynasty Network, Family Name, Inherited Privilege, Pull Strings, Trust Fund) are generic draw/push/AP value cards — swap the tag and they're any class. The schemer plays like a vanilla goodstuff deck.

### 4. Celebrity has zero cards
Not one card tagged `celebrity`. None of Attention / Scandal-generate / Drama King authored (`GainAttentionEffect`, `SpinScandalsEffect`, `SpendAttentionEffect` exist in code, used nowhere). Celebrity is a passive and an empty pool. The "open canvas" can't be drafted because there's nothing in it.

### 5. Scandal is implemented backwards
Design: Scandals are an **anti-Curse you WANT** — they clog the hand but *power your other cards per-Scandal*, with a spin/cash-out. Actual Scandal cards (Scandal, Doubt, Crisis of Faith, False Accusations) are **pure Slay-the-Spire punishment curses** (lose Support/Resolve while held, discard random) — and they're tagged **faithleader/universal**, not Celebrity. There is **no card that rewards carrying a Scandal.** The mechanic that defines Celebrity's most distinctive line is built as its exact opposite.

### 6. Description ↔ effect desync is everywhere (content is half-built)
Cards whose text says the opposite of, or unrelated to, what they do:
- **Ego Trip** — text "Raise Hostility by 3," effect *reduces* hostility.
- **Bold Accusation** — text "Increase 2 Hostility," effect *reduces* hostility.
- **Martyrdom** — text "Deal 8 damage. Raise all enemies' Hostility by 2," effects are *GainAP-next-turn + single-target hostility* (no pressure, not all-enemies).
- **Charming Gambit** — text "50% chance: Draw 1," effects are *MakeCardFree* (no draw).
- **"Pray"** (file `Gather Thoughts`) — text "Gain 4 Support," effects *GainBuffer + raise ALL enemies' hostility* (text omits the hostility entirely).

Plus **~8 empty descriptions** (Condemnation, Pacify, Tax the Rich, Gospel, Judgement, Rebuke, Stir the Flock) and **~8 "(Configure in Inspector)" placeholders** (Crisis of Faith, False Prophet, Holy Patience, Holy Alliance, Pastoral Care, Moral High Ground, Revelation). These are unfinished cards sitting live in the database.

### 7. Null-effect cards live in the DB: **Sermon** and **Pastoral Care** both have `class:` empty — they do nothing when played.

### 8. Content hygiene: file-name vs card-name divergence + duplicates
`Gather Thoughts.asset` is the card **"Pray"**; `Find Common Ground.asset` is the card **"Rebuke"**; and there is a *separate* card also named **"Rebuke"** (`Rebuke.asset`). Two cards named Rebuke, two files misnamed. Cards were repurposed without renaming the asset — a maintenance trap.

### 9. Hostility tools lean heavily toward REDUCING it — fights the "keep a villain" fantasy
Reduce-hostility cards: Deflect, Pacify, Fan Favorite, Find Common Ground, Pull Strings, Spotlight Hog, Ego Trip, Bold Accusation. Seed-hostility cards: Stir the Flock, False Prophet, Martyrdom (and a couple of the untagged). The doc's thesis is *hostility is a resource; never sweep the room; the echo chamber punishes converting everyone* — but the deck is stocked with cheap "calm the room" buttons and few reasons to seed. The default echo-chamber escape valve (the universal hostility card) isn't clearly present as a starter in any class.

### 10. CardType color taxonomy carries no meaning as authored
Pressure (green, persuade) vs Rhetoric (red, aggressive) is applied at random: draw-only cards (Call In Favor, Backroom Deal, Dynasty Network) are **Rhetoric**; damage is split across both (Find Common Ground deals 5 as **Pressure**, Accusation deals 6 as **Rhetoric**). If the colors are supposed to mean something to the player (and inform Silenced = "no Rhetoric"), they need a consistent rule.

---

## Per-class card list & verdict

### Faith Leader (the converter)
| Card | Type | R | Starter | Effects (as authored) | Verdict |
|---|---|---|---|---|---|
| Accusation | Rhetoric | 0 | ✓ | Push 6 + Draw 2 | Fine value card; text "Resolve damage"; not FL-flavored |
| Blessing | Pressure | 0 | ✓ | RaiseOpinion = Support, consume Support | OK payoff; no link to conversion |
| Deflect | Rhetoric | 0 | ✓ | Support 3 + ReduceHostility 1 | Generic; reduces hostility |
| "Pray" (`Gather Thoughts`) | Rhetoric | 0 | ✓ | Support + raise ALL hostility | Text/effect desync; weird combo |
| "Rebuke" (`Find Common Ground`) | Pressure | 0 | ✓ | ReduceHostility + Push 5 | Misnamed file; dup name |
| Judgement | Rhetoric | 0 | ✓ | Apply **Guilt** | Only real stacker in starter; empty desc |
| Stir the Flock | Rhetoric | 0 | ✓ | RaiseTargetHostility + Push | Good (seeds villain); empty desc |
| Excommunicate | Rhetoric | 1 | | **Guilt + Shame** | The one true multi-stacker — should be core, not uncommon |
| Condemnation | Rhetoric | 2 | | PressureBasedOnHostility | Empty desc; legacy effect (superseded) |
| Congregation | Policy | 1 | | Power: TurnStart apply Strength | Strength isn't a pacify status; off-engine |
| Absolution | Pressure | 2 | | **ConsumeStatusAndRaiseOpinion** | The ONLY convert-payoff in the game; gated to rare |
| False Prophet | Rhetoric | 0 | | Push + RaiseTargetHostility | Placeholder desc |
| Gospel | Policy | 1 | | Power: on Fanatic→Draw | On-engine idea; empty desc |
| Holy Patience | Rhetoric | 1 | | (placeholder) | Unbuilt |
| Holy Alliance | Policy | 2 | | GainBuffer | Placeholder; "resolve doubles" not implemented |
| Pacify | Rhetoric | 0 | | ReduceHostility | Name lies — not the convert mechanic |
| Pastoral Care | Rhetoric | 1 | | **NULL** | Does nothing |
| Moral High Ground | Rhetoric | 0 | | Power: CardDrawn→Retain + Buffer | OK; no FL identity |
| Sermon | Pressure | 0 | | **NULL** | Does nothing — and it's the harvest payoff |
| Rebuke | Pressure | 0 | | RaiseOpinion | Empty desc; dup name with "Rebuke" above |
| Righteous Fury | Rhetoric | 0 | | Push + LoseBuffer | OK |
| Preach | Pressure | 0 | | Push + Buffer | Generic |
| Revelation | Pressure | 2 | | DrawCards | Placeholder; "scry + free play" not built |
| Martyrdom | Rhetoric | 2 | | GainAP-next + RaiseTargetHostility | Desc fully desynced |
| Prayer | Pressure | 1 | | GainBuffer | "Strength + token" not implemented |

**FL gap:** Doubt-applier (playable), a starter Shame card, a multi-status reward suite, and a *working* Sermon. Right now the spine reaches the table on Judgement + Excommunicate + Absolution only.

### Nepo Baby (the schemer)
| Card | Type | Starter | Effects | Verdict |
|---|---|---|---|---|
| Backroom Deal | Rhetoric | ✓ | Draw 3 + AP-next | Generic value |
| Call In Favor | Rhetoric | ✓ | Draw 2 | Generic |
| Dynasty Network | Rhetoric | ✓ | Draw 2 + Discard 1 | Generic |
| Family Name | Pressure | ✓ | Push 3 | Vanilla strike |
| Inherited Privilege | Pressure | ✓ | Push 5 + Draw 1 | Generic |
| Pull Strings | Rhetoric | ✓ | Push 4 + ReduceHostility | Generic |
| Trust Fund | Rhetoric | ✓ | Support 2 + AP this turn | Generic |

**Nepo gap:** everything. No Patronage cost, no `GeneratePatronage` (burn-a-card), no `SummonBody`, no Plant. The class identity (`crookedile-starter-decks.md` §Nepo) is 0% authored.

### Celebrity (open canvas)
**No cards exist.** Needed: basics (Soundbite/Spin Control), Read the Room, Manufactured Drama (flip to hostile), and one seed each of Attention / Scandal-generate / Woe-Is-Me. None authored.

### Heckle / status cards (type 3)
| Card | Effects | Note |
|---|---|---|
| Deflated | Power: TurnEnd Exhaust | Ethereal dead card; fine as junk |
| Disappointed | Power: TurnEnd Exhaust + Draw | Junk that cantrips — odd |
| Emptiness | Power: CardDrawn → GainAP (negative?) | Verify sign; desc "removes 1 AP" vs GainAP effect |
| Self Doubt | Power: TurnEnd apply Rattled + Exhaust | OK debuff-curse |
| Self Pity | Power: TurnEnd self-Push + Exhaust | "Lose 1 Resolve" wording |

### Scandal cards (type 4) — all built as punishment, none as the Celebrity resource
| Card | Effects | Note |
|---|---|---|
| Scandal | Power: TurnEnd lose Resolve | Punishment curse; tagged faithleader |
| Doubt | Power: TurnEnd Push (lose Support) | A *curse* named Doubt — collides with the FL pacify status |
| Crisis of Faith | lose 4 Support EoT | Placeholder punishment |
| False Accusations | Power: CardDrawn → Discard | Punishment |

### Untagged starters (class unclear — likely meant for Celebrity/universal)
All or Nothing (random push 1–9), Bold Accusation (push 5 + reduce hostility — *text says increase*), Charming Gambit (push 3 + MakeCardFree — *text says draw*), Ego Trip (reduce hostility + push 8 — *text says raise*), Fan Favorite (consume Support → reduce hostility), High Stakes (discard hand, draw 3), Spotlight Hog (push 6 + Support 3 + reduce hostility 2). These need a tag and a desc/effect reconciliation.

---

## Suggested fix order (cheap → identity-defining)
1. **Text pass:** purge "damage/Resolve," settle one vocabulary, fix the ~5 desc↔effect inversions, fill the ~16 empty/placeholder descs. Pure data; biggest fantasy ROI.
2. **Un-break the two null cards** (Sermon, Pastoral Care) and the misnamed/duplicate Rebuke files.
3. **Faith Leader spine:** author a Doubt applier + a starter Shame stacker; make Sermon the real harvest; demote a convert-payoff into the common pool so the loop is reachable.
4. **Scandal inversion:** add per-Scandal payoff cards + a spin/cash-out; re-tag Scandals to Celebrity; stop using them as FL punishment.
5. **Nepo Patronage + summon cards** (the effects already exist — author Call in Patronage, Call a Favor, Plant).
6. **Celebrity card pool** from zero — at minimum the starter's seven.
