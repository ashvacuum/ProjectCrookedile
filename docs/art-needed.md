# Art Still Needed

Have: card art, card frames/backs. Missing: **status icons, enemy intent icons, enemy portraits**.

Sizes are recommended authoring targets — transparent PNG, power-of-two, square. Author at the size shown; the prefab RectTransform scales down.

| Group | Slot (data) | Count | Size (px) |
|---|---|---|---|
| Status icons | `StatusEffectIconMapSO` | 29 | 128×128 |
| Enemy intent icons | `EnemyIntentTheme` | 10 | 128×128 |
| Enemy portraits | `EnemyData._portrait` | 11 | 512×512 |

---

## Status icons (29) — flat/single-color, readable at ~32px badge

Source of truth: `StatusRegistry`. Group hints for the artist:

**Player debuffs (enemy inflicts):** Weakened, Vulnerable, Frail, Entangled, Exposed, Confused, Silenced, Stunned, Rattled, Smear
**Player buffs:** Strength, Dexterity, Focus, Energized, Plated, Regeneration, Intangible, Thorns, Ritual, Momentum, Echo
**Faith Leader pacify:** Guilt, Shame, Doubt, Jaded
**Hostility flags (on enemies):** Hardened, Fanatic, Devotion, Turncoat

> Doubt, Jaded, Smear are confirmed-missing. Run Crookedile → Generate → Seed Status Icon Map, then the Content Hub → Statuses tab shows which of the 29 are still blank.

## Enemy intent icons (10) — author neutral white, theme recolors

One per `EnemyMoveType`:

| Icon | Means |
|---|---|
| Attack | Pressure/debuff to the player |
| Defend | Gains shield / self-heal |
| Buff | Self-buff only |
| Debuff | Debuffs player, no direct damage |
| OffensiveBuff | Attacks AND self-buffs |
| DebuffAttack | Debuffs AND deals damage |
| SummonMinion | Spawns a new enemy |
| Idle | Does nothing this turn |
| DefendOpinion | Gains Denial (shields the meter) |
| RileOthers | Raises other enemies' hostility |

## Enemy portraits (11) — square bust, Filipino-political satire

Prototype roster + elites (from `docs/enemy-design`):

| Enemy | Archetype | Stance |
|---|---|---|
| Heckler | loud opposition | Hostile |
| Diehard | fanatic supporter | Receptive |
| Tita | gossip / swing | Neutral |
| Troll | chaff in packs | Hostile |
| Tabloid Reporter | scandal-monger | Hostile |
| Disillusioned Voter | cynic | Neutral (pre-Jaded) |
| Rival's Plant | fake-friendly | Fake-receptive |
| Fixer | protector | Hostile |
| Padrino | immovable wall | Hostile (Hardened) |
| The Televangelist (elite) | converts the room | — |
| The Dynast (elite) | summons bodies | — |

---

Content Hub (Statuses / Intents / Enemies tabs) is the live blank-slot checker; this doc is the spec.

<!-- ponytail: enumerated from StatusRegistry + EnemyMoveType + the design doc. Counts are real, not estimates. Enemy list is the prototype scope; bosses/full roster come later. -->
