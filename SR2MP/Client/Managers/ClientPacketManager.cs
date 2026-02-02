using System.Reflection;
using System.Net;
using SR2MP.Packets.Utils;
using SR2MP.Packets.Internal;
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

    public void HandlePacket(byte[] data, IPEndPoint serverEp)
    {
        if (data.Length < 10)
        {
            SrLogger.LogMessage($"Received packet too small for chunk header: {data.Length} bytes", SrLogTarget.Both);
            return;
        }

        byte packetType = data[0];
        ushort chunkIndex = (ushort)(data[1] | (data[2] << 8));
        ushort totalChunks = (ushort)(data[3] | (data[4] << 8));
        ushort packetId = (ushort)(data[5] | (data[6] << 8));
        PacketReliability reliability = (PacketReliability)data[7];
        ushort sequenceNumber = (ushort)(data[8] | (data[9] << 8));

        byte[] chunkData = new byte[data.Length - 10];
        Buffer.BlockCopy(data, 10, chunkData, 0, chunkData.Length);
        // Buffer.BlockCopy(data, 10, chunkData, 0, data.Length - 10);

        // Client uses "server" as sender key
        string senderKey = "server";
        
        if (!PacketChunkManager.TryMergePacket((PacketType)packetType, chunkData, chunkIndex, 
            totalChunks, packetId, senderKey, reliability, sequenceNumber,
            out data, out var packetReliability, out var packetSequenceNumber))
            return;

        // Handle reliability ACK packets
        if (packetType == 254)
        {
            var ackPacket = new AckPacket();
            using (var reader = new PacketReader(data))
            {
                reader.Skip(1);
                ackPacket.Deserialise(reader);
            }
            client.HandleAck(serverEp, ackPacket.PacketId, ackPacket.OriginalPacketType);
            return;
        }

        // Sends ACK for reliable packets
        if (packetReliability != PacketReliability.Unreliable)
        {
            SendAck(packetId, packetType);
        }
        
        string packetTypeKey = ((PacketType)packetType).ToString();
        string uniqueId = packetId.ToString();

        if (PacketDeduplication.IsDuplicate(packetTypeKey, uniqueId))
        {
            SrLogger.LogPacketSize($"Duplicate packet ignored: {packetTypeKey} (packetId={packetId})", SrLogTarget.Both);
            return;
        }
        
        if (packetReliability == PacketReliability.ReliableOrdered)
        {
            if (!client.ShouldProcessOrderedPacket(serverEp, packetSequenceNumber, packetType))
                return;
        }

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

    private void SendAck(ushort packetId, byte packetType)
    {
        if (!Main.Client.IsConnected) return;

        var ackPacket = new AckPacket
        {
            PacketId = packetId,
            OriginalPacketType = packetType
        };

        client.SendPacket(ackPacket);
    }
}