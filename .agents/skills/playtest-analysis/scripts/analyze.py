"""Deeper cuts of a Crookedile playtest folder than report.md gives.

Usage (from the repo root):
    py -3 -I .claude/skills/playtest-analysis/scripts/analyze.py Playtests/<stamp> [Playtests/<previous-stamp>]

Prints: coverage, skill gap, classes (per-origin skill gap, damage taken, output, worst encounters,
campaigns, class multipliers), per-encounter win rates (with deltas against a previous run), cards against
each class's Basic attack curve (over-curve, weak for their rarity, no output, utility, never played),
decisive single-play bursts, every boss fight's turn-by-turn room, how boss wins happened, and prior
campaign losses against the boss result. Reads only the CSVs; changes nothing.
"""
import csv
import collections as C
import os
import sys

CAREFUL, CARELESS = ("Scored", "Greedy"), "Random"
BURST = 30  # a single play worth this much Opinion is a burst


def load(folder, name):
    path = os.path.join(folder, name)
    if not os.path.exists(path):
        return []
    with open(path, encoding="utf8") as f:
        return list(csv.DictReader(f))


def rate(rows):
    return sum(r["win"] == "1" for r in rows) / len(rows) if rows else 0.0


def pct(x):
    return f"{x * 100:.0f}%"


def section(title):
    print(f"\n## {title}")


def standalone(battles):
    return [b for b in battles if b["context"] == "battle"]


def coverage(battles, campaigns):
    section("Coverage")
    sa = standalone(battles)
    print("standalone battles:", len(sa), "| origins:", sorted({b["origin"] for b in sa}),
          "| bots:", sorted({b["bot"] for b in sa}))
    per = C.Counter((b["encounter"], b["origin"], b["bot"]) for b in sa)
    if per:
        print("battles per encounter x origin x bot:", min(per.values()), "-", max(per.values()),
              "(small cells are noisy; 5 = +/-20 pts)")
    print("campaign runs:", len(campaigns), dict(C.Counter((c["bot"], c["origin"]) for c in campaigns)))


def skill_gap(battles, prev):
    section("Skill gap (best of Scored/Greedy minus Random), target 40+")
    for label, rows in (("now", standalone(battles)), ("previous", standalone(prev))):
        if not rows:
            continue
        by = {bot: rate([b for b in rows if b["bot"] == bot]) for bot in {b["bot"] for b in rows}}
        careful = max(by.get(b, 0) for b in CAREFUL)
        print(f"{label:9} careful {pct(careful)}  Random {pct(by.get(CARELESS, 0))}  gap {pct(careful - by.get(CARELESS, 0))}")
    sa = standalone(battles)
    print("\nby origin:", {o: pct(rate([b for b in sa if b["origin"] == o])) for o in sorted({b["origin"] for b in sa})})


def encounters(battles, prev):
    section("Encounters: careful / Random / gap (previous run in brackets)")
    old = {e: rate([b for b in standalone(prev) if b["encounter"] == e]) for e in {b["encounter"] for b in standalone(prev)}}
    rows = []
    for e in sorted({b["encounter"] for b in standalone(battles)}):
        g = [b for b in standalone(battles) if b["encounter"] == e]
        careful = max(rate([b for b in g if b["bot"] == bot]) for bot in CAREFUL)
        rnd = rate([b for b in g if b["bot"] == CARELESS])
        rows.append((careful - rnd, e, careful, rnd, rate(g)))
    for gap, e, careful, rnd, overall in sorted(rows):
        was = f" (all bots was {pct(old[e])})" if e in old else ""
        print(f"  {e:30} careful {pct(careful):>4}  Random {pct(rnd):>4}  gap {pct(gap):>5}  all {pct(overall)}{was}")


def bursts(battles, plays):
    section(f"Decisive bursts: one play of {BURST}+ without which the win is lost")
    by_battle = {(b["context"], b["bot"], b["origin"], b["encounter"], b["seed"]): b for b in battles}
    dec, seen, ex = C.Counter(), C.Counter(), C.defaultdict(list)
    for p in plays:
        b = by_battle.get((p["context"], p["bot"], p["origin"], p["encounter"], p["seed"]))
        if not b or b["win"] != "1" or int(p["opinion_delta"]) < BURST:
            continue
        seen[p["card"]] += 1
        # ponytail: assumes a 60-point line on a 100 meter; boss meters differ, so read boss rows with care.
        line = 60 if b["judgment"] == "1" else 100
        if int(b["final_opinion"]) - int(p["opinion_delta"]) < line:
            dec[p["card"]] += 1
            if len(ex[p["card"]]) < 3:
                ex[p["card"]].append(f"{b['origin']}/{b['encounter']} +{p['opinion_delta']} T{p['turn']}")
    for card, n in seen.most_common(12):
        print(f"  {card:24} decisive {dec[card]:4} of {n:4}  e.g. {ex[card]}")


def bosses(battles, plays, campaigns):
    boss_names = sorted({b["encounter"] for b in battles if b["encounter"] in BOSS_ENCOUNTERS})
    if not boss_names:
        return
    turns = load(FOLDER, "turns.csv")
    for name in boss_names:
        fights = [b for b in battles if b["encounter"] == name]
        section(f"Boss: {name}")
        print("rosters:", dict(C.Counter(b["enemies"] for b in fights)))
        print("start Opinion:", dict(C.Counter(b["start_opinion"] for b in fights)),
              "| meter:", dict(C.Counter(t["max_opinion"] for t in turns if t["encounter"] == name)))
        print(f"wins {sum(b['win'] == '1' for b in fights)} of {len(fights)} | avg turns "
              f"{sum(int(b['turns']) for b in fights) / len(fights):.1f} | avg damage taken "
              f"{sum(int(b['damage_to_player']) for b in fights) / len(fights):.0f}")
        by_turn = C.defaultdict(list)
        for t in turns:
            if t["encounter"] == name and t["turn"].isdigit():
                by_turn[int(t["turn"])].append(t)
        print("turn  Opinion  room hostility  hostile/neutral/receptive  support")
        for n in sorted(by_turn)[:8]:
            rs = by_turn[n]
            a = lambda k: sum(float(r[k]) for r in rs) / len(rs)
            print(f"  T{n:<2} {a('opinion'):7.0f} {a('avg_hostility'):+15.1f}   "
                  f"{a('hostile'):.1f}/{a('neutral'):.1f}/{a('receptive'):.1f}   {a('support'):6.1f}  (n={len(rs)})")
        for w in [b for b in fights if b["win"] == "1"][:6]:
            seq = [p for p in plays if p["context"] == w["context"] and p["seed"] == w["seed"] and p["encounter"] == name]
            print(f"  WIN {w['origin']}/{w['bot']} ({w['context']}) turn {w['turns']}: "
                  + ", ".join(f"T{p['turn']} {p['card']} {int(p['opinion_delta']):+d}" for p in seq))
        prior_losses(battles, name)


def prior_losses(battles, name):
    runs = C.defaultdict(list)
    for b in battles:
        if b["context"].startswith("campaign:"):
            runs[b["context"]].append(b)
    rows = []
    for fights in runs.values():
        boss = [f for f in fights if f["encounter"] == name]
        if boss:
            before = fights[: fights.index(boss[-1])]
            rows.append((min(sum(f["win"] == "0" for f in before), 4), boss[-1]))
    if not rows:
        return
    print("prior campaign losses | runs | boss wins | avg end Opinion | avg turns | deck")
    for k in sorted({r[0] for r in rows}):
        g = [b for n, b in rows if n == k]
        print(f"  {('4+' if k == 4 else k):>20} | {len(g):4} | {sum(b['win'] == '1' for b in g):9} | "
              f"{sum(int(b['final_opinion']) for b in g) / len(g):15.1f} | {sum(int(b['turns']) for b in g) / len(g):9.1f} | "
              f"{sum(int(b['deck']) for b in g) / len(g):4.0f}")


CARD_ROOT = os.path.join("Assets", "Data", "Cards")
CLASS_FOLDER = {"FaithLeader": "FaithLeader", "NepoBaby": "NepoBaby", "Celebrity": "Actor"}  # folder -> origin in the CSVs
RARITY = {0: "Basic", 1: "Enhanced", 2: "Rare"}
CARD_TYPE = {0: "Pressure", 1: "Rhetoric", 2: "Policy", 3: "Heckle", 4: "Scandal"}


def card_catalog():
    """Card name -> origin, rarity, type, cost, starter, generated, read from the assets."""
    import re
    cat = {}
    for folder, origin in CLASS_FOLDER.items():
        for dirpath, _, files in os.walk(os.path.join(CARD_ROOT, folder)):
            for fn in files:
                if not fn.endswith(".asset"):
                    continue
                t = open(os.path.join(dirpath, fn), encoding="utf8").read()
                name = re.search(r"^  _cardName: (.*)$", t, re.M)
                if not name:
                    continue
                field = lambda k: int((re.search(rf"^  {k}: (\d+)", t, re.M) or [None, "0"])[1])
                base = re.search(r"_baseAmount: (\d+)", t)  # the first cost entry is the AP cost
                x = re.search(r"_isXCost: (\d)", t)
                cat[name.group(1).strip().strip("'\"")] = {
                    "origin": origin,
                    "rarity": RARITY.get(field("_rarity"), "?"),
                    "type": CARD_TYPE.get(field("_cardType"), "?"),
                    "cost": "X" if x and x.group(1) == "1" else (base.group(1) if base else "?"),
                    "starter": field("_isStarterCard") == 1,
                    "generated": field("_isGeneratedOnly") == 1,
                }
    return cat


def utility_from_report(folder):
    """Card -> (prevented, energy, extra plays, passive Opinion) per play, from report.md's Utility table."""
    path = os.path.join(folder, "report.md")
    util, inside = {}, False
    for line in open(path, encoding="utf8") if os.path.exists(path) else []:
        if line.startswith("## Utility"):
            inside = True
        elif inside and line.startswith("## "):
            break
        elif inside and line.startswith("| ") and not line.startswith("| Card"):
            cells = [c.strip() for c in line.strip().strip("|").split("|")]
            try:
                util[cells[0]] = tuple(float(c) for c in cells[3:7])
            except (ValueError, IndexError):
                pass
    return util


def classes(battles, plays, campaigns):
    section("Classes (origins)")
    sa = standalone(battles)
    origins = sorted({b["origin"] for b in sa})
    turns = load(FOLDER, "turns.csv")
    print("origin       win   careful Random gap   dmg/battle cards/turn Opinion/AP hostility/play")
    for o in origins:
        g = [b for b in sa if b["origin"] == o]
        careful = max(rate([b for b in g if b["bot"] == bot]) for bot in CAREFUL)
        rnd = rate([b for b in g if b["bot"] == CARELESS])
        p = [x for x in plays if x["origin"] == o and x["context"] == "battle"]
        ap = sum(max(1, int(x["cost"])) for x in p) or 1
        print(f"  {o:11} {pct(rate(g)):>4}  {pct(careful):>5}  {pct(rnd):>5} {pct(careful - rnd):>5} "
              f"{sum(int(b['damage_to_player']) for b in g) / len(g):9.1f} "
              f"{sum(int(b['cards_played']) / max(1, int(b['turns'])) for b in g) / len(g):10.2f} "
              f"{sum(int(x['opinion_delta']) for x in p) / ap:10.2f} {sum(int(x['room_hostility_delta']) for x in p) / max(1, len(p)):+13.2f}")
    print("\nworst encounters per origin, against the other origins on the same encounter:")
    for o in origins:
        gaps = []
        for e in {b["encounter"] for b in sa}:
            mine = [b for b in sa if b["encounter"] == e and b["origin"] == o]
            others = [b for b in sa if b["encounter"] == e and b["origin"] != o]
            if mine and others:
                gaps.append((rate(mine) - rate(others), e, rate(mine), rate(others)))
        print(f"  {o}: " + "; ".join(f"{e} {pct(m)} vs {pct(t)}" for _, e, m, t in sorted(gaps)[:3]))
    if campaigns:
        print("\ncampaigns per origin (completed, Credibility at the end, final deck):")
        for o in origins:
            c = [r for r in campaigns if r["origin"] == o]
            if c:
                print(f"  {o:11} {sum(r['end'] == 'Completed' for r in c)}/{len(c)}  cred {sum(int(r['credibility']) for r in c) / len(c):.0f}"
                      f"  deck {sum(int(r['deck']) for r in c) / len(c):.0f}")
    for cfg in ("NepoBabyConfig", "FaithLeaderConfig"):
        path = os.path.join("Assets", "Resources", cfg + ".asset")
        if os.path.exists(path):
            lines = [l.strip() for l in open(path, encoding="utf8") if "Multiplier" in l]
            if lines:
                print(f"{cfg}: {', '.join(lines)}")


def cards(plays):
    section("Cards: value against each class's curve")
    cat, util = card_catalog(), utility_from_report(FOLDER)
    stats = C.defaultdict(lambda: [0, 0, 0, 0])  # plays, opinion, AP, hostility
    for p in plays:
        s = stats[p["card"]]
        s[0] += 1
        s[1] += int(p["opinion_delta"])
        s[2] += max(1, int(p["cost"]))
        s[3] += int(p["room_hostility_delta"])
    per_ap = {c: s[1] / s[2] for c, s in stats.items() if s[2]}

    def useful(c):  # prevented, extra plays or passive Opinion worth mentioning
        u = util.get(c)
        return u and (u[0] >= 2 or u[2] >= 0.5 or u[3] >= 1)

    for origin in sorted({v["origin"] for v in cat.values()}):
        mine = [c for c, v in cat.items() if v["origin"] == origin and not v["generated"]]
        attacks = sorted(per_ap[c] for c in mine if c in per_ap and per_ap[c] > 0 and stats[c][0] >= 30 and cat[c]["rarity"] == "Basic")
        if not attacks:
            continue
        curve = attacks[len(attacks) // 2]
        print(f"\n{origin}: Basic attack curve {curve:.1f} Opinion per AP")
        tag = lambda c: f"{cat[c]['rarity']} {cat[c]['type']} cost {cat[c]['cost']}{' starter' if cat[c]['starter'] else ''}"
        over = sorted((per_ap[c], c) for c in mine if c in per_ap and stats[c][0] >= 20 and per_ap[c] >= 1.6 * curve)
        for v, c in reversed(over):
            print(f"  OVER CURVE  {c:24} {v:5.1f}/AP over {stats[c][0]} plays, hostility {stats[c][3] / stats[c][0]:+.1f}/play  ({tag(c)})")
        for c in sorted(mine):
            if c not in per_ap or stats[c][0] < 20 or cat[c]["rarity"] == "Basic" or useful(c):
                continue
            if 0 < per_ap[c] < curve:
                print(f"  WEAK RARITY {c:24} {per_ap[c]:5.1f}/AP and no utility to speak of  ({tag(c)})")
            elif per_ap[c] <= 0:
                # Draws, statuses and card moves aren't measured; hostility is shown so calming cards read right.
                print(f"  NOTHING MEASURED {c:19} no Opinion, Support or energy over {stats[c][0]} plays, "
                      f"hostility {stats[c][3] / stats[c][0]:+.1f}/play  ({tag(c)})")
        for c in sorted(mine):
            if c in util and useful(c):
                u = util[c]
                print(f"  UTILITY     {c:24} prevented {u[0]:.1f}, energy {u[1]:.1f}, extra plays {u[2]:.2f}, passive Opinion {u[3]:.1f} per play")
        unplayed = [c for c in mine if c not in stats and not cat[c]["starter"]]
        if unplayed:
            print(f"  NEVER PLAYED ({len(unplayed)}): {', '.join(sorted(unplayed))}")


# Boss encounters by name. ponytail: battles.csv has no boss column; add one if bosses multiply.
BOSS_ENCOUNTERS = {"The Incumbent", "The Debate Stage"}

if __name__ == "__main__":
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    FOLDER = sys.argv[1]
    PREV = sys.argv[2] if len(sys.argv) > 2 else None
    battles, plays, campaigns = load(FOLDER, "battles.csv"), load(FOLDER, "card_plays.csv"), load(FOLDER, "campaigns.csv")
    prev = load(PREV, "battles.csv") if PREV else []
    if not battles:
        sys.exit(f"No battles.csv in {FOLDER}")
    coverage(battles, campaigns)
    skill_gap(battles, prev)
    classes(battles, plays, campaigns)
    encounters(battles, prev)
    cards(plays)
    bursts(battles, plays)
    bosses(battles, plays, campaigns)
