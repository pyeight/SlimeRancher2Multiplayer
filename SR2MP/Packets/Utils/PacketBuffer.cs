using JetBrains.Annotations;
using SR2MP.Shared.Utils;
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable InconsistentNaming

namespace SR2MP.Packets.Utils;

/// <summary>
/// Represents a recyclable packet buffer abstraction that tracks cursor state,
/// supports bit-packing operations, and provides movement semantics for derived buffers.
/// </summary>
[PublicAPI]
public abstract class PacketBuffer : IRecyclable
{
    protected byte[]? buffer;

    protected byte currentPackedByte;
    protected int currentBitIndex;

    protected int position;

    private readonly int startingIndex;

    /// <summary>
    /// Gets the current cursor position within the buffer.
    /// </summary>
    public int Position => position;

    /// <summary>
    /// Gets or sets a value indicating whether this buffer has been recycled.
    /// </summary>
    public bool IsRecycled { get; set; }

    /// <summary>
    /// Gets the total size of the readable/writable data represented by this buffer.
    /// </summary>
    public abstract int DataSize { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PacketBuffer"/> class.
    /// </summary>
    /// <param name="starting">
    /// The starting bit index used when packing booleans and when resetting state in <see cref="Clear"/>.
    /// </param>
    protected PacketBuffer(int starting) => startingIndex = currentBitIndex = starting;

    /// <summary>
    /// Gets the byte at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the byte to retrieve.</param>
    /// <returns>The byte value at <paramref name="index"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the buffer has already been recycled.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is outside <see cref="DataSize"/>.</exception>
    public byte this[int index] => IsRecycled
        ? throw new InvalidOperationException("PacketBuffer is already recycled!")
        : (uint)index >= (uint)DataSize
            ? throw new ArgumentOutOfRangeException(nameof(index), "Index must be within the bounds of the data size.")
            : buffer![index];

    protected virtual void OnRecycle() { }

    protected abstract void EnsureBounds(int count);

    /// <summary>
    /// Moves the cursor forward by the specified number of bytes.
    /// </summary>
    /// <param name="count">The number of bytes to move forward.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the count is negative.</exception>
    /// <exception cref="EndOfStreamException">Thrown if advancing exceeds buffer bounds.</exception>
    public abstract void MoveForward(int count);

    /// <summary>
    /// Moves the cursor backward by the specified number of bytes.
    /// </summary>
    /// <param name="count">The number of bytes to move backward.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the count is negative.</exception>
    /// <exception cref="InvalidOperationException">Thrown if retreating goes before the start of the stream.</exception>
    public abstract void MoveBack(int count);

    /// <summary>
    /// Finalizes any pending packed boolean state and aligns the buffer for subsequent operations.
    /// </summary>
    public abstract void EndPackingBools();

    /// <summary>
    /// Recycles this buffer instance, allowing implementations to release resources and clear backing storage.
    /// </summary>
    public void Recycle()
    {
        OnRecycle();
        buffer = null!;
    }

    /// <summary>
    /// Resets cursor and bit-packing state to their initial values.
    /// </summary>
    public virtual void Clear()
    {
        position = 0;
        currentBitIndex = startingIndex;
        currentPackedByte = 0;
    }

    /// <summary>
    /// Sets the absolute cursor position.
    /// </summary>
    /// <param name="pos">The zero-based absolute position to move to.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pos"/> is negative or exceeds <see cref="int.MaxValue"/>.</exception>
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

    /// <summary>
    /// Sets the cursor position by applying an offset relative to the specified origin.
    /// </summary>
    /// <param name="offset">The offset from <paramref name="origin"/>.</param>
    /// <param name="origin">The reference point used to compute the target position.</param>
    public void Seek(long offset, SeekOrigin origin) => SetCursor(offset + (origin switch
    {
        SeekOrigin.Begin => 0,
        SeekOrigin.End => DataSize,
        _ => Position
    }));
}