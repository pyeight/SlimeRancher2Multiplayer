using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using LZ4ps;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Utils;

namespace SR2MP.Shared.Managers;

internal static class PacketChunkManager
{
    private sealed class IncompletePacket : IRecyclable
    {
        public byte[]?[]? Chunks;
        public int[]? ChunkLengths;
        public bool[]? Received;

        public ushort TotalChunks;
        public int ReceivedCount;
        public DateTime LastChunkTime;
        public PacketReliability Reliability;
        public NetworkChannel Channel;
        public ushort SequenceNumber;

        public bool IsRecycled { get; set; }

        private void Initialize(ushort totalChunks, PacketReliability reliability, NetworkChannel channel, ushort sequenceNumber)
        {
            TotalChunks = totalChunks;
            Reliability = reliability;
            Channel = channel;
            SequenceNumber = sequenceNumber;

            ReceivedCount = 0;
            LastChunkTime = DateTime.UtcNow;

            Chunks = ArrayPool<byte[]>.Shared.Rent(totalChunks);
            ChunkLengths = ArrayPool<int>.Shared.Rent(totalChunks);
            Received = ArrayPool<bool>.Shared.Rent(totalChunks);

            Array.Clear(Received, 0, totalChunks);
        }

        public void Recycle()
        {
            if (Chunks != null)
                ArrayPool<byte[]?>.Shared.Return(Chunks, true);

            if (ChunkLengths != null)
                ArrayPool<int>.Shared.Return(ChunkLengths);

            if (Received != null)
                ArrayPool<bool>.Shared.Return(Received);

            Chunks = null!;
            ChunkLengths = null!;
            Received = null!;
        }

        public void Dispose() => Return(this);

        public static IncompletePacket Borrow(ushort totalChunks, PacketReliability reliability, NetworkChannel channel, ushort sequenceNumber)
        {
            var packet = RecyclePool<IncompletePacket>.Borrow();
            packet.Initialize(totalChunks, reliability, channel, sequenceNumber);
            return packet;
        }

        public static void Return(IncompletePacket packet) => RecyclePool<IncompletePacket>.Return(packet);
    }

    private static readonly ConcurrentDictionary<PacketKey, IncompletePacket> IncompletePackets = new();

    private static int nextPacketId = 1;

    private const int MaxChunkBytes = 512 - HeaderSize;
    private const int CompressionThreshold = 64;
    private static readonly TimeSpan PacketTimeout = TimeSpan.FromSeconds(30);

    private static int packetCounter;
    private const int CleanupInterval = 100;

    private const byte All8Bits = byte.MaxValue;

    internal static bool TryMergePacket(PacketType packetType, byte[] data,
        int chunkLength, ushort chunkIndex, ushort totalChunks, ushort packetId,
        IPEndPoint senderEp, PacketReliability reliability, NetworkChannel channel, ushort sequenceNumber,
        out PacketReader reader, out PacketReliability outReliability, out NetworkChannel outChannel, out ushort outSequenceNumber)
    {
        reader = null!;
        outReliability = reliability;
        outChannel = channel;
        outSequenceNumber = sequenceNumber;

        if (totalChunks == 0 || chunkIndex >= totalChunks)
        {
            SrLogger.LogWarning($"Invalid chunk parameters: index={chunkIndex}, total={totalChunks}");
            ArrayPool<byte>.Shared.Return(data);
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
            IncompletePacket.Borrow(totalChunks, reliability, channel, sequenceNumber));

        if (packet.TotalChunks != totalChunks)
        {
            SrLogger.LogWarning($"Chunk count mismatch for {key}: expected={packet.TotalChunks} got={totalChunks}");
            IncompletePackets.TryRemove(key, out _);
            return false;
        }

        // Store chunks
        if (!packet.Received![chunkIndex])
        {
            packet.Chunks![chunkIndex] = data;
            packet.ChunkLengths![chunkIndex] = chunkLength;
            packet.Received[chunkIndex] = true;
            packet.ReceivedCount++;
            packet.LastChunkTime = DateTime.UtcNow;
        }

        // Wait for all chunks
        if (packet.ReceivedCount != totalChunks)
            return false;

        // Merge chunks
        var totalSize = 0;
        for (var i = 0; i < totalChunks; i++)
            totalSize += packet.ChunkLengths![i];

        var assemblyBuffer = ArrayPool<byte>.Shared.Rent(totalSize);
        var offset = 0;

        for (var i = 0; i < totalChunks; i++)
        {
            packet.Chunks![i].AsSpan(0, packet.ChunkLengths![i]).CopyTo(assemblyBuffer.AsSpan(offset));
            offset += packet.ChunkLengths[i];
            ArrayPool<byte>.Shared.Return(packet.Chunks![i]!);
        }

        outReliability = packet.Reliability;
        outChannel = packet.Channel;
        outSequenceNumber = packet.SequenceNumber;

        IncompletePackets.TryRemove(key, out _);

        // Decompress if compressed
        if (totalSize > 0 && assemblyBuffer[0] == (byte)PacketType.ReservedCompression)
        {
            using var decompWriter = PacketWriter.Borrow(totalSize);

            try
            {
                Decompress(assemblyBuffer, totalSize, decompWriter);
                var finalBuffer = decompWriter.DetachBuffer(out var finalSize);
                reader = PacketReader.Borrow(finalBuffer, finalSize, true);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(assemblyBuffer);
            }
        }
        else
        {
            reader = PacketReader.Borrow(assemblyBuffer, totalSize, true);
        }

        IncompletePacket.Return(packet);
        return true;
    }

    internal static SplitResult SplitPacket(ReadOnlySpan<byte> data, PacketReliability reliability,
        NetworkChannel channel, ushort sequenceNumber, out ushort packetId)
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
                compressionWriter = PacketWriter.Borrow(data.Length);
                Compress(data, compressionWriter);

                if (compressionWriter.Position < data.Length * 0.9f)
                    sourceToSplit = compressionWriter.ToSpan();
            }

            var chunkCount = (sourceToSplit.Length + MaxChunkBytes - 1) / MaxChunkBytes;
            var resultChunks = ArrayPool<ArraySegment<byte>>.Shared.Rent(chunkCount);

            for (ushort index = 0; index < chunkCount; index++)
            {
                var chunkOffset = index * MaxChunkBytes;
                var chunkSize = Math.Min(MaxChunkBytes, sourceToSplit.Length - chunkOffset);
                var totalChunkLength = HeaderSize + chunkSize;

                // 13 byte header:
                var buffer = ArrayPool<byte>.Shared.Rent(totalChunkLength);

                // [0] Packet type
                buffer[0] = packetType;

                // [1-2] Chunk index
                buffer[1] = (byte)(index & All8Bits);
                buffer[2] = (byte)((index >> 8) & All8Bits);

                // [3-4] Total chunks
                buffer[3] = (byte)(chunkCount & All8Bits);
                buffer[4] = (byte)((chunkCount >> 8) & All8Bits);

                // [5-6] Packet ID
                buffer[5] = (byte)(packetId & All8Bits);
                buffer[6] = (byte)((packetId >> 8) & All8Bits);

                // [7] Channel
                buffer[7] = (byte)channel;

                // [8] Reliability
                buffer[8] = (byte)reliability;

                // [9-10] Sequence number
                buffer[9] = (byte)(sequenceNumber & All8Bits);
                buffer[10] = (byte)((sequenceNumber >> 8) & All8Bits);

                // Copy data into buffer at offset HeaderSize, then compute CRC over it
                sourceToSplit.Slice(chunkOffset, chunkSize).CopyTo(buffer.AsSpan(HeaderSize));

                var crc = PacketCRC.Compute(buffer, HeaderSize, chunkSize);

                // [11-12] CRC16 of data
                buffer[11] = (byte)(crc & All8Bits);
                buffer[12] = (byte)((crc >> 8) & All8Bits);

                resultChunks[index] = new ArraySegment<byte>(buffer, 0, totalChunkLength);
            }

            return new SplitResult(resultChunks, chunkCount);
        }
        finally
        {
            if (compressionWriter != null)
                PacketWriter.Return(compressionWriter);
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
            if (now - kvp.Value.LastChunkTime > PacketTimeout)
                keysToRemove[removeCount++] = kvp.Key;
        }

        for (var i = 0; i < removeCount; i++)
        {
            var key = keysToRemove[i];

            if (!IncompletePackets.TryRemove(key, out var packet))
                continue;

            SrLogger.LogWarning($"Timeout: {key.PacketType} from {key.EndPoint} ({packet.ReceivedCount}/{packet.TotalChunks} chunks)");

            for (var c = 0; c < packet.TotalChunks; c++)
            {
                if (!packet.Received![c] || packet.Chunks?[c] == null)
                    continue;

                ArrayPool<byte>.Shared.Return(packet.Chunks![c]!);
                packet.Chunks[c] = null!;
            }

            IncompletePacket.Return(packet);
        }

        ArrayPool<PacketKey>.Shared.Return(keysToRemove);
    }

    private static void Compress(ReadOnlySpan<byte> data, PacketWriter targetWriter)
    {
        targetWriter.WriteByte((byte)PacketType.ReservedCompression);
        targetWriter.WriteByte(data[0]);

        var sourceSpan = data[1..];
        var sourceLen = sourceSpan.Length;

        targetWriter.WritePackedInt(sourceLen);

        var maxOutputSize = sourceLen + (sourceLen / 255) + 16;

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
        var reader = PacketReader.Borrow(data, dataSize);
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
            PacketReader.Return(reader);
        }
    }

    internal static PacketReader DecompressSingleChunk(byte[] data, int offset, int length)
    {
        var tempReader = PacketReader.Borrow(data, offset + length);
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

                return PacketReader.Borrow(outputPoolBuffer, actualDecompressed + 1, true);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(inputPoolBuffer);
            }
        }
        finally
        {
            PacketReader.Return(tempReader);
        }
    }
}