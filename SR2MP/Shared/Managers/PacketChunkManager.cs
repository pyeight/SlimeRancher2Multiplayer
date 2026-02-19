using SR2MP.Packets.Utils;
using System.Collections.Concurrent;
using LZ4ps;
using System.Buffers;

namespace SR2MP.Shared.Managers;

public static class PacketChunkManager
{
    private sealed class IncompletePacket
    {
        public byte[][] chunks;
        public bool[] received;
        public ushort totalChunks;
        public int receivedCount;
        public DateTime lastChunkTime;
        public PacketReliability reliability;
        public ushort sequenceNumber;

        public IncompletePacket(ushort totalChunks, PacketReliability reliability, ushort sequenceNumber)
        {
            this.chunks = new byte[totalChunks][];
            this.received = new bool[totalChunks];
            this.totalChunks = totalChunks;
            this.receivedCount = 0;
            this.lastChunkTime = DateTime.UtcNow;
            this.reliability = reliability;
            this.sequenceNumber = sequenceNumber;
        }
    }

    // Format: PacketType_PacketId_Sender
    private static readonly ConcurrentDictionary<string, IncompletePacket> IncompletePackets = new();

    private static int nextPacketId = 1;

    private const int MaxChunkBytes = 500;
    private const int CompressionThreshold = 30;
    private static readonly TimeSpan PacketTimeout = TimeSpan.FromSeconds(30);

    private static int packetCounter;
    private const int CleanupInterval = 100;

    private const byte All8Bits = byte.MaxValue;

    internal static bool TryMergePacket(PacketType packetType, byte[] data, ushort chunkIndex,
        ushort totalChunks, ushort packetId, string senderKey, PacketReliability reliability,
        ushort sequenceNumber, out PacketReader reader, out PacketReliability outReliability,
        out ushort outSequenceNumber)
    {
        reader = null!;
        outReliability = reliability;
        outSequenceNumber = sequenceNumber;

        if (chunkIndex >= totalChunks)
        {
            SrLogger.LogWarning($"Invalid chunk: index={chunkIndex} >= total={totalChunks}", SrLogTarget.Both);
            return false;
        }

        // Cleanup every 100 packets
        if (++packetCounter >= CleanupInterval)
        {
            packetCounter = 0;
            CleanupStalePackets();
        }

        var key = $"{(byte)packetType}_{packetId}_{senderKey}";

        var packet = IncompletePackets.GetOrAdd(key, _ =>
            new IncompletePacket(totalChunks, reliability, sequenceNumber));

        if (packet.totalChunks != totalChunks)
        {
            SrLogger.LogWarning($"Chunk count mismatch for {key}: expected={packet.totalChunks} got={totalChunks}", SrLogTarget.Both);
            IncompletePackets.TryRemove(key, out _);
            return false;
        }

        // Store chunks
        if (!packet.received[chunkIndex])
        {
            packet.chunks[chunkIndex] = data;
            packet.received[chunkIndex] = true;
            packet.receivedCount++;
            packet.lastChunkTime = DateTime.UtcNow;
        }

        // Wait for all chunks
        if (packet.receivedCount != totalChunks)
            return false;

        // Merge chunks
        var totalSize = 0;
        for (var i = 0; i < totalChunks; i++)
            totalSize += packet.chunks[i].Length;

        var assemblyBuffer = ArrayPool<byte>.Shared.Rent(totalSize);
        var offset = 0;

        for (var i = 0; i < totalChunks; i++)
        {
            packet.chunks[i].AsSpan().CopyTo(assemblyBuffer.AsSpan(offset));
            // Buffer.BlockCopy(packet.chunks[i], 0, fullData, offset, packet.chunks[i].Length);
            offset += packet.chunks[i].Length;
        }

        outReliability = packet.reliability;
        outSequenceNumber = packet.sequenceNumber;

        IncompletePackets.TryRemove(key, out _);

        // Decompress if compressed
        if (totalSize > 0 && assemblyBuffer[0] == (byte)PacketType.ReservedCompression)
        {
            var decompWriter = PacketBufferPool.GetWriter(totalSize);

            try
            {
                Decompress(assemblyBuffer, totalSize, decompWriter);
                var finalBuffer = decompWriter.DetachBuffer(out var finalSize);
                reader = PacketBufferPool.GetReader(finalBuffer, finalSize, true);
            }
            finally
            {
                PacketBufferPool.Return(decompWriter);
                ArrayPool<byte>.Shared.Return(assemblyBuffer);
            }
        }
        else
        {
            reader = PacketBufferPool.GetReader(assemblyBuffer, totalSize, true);
        }

        return true;
    }

    internal static byte[][] SplitPacket(ReadOnlySpan<byte> data, PacketReliability reliability,
        ushort sequenceNumber, out ushort packetId)
    {
        var packetType = data[0];

        // Thread-safe packet ID generation
        var id = Interlocked.Increment(ref nextPacketId);

        // Reset to 1 if we've exceeded ushort range
        if (id > ushort.MaxValue)
        {
            Interlocked.CompareExchange(ref nextPacketId, 1, id);
            id = Interlocked.Increment(ref nextPacketId);
        }

        packetId = (ushort)id;
        PacketWriter? compressionWriter = null;
        var sourceToSplit = data;

        try
        {
            // Compress if threshold is reached
            if (data.Length > CompressionThreshold)
            {
                compressionWriter = PacketBufferPool.GetWriter(data.Length);
                Compress(data, compressionWriter);

                if (compressionWriter.Position < data.Length * 0.9f)
                    sourceToSplit = compressionWriter.ToSpan();
            }

            var chunkCount = (sourceToSplit.Length + MaxChunkBytes - 1) / MaxChunkBytes;
            var result = new byte[chunkCount][];

            for (ushort index = 0; index < chunkCount; index++)
            {
                var offset = index * MaxChunkBytes;
                var chunkSize = Math.Min(MaxChunkBytes, sourceToSplit.Length - offset);

                // 10 byte header:
                var buffer = new byte[10 + chunkSize];

                // Packet Type
                buffer[0] = packetType;

                // Chunk index
                buffer[1] = (byte)(index & All8Bits);
                buffer[2] = (byte)((index >> 8) & All8Bits);

                // Total chunks
                buffer[3] = (byte)(chunkCount & All8Bits);
                buffer[4] = (byte)((chunkCount >> 8) & All8Bits);

                // Packet ID
                buffer[5] = (byte)(packetId & All8Bits);
                buffer[6] = (byte)((packetId >> 8) & All8Bits);

                // Reliability
                buffer[7] = (byte)reliability;

                // Sequence number (for ordered packets)
                buffer[8] = (byte)(sequenceNumber & All8Bits);
                buffer[9] = (byte)((sequenceNumber >> 8) & All8Bits);

                sourceToSplit.Slice(offset, chunkSize).CopyTo(buffer.AsSpan(10));
                result[index] = buffer;
            }

            return result;
        }
        finally
        {
            if (compressionWriter != null)
                PacketBufferPool.Return(compressionWriter);
        }
    }

    private static void CleanupStalePackets()
    {
        var now = DateTime.UtcNow;
        var keysToRemove = (from kvp in IncompletePackets
                           where now - kvp.Value.lastChunkTime > PacketTimeout
                           select kvp.Key).ToList();

        foreach (var key in keysToRemove)
        {
            if (IncompletePackets.TryRemove(key, out var packet))
            {
                SrLogger.LogWarning($"Timeout: {key} ({packet.receivedCount}/{packet.totalChunks} chunks)", SrLogTarget.Both);
            }
        }
    }

    private static void Compress(ReadOnlySpan<byte> data, PacketWriter targetWriter)
    {
        // (byte)PacketType.ReservedCompression instead of 0xFF so shows as used in PacketType.cs
        targetWriter.WriteByte((byte)PacketType.ReservedCompression);
        targetWriter.WriteByte(data[0]);

        var sourceSpan = data[1..];
        var sourceLen = sourceSpan.Length;

        targetWriter.WritePackedInt(sourceLen);

        var maxOutputSize = sourceLen + (sourceLen / 255) + 16; // Had to google this formula

        var inputBuffer = ArrayPool<byte>.Shared.Rent(sourceLen);
        var outputBuffer = ArrayPool<byte>.Shared.Rent(maxOutputSize);

        sourceSpan.CopyTo(inputBuffer);

        try
        {
            var compressedBytes = LZ4Codec.Encode64(
                inputBuffer, 0, sourceLen,
                outputBuffer, 0, maxOutputSize
            );

            if (compressedBytes > 0)
                targetWriter.WriteSpan(outputBuffer.AsSpan(0, compressedBytes));
            else
                throw new Exception("LZ4 Compression failed");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(inputBuffer);
            ArrayPool<byte>.Shared.Return(outputBuffer);
        }
    }

    private static void Decompress(byte[] data, int dataSize, PacketWriter targetWriter)
    {
        var reader = PacketBufferPool.GetReader(data, dataSize);

        try
        {
            reader.MoveForward(1); // Skip the ReservedCompression flag
            var originalType = reader.ReadByte();
            var uncompressedLen = reader.ReadPackedInt();

            var compressedLen = reader.BytesRemaining;

            var inputPoolBuffer = ArrayPool<byte>.Shared.Rent(compressedLen);
            var outputPoolBuffer = ArrayPool<byte>.Shared.Rent(uncompressedLen);

            try
            {
                reader.ReadToSpan(inputPoolBuffer.AsSpan(0, compressedLen));

                var actualDecompressed = LZ4Codec.Decode64(
                    inputPoolBuffer, 0, compressedLen,
                    outputPoolBuffer, 0, uncompressedLen,
                    true
                );

                targetWriter.WriteByte(originalType);
                targetWriter.WriteSpan(outputPoolBuffer.AsSpan(0, actualDecompressed));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(inputPoolBuffer);
                ArrayPool<byte>.Shared.Return(outputPoolBuffer);
            }
        }
        finally
        {
            PacketBufferPool.Return(reader);
        }
    }
}