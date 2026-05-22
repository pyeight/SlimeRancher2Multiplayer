using System.Collections.Concurrent;
using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using SR2MP.Packets.Utils;
using Starlight.Storage;

namespace SR2MP.Shared.Utils;

[InjectIntoIL]
internal sealed class MainThreadDispatcher : MonoBehaviour
{
    public static MainThreadDispatcher Instance { get; private set; }

    // ReSharper disable once InconsistentNaming
    private readonly ConcurrentQueue<Action> actionQueue = new();

    // ReSharper disable once InconsistentNaming
    private readonly ConcurrentQueue<ClientHandleCache> clientPacketQueue = new();

    // ReSharper disable once InconsistentNaming
    private readonly ConcurrentQueue<ServerHandleCache> serverPacketQueue = new();

    public static void Initialize()
    {
        if (Instance) return;

        var obj = new GameObject("SR2MP_MainThreadDispatcher");
        Instance = obj.AddComponent<MainThreadDispatcher>();
        DontDestroyOnLoad(obj);

        SrLogger.LogMessage("Main thread dispatcher initialized");
    }

    public void Update()
    {
        // Process general actions
        while (actionQueue.TryDequeue(out var action))
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Error executing main thread action: {ex}");
            }
        }

        // Process client packets
        while (clientPacketQueue.TryDequeue(out var clientCache))
        {
            try
            {
                clientCache.Handler.Handle(clientCache.Reader);
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Error executing client packet handler: {ex}");
            }
            finally
            {
                PacketReader.Return(clientCache.Reader);
            }
        }

        // Process server packets
        while (serverPacketQueue.TryDequeue(out var serverCache))
        {
            try
            {
                serverCache.Handler.Handle(serverCache.Reader, serverCache.ClientEp);
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Error executing server packet handler: {ex}");
            }
            finally
            {
                PacketReader.Return(serverCache.Reader);
            }
        }
    }

    [HideFromIl2Cpp]
    public void Enqueue(Action action) => actionQueue.Enqueue(action);

    [HideFromIl2Cpp]
    public void Enqueue(in ClientHandleCache cache) => clientPacketQueue.Enqueue(cache);

    [HideFromIl2Cpp]
    public void Enqueue(in ServerHandleCache cache) => serverPacketQueue.Enqueue(cache);

    public void OnDestroy()
    {
        Instance = null!;

        while (clientPacketQueue.TryDequeue(out var c))
            PacketReader.Return(c.Reader);

        while (serverPacketQueue.TryDequeue(out var s))
            PacketReader.Return(s.Reader);
    }
}