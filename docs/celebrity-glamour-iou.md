# Celebrity: Glamour / IOU build (proposal v0.1)

**Status: proposal, built as a variant.** `core-design.md` section 7 (Attention / Scandal / Drama King) is still the
canonical Celebrity design. This build exists to test an alternative against it. All numbers are placeholders and live
on the card assets; shared engine rules are in `CelebrityRules` (`Assets/Scripts/Gameplay/Battle/Celebrity/GlamourIou.cs`).

Vocabulary: **Composure** = Support (the shield). **Sway** = a push on the Opinion meter. Statuses, not keywords.

## Fantasy
A guy who knows nothing, talks the talk, and never walks the walk. Every promise is empty and it is all for the press.
He does not convert enemies. He wins by spectacle and borrowed momentum.

## Structure (Silent-style: base + poles + connector)
- **Base:** plain, defensive, good alone. Playable with no pole online.
- **Glamour pole:** slow, defensive, long-fight engine.
- **IOU pole:** fast, aggressive overcharge engine (borrow now, pay next turn).
- **Connector: Soundbite tokens.** Cheap generated cards that glue the poles together.

Guardrails: nothing stacks on enemies and nothing moves Hostility toward Receptive (Faith Leader's lane); no fetch,
tutor, search or choose-from-pile effects and no redirect or reflect (Nepo Baby's lane); no new keywords; no class
passive designed for this build. Persistent Policies declare the build, one-shot Policies are tactical.

## Rules
**Glamour** (player status, stacking, never decays on its own)
1. Gained from card effects (`GainGlamourEffect`).
2. After all enemy actions, Opinion rises by the stack count, then 1 stack is removed (`BattleManager.TickGlamour`).
3. Scrutiny: each Opinion hit that leaks past Composure strips 1 Glamour, flat. A fully blocked hit strips nothing.
4. Enemy hits resolve before the tick. Unpaid-Debt damage is not a hit.

**Debt** (player number, `CelebrityState`)
1. Gained from Borrow cards.
2. Settled at the start of the player's next turn, after energy refreshes: Debt reduces energy (floor 0), the remainder
   becomes Opinion damage 1:1. Debt resets to 0.
3. Default: unpaid-Debt damage ignores Composure (`CelebrityRules.UnpaidDebtBlockable`).
4. Debt outstanding when the fight ends is never collected.

**Soundbite** (token): cost 0, Exhaust, Sway 2, generated only (Rhetoric).

## Starter deck (10)
3 Hot Take (Pressure, 1: Sway 6), 3 No Comment (Pressure, 1: 5 Composure), 3 Autograph (Pressure, 1: -2 Hostility),
1 **Smile and Wave** (Rhetoric, 1: 5 Composure + 3 Glamour; upgraded 7 + 4).

## Cards
| Pole | Card | Type / rarity / cost | Effect |
|---|---|---|---|
| Glamour | Thumbs Up | Rhetoric basic 0 | 3 Composure, 1 Glamour |
| Glamour | Press Release | Rhetoric basic 1 | 6 Composure, 2 Glamour |
| Glamour | Prepared Remarks | Rhetoric basic 1 | 4 Composure, add 2 Soundbites |
| Glamour | **Photo Op** | Policy enhanced 1 | Persistent: whenever you gain Composure, gain 1 Glamour |
| Glamour | Behind the Podium | Rhetoric enhanced 1 | Composure equal to Glamour |
| Glamour | Trending | Rhetoric rare 1, exhaust | Double Glamour |
| Glamour | Going Viral | Rhetoric rare 1 | Glamour equal to half current Composure |
| Glamour | Standing Ovation | Rhetoric enhanced 2 | Sway equal to Glamour |
| Glamour | Fan Mail | Rhetoric enhanced 1 | Draw 1; draw 1 more at 5+ Glamour |
| IOU | Cash Advance (Borrow) | Rhetoric basic 1, tag `borrow` | +2 energy, +2 Debt, add 2 Soundbites |
| IOU | Calling It In (Leverage) | Rhetoric enhanced 1 | Sway = 4 x Debt |
| IOU | Settle Up (Forgive) | Rhetoric enhanced 1, exhaust | Cancel all Debt, 2 Composure per Debt |
| IOU | **Line of Credit** | Policy enhanced 1 | Persistent: first Debt gain each turn is 1 less |
| IOU | **Open Tab** | Policy enhanced 1 | Persistent: first Borrow card each turn costs 0 |
| IOU | **Bailout** | Policy rare 2 | Persistent: unpaid-Debt damage adds that many Soundbites (cap 3/turn) |
| IOU | Too Big to Fail | Policy enhanced 2, exhaust | First unpaid-Debt damage this fight deals none |
| IOU | Campaign Donors | Policy enhanced 1 | Composure per Debt gained this turn |
| IOU | Overdraft | Policy basic 1, exhaust | This turn Borrow gives double energy and double Debt |
| IOU | Rain Check | Policy basic 1, exhaust | Delay Debt settlement one turn |
| IOU | Fine Print | Policy basic 0, exhaust | Composure equal to current Debt |

Rarities and costs not given in the original spec (Trending, Going Viral, Standing Ovation, Overdraft, Rain Check,
Fine Print, Calling It In, Settle Up, Cash Advance) are my placeholders.

## Open questions (default implemented)
1. Which Policies persist vs one-shot: assignment above is a proposal. Persistence is a battle flag set when the Policy
   is played, so it lasts the fight regardless of where the card goes.
2. Unpaid-Debt damage: unblockable (flip `UnpaidDebtBlockable`).
3. Soundbite card type: Rhetoric.
4. Photo Op: enhanced, cost 1.
5. Class passive: none designed for this build. The `Actor` origin currently points at `Daddy's Gifts.asset`, a
   once-per-battle mulligan that duplicates Nepo Baby's — a placeholder to replace, not a design.

## Authoring and testing
- `Crookedile > Celebrity > Build Glamour-IOU Cards` writes the assets to `Assets/Data/Cards/Celebrity/GlamourIou/`
  (re-running overwrites in place). `Set Actor Starter Deck to Glamour-IOU` swaps the Actor starter deck.
  `Self-Check Debt Rules` runs asserts on settlement, Line of Credit, Rain Check and the waiver.
- Bot run: use the existing playtest runner with origin `Actor`. Snowball combos to read first: Photo Op + Open Tab +
  Campaign Donors; Too Big to Fail + Bailout; Overdraft + Rain Check; Trending on a Photo Op deck.
- Known gaps: no Glamour/Debt HUD, no status icon, cards have no artwork (so they read as in-development),
  and the bots do not value Glamour or Debt (they rank cards by Opinion preview).
