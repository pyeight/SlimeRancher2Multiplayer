using SR2MP.Shared.Utils;

namespace SR2MP.Packets.Utils;

public abstract class PacketBuffer : IRecyclable
{
    protected byte[] buffer;

    protected byte currentPackedByte;
    protected int currentBitIndex;

    protected int position = 0;

    private int startingIndex;

    public int Position => position;
    
    public bool IsRecycled { get; set; }

    public abstract int DataSize { get; }

    protected PacketBuffer(int starting) => startingIndex = currentBitIndex = starting;

    public byte this[int index] => IsRecycled
        ? throw new InvalidOperationException("PacketBuffer is already recycled!")
        : (uint)index >= (uint)DataSize
            ? throw new ArgumentOutOfRangeException(nameof(index), "Index must be within the bounds of the data size.")
            : buffer[index];

    protected virtual void OnRecycle() { }

    protected abstract void EnsureBounds(int count);

    public abstract void MoveForward(int count);

    public abstract void MoveBack(int count);

    public abstract void EndPackingBools();

    public void Recycle()
    {
        OnRecycle();
        buffer = null!;
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

    public void Seek(long offset, SeekOrigin origin) => SetCursor(offset + (origin switch
    {
        SeekOrigin.Begin => 0,
        SeekOrigin.End => DataSize,
        _ => Position
    }));
}