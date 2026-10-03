using System.Collections;
using UnityEngine;

namespace Crookedile.Utilities
{
    /// <summary>
    /// A bare MonoBehaviour to run a coroutine on. Editor tools that need Play Mode frames (the
    /// playtest bot) can't attach their own MonoBehaviours — Unity refuses components from Editor
    /// assemblies — so they borrow this one.
    /// </summary>
    public class CoroutineHost : MonoBehaviour
    {
        /// <summary>Creates a host GameObject named <paramref name="name"/> and starts <paramref name="routine"/> on it.</summary>
        public static CoroutineHost Run(string name, IEnumerator routine)
        {
            var host = new GameObject(name).AddComponent<CoroutineHost>();
            host.StartCoroutine(routine);
            return host;
        }
    }
}
