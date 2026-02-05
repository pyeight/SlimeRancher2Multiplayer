using System.Buffers;

namespace SR2MP.Packets.Utils;

public abstract class PacketBase : IDisposable
{
    protected byte[] buffer;
    protected byte currentPackedByte;
    protected int currentBitIndex;

    protected int position = 0;
    protected bool disposed = false;

    protected PacketBase(int initialCapacity, int startingIndex)
    {
        buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        currentBitIndex = startingIndex;
    }

    public int Position => position;

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;

        if (buffer != null)
        {
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = null!;
        }

        GC.SuppressFinalize(this);
    }
}