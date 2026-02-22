using System.Net;
using System.Reflection;
using SR2MP.Packets.Utils;
using SR2MP.Packets.Internal;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;
using System.Buffers;
using SR2MP.Shared;

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

    public void HandlePacket(byte[] data, int receivedBytes, IPEndPoint clientEp)
    {
        if (receivedBytes < 10)
        {
            SrLogger.LogWarning($"Received packet too small for chunk header: {receivedBytes} bytes", SrLogTarget.Both);
            return;
        }

        var packetTypeHeader = data[0];
        var chunkIndex = (ushort)(data[1] | (data[2] << 8));
        var totalChunks = (ushort)(data[3] | (data[4] << 8));
        var packetId = (ushort)(data[5] | (data[6] << 8));
        var reliability = (PacketReliability)data[7];
        var sequenceNumber = (ushort)(data[8] | (data[9] << 8));

        var trueChunkLength = receivedBytes - 10;

        var packetType = (PacketType)packetTypeHeader;

        var packetReliability = reliability;
        var packetSequenceNumber = sequenceNumber;

        PacketReader reader;

        if (totalChunks == 1)
        {
            if (trueChunkLength > 0 && data[10] == (byte)PacketType.ReservedCompression)
            {
                reader = PacketChunkManager.DecompressSingleChunk(data, 10, trueChunkLength);
            }
            else
            {
                var singleChunkData = ArrayPool<byte>.Shared.Rent(trueChunkLength);
                data.AsSpan(10, trueChunkLength).CopyTo(singleChunkData);
                reader = PacketBufferPool.GetReader(singleChunkData, trueChunkLength, true);
            }
        }
        else
        {
            var chunkData = ArrayPool<byte>.Shared.Rent(trueChunkLength);
            data.AsSpan(10, trueChunkLength).CopyTo(chunkData);

            if (!PacketChunkManager.TryMergePacket(packetType,
                chunkData, trueChunkLength, chunkIndex, totalChunks,
                packetId, clientEp, reliability, sequenceNumber,
                out reader, out packetReliability, out packetSequenceNumber))
            {
                PacketBufferPool.Return(reader);
                return;
            }
        }

        // Handle reliability ACK packets
        if (packetTypeHeader == 254)
        {
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
            SendAck(clientEp, packetId, packetTypeHeader);
        }

        // Packet deduplication (per client)
        var packetKey = new PacketKey(packetTypeHeader, packetId, clientEp);
        
        if (PacketDeduplication.IsDuplicate(packetKey))
        {
            PacketBufferPool.Return(reader);
            SrLogger.LogPacketSize($"Duplicate packet ignored from {clientEp}: {packetType} (packetId={packetId})", SrLogTarget.Both);
            return;
        }

        // Ordered reliable packets must be processed in sequence
        if (packetReliability == PacketReliability.ReliableOrdered &&
            !networkManager.ShouldProcessOrderedPacket(clientEp, packetSequenceNumber, packetTypeHeader))
        {
            PacketBufferPool.Return(reader);
            return;
        }

        if (handlers.TryGetValue(packetTypeHeader, out var handler))
        {
            MainThreadDispatcher.Enqueue(new ServerHandleCache(reader, handler, clientEp));
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
            networkManager.Send(writer.ToSpan(), clientEp, PacketReliability.Unreliable);
        }
        finally
        {
            PacketBufferPool.Return(writer);
        }
    }
}