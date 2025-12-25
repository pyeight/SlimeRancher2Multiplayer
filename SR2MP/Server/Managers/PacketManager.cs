using System.Net;
using System.Reflection;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Server.Managers;

public class PacketManager
{
    private readonly Dictionary<byte, IPacketHandler> handlers = new();
    private readonly NetworkManager networkManager;
    private readonly ClientManager clientManager;

    public PacketManager(NetworkManager networkManager, ClientManager clientManager)
    {
        this.networkManager = networkManager;
        this.clientManager = clientManager;
    }

    public void RegisterHandlers()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var handlerTypes = assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<PacketHandlerAttribute>() != null
                           && typeof(IPacketHandler).IsAssignableFrom(type)
                           && !type.IsAbstract);

        foreach (var type in handlerTypes)
        {
            var attribute = type.GetCustomAttribute<PacketHandlerAttribute>();
            if (attribute == null) continue;

            try
            {
                var handler = Activator.CreateInstance(type, networkManager, clientManager) as IPacketHandler;

                if (handler != null)
                {
                    handlers[attribute.PacketType] = handler;
                    SrLogger.LogMessage($"Registered handler: {type.Name} for packet type {attribute.PacketType}", SrLogger.LogTarget.Both);
                }
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Failed to register handler {type.Name}: {ex}", SrLogger.LogTarget.Both);
            }
        }

        SrLogger.LogMessage($"Total handlers registered: {handlers.Count}", SrLogger.LogTarget.Both);
    }

    public void HandlePacket(byte[] data, IPEndPoint clientEP)
    {
        if (data.Length < 1)
        {
            SrLogger.LogWarning("Received empty packet", SrLogger.LogTarget.Both);
            return;
        }

        byte packetType = data[0];
        byte chunkIndex = data[1];
        byte totalChunks = data[2];
        
        byte[] chunkData = new byte[data.Length - 3];
        Buffer.BlockCopy(data, 3, chunkData, 0, data.Length - 3);
        if (!PacketChunkManager.TryMergePacket((PacketType)packetType, chunkData, chunkIndex, totalChunks, out data))
            return;
        
        if (handlers.TryGetValue(packetType, out var handler))
        {
            try
            {
                MainThreadDispatcher.Enqueue(() => handler.Handle(data, clientEP));
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Error handling packet type {packetType}: {ex}", SrLogger.LogTarget.Both);
            }
        }
        else
        {
            SrLogger.LogWarning($"No handler found for packet type: {packetType}");
        }
    }
}