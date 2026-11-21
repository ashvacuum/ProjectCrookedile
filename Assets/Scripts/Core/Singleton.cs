using UnityEngine;

namespace Crookedile.Core
{
    /// <summary>
    /// One instance for the whole session, kept across scene loads. Only for services that own
    /// everything they reference (audio, cheats, Steam, scene loading); the UI assembly's
    /// <c>Bootstrap</c> creates them before the first scene, so <see cref="Instance"/> never
    /// makes an unconfigured stand-in: a service the bootstrapper didn't start is null and logged.
    /// A manager wired to objects in its scene is a <see cref="SceneSingleton{T}"/> instead.
    /// </summary>
    public class Singleton<T> : MonoBehaviour
        where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindFirstObjectByType<T>();
                        if (_instance == null && !_reportedMissing)
                        {
                            _reportedMissing = true;
                            Debug.LogError(
                                $"{typeof(T).Name} is not running: Bootstrap didn't start it."
                            );
                        }
                    }

                    return _instance;
                }
            }
        }

        private static bool _reportedMissing;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                // Services share the bootstrapper's object; only a root can be kept across loads.
                DontDestroyOnLoad(transform.root.gameObject);
                OnAwake();
            }
            else if (_instance != this)
            {
                // Only this component: a shared services object must not lose its neighbours.
                Destroy(this);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                OnCleanup();
                _applicationIsQuitting = true;
            }
        }

        protected virtual void OnAwake() { }

        protected virtual void OnCleanup() { }
    }
}
