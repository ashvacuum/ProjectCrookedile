# UI VFX & Card Juice

*Reference for the canvas-space effect systems: what exists, which tool to reach for, and
how to author each. Written 2026-07-28.*

All battle VFX live on a **Screen Space – Camera** canvas (`main.unity`, the canvas assigned
to `VFXManager._vfxCanvas`). That render mode is the single fact that decides most of what
follows — see [Why the canvas mode matters](#why-the-canvas-mode-matters).

---

## Which tool for which effect

| You want | Use | Cost |
|---|---|---|
| A one-shot impact, slash, flash, pop | **Flipbook** via `VFXManager` | One authored clip |
| A gloss sweep across a card | **`CardShine`** | One gradient sprite |
| A glow that rides a moving card | **`CardFlyAnimator._flyTrail`** | One looping clip |
| A ribbon that lags behind a card's path | **UIParticle** (installed, not yet wired) | Package + particle authoring |
| Randomised sparks scattering off a play | **UIParticle** | Package + particle authoring |

The first three need no dependencies. Reach for UIParticle only when the effect is
*continuous and path-derived* or *randomised* — a flipbook's shape is authored at art time,
so it can neither follow an arbitrary path nor vary per play.

---

## Flipbook VFX — `VFXManager`

The workhorse. A single pooled prefab (Image + Animator) whose Animator Controller holds
every clip; a `VFXEvent` asset selects which state to play.

**Authoring:** Assets → Create → Crookedile → VFX → VFX Event. Pick the animation state
(dropdown auto-generated from the controller by `VFXAnimationEnumGenerator`), set the canvas
offset, and set **Hit Time** — the normalized point in the clip where card effects land.

**Playing:**

```csharp
VFXManager.Instance.Play(evt, targetRectTransform);                    // fire and forget
var vfx = VFXManager.Instance.PlayAndSetInstance(evt, target, context); // battle-timed
```

Timing is **fully code-driven** — `VFXAnimatedImage` runs a UniTask playback driver that
fires `ApplyEffects` at the hit time and `OnAnimationComplete` at the clip's end. Do **not**
key AnimationEvents in new clips; legacy clips that still have them are tolerated because
both methods are idempotent.

**The safety net worth knowing about:** if a VFX GameObject is disabled before its animation
finishes (parent deactivated, scene change, card pooled), `OnDisable` force-completes it —
battle callbacks fire immediately so `_vfxInFlight` can never strand the battle, and the
pool-return is deferred a frame to dodge Unity's "cannot SetParent while activating parent"
error. It logs a warning when this fires. A warning per discard means something is killing
VFX early, not that the net is working as intended.

---

## Card shine — `CardShine`

A gradient Image swept across a `RectMask2D` by a DOTween anchored-position tween. No
shader, no material.

**Setup:**
1. Add an empty child to the card **sized to the card art, not the card root**. A
   `RectMask2D` on the root would also clip `CardButton`'s selection outline.
2. Add `CardShine` to that child (`RectMask2D` comes with it via `RequireComponent`).
3. Add a soft diagonal white gradient Image as its child, assign to `_shine`. Rotate it
   20–30° for the classic sweep.

`Play()` for one-shots (card drawn, upgraded, reward revealed). Set `_loopInterval > 0` for
an idle shimmer on rare cards. The shine Image's `raycastTarget` is forced off in `Awake` —
a raycast-target shine sits on top of the card and eats its clicks, and the symptom (dead
cards) looks nothing like the cause.

---

## Fly trail — `CardFlyAnimator`

`CardFlyAnimator` owns the draw pop-in, the discard fly, and the card-grant sequence. A
`_flyTrail` VFXEvent, when assigned, spawns as a **child of the flying card** so it rides the
`DOMove` with no per-frame position copying.

Started and stopped at both fly sites (`AnimateDiscardOut`, grant phase 3). The stop is
explicit — `trail?.OnAnimationComplete()` — because the trail clip's length and the tween's
duration are authored independently, and because stopping *before* the card is pooled avoids
tripping the `OnDisable` force-complete path on every discard.

**What this is and isn't:** the trail is a child of the card, so it *moves with* the card. It
reads as an attached glow or streak, not a ribbon left behind in the space the card passed
through. A lagging ribbon is path-derived and needs UIParticle (or a hand-rolled motion
streak). The trail also shrinks with the card's `DOScale(0)` on discard and grant flies,
which reads as collapsing into the pile.

> **Not built yet:** there is no "card flies forward on play" animation.
> [`HandPanel.OnCardPlayed`](../Assets/Scripts/UI/Battle/Panels/HandPanel.cs) pulls the
> played card from the hand and holds it in place while VFX resolves, then flies it to
> discard on resolve. A forward lunge would slot in at that hold.

---

## UIParticle

`com.coffee.ui-particle` ([mob-sakai/ParticleEffectForUGUI](https://github.com/mob-sakai/ParticleEffectForUGUI))
is in `Packages/manifest.json`. **Currently unpinned** — it tracks the default branch. Pin it
to the resolved tag (`...ParticleEffectForUGUI.git#4.x.y`) so a fresh clone can't pull a
different version.

**Authoring a trail:**
- `UIParticle` component on a GameObject, `ParticleSystem` as its child
- Simulation Space → **World** — this is what makes the trail lag behind the card rather than
  teleport with it
- Enable the **Trails** module, not just particle emission
- Scale up hard via `UIParticle.Scale`: particle sizes are authored in world units and need
  amplifying to read in canvas space

**Not wired yet.** `CardFlyAnimator.StartTrail` still spawns a flipbook. Swapping its body to
spawn a pooled UIParticle prefab is a contained change — both fly sites call through that one
method, so nothing else moves.

**Budget:** UIParticle rebuilds a mesh per instance per frame. Fine for a handful of
simultaneous cards; not for hundreds of emitters.

---

## Why the canvas mode matters

A `ParticleSystem` is a MeshRenderer, not a `CanvasRenderer`. Parented under a card
RectTransform on a scaled canvas, it inherits the canvas scale factor — where 1 unit is a
scaled pixel, not a meter. That breaks four things at once:

- Particle size, velocity, and gravity are authored in world units, so they render at the
  wrong scale and shift with resolution as the CanvasScaler adjusts
- World simulation space fights the card's movement; local space teleports the whole burst
  with the card
- No masking — an effect can't be clipped to a card frame by a `RectMask2D`
- Sorting is all-or-nothing per canvas; interleaving with specific UI layers needs
  sub-canvases with manual `sortingOrder`

UIParticle exists to bake the particle mesh into a CanvasRenderer, which fixes all four. That
is the whole reason the dependency is justified — not particle quality.
