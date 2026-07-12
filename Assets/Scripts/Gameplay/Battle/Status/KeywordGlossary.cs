using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// The keyword layer for card text: every StatusBehavior display name plus a small set of
    /// core game terms. UI code calls <see cref="Linkify"/> to wrap known keywords in TMP
    /// link tags (underlined), then resolves hovers back through <see cref="TryGet"/> to feed
    /// the tooltip. Effect descriptions stay PLAIN text — markup is applied at display time
    /// only, so the CSV exports, logs, and editor windows never see tags.
    /// </summary>
    public static class KeywordGlossary
    {
        public const string LinkPrefix = "kw:";

        // Core non-status terms. Statuses come from the registry automatically.
        // ponytail: hardcoded dictionary — move to an SO only if a designer ever needs to edit
        // these without a code change.
        private static readonly Dictionary<string, string> CoreTerms = new Dictionary<string, string>
        {
            ["Convert"] =
                "Consume the target's pacify stacks (needs 3 + their Jaded) for an opinion burst. The enemy reverts to neutral.",
            ["Exhaust"] = "Removed from play for the rest of the battle.",
            ["Retain"] = "Not discarded at the end of this turn.",
            ["Unplayable"] = "Cannot be played; it clogs your hand.",
            ["Scandal"] = "Unplayable junk that clogs your hand until addressed.",
            ["Heckle"] = "Temporary junk card; leaves your deck when the battle ends.",
            ["Pressure"] = "Pushes the Opinion Meter toward your side. Blocked by Denial.",
            ["Opinion"] = "The shared meter. Fill it to win the room; hit zero and you lose it.",
            ["Support"] = "Your shield on the meter: absorbs enemy pushes. Expires at the start of your next turn.",
            ["Denial"] = "The enemy shield on the meter: absorbs your Pressure. Expires at the start of their next turn.",
            ["Hostility"] = "How aggressive an enemy is. Hostile enemies push harder; receptive ones hold back.",
            ["Patronage"] = "Banked by sacrificing cards: their cost, +1 if Rare, +1 if Upgraded (junk and 0-cost give 1). Pays for Patronage-gated cards.",
            ["Attention"] = "Banked spotlight. Spend it for Opinion payoffs.",
        };

        private static Dictionary<string, (string title, string description)> _entries;
        private static Regex _matcher;

        private static void EnsureBuilt()
        {
            if (_entries != null)
                return;

            _entries = new Dictionary<string, (string, string)>();
            foreach (var behavior in StatusRegistry.All)
                _entries[behavior.DisplayName] = (behavior.DisplayName, behavior.Describe(1));
            foreach (var kvp in CoreTerms)
                _entries[kvp.Key] = (kvp.Key, kvp.Value);

            // One alternation regex over all keywords, longest first so "Drama King"-style
            // multiword names beat their prefixes. Word boundaries keep "Ward" out of "Warded".
            var names = new List<string>(_entries.Keys);
            names.Sort((a, b) => b.Length.CompareTo(a.Length));
            for (int i = 0; i < names.Count; i++)
                names[i] = Regex.Escape(names[i]);
            _matcher = new Regex($@"\b({string.Join("|", names)})\b");
        }

        /// <summary>
        /// Wraps every known keyword in a <c>&lt;link&gt;</c> tag (hover detection) plus the
        /// "Keyword" TMP style — the visual treatment lives in the TMP Settings default style
        /// sheet, editable any time without touching code. An undefined style renders as plain
        /// text, so the system degrades gracefully until the style is authored.
        /// </summary>
        public static string Linkify(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            EnsureBuilt();
            return _matcher.Replace(
                text,
                m => $"<link=\"{LinkPrefix}{m.Value}\"><style=\"Keyword\">{m.Value}</style></link>"
            );
        }

        /// <summary>Resolves a link id (from TMP hover) back to tooltip content.</summary>
        public static bool TryGet(string linkId, out string title, out string description)
        {
            EnsureBuilt();
            string key = linkId != null && linkId.StartsWith(LinkPrefix)
                ? linkId.Substring(LinkPrefix.Length)
                : linkId;
            if (key != null && _entries.TryGetValue(key, out var entry))
            {
                title = entry.title;
                description = entry.description;
                return true;
            }
            title = description = null;
            return false;
        }
    }
}
