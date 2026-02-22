using System.Collections.Concurrent;
using MelonLoader;
using SR2MP.Packets.Utils;

namespace SR2MP.Shared.Utils;

[RegisterTypeInIl2Cpp(false)]
public sealed class MainThreadDispatcher : MonoBehaviour
{
    public static MainThreadDispatcher Instance { get; private set; }

    // ReSharper disable once InconsistentNaming
    private static readonly ConcurrentQueue<ClientHandleCache> clientPacketQueue = new();
    
    // ReSharper disable once InconsistentNaming
    private static readonly ConcurrentQueue<ServerHandleCache> serverPacketQueue = new();

    public static void Initialize()
    {
        if (Instance != null) return;

        var obj = new GameObject("SR2MP_MainThreadDispatcher");
        Instance = obj.AddComponent<MainThreadDispatcher>();
        DontDestroyOnLoad(obj);

        SrLogger.LogMessage("Main thread dispatcher initialized", SrLogTarget.Both);
    }

#pragma warning disable CA1822 // Mark members as static
    public void Update()
#pragma warning restore CA1822 // Mark members as static
    {
        // Process client packets
        while (clientPacketQueue.TryDequeue(out var clientCache))
        {
            try
            {
                clientCache.Handler.Handle(clientCache.Reader);
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Error executing client packet handler: {ex}", SrLogTarget.Both);
            }
            finally
            {
                PacketBufferPool.Return(clientCache.Reader);
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
                SrLogger.LogError($"Error executing server packet handler: {ex}", SrLogTarget.Both);
            }
            finally
            {
                PacketBufferPool.Return(serverCache.Reader);
            }
        }
    }

    public static void Enqueue(in ClientHandleCache cache) => clientPacketQueue.Enqueue(cache);

    public static void Enqueue(in ServerHandleCache cache) => serverPacketQueue.Enqueue(cache);

#pragma warning disable CA1822 // Mark members as static
    public void OnDestroy() => Instance = null!;
#pragma warning restore CA1822 // Mark members as static
}