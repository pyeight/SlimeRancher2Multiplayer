using JetBrains.Annotations;
using SR2MP.Shared.Utils;
// ReSharper disable InconsistentNaming

namespace SR2MP.Packets.Utils;

/// <summary>
/// Base class implementations for packet buffers.
/// </summary>
[PublicApi]
public abstract class PacketBuffer : IRecyclable
{
    /// <summary>
    /// The underlying buffer.
    /// </summary>
    protected byte[]? buffer;

    /// <summary>
    /// The current packed byte.
    /// </summary>
    protected byte currentPackedByte;

    /// <summary>
    /// The current packing bit index.
    /// </summary>
    protected int currentBitIndex;

    /// <summary>
    /// The current cursor position.
    /// </summary>
    protected int position;

    /// <summary>
    /// The starting bit packing index.
    /// </summary>
    private readonly int startingIndex;

    /// <summary>
    /// Gets the current cursor position within the buffer.
    /// </summary>
    public int Position => position;

    /// <inheritdoc cref="IRecyclable.IsRecycled"/>
    public bool IsRecycled { get; set; }

    /// <summary>
    /// Gets the total size of the readable/writable data represented by this buffer.
    /// </summary>
    public abstract int DataSize { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PacketBuffer"/> class.
    /// </summary>
    /// <param name="startingBitIndex">The starting bit index used when packing booleans and when resetting state in <see cref="Clear"/>.</param>
    protected PacketBuffer(int startingBitIndex) => startingIndex = currentBitIndex = startingBitIndex;

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

    /// <summary>
    /// Called when the buffer is recycled.
    /// </summary>
    protected virtual void OnRecycle() { }

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

    /// <inheritdoc cref="IRecyclable.Recycle"/>
    public void Recycle()
    {
        OnRecycle();
        buffer = null!;
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public abstract void Dispose();

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
    public void SetCursor(int pos)
    {
        if (pos is > int.MaxValue or < 0)
            throw new ArgumentOutOfRangeException(nameof(pos), "Position must be non negative and within int32 bounds.");

        var delta = pos - Position;

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
    public void Seek(int offset, SeekOrigin origin) => SetCursor(offset + (origin switch
    {
        SeekOrigin.Begin => 0,
        SeekOrigin.End => DataSize,
        _ => Position
    }));
}