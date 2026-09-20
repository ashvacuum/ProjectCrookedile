using System;
using System.Collections.Generic;
using System.Linq;
using Crookedile.Data.Campaign;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>How a single authored site touches a flag.</summary>
    internal enum FlagUseKind
    {
        Set,
        Clear,
        Read,
        ReadNot,
    }

    /// <summary>One place a flag name appears, resolved back to the asset that spells it.</summary>
    internal readonly struct FlagUse
    {
        public readonly string Flag;
        public readonly FlagUseKind Kind;

        /// <summary>Asset to ping when the row is clicked — the encounter, or the pool.</summary>
        public readonly UnityEngine.Object Owner;

        /// <summary>Human-readable site, e.g. <c>option "Take the envelope"</c>.</summary>
        public readonly string Where;

        public FlagUse(string flag, FlagUseKind kind, UnityEngine.Object owner, string where)
        {
            Flag = flag;
            Kind = kind;
            Owner = owner;
            Where = where;
        }

        public bool IsWrite => Kind == FlagUseKind.Set || Kind == FlagUseKind.Clear;
    }

    /// <summary>
    /// Flags are free-form strings with nothing linking a <see cref="SetFlagOutcome"/> to the
    /// <see cref="HasFlag"/> that reads it — a typo on either side fails silently and forever.
    /// This walks every encounter (and a pool's rows) once and pairs the two sides up by name,
    /// which is all it takes to make a dangling flag visible.
    /// </summary>
    internal static class FlagIndex
    {
        /// <summary>Blank flag names still get a group, so they can't hide.</summary>
        public const string Blank = "(blank)";

        public static List<FlagUse> Build(EncounterPoolData pool)
        {
            var uses = new List<FlagUse>();

            foreach (string guid in AssetDatabase.FindAssets("t:EncounterData"))
            {
                var encounter = AssetDatabase.LoadAssetAtPath<EncounterData>(
                    AssetDatabase.GUIDToAssetPath(guid)
                );
                if (!(encounter is EventEncounterData ev))
                    continue;

                foreach (var option in ev.Options)
                {
                    if (option == null)
                        continue;
                    string site = string.IsNullOrWhiteSpace(option.Label)
                        ? "option (unlabelled)"
                        : $"option \"{option.Label}\"";

                    foreach (var req in option.Requirements)
                        if (req is HasFlag f)
                            uses.Add(
                                new FlagUse(
                                    Name(f.Flag),
                                    f.Negated ? FlagUseKind.ReadNot : FlagUseKind.Read,
                                    ev,
                                    site
                                )
                            );

                    foreach (var outcome in option.Outcomes)
                        if (outcome is SetFlagOutcome s)
                            uses.Add(
                                new FlagUse(
                                    Name(s.Flag),
                                    s.Clears ? FlagUseKind.Clear : FlagUseKind.Set,
                                    ev,
                                    site
                                )
                            );
                }
            }

            if (pool != null)
            {
                foreach (var entry in pool.Entries)
                {
                    if (entry?.Encounter == null)
                        continue;
                    string label = entry.Encounter.name;
                    Collect(entry.Requirements, $"pool row {label} → Requirements");
                    Collect(entry.BoostIf, $"pool row {label} → BoostIf");
                }

                void Collect(IReadOnlyList<RunRequirement> reqs, string site)
                {
                    foreach (var req in reqs)
                        if (req is HasFlag f)
                            uses.Add(
                                new FlagUse(
                                    Name(f.Flag),
                                    f.Negated ? FlagUseKind.ReadNot : FlagUseKind.Read,
                                    pool,
                                    site
                                )
                            );
                }
            }

            return uses;
        }

        private static string Name(string flag) =>
            string.IsNullOrWhiteSpace(flag) ? Blank : flag.Trim();

        /// <summary>
        /// Closest write-side flag to <paramref name="flag"/>, or null when nothing is near
        /// enough to be a plausible typo. Threshold scales with length so short names don't
        /// match everything.
        /// </summary>
        public static string ClosestMatch(string flag, IEnumerable<string> candidates)
        {
            if (flag == Blank)
                return null;

            int budget = Mathf.Clamp(flag.Length / 3, 1, 4);
            string best = null;
            int bestDistance = int.MaxValue;

            foreach (string candidate in candidates)
            {
                if (candidate == flag || candidate == Blank)
                    continue;
                int d = Distance(flag, candidate);
                if (d <= budget && d < bestDistance)
                {
                    best = candidate;
                    bestDistance = d;
                }
            }
            return best;
        }

        // ponytail: plain Levenshtein over two rows. Flag counts are in the dozens; if this ever
        // runs over thousands of names, cap the candidate list before reaching for anything smarter.
        private static int Distance(string a, string b)
        {
            var previous = new int[b.Length + 1];
            var current = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++)
                previous[j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                current[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    current[j] = Math.Min(
                        Math.Min(current[j - 1] + 1, previous[j] + 1),
                        previous[j - 1] + cost
                    );
                }
                (previous, current) = (current, previous);
            }
            return previous[b.Length];
        }
    }
}
