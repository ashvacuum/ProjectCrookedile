# Celebrity: Glamour / IOU / Promises

> **Kind:** Design · **Status:** Built · **Updated:** 2026-10-04
>
> **Summary:** Celebrity banks and spends Glamour, borrows tempo through Debt, and earns repayment relief through next-turn commitments. Four core Composure cards protect the engine; rare Charm Offensive is a hybrid exception. Numbers are playtest starting points.
>
> **Source of truth:** this doc; values in [`Data/Cards/Celebrity/GlamourIou/`](../Assets/Data/Cards/Celebrity/GlamourIou/), shared rules in [`GlamourIou.cs`](../Assets/Scripts/Gameplay/Battle/Celebrity/GlamourIou.cs), effects in [`Celebrity/`](../Assets/Scripts/Gameplay/Battle/Celebrity/) · **Related:** [`core-design.md`](core-design.md) · [`needs-detailing.md`](needs-detailing.md)

## Identity

Celebrity wins through spectacle, borrowed momentum, and promises he may or may not deliver.
Composure means Support; Sway means an ordinary Opinion push through Denial and the existing modifiers.

- **Glamour:** bank it for round ticks, cash it out for Sway, or spend it on draws and suppression.
- **Debt:** acquire cards and energy now, then manage the repayment deadline.
- **Promises:** reserve existing Debt and earn cancellation by delivering a costly card next turn.
- **Soundbites:** generated, zero-energy, 2 Sway, Exhaust. No Glamour payment prompt on the token.

This revision supersedes the earlier defensive v0.2 proposals. There is no blanket Glamour damage bonus.
Enemy debuffs, hostility changes, and limited card recovery are now explicit parts of this kit.
Celebrity has no separate conversion mechanic; these effects use normal stance and Opinion rules.

## Shared rules

### Glamour

After each enemy turn, Glamour raises Opinion by its stacks, then loses 1. This direct raise bypasses Denial
and observes the echo-chamber rule. Each enemy hit leaking past Composure strips 1 Glamour.
Debt settlement is not an enemy hit and does not strip Glamour.
Iconic Line consumes Glamour before its normal Sway hit. Disarming Charm requires a hostile target and sufficient Glamour.
Its Weakened application does not change stance; the ordinary targeted-card crowd reaction still applies.

### Debt and Promises

Normal Debt settles at next player-turn start after energy refreshes. It consumes energy first, then lowers
Opinion directly, bypassing Composure. Battle end discards the bill. Debt gained this turn records the amount actually added.

Line of Credit is a Rhetoric setup, not a permanent Policy. Only the next positive Debt gain this turn is reduced;
it also discounts a random card still in hand until next turn. Old Debt is unaffected; an unused setup expires.
Payment Holiday skips the next normal settlement and adds 1 Debt. Rain Check remains an alternative with no added bill.
Neither postpones a missed Promise deadline.

Paid-Off Promises reserves up to 3 existing Debt, exempting that portion from next-turn-start repayment.
Play a card with printed energy cost 2+ during the next player turn to cancel it. Discounts do not invalidate delivery.
X-cost cards qualify when at least 2 energy was actually spent.
Setup-turn plays and ordinary effect replays do not count. One qualifying play fulfils all due commitments.
A missed commitment collects at the end of that next turn, using remaining energy first, then Opinion.
Fresh Debt keeps its normal deadline. Settle Up can remove ordinary or reserved Debt.

### Tokens, draws, and recovery

Soundbite: 0 energy, 2 Sway, Exhaust. Movie Quote: 0 energy, gain 1 Glamour, Exhaust.
Both are Rhetoric tokens excluded from acquisition pools.
Signature Catchphrase transforms existing Soundbites in every zone and future generated copies for this battle.
It never changes the campaign deck or source assets.

Media Training opens a once-per-player-turn button after normal draws: spend 2 Glamour to draw 1 without energy.
The first card play or ending the turn dismisses it. No mandatory modal prompt.
The button is disabled when Glamour is insufficient, the hand is full, or draw and discard piles are empty.
Playing the Policy does not open the current turn's window; extra copies do not grant extra activations.

Second Take and Advance Booking charge Debt only when selection successfully moves a non-Exhaust card.
Recovery preserves its normal energy cost and does not access Exhaust.
Booked cards arrive after next turn's normal draws; hand overflow goes to discard rather than losing the card.

### Poisoned Perception

Never Meet Your Heroes applies the debuff to a non-hostile opponent for 3 enemy turns.
Each enemy-turn start raises their Hostility by 2, respecting Fanatic, Devotion, Ward, and hostility limits.
Becoming hostile by any source removes the debuff before dealing 10 normal Sway; it triggers once.
Otherwise it expires after the third enemy turn. Reapplication refreshes the deadline without stacking payoffs.

## Retained defensive cards

No Comment, Smile and Wave, Behind the Podium, and Prepared Remarks retain their existing assets and upgrades.
No Comment currently grants 7 Composure. Prepared Remarks supplies 2 Soundbites alongside its 4 Composure.
Charm Offensive is the rare hybrid exception to the four core defensive card types.

## Reworked cards

- **Thumbs Up:** Pressure, 0 energy. If Glamour is 2 or less, gain 2 Glamour; deal 2 Sway. Upgraded: 3 Sway. Threshold checked first.
- **Press Release:** Rhetoric, 1 energy. If Glamour is 6+, gain 5 Glamour. Upgraded: gain 6.
- **Photo Op:** Policy, 1 energy. The first Pressure card played each turn grants 1 Glamour.
- **Going Viral:** Policy, 1 energy. Trigger a Glamour tick now, including decay. Upgraded: 0 energy.
- **Trending:** Policy, 2 energy. Gain 2 Glamour and add 2 Soundbites; token setup rather than another doubling effect.
- **Campaign Donors:** Policy, 1 energy. Deal 2 Sway per Debt gained this turn. Upgraded: 3 per Debt.
- **Fine Print:** Rhetoric, 0 energy. Draw 2, take 2 Debt, Exhaust. Upgraded: draw 3.
- **Settle Up:** Rhetoric, 0 energy. Cancel up to 1 Debt. Upgraded: up to 2. No Glamour or Composure payoff.
- **Line of Credit:** Rhetoric, 0 energy. The next Debt gain this turn is reduced by up to 2; discount a random hand card by 1 this turn. Exhaust.
- **Soundbite:** keep 0 energy, 2 Sway, Exhaust; extra options live on other cards and Policies.

## New cards

- **Iconic Line:** Enhanced Rhetoric, 1 energy. Consume all Glamour; deal 2 Sway per consumed stack. Upgraded: 0 energy.
- **Media Training:** Enhanced Policy, 1 energy. Install the optional start-of-turn Glamour draw button.
- **Signature Catchphrase:** Rare Policy, 1 energy. All Soundbites become Movie Quotes for this battle.
- **Victim Narrative:** Enhanced Rhetoric, 0 energy. Lose 5 Opinion bypassing Composure; double Glamour. Exhaust.
- **Disarming Charm:** Enhanced Rhetoric, 1 energy. Spend 2 Glamour to apply 3 Weakened to a hostile opponent.
- **Overpromise:** Rare Rhetoric, 1 energy. Take 3 Debt; the next card this turn deals triple its Sway. Exhaust. Other effects are not tripled.
- **Payment Holiday:** Enhanced Rhetoric, 0 energy. Defer the next normal settlement; take 1 Debt. Exhaust.
- **Paid-Off Promises:** Enhanced Rhetoric, 0 energy. Reserve up to 3 Debt; next turn's printed-cost-2+ play cancels it. Exhaust.
- **Second Take:** Enhanced Rhetoric, 0 energy. Recover a non-Exhaust discard card; take 1 Debt.
- **Advance Booking:** Enhanced Rhetoric, 0 energy. Reserve a non-Exhaust draw-pile card for next turn; take 2 Debt.
- **Never Meet Your Heroes:** Enhanced Rhetoric, 1 energy. Apply Poisoned Perception to a non-hostile opponent.
- **Charm Offensive:** Rare Pressure, X energy. Deal 2X Sway, gain 2X Composure, reduce Hostility by X, in that order. X is energy actually spent; ordinary modifiers and subsequent crowd reaction apply.
- **Movie Quote:** Basic generated-only Rhetoric, 0 energy. Gain 1 Glamour. Exhaust.

Unlisted existing cards retain their existing values. New cards reuse Soundbite's illustration as placeholder art.

## Authoring and validation

Effects use the existing SerializeReference model, visible in the Database window's Building blocks tab.
Each mechanic has its own effect type: MediaTrainingEffect, SignatureCatchphraseEffect, LineOfCreditEffect,
OverpromiseEffect, PaidOffPromisesEffect, DisarmingCharmEffect, and CharmOffensiveEffect.
Debt policies likewise use OpenTabEffect, BailoutEffect, DebtWaiverEffect, DelayDebtSettlementEffect,
and OverdraftEffect. Choose the mechanic through the effect type picker; each exposes only its own named
tuning fields. Enums may configure a single mechanic's target, source, filter, or amount formula; they must
not select unrelated mechanics. The project rule is recorded in `AGENTS.md`.
Media Training accepts an authored BattleUI button or creates one above End Turn when unassigned.
Runtime transformations and policy rules reset with each battle; turn setups reset at player-turn start.
Reward bags are a separate future change.
Focused Play Mode tests cover card effects and deadlines; CardSmokeTests exercise each card against three stances.

## Playtest questions

- Does Press Release reward stockpiling without stranding low-Glamour hands?
- Are zero-energy Debt tools constrained enough by repayment, hand size, and Exhaust?
- Are promised Debt deadlines and printed-cost delivery clear?
- Does Overpromise create satisfying big turns without making ordinary attacks irrelevant?
- Is the Glamour button readable beside End Turn at supported resolutions?
