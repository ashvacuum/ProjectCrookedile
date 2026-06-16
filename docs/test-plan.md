# Test Plan — rebuilding the test suite

*As of 2026-06-10. Current coverage is a single file (`EffectResolverTest.cs`) written against the enum-status world. The suite gets rebuilt against the StatusBehavior API and doubles as the gate for the destructive half of the status cutover (see `work-now.md` §1).*

## Principles

- **Edit-mode tests where possible** — the pressure pipeline, ledger, and status hooks are plain C# math; they shouldn't need a scene.
- **Test the rules, not the wiring.** The EventBus is notify-only by architectural rule, so tests assert on state (opinion value, shield values, stacks), not on events fired.
- **Each locked design rule gets a named test.** When a playtest motivates a tuning change, the test tells you what rule you're bending.

## Suite 1 — OpinionLedger (the single command path)

The most leveraged tests in the project; everything routes through `ApplyPressure`.

- Player pressure consumes Denial before moving the meter; overflow moves the meter.
- Enemy pressure consumes Support before moving the meter.
- Shields never go negative; meter clamps to [0, 100].
- `RaiseDirect` bypasses Denial (the Fanatic-burst path).
- Turn-start shield decay.
- Win at 100 / loss at 0 / Judgment at turn limit (>half = win).

## Suite 2 — Status behavior hooks (the cutover gate)

One test per hook, using representative behaviors:

- `ModifyOutgoing`: Guilt reduces enemy push by stacks; Weakened/Strength on player pressure.
- `ModifyIncoming`: Vulnerable; Intangible override; Exposed consume-on-hit.
- `SupportGained` / `DenialGained`: Shame reduces enemy Denial gain.
- `ModifyCardCost`.
- Hostility flags: Hardened blocks reduction, Fanatic blocks gain, Devotion resists per stack.
- **Parity check as a test:** every `StatusRegistry` behavior has a unique Id and an icon-map entry (replaces the eyeball check in the Content Hub parity tab as the destructive-step gate).

## Suite 3 — Faith Leader conversion engine

The most-specified spine; the math the class lives or dies on.

- 3 mixed pacify statuses (Guilt/Shame/Doubt in any combination) trigger conversion; 2 don't.
- Threshold = 3 + Jaded stacks; Jaded +1 applied on each conversion; Jaded does NOT count toward its own threshold.
- Pacify statuses consumed on convert; over-stack consumed and reflected in burst size (burst = consumedStacks × ConvertBurstPerStack).
- Burst goes through `RaiseDirect` (not absorbed by Denial).
- Converted enemy reverts to hostility 0 (neutral, not receptive — the echo-chamber-immunity property).
- Hardened target: Silenced applied instead, no burst, no Jaded.
- `ConversionsThisTurn` increments and resets at player turn start.

## Suite 4 — Crowd rules

- **Echo Chamber:** all-receptive ⇒ opinion gains halved; per-turn decay applied; breaking the chamber (one enemy non-receptive) stops both immediately, same turn.
- **Turncoat cascade:** receptive→hostile flip applies Turncoat status, opinion hit, adjacent hostility nudge, forced-aggressive intent.
- Hostility multiplier math: `max(0.1, 1 + h × 0.5)` for h > 0; receptive skip chance for h < 0.

## Suite 5 — Economy & archetype resources

- Patronage: only gained via sacrifice; value formula (base cost, +1 Rare, +1 Upgraded, flat 1 for 0-cost/Status/Scandal); spend gates `CanPlayCard`; double-gated (AP + Patronage) cards in either cost order; reset at battle init.
- Attention: gain/spend/reset; `SpendAttention` opinion payout.
- Scandal counters: `ScandalsInHand`, `ScandalsDrawnThisTurn` increment/reset; `SpinScandalsEffect` clears hand Scandals and pays per cleared.
- Celebrity first-card-upgraded: fires exactly once per battle, before costs are paid.

## Suite 6 — Targeting

`EffectExecutionContext.GetTargets` for each `TargetType`, especially the crowd-shaped ones: Adjacent, AllHostile, AllReceptive, RandomReceptive — including edge cases (target at row edge, empty category).

## Sequencing

1. Suites 1–2 first — they gate the status cutover.
2. Suite 3 next — protects the FL deck-authoring work.
3. Suites 4–6 as the systems they cover get touched.

Anything requiring a full scene (UI panels, card choice flow, intents rendering) stays manual for now; don't build play-mode infrastructure until the edit-mode layer exists.
