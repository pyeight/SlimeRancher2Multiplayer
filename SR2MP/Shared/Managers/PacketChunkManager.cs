using SR2MP.Packets.Utils;
using System.Collections.Concurrent;
using LZ4ps;
using System.Buffers;
using SR2MP.Shared.Utils;
using System.Net;

namespace SR2MP.Shared.Managers;

public static class PacketChunkManager
{
    private sealed class IncompletePacket : IRecyclable
    {
        public byte[][] chunks = null!;
        public int[] chunkLengths = null!;
        public bool[] received = null!;

        public ushort totalChunks;
        public int receivedCount;
        public DateTime lastChunkTime;
        public PacketReliability reliability;
        public ushort sequenceNumber;

        public bool IsRecycled { get; set; }

        public void Initialize(ushort totalChunks, PacketReliability reliability, ushort sequenceNumber)
        {
            this.totalChunks = totalChunks;
            this.reliability = reliability;
            this.sequenceNumber = sequenceNumber;

            receivedCount = 0;
            lastChunkTime = DateTime.UtcNow;

            chunks = ArrayPool<byte[]>.Shared.Rent(totalChunks);
            chunkLengths = ArrayPool<int>.Shared.Rent(totalChunks);
            received = ArrayPool<bool>.Shared.Rent(totalChunks);

            Array.Clear(received, 0, totalChunks);
        }

        public void Recycle()
        {
            if (chunks != null) ArrayPool<byte[]>.Shared.Return(chunks, true);
            if (chunkLengths != null) ArrayPool<int>.Shared.Return(chunkLengths);
            if (received != null) ArrayPool<bool>.Shared.Return(received);

            chunks = null!;
            chunkLengths = null!;
            received = null!;
        }

        public static IncompletePacket Borrow(ushort totalChunks, PacketReliability reliability, ushort sequenceNumber)
        {
            var packet = RecyclePool<IncompletePacket>.Borrow();
            packet.Initialize(totalChunks, reliability, sequenceNumber);
            return packet;
        }
        
        public static void Return(IncompletePacket packet) => RecyclePool<IncompletePacket>.Return(packet);
    }

    private static readonly ConcurrentDictionary<PacketKey, IncompletePacket> IncompletePackets = new();

    private static int nextPacketId = 1;

    private const int MaxChunkBytes = 500;
    private const int CompressionThreshold = 30;
    private static readonly TimeSpan PacketTimeout = TimeSpan.FromSeconds(30);

    private static int packetCounter;
    private const int CleanupInterval = 100;

    private const byte All8Bits = byte.MaxValue;

    internal static bool TryMergePacket(PacketType packetType, byte[] data, int chunkLength, ushort chunkIndex,
        ushort totalChunks, ushort packetId, IPEndPoint senderEp, PacketReliability reliability,
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

        var key = new PacketKey((byte)packetType, packetId, senderEp);

        var packet = IncompletePackets.GetOrAdd(key, _ =>
            IncompletePacket.Borrow(totalChunks, reliability, sequenceNumber));

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
            packet.chunkLengths[chunkIndex] = chunkLength;
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
            totalSize += packet.chunkLengths[i];

        var assemblyBuffer = ArrayPool<byte>.Shared.Rent(totalSize);
        var offset = 0;

        for (var i = 0; i < totalChunks; i++)
        {
            packet.chunks[i].AsSpan(0, packet.chunkLengths[i]).CopyTo(assemblyBuffer.AsSpan(offset));
            offset += packet.chunkLengths[i];
            ArrayPool<byte>.Shared.Return(packet.chunks[i]);
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

        IncompletePacket.Return(packet);
        return true;
    }

    internal static SplitResult SplitPacket(ReadOnlySpan<byte> data, PacketReliability reliability,
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
            var resultChunks = ArrayPool<ArraySegment<byte>>.Shared.Rent(chunkCount);

            for (ushort index = 0; index < chunkCount; index++)
            {
                var offset = index * MaxChunkBytes;
                var chunkSize = Math.Min(MaxChunkBytes, sourceToSplit.Length - offset);
                var totalChunkLength = 10 + chunkSize;

                // 10 byte header:
                var buffer = ArrayPool<byte>.Shared.Rent(totalChunkLength);

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
                resultChunks[index] = new ArraySegment<byte>(buffer, 0, totalChunkLength);
            }

            return new SplitResult(resultChunks, chunkCount);
        }
        finally
        {
            if (compressionWriter != null)
                PacketBufferPool.Return(compressionWriter);
        }
    }

    private static void CleanupStalePackets()
    {
        var count = IncompletePackets.Count;
        if (count == 0) return;

        var keysToRemove = ArrayPool<PacketKey>.Shared.Rent(count);
        var removeCount = 0;
        var now = DateTime.UtcNow;

        foreach (var kvp in IncompletePackets)
        {
            if (now - kvp.Value.lastChunkTime > PacketTimeout)
                keysToRemove[removeCount++] = kvp.Key;
        }

        for (var i = 0; i < removeCount; i++)
        {
            var key = keysToRemove[i];

            if (!IncompletePackets.TryRemove(key, out var packet))
                continue;

            SrLogger.LogWarning($"Timeout: {key.PacketType} from {key.EndPoint} ({packet.receivedCount}/{packet.totalChunks} chunks)", SrLogTarget.Both);

            for (var c = 0; c < packet.totalChunks; c++)
            {
                if (!packet.received[c] || packet.chunks[c] == null)
                    continue;

                ArrayPool<byte>.Shared.Return(packet.chunks[c]);
                packet.chunks[c] = null!;
            }

            IncompletePacket.Return(packet);
        }

        ArrayPool<PacketKey>.Shared.Return(keysToRemove);
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
        reader.MoveForward(1); // Skip the ReservedCompression flag

        try
        {
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

    internal static PacketReader DecompressSingleChunk(byte[] data, int offset, int length)
    {
        var tempReader = PacketBufferPool.GetReader(data, offset + length);
        tempReader.MoveForward(offset + 1); // Skip to right after the ReservedCompression flag

        try
        {
            var originalType = tempReader.ReadByte();
            var uncompressedLen = tempReader.ReadPackedInt();
            var compressedLen = tempReader.BytesRemaining;

            var inputPoolBuffer = ArrayPool<byte>.Shared.Rent(compressedLen);
            var outputPoolBuffer = ArrayPool<byte>.Shared.Rent(uncompressedLen + 1); // +1 to fit the packet type byte

            try
            {
                tempReader.ReadToSpan(inputPoolBuffer.AsSpan(0, compressedLen));

                // Decompress directly into the output pool buffer, offset by 1
                var actualDecompressed = LZ4Codec.Decode64(
                    inputPoolBuffer, 0, compressedLen,
                    outputPoolBuffer, 1, uncompressedLen,
                    true
                );

                outputPoolBuffer[0] = originalType;

                return PacketBufferPool.GetReader(outputPoolBuffer, actualDecompressed + 1, true);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(inputPoolBuffer);
            }
        }
        finally
        {
            PacketBufferPool.Return(tempReader);
        }
    }
}