using System.Collections.Generic;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.UI
{
    /// <summary>
    /// Per-scene composition root — the ONLY place that knows both the game side and the UI
    /// side. On start it finds every <see cref="IBindable{T}"/> under itself (inactive
    /// included) and injects the scene context exactly once. Components receive; they never
    /// fetch.
    ///
    /// Derive per scene: <c>BattleUIRoot : UIRoot&lt;BattleManager&gt;</c>. The metagame scene
    /// gets its own root bound to its own context type.
    /// </summary>
    public abstract class UIRoot<T> : MonoBehaviour
        where T : class
    {
        #region Fields

        [Tooltip("The scene's navigation owner (screen + popup stacks).")]
        [SerializeField]
        private UIRouter _router;

        #endregion

        #region Properties

        /// <summary>The scene's navigation owner.</summary>
        public UIRouter Router => _router;

        /// <summary>The scene context injected into every IBindable child.</summary>
        protected abstract T Context { get; }

        #endregion

        #region Lifecycle

        protected virtual void Awake()
        {
            BindChildren();
        }

        /// <summary>
        /// Injects the context into every IBindable child, active or not. Idempotent —
        /// safe to re-run if views are spawned late (call after instantiating).
        /// </summary>
        public void BindChildren()
        {
            T context = Context;
            if (context == null)
            {
                GameLogger.LogError<UIRouter>(
                    $"{name}: UIRoot has no context to bind — assign it in the Inspector"
                );
                return;
            }

            var bindables = GetComponentsInChildren<IBindable<T>>(true);
            foreach (IBindable<T> bindable in bindables)
                bindable.Bind(context);

            GameLogger.LogInfo<UIRouter>($"{name}: bound {bindables.Length} view(s)");
        }

        #endregion
    }
}
