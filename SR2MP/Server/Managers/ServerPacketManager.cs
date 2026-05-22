using System.Buffers;
using System.Net;
using System.Reflection;
using HarmonyLib;
using SR2MP.Packets.Internal;
using SR2MP.Packets.Utils;
using SR2MP.Shared;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Server.Managers;

internal sealed class ServerPacketManager
{
    private readonly Dictionary<byte, IServerPacketHandler> Handlers = new();
    private readonly NetworkManager NetworkManager;
    private readonly ClientManager ClientManager;

    public ServerPacketManager(NetworkManager networkManager, ClientManager clientManager)
    {
        NetworkManager = networkManager;
        ClientManager = clientManager;
    }

    public void RegisterHandlers(Assembly assembly)
    {
        var handlerTypes = AccessTools.GetTypesFromAssembly(assembly)
            .Where(type => type.GetCustomAttribute<PacketHandlerAttribute>() != null
                        && !type.IsAbstract);

        foreach (var type in handlerTypes)
        {
            var attribute = type.GetCustomAttribute<PacketHandlerAttribute>();
            if (attribute == null || attribute.HandlerType == HandlerType.Client) continue;

            try
            {
                if (Activator.CreateInstance(type) is IServerPacketHandler handler)
                {
                    Handlers[attribute.PacketType] = handler;
                    handler.IsServerSide = true;
                    SrLogger.LogMessage($"Registered server handler: {type.Name} for packet type {attribute.PacketType}");
                }
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Failed to register handler {type.Name}: {ex}");
            }
        }

        SrLogger.LogMessage($"Total handlers registered: {Handlers.Count}");
    }

    public void HandlePacket(byte[] data, int receivedBytes, IPEndPoint clientEp)
    {
        if (receivedBytes < HeaderSize)
        {
            SrLogger.LogWarning($"Received packet too small for chunk header: {receivedBytes} bytes");
            return;
        }

        // 13 byte header:
        var packetTypeHeader = data[0];
        var chunkIndex       = (ushort)(data[1] | (data[2] << 8));
        var totalChunks      = (ushort)(data[3] | (data[4] << 8));
        var packetId         = (ushort)(data[5] | (data[6] << 8));
        var channel          = (NetworkChannel)data[7];
        var reliability      = (PacketReliability)data[8];
        var sequenceNumber   = (ushort)(data[9] | (data[10] << 8));
        var receivedCrc      = (ushort)(data[11] | (data[12] << 8));

        var trueChunkLength = receivedBytes - HeaderSize;

        var expectedCrc = PacketCRC.Compute(data, HeaderSize, trueChunkLength);

        if (receivedCrc != expectedCrc)
        {
            SrLogger.LogPacketAcknowledge(
                $"Corrupted packet dropped: type={packetTypeHeader}" +
                $"expected=0x{expectedCrc:X4} received=0x{receivedCrc:X4}");
            return;
        }

        var packetType = (PacketType)packetTypeHeader;
        var packetReliability = reliability;
        var packetChannel = channel;
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
                    packetId, clientEp, reliability, channel, sequenceNumber,
                    out reader, out packetReliability, out packetChannel, out packetSequenceNumber))
            {
                return;
            }
        }

        // Handle ACK packets
        if (packetTypeHeader == (byte)PacketType.ReservedAcknowledge)
        {
            try
            {
                var ackPacket = reader.ReadPacket<AckPacket>();
                NetworkManager.HandleAck(clientEp, ackPacket.PacketId, ackPacket.OriginalPacketType);
            }
            finally
            {
                PacketReader.Return(reader);
            }

            return;
        }

        if (packetReliability.HasFlag(PacketReliability.Reliable))
            SendAck(clientEp, packetId, packetTypeHeader);

        // Packet deduplication (per client)
        var packetKey = new PacketKey(packetTypeHeader, packetId, clientEp);

        if (PacketDeduplication.IsDuplicate(packetKey))
        {
            PacketReader.Return(reader);
            SrLogger.LogPacketSize($"Duplicate packet ignored from {clientEp}: {packetType} (packetId={packetId})");
            return;
        }

        if (!packetReliability.HasFlag(PacketReliability.Ordered))
        {
            DispatchAction();
            return;
        }

        if (NetworkManager.ShouldProcessOrderedPacket(clientEp, packetSequenceNumber, packetTypeHeader, packetChannel, packetReliability, DispatchAction))
        {
            DispatchAction();
            return;
        }

        if (packetReliability == PacketReliability.Ordered)
            PacketReader.Return(reader);

        void DispatchAction()
        {
            if (Handlers.TryGetValue(packetTypeHeader, out var handler))
            {
                MainThreadDispatcher.Instance.Enqueue(new ServerHandleCache(reader, handler, clientEp));
            }
            else
            {
                SrLogger.LogError($"No handler found for packet type: {packetType}");
                PacketReader.Return(reader);
            }
        }
    }

    private void SendAck(IPEndPoint clientEp, ushort packetId, byte packetType)
    {
        if (!ClientManager.TryGetClient(clientEp, out _))
            return;

        var ackPacket = new AckPacket
        {
            PacketId = packetId,
            OriginalPacketType = packetType
        };

        using var writer = PacketWriter.Borrow();
        writer.WritePacket(ackPacket);

        try
        {
            NetworkManager.Send(writer.ToSpan(), clientEp, PacketReliability.Unreliable, NetworkChannel.Acknowledge);
        }
        catch
        {
            // ignored
        }
    }
}