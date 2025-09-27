using System;
using System.Collections.Generic;
using Core;
using Projects;
using UnityEngine;

public static class EventBus
{
    private static Dictionary<Type, List<IEventListener>> eventListeners = new Dictionary<Type, List<IEventListener>>();
    private static Queue<IGameEvent> eventQueue = new Queue<IGameEvent>();
    private static bool isProcessingEvents = false;
    private static bool isInitialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (!isInitialized)
        {
            eventListeners = new Dictionary<Type, List<IEventListener>>();
            eventQueue = new Queue<IGameEvent>();
            isProcessingEvents = false;
            isInitialized = true;

            // Subscribe to application events for cleanup
            Application.quitting += OnApplicationQuit;

            Debug.Log("Static EventBus initialized");
        }
    }

    public static void Subscribe<T>(IEventListener<T> listener) where T : IGameEvent
    {
        Type eventType = typeof(T);

        if (!eventListeners.ContainsKey(eventType))
        {
            eventListeners[eventType] = new List<IEventListener>();
        }

        if (!eventListeners[eventType].Contains(listener))
        {
            eventListeners[eventType].Add(listener);
            Debug.Log($"Subscribed {listener.GetType().Name} to {eventType.Name}");
        }
    }

    public static void Unsubscribe<T>(IEventListener<T> listener) where T : IGameEvent
    {
        Type eventType = typeof(T);

        if (eventListeners.ContainsKey(eventType))
        {
            eventListeners[eventType].Remove(listener);
            Debug.Log($"Unsubscribed {listener.GetType().Name} from {eventType.Name}");

            if (eventListeners[eventType].Count == 0)
            {
                eventListeners.Remove(eventType);
            }
        }
    }

    public static void Publish<T>(T gameEvent) where T : IGameEvent
    {
        if (gameEvent == null)
        {
            Debug.LogWarning("Attempted to publish null event");
            return;
        }

        eventQueue.Enqueue(gameEvent);

        if (!isProcessingEvents)
        {
            ProcessEvents();
        }
    }

    private static void ProcessEvents()
    {
        isProcessingEvents = true;
        int safetyCounter = 0;
        const int maxEventsPerFrame = 100;

        try
        {
            while (eventQueue.Count > 0 && safetyCounter < maxEventsPerFrame)
            {
                IGameEvent gameEvent = eventQueue.Dequeue();
                Type eventType = gameEvent.GetType();

                if (eventListeners.ContainsKey(eventType))
                {
                    // Create a copy of listeners to avoid modification during iteration
                    List<IEventListener> listeners = new List<IEventListener>(eventListeners[eventType]);

                    foreach (IEventListener listener in listeners)
                    {
                        try
                        {
                            // Use reflection to call HandleEvent with proper type
                            var handleMethod = listener.GetType().GetMethod("HandleEvent", new[] { eventType });
                            if (handleMethod != null)
                            {
                                handleMethod.Invoke(listener, new object[] { gameEvent });
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Error in event listener {listener.GetType().Name} handling {eventType.Name}: {e.Message}");
                            Debug.LogException(e);
                            // Continue processing other listeners even if one fails
                        }
                    }
                }

                safetyCounter++;
            }

            if (safetyCounter >= maxEventsPerFrame)
            {
                Debug.LogWarning($"EventBus safety limit reached. {eventQueue.Count} events remaining in queue.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Critical error in EventBus.ProcessEvents: {e.Message}");
            Debug.LogException(e);
        }
        finally
        {
            isProcessingEvents = false;
        }

        // If there are still events in queue, schedule processing for next frame
        if (eventQueue.Count > 0)
        {
            ScheduleNextFrameProcessing();
        }
    }

    private static void ScheduleNextFrameProcessing()
    {
        // Use Unity's EditorApplication.delayCall for next-frame execution
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += ProcessRemainingEvents;
        #else
        // In builds, use a MonoBehaviour helper for next-frame processing
        EventBusHelper.ScheduleNextFrameCallback(ProcessRemainingEvents);
        #endif
    }

    private static void ProcessRemainingEvents()
    {
        if (eventQueue.Count > 0 && !isProcessingEvents)
        {
            ProcessEvents();
        }
    }

    public static void ClearQueue()
    {
        eventQueue.Clear();
        Debug.Log("EventBus queue cleared");
    }

    public static void ClearAllSubscriptions()
    {
        eventListeners.Clear();
        Debug.Log("All EventBus subscriptions cleared");
    }

    public static int GetQueueSize()
    {
        return eventQueue.Count;
    }

    public static int GetListenerCount(Type eventType)
    {
        return eventListeners.ContainsKey(eventType) ? eventListeners[eventType].Count : 0;
    }

    private static void OnApplicationQuit()
    {
        ClearQueue();
        ClearAllSubscriptions();
        isInitialized = false;
        Debug.Log("EventBus cleaned up on application quit");
    }
}

// Base interface for all game events
public interface IGameEvent
{
    string EventName { get; }
    float Timestamp { get; }
}

// Base interface for event listeners
public interface IEventListener
{
}

// Generic interface for typed event listeners
public interface IEventListener<T> : IEventListener where T : IGameEvent
{
    void HandleEvent(T gameEvent);
}

// Base class for game events
public abstract class GameEvent : IGameEvent
{
    public string EventName { get; protected set; }
    public float Timestamp { get; protected set; }

    protected GameEvent(string eventName)
    {
        EventName = eventName;
        Timestamp = Time.time;
    }
}

// Common game events
public class ResourceChangedEvent : GameEvent
{
    public string ResourceType { get; private set; }
    public int OldValue { get; private set; }
    public int NewValue { get; private set; }
    public int Change { get; private set; }

    public ResourceChangedEvent(string resourceType, int oldValue, int newValue)
        : base("ResourceChanged")
    {
        ResourceType = resourceType;
        OldValue = oldValue;
        NewValue = newValue;
        Change = newValue - oldValue;
    }
}

public class CardPlayedEvent : GameEvent
{
    public Card PlayedCard { get; private set; }

    public CardPlayedEvent(Card card) : base("CardPlayed")
    {
        PlayedCard = card;
    }
}

public class TurnChangedEvent : GameEvent
{
    public int OldTurn { get; private set; }
    public int NewTurn { get; private set; }
    public Season CurrentSeason { get; private set; }

    public TurnChangedEvent(int oldTurn, int newTurn, Season season) : base("TurnChanged")
    {
        OldTurn = oldTurn;
        NewTurn = newTurn;
        CurrentSeason = season;
    }
}

public class ProjectCompletedEvent : GameEvent
{
    public Project CompletedProject { get; private set; }

    public ProjectCompletedEvent(Project project) : base("ProjectCompleted")
    {
        CompletedProject = project;
    }
}

public class RelationshipChangedEvent : GameEvent
{
    public Character Character { get; private set; }
    public int OldRapport { get; private set; }
    public int NewRapport { get; private set; }
    public RelationshipStatus OldStatus { get; private set; }
    public RelationshipStatus NewStatus { get; private set; }

    public RelationshipChangedEvent(Character character, int oldRapport, int newRapport,
        RelationshipStatus oldStatus, RelationshipStatus newStatus) : base("RelationshipChanged")
    {
        Character = character;
        OldRapport = oldRapport;
        NewRapport = newRapport;
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }
}