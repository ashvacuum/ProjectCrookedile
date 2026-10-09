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
        private static readonly Dictionary<string, string> CoreTerms = new Dictionary<
            string,
            string
        >
        {
            ["Convert"] =
                "Consume the target's pacify stacks (needs 3 + their Jaded) for an opinion burst. The enemy reverts to neutral.",
            ["Exhaust"] = "Removed from play for the rest of the battle.",
            ["Retain"] = "Not discarded at the end of this turn.",
            ["Unplayable"] = "Cannot be played; it clogs your hand.",
            ["Scandal"] = "Unplayable junk that clogs your hand until addressed.",
            ["Heckle"] = "Temporary junk card; leaves your deck when the battle ends.",
            ["Opinion"] = "The shared meter. Fill it to win the room; hit zero and you lose it.",
            ["Support"] =
                "Absorbs incoming Opinion drops on the meter. Expires at the start of your next turn.",
            ["Denial"] =
                "Absorbs incoming Opinion rises on the meter. Expires at the start of their next turn.",
            ["Hostility"] =
                "How aggressive an enemy is. Hostile enemies push harder; receptive ones hold back.",
            ["Aggravate"] = "Raise an enemy's Hostility. Fanatic enemies ignore it.",
            ["Soothe"] = "Lower an enemy's Hostility. Hardened enemies ignore it.",
            ["Hostile"] =
                "An enemy with high Hostility. It uses its hostile moves, which push hardest.",
            ["Neutral"] = "An enemy between Hostile and Receptive. It uses its neutral moves.",
            ["Receptive"] =
                "An enemy with low Hostility. It uses its gentler moves and gives you Support.",
            ["Sway"] = "An Opinion push from your cards. It goes through the enemy's Denial.",
            ["Energy"] = "Spent to play cards. Refills at the start of your turn.",
            ["Debt"] =
                "Owed Energy. Settled at the start of your next turn, out of that turn's Energy first and the meter second.",
            ["Borrow"] = "Gain Energy now in exchange for Debt.",
            ["Burn"] = "Exhaust a card from your hand as a cost. Policies can't be burned.",
            ["Pull"] = "Take a chosen card from your draw pile into your hand.",
            ["Rehearse"] =
                "Put up to that many cards from your hand on top of your draw pile, in the order you choose.",
            ["Scry"] = "Look at the top cards of your draw pile and discard any of them.",
            ["Replay"] = "The card's effects happen again. A replay isn't a new play.",
        };

        private static Dictionary<string, (string title, string description)> _entries;
        private static Regex _matcher;

        private static void EnsureBuilt()
        {
            if (_entries != null)
                return;

            _entries = new Dictionary<string, (string, string)>(
                System.StringComparer.OrdinalIgnoreCase
            );
            foreach (var behavior in StatusRegistry.All)
                _entries[behavior.DisplayName] = (behavior.DisplayName, behavior.Describe(1));
            foreach (var kvp in CoreTerms)
                _entries[kvp.Key] = (kvp.Key, kvp.Value);

            // One alternation regex over all keywords, longest first so "Drama King"-style
            // multiword names beat their prefixes. Word boundaries keep "Ward" out of "Warded"; case is
            // ignored so "hostile" and "energy" in running text link too.
            var names = new List<string>(_entries.Keys);
            names.Sort((a, b) => b.Length.CompareTo(a.Length));
            for (int i = 0; i < names.Count; i++)
                names[i] = Regex.Escape(names[i]);
            _matcher = new Regex($@"\b({string.Join("|", names)})\b", RegexOptions.IgnoreCase);
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

        /// <summary>The first known keyword in plain <paramref name="text"/>, with its glossary entry.</summary>
        public static bool TryGetFirst(string text, out string title, out string description)
        {
            EnsureBuilt();
            var match = string.IsNullOrEmpty(text) ? null : _matcher.Match(text);
            if (match != null && match.Success)
                return TryGet(match.Value, out title, out description);
            title = description = null;
            return false;
        }

        /// <summary>Resolves a link id (from TMP hover) back to tooltip content.</summary>
        public static bool TryGet(string linkId, out string title, out string description)
        {
            EnsureBuilt();
            string key =
                linkId != null && linkId.StartsWith(LinkPrefix)
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
