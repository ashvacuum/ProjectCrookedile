[← `readme.md`](../../readme.md)  ·  [Current docs](../../readme.md#documentation-map)

# Deprecated Docs

*Nothing in this folder is current. Moved here 2026-07-28.*

These predate the combat + class redesign — the shift from an HP/damage prototype to the
shared **Opinion Meter** model, and from the original three "origins" to the current three
archetypes. Their specifics no longer match the code or the design.

**They are kept, not deleted, because several contain ideas that were never wrong — only
unbuilt.** See [Still worth mining](#still-worth-mining) below. If you pull an idea from
here, re-derive it against the canonical docs; do not copy numbers, names, or file paths.

Every file carries a banner naming what superseded it. Relative links *between* files in this
folder still work; links pointing out of it were rewritten to `../`, but any link inside the
bodies still points at pre-move paths — treat those as broken.

---

## What's here and what replaced it

### Original design source (pre-redesign)

The first full design pass, written around the campaign-era concept.

| File | Was |
|---|---|
| `game_overview.md` | The original pitch and loop |
| `origins.md` | Origin classes before the current three archetypes |
| `resources.md` | The pre-Opinion-Meter resource model |
| `cards.md` | Card list against the old effect vocabulary |
| `locations.md` | Town map locations |
| `events.md` | Event concepts |
| `progression.md` | Run and meta progression |
| `technical.md` | Intended technical architecture |

→ Superseded by [`core-design.md`](../core-design.md) and [`metagame-campaign.md`](../metagame-campaign.md).

### Consolidated wiki and status snapshots

| File | Was |
|---|---|
| `GAME_WIKI.md` | Everything-in-one-place wiki |
| `SYSTEMS_STUDY.md` | System-by-system study |
| `IMPLEMENTATION_STATUS.md` | Point-in-time build status |

→ Superseded by [`readme.md`](../../readme.md) (orientation) and [`roadmap.md`](../roadmap.md) (status).
Status snapshots rot fastest; trust `git log` over any of these.

### Setup and integration guides

Step-by-step scene, prefab, and script wiring for a layout that has since been restructured
(the BattleUI decomposition into self-subscribing panel islands, the `[SerializeReference]`
effect system, `GameDatabase<T>`).

`BATTLE_SYSTEM.md`, `BATTLE_SYSTEM_TASKS.md`, `BATTLE_INTEGRATION_FLOW.md`,
`BATTLE_UI_SETUP.md`, `CARD_2D_SETUP.md`, `CARD_ACQUISITION.md`, `CARD_EFFECTS.md`,
`SCENE_SETUP_GUIDE.md`, `STARTER_CARDS_GUIDE.md`

→ Superseded by [`core-design.md`](../core-design.md) and the code. For "how do I author an
effect", use the **Authoring Catalog** editor window — it's reflection-built and cannot drift.

### Completed trackers

| File | Was |
|---|---|
| `work-now.md` | Execution tasks where the decision was already made |
| `test-plan.md` | Rebuilding the test suite against the StatusBehavior API |

Both dated 2026-06-10 and scoped to branch `test-new-gameplay`, which no longer exists. The
work is done. → Live tracking is now [`roadmap.md`](../roadmap.md) and
[`campaign-build-checklist.md`](../campaign-build-checklist.md).

### Superseded checklist

| File | Was |
|---|---|
| `art-needed.md` | Thin missing-art checklist |

→ Superseded by [`art-bible.md`](../art-bible.md), which says so in its own header.

---

## Still worth mining

Concrete things in here that were never rejected on merit, only overtaken:

- **`locations.md` / `events.md`** — the town-map fiction and event premises. The *mechanics*
  are dead, but the situations are reusable content for `EventEncounterData` now that the
  choice system exists ([`campaign-encounters.md`](../campaign-encounters.md)).
- **`progression.md`** — the meta-progression shape. The campaign layer is being built now
  and its progression beyond Funds/Credibility is still unsettled.
- **`resources.md`** — the pre-Opinion-Meter economy. Worth a read before adding a third meta
  currency, if only to see which ideas were already tried and dropped.
- **`GAME_WIKI.md`** — the fiction, factions, and tone. Design churned; the satire didn't.

Everything else is superseded on substance, not just on age.
