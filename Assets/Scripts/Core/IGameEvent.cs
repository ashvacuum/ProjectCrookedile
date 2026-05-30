namespace Crookedile.Core
{
    /// <summary>
    /// Marker interface for all EventBus events. Every event struct must implement this interface.
    ///
    /// Usage pattern:
    /// <code>
    ///   // Define an event (in BattleEvents.cs or a similar file):
    ///   public struct MyEvent : IGameEvent { public int Value; }
    ///
    ///   // Subscribe (call in OnEnable or constructor):
    ///   EventBus.Subscribe&lt;MyEvent&gt;(OnMyEvent);
    ///
    ///   // Publish:
    ///   EventBus.Publish(new MyEvent { Value = 42 });
    ///
    ///   // Unsubscribe (call in OnDisable or cleanup):
    ///   EventBus.Unsubscribe&lt;MyEvent&gt;(OnMyEvent);
    /// </code>
    ///
    /// See <c>Assets/Scripts/Gameplay/Battle/BattleEvents.cs</c> for the full list of
    /// battle-system events with publisher and subscriber documentation.
    /// </summary>
    public interface IGameEvent { }
}
