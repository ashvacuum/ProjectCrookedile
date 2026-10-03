using System;
using System.Collections.Generic;
using Crookedile.Data.Save;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data.Unlocks
{
    /// <summary>
    /// When a locked piece of content unlocks for a profile. Lives on the content asset itself
    /// (<c>CardData</c>), so there is no separate unlock database. Conditions only read the
    /// profile, so they can be checked anywhere, including editor tools with no game running.
    ///
    /// To add one: a <c>[Serializable]</c> subclass with its fields and a description. Counters
    /// only grow, so a condition that has passed keeps passing.
    /// </summary>
    [Serializable]
    [InfoBox(
        "@$value == null ? \"(no condition chosen)\" : $value.Describe()",
        InfoMessageType.None
    )]
    public abstract class UnlockCondition
    {
        /// <summary>True when <paramref name="profile"/> has unlocked the content with <paramref name="contentId"/>.</summary>
        public abstract bool IsMet(ProfileData profile, string contentId);

        /// <summary>How to unlock it, phrased for the player ("Win a run as Nepo Baby").</summary>
        public abstract string Describe();
    }

    /// <summary>Unlocks once a lifetime profile counter reaches a value.</summary>
    [Serializable]
    public class CounterAtLeast : UnlockCondition
    {
        [Tooltip("The profile counter to read. Code increments these; pick, don't invent.")]
        [ValueDropdown(nameof(CounterKeys))]
        [SerializeField]
        private string _counter = ProfileCounters.RunsWon;

        [Tooltip("Unlocks when the counter reaches this value.")]
        [MinValue(1)]
        [SerializeField]
        private int _value = 1;

        private static IEnumerable<string> CounterKeys() => ProfileCounters.All();

        public override bool IsMet(ProfileData profile, string contentId) =>
            profile != null && profile.GetCounter(_counter) >= _value;

        public override string Describe() => $"Reach {_value} {_counter.Replace('_', ' ')}";
    }

    /// <summary>Unlocks after winning a run as a given origin.</summary>
    [Serializable]
    public class WonRunAs : UnlockCondition
    {
        [Tooltip("The origin to win a run with.")]
        [SerializeField]
        private OriginType _origin = OriginType.NepoBaby;

        public override bool IsMet(ProfileData profile, string contentId) =>
            profile != null && profile.GetCounter(ProfileCounters.RunsWonAs(_origin)) > 0;

        public override string Describe() => $"Win a run as {OriginDisplayName(_origin)}";

        internal static string OriginDisplayName(OriginType origin) =>
            origin switch
            {
                OriginType.FaithLeader => "Faith Leader",
                OriginType.NepoBaby => "Nepo Baby",
                OriginType.Actor => "Celebrity",
                _ => origin.ToString(),
            };
    }

    /// <summary>
    /// Unlocks only through an explicit grant (a campaign event's UnlockContentOutcome). Any
    /// content can be granted; this condition says a grant is the only way.
    /// </summary>
    [Serializable]
    public class GrantedByEvent : UnlockCondition
    {
        [Tooltip("Where the player finds it, shown in the unlock list (e.g. 'Help the Fixer').")]
        [SerializeField]
        private string _hint = "";

        public override bool IsMet(ProfileData profile, string contentId) =>
            profile != null && contentId != null && profile.GrantedUnlocks.Contains(contentId);

        public override string Describe() =>
            string.IsNullOrEmpty(_hint) ? "Found during a campaign" : _hint;
    }
}
