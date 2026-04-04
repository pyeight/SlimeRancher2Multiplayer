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
/// A reusable reader for extracting data from a packet buffer.
/// </summary>
[PublicAPI]
public sealed class PacketReader : PacketBuffer
{
    /// <summary>
    /// Gets the number of unread bytes remaining in the buffer.
    /// </summary>
    public int BytesRemaining => dataSize - position;

    /// <summary>
    /// Gets the total size of the readable data represented by this buffer.
    /// </summary>
    // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
    public override int DataSize => dataSize;

    private bool isRented;
    private int dataSize;

    /// <summary>
    /// Initialises a new instance of the <see cref="PacketReader"/> class.
    /// </summary>
    [Obsolete("Use PacketReader.Borrow instead!", true)]
    public PacketReader() : base(8) { }

    /// <summary>
    /// Ensures that the reader can actually read data.
    /// </summary>
    /// <param name="bytesToRead">The number of bytes to read.</param>
    /// <exception cref="InvalidOperationException">Thrown if the buffer is recycled.</exception>
    /// <exception cref="EndOfStreamException">Thrown if there are not enough bytes left.</exception>
    private void EnsureReadable(int bytesToRead)
    {
        if (IsRecycled)
            throw new InvalidOperationException("PacketReader is already recycled!");

        if (position + bytesToRead > dataSize)
            throw new EndOfStreamException($"Attempted to read {bytesToRead} bytes, but only {BytesRemaining} remain.");

        EndPackingBools();
    }

    /// <summary>
    /// Reads a byte.
    /// </summary>
    /// <returns>The read byte.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
        EnsureReadable(1);
        return buffer![position++];
    }

    /// <summary>
    /// Reads a boolean.
    /// </summary>
    /// <returns><c>true</c> if the byte is non-zero, otherwise <c>false</c>.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool() => ReadByte() != 0;

    /// <summary>
    /// Reads an sbyte.
    /// </summary>
    /// <returns>The read signed byte.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte() => (sbyte)ReadByte();

    /// <summary>
    /// Reads a short.
    /// </summary>
    /// <returns>The read short.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShort() => BinaryPrimitives.ReadInt16LittleEndian(ReadRequest(2));

    /// <summary>
    /// Reads a ushort.
    /// </summary>
    /// <returns>The read ushort.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort() => BinaryPrimitives.ReadUInt16LittleEndian(ReadRequest(2));

    /// <summary>
    /// Reads an int.
    /// </summary>
    /// <returns>The read iny.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt() => BinaryPrimitives.ReadInt32LittleEndian(ReadRequest(4));

    /// <summary>
    /// Reads a uint.
    /// </summary>
    /// <returns>The read uint.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt() => BinaryPrimitives.ReadUInt32LittleEndian(ReadRequest(4));

    /// <summary>
    /// Reads a long.
    /// </summary>
    /// <returns>The read long.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLong() => BinaryPrimitives.ReadInt64LittleEndian(ReadRequest(8));

    /// <summary>
    /// Reads a ulong.
    /// </summary>
    /// <returns>The read ulong.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadULong() => BinaryPrimitives.ReadUInt64LittleEndian(ReadRequest(8));

    /// <summary>
    /// Reads a double.
    /// </summary>
    /// <returns>The read double.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble() => BinaryPrimitives.ReadDoubleLittleEndian(ReadRequest(8));

    /// <summary>
    /// Reads a float.
    /// </summary>
    /// <returns>The read float.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat() => BinaryPrimitives.ReadSingleLittleEndian(ReadRequest(4));

    /// <summary>
    /// Reads a packed int.
    /// </summary>
    /// <returns>The unpacked int.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    /// <inheritdoc cref="ReadVarInt"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadPackedInt()
    {
        var val = ReadPackedUInt();
        return (int)(val >> 1) ^ -(int)(val & 1);
    }

    /// <summary>
    /// Reads a packed uint.
    /// </summary>
    /// <returns>The unpacked uint.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    /// <exception cref="InvalidDataException">Thrown if the varint exceeds the maximum shift size.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadPackedUInt() => (uint)ReadVarInt(35);

    /// <summary>
    /// Reads a packed long.
    /// </summary>
    /// <returns>The unpacked long.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    /// <exception cref="InvalidDataException">Thrown if the varint exceeds the maximum shift size.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadPackedLong()
    {
        var val = ReadPackedULong();
        return (long)(val >> 1) ^ -(long)(val & 1);
    }

    /// <summary>
    /// Reads a packed ulong.
    /// </summary>
    /// <returns>The unpacked ulong.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    /// <exception cref="InvalidDataException">Thrown if the varint exceeds the maximum shift size.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadPackedULong() => ReadVarInt(70);

    private void ReadFloats(Span<float> values)
    {
        var span = ReadRequest(values.Length * 4);

        for (var i = 0; i < span.Length; i += 4)
            values[i] = BinaryPrimitives.ReadSingleLittleEndian(span[i..]);
    }

    /// <summary>
    /// Reads a Vector2.
    /// </summary>
    /// <returns>The parsed Vector2.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    public Vector2 ReadVector2()
    {
        Span<float> v = stackalloc float[2];
        ReadFloats(v);
        return new(v[0], v[1]);
    }

    /// <summary>
    /// Reads a Vector3.
    /// </summary>
    /// <returns>The parsed Vector3.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    public Vector3 ReadVector3()
    {
        Span<float> v = stackalloc float[3];
        ReadFloats(v);
        return new(v[0], v[1], v[2]);
    }

    /// <summary>
    /// Reads a Quaternion.
    /// </summary>
    /// <returns>The parsed Quaternion.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    public Quaternion ReadQuaternion()
    {
        Span<float> v = stackalloc float[4];
        ReadFloats(v);
        return new(v[0], v[1], v[2], v[3]);
    }

    /// <summary>
    /// Reads a float4.
    /// </summary>
    /// <returns>The parsed float4.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    public float4 ReadFloat4()
    {
        Span<float> v = stackalloc float[4];
        ReadFloats(v);
        return new(v[0], v[1], v[2], v[3]);
    }

    /// <summary>
    /// Reads an enum value.
    /// </summary>
    /// <typeparam name="T">The type of the enum.</typeparam>
    /// <returns>The read enum value.</returns>
    /// <exception cref="NotSupportedException">Thrown if the enum size is not supported.</exception>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadEnum<T>() where T : struct, Enum => PacketReaderDels.Enum<T>.Reader(this);

    /// <summary>
    /// Reads an enum value by parsing a string representation.
    /// </summary>
    /// <typeparam name="T">The type of the enum.</typeparam>
    /// <returns>The parsed enum value.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadEnumFromString<T>() where T : struct, Enum => Enum.Parse<T>(ReadString()!);

    // ReSharper disable InvalidXmlDocComment

    /// <summary>
    /// Reads a string prefixed by its length.
    /// </summary>
    /// <returns>The read string.</returns>
    /// <inheritdoc cref="ReadStringWithSize"/>
    /// <inheritdoc cref="ReadCount"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string? ReadString(CountType countType = CountType.UShort, bool returnNullOnZero = false)
        => ReadStringWithSize(ReadCount(countType), returnNullOnZero);

    /// <summary>
    /// Reads a UTF-8 encoded string of the specified length.
    /// </summary>
    /// <param name="len">The length of the string in bytes.</param>
    /// <param name="returnNullOnZero">Indicates whether the method should return a null if the length given is zero.</param>
    /// <returns>The read string, or null if the length is negative.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    public string? ReadStringWithSize(int len, bool returnNullOnZero = false)
    {
        switch (len)
        {
            case < 0:
                return null;
            case 0:
                return returnNullOnZero ? null : string.Empty;
        }

        EnsureReadable(len);
        var s = Encoding.UTF8.GetString(buffer.AsSpan(position, len));
        position += len;
        return s;
    }

    /// <summary>
    /// Reads a collection from the buffer.
    /// </summary>
    /// <param name="countType">The type of the count value that was serialised.</param>
    /// <inheritdoc cref="EnsureReadable"/>
    /// <inheritdoc cref="ReadCollectionWithSize"/>
    private TCollection? ReadCollection<TCollection, T>(Func<int, TCollection> factory, Action<TCollection, T> add, Func<PacketReader, T> reader, CountType countType, bool returnNullOnZero)
        => ReadCollectionWithSize(ReadCount(countType), factory, add, reader, returnNullOnZero);

    /// <summary>
    /// Reads a collection using an externally provided count.
    /// </summary>
    /// <param name="count">The number of items to read.</param>
    /// <param name="factory">The creation delegate for the collection.</param>
    /// <param name="add">The delegate that maps to the collection's Add method.</param>
    /// <param name="reader">The delegate that reads the value.</param>
    /// <param name="returnNullOnZero">Indicates whether the method should return a null if the length given is zero.</param>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <typeparam name="T">The type of the collection's elements.</typeparam>
    /// <returns>A collection of data deserialised from the buffer.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    private TCollection? ReadCollectionWithSize<TCollection, T>(int count, Func<int, TCollection> factory, Action<TCollection, T> add, Func<PacketReader, T> reader, bool returnNullOnZero = false)
    {
        switch (count)
        {
            case < 0:
                return default;
            case 0:
                return returnNullOnZero ? default : factory(0);
        }

        var collection = factory(count);

        for (var i = 0; i < count; i++)
            add(collection, reader(this));

        return collection;
    }

    /// <summary>
    /// Reads an array, prefixed by its length.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="reader">The function to read individual elements.</param>
    /// <param name="countType">The header type used to store array length.</param>
    /// <returns>The read array.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    public T[]? ReadArray<T>(Func<PacketReader, T> reader, CountType countType = CountType.UShort, bool returnNullOnZero = false)
        => ReadArrayWithSize(ReadCount(countType), reader, returnNullOnZero);

    /// <summary>
    /// Reads an array without prefixing its length.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="count">The number of elements to read.</param>
    /// <param name="reader">The function to read individual elements.</param>
    /// <returns>The read array.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative.</exception>
    /// <exception cref="InvalidDataException">Thrown if count is zero.</exception>
    /// <inheritdoc cref="EnsureReadable"/>
    public T[]? ReadArrayWithSize<T>(int count, Func<PacketReader, T> reader, bool returnNullOnZero = false)
    {
        switch (count)
        {
            case < 0:
                return null;
            case 0:
                return returnNullOnZero ? null : Array.Empty<T>();
        }

        var array = new T[count];

        for (var i = 0; i < array.Length; i++)
            array[i] = reader(this);

        return array;
    }

    /// <summary>
    /// Reads a List, prefixed by its length.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="reader">The function to read individual elements.</param>
    /// <param name="countType">The header type used to store list length.</param>
    /// <returns>The read list.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<T>? ReadList<T>(Func<PacketReader, T> reader, CountType countType = CountType.UShort, bool returnNullOnZero = false)
        => ReadCollection(PacketReaderDels.ListDels<T>.Create, PacketReaderDels.ListDels<T>.Add, reader, countType, returnNullOnZero);

    /// <summary>
    /// Reads a List without prefixing its length.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="count">The number of elements to read.</param>
    /// <param name="reader">The function to read individual elements.</param>
    /// <returns>The read list.</returns>
    /// <inheritdoc cref="ReadCollectionWithSize{TCollection,T}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<T>? ReadListWithoutSize<T>(int count, Func<PacketReader, T> reader, bool returnNullOnZero = false)
        => ReadCollectionWithSize(count, PacketReaderDels.ListDels<T>.Create, PacketReaderDels.ListDels<T>.Add, reader, returnNullOnZero);

    /// <summary>
    /// Reads a HashSet, prefixed by its length.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="reader">The function to read individual elements.</param>
    /// <param name="countType">The header type used to store set length.</param>
    /// <returns>The read HashSet.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HashSet<T>? ReadSet<T>(Func<PacketReader, T> reader, CountType countType = CountType.UShort, bool returnNullOnZero = false)
        => ReadCollection(PacketReaderDels.HastSetDels<T>.Create, PacketReaderDels.HastSetDels<T>.Add, reader, countType, returnNullOnZero);

    /// <summary>
    /// Reads a HashSet without prefixing its length.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="count">The number of elements to read.</param>
    /// <param name="reader">The function to read individual elements.</param>
    /// <returns>The read HashSet.</returns>
    /// <inheritdoc cref="ReadCollectionWithSize{TCollection,T}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HashSet<T>? ReadSetWithSize<T>(int count, Func<PacketReader, T> reader, bool returnNullOnZero = false)
        => ReadCollectionWithSize(count, PacketReaderDels.HastSetDels<T>.Create, PacketReaderDels.HastSetDels<T>.Add, reader, returnNullOnZero);

    /// <summary>
    /// Reads an Il2Cpp HashSet, prefixed by its length.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="reader">The function to read individual elements.</param>
    /// <param name="countType">The header type used to store set length.</param>
    /// <returns>The read HashSet.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CppCollections.HashSet<T>? ReadCppSet<T>(Func<PacketReader, T> reader, CountType countType = CountType.UShort, bool returnNullOnZero = false)
        => ReadCollection(PacketReaderDels.CppHashSetDels<T>.Create, PacketReaderDels.CppHashSetDels<T>.Add, reader, countType, returnNullOnZero);

    /// <summary>
    /// Reads an Il2Cpp HashSet without prefixing its length.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="count">The number of elements to read.</param>
    /// <param name="reader">The function to read individual elements.</param>
    /// <returns>The read HashSet.</returns>
    /// <inheritdoc cref="ReadCollectionWithSize{TCollection,T}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CppCollections.HashSet<T>? ReadCppSetWithSize<T>(int count, Func<PacketReader, T> reader, bool returnNullOnZero = false)
        => ReadCollectionWithSize(count, PacketReaderDels.CppHashSetDels<T>.Create, PacketReaderDels.CppHashSetDels<T>.Add, reader, returnNullOnZero);

    /// <summary>
    /// Reads a Dictionary, prefixed by its length.
    /// </summary>
    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <param name="keyReader">The function to read individual keys.</param>
    /// <param name="valueReader">The function to read individual values.</param>
    /// <param name="countType">The header type used to store dictionary length.</param>
    /// <returns>The read Dictionary.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    public Dictionary<TKey, TValue>? ReadDictionary<TKey, TValue>(Func<PacketReader, TKey> keyReader, Func<PacketReader, TValue> valueReader, CountType countType = CountType.UShort, bool returnNullOnZero = false) where TKey : notnull
        => ReadDictionaryWithSize(ReadCount(countType),  keyReader, valueReader, returnNullOnZero);

    /// <summary>
    /// Reads a Dictionary without prefixing its length.
    /// </summary>
    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <param name="count">The number of entries to read.</param>
    /// <param name="keyReader">The function to read individual keys.</param>
    /// <param name="valueReader">The function to read individual values.</param>
    /// <param name="returnNullOnZero">Indicates whether the method should return a null if the length given is zero.</param>
    /// <returns>The read Dictionary.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    public Dictionary<TKey, TValue>? ReadDictionaryWithSize<TKey, TValue>(int count, Func<PacketReader, TKey> keyReader, Func<PacketReader, TValue> valueReader, bool returnNullOnZero = false) where TKey : notnull
    {
        switch (count)
        {
            case < 0:
                return null;
            case 0:
                return returnNullOnZero ? null : new();
        }

        var dict = new Dictionary<TKey, TValue>(count);

        for (var i = 0; i < count; i++)
            dict[keyReader(this)] = valueReader(this);

        return dict;
    }

    // ReSharper enable InvalidXmlDocComment

    /// <summary>
    /// Reads an object that implements <see cref="INetObject"/>.
    /// </summary>
    /// <typeparam name="T">The type of the net object.</typeparam>
    /// <returns>The deserialized object.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadNetObject<T>() where T : INetObject, new()
    {
        var result = PacketReaderDels.NetObjectFactory<T>.Factory();
        result.Deserialise(this);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T ReadPacket<T>() where T : IPacket, new()
    {
        EnsureReadable(1);
        position++; // The byte header is already read, so we skip this byte
        return ReadNetObject<T>();
    }

    /// <summary>
    /// Reads a custom packet that implements <see cref="ICustomPacket"/>.
    /// </summary>
    /// <typeparam name="T">The type of the packet.</typeparam>
    /// <returns>A custom implementation of a packet.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadCustomPacket<T>() where T : ICustomPacket, new()
    {
        EnsureReadable(1);
        position++; // The byte header is already read, so we skip this byte
        return ReadNetObject<T>();
    }

    /// <summary>
    /// Reads a single boolean value from a packed bit state.
    /// </summary>
    /// <returns>The unpacked boolean.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    public bool ReadPackedBool()
    {
        if (currentBitIndex >= 8)
        {
            currentPackedByte = ReadByte();
            currentBitIndex = 0;
        }

        var value = (currentPackedByte & (1 << currentBitIndex)) != 0;
        currentBitIndex++;
        return value;
    }

    /// <summary>
    /// Finalises reading packed booleans and realigns the reader for byte-level operations.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void EndPackingBools() => currentBitIndex = 8;

    /// <summary>
    /// Reads a value.
    /// </summary>
    /// <typeparam name="T">The type to read.</typeparam>
    /// <returns>The read value.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadObject<T>() => PacketReaderDels.Object<T>.Reader(this);

    /// <summary>
    /// Reads an optional value, returning null if the boolean flag is false.
    /// </summary>
    /// <typeparam name="T">The value type to read.</typeparam>
    /// <returns>The read value, or null.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? ReadNullable<T>() => ReadBool() ? ReadObject<T>() : default;

    /// <summary>
    /// Reads a packed enum value.
    /// </summary>
    /// <typeparam name="T">The type of the enum.</typeparam>
    /// <returns>The read enum value.</returns>
    /// <exception cref="NotSupportedException">Thrown if the enum size is not supported.</exception>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadPackedEnum<T>() where T : struct, Enum => PacketReaderDels.PackedEnum<T>.Reader(this);

    /// <summary>
    /// Reads a block of data into the provided span.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadToSpan(Span<byte> destination)
    {
        EnsureReadable(destination.Length);
        buffer.AsSpan(position, destination.Length).CopyTo(destination);
        position += destination.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void EnsureBounds(int count) => EnsureReadable(count);

    /// <inheritdoc cref="PacketBuffer.MoveForward"/>
    public override void MoveForward(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        EnsureReadable(count);
        position += count;
    }

    /// <inheritdoc cref="PacketBuffer.MoveBack"/>
    public override void MoveBack(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        if (position < count)
            throw new InvalidOperationException("Cannot return to a position before the start of the stream!");

        EndPackingBools();
        position -= count;
    }

    private ReadOnlySpan<byte> ReadRequest(int size)
    {
        EnsureReadable(size);
        var span = buffer.AsSpan(position, size);
        position += size;
        return span;
    }

    /// <summary>
    /// Sets the underlying byte buffer and size for this reader.
    /// </summary>
    /// <param name="data">The byte array to read from.</param>
    /// <param name="size">The size of the data to read, or -1 to use the array length.</param>
    /// <param name="rented">Indicates whether the buffer was rented from a pool.</param>
    public void SetBuffer(byte[] data, int size = -1, bool rented = false)
    {
        buffer = data;
        dataSize = size >= 0 && size <= data.Length ? size : data.Length;
        isRented = rented;
        Clear();
    }

    protected override void OnRecycle()
    {
        if (isRented && buffer != null)
            ArrayPool<byte>.Shared.Return(buffer);
    }

    /// <summary>
    /// Borrows a <see cref="PacketReader"/> instance from the recycle pool and initialises it with the specified data.
    /// </summary>
    /// <param name="data">The byte array to read from.</param>
    /// <param name="size">The size of the data to read, or -1 to use the array length.</param>
    /// <param name="rented">Indicates whether the buffer was rented from a pool.</param>
    /// <returns>A configured <see cref="PacketReader"/>.</returns>
    public static PacketReader Borrow(byte[] data, int size = -1, bool rented = false)
    {
        var reader = RecyclePool<PacketReader>.Borrow();
        reader.SetBuffer(data, size, rented);
        return reader;
    }

    /// <summary>
    /// Returns a <see cref="PacketReader"/> instance to the recycle pool.
    /// </summary>
    /// <param name="reader">The reader to return.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(PacketReader reader) => RecyclePool<PacketReader>.Return(reader);

    private ulong ReadVarInt(int maxShift)
    {
        var result = 0ul;
        var shift = 0;

        while (true)
        {
            if (position >= dataSize)
                throw new EndOfStreamException("Unexpected end of stream during VarInt.");

            var b = buffer![position++];
            result |= (ulong)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
                break;

            shift += 7;

            if (shift >= maxShift)
                throw new InvalidDataException("VarInt too long");
        }

        return result;
    }

    /// <summary>
    /// Reads a count value from the buffer.
    /// </summary>
    /// <param name="countType">The type of the value to be read.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if an unsupported configuration was passed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ReadCount(CountType countType) => countType switch
    {
        CountType.Byte => ReadByte(),
        CountType.UShort => ReadUShort(),
        CountType.UInt => (int)ReadUInt(),
        CountType.VarUInt => (int)ReadPackedUInt(),
        _ => throw new ArgumentOutOfRangeException(nameof(countType), countType, "Unsupported count type.")
    };
}

/// <summary>
/// Reusable cached delegates to improve performance, add more for data types as needed to avoid excess GC overhead.
/// </summary>
[PublicAPI]
public static class PacketReaderDels
{
    /// <summary>
    /// A delegate to read a byte.
    /// </summary>
    public static readonly Func<PacketReader, byte> Byte = reader => reader.ReadByte();

    /// <summary>
    /// A delegate to read an sbyte.
    /// </summary>
    public static readonly Func<PacketReader, sbyte> SByte = reader => reader.ReadSByte();

    /// <summary>
    /// A delegate to read a string.
    /// </summary>
    public static readonly Func<PacketReader, string?> String = reader => reader.ReadString();

    /// <summary>
    /// A delegate to read a ushort.
    /// </summary>
    public static readonly Func<PacketReader, ushort> UShort = reader => reader.ReadUShort();

    /// <summary>
    /// A delegate to read an Int32.
    /// </summary>
    public static readonly Func<PacketReader, int> Int32 = reader => reader.ReadInt();

    /// <summary>
    /// A delegate to read a packed varint Int32.
    /// </summary>
    public static readonly Func<PacketReader, int> PackedInt32 = reader => reader.ReadPackedInt();

    /// <summary>
    /// Caches a reading delegate for types implementing INetObject.
    /// </summary>
    /// <typeparam name="T">The net object type.</typeparam>
    public static class NetObject<T> where T : INetObject, new()
    {
        /// <summary>
        /// A delegate to read an INetObject.
        /// </summary>
        public static readonly Func<PacketReader, T> Reader = reader => reader.ReadNetObject<T>();
    }

    /// <summary>
    /// Caches a reading delegate for value Tuples.
    /// </summary>
    /// <typeparam name="T1">The first tuple element type.</typeparam>
    /// <typeparam name="T2">The second tuple element type.</typeparam>
    public static class Tuple<T1, T2>
    {
        /// <summary>
        /// A delegate to read a Tuple.
        /// </summary>
        public static readonly Func<PacketReader, (T1, T2)> Reader = CreateTupleReader<(T1, T2)>(typeof(T1), typeof(T2));
    }

    /// <summary>
    /// Caches a dynamically generated reading delegate for custom structs.
    /// </summary>
    /// <typeparam name="T">The struct type.</typeparam>
    public static class Object<T>
    {
        /// <summary>
        /// A delegate to read a struct.
        /// </summary>
        public static readonly Func<PacketReader, T> Reader = (Func<PacketReader, T>)Delegate.CreateDelegate(typeof(Func<PacketReader, T>), GetReadExpression(typeof(T)));
    }

    /// <summary>
    /// Caches a reading delegate for Enum types.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    public static class Enum<T> where T : struct, Enum
    {
        /// <summary>
        /// A delegate to read an enum based on its underlying size.
        /// </summary>
        public static readonly Func<PacketReader, T> Reader = CreateReader();

        private static Func<PacketReader, T> CreateReader()
        {
            var size = Unsafe.SizeOf<T>();
            return size switch
            {
                1 => r =>
                {
                    var v = r.ReadByte();
                    return Unsafe.As<byte, T>(ref v);
                },
                2 => r =>
                {
                    var v = r.ReadUShort();
                    return Unsafe.As<ushort, T>(ref v);
                },
                4 => r =>
                {
                    var v = r.ReadUInt();
                    return Unsafe.As<uint, T>(ref v);
                },
                8 => r =>
                {
                    var v = r.ReadULong();
                    return Unsafe.As<ulong, T>(ref v);
                },
                _ => throw new NotSupportedException($"Enum size {size} not supported")
            };
        }
    }

    /// <summary>
    /// Caches a reading delegate for Enums using packed varint formats.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    public static class PackedEnum<T> where T : struct, Enum
    {
        /// <summary>
        /// A delegate to read a packed enum.
        /// </summary>
        public static readonly Func<PacketReader, T> Reader = CreateReader();

        private static Func<PacketReader, T> CreateReader()
        {
            var size = Unsafe.SizeOf<T>();
            var underlying = Enum.GetUnderlyingType(typeof(T));
            return size switch
            {
                1 => r =>
                {
                    var v = r.ReadByte();
                    return Unsafe.As<byte, T>(ref v);
                },
                2 => r =>
                {
                    var v = r.ReadUShort();
                    return Unsafe.As<ushort, T>(ref v);
                },
                4 when underlying == typeof(int) => r =>
                {
                    var v = r.ReadPackedInt();
                    return Unsafe.As<int, T>(ref v);
                },
                4 => r =>
                {
                    var v = r.ReadPackedUInt();
                    return Unsafe.As<uint, T>(ref v);
                },
                8 when underlying == typeof(long) => r =>
                {
                    var v = r.ReadPackedLong();
                    return Unsafe.As<long, T>(ref v);
                },
                8 => r =>
                {
                    var v = r.ReadPackedULong();
                    return Unsafe.As<ulong, T>(ref v);
                },
                _ => throw new NotSupportedException($"Enum size {size} not supported")
            };
        }
    }

    private static readonly ConcurrentDictionary<Type, MethodInfo> TypeReadCache = new();

    private static readonly ReadOnlyDictionary<Type, string> ReadMethodMap = new(new Dictionary<Type, string>()
    {
        [typeof(byte)] = nameof(PacketReader.ReadByte),
        [typeof(int)] = nameof(PacketReader.ReadInt),
        [typeof(bool)] = nameof(PacketReader.ReadBool),
        [typeof(uint)] = nameof(PacketReader.ReadUInt),
        [typeof(long)] = nameof(PacketReader.ReadLong),
        [typeof(sbyte)] = nameof(PacketReader.ReadSByte),
        [typeof(short)] = nameof(PacketReader.ReadShort),
        [typeof(ulong)] = nameof(PacketReader.ReadULong),
        [typeof(float)] = nameof(PacketReader.ReadFloat),
        [typeof(ushort)] = nameof(PacketReader.ReadUShort),
        [typeof(double)] = nameof(PacketReader.ReadDouble),
        [typeof(string)] = nameof(PacketReader.ReadString),
        [typeof(float4)] = nameof(PacketReader.ReadFloat4),
        [typeof(Vector3)] = nameof(PacketReader.ReadVector3),
        [typeof(Quaternion)] = nameof(PacketReader.ReadQuaternion),
    });

    /// <summary>
    /// Generates a reader function for custom tuples based on component types.
    /// </summary>
    /// <typeparam name="TTuple">The tuple type.</typeparam>
    /// <param name="componentTypes">The types making up the tuple.</param>
    /// <returns>A compiled function to read the tuple.</returns>
    public static Func<PacketReader, TTuple> CreateTupleReader<TTuple>(params Type[] componentTypes)
    {
        var readerParam = Expression.Parameter(typeof(PacketReader), "reader");
        var readCalls = new Expression[componentTypes.Length];

        for (var i = 0; i < componentTypes.Length; i++)
            readCalls[i] = Expression.Call(readerParam, GetReadExpression(componentTypes[i]));

        var constructor = typeof(TTuple).GetConstructor(componentTypes) ?? throw new InvalidOperationException($"Could not find constructor for tuple {typeof(TTuple)}");
        var newTuple = Expression.New(constructor, readCalls);
        return Expression.Lambda<Func<PacketReader, TTuple>>(newTuple, readerParam).Compile();
    }

    private static MethodInfo GetReadExpression(Type type)
    {
        if (TypeReadCache.TryGetValue(type, out var method))
            return method;

        if (ReadMethodMap.TryGetValue(type, out var methodName))
            method = Method(methodName);
        else if (type.IsEnum)
            method = Method(nameof(PacketReader.ReadEnum)).MakeGenericMethod(type);
        else if (typeof(IPacket).IsAssignableFrom(type))
            method = Method(nameof(PacketReader.ReadPacket)).MakeGenericMethod(type);
        else if (typeof(ICustomPacket).IsAssignableFrom(type))
            method = Method(nameof(PacketReader.ReadCustomPacket)).MakeGenericMethod(type);
        else if (typeof(INetObject).IsAssignableFrom(type))
            method = Method(nameof(PacketReader.ReadNetObject)).MakeGenericMethod(type);

        if (method == null)
            throw new NotSupportedException($"Type {type.Name} is not supported in automatic deserialization.");

        TypeReadCache[type] = method;
        return method;
    }

    private static MethodInfo Method(string name) =>
        typeof(PacketReader).GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new MissingMethodException($"PacketReader missing method: {name}");

    /// <summary>
    /// Caches a factory method for instantiating objects implementing INetObject.
    /// </summary>
    /// <typeparam name="T">The type to construct.</typeparam>
    public static class NetObjectFactory<T> where T : INetObject, new()
    {
        /// <summary>
        /// A delegate that returns a new instance of T.
        /// </summary>
        public static readonly Func<T> Factory = CreateFactory();

        private static Func<T> CreateFactory()
        {
            var newExp = Expression.New(typeof(T));
            var lambda = Expression.Lambda<Func<T>>(newExp);
            return lambda.Compile();
        }
    }

    /// <summary>
    /// Delegate cache for Lists.
    /// </summary>
    /// <typeparam name="T">The list element type.</typeparam>
    public static class ListDels<T>
    {
        /// <summary>
        /// A delegate that instantiates a List of size count.
        /// </summary>
        public static readonly Func<int, List<T>> Create = count => new List<T>(count);

        /// <summary>
        /// A delegate that adds an item to a list.
        /// </summary>
        public static readonly Action<List<T>, T> Add = (list, item) => list.Add(item);
    }

    /// <summary>
    /// Delegate cache for HashSets.
    /// </summary>
    /// <typeparam name="T">The set element type.</typeparam>
    public static class HastSetDels<T>
    {
        /// <summary>
        /// A delegate that instantiates a HashSet with capacity.
        /// </summary>
        public static readonly Func<int, HashSet<T>> Create = count => new HashSet<T>(count);

        /// <summary>
        /// A delegate that adds an item to a HashSet.
        /// </summary>
        public static readonly Action<HashSet<T>, T> Add = (set, item) => set.Add(item);
    }

    /// <summary>
    /// Delegate cache for Il2Cpp HashSets.
    /// </summary>
    /// <typeparam name="T">The set element type.</typeparam>
    public static class CppHashSetDels<T>
    {
        /// <summary>
        /// A delegate that instantiates an Il2Cpp HashSet.
        /// </summary>
        public static readonly Func<int, CppCollections.HashSet<T>> Create = _ => new CppCollections.HashSet<T>();

        /// <summary>
        /// A delegate that adds an item to an Il2Cpp HashSet.
        /// </summary>
        public static readonly Action<CppCollections.HashSet<T>, T> Add = (set, item) => set.Add(item);
    }
}