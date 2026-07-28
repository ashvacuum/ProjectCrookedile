> [!WARNING]
> **Deprecated — no longer in use.** Completed. Scoped to branch `test-new-gameplay`, which no longer exists.
> Kept because the ideas may still be worth mining; the specifics are not.
> **Superseded by:** [`docs/roadmap.md`](../roadmap.md), [`docs/campaign-build-checklist.md`](../campaign-build-checklist.md)
> Index: [`docs/deprecated/README.md`](README.md)  ·  Back to [`readme.md`](../../readme.md)

---

# Work Now — actionable engineering & content tasks

*As of 2026-06-10, branch `test-new-gameplay`. These are tasks where the decision is already made and only execution remains. Design questions that still need a decision live in `needs-detailing.md`; the test rebuild has its own piece in `test-plan.md`.*

Ordered roughly by dependency, not difficulty.

---

## 1. Status cutover (enum → StatusBehavior) — DONE (2026-06-10)

Completed in three commits on `test-new-gameplay` (verified by compiling all four assemblies outside Unity):

1. Store cutover: `StatusEffect` wraps a `StatusBehavior`; manager keyed by Id; pipeline methods fold the behavior hooks.
2. Consumer migration: event payload carries the behavior (`StatusEffectAppliedEvent.Behavior`/`StatusId`); BattleManager/CrowdReactions/UI/passives/editor/tests on the behavior API; `EnemyData.StartingEffects` re-authored as `StartingStatusEntry`; icon map re-keyed by id (asset YAML migrated); legacy `ApplyStatusEffect` deleted (zero asset refs confirmed).
3. Deletion: `StatusEffectType`, `StatusBridge`, transitional wrappers all gone. `StatusRegistry` is the sole source of truth.

**Behavior change to verify in play:** Exposed now doubles INCOMING pressure on its holder (per its description), not the holder's outgoing hit — the old enum code had it on the wrong side.

**Unity follow-ups (user):** let Unity recompile and regenerate csproj; commit any new/changed `.meta`; re-run Crookedile → Generate → Seed Status Icon Map (adds Doubt/Jaded/etc. entries by id); spot-check status badges and tooltips in a battle.

## 2. Rebuild the tests

See `test-plan.md`. Do this *alongside* step 1, not after — the new tests should target the behavior API and act as the cutover gate. The old `EffectResolverTest` is written against the enum world and will need rewriting anyway.

## 3. Receptive bonus overhaul (implementation pending design)

Current implementation: `_supportPerReceptiveEnemy` (default 1) Support per receptive enemy at player turn start (`BattleManager.cs` ~line 1063). **Confirmed inadequate** — it's passive, invisible, and pulls in the same direction as the echo-chamber trap (rewards stacking receptives with the same currency the chamber then punishes). The replacement design is an open question (see `needs-detailing.md` §1); once decided, implementation is small and isolated.

## 4. Finish or park the naming refactor

Remaining from the glossary pass: `DamageDealtEvent` → `MeterPressureEvent` (many subscribers), `attacker` → `source` (~59 refs), `EffectContextValue.LastDamageDealt`/`LastHealAmount` renames (enum-ordinal-safe). The `ModifyDamage*` pipeline names get resolved by the status cutover (step 1) for free. Either do the event/field renames in one focused commit or explicitly park them in the glossary doc so the codebase isn't half-glossary indefinitely.

## 5. Content authoring (the actual bottleneck)

- **Faith Leader starter deck** — regenerate with `FaithLeaderDeckGenerator` AFTER the status cutover so cards author `ApplyStatusBehaviorEffect`. Remember: FL cards are **status appliers, not damage dealers** — the pressure comes from the Fanatic burst, so most FL cards should carry little or no direct pressure. Pacify statuses must be authored **Permanent** duration or they decay before reaching threshold.
- **Nepo Baby and Celebrity starter decks** — not yet authored; mechanics all exist.
- **Enemy roster** — prototype target is 6–8 enemies covering hostility-stance × intent-pattern combos. Currently a handful. This blocks every playtest question.
- Unflag stale `IsStarterCard` cards (e.g. old FL Blessing) so the tag-driven starter collection doesn't pick up junk.

## 6. Wire or kill the unconsumed databases

Additive DBs built but not consumed — each is either a small wiring task or a deletion:
- `OriginDatabase` → replace `OriginStats` + `BattleManager._originPassives` array.
- `RewardConfig` → `CardDatabase.GenerateRewardOffer` still uses hardcoded 70/25/5 weights.
- Relic runtime layer (acquisition, RunState, registering relic passives) — explicitly deferred; fine to leave, but don't build more relic data until it exists.

## 7. Hygiene

- Commit any pending `.meta` files for new scripts.
- BattleManager (~1,375 lines) is re-accreting archetype state (Patronage, Attention, conversion counter, first-card flag). Before a fourth resource lands, extract a per-origin resource collaborator following the `CrowdReactions` extraction pattern.
