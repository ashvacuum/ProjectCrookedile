namespace Crookedile.UI
{
    /// <summary>
    /// A component that receives its context from the scene's composition root (UIRoot)
    /// instead of fetching it. This is the ONLY sanctioned way for UI to acquire game refs —
    /// no FindObjectOfType, no singleton grabs.
    /// </summary>
    /// <typeparam name="T">The scene context, e.g. BattleManager for the battle scene.</typeparam>
    public interface IBindable<in T>
    {
        void Bind(T context);
    }
}
