using SR2MP.Packets.Utils;

namespace SR2MP.Shared.Managers;

public static class PacketChunkManager
{
    private sealed class IncompletePacket
    {
        public byte[][] chunks;
        public bool[] received;
        public byte totalChunks;
        public int receivedCount;
    }

    private static readonly Dictionary<PacketType, IncompletePacket> IncompletePackets = new();

    private const int MaxChunkBytes = 250;

    internal static bool TryMergePacket(PacketType packetType, byte[] data, byte chunkIndex, byte totalChunks, out byte[] fullData)
    {
        fullData = null!;
        
        if (chunkIndex >= totalChunks)
        {
            SrLogger.LogWarning($"Rejected packet: chunkIndex={chunkIndex} >= totalChunks={totalChunks}");
            return false;
        }

        if (!IncompletePackets.TryGetValue(packetType, out var packet))
        {
            packet = new IncompletePacket
            {
                chunks = new byte[totalChunks][],
                received = new bool[totalChunks],
                totalChunks = totalChunks,
                receivedCount = 0
            };
            IncompletePackets[packetType] = packet;
        }
        else
        {
            if (packet.totalChunks != totalChunks)
            {
                SrLogger.LogWarning($"Packet chunk mismatch: existing total: {packet.totalChunks} new total: {totalChunks}");
                IncompletePackets.Remove(packetType);
                return false;
            }
        }

        if (!packet.received[chunkIndex])
        {
            packet.chunks[chunkIndex] = data;
            packet.received[chunkIndex] = true;
            packet.receivedCount++;
        }

        SrLogger.LogPacketSize($"Received chunk {chunkIndex + 1}/{totalChunks} for type={packetType}");

        if (packet.receivedCount != totalChunks)
            return false;

        var complete = new List<byte>();
        for (int i = 0; i < totalChunks; i++)
        {
            complete.AddRange(packet.chunks[i]);
        }

        IncompletePackets.Remove(packetType);
        SrLogger.LogPacketSize($"Merged full packet: type={packetType}");

        fullData = complete.ToArray();
        return true;
    }

    internal static byte[][] SplitPacket(byte[] data)
    {
        var chunkCount = (data.Length + MaxChunkBytes - 1) / MaxChunkBytes;
        var packetType = data[0];
        var result = new byte[chunkCount][];

        for (byte index = 0; index < chunkCount; index++)
        {
            var offset = index * MaxChunkBytes;
            var chunkSize = Math.Min(MaxChunkBytes, data.Length - offset);

            var buffer = new byte[3 + chunkSize];
            buffer[0] = packetType;
            buffer[1] = index;
            buffer[2] = (byte)chunkCount;

            Buffer.BlockCopy(data, offset, buffer, 3, chunkSize);
            result[index] = buffer;
        }
        return result;
    }
}