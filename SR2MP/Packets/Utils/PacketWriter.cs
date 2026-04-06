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
/// A reusable writer for packing data into a byte buffer for network transmission.
/// </summary>
[PublicAPI]
public sealed class PacketWriter : PacketBuffer
{
    private int size;

    /// <summary>
    /// Gets the total size of the data written to this buffer.
    /// </summary>
    // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
    public override int DataSize => size;

    /// <summary>
    /// Initialises a new instance of the <see cref="PacketWriter"/> class.
    /// </summary>
    [Obsolete("Use PacketWriter.Borrow instead!", true)]
    public PacketWriter() : base(0) { }

    /// <summary>
    /// Ensures that the writer has enough space to write the provided number of bytes.
    /// </summary>
    /// <param name="bytesToAdd">The number of bytes to add.</param>
    /// <exception cref="InvalidOperationException">Thrown if the writer is recycled or detached.</exception>
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
    /// Writes a byte.
    /// </summary>
    /// <param name="value">The byte to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value) => WriteAlloc(1)[0] = value;

    /// <summary>
    /// Writes a boolean.
    /// </summary>
    /// <param name="value">The boolean to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value) => WriteByte((byte)(value ? 1 : 0));

    /// <summary>
    /// Writes an sbyte.
    /// </summary>
    /// <param name="value">The sbyte to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSByte(sbyte value) => WriteByte((byte)value);

    /// <summary>
    /// Writes a short.
    /// </summary>
    /// <param name="value">The short to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteShort(short value) => BinaryPrimitives.WriteInt16LittleEndian(WriteAlloc(2), value);

    /// <summary>
    /// Writes a ushort.
    /// </summary>
    /// <param name="value">The ushort to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUShort(ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(WriteAlloc(2), value);

    /// <summary>
    /// Writes an int.
    /// </summary>
    /// <param name="value">The int to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt(int value) => BinaryPrimitives.WriteInt32LittleEndian(WriteAlloc(4), value);

    /// <summary>
    /// Writes a uint.
    /// </summary>
    /// <param name="value">The uint to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt(uint value) => BinaryPrimitives.WriteUInt32LittleEndian(WriteAlloc(4), value);

    /// <summary>
    /// Writes a float.
    /// </summary>
    /// <param name="value">The float to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat(float value) => BinaryPrimitives.WriteSingleLittleEndian(WriteAlloc(4), value);

    /// <summary>
    /// Writes a long.
    /// </summary>
    /// <param name="value">The long to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLong(long value) => BinaryPrimitives.WriteInt64LittleEndian(WriteAlloc(8), value);

    /// <summary>
    /// Writes a ulong.
    /// </summary>
    /// <param name="value">The ulong to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteULong(ulong value) => BinaryPrimitives.WriteUInt64LittleEndian(WriteAlloc(8), value);

    /// <summary>
    /// Writes a double.
    /// </summary>
    /// <param name="value">The double to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDouble(double value) => BinaryPrimitives.WriteDoubleLittleEndian(WriteAlloc(8), value);

    private void WriteFloats(ReadOnlySpan<float> values)
    {
        var span = WriteAlloc(values.Length * 4);

        for (var i = 0; i < values.Length; i++)
            BinaryPrimitives.WriteSingleLittleEndian(span[(i * 4)..], values[i]);
    }

    /// <summary>
    /// Writes a Vector2.
    /// </summary>
    /// <param name="value">The Vector2 to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVector2(Vector2 value) => WriteFloats(stackalloc float[2] { value.x, value.y });

    /// <summary>
    /// Writes a Vector3.
    /// </summary>
    /// <param name="value">The Vector3 to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVector3(Vector3 value) => WriteFloats(stackalloc float[3] { value.x, value.y, value.z });

    /// <summary>
    /// Writes a Quaternion.
    /// </summary>
    /// <param name="value">The Quaternion to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteQuaternion(Quaternion value) => WriteFloats(stackalloc float[4] { value.x, value.y, value.z, value.w });

    /// <summary>
    /// Writes a float4.
    /// </summary>
    /// <param name="value">The float4 to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat4(float4 value) => WriteFloats(stackalloc float[4] { value.x, value.y, value.z, value.w });

    /// <summary>
    /// Writes an enum value.
    /// </summary>
    /// <typeparam name="T">The type of the enum.</typeparam>
    /// <param name="value">The enum value to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteEnum<T>(T value) where T : struct, Enum => PacketWriterDels.Enum<T>.Writer(this, value);

    /// <summary>
    /// Writes an enum value by its string representation.
    /// </summary>
    /// <typeparam name="T">The type of the enum.</typeparam>
    /// <param name="value">The enum value to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteEnumAsString<T>(T value) where T : struct, Enum => WriteString(value.ToString());

    /// <summary>
    /// Writes an object that implements <see cref="INetObject"/>.
    /// </summary>
    /// <typeparam name="T">The type of the network object.</typeparam>
    /// <param name="value">The network object to serialise.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteNetObject<T>(T value) where T : INetObject => value.Serialise(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WritePacket<T>(T value) where T : IPacket
    {
        WriteByte((byte)value.Type);
        value.Serialise(this);
    }

    /// <summary>
    /// Writes a packet that implements <see cref="ICustomPacket"/>.
    /// </summary>
    /// <typeparam name="T">The type of the custom packet.</typeparam>
    /// <param name="value">The custom packet to serialise.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteCustomPacket<T>(T value) where T : ICustomPacket
    {
        WriteByte(value.PacketHeader);
        value.Serialise(this);
    }

    /// <summary>
    /// Writes a packed int.
    /// </summary>
    /// <param name="value">The int to pack and write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePackedInt(int value) => WritePackedUInt((uint)((value << 1) ^ (value >> 31)));

    /// <summary>
    /// Writes a packed uint.
    /// </summary>
    /// <param name="value">The uint to pack and write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePackedUInt(uint value) => WriteVarInt(value, 5);

    /// <summary>
    /// Writes a packed long.
    /// </summary>
    /// <param name="value">The long to pack and write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePackedLong(long value) => WritePackedULong((ulong)((value << 1) ^ (value >> 63)));

    /// <summary>
    /// Writes a packed ulong.
    /// </summary>
    /// <param name="value">The ulong to pack and write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePackedULong(ulong value) => WriteVarInt(value, 10);

    /// <summary>
    /// Writes a string prefixed by its length. Null or empty strings write a length of 0.
    /// </summary>
    /// <param name="value">The string to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    /// <inheritdoc cref="WriteCount"/>
    public void WriteString(string? value, CountType countType = CountType.UShort)
    {
        if (string.IsNullOrEmpty(value))
        {
            WriteCount(0, countType);
            return;
        }

        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);

        if (countType != CountType.VarUInt)
        {
            var headerSize = GetFixedCountHeaderSize(countType);

            EnsureCapacity(headerSize + maxByteCount);

            var headerIndex = position;
            Advance(headerSize);

            var actualCount = Encoding.UTF8.GetBytes(value.AsSpan(), buffer.AsSpan(position));
            BackfillCount(headerIndex, actualCount, countType);
            Advance(actualCount);
            return;
        }

        const int maxHeaderSize = 5;

        EnsureCapacity(maxHeaderSize + maxByteCount);

        var innerHeaderIndex = position;
        var stringStartIndex = innerHeaderIndex + maxHeaderSize;
        var actualStringBytes = Encoding.UTF8.GetBytes(value.AsSpan(), buffer.AsSpan(stringStartIndex));
        var actualHeaderSize = GetVarUIntSize((uint)actualStringBytes);
        var gap = maxHeaderSize - actualHeaderSize;

        if (gap > 0)
        {
            var sourceSpan = buffer.AsSpan(stringStartIndex, actualStringBytes);
            var destSpan = buffer.AsSpan(innerHeaderIndex + actualHeaderSize, actualStringBytes);

            sourceSpan.CopyTo(destSpan);
        }

        WriteVarUIntToSpan((uint)actualStringBytes, buffer.AsSpan(innerHeaderIndex));

        position = innerHeaderIndex + actualHeaderSize + actualStringBytes;

        if (position > size)
            size = position;
    }

    /// <summary>
    /// Writes a string without prefixing its length.
    /// </summary>
    /// <param name="value">The string to write.</param>
    /// <exception cref="ArgumentException">Thrown if the provided string is null or empty.</exception>
    /// <inheritdoc cref="EnsureCapacity"/>
    public void WriteStringWithoutSize(string? value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("Value cannot be null or empty");

        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        EnsureCapacity(maxByteCount);
        Advance(Encoding.UTF8.GetBytes(value.AsSpan(), buffer.AsSpan(position)));
    }

    // ReSharper disable InvalidXmlDocComment

    /// <summary>
    /// Writes a collection with a configurable count header.
    /// </summary>
    /// <inheritdoc cref="WriteCount"/>
    /// <inheritdoc cref="WriteCollectionWithoutSize"/>
    private void WriteCollection<T>(int count, IEnumerable<T> items, Action<PacketWriter, T> writer, CountType countType)
    {
        WriteCount(count, countType);

        foreach (var item in items)
            writer(this, item);
    }

    /// <summary>
    /// Writes a collection without serialising its count first.
    /// </summary>
    /// <param name="items">The sequence of items to serialise.</param>
    /// <param name="writer">The delegate used to write each element.</param>
    /// <typeparam name="T">The element type.</typeparam>
    /// <inheritdoc cref="EnsureCapacity"/>
    /// <exception cref="InvalidDataException">Thrown if the collection is null or empty.</exception>
    private void WriteCollectionWithoutSize<T>(IEnumerable<T>? items, Action<PacketWriter, T> writer)
    {
        // ReSharper disable once PossibleMultipleEnumeration
        if (items.IsNullOrEmpty())
            throw new InvalidDataException("Collection cannot be null or empty.");

        // ReSharper disable once PossibleMultipleEnumeration
        foreach (var item in items!)
            writer(this, item);
    }

    /// <summary>
    /// Writes an array prefixed by its length. Null or empty arrays write a length of 0.
    /// </summary>
    /// <param name="array">The array to write.</param>
    /// <inheritdoc cref="WriteCollection"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteArray<T>(T[]? array, Action<PacketWriter, T> writer, CountType countType = CountType.UShort)
        => WriteCollection(array?.Length ?? 0, array ?? Enumerable.Empty<T>(), writer, countType);

    /// <summary>
    /// Writes an array without prefixing its length.
    /// </summary>
    /// <param name="array">The array to write.</param>
    /// <inheritdoc cref="WriteCollectionWithoutSize"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteArrayWithoutSize<T>(T[]? array, Action<PacketWriter, T> writer)
        => WriteCollectionWithoutSize(array, writer);

    /// <summary>
    /// Writes a list prefixed by its length. Null or empty lists write a length of 0.
    /// </summary>
    /// <param name="list">The list to write.</param>
    /// <inheritdoc cref="WriteCollection"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteList<T>(List<T>? list, Action<PacketWriter, T> writer, CountType countType = CountType.UShort)
        => WriteCollection(list?.Count ?? 0, list ?? Enumerable.Empty<T>(), writer, countType);

    /// <summary>
    /// Writes a list without prefixing its length.
    /// </summary>
    /// <param name="list">The list to write.</param>
    /// <inheritdoc cref="WriteCollectionWithoutSize"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteListWithoutSize<T>(List<T>? list, Action<PacketWriter, T> writer)
        => WriteCollectionWithoutSize(list, writer);

    /// <summary>
    /// Writes a hash set prefixed by its length. Null or empty sets write a length of 0.
    /// </summary>
    /// <param name="set">The hash set to write.</param>
    /// <inheritdoc cref="WriteCollection"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteHashSet<T>(HashSet<T>? set, Action<PacketWriter, T> writer, CountType countType = CountType.UShort)
        => WriteCollection(set?.Count ?? 0, set ?? Enumerable.Empty<T>(), writer, countType);

    /// <summary>
    /// Writes a hash set without prefixing its length.
    /// </summary>
    /// <param name="set">The hash set to write.</param>
    /// <inheritdoc cref="WriteCollectionWithoutSize"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteHashSetWithoutSize<T>(HashSet<T>? set, Action<PacketWriter, T> writer)
        => WriteCollectionWithoutSize(set, writer);

    /// <summary>
    /// Writes an il2cpp hash set prefixed by its length. Null or empty sets write a length of 0.
    /// </summary>
    /// <param name="set">The il2cpp hash set to write.</param>
    /// <inheritdoc cref="WriteCollection"/>
    public void WriteCppHashSet<T>(CppCollections.HashSet<T>? set, Action<PacketWriter, T> writer, CountType countType = CountType.UShort)
    {
        if (set == null)
        {
            WriteCount(0, countType);
            return;
        }

        WriteCount(set.Count, countType);

        foreach (var item in set)
            writer(this, item);
    }

    /// <summary>
    /// Writes an il2cpp hash set without prefixing its length.
    /// </summary>
    /// <param name="set">The il2cpp hash set to write.</param>
    /// <inheritdoc cref="WriteCollectionWithoutSize"/>
    public void WriteCppHashSetWithoutSize<T>(CppCollections.HashSet<T>? set, Action<PacketWriter, T> writer)
    {
        if (set == null || set.Count == 0)
            throw new InvalidDataException("Collection cannot be null or empty.");

        foreach (var item in set)
            writer(this, item);
    }

    /// <summary>
    /// Writes a dictionary prefixed by its length. Null or empty dictionaries write a length of 0.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    /// <param name="dict">The dictionary to write.</param>
    /// <param name="keyWriter">The delegate used to write keys.</param>
    /// <param name="valueWriter">The delegate used to write values.</param>
    /// <param name="countType">The header type used to store dictionary length.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    /// <inheritdoc cref="WriteCount"/>
    public void WriteDictionary<TKey, TValue>(Dictionary<TKey, TValue>? dict, Action<PacketWriter, TKey> keyWriter, Action<PacketWriter, TValue> valueWriter, CountType countType = CountType.UShort) where TKey : notnull
    {
        WriteCount(dict?.Count ?? 0, countType);

        if (dict == null)
            return;

        foreach (var (key, value) in dict)
        {
            keyWriter(this, key);
            valueWriter(this, value);
        }
    }

    /// <summary>
    /// Writes a dictionary without prefixing its length.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    /// <param name="dict">The dictionary to write.</param>
    /// <param name="keyWriter">The delegate used to write keys.</param>
    /// <param name="valueWriter">The delegate used to write values.</param>
    /// <exception cref="InvalidDataException">Thrown if the provided dictionary is null or empty.</exception>
    /// <inheritdoc cref="EnsureCapacity"/>
    public void WriteDictionaryWithoutSize<TKey, TValue>(Dictionary<TKey, TValue>? dict, Action<PacketWriter, TKey> keyWriter, Action<PacketWriter, TValue> valueWriter) where TKey : notnull
    {
        if (dict == null || dict.Count == 0)
            throw new InvalidDataException("Collection cannot be null or empty.");

        foreach (var (key, value) in dict)
        {
            keyWriter(this, key);
            valueWriter(this, value);
        }
    }

    // ReSharper enable InvalidXmlDocComment

    /// <summary>
    /// Writes a packed boolean value.
    /// </summary>
    /// <param name="value">The boolean to pack and write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    public void WritePackedBool(bool value)
    {
        if (value)
            currentPackedByte |= (byte)(1 << currentBitIndex);

        currentBitIndex++;

        if (currentBitIndex == 8)
            EnsureCapacity(0);
    }

    /// <inheritdoc/>
    /// <inheritdoc cref="EnsureCapacity"/>
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
    /// Writes a span of bytes.
    /// </summary>
    /// <param name="data">The byte span to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSpan(Span<byte> data)
    {
        if (data.IsEmpty)
            return;

        EnsureCapacity(data.Length);
        data.CopyTo(buffer.AsSpan(position));
        Advance(data.Length);
    }

    /// <summary>
    /// Writes a nullable value.
    /// </summary>
    /// <typeparam name="T">The value's type.</typeparam>
    /// <param name="value">The nullable value to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    public void WriteNullable<T>(T? value)
    {
        var hasValue = value != null;
        WriteBool(hasValue);

        if (hasValue)
            WriteObject(value!);
    }

    /// <summary>
    /// Writes a tuple.
    /// </summary>
    /// <typeparam name="T">The tuple type.</typeparam>
    /// <param name="value">The tuple to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteTuple<T>(T value) where T : struct, ITuple => PacketWriterDels.Tuple<T>.Writer(this, value);

    /// <summary>
    /// Writes a generic value.
    /// </summary>
    /// <typeparam name="T">The object type.</typeparam>
    /// <param name="value">The value to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteObject<T>(T value) => PacketWriterDels.Object<T>.Writer(this, value);

    /// <inheritdoc/>
    public override void MoveForward(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        EnsureCapacity(count);
        buffer.AsSpan(position, count).Clear();
        Advance(count);
    }

    /// <inheritdoc/>
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
    /// Writes a packed enum value.
    /// </summary>
    /// <typeparam name="T">The type of the enum.</typeparam>
    /// <param name="value">The enum value to pack and write.</param>
    /// <exception cref="ArgumentException">Thrown if the enum size is not supported.</exception>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePackedEnum<T>(T value) where T : struct, Enum => PacketWriterDels.PackedEnum<T>.Writer(this, value);

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
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ToSpan()
    {
        EnsureCapacity(0);
        return buffer.AsSpan(0, size);
    }

    /// <inheritdoc/>
    protected override void OnRecycle()
    {
        if (buffer != null)
            ArrayPool<byte>.Shared.Return(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void EnsureBounds(int count) => EnsureCapacity(count);

    private void Reset(int initialCapacity = 256)
    {
        buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        Clear();
    }

    /// <summary>
    /// Detaches the current underlying buffer from the writer so it will not be returned to the ArrayPool on recycle.
    /// </summary>
    /// <param name="length">Outputs the total bytes written to the detached buffer.</param>
    /// <returns>The byte array containing the serialized data.</returns>
    /// <remarks>Once this method completes, it is YOUR responsibility to return the buffer to the pool!</remarks>
    /// <inheritdoc cref="EnsureCapacity"/>
    public byte[] DetachBuffer(out int length)
    {
        EnsureCapacity(0);
        length = position;

        var detachedBuffer = buffer;

        buffer = null;
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

    /// <inheritdoc/>
    public override void Clear()
    {
        base.Clear();
        size = 0;
    }

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

    /// <summary>
    /// Writes a configurable count value.
    /// </summary>
    /// <param name="count">The count.</param>
    /// <param name="countType">The method at which the count will be written.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the count is out of bounds of the configuration or if an unsupported configuration is passed.</exception>
    private void WriteCount(int count, CountType countType)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        switch (countType)
        {
            case CountType.Byte:
            {
                if (count > byte.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(count), $"Count {count} exceeds Byte max {byte.MaxValue}.");

                WriteByte((byte)count);
                return;
            }
            case CountType.UShort:
            {
                if (count > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(count), $"Count {count} exceeds UShort max {ushort.MaxValue}.");

                WriteUShort((ushort)count);
                return;
            }
            case CountType.VarUInt:
            {
                WritePackedUInt((uint)count);
                return;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(countType), countType, "Unsupported count type.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetFixedCountHeaderSize(CountType countType) => countType switch
    {
        CountType.Byte => 1,
        CountType.UShort => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(countType), countType, "CountType is not a fixed size.")
    };

    private void BackfillCount(int index, int count, CountType countType)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        var span = buffer.AsSpan(index);

        switch (countType)
        {
            case CountType.Byte:
            {
                if (count > byte.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(count), $"Count {count} exceeds Byte max {byte.MaxValue}.");

                span[0] = (byte)count;
                break;
            }
            case CountType.UShort:
            {
                if (count > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(count), $"Count {count} exceeds UShort max {ushort.MaxValue}.");

                BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)count);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(countType), countType, "Unsupported count type.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetVarUIntSize(uint value) => value switch
    {
        < 0x80 => 1,
        < 0x4000 => 2,
        < 0x200000 => 3,
        < 0x10000000 => 4,
        _ => 5
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteVarUIntToSpan(uint value, Span<byte> target)
    {
        var index = 0;

        while (value >= 0x80)
        {
            target[index++] = (byte)(value | 0x80);
            value >>= 7;
        }

        target[index] = (byte)value;
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
    /// A delegate to write an int.
    /// </summary>
    public static readonly Action<PacketWriter, int> Int = (writer, value) => writer.WriteInt(value);

    /// <summary>
    /// A delegate to write a packed int.
    /// </summary>
    public static readonly Action<PacketWriter, int> PackedInt = (writer, value) => writer.WritePackedInt(value);

    /// <summary>
    /// A delegate to write a boolean.
    /// </summary>
    public static readonly Action<PacketWriter, bool> Bool = (writer, value) => writer.WriteBool(value);

    /// <summary>
    /// A delegate to write a short.
    /// </summary>
    public static readonly Action<PacketWriter, short> Short = (writer, value) => writer.WriteShort(value);

    /// <summary>
    /// A delegate to write a uint.
    /// </summary>
    public static readonly Action<PacketWriter, uint> UInt = (writer, value) => writer.WriteUInt(value);

    /// <summary>
    /// A delegate to write a long.
    /// </summary>
    public static readonly Action<PacketWriter, long> Long = (writer, value) => writer.WriteLong(value);

    /// <summary>
    /// A delegate to write a ulong.
    /// </summary>
    public static readonly Action<PacketWriter, ulong> ULong = (writer, value) => writer.WriteULong(value);

    /// <summary>
    /// A delegate to write a float.
    /// </summary>
    public static readonly Action<PacketWriter, float> Float = (writer, value) => writer.WriteFloat(value);

    /// <summary>
    /// A delegate to write a double.
    /// </summary>
    public static readonly Action<PacketWriter, double> Double = (writer, value) => writer.WriteDouble(value);

    /// <summary>
    /// A delegate to write a packed uint.
    /// </summary>
    public static readonly Action<PacketWriter, uint> PackedUInt = (writer, value) => writer.WritePackedUInt(value);

    /// <summary>
    /// A delegate to write a packed long.
    /// </summary>
    public static readonly Action<PacketWriter, long> PackedLong = (writer, value) => writer.WritePackedLong(value);

    /// <summary>
    /// A delegate to write a packed ulong.
    /// </summary>
    public static readonly Action<PacketWriter, ulong> PackedULong = (writer, value) => writer.WritePackedULong(value);

    /// <summary>
    /// A delegate to write a packed bool.
    /// </summary>
    public static readonly Action<PacketWriter, bool> PackedBool = (writer, value) => writer.WritePackedBool(value);

    /// <summary>
    /// A delegate to write a Vector2.
    /// </summary>
    public static readonly Action<PacketWriter, Vector2> Vector2 = (writer, value) => writer.WriteVector2(value);

    /// <summary>
    /// A delegate to write a Vector3.
    /// </summary>
    public static readonly Action<PacketWriter, Vector3> Vector3 = (writer, value) => writer.WriteVector3(value);

    /// <summary>
    /// A delegate to write a Quaternion.
    /// </summary>
    public static readonly Action<PacketWriter, Quaternion> Quaternion = (writer, value) => writer.WriteQuaternion(value);

    /// <summary>
    /// A delegate to write a float4.
    /// </summary>
    public static readonly Action<PacketWriter, float4> Float4 = (writer, value) => writer.WriteFloat4(value);

    /// <summary>
    /// Caches a writing delegate for types implementing <see cref="INetObject"/>.
    /// </summary>
    /// <typeparam name="T">The net object type.</typeparam>
    public static class NetObject<T> where T : INetObject
    {
        /// <summary>
        /// A delegate to write an <see cref="INetObject"/>.
        /// </summary>
        public static readonly Action<PacketWriter, T> Writer = (writer, value) => value.Serialise(writer);
    }

    /// <summary>
    /// Caches a writing delegate for optional values (nullables).
    /// </summary>
    /// <typeparam name="T">The object type.</typeparam>
    public static class Nullable<T>
    {
        /// <summary>
        /// A delegate to write an optional value.
        /// </summary>
        public static readonly Action<PacketWriter, T?> Writer = (writer, value) => writer.WriteNullable(value);
    }

    /// <summary>
    /// Caches a writing delegate for value <see cref="Tuple"/>s.
    /// </summary>
    /// <typeparam name="T">The tuple type.</typeparam>
    public static class Tuple<T> where T : struct, ITuple
    {
        /// <summary>
        /// A delegate to write a <see cref="Tuple"/>.
        /// </summary>
        public static readonly Action<PacketWriter, T> Writer = CreateWriter();

        private static Action<PacketWriter, T> CreateWriter()
        {
            var writerParam = Expression.Parameter(typeof(PacketWriter), "writer");
            var tupleParam = Expression.Parameter(typeof(T), "value");

            var componentTypes = typeof(T).GetGenericArguments();
            var writeCalls = new Expression[componentTypes.Length];
            var writeObjectMethodDef = Method(nameof(PacketWriter.WriteObject));

            for (var i = 0; i < componentTypes.Length; i++)
            {
                var fieldAccess = Expression.Field(tupleParam, $"Item{i + 1}");
                var genericWrite = writeObjectMethodDef.MakeGenericMethod(componentTypes[i]);
                writeCalls[i] = Expression.Call(writerParam, genericWrite, fieldAccess);
            }

            var block = Expression.Block(writeCalls);
            return Expression.Lambda<Action<PacketWriter, T>>(block, writerParam, tupleParam).Compile();
        }
    }

    /// <summary>
    /// Caches a writing delegate for custom objects that aren't natively supported.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    public static class Object<T>
    {
        /// <summary>
        /// A delegate to write a value.
        /// </summary>
        public static Action<PacketWriter, T> Writer { get; internal set; } = CreateWriter();

        private static Action<PacketWriter, T> CreateWriter()
        {
            try
            {
                return (Action<PacketWriter, T>)Delegate.CreateDelegate(typeof(Action<PacketWriter, T>), GetWriteExpression(typeof(T)));
            }
            catch
            {
                return (_, _) => throw new NotImplementedException($"Type {typeof(T).Name} is not supported natively. Did you forget to register it?");
            }
        }
    }

    /// <summary>
    /// Caches a writing delegate for <see cref="Enum"/> types.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    public static class Enum<T> where T : struct, Enum
    {
        /// <summary>
        /// A delegate to write an enum value.
        /// </summary>
        public static readonly Action<PacketWriter, T> Writer = CreateWriter();

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
    /// Caches a writing delegate for <see cref="Enum"/> types in a packed format.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    public static class PackedEnum<T> where T : struct, Enum
    {
        /// <summary>
        /// A delegate to write a packed enum value.
        /// </summary>
        public static readonly Action<PacketWriter, T> Writer = CreateWriter();

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

    private static readonly ReadOnlyDictionary<Type, string> WriteMethodMap = new(new ConcurrentDictionary<Type, string>()
    {
        [typeof(int)] = nameof(PacketWriter.WriteInt),
        [typeof(byte)] = nameof(PacketWriter.WriteByte),
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

    private static MethodInfo GetWriteExpression(Type type)
    {
        if (TypeWriteCache.TryGetValue(type, out var method))
            return method;

        if (WriteMethodMap.TryGetValue(type, out var methodName))
            method = Method(methodName);
        else if (type.IsEnum)
            method = Method(nameof(PacketWriter.WriteEnum)).MakeGenericMethod(type);
        else if (typeof(ITuple).IsAssignableFrom(type) && type.IsValueType)
            method = Method(nameof(PacketWriter.WriteTuple)).MakeGenericMethod(type);
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