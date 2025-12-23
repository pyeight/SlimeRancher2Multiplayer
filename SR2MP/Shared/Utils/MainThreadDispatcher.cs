using UnityEngine;
using System.Collections.Concurrent;
using MelonLoader;

namespace SR2MP.Shared.Utils;

[RegisterTypeInIl2Cpp(false)]
public class MainThreadDispatcher : MonoBehaviour
{
    public static MainThreadDispatcher Instance { get; private set; }

    private static readonly ConcurrentQueue<Action> actionQueue = new();

    public static void Initialize()
    {
        if (Instance != null) return;

        var obj = new GameObject("SR2MP_MainThreadDispatcher");
        Instance = obj.AddComponent<MainThreadDispatcher>();
        DontDestroyOnLoad(obj);

        SrLogger.LogMessage("Main thread dispatcher initialized", SrLogger.LogTarget.Both);
    }

#pragma warning disable CA1822 // Mark members as static
    public void Update()
#pragma warning restore CA1822 // Mark members as static
    {
        while (actionQueue.TryDequeue(out var action))
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Error executing main thread action: {ex}", SrLogger.LogTarget.Both);
            }
        }
    }

    public static void Enqueue(Action action)
    {
        actionQueue.Enqueue(action);
    }

#pragma warning disable CA1822 // Mark members as static
    public void OnDestroy() => Instance = null!;
#pragma warning restore CA1822 // Mark members as static
}