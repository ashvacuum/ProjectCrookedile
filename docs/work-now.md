# Work Now — actionable engineering & content tasks

*As of 2026-06-10, branch `test-new-gameplay`. These are tasks where the decision is already made and only execution remains. Design questions that still need a decision live in `needs-detailing.md`; the test rebuild has its own piece in `test-plan.md`.*

Ordered roughly by dependency, not difficulty.

---

## 1. Finish the status cutover (enum → StatusBehavior)

The behavior system (`Status/StatusBehavior.cs`, `StatusRegistry`, all 29 behaviors) is the keeper. The enum path is ready to fall. Remaining, in order:

1. **Re-key the status DB.** `StatusEffectIconMapSO` is the last structural hook on the enum: entries, lookup dictionary, and `TryGet` are all `StatusEffectType`-keyed, and `StatusEffectIconUI`/`StatusEffectPanelUI` consume it that way. Re-key entries by string `Id` (behavior Ids == lowercase enum names, so a one-time migration of the asset is mechanical — `StatusIconMapSeeder` can be adapted to do it). Keep icon/color/name/description fields as-is.
2. **Migrate consumers off the enum** file by file using the behavior API already on `StatusEffectManager` (`GetStacks<T>()`, `HasStatus(behavior)`, `ApplyStatus(behavior, ...)`). Biggest holders: `StatusEffectManager` (~54 refs), `StatusEffect` (~34), `BattleManager` (~27). The damage-pipeline switches (ModifyOutgoing/Incoming/SupportGained/DenialGained/CardCost) are the one real cutover — replace switch bodies with "iterate behaviors via hooks" in a single parity-checked commit.
3. **Re-author the 5 legacy assets** still on enum `ApplyStatusEffect` if any remain (Content Hub → "Status migration" tab is the live checklist; last grep of `.asset` files found zero — verify the tab agrees).
4. **Delete the legacy layer:** `ApplyStatusEffect` (already `[Obsolete(error)]`), the `StatusEffectType` enum, `StatusBridge`, and the enum payload on `StatusEffectAppliedEvent` (→ string Id). Gate on the Content Hub "Status parity" tab being green AND the new tests (see `test-plan.md`) passing.

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
