using SR2MP.Packets.Utils;
using System.IO.Compression;

namespace SR2MP.Shared.Managers;

public static class PacketChunkManager
{
    private sealed class IncompletePacket
    {
        public byte[][] chunks;
        public bool[] received;
        public ushort totalChunks;
        public int receivedCount;
    }

    private static readonly Dictionary<PacketType, IncompletePacket> IncompletePackets = new();

    private const int MaxChunkBytes = 250;
    private const int CompressionThreshold = 50;

    internal static bool TryMergePacket(PacketType packetType, byte[] data, ushort chunkIndex, ushort totalChunks, out byte[] fullData)
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
        
        fullData = complete.ToArray();
        
        if (fullData.Length > 0 && fullData[0] == 0xFF)
        {
            fullData = Decompress(fullData);
            SrLogger.LogPacketSize($"Decompressed packet: type={packetType}");
        }
        
        SrLogger.LogPacketSize($"Merged full packet: type={packetType}");

        return true;
    }

    internal static byte[][] SplitPacket(byte[] data)
    {
        var originalSize = data.Length;
        var packetType = data[0];
        
        if (data.Length > CompressionThreshold)
        {
            var compressed = Compress(data);
            if (compressed.Length < data.Length * 0.9f)
            {
                data = compressed;
                SrLogger.LogPacketSize($"Compressed packet: {originalSize} -> {data.Length} bytes ({(1 - (float)data.Length / originalSize) * 100:F1}% reduction)");
            }
        }
        
        var chunkCount = (data.Length + MaxChunkBytes - 1) / MaxChunkBytes;
        var result = new byte[chunkCount][];

        for (ushort index = 0; index < chunkCount; index++)
        {
            var offset = index * MaxChunkBytes;
            var chunkSize = Math.Min(MaxChunkBytes, data.Length - offset);

            var buffer = new byte[5 + chunkSize];
            buffer[0] = packetType;
            
            buffer[1] = (byte)(index & 0xFF);
            buffer[2] = (byte)((index >> 8) & 0xFF);
            
            buffer[3] = (byte)(chunkCount & 0xFF);
            buffer[4] = (byte)((chunkCount >> 8) & 0xFF);

            Buffer.BlockCopy(data, offset, buffer, 5, chunkSize);
            result[index] = buffer;
        }
        return result;
    }

    private static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        
        output.WriteByte(0xFF);
        
        output.WriteByte(data[0]);
        
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest))
        {
            gzip.Write(data, 1, data.Length - 1);
        }
        
        return output.ToArray();
    }

    private static byte[] Decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        
        input.ReadByte();
        
        var packetType = (byte)input.ReadByte();
        
        using var output = new MemoryStream();
        
        output.WriteByte(packetType);
        
        using (var gzip = new GZipStream(input, CompressionMode.Decompress))
        {
            gzip.CopyTo(output);
        }
        
        return output.ToArray();
    }
}