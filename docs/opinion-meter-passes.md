# Opinion-Meter Redesign — Implementation Spec (Passes 2–4)

**Audience:** an implementing agent (Sonnet) working cold. Everything needed to execute is in this doc — you should not need to re-derive design context.

**Scope of this doc:** the three remaining *buildable* passes of the opinion-meter combat redesign. A final section lists design-pending work that must NOT be built yet.

---

## 0. Context — what already exists (Pass 1, done)

Combat was converted from an HP model to a shared **Opinion Meter** model. Key invariants you must respect:

- **No resolve / HP anywhere.** `BattleStats` has no resolve fields. `BattleStats.IsDefeated` is hard-wired `false`. Do not reintroduce resolve.
- **Opinion Meter** (0–100, shared) is the only win/loss axis. Win at 100, loss at 0, or Judgment at the turn limit (`opinion >= maxOpinion/2` = win). Lives in `BattleManager` (`CurrentOpinion`, `MaxOpinion`, `OpinionPercentage`, `RaiseOpinion`, `LowerOpinion`).
- **Composure shields the opinion meter, both sides.** Player composure absorbs opinion *drops* (enemy attacks); enemy composure absorbs opinion *rises* (player cards). The absorption happens in `BattleStats.AbsorbThroughComposure(int) -> int remainder`, called from the pressure pipeline in `BattleEffect.ApplyPressure` and `EffectResolver.ApplyDamagePipeline`. Only the post-composure remainder is published as `DamageDealtEvent`, which `BattleManager.OnDamageDealtForOpinion` routes to `RaiseOpinion`/`LowerOpinion`.
- **Hostility** is a per-enemy signed int. `>0` hostile (damage multiplier up), `<0` receptive, `0` neutral. Clamped per-enemy via `EnemyData.MinHostility`/`MaxHostility` (defaults ±10). `BattleStats.IsHostile`/`IsReceptive`/`HostilityDamageMultiplier`.
- **Battle FSM**: `BattleManager` owns a `StateMachine<BattleState>` and publishes `BattleStateChangedEvent { Previous, Current }` on every transition. `BattleUI` drives all structural UI from a single `OnBattleStateChanged` → `ConfigureForBattleState(BattleState)`. Do not add a parallel UI FSM.

## Conventions you MUST follow

1. **Effect authoring**: new gameplay effects are `[Serializable]` classes inheriting `BattleEffect` (`Assets/Scripts/Gameplay/Battle/Effects/`). Implement `Execute(EffectExecutionContext ctx, int? amountOverride = null)` and `GetDescription()`. Override `Target` only if the effect has an explicit target field. No switch statements, no registry edits — adding a file is enough.
2. **`[SerializeReference]` + `[MovedFrom]`**: every concrete effect/passive type carries `[UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]` directly above the class. New effect classes MUST include this attribute too (the project's assemblies were moved out of `Assembly-CSharp`; without it, serialized references to the new type can break if the asset is ever saved from a context that records the assembly).
3. **Enum serialization**: `EnemyMoveType` has explicit integer values "to preserve existing .asset serialization." When adding values, APPEND new explicit integers (7, 8, …). Never reorder or renumber existing entries.
4. **Formatting**: code is CSharpier-formatted and uses `#region` blocks (not `// ─── ` dividers). A pre-commit hook runs `csharpier format` on staged `.cs` files, so don't hand-format — just write clean code.
5. **Tweens**: UI animation uses DOTween (`using DG.Tweening;`). Match the existing pattern: `DOTween.Kill(target)` then a `.To(...).SetLink(gameObject)` tween. See `PlayerSlotUI.Refresh` / `EnemySlotUI.ShowNameLabel` for the idiom.
6. **Code vs Unity-editor work**: you (the agent) write only `.cs` files. Anything that adds a `[SerializeField]` reference, edits a prefab, or creates/edits a `.asset` ScriptableObject must be done by a human in the Unity Inspector. Each pass below separates **Code steps (you)** from **Unity steps (human, list clearly)**.

---

## Pass 2 — Enemy hostility split-bar on `EnemySlotUI`

**Goal:** replace the coloured hostility *text* with a centre-split bar. Fills left (green) when receptive, right (red) when hostile, empty at neutral. Single value, no ambiguity.

**File:** `Assets/Scripts/UI/Battle/EnemySlotUI.cs`

### Current state
Hostility is shown via `hostilityText` (a `TMP_Text`) — `"Hostile"`/`"Receptive"`/`"Guarded"` or, for the Actor origin, the exact signed number. `composureText`/`composureObject` show enemy composure (keep these). There is dead resolve UI: `resolveText`, `resolveBarFill`, `barLerpSpeed`, and a no-op `Update()` left from the old HP bar.

### Code steps (you)

1. **Add serialized fields** for the two fill images (keep `hostilityText` — repurpose it as an optional exact-value label, or leave it for the Actor origin):
   ```csharp
   [Header("Hostility Bar")]
   [Tooltip("Right-half fill (grows rightward as hostility goes positive). Tint red/orange.")]
   [SerializeField] private Image _hostileFill;

   [Tooltip("Left-half fill (grows leftward as hostility goes negative). Tint green.")]
   [SerializeField] private Image _receptiveFill;

   [Tooltip("Seconds for the hostility bar fills to tween to their new amount.")]
   [SerializeField] private float _hostilityBarDuration = 0.25f;
   ```

2. **Drive the bar in `Refresh()`** (replace the hostility-text block, or run alongside it). Read the per-enemy clamps from `enemy.EnemyData` (it exposes `MinHostility`/`MaxHostility`):
   ```csharp
   int   h    = enemy.Stats.CurrentHostility;
   float posT = enemy.EnemyData.MaxHostility > 0
       ? Mathf.Clamp01((float)Mathf.Max(0, h) / enemy.EnemyData.MaxHostility) : 0f;
   float negT = enemy.EnemyData.MinHostility < 0
       ? Mathf.Clamp01((float)Mathf.Max(0, -h) / -enemy.EnemyData.MinHostility) : 0f;

   TweenFill(_hostileFill,   posT);
   TweenFill(_receptiveFill, negT);
   ```
   Add the helper:
   ```csharp
   private void TweenFill(Image img, float target)
   {
       if (img == null) return;
       DOTween.Kill(img);
       DOTween.To(() => img.fillAmount, x => img.fillAmount = x, target, _hostilityBarDuration)
              .SetEase(Ease.OutQuad)
              .SetLink(gameObject);
   }
   ```
   Keep `PulseHostility()` working: have it punch whichever bar is active (or the bar container). Simplest: punch `_hostileFill.transform.parent` (the bar root). If you keep `hostilityText` for the Actor exact readout, leave its update logic intact.

3. **Dead-code cleanup** (do this in the same pass):
   - Remove `resolveText`, `resolveBarFill`, `barLerpSpeed` fields and any references.
   - Remove the no-op `Update()` method and its `#region HP Bar` wrapper.
   - In `MarkDefeated()`, remove the `resolveText`/`resolveBarFill` references. **Leave the rest of `MarkDefeated`/`defeatedOverlay` as-is** — enemies can't currently be defeated, but the method is harmless and removing the defeat plumbing touches `BattleUI` event wiring; that's out of scope here. (Note it as future cleanup, don't action it.)
   - Fix the class summary comment ("Displays the enemy's name, Resolve, hostility…") to drop "Resolve".

### Unity steps (human)
- On the enemy-slot prefab: build the bar as three sibling `Image`s in a `HorizontalLayoutGroup` (or a simpler two-`Image` overlay if you prefer), per the centre-split design. Set both fills to **Filled / Horizontal**; the receptive fill should fill from the **right origin** (so it grows leftward from centre), the hostile fill from the **left origin** (grows rightward from centre). Tint receptive green, hostile red/orange.
- Wire `_hostileFill` and `_receptiveFill` to the new fields. Remove the old resolve bar/text objects from the prefab.

### Verification
- Start a battle, play single-target cards that raise an enemy's hostility (or use a Rile move once Pass 4 lands) and confirm the red half fills smoothly. Use a card/move that reduces hostility below 0 and confirm the green half fills. Neutral = both empty.

---

## Pass 3 — Three-piece Opinion bar on `OpinionMeterUI`

**Goal:** show the opinion meter flanked by two composure "shields": `[player-composure shield | opinion fill | enemy-composure shield]`. Player composure (left) represents resistance to drops; enemy composure (right) represents resistance to rises.

**Files:** `Assets/Scripts/UI/Battle/OpinionMeterUI.cs`, `Assets/Scripts/UI/Battle/BattleUI.cs`

### Design decision (locked for this spec)
Enemy composure is **per-enemy**, but the opinion bar is **shared/global**, so "which enemy's shield" is ambiguous with multiple enemies. **Decision: the right shield shows the FOCUSED enemy's composure** (`BattleManager.OpponentStats?.CurrentComposure`, which is `FocusedEnemy?.Stats`). Rationale: single-target player cards route opinion-rise through the focused enemy's composure, so that's the shield the player is actually pushing against. It updates when the player re-focuses. Per-enemy composure also remains visible on each enemy slot (Pass 2 keeps `composureText`). *If the user later wants summed/averaged composure instead, only `RefreshOpinionMeter` changes.*

### Code steps (you)

1. **`OpinionMeterUI`** — extend `Refresh` to accept the two composure values and add two shield Images:
   ```csharp
   [Header("Composure Shields")]
   [Tooltip("Left shield image — player composure (resists opinion drops). Width scales with value.")]
   [SerializeField] private RectTransform _playerShield;
   [Tooltip("Right shield image — focused enemy composure (resists opinion rises). Width scales with value.")]
   [SerializeField] private RectTransform _enemyShield;
   [Tooltip("Composure value that maps to the maximum shield width.")]
   [SerializeField] private int _shieldFullValue = 20;
   [Tooltip("Max shield width in px at _shieldFullValue composure.")]
   [SerializeField] private float _shieldMaxWidth = 60f;
   ```
   Change the signature to:
   ```csharp
   public void Refresh(int currentOpinion, int maxOpinion, int turnsElapsed, int maxTurns,
                       int playerComposure, int enemyComposure)
   ```
   Keep all existing opinion-fill / threshold / countdown logic. Add shield sizing at the end:
   ```csharp
   SizeShield(_playerShield, playerComposure);
   SizeShield(_enemyShield, enemyComposure);
   ```
   ```csharp
   private void SizeShield(RectTransform shield, int composure)
   {
       if (shield == null) return;
       float w = _shieldFullValue > 0
           ? Mathf.Clamp01((float)composure / _shieldFullValue) * _shieldMaxWidth : 0f;
       var size = shield.sizeDelta;
       shield.sizeDelta = new Vector2(w, size.y);
       shield.gameObject.SetActive(composure > 0);
   }
   ```
   (If you instead model the shields as `Image.fillAmount` per the user's HorizontalLayoutGroup sketch, use `LayoutElement.preferredWidth` and tween that — either is acceptable; the value→width mapping above is the contract.)

2. **`BattleUI.RefreshOpinionMeter`** — pass the composure values:
   ```csharp
   _opinionMeterUI.Refresh(
       battleManager.CurrentOpinion,
       battleManager.MaxOpinion,
       battleManager.PlayerTurnsElapsed,
       battleManager.MaxTurns,
       battleManager.PlayerStats?.CurrentComposure ?? 0,
       battleManager.OpponentStats?.CurrentComposure ?? 0);
   ```

3. **Refresh the bar on composure changes.** `BattleUI.OnComposureChanged` currently only calls `UpdateStatsDisplay()`. Add `RefreshOpinionMeter();` to it so the shields update live. Also call `RefreshOpinionMeter()` from wherever focus changes so the right shield follows the focused enemy — search for `SetFocusedEnemy`/focus-change handling in `BattleUI`; if there is a focus-changed UI handler, add the refresh there. If focus changes don't currently raise a UI event, add `RefreshOpinionMeter()` to the existing focus-update path in `BattleUI`.

### Unity steps (human)
- On the OpinionMeter prefab: add the two shield Images flanking the opinion fill (left = player, right = enemy). Wire `_playerShield`/`_enemyShield`. Tune `_shieldFullValue`/`_shieldMaxWidth`.

### Verification
- Gain player composure (a Defend-type player card) → left shield grows. Play an opinion-raising card into a focused enemy that has composure → right shield shrinks as its composure is consumed. Switch focus between two enemies with different composure → right shield jumps to the newly focused enemy's value.

---

## Pass 4 — New enemy move types (Idle, DefendOpinion, RileOthers)

**Goal:** give enemies non-damage behaviours that fit the rally model. Three are buildable now; a fourth (EncourageSides) is design-pending — see the final section, do NOT build it.

Enemy moves are data (`EnemyMoveData` ScriptableObjects) with a polymorphic `BattleEffect` list and an `EnemyMoveType` for the intent badge. Most of this pass is small code additions + human-authored assets.

### 4a. `EnemyMoveType` enum additions
**File:** `Assets/Scripts/Data/Enemy/EnemyMoveData.cs`

Append (do not renumber existing):
```csharp
Idle = 7,         // Does nothing this turn (passive / waiting)
DefendOpinion = 8,// Gains composure to shield the opinion meter from the player's rises
RileOthers = 9,   // Raises the OTHER enemies' hostility
```
Update the enum's doc comment count ("Seven broad categories" → current count).

### 4b. New effect: `RaiseAlliesHostilityEffect`
**New file:** `Assets/Scripts/Gameplay/Battle/Effects/Resource/RaiseAlliesHostilityEffect.cs`

`TargetType.AllAllies` for an enemy caster resolves to *all living enemies including self* (see `EffectExecutionContext.GetTargets`). For "rile *others*", exclude the caster. Model it on `RaiseAllOpponentsHostilityEffect`:
```csharp
using System;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Raises the Hostility of every OTHER living enemy (excludes the caster).
    /// For enemy "rile the room" moves. No-op when the source is a player card.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class RaiseAlliesHostilityEffect : BattleEffect
    {
        [MinValue(1)]
        [SerializeField]
        private int _amount = 2;

        [Tooltip("If true, the casting enemy also rouses itself.")]
        [SerializeField]
        private bool _includeSelf = false;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount = amountOverride ?? _amount;
            int count = 0;
            foreach (var (stats, _) in ctx.GetTargets(TargetType.AllAllies))
            {
                if (!_includeSelf && stats == ctx.Caster) continue;
                stats.GainHostility(amount);
                count++;
            }
            GameLogger.LogInfo<RaiseAlliesHostilityEffect>(
                $"Riled {count} other enemy(ies) by {amount}");
        }

        public override string GetDescription() => $"Rile other enemies' Hostility by {_amount}";
    }
}
```
**Bonus-draw interaction (no extra wiring needed):** the player's hostile-enemy bonus draw counts `IsHostile || BecameHostileThisTurn` at player-turn start, then re-snapshots. RileOthers runs on the *opponent* turn, so by the next player-turn start the riled enemies read as `IsHostile` and are counted normally. Do not add `CheckBecameHostile` plumbing to the enemy path.

### 4c. Idle behaviour
- `EnemyMoveType.Idle` is just an intent category; an Idle move is an `EnemyMoveData` with an **empty** `_newEffects` list. The effect-resolution loop handles zero effects fine (the enemy simply does nothing).
- `EnemyController.SelectNextMove` already biases receptive enemies toward non-offensive moves; `Idle` is non-offensive, so a receptive enemy with an Idle move in its pool will tend to pick it. No code change required there. (The separate 20%-per-stack receptive *skip* in `OpponentTurnState` is independent and still applies.)

### 4d. DefendOpinion
- No new effect needed: a DefendOpinion move is `EnemyMoveType.DefendOpinion` + a `GainComposureEffect` (enemy composure already shields opinion rises). The new enum value just gives it a distinct intent badge.

### 4e. Intent theme
**File:** `Assets/Scripts/Data/Enemy/EnemyIntentTheme.cs` needs no code change — it already falls back to `(null icon, white badge)` for unmapped types. But the **human** should add `EnemyIntentEntry` rows for `Idle`, `DefendOpinion`, `RileOthers` to the theme `.asset` (icon + colour) so the new intents read clearly.

### Unity steps (human)
- Add intent-theme entries (4e).
- Author enemy-move `.asset`s: an Idle move (empty effects, `Idle` type, intent text e.g. "Waiting…"); a DefendOpinion move (`GainComposureEffect`, `DefendOpinion` type); a RileOthers move (`RaiseAlliesHostilityEffect`, `RileOthers` type). Add them to enemies' `Moves` lists.

### Verification
- Multi-enemy fight: confirm a RileOthers move raises the *other* enemies' red bars but not the caster's (unless `_includeSelf`). Confirm a DefendOpinion move grows that enemy's composure and visibly blunts the next opinion-raise the player aims at it. Confirm an Idle enemy declares the Idle intent and does nothing on its turn. Confirm intent badges show the right icon/colour.

---

## Design-pending — DO NOT BUILD YET

These were discussed but are not specced because the rules aren't defined. Do not implement; if you think you need them, stop and ask.

1. **EncourageSides** ("encourage others to switch sides") — the mechanical meaning is undefined (defect an ally toward the player? convert hostility? pull opinion?). No enum value, no effect, no asset until the rule is decided.
2. **Receptive "bad practices"** — what a receptive enemy does that *negatively* affects the player is still being designed. The receptive *skip* and non-offensive *preference* already exist; do not invent new receptive penalties.
3. **Enemy leaves battle** — explicitly out of scope. `BattleStats.IsDefeated` stays `false`; do not add a removal/defeat mechanic.
4. **Meta popularity meter / campaign** — deferred. `RunState.RecordBattleVictory()` is an intentional stub; leave it. `BattleSession` already has the per-round scaffolding to build on later.

---

## File reference index

| Concern | File |
|---|---|
| Opinion meter state + win/loss | `Assets/Scripts/Gameplay/Battle/BattleManager.cs` |
| Composure absorb | `Assets/Scripts/Gameplay/BattleStats.cs` (`AbsorbThroughComposure`) |
| Pressure pipeline (new effects) | `Assets/Scripts/Gameplay/Battle/Effects/BattleEffect.cs` (`ApplyPressure`) |
| Pressure pipeline (legacy) | `Assets/Scripts/Gameplay/Battle/EffectResolver.cs` (`ApplyDamagePipeline`) |
| Effect target resolution | `Assets/Scripts/Gameplay/Battle/Effects/EffectExecutionContext.cs` (`GetTargets`) |
| Example effect to copy | `Assets/Scripts/Gameplay/Battle/Effects/Resource/RaiseAllOpponentsHostilityEffect.cs` |
| Enemy move data + move-type enum | `Assets/Scripts/Data/Enemy/EnemyMoveData.cs` |
| Enemy move selection / receptive bias | `Assets/Scripts/Gameplay/Battle/EnemyController.cs` (`SelectNextMove`) |
| Intent visuals | `Assets/Scripts/Data/Enemy/EnemyIntentTheme.cs` |
| Enemy slot UI | `Assets/Scripts/UI/Battle/EnemySlotUI.cs` |
| Opinion meter UI | `Assets/Scripts/UI/Battle/OpinionMeterUI.cs` |
| Battle UI coordinator | `Assets/Scripts/UI/Battle/BattleUI.cs` (`RefreshOpinionMeter`, `OnComposureChanged`, `ConfigureForBattleState`) |
| Hostility clamps | `Assets/Scripts/Data/Enemy/EnemyData.cs` (`MinHostility`/`MaxHostility`) |
