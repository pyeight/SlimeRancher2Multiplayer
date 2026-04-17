using System.Buffers;

namespace SR2MP.Shared.Utils;

internal sealed class SplitResult : IDisposable
{
    private readonly byte[] MasterBuffer;
    private readonly int SourcePayloadLength;
    private readonly int MaxChunkBytes;
    private readonly int HeaderSize;

    public readonly int Count;

    public SplitResult(byte[] masterBuffer, int count, int sourcePayloadLength, int maxChunkBytes, int headerSize)
    {
        Count = count;
        MasterBuffer = masterBuffer;
        SourcePayloadLength = sourcePayloadLength;
        MaxChunkBytes = maxChunkBytes;
        HeaderSize = headerSize;
    }

    public ArraySegment<byte> GetChunk(int index)
    {
        if (index < 0 || index >= Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        // Every chunk before this one took up exactly (HeaderSize + MaxChunkBytes)
        var offset = index * (HeaderSize + MaxChunkBytes);

        // The last chunk is whatever payload is left over; all others are MaxChunkBytes
        var payloadSize = index == Count - 1
            ? SourcePayloadLength - (index * MaxChunkBytes)
            : MaxChunkBytes;

        return new ArraySegment<byte>(MasterBuffer, offset, HeaderSize + payloadSize);
    }

    public void Dispose()
    {
        if (MasterBuffer != null)
            ArrayPool<byte>.Shared.Return(MasterBuffer);
    }
}