using System.Reflection;
using System.Net;
using SR2MP.Packets.Utils;
using SR2MP.Packets.Internal;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;
using System.Buffers;
using SR2MP.Shared;

namespace SR2MP.Client.Managers;

public sealed class ClientPacketManager
{
    private readonly Dictionary<byte, IClientPacketHandler> handlers = new();
    private readonly SR2MPClient client;

    public ClientPacketManager(SR2MPClient client)
    {
        this.client = client;
    }

    public void RegisterHandlers()
    {
        var handlerTypes = Main.Core.GetTypes()
            .Where(type => type.GetCustomAttribute<PacketHandlerAttribute>() != null
                        && typeof(IClientPacketHandler).IsAssignableFrom(type)
                        && !type.IsAbstract);

        foreach (var type in handlerTypes)
        {
            var attribute = type.GetCustomAttribute<PacketHandlerAttribute>();
            if (attribute == null || attribute.HandlerType == HandlerType.Server) continue;

            try
            {
                if (Activator.CreateInstance(type) is IClientPacketHandler handler)
                {
                    handlers[attribute.PacketType] = handler;
                    handler.IsServerSide = false;
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

    public void HandlePacket(byte[] data, int receivedBytes, IPEndPoint serverEp)
    {
        if (receivedBytes < 10)
        {
            SrLogger.LogMessage($"Received packet too small for chunk header: {receivedBytes} bytes", SrLogTarget.Both);
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
                packetId, serverEp, reliability, sequenceNumber,
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
                client.HandleAck(serverEp, ackPacket.PacketId, ackPacket.OriginalPacketType);
            }
            finally
            {
                PacketBufferPool.Return(reader);
            }

            return;
        }

        // Sends ACK for reliable packets
        if (packetReliability != PacketReliability.Unreliable)
        {
            SendAck(packetId, packetTypeHeader);
        }

        var packetKey = new PacketKey(packetTypeHeader, packetId, serverEp);

        if (PacketDeduplication.IsDuplicate(packetKey))
        {
            PacketBufferPool.Return(reader);
            SrLogger.LogPacketSize($"Duplicate packet ignored: {packetType} (packetId={packetId})", SrLogTarget.Both);
            return;
        }

        if (packetReliability == PacketReliability.ReliableOrdered &&
            !client.ShouldProcessOrderedPacket(serverEp, packetSequenceNumber, packetTypeHeader))
        {
            PacketBufferPool.Return(reader);
            return;
        }

        if (handlers.TryGetValue(packetTypeHeader, out var handler))
        {
            MainThreadDispatcher.Enqueue(new ClientHandleCache(reader, handler));
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