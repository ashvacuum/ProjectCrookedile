using UnityEngine;

namespace Crookedile.Core
{
    /// <summary>
    /// One instance per loaded scene, destroyed with it. For managers whose serialized references
    /// point into their scene (canvases, pile anchors, pooled UI), which a cross-scene
    /// <see cref="Singleton{T}"/> would keep pointing at the destroyed copy of. Never auto-created:
    /// <see cref="Instance"/> is null outside the scene that holds one, so callers use <c>?.</c>.
    /// </summary>
    public class SceneSingleton<T> : MonoBehaviour
        where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<T>();
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this as T;
            OnAwake();
        }

        private void OnDestroy()
        {
            if (_instance != this)
                return;
            OnCleanup();
            _instance = null;
        }

        protected virtual void OnAwake() { }

        protected virtual void OnCleanup() { }
    }
}
