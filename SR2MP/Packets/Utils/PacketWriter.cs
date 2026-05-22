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
using System.Runtime.InteropServices;

namespace SR2MP.Packets.Utils;

/// <summary>
/// A reusable writer for packing data into a byte buffer for network transmission.
/// </summary>
[PublicApi]
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

    /// <summary>
    /// Writes a decimal.
    /// </summary>
    /// <param name="value">The decimal to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDecimal(decimal value) => MemoryMarshal.Write(WriteAlloc(16), ref value);

    /// <summary>
    /// Writes a Half.
    /// </summary>
    /// <param name="value">The Half to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteHalf(Half value) => BinaryPrimitives.WriteHalfLittleEndian(WriteAlloc(2), value);

    /// <summary>
    /// Writes a char.
    /// </summary>
    /// <param name="value">The char to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteChar(char value) => BinaryPrimitives.WriteUInt16LittleEndian(WriteAlloc(2), value);

    /// <summary>
    /// Writes a Color32.
    /// </summary>
    /// <param name="value">The Color32 to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    public void WriteColor32(Color32 value)
    {
        var span = WriteAlloc(4);
        span[0] = value.r;
        span[1] = value.g;
        span[2] = value.b;
        span[3] = value.a;
    }

    /// <summary>
    /// Writes a span of floats.
    /// </summary>
    /// <param name="values">The span to write.</param>
    public void WriteFloats(ReadOnlySpan<float> values)
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
    /// Writes a Color.
    /// </summary>
    /// <param name="value">The Color to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteColor(Color value) => WriteFloats(stackalloc float[4] { value.r, value.g, value.b, value.a });

    /// <summary>
    /// Writes a DateTime.
    /// </summary>
    /// <param name="value">The DateTime to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    public void WriteDateTime(DateTime value) => WriteLong(value.Ticks);

    /// <summary>
    /// Writes a TimeSpan.
    /// </summary>
    /// <param name="value">The TimeSpan to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    public void WriteTimeSpan(TimeSpan value) => WriteLong(value.Ticks);

    /// <summary>
    /// Writes a Guid.
    /// </summary>
    /// <param name="value">The Guid to write.</param>
    /// <inheritdoc cref="EnsureCapacity"/>
    public void WriteGuid(Guid value) => _ = value.TryWriteBytes(WriteAlloc(16));

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

    // ReSharper disable InvalidXmlDocComment
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

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

        var charSpan = value.AsSpan();
        var actualByteCount = Encoding.UTF8.GetByteCount(charSpan);

        var headerSize = countType == CountType.VarUInt
            ? GetVarUIntSize((uint)actualByteCount)
            : GetFixedCountHeaderSize(countType);

        EnsureCapacity(headerSize + actualByteCount);

        WriteCountWithoutEnsuring(actualByteCount, countType);

        Encoding.UTF8.GetBytes(charSpan, buffer.AsSpan(position));
        Advance(actualByteCount);
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

    /// <summary>
    /// Writes a collection with a configurable count header.
    /// </summary>
    /// <inheritdoc cref="WriteCount"/>
    /// <inheritdoc cref="WriteCollectionWithoutSize"/>
    private void WriteCollection<T>(int count, IEnumerable<T> items, WriteDel<T> writer, CountType countType)
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
    private void WriteCollectionWithoutSize<T>(IEnumerable<T>? items, WriteDel<T> writer)
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
    public void WriteArray<T>(T[]? array, WriteDel<T> writer, CountType countType = CountType.UShort)
        => WriteCollection(array?.Length ?? 0, array ?? Enumerable.Empty<T>(), writer, countType);

    /// <summary>
    /// Writes an array without prefixing its length.
    /// </summary>
    /// <param name="array">The array to write.</param>
    /// <inheritdoc cref="WriteCollectionWithoutSize"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteArrayWithoutSize<T>(T[]? array, WriteDel<T> writer)
        => WriteCollectionWithoutSize(array, writer);

    /// <summary>
    /// Writes a list prefixed by its length. Null or empty lists write a length of 0.
    /// </summary>
    /// <param name="list">The list to write.</param>
    /// <inheritdoc cref="WriteCollection"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteList<T>(List<T>? list, WriteDel<T> writer, CountType countType = CountType.UShort)
        => WriteCollection(list?.Count ?? 0, list ?? Enumerable.Empty<T>(), writer, countType);

    /// <summary>
    /// Writes a list without prefixing its length.
    /// </summary>
    /// <param name="list">The list to write.</param>
    /// <inheritdoc cref="WriteCollectionWithoutSize"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteListWithoutSize<T>(List<T>? list, WriteDel<T> writer)
        => WriteCollectionWithoutSize(list, writer);

    /// <summary>
    /// Writes a hash set prefixed by its length. Null or empty sets write a length of 0.
    /// </summary>
    /// <param name="set">The hash set to write.</param>
    /// <inheritdoc cref="WriteCollection"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteHashSet<T>(HashSet<T>? set, WriteDel<T> writer, CountType countType = CountType.UShort)
        => WriteCollection(set?.Count ?? 0, set ?? Enumerable.Empty<T>(), writer, countType);

    /// <summary>
    /// Writes a hash set without prefixing its length.
    /// </summary>
    /// <param name="set">The hash set to write.</param>
    /// <inheritdoc cref="WriteCollectionWithoutSize"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteHashSetWithoutSize<T>(HashSet<T>? set, WriteDel<T> writer)
        => WriteCollectionWithoutSize(set, writer);

    /// <summary>
    /// Writes an il2cpp hash set prefixed by its length. Null or empty sets write a length of 0.
    /// </summary>
    /// <param name="set">The il2cpp hash set to write.</param>
    /// <inheritdoc cref="WriteCollection"/>
    public void WriteCppHashSet<T>(CppCollections.HashSet<T>? set, WriteDel<T> writer, CountType countType = CountType.UShort)
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
    public void WriteCppHashSetWithoutSize<T>(CppCollections.HashSet<T>? set, WriteDel<T> writer)
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
    public void WriteDictionary<TKey, TValue>(Dictionary<TKey, TValue>? dict, WriteDel<TKey> keyWriter, WriteDel<TValue> valueWriter, CountType countType = CountType.UShort) where TKey : notnull
    {
        if (dict == null)
        {
            WriteCount(0, countType);
            return;
        }

        WriteCount(dict.Count, countType);

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
    public void WriteDictionaryWithoutSize<TKey, TValue>(Dictionary<TKey, TValue>? dict, WriteDel<TKey> keyWriter, WriteDel<TValue> valueWriter) where TKey : notnull
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
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

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
        var hasValue = value != null!;
        WritePackedBool(hasValue);

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

    /// <summary>
    /// Truncates the high-water size of the writer back down to the pointer position.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Truncate()
    {
        if (position < size)
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
        WriteVarIntWithoutEnsuring(value);
    }

    private void WriteVarIntWithoutEnsuring(ulong value)
    {
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

    /// <inheritdoc cref="WriteCount"/>
    private void WriteCountWithoutEnsuring(int count, CountType countType)
    {
        switch (countType)
        {
            case CountType.Byte:
            {
                if (count > byte.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(count), $"Count {count} exceeds Byte max {byte.MaxValue}.");

                buffer![position++] = (byte)count;
                return;
            }
            case CountType.UShort:
            {
                if (count > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(count), $"Count {count} exceeds UShort max {ushort.MaxValue}.");

                buffer![position++] = (byte)count;
                buffer![position++] = (byte)(count >> 8);
                return;
            }
            case CountType.VarUInt:
            {
                WriteVarIntWithoutEnsuring((uint)count);
                return;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(countType), countType, "Unsupported count type.");
        }
    }

    /// <inheritdoc/>
    public override void Dispose() => Return(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetFixedCountHeaderSize(CountType countType) => countType switch
    {
        CountType.Byte => 1,
        CountType.UShort => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(countType), countType, "CountType is not a fixed size.")
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetVarUIntSize(uint value) => value switch
    {
        < 0x80 => 1,
        < 0x4000 => 2,
        < 0x200000 => 3,
        < 0x10000000 => 4,
        _ => 5
    };

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
[PublicApi]
public static class PacketWriterDels
{
    /// <summary>
    /// A delegate to write a byte.
    /// </summary>
    public static readonly WriteDel<byte> Byte = (writer, value) => writer.WriteByte(value);

    /// <summary>
    /// A delegate to write an sbyte.
    /// </summary>
    public static readonly WriteDel<sbyte> SByte = (writer, value) => writer.WriteSByte(value);

    /// <summary>
    /// A delegate to write a string.
    /// </summary>
    public static readonly WriteDel<string?> String = (writer, value) => writer.WriteString(value);

    /// <summary>
    /// A delegate to write a ushort.
    /// </summary>
    public static readonly WriteDel<ushort> UShort = (writer, value) => writer.WriteUShort(value);

    /// <summary>
    /// A delegate to write an int.
    /// </summary>
    public static readonly WriteDel<int> Int = (writer, value) => writer.WriteInt(value);

    /// <summary>
    /// A delegate to write a packed int.
    /// </summary>
    public static readonly WriteDel<int> PackedInt = (writer, value) => writer.WritePackedInt(value);

    /// <summary>
    /// A delegate to write a boolean.
    /// </summary>
    public static readonly WriteDel<bool> Bool = (writer, value) => writer.WriteBool(value);

    /// <summary>
    /// A delegate to write a short.
    /// </summary>
    public static readonly WriteDel<short> Short = (writer, value) => writer.WriteShort(value);

    /// <summary>
    /// A delegate to write a uint.
    /// </summary>
    public static readonly WriteDel<uint> UInt = (writer, value) => writer.WriteUInt(value);

    /// <summary>
    /// A delegate to write a long.
    /// </summary>
    public static readonly WriteDel<long> Long = (writer, value) => writer.WriteLong(value);

    /// <summary>
    /// A delegate to write a ulong.
    /// </summary>
    public static readonly WriteDel<ulong> ULong = (writer, value) => writer.WriteULong(value);

    /// <summary>
    /// A delegate to write a float.
    /// </summary>
    public static readonly WriteDel<float> Float = (writer, value) => writer.WriteFloat(value);

    /// <summary>
    /// A delegate to write a double.
    /// </summary>
    public static readonly WriteDel<double> Double = (writer, value) => writer.WriteDouble(value);

    /// <summary>
    /// A delegate to write a packed uint.
    /// </summary>
    public static readonly WriteDel<uint> PackedUInt = (writer, value) => writer.WritePackedUInt(value);

    /// <summary>
    /// A delegate to write a packed long.
    /// </summary>
    public static readonly WriteDel<long> PackedLong = (writer, value) => writer.WritePackedLong(value);

    /// <summary>
    /// A delegate to write a packed ulong.
    /// </summary>
    public static readonly WriteDel<ulong> PackedULong = (writer, value) => writer.WritePackedULong(value);

    /// <summary>
    /// A delegate to write a packed bool.
    /// </summary>
    public static readonly WriteDel<bool> PackedBool = (writer, value) => writer.WritePackedBool(value);

    /// <summary>
    /// A delegate to write a Vector2.
    /// </summary>
    public static readonly WriteDel<Vector2> Vector2 = (writer, value) => writer.WriteVector2(value);

    /// <summary>
    /// A delegate to write a Vector3.
    /// </summary>
    public static readonly WriteDel<Vector3> Vector3 = (writer, value) => writer.WriteVector3(value);

    /// <summary>
    /// A delegate to write a Quaternion.
    /// </summary>
    public static readonly WriteDel<Quaternion> Quaternion = (writer, value) => writer.WriteQuaternion(value);

    /// <summary>
    /// A delegate to write a float4.
    /// </summary>
    public static readonly WriteDel<float4> Float4 = (writer, value) => writer.WriteFloat4(value);

    /// <summary>
    /// A delegate to write a half.
    /// </summary>
    public static readonly WriteDel<Half> Half = (writer, value) => writer.WriteHalf(value);

    /// <summary>
    /// A delegate to write a decimal.
    /// </summary>
    public static readonly WriteDel<decimal> Decimal = (writer, value) => writer.WriteDecimal(value);

    /// <summary>
    /// A delegate to write a Color.
    /// </summary>
    public static readonly WriteDel<Color> Color = (writer, value) => writer.WriteColor(value);

    /// <summary>
    /// A delegate to write a Color32.
    /// </summary>
    public static readonly WriteDel<Color32> Color32 = (writer, value) => writer.WriteColor32(value);

    /// <summary>
    /// A delegate to write a DateTime.
    /// </summary>
    public static readonly WriteDel<DateTime> DateTime = (writer, value) => writer.WriteDateTime(value);

    /// <summary>
    /// A delegate to write a TimeSpan.
    /// </summary>
    public static readonly WriteDel<TimeSpan> TimeSpan = (writer, value) => writer.WriteTimeSpan(value);

    /// <summary>
    /// A delegate to write a Guid.
    /// </summary>
    public static readonly WriteDel<Guid> Guid = (writer, value) => writer.WriteGuid(value);

    /// <summary>
    /// A delegate to write a char.
    /// </summary>
    public static readonly WriteDel<char> Char = (writer, value) => writer.WriteChar(value);

    /// <summary>
    /// Caches a writing delegate for types implementing <see cref="INetObject"/>.
    /// </summary>
    /// <typeparam name="T">The net object type.</typeparam>
    public static class NetObject<T> where T : INetObject
    {
        /// <summary>
        /// A delegate to write an <see cref="INetObject"/>.
        /// </summary>
        public static readonly WriteDel<T> Writer = (writer, value) => value.Serialise(writer);
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
        public static readonly WriteDel<T?> Writer = (writer, value) => writer.WriteNullable(value);
    }

    /// <summary>
    /// Caches a writing delegate for value <see cref="Tuple"/>s.
    /// </summary>
    /// <typeparam name="T">The tuple type.</typeparam>
    /// <remarks>If you are using tuples of elements greater than 7 values...why? Just use an <see cref="INetObject"/> at that point.</remarks>
    public static class Tuple<T> where T : struct, ITuple
    {
        /// <summary>
        /// A delegate to write a <see cref="Tuple"/>.
        /// </summary>
        public static readonly WriteDel<T> Writer = CreateWriter();

        private static WriteDel<T> CreateWriter()
        {
            var writerParam = Expression.Parameter(typeof(PacketWriter), "writer");
            var tupleParam = Expression.Parameter(typeof(T), "value");

            var componentTypes = typeof(T).GetGenericArguments();
            var writeCalls = new Expression[componentTypes.Length];
            var writeObjectMethodDef = Method(nameof(PacketWriter.WriteObject));

            var elemCount = Mathf.Min(componentTypes.Length, 7);

            for (var i = 0; i < elemCount; i++)
            {
                var fieldAccess = Expression.PropertyOrField(tupleParam, $"Item{i + 1}");
                var genericWrite = writeObjectMethodDef.MakeGenericMethod(componentTypes[i]);
                writeCalls[i] = Expression.Call(writerParam, genericWrite, fieldAccess);
            }

            if (componentTypes.Length == 8)
            {
                var fieldAccess = Expression.PropertyOrField(tupleParam, "Rest");
                var genericWrite = Method(nameof(PacketWriter.WriteTuple)).MakeGenericMethod(componentTypes[7]);
                writeCalls[7] = Expression.Call(writerParam, genericWrite, fieldAccess);
            }

            var block = Expression.Block(writeCalls);
            return Expression.Lambda<WriteDel<T>>(block, writerParam, tupleParam).Compile();
        }
    }

    /// <summary>
    /// Caches a writing delegate for custom objects that aren't natively supported.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    public static class Object<T>
    {
        internal static volatile WriteDel<T> _writer = CreateWriter();

        /// <summary>
        /// A delegate to write a value.
        /// </summary>
        public static WriteDel<T> Writer => _writer;

        private static WriteDel<T> CreateWriter()
        {
            try
            {
                return (WriteDel<T>)Delegate.CreateDelegate(typeof(WriteDel<T>), GetWriteExpression(typeof(T)));
            }
            catch
            {
                return (_, _) => throw new NotSupportedException($"Type {typeof(T).Name} is not supported natively. Did you forget to register it?");
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
        public static readonly WriteDel<T> Writer = CreateWriter();

        private static WriteDel<T> CreateWriter()
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
        public static readonly WriteDel<T> Writer = CreateWriter();

        private static WriteDel<T> CreateWriter()
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
        [typeof(Half)] = nameof(PacketWriter.WriteHalf),
        [typeof(Guid)] = nameof(PacketWriter.WriteGuid),
        [typeof(char)] = nameof(PacketWriter.WriteChar),
        [typeof(short)] = nameof(PacketWriter.WriteShort),
        [typeof(ulong)] = nameof(PacketWriter.WriteULong),
        [typeof(sbyte)] = nameof(PacketWriter.WriteSByte),
        [typeof(float)] = nameof(PacketWriter.WriteFloat),
        [typeof(Color)] = nameof(PacketWriter.WriteColor),
        [typeof(ushort)] = nameof(PacketWriter.WriteUShort),
        [typeof(double)] = nameof(PacketWriter.WriteDouble),
        [typeof(string)] = nameof(PacketWriter.WriteString),
        [typeof(float4)] = nameof(PacketWriter.WriteFloat4),
        [typeof(decimal)] = nameof(PacketWriter.WriteDecimal),
        [typeof(Vector2)] = nameof(PacketWriter.WriteVector2),
        [typeof(Vector3)] = nameof(PacketWriter.WriteVector3),
        [typeof(Color32)] = nameof(PacketWriter.WriteColor32),
        [typeof(DateTime)] = nameof(PacketWriter.WriteDateTime),
        [typeof(TimeSpan)] = nameof(PacketWriter.WriteTimeSpan),
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

        TypeWriteCache.GetOrAdd(type, method);
        return method;
    }

    private static MethodInfo Method(string name) =>
        typeof(PacketWriter).GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new MissingMethodException($"PacketWriter missing method: {name}");
}