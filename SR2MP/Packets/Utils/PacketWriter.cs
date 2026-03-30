using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using SR2MP.Shared.Utils;
using Unity.Mathematics;

namespace SR2MP.Packets.Utils;

/// <summary>
/// A reusable writer for packing primitive types, structs, and objects into a byte buffer for network transmission.
/// </summary>
[PublicAPI]
public sealed class PacketWriter : PacketBuffer
{
    private int size;

    /// <summary>
    /// Gets the total size of the data written to this buffer.
    /// </summary>
    public override int DataSize => size;

    /// <summary>
    /// Initializes a new instance of the <see cref="PacketWriter"/> class.
    /// </summary>
    [Obsolete("Use PacketWriter,Borrow instead!", true)]
    public PacketWriter() : base(0) { }

    private void EnsureCapacity(int bytesToAdd)
    {
        if (IsRecycled)
            throw new InvalidOperationException("PacketWriter is already recycled!");

        if (buffer == null)
            throw new InvalidOperationException("The buffer has been detached and is no longer available.");

        if (currentBitIndex > 0)
            FlushPackedByte();

        if (position + bytesToAdd > buffer.Length)
            ResizeBuffer(bytesToAdd);
    }

    private void ResizeBuffer(int bytesToAdd)
    {
        var newSize = Math.Max(position + bytesToAdd, buffer!.Length * 2);
        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        buffer.AsSpan(0, position).CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(buffer);
        buffer = newBuffer;
    }

    /// <summary>
    /// Writes a single byte to the buffer.
    /// </summary>
    /// <param name="value">The byte to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value) => WriteAlloc(1)[0] = value;

    /// <summary>
    /// Writes a boolean value to the buffer as a single byte.
    /// </summary>
    /// <param name="value">The boolean to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value) => WriteByte((byte)(value ? 1 : 0));

    /// <summary>
    /// Writes a signed byte to the buffer.
    /// </summary>
    /// <param name="value">The signed byte to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSByte(sbyte value) => WriteByte((byte)value);

    /// <summary>
    /// Writes a 16-bit signed integer using little-endian encoding.
    /// </summary>
    /// <param name="value">The short to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteShort(short value) => BinaryPrimitives.WriteInt16LittleEndian(WriteAlloc(2), value);

    /// <summary>
    /// Writes a 16-bit unsigned integer using little-endian encoding.
    /// </summary>
    /// <param name="value">The unsigned short to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUShort(ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(WriteAlloc(2), value);

    /// <summary>
    /// Writes a 32-bit signed integer using little-endian encoding.
    /// </summary>
    /// <param name="value">The integer to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt(int value) => BinaryPrimitives.WriteInt32LittleEndian(WriteAlloc(4), value);

    /// <summary>
    /// Writes a 32-bit unsigned integer using little-endian encoding.
    /// </summary>
    /// <param name="value">The unsigned integer to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt(uint value) => BinaryPrimitives.WriteUInt32LittleEndian(WriteAlloc(4), value);

    /// <summary>
    /// Writes a single-precision floating-point number using little-endian encoding.
    /// </summary>
    /// <param name="value">The float to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat(float value) => BinaryPrimitives.WriteSingleLittleEndian(WriteAlloc(4), value);

    /// <summary>
    /// Writes a 64-bit signed integer using little-endian encoding.
    /// </summary>
    /// <param name="value">The long to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLong(long value) => BinaryPrimitives.WriteInt64LittleEndian(WriteAlloc(8), value);

    /// <summary>
    /// Writes a 64-bit unsigned integer using little-endian encoding.
    /// </summary>
    /// <param name="value">The unsigned long to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteULong(ulong value) => BinaryPrimitives.WriteUInt64LittleEndian(WriteAlloc(8), value);

    /// <summary>
    /// Writes a double-precision floating-point number using little-endian encoding.
    /// </summary>
    /// <param name="value">The double to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDouble(double value) => BinaryPrimitives.WriteDoubleLittleEndian(WriteAlloc(8), value);

    private void WriteFloats(ReadOnlySpan<float> values)
    {
        var span = WriteAlloc(values.Length * 4);

        for (var i = 0; i < values.Length; i++)
            BinaryPrimitives.WriteSingleLittleEndian(span[(i * 4)..], values[i]);
    }

    /// <summary>
    /// Writes a Vector2 structure composed of 2 floats.
    /// </summary>
    /// <param name="value">The Vector2 to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVector2(Vector2 value) => WriteFloats(stackalloc float[2] { value.x, value.y });

    /// <summary>
    /// Writes a Vector3 structure composed of 3 floats.
    /// </summary>
    /// <param name="value">The Vector3 to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVector3(Vector3 value) => WriteFloats(stackalloc float[3] { value.x, value.y, value.z });

    /// <summary>
    /// Writes a Quaternion structure composed of 4 floats.
    /// </summary>
    /// <param name="value">The Quaternion to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteQuaternion(Quaternion value) => WriteFloats(stackalloc float[4] { value.x, value.y, value.z, value.w });

    /// <summary>
    /// Writes a float4 structure composed of 4 floats.
    /// </summary>
    /// <param name="value">The float4 to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat4(float4 value) => WriteFloats(stackalloc float[4] { value.x, value.y, value.z, value.w });

    /// <summary>
    /// Writes an enum value to the buffer.
    /// </summary>
    /// <typeparam name="T">The type of the enum.</typeparam>
    /// <param name="value">The enum value to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteEnum<T>(T value) where T : struct, Enum => PacketWriterDels.Enum<T>.Func(this, value);

    /// <summary>
    /// Writes an enum value by its string representation.
    /// </summary>
    /// <typeparam name="T">The type of the enum.</typeparam>
    /// <param name="value">The enum value to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteEnumAsString<T>(T value) where T : struct, Enum => WriteString(value.ToString());

    /// <summary>
    /// Writes an object that implements <see cref="INetObject"/> by invoking its Serialise method.
    /// </summary>
    /// <typeparam name="T">The type of the network object.</typeparam>
    /// <param name="value">The network object to serialize.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteNetObject<T>(T value) where T : INetObject => value.Serialise(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WritePacket<T>(T value) where T : IPacket
    {
        WriteByte((byte)value.Type);
        value.Serialise(this);
    }

    /// <summary>
    /// Writes a custom packet to the buffer, including its header byte.
    /// </summary>
    /// <typeparam name="T">The type of the custom packet.</typeparam>
    /// <param name="value">The custom packet to serialize.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteCustomPacket<T>(T value) where T : ICustomPacket
    {
        WriteByte(value.PacketHeader);
        value.Serialise(this);
    }

    /// <summary>
    /// Writes a 32-bit signed integer using variable-length ZigZag encoding.
    /// </summary>
    /// <param name="value">The integer to pack and write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePackedInt(int value) => WritePackedUInt((uint)((value << 1) ^ (value >> 31)));

    /// <summary>
    /// Writes a 32-bit unsigned integer using variable-length encoding.
    /// </summary>
    /// <param name="value">The unsigned integer to pack and write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePackedUInt(uint value) => WriteVarInt(value, 5);

    /// <summary>
    /// Writes a 64-bit signed integer using variable-length ZigZag encoding.
    /// </summary>
    /// <param name="value">The long to pack and write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePackedLong(long value) => WritePackedULong((ulong)((value << 1) ^ (value >> 63)));

    /// <summary>
    /// Writes a 64-bit unsigned integer using variable-length encoding.
    /// </summary>
    /// <param name="value">The unsigned long to pack and write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePackedULong(ulong value) => WriteVarInt(value, 10);

    /// <summary>
    /// Writes a UTF-8 encoded string prefixed by its length as a ushort. Null or empty strings write a length of 0.
    /// </summary>
    /// <param name="value">The string to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    public void WriteString(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            WriteUShort(0);
            return;
        }

        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        EnsureCapacity(2 + maxByteCount);

        var lengthIndex = position;
        Advance(2);

        var actualCount = Encoding.UTF8.GetBytes(value.AsSpan(), buffer.AsSpan(position));
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(lengthIndex), (ushort)actualCount);

        Advance(actualCount);
    }

    /// <summary>
    /// Writes a UTF-8 encoded string without prefixing its length.
    /// </summary>
    /// <param name="value">The string to write.</param>
    /// <exception cref="ArgumentException">Thrown if the provided string is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    public void WriteStringWithoutSize(string? value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("Value cannot be null or empty");

        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        EnsureCapacity(maxByteCount);
        Advance(Encoding.UTF8.GetBytes(value.AsSpan(), buffer.AsSpan(position)));
    }

    private void WriteCollection<T>(int count, IEnumerable<T> items, Action<PacketWriter, T> writer)
    {
        WriteUShort((ushort)count);

        foreach (var item in items)
            writer(this, item);
    }

    /// <summary>
    /// Writes an array prefixed by its length as a ushort.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="array">The array to write.</param>
    /// <param name="writer">The delegate used to write each element.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteArray<T>(T[]? array, Action<PacketWriter, T> writer)
        => WriteCollection(array?.Length ?? 0, array ?? Enumerable.Empty<T>(), writer);

    /// <summary>
    /// Writes a List prefixed by its length as a ushort.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="list">The list to write.</param>
    /// <param name="writer">The delegate used to write each element.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteList<T>(List<T>? list, Action<PacketWriter, T> writer)
        => WriteCollection(list?.Count ?? 0, list ?? Enumerable.Empty<T>(), writer);

    /// <summary>
    /// Writes a HashSet prefixed by its length as a ushort.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="set">The set to write.</param>
    /// <param name="writer">The delegate used to write each element.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSet<T>(HashSet<T>? set, Action<PacketWriter, T> writer)
        => WriteCollection(set?.Count ?? 0, set ?? Enumerable.Empty<T>(), writer);

    /// <summary>
    /// Writes a custom CppCollections HashSet prefixed by its length as a ushort.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="set">The set to write.</param>
    /// <param name="writer">The delegate used to write each element.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    public void WriteCppSet<T>(CppCollections.HashSet<T>? set, Action<PacketWriter, T> writer)
    {
        if (set == null)
        {
            WriteUShort(0);
            return;
        }

        WriteUShort((ushort)set.Count);

        foreach (var item in set)
            writer(this, item);
    }

    /// <summary>
    /// Writes a Dictionary prefixed by its length as a ushort.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    /// <param name="dict">The dictionary to write.</param>
    /// <param name="keyWriter">The delegate used to write keys.</param>
    /// <param name="valueWriter">The delegate used to write values.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    public void WriteDictionary<TKey, TValue>(Dictionary<TKey, TValue>? dict, Action<PacketWriter, TKey> keyWriter, Action<PacketWriter, TValue> valueWriter) where TKey : notnull
    {
        WriteUShort((ushort)(dict?.Count ?? 0));

        if (dict == null)
            return;

        foreach (var (key, value) in dict)
        {
            keyWriter(this, key);
            valueWriter(this, value);
        }
    }

    /// <summary>
    /// Packs a boolean value into the current working byte. Flushes to buffer when a byte is full.
    /// </summary>
    /// <param name="value">The boolean to pack.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    public void WritePackedBool(bool value)
    {
        if (value)
            currentPackedByte |= (byte)(1 << currentBitIndex);

        currentBitIndex++;

        if (currentBitIndex == 8)
            EnsureCapacity(0);
    }

    /// <summary>
    /// Forces any pending packed booleans to be flushed to the buffer, realigning the cursor to byte boundaries.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void EndPackingBools() => EnsureCapacity(0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FlushPackedByte()
    {
        var packed = currentPackedByte;
        currentPackedByte = 0;
        currentBitIndex = 0;

        if (position + 1 > buffer!.Length)
            ResizeBuffer(1);

        buffer[position] = packed;
        Advance(1);
    }

    /// <summary>
    /// Writes a read-only span of bytes to the buffer.
    /// </summary>
    /// <param name="data">The byte span to write.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSpan(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return;

        EnsureCapacity(data.Length);
        data.CopyTo(buffer.AsSpan(position));
        Advance(data.Length);
    }

    /// <summary>
    /// Writes a nullable struct, prefixing it with a boolean indicating if a value is present.
    /// </summary>
    /// <typeparam name="T">The struct type.</typeparam>
    /// <param name="value">The nullable value to write.</param>
    public void WriteNullable<T>(T? value) where T : struct
    {
        var hasValue = value.HasValue;
        WriteBool(hasValue);

        if (hasValue)
            WriteStruct(value!.Value);
    }

    /// <summary>
    /// Writes a struct dynamically using cached delegates.
    /// </summary>
    /// <typeparam name="T">The struct type.</typeparam>
    /// <param name="value">The struct to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteStruct<T>(T value) where T : struct => PacketWriterDels.Struct<T>.Writer(this, value);

    /// <summary>
    /// Advances the write cursor forward by the specified amount, filling the skipped space with zeros.
    /// </summary>
    /// <param name="count">The number of bytes to move forward.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the count is negative.</exception>
    public override void MoveForward(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        EnsureCapacity(count);
        buffer.AsSpan(position, count).Clear();
        Advance(count);
    }

    /// <summary>
    /// Retreats the write cursor backward by the specified amount.
    /// </summary>
    /// <param name="count">The number of bytes to move backward.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the count is negative.</exception>
    /// <exception cref="InvalidOperationException">Thrown if moving backward exceeds the current position.</exception>
    public override void MoveBack(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        EnsureCapacity(0);

        if (position < count)
            throw new InvalidOperationException("New position cannot be negative.");

        position -= count;
    }

    /// <summary>
    /// Writes an enum value optimized with varint packing for its underlying type.
    /// </summary>
    /// <typeparam name="T">The type of the enum.</typeparam>
    /// <param name="value">The enum value to write.</param>
    /// <exception cref="ArgumentException">Thrown if the enum size is not supported.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePackedEnum<T>(T value) where T : struct, Enum => PacketWriterDels.PackedEnum<T>.Func(this, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<byte> WriteAlloc(int count)
    {
        EnsureCapacity(count);
        var span = buffer.AsSpan(position, count);
        Advance(count);
        return span;
    }

    /// <summary>
    /// Returns a ReadOnlySpan representing the valid written data in the buffer.
    /// </summary>
    /// <returns>A span containing the serialized data.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ToSpan()
    {
        EnsureCapacity(0);
        return buffer.AsSpan(0, size);
    }

    protected override void OnRecycle()
    {
        if (buffer != null)
            ArrayPool<byte>.Shared.Return(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void EnsureBounds(int count) => EnsureCapacity(count);

    /// <summary>
    /// Resets the writer with a newly rented byte array of the specified capacity.
    /// </summary>
    /// <param name="initialCapacity">The starting size of the buffer.</param>
    public void Reset(int initialCapacity = 256)
    {
        buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        Clear();
    }

    /// <summary>
    /// Detaches the current underlying buffer from the writer so it will not be returned to the ArrayPool on recycle.
    /// </summary>
    /// <param name="length">Outputs the total bytes written to the detached buffer.</param>
    /// <returns>The byte array containing the serialized data.</returns>
    public byte[] DetachBuffer(out int length)
    {
        EnsureCapacity(0);
        length = position;

        var detachedBuffer = buffer;

        buffer = null!;
        Clear();

        return detachedBuffer!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Advance(int count)
    {
        position += count;

        if (position > size)
            size = position;
    }

    /// <summary>
    /// Clears the writer state and resets the size tracking to 0.
    /// </summary>
    public override void Clear()
    {
        base.Clear();
        size = 0;
    }

    /// <summary>
    /// Borrows a <see cref="PacketWriter"/> instance from the recycle pool.
    /// </summary>
    /// <param name="initialCapacity">The starting capacity of the buffer.</param>
    /// <returns>A ready-to-use <see cref="PacketWriter"/>.</returns>
    public static PacketWriter Borrow(int initialCapacity = 256)
    {
        var writer = RecyclePool<PacketWriter>.Borrow();
        writer.Reset(initialCapacity);
        return writer;
    }

    /// <summary>
    /// Returns a <see cref="PacketWriter"/> instance to the recycle pool, returning its buffer to the shared array pool.
    /// </summary>
    /// <param name="writer">The writer to return.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(PacketWriter writer) => RecyclePool<PacketWriter>.Return(writer);

    private void WriteVarInt(ulong value, int maxSize)
    {
        EnsureCapacity(maxSize);

        while (value >= 0x80)
        {
            buffer![position++] = (byte)(value | 0x80);
            value >>= 7;
        }

        buffer![position++] = (byte)value;

        if (position > size)
            size = position;
    }
}

/// <summary>
/// Reusable cached delegates to improve performance, add more for data types as needed to avoid excess GC overhead.
/// </summary>
[PublicAPI]
public static class PacketWriterDels
{
    /// <summary>
    /// A delegate to write a byte.
    /// </summary>
    public static readonly Action<PacketWriter, byte> Byte = (writer, value) => writer.WriteByte(value);

    /// <summary>
    /// A delegate to write an sbyte.
    /// </summary>
    public static readonly Action<PacketWriter, sbyte> SByte = (writer, value) => writer.WriteSByte(value);

    /// <summary>
    /// A delegate to write a string.
    /// </summary>
    public static readonly Action<PacketWriter, string?> String = (writer, value) => writer.WriteString(value);

    /// <summary>
    /// A delegate to write a ushort.
    /// </summary>
    public static readonly Action<PacketWriter, ushort> UShort = (writer, value) => writer.WriteUShort(value);

    /// <summary>
    /// A delegate to write an Int32.
    /// </summary>
    public static readonly Action<PacketWriter, int> Int32 = (writer, value) => writer.WriteInt(value);

    /// <summary>
    /// A delegate to write a packed varint Int32.
    /// </summary>
    public static readonly Action<PacketWriter, int> PackedInt32 = (writer, value) => writer.WritePackedInt(value);

    /// <summary>
    /// Caches a writing delegate for types implementing INetObject.
    /// </summary>
    /// <typeparam name="T">The net object type.</typeparam>
    public static class NetObject<T> where T : INetObject
    {
        /// <summary>
        /// A delegate to write an INetObject.
        /// </summary>
        public static readonly Action<PacketWriter, T> Func = (writer, value) => value.Serialise(writer);
    }

    /// <summary>
    /// Caches a writing delegate for value Tuples.
    /// </summary>
    /// <typeparam name="T1">The first tuple element type.</typeparam>
    /// <typeparam name="T2">The second tuple element type.</typeparam>
    public static class Tuple<T1, T2>
    {
        /// <summary>
        /// A delegate to write a Tuple.
        /// </summary>
        public static readonly Action<PacketWriter, (T1, T2)> Func = CreateTupleWriter<(T1, T2)>(typeof(T1), typeof(T2));
    }

    /// <summary>
    /// Caches a dynamically generated writing delegate for custom structs.
    /// </summary>
    /// <typeparam name="T">The struct type.</typeparam>
    public static class Struct<T> where T : struct
    {
        /// <summary>
        /// A delegate to write a struct.
        /// </summary>
        public static readonly Action<PacketWriter, T> Writer = (Action<PacketWriter, T>)Delegate.CreateDelegate(typeof(Action<PacketWriter, T>), GetWriteExpression(typeof(T)));
    }

    /// <summary>
    /// Caches a writing delegate for Enum types.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    public static class Enum<T> where T : struct, Enum
    {
        /// <summary>
        /// A delegate to write an enum based on its underlying size.
        /// </summary>
        public static readonly Action<PacketWriter, T> Func = CreateWriter();

        private static Action<PacketWriter, T> CreateWriter()
        {
            var size = Unsafe.SizeOf<T>();
            return size switch
            {
                1 => (writer, value) => writer.WriteByte(Unsafe.As<T, byte>(ref value)),
                2 => (writer, value) => writer.WriteUShort(Unsafe.As<T, ushort>(ref value)),
                4 => (writer, value) => writer.WriteUInt(Unsafe.As<T, uint>(ref value)),
                8 => (writer, value) => writer.WriteULong(Unsafe.As<T, ulong>(ref value)),
                _ => throw new ArgumentException($"Enum size {size} not supported")
            };
        }
    }

    /// <summary>
    /// Caches a writing delegate for Enums using packed varint formats.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    public static class PackedEnum<T> where T : struct, Enum
    {
        /// <summary>
        /// A delegate to write a packed enum.
        /// </summary>
        public static readonly Action<PacketWriter, T> Func = CreateWriter();

        private static Action<PacketWriter, T> CreateWriter()
        {
            var size = Unsafe.SizeOf<T>();
            var underlying = Enum.GetUnderlyingType(typeof(T));
            return size switch
            {
                1 => (writer, value) => writer.WriteByte(Unsafe.As<T, byte>(ref value)),
                2 => (writer, value) => writer.WriteUShort(Unsafe.As<T, ushort>(ref value)),
                4 when underlying == typeof(int) => (writer, value) => writer.WritePackedInt(Unsafe.As<T, int>(ref value)),
                4 => (writer, value) => writer.WritePackedUInt(Unsafe.As<T, uint>(ref value)),
                8 when underlying == typeof(long) => (writer, value) => writer.WritePackedLong(Unsafe.As<T, long>(ref value)),
                8 => (writer, value) => writer.WritePackedULong(Unsafe.As<T, ulong>(ref value)),
                _ => throw new ArgumentException($"Enum size {size} not supported")
            };
        }
    }

    private static readonly ConcurrentDictionary<Type, MethodInfo> TypeWriteCache = new();

    private static readonly ReadOnlyDictionary<Type, string> WriteMethodMap = new(new Dictionary<Type, string>()
    {
        [typeof(byte)] = nameof(PacketWriter.WriteByte),
        [typeof(int)] = nameof(PacketWriter.WriteInt),
        [typeof(uint)] = nameof(PacketWriter.WriteUInt),
        [typeof(long)] = nameof(PacketWriter.WriteLong),
        [typeof(bool)] = nameof(PacketWriter.WriteBool),
        [typeof(short)] = nameof(PacketWriter.WriteShort),
        [typeof(ulong)] = nameof(PacketWriter.WriteULong),
        [typeof(sbyte)] = nameof(PacketWriter.WriteSByte),
        [typeof(float)] = nameof(PacketWriter.WriteFloat),
        [typeof(ushort)] = nameof(PacketWriter.WriteUShort),
        [typeof(double)] = nameof(PacketWriter.WriteDouble),
        [typeof(string)] = nameof(PacketWriter.WriteString),
        [typeof(float4)] = nameof(PacketWriter.WriteFloat4),
        [typeof(Vector3)] = nameof(PacketWriter.WriteVector3),
        [typeof(Quaternion)] = nameof(PacketWriter.WriteQuaternion),
    });

    /// <summary>
    /// Generates a writer function for custom tuples based on component types. The type parameters should match in order and be surrounded by brackets.
    /// </summary>
    /// <typeparam name="TTuple">The tuple type.</typeparam>
    /// <param name="componentTypes">The types making up the tuple.</param>
    /// <returns>A compiled action to write the tuple.</returns>
    public static Action<PacketWriter, TTuple> CreateTupleWriter<TTuple>(params Type[] componentTypes)
    {
        var writerParam = Expression.Parameter(typeof(PacketWriter), "writer");
        var tupleParam = Expression.Parameter(typeof(TTuple), "value");

        var writeCalls = new Expression[componentTypes.Length];

        for (var i = 0; i < componentTypes.Length; i++)
            writeCalls[i] = Expression.Call(writerParam, GetWriteExpression(componentTypes[i]), Expression.Field(tupleParam, $"Item{i + 1}"));

        var block = Expression.Block(writeCalls);
        return Expression.Lambda<Action<PacketWriter, TTuple>>(block, writerParam, tupleParam).Compile();
    }

    private static MethodInfo GetWriteExpression(Type type)
    {
        if (TypeWriteCache.TryGetValue(type, out var method))
            return method;

        if (WriteMethodMap.TryGetValue(type, out var methodName))
            method = Method(methodName);
        else if (type.IsEnum)
            method = Method(nameof(PacketWriter.WriteEnum)).MakeGenericMethod(type);
        else if (typeof(IPacket).IsAssignableFrom(type))
            method = Method(nameof(PacketWriter.WritePacket)).MakeGenericMethod(type);
        else if (typeof(ICustomPacket).IsAssignableFrom(type))
            method = Method(nameof(PacketWriter.WriteCustomPacket)).MakeGenericMethod(type);
        else if (typeof(INetObject).IsAssignableFrom(type))
            method = Method(nameof(PacketWriter.WriteNetObject)).MakeGenericMethod(type);

        if (method == null)
            throw new NotSupportedException($"Type {type.Name} is not supported in automatic serialization.");

        TypeWriteCache[type] = method;
        return method;
    }

    private static MethodInfo Method(string name) =>
        typeof(PacketWriter).GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new MissingMethodException($"PacketWriter missing method: {name}");
}