# Battle UI mock prompt

> **Kind:** Reference · **Status:** Draft · **Updated:** 2026-10-09
>
> **Summary:** Image-edit prompt for the battle screen UI, built on the committee-hearing battle mock rather than a new layout.
>
> **Source of truth:** this doc · **Related:** [`style-mock-prompt.md`](style-mock-prompt.md) · [`art-bible.md`](../art-bible.md)

Attach `docs/design/battle-ui-reference.png` as the image reference and use the prompt
below as an edit of it. The scene, cast and framing stay; only the interface is tightened. Drop results
beside this file.

---

Edit of the attached Project Crookedile battle mock. Keep the scene exactly: a warm, muted committee
hearing hall in a Filipino legislature, a composed crocodile politician (buwaya) in a plain barong with
his back to camera at a wooden podium with a microphone and a glass of water, and a single row of eight
anthropomorphic animal opponents seated behind a raised wooden panel, campaign tarpaulins on the back
wall. Same cast, same flat muted illustration, same dusty mustard, brick, teal, olive and cream palette,
same even soft lighting. Do not repaint the characters or the room.

Tighten the interface so it reads at a glance and every element earns its space:

- Top: one Opinion bar across the width. Gold fill from the left for the player, dark from the right,
  chevrons at the meeting point showing the last push. A round gold Support badge with its number at
  the left end, a round brick Denial badge with its number at the right end. Above the bar, small text
  "OPINION 62", "win at 100, lose at 0, hold 50 at Judgment". Top right: "TURN 4 · JUDGMENT IN 8" and
  a small LOG button. Top left: the carried items as small round chips.
- Each opponent: a name plate on the panel edge, one stance tag under it (HOSTILE brick, NEUTRAL olive,
  RECEPTIVE teal), a thin hostility track under the tag with a marker, and at most two small status
  chips with numbers. Above each head, one intent tag: an icon and a single word or number (an arrow
  and -9, Ward, Convert, Waits, +3, Riles). No other text on the opponents.
- The player: directly above the hand, a small dark tray with the class resource chips (Congregation 2,
  Tithe Drive 1). Bottom left: three round counters, ENERGY 3/3, DRAW 12, DISCARD 6.
- The hand: five cards fanned along the bottom edge in the game's frame style, colour-coded by type
  (red Rhetoric, green Pressure, blue Policy), a cost circle top left, the title in caps, an icon-style
  illustration (one object, bold silhouette, flat colour), one or two short lines of rules text, and a
  target label on the bottom strip (TARGET, ALL ALLIES).
- Bottom right: a large mustard END TURN button, with three small diamond pips above it.

Keep the chrome thin and quiet: dark walnut panels with a fine gold edge, one condensed sans-serif for
labels, no parchment notes, no tutorial text, no logo, no flavour quotes on cards. The room is the
picture; the UI frames it without covering faces.

NOT oil painting, NOT grimdark, NOT bright slapstick, NOT photoreal, NOT a new layout.
