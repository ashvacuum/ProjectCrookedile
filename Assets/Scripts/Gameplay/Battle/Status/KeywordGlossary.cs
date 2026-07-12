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
        /// Wraps every known keyword in <c>&lt;link&gt;</c> + underline TMP tags for tooltip
        /// hover detection. Idempotent-enough for card text (call on plain descriptions only).
        /// </summary>
        public static string Linkify(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            EnsureBuilt();
            return _matcher.Replace(text, m => $"<link=\"{LinkPrefix}{m.Value}\"><u>{m.Value}</u></link>");
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
