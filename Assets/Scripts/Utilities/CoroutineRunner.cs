using System.Collections;
using UnityEngine;
using Crookedile.Core;

namespace Crookedile.Utilities
{
    public class CoroutineRunner : Singleton<CoroutineRunner>
    {
        public static Coroutine Run(IEnumerator coroutine)
        {
            if (Instance == null) return null;
            return Instance.StartCoroutine(coroutine);
        }

        public static void Stop(Coroutine coroutine)
        {
            if (Instance == null || coroutine == null) return;
            Instance.StopCoroutine(coroutine);
        }

        public static void StopAll()
        {
            if (Instance == null) return;
            Instance.StopAllCoroutines();
        }
    }
}
