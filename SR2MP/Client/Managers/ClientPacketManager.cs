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

    public void HandlePacket(byte[] data, IPEndPoint serverEp)
    {
        if (data.Length < 10)
        {
            SrLogger.LogMessage($"Received packet too small for chunk header: {data.Length} bytes", SrLogTarget.Both);
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

        // Client uses "server" as sender key
        const string senderKey = "server";

        if (!PacketChunkManager.TryMergePacket((PacketType)packetType, chunkData, chunkIndex,
            totalChunks, packetId, senderKey, reliability, sequenceNumber,
            out var reader, out var packetReliability, out var packetSequenceNumber))
        {
            return;
        }

        // Handle reliability ACK packets
        if (packetType == 254)
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
            SendAck(packetId, packetType);
        }

        var packetTypeKey = ((PacketType)packetType).ToString();
        var uniqueId = packetId.ToString();

        if (PacketDeduplication.IsDuplicate(packetTypeKey, uniqueId))
        {
            SrLogger.LogPacketSize($"Duplicate packet ignored: {packetTypeKey} (packetId={packetId})", SrLogTarget.Both);
            return;
        }

        if (packetReliability == PacketReliability.ReliableOrdered &&
            !client.ShouldProcessOrderedPacket(serverEp, packetSequenceNumber, packetType))
        {
            return;
        }

        if (handlers.TryGetValue(packetType, out var handler))
        {
            try
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    try
                    {
                        handler.Handle(reader);
                    }
                    catch (Exception ex)
                    {
                        SrLogger.LogError($"Error in handler for packet type {packetType}: {ex}", SrLogTarget.Both);
                    }
                    finally
                    {
                        PacketBufferPool.Return(reader);
                    }
                });
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