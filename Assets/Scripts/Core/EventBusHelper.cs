using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// MonoBehaviour helper for EventBus to handle coroutines and frame-based operations
/// </summary>
public class EventBusHelper : MonoBehaviour
{
    private static EventBusHelper instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        if (instance == null)
        {
            GameObject helperObject = new GameObject("EventBusHelper");
            instance = helperObject.AddComponent<EventBusHelper>();
            DontDestroyOnLoad(helperObject);
            helperObject.hideFlags = HideFlags.HideInHierarchy;
        }
    }

    public static void ScheduleNextFrameCallback(Action callback)
    {
        if (instance != null)
        {
            instance.StartCoroutine(instance.ExecuteNextFrame(callback));
        }
    }

    private IEnumerator ExecuteNextFrame(Action callback)
    {
        yield return null; // Wait one frame
        try
        {
            callback?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in EventBus next-frame callback: {e.Message}");
            Debug.LogException(e);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}