using System;
using UnityEngine;

namespace Crookedile.Utilities
{
    /// <summary>
    /// The runtime cheat switch. <c>CHEATS_ENABLED</c> decides whether cheat code is compiled into
    /// a build at all; this decides whether a build that has it will run it.
    ///
    /// On from the start in the editor and in development builds. Any other build starts with it
    /// off until the <c>-cheats</c> launch argument or <see cref="EnableCode"/> turns it on.
    /// </summary>
    public static class Cheats
    {
        /// <summary>Typed anywhere outside the console, this turns cheats on for the session.</summary>
        public static string EnableCode = "crookedile";

        private static bool? _enabled;

        /// <summary>True when cheat commands may run and the console may open.</summary>
        public static bool Enabled
        {
            get =>
                _enabled ??=
                    !Enforced
                    || Array.IndexOf(Environment.GetCommandLineArgs(), "-cheats") >= 0;
            set => _enabled = value;
        }

        /// <summary>
        /// True where using a cheat has consequences (no profile progress, no Steam progress):
        /// everywhere except the editor and development builds, which are a developer's sandbox.
        /// </summary>
        public static bool Enforced => !Application.isEditor && !Debug.isDebugBuild;

        /// <summary>True once any cheat has run since launch. Never resets.</summary>
        public static bool UsedThisSession { get; private set; }

        /// <summary>Records that a cheat ran. The caller also flags the run in progress, if any.</summary>
        public static void MarkUsed() => UsedThisSession = true;
    }
}
