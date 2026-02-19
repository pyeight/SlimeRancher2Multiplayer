namespace SR2MP.Packets.Utils;

public abstract class PacketBuffer : IDisposable
{
    protected byte[] buffer;

    protected byte currentPackedByte;
    protected int currentBitIndex;

    protected int position = 0;
    protected bool disposed;

    private int startingIndex;

    public int Position => position;

    public abstract int DataSize { get; }

    protected PacketBuffer(byte[] data, int starting)
    {
        buffer = data;
        startingIndex = currentBitIndex = starting;
    }

    public byte this[int index] => disposed
        ? throw new ObjectDisposedException(nameof(PacketBuffer))
        : (uint)index >= (uint)DataSize
            ? throw new ArgumentOutOfRangeException(nameof(index), "Index must be within the bounds of the data size.")
            : buffer[index];

    protected virtual void OnDispose() { }

    protected abstract void EnsureBounds(int count);

    public abstract void MoveForward(int count);

    public abstract void MoveBack(int count);

    public abstract void EndPackingBools();

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        OnDispose();
        buffer = null!;
        GC.SuppressFinalize(this);
    }

    public virtual void Clear()
    {
        position = 0;
        currentBitIndex = startingIndex;
        currentPackedByte = 0;
    }

    public void SetCursor(long pos)
    {
        if (pos is > int.MaxValue or < 0)
            throw new ArgumentOutOfRangeException(nameof(pos), "Position must be non negative and within int32 bounds.");

        var delta = (int)pos - Position;

        if (delta > 0)
            MoveForward(delta);
        else if (delta < 0)
            MoveBack(-delta);
    }
}