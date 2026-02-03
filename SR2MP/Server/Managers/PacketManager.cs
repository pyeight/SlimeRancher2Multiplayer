using System.Net;
using System.Reflection;
using SR2MP.Packets.Utils;
using SR2MP.Packets.Internal;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Server.Managers;

public sealed class PacketManager
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
                if (Activator.CreateInstance(type, networkManager, clientManager) is IPacketHandler handler)
                {
                    handlers[attribute.PacketType] = handler;
                    SrLogger.LogMessage($"Registered server handler: {type.Name} for packet type {attribute.PacketType}", SrLogTarget.Both);
                }
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Failed to register handler {type.Name}: {ex}", SrLogTarget.Both);
            }
        }

        SrLogger.LogMessage($"Total handlers registered: {handlers.Count}", SrLogTarget.Both);
    }

    public void HandlePacket(byte[] data, IPEndPoint clientEp)
    {
        if (data.Length < 10)
        {
            SrLogger.LogWarning($"Received packet too small for chunk header: {data.Length} bytes", SrLogTarget.Both);
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

        string senderKey = clientEp.ToString();

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

            networkManager.HandleAck(clientEp, ackPacket.PacketId, ackPacket.OriginalPacketType);
            return;
        }

        // Always ACK reliable packets (even duplicates)
        // Otherwise clients will resend if the ACK packet was lost
        if (packetReliability != PacketReliability.Unreliable)
        {
            SendAck(clientEp, packetId, packetType);
        }

        // Packet deduplication (per client)
        var packetTypeKey = ((PacketType)packetType).ToString();
        var uniqueId = $"{senderKey}_{packetId}";

        if (PacketDeduplication.IsDuplicate(packetTypeKey, uniqueId))
        {
            SrLogger.LogPacketSize($"Duplicate packet ignored from {senderKey}: {packetTypeKey} (packetId={packetId})", SrLogTarget.Both);
            return;
        }

        // Ordered reliable packets must be processed in sequence
        if (packetReliability == PacketReliability.ReliableOrdered)
        {
            if (!networkManager.ShouldProcessOrderedPacket(clientEp, packetSequenceNumber, packetType))
                return;
        }

        if (handlers.TryGetValue(packetType, out var handler))
        {
            try
            {
                MainThreadDispatcher.Enqueue(() => handler.Handle(data, clientEp));
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Error handling packet type {packetType}: {ex}", SrLogTarget.Both);
            }
        }
        else
        {
            SrLogger.LogWarning($"No handler found for packet type: {packetType}");
        }
    }

    private void SendAck(IPEndPoint clientEp, ushort packetId, byte packetType)
    {
        if (!clientManager.TryGetClient(clientEp, out _))
            return;

        var ackPacket = new AckPacket
        {
            PacketId = packetId,
            OriginalPacketType = packetType
        };

        using var writer = new PacketWriter();
        writer.WritePacket(ackPacket);

        // no need to acknowledge ACK packets
        networkManager.Send(writer.ToArray(), clientEp, PacketReliability.Unreliable);
    }
}