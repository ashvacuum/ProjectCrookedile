using Crookedile.Data.Cards;
using Crookedile.Data.Save;

namespace Crookedile.Data.Unlocks
{
    /// <summary>
    /// The one answer to "is this unlocked?". Pure functions over a profile or a run, with no
    /// lifetime, so reward pools, the save system and editor tools all ask the same thing.
    /// </summary>
    public static class UnlockRules
    {
        /// <summary>
        /// When false, every piece of content counts as unlocked, whatever its condition. Off
        /// while the unlock content is unauthored (docs/unlocks.md); conditions, grants and
        /// counters keep recording either way, so turning it on loses nothing.
        /// ponytail: a static switch. Move it to a settings asset if designers need to flip it.
        /// </summary>
        public static bool LocksEnabled { get; set; }

        /// <summary>
        /// True when <paramref name="card"/> is unlocked for <paramref name="profile"/>: it isn't
        /// locked at all, the dev override is on, it was granted, or its condition is met. A
        /// locked card with no condition only unlocks through a grant.
        /// </summary>
        public static bool IsUnlocked(CardData card, ProfileData profile)
        {
            if (card == null)
                return false;
            if (!card.IsUnlockable || !LocksEnabled)
                return true;
            if (profile == null)
                return false;
            return profile.UnlockAll
                || profile.GrantedUnlocks.Contains(card.ID)
                || (card.UnlockCondition != null && card.UnlockCondition.IsMet(profile, card.ID));
        }

        /// <summary>
        /// True when <paramref name="card"/> may appear in the current run: not locked, or in
        /// the unlock set the run took when it started. Unlocks earned mid-run apply from the
        /// next run. With no run (editor tools, tests) a locked card is unavailable.
        /// </summary>
        public static bool IsAvailableThisRun(CardData card)
        {
            if (card == null)
                return false;
            if (!card.IsUnlockable || !LocksEnabled)
                return true;
            var run = RunState.Current;
            return run != null && run.UnlockedContent.Contains(card.ID);
        }

        /// <summary>How to unlock <paramref name="card"/>, for the unlock list.</summary>
        public static string HowToUnlock(CardData card)
        {
            if (card == null || !card.IsUnlockable)
                return "";
            return card.UnlockCondition != null
                ? card.UnlockCondition.Describe()
                : "Found during a campaign";
        }
    }
}
