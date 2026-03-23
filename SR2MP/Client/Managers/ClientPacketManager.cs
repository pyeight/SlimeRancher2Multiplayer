using System.Reflection;
using System.Net;
using SR2MP.Packets.Utils;
using SR2MP.Packets.Internal;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;
using System.Buffers;
using SR2MP.Shared;
using HarmonyLib;

namespace SR2MP.Client.Managers;

public sealed class ClientPacketManager
{
    private readonly Dictionary<byte, IClientPacketHandler> handlers = new();
    private readonly SR2MPClient client;

    internal ClientPacketManager(SR2MPClient client)
    {
        this.client = client;
    }

    public void RegisterHandlers(Assembly assembly)
    {
        var handlerTypes = AccessTools.GetTypesFromAssembly(assembly)
            .Where(type => type.GetCustomAttribute<PacketHandlerAttribute>() != null
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
                    SrLogger.LogMessage($"Registered client handler: {type.Name} for packet type {attribute.PacketType}");
                }
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Failed to register client handler {type.Name}: {ex}");
            }
        }

        SrLogger.LogMessage($"Total client packet handlers registered: {handlers.Count}");
    }

    internal void HandlePacket(byte[] data, int receivedBytes, IPEndPoint serverEp)
    {
        // Header is now 12 bytes: 10 original + 2 CRC
        if (receivedBytes < HeaderSize)
        {
            SrLogger.LogMessage($"Received packet too small for chunk header: {receivedBytes} bytes");
            return;
        }

        var packetTypeHeader = data[0];
        var chunkIndex       = (ushort)(data[1] | (data[2] << 8));
        var totalChunks      = (ushort)(data[3] | (data[4] << 8));
        var packetId         = (ushort)(data[5] | (data[6] << 8));
        var reliability      = (PacketReliability)data[7];
        var sequenceNumber   = (ushort)(data[8] | (data[9] << 8));
        var receivedCrc      = (ushort)(data[10] | (data[11] << 8));

        var trueChunkLength = receivedBytes - HeaderSize;

        var expectedCrc = PacketCRC.Compute(data, HeaderSize, trueChunkLength);
        if (receivedCrc != expectedCrc)
        {
            SrLogger.LogPacketAcknowledge(
                $"Corrupted packet dropped: type={packetTypeHeader}" +
                $"expected=0x{expectedCrc:X4} received=0x{receivedCrc:X4}",
                SrLogTarget.Both);
            return;
        }

        var packetType = (PacketType)packetTypeHeader;

        var packetReliability = reliability;
        var packetSequenceNumber = sequenceNumber;

        PacketReader reader;

        if (totalChunks == 1)
        {
            if (trueChunkLength > 0 && data[HeaderSize] == (byte)PacketType.ReservedCompression)
            {
                reader = PacketChunkManager.DecompressSingleChunk(data, HeaderSize, trueChunkLength);
            }
            else
            {
                var singleChunkData = ArrayPool<byte>.Shared.Rent(trueChunkLength);
                data.AsSpan(HeaderSize, trueChunkLength).CopyTo(singleChunkData);
                reader = PacketReader.Borrow(singleChunkData, trueChunkLength, true);
            }
        }
        else
        {
            var chunkData = ArrayPool<byte>.Shared.Rent(trueChunkLength);
            data.AsSpan(HeaderSize, trueChunkLength).CopyTo(chunkData);

            if (!PacketChunkManager.TryMergePacket(packetType,
                chunkData, trueChunkLength, chunkIndex, totalChunks,
                packetId, serverEp, reliability, sequenceNumber,
                out reader, out packetReliability, out packetSequenceNumber))
            {
                PacketReader.Return(reader);
                return;
            }
        }

        // Handle reliability ACK packets
        if (packetTypeHeader == (byte)PacketType.ReservedAcknowledge)
        {
            try
            {
                var ackPacket = reader.ReadPacket<AckPacket>();
                client.HandleAck(serverEp, ackPacket.PacketId, ackPacket.OriginalPacketType);
            }
            finally
            {
                PacketReader.Return(reader);
            }

            return;
        }

        // Sends ACK for reliable packets
        if (packetReliability != PacketReliability.Unreliable)
        {
            if (!Main.Client.IsConnected) return;
            SendAck(packetId, packetTypeHeader);
        }

        var packetKey = new PacketKey(packetTypeHeader, packetId, serverEp);

        if (PacketDeduplication.IsDuplicate(packetKey))
        {
            PacketReader.Return(reader);
            SrLogger.LogPacketSize($"Duplicate packet ignored: {packetType} (packetId={packetId})");
            return;
        }

        if (packetReliability == PacketReliability.ReliableOrdered &&
            !client.ShouldProcessOrderedPacket(serverEp, packetSequenceNumber, packetTypeHeader))
        {
            PacketReader.Return(reader);
            return;
        }

        if (handlers.TryGetValue(packetTypeHeader, out var handler))
        {
            MainThreadDispatcher.Instance.Enqueue(new ClientHandleCache(reader, handler));
        }
        else
        {
            SrLogger.LogError($"No client handler found for packet type: {packetType}");
        }
    }

    private void SendAck(ushort packetId, byte packetType)
    {
        if (!Main.Client.IsConnected) return;

        var ackPacket = new AckPacket()
        {
            PacketId = packetId,
            OriginalPacketType = packetType
        };

        client.SendPacket(ackPacket);
    }
}