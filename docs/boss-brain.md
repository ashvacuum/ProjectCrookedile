# Boss Brain — Authoring and Runtime

> **Kind:** Systems · **Status:** Built · **Updated:** 2026-10-04
>
> **Summary:** Author Behaviour Designer boss plans, tune ordered move bundles, and understand commitment, cooldowns, targeting and fallback behavior.
>
> **Source of truth:** [`BossBrain.cs`](../Assets/Scripts/Gameplay/Battle/Boss/BossBrain.cs), [`BossController.cs`](../Assets/Scripts/Gameplay/Battle/Boss/BossController.cs), [`BossData.cs`](../Assets/Scripts/Data/Boss/BossData.cs) · **Related:** [`core-design.md`](core-design.md), [`enemy-design-bible.md`](enemy-design-bible.md), [`campaign-encounters.md`](campaign-encounters.md)

## What the brain decides

The boss brain chooses **one named bundle of two or three moves at the start of each player turn**. It reads the public board, commits the plan, then stops before the player can act. The opponent phase executes that plan in order. The tree does not execute combat effects itself or reconsider the plan during card play.

The rival sits outside the audience row. It has no HP, cannot convert, and cannot be targeted by player cards. It never contributes to Echo Chamber, audience counts, hostile bonus draws, Fanatics or adjacency. Its moves influence the same audience and Opinion Meter as the player.

## Author a boss encounter

1. Open **Crookedile → Database → Encounters** and select a battle encounter.
2. Expand its Battle Session and the desired round. Assign a Boss asset to the round's **Boss** field. The round now reads **Boss Debate**; leaving the field empty produces **Standard Fight**.
3. Set the encounter's campaign **Is Boss** flag to match. The flag controls campaign classification; the round's boss assignment supplies the combat rival. Database audits report disagreement.
4. Tune audience composition, starting/max Opinion and turn limit in the round. Keep a populated audience so crowd-based cards have useful targets.
5. Expand the Boss asset. Set its name, optional portrait, planning subtree, move bundles and fallback bundle index. Edit each bundle's move assets inline.
6. Open the subtree using **Open graph** and build its selection logic using the tasks below.
7. Check **Database → Checks**, then use the Encounters preview's **Play selected encounter with Scored bot** button with a chosen origin and seed.

Boss assets, subtrees and move assets are shared references. Editing them changes every encounter that uses them. For independent tuning, use **Database → Bosses → Duplicate boss, tree and moves for a local variant**, then assign that new Boss asset to the round. Duplicating only the encounter or session keeps its referenced boss shared.

## Build the planning tree

Use a **Start** event connected to a **Selector**. Put higher-priority plans first. For each conditional plan, use a **Sequence** containing one or more checks followed by **Choose Boss Bundle**. End the selector with an unconditional choice for the fallback bundle.

The custom tasks appear under **Crookedile/Boss** in Behaviour Designer.

### Check Boss Board

[`CheckBossBoard`](../Assets/Scripts/Gameplay/Battle/Boss/CheckBossBoard.cs) is a conditional task. Set **Metric**, **Threshold** and **At Least**.

- **Opinion Percent:** meter fill from 0 to 100; use `75` for 75%, not `0.75`.
- **Receptive Audience:** number of living receptive audience members.
- **Hostile Audience:** number of living hostile audience members.
- **Player Turn:** player's turn number, starting at 1. It does not count the opponent phase as an additional turn.
- **Support:** current session-level player shield.
- **Denial:** current session-level enemy shield.

With **At Least** enabled, the check passes at or above the threshold. With it disabled, the check passes strictly below the threshold. Neutral audience members count toward neither stance metric; the rival counts toward neither.

### Choose Boss Bundle

[`ChooseBossBundle`](../Assets/Scripts/Gameplay/Battle/Boss/ChooseBossBundle.cs) is an action task. Enter the exact bundle name from the assigned Boss asset, including capitalization and spaces. Renaming a bundle requires updating its tree tasks.

The task succeeds when the bundle exists, contains two or three non-null moves, and is off cooldown. It fails otherwise, letting a selector try its next branch. Once one bundle has been chosen, further choices fail for that planning cycle. Place the choice last in its sequence: selection commits immediately, so a later failing task does not undo it.

Keep planning trees short. Use board checks and bundle choices; waits, animation tasks and tasks that execute effects do not belong in this tree. It runs manually during declaration, with a ceiling of 64 planning ticks and an update-frame yield between unsuccessful ticks.

## Prototype tree and tuning

The supplied [Rival Planning subtree](../Assets/Data/Bosses/Debate%20Prototype/Rival%20Planning.asset) uses these branches in priority order:

1. If Opinion is at least 75%, choose **Protect the lead**: Stonewall → Protect the Spokesperson → Closing Argument.
2. Otherwise, if at least two audience members are receptive, choose **Win them back**: Take Back the Microphone → Protect the Spokesperson → Closing Argument.
3. Otherwise, choose **Opening statements**: Rally the Base → Closing Argument.

A higher-priority branch on cooldown also falls through to the next branch. For example, Protect the lead can be selected on turn 1, then Win them back can be selected on turn 2 if the defensive bundle is unavailable and the audience condition passes.

Tune **when** a plan appears through tree thresholds and branch priority. Tune **how often** it appears through bundle cooldown. Tune **what it does** through move effects and their order. Tune the overall fight through audience composition, Opinion settings and the turn limit. The prototype is a starting point for playtesting, not a balanced difficulty target.

## Cooldowns and fallback

Cooldowns count player turns and start when the bundle is **selected**, even if the battle ends before its moves execute.

- **0:** available on consecutive player turns.
- **1:** selecting on turn 1 blocks turn 2; it becomes available again on turn 3.
- **2:** selecting on turn 1 blocks turns 2 and 3; it becomes available again on turn 4.

**Fallback Bundle Index** is a zero-based index into the Boss asset's bundle list. Reordering that list can change which bundle is the fallback. Its bundle must contain two or three valid moves and have cooldown 0.

If the subtree is absent or fails to select a plan within the tick budget, the controller attempts this fallback. A missing subtree is still an authoring error reported by Database audits. If neither the tree nor fallback produces a valid plan, the brain logs `no valid boss plan or fallback`; the runtime does not invent replacement moves. A successful fallback alone does not prove the tree is configured correctly.

## Revealed targets and move execution

After selection, the controller snapshots the ordered list of move references. It then locks **Random Receptive** and **Random Hostile** targets declared by the moves' direct effects, before publishing the reveal event.

The selected audience member stays the target even if the player changes its stance. If no eligible member existed at reveal, that target remains absent; it does not switch to someone who becomes eligible later. Within one move, effects using the same target category share its locked target. Separate moves choose independently. Group effects use the live audience at execution time.

Target locking currently scans direct move effects; it does not recursively inspect nested effect wrappers. The ordered move list is a snapshot of asset references, not a deep copy of their values. Editing an asset during play can therefore change its pending effects.

Direct effects can veto candidates through `BattleEffect.CanSelectBossTarget`. Every effect sharing a locked target category must accept the candidate. Hostility-raising effects exclude Fanatics and members already at maximum Hostility; calming excludes Hardened members and those at the applicable floor (including Neutral when configured). Fixed signed hostility shifts use the same directional rules. Context-sourced signed shifts currently retain stance-only selection because their direction is calculated at execution. This filter applies to boss single-target selection, not player targeting or group effects. Ward remains valid counterplay rather than a reason to avoid a target. Applying Fanatic after reveal blocks the pending rile through its normal execution guard, without changing the revealed target. If every candidate is immune, the intent reveals no eligible audience; this does not automatically choose another bundle, and raw stance-count tree conditions still count immune members.

Boss moves reuse `EnemyMoveData` and the existing effect resolver. Author ordinary audience conditions in the planning tree: the move asset's **Condition** must be **None**, because boss execution does not evaluate those conditions. A **Counter** move still checks whether its configured card type was played that turn. A summon move can add audience members, who begin acting on the following turn.

The rival has no row position, so **Adjacent** and **Adjacent Allies** effects are invalid for its moves. `DelayedEffect` is a player-turn wrapper and is unsupported for boss moves; schedule a later threat through future bundle selection instead. Database audits check direct effects for these limitations, empty effects, invalid bundles, duplicate names and an invalid fallback.

Opponent resolution is: **boss moves in displayed order → audience modifiers → audience direct moves**. Full Opinion or zero Opinion ends the battle immediately, so remaining moves stop. With a turn limit, boss-debate Judgment follows the final opponent response; standard fights retain their existing Judgment timing.

## Runtime ownership and debugging

[`BattleManager`](../Assets/Scripts/Gameplay/Battle/BattleManager.cs) creates a separate `BossController` and a child `BossBrain` for a boss setup. `BossBrain` adds a Behaviour Designer wrapper tree with **Start When Enabled off**, **Manual** updates and **Entire Tree** evaluation, using the assigned subtree.

At player-turn start, [`TurnStartState`](../Assets/Scripts/Gameplay/Battle/States/TurnStartState.cs) declares audience intents and awaits boss planning before enabling player input. The brain begins a fresh planning cycle, ticks the tree, stops it, applies fallback if needed, locks targets, and publishes `BossIntentsDeclaredEvent`.

[`OpponentTurnState`](../Assets/Scripts/Gameplay/Battle/States/OpponentTurnState.cs) consumes each intent once and publishes `BossActingEvent` before executing it. [`BossPanel`](../Assets/Scripts/UI/Battle/Panels/BossPanel.cs) shows the ordered bundle and selected audience names; the battle log records each boss move. The brain is stopped when the battle ends, restarts or is destroyed. Cooldown history belongs to the controller and resets for a new battle.

When a boss keeps showing the fallback, check the exact bundle names, board thresholds, branch order, cooldowns and assigned subtree. When a target reads **no eligible audience**, inspect the stance at reveal time. When a fight appears standard, check the session round's Boss assignment rather than relying on the encounter's campaign flag.

[`BossControllerTests`](../Assets/Scripts/Tests/EditMode/BossControllerTests.cs) cover ordered commitment, cooldowns, audience isolation and locked targets. [`BossBattleTests`](../Assets/Scripts/Tests/PlayMode/BossBattleTests.cs) exercise the real prototype tree across turns, committed choices, final opponent response before Judgment, and returning to a standard fight.
