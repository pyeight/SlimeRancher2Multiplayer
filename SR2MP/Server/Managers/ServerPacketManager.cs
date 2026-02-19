using System.Net;
using System.Reflection;
using SR2MP.Packets.Utils;
using SR2MP.Packets.Internal;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Server.Managers;

public sealed class ServerPacketManager
{
    private readonly Dictionary<byte, IServerPacketHandler> handlers = new();
    private readonly NetworkManager networkManager;
    private readonly ClientManager clientManager;

    public ServerPacketManager(NetworkManager networkManager, ClientManager clientManager)
    {
        this.networkManager = networkManager;
        this.clientManager = clientManager;
    }

    public void RegisterHandlers()
    {
        var handlerTypes = Main.Core.GetTypes()
            .Where(type => type.GetCustomAttribute<PacketHandlerAttribute>() != null
                        && typeof(IServerPacketHandler).IsAssignableFrom(type)
                        && !type.IsAbstract);

        foreach (var type in handlerTypes)
        {
            var attribute = type.GetCustomAttribute<PacketHandlerAttribute>();
            if (attribute == null || attribute.HandlerType == HandlerType.Client) continue;

            try
            {
                if (Activator.CreateInstance(type) is IServerPacketHandler handler)
                {
                    handlers[attribute.PacketType] = handler;
                    handler.IsServerSide = true;
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

        var packetType = data[0];
        var chunkIndex = (ushort)(data[1] | (data[2] << 8));
        var totalChunks = (ushort)(data[3] | (data[4] << 8));
        var packetId = (ushort)(data[5] | (data[6] << 8));
        var reliability = (PacketReliability)data[7];
        var sequenceNumber = (ushort)(data[8] | (data[9] << 8));

        var chunkData = new byte[data.Length - 10];
        data.AsSpan(10, chunkData.Length).CopyTo(chunkData);
        // Buffer.BlockCopy(data, 10, chunkData, 0, chunkData.Length);
        // Buffer.BlockCopy(data, 10, chunkData, 0, data.Length - 10);

        var senderKey = clientEp.ToString();

        if (!PacketChunkManager.TryMergePacket((PacketType)packetType, chunkData, chunkIndex,
            totalChunks, packetId, senderKey, reliability, sequenceNumber,
            out data, out var packetReliability, out var packetSequenceNumber))
        {
            return;
        }

        // Handle reliability ACK packets
        if (packetType == 254)
        {
            var reader = PacketBufferPool.GetReader(data);

            try
            {
                var ackPacket = reader.ReadPacket<AckPacket>();
                networkManager.HandleAck(clientEp, ackPacket.PacketId, ackPacket.OriginalPacketType);
            }
            finally
            {
                PacketBufferPool.Return(reader);
            }

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

        var writer = PacketBufferPool.GetWriter();
        try
        {
            writer.WritePacket(ackPacket);

            // no need to acknowledge ACK packets
            var data = writer.ToSpan();
            networkManager.Send(data, clientEp, PacketReliability.Unreliable);
        }
        finally
        {
            PacketBufferPool.Return(writer);
        }
    }
}