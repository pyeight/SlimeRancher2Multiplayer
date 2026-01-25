using System.Reflection;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Client.Managers;

public sealed class ClientPacketManager
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
                if (Activator.CreateInstance(type, client, playerManager) is IClientPacketHandler handler)
                {
                    handlers[attribute.PacketType] = handler;
                    SrLogger.LogMessage($"Registered client handler: {type.Name} for packet type {attribute.PacketType}", SrLogTarget.Both);
                }
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Failed to register client handler {type.Name}: {ex}", SrLogTarget.Both);
            }
        }

        SrLogger.LogMessage($"Total client packet handlers registered: {handlers.Count}", SrLogTarget.Both);
    }

    public void HandlePacket(byte[] data)
    {
        if (data.Length < 7)
        {
            SrLogger.LogMessage($"Received packet too small for chunk header: {data.Length} bytes", SrLogTarget.Both);
            return;
        }

        byte packetType = data[0];
        ushort chunkIndex = (ushort)(data[1] | (data[2] << 8));
        ushort totalChunks = (ushort)(data[3] | (data[4] << 8));
        ushort packetId = (ushort)(data[5] | (data[6] << 8));
        
        byte[] chunkData = new byte[data.Length - 7];
        Buffer.BlockCopy(data, 7, chunkData, 0, data.Length - 7);
        
        // Client uses "server" as sender key
        string senderKey = "server";
        
        if (!PacketChunkManager.TryMergePacket((PacketType)packetType, chunkData, chunkIndex, 
            totalChunks, packetId, senderKey, out data))
            return;

        if (handlers.TryGetValue(packetType, out var handler))
        {
            try
            {
                MainThreadDispatcher.Enqueue(() => handler.Handle(data));
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Error handling packet type {packetType}: {ex}", SrLogTarget.Both);
            }
        }
        else
        {
            SrLogger.LogError($"No client handler found for packet type: {packetType}", SrLogTarget.Both);
        }
    }
}