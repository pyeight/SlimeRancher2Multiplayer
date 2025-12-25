using System.Reflection;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Client.Managers;

public class ClientPacketManager
{
    private readonly Dictionary<byte, IClientPacketHandler> handlers = new();
    private readonly Client client;
    private readonly RemotePlayerManager playerManager;

    public ClientPacketManager(Client client, RemotePlayerManager playerManager)
    {
        this.client = client;
        this.playerManager = playerManager;
    }

    public void RegisterHandlers()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var handlerTypes = assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<PacketHandlerAttribute>() != null
                     && typeof(IClientPacketHandler).IsAssignableFrom(type)
                     && !type.IsAbstract);

        foreach (var type in handlerTypes)
        {
            var attribute = type.GetCustomAttribute<PacketHandlerAttribute>();
            if (attribute == null) continue;

            try
            {
                var handler = Activator.CreateInstance(type, client, playerManager) as IClientPacketHandler;

                if (handler != null)
                {
                    handlers[attribute.PacketType] = handler;
                    SrLogger.LogMessage($"Registered client handler: {type.Name} for packet type {attribute.PacketType}", SrLogger.LogTarget.Both);
                }
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Failed to register client handler {type.Name}: {ex}", SrLogger.LogTarget.Both);
            }
        }

        SrLogger.LogMessage($"Total client packet handlers registered: {handlers.Count}", SrLogger.LogTarget.Both);
    }

    public void HandlePacket(byte[] data)
    {
        if (data.Length < 1)
        {
            SrLogger.LogMessage("Received empty packet", SrLogger.LogTarget.Both);
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
                MainThreadDispatcher.Enqueue(() => handler.Handle(data));
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Error handling packet type {packetType}: {ex}", SrLogger.LogTarget.Both);
            }
        }
        else
        {
            SrLogger.LogError($"No client handler found for packet type: {packetType}", SrLogger.LogTarget.Both);
        }
    }
}