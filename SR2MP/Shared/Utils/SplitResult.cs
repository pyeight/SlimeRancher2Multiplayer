using System.Buffers;

namespace SR2MP.Shared.Utils;

public readonly struct SplitResult : IDisposable
{
    public readonly ArraySegment<byte>[] Chunks;
    public readonly int Count;

    public SplitResult(ArraySegment<byte>[] chunks, int count)
    {
        Chunks = chunks;
        Count = count;
    }

    public void Dispose()
    {
        for (var i = 0; i < Count; i++)
        {
            if (Chunks[i].Array != null)
                ArrayPool<byte>.Shared.Return(Chunks[i].Array!);
        }

        ArrayPool<ArraySegment<byte>>.Shared.Return(Chunks);
    }
}