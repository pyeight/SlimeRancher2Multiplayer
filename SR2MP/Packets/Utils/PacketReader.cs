using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using SR2MP.Shared.Utils;
using Unity.Mathematics;

namespace SR2MP.Packets.Utils;

/// <summary>
/// A reusable reader for extracting data from a network buffer.
/// </summary>
[PublicApi]
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
    /// <exception cref="InvalidOperationException">Thrown if the buffer is recycled or null.</exception>
    /// <exception cref="EndOfStreamException">Thrown if there are not enough bytes left.</exception>
    private void EnsureReadable(int bytesToRead)
    {
        if (IsRecycled)
            throw new InvalidOperationException("PacketReader is already recycled!");

        if (position + bytesToRead > dataSize)
            throw new EndOfStreamException($"Attempted to read {bytesToRead} bytes, but only {BytesRemaining} remain.");

        if (buffer == null)
            throw new InvalidOperationException("The buffer is somehow not set!");

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
    /// <returns>The read sbyte.</returns>
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
    /// <returns>The read int.</returns>
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
    /// Reads a Half.
    /// </summary>
    /// <returns>The read Half.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Half ReadHalf() => BinaryPrimitives.ReadHalfLittleEndian(ReadRequest(2));

    /// <summary>
    /// Reads a decimal.
    /// </summary>
    /// <returns>The read decimal.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal ReadDecimal() => MemoryMarshal.Read<decimal>(ReadRequest(16));

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

    /// <summary>
    /// Reads floats to a span.
    /// </summary>
    /// <param name="destination">The destination span to allot the floats to.</param>
    public void ReadFloats(Span<float> destination)
    {
        var span = ReadRequest(destination.Length * 4);

        for (var i = 0; i < destination.Length; i++)
            destination[i] = BinaryPrimitives.ReadSingleLittleEndian(span[(i * 4)..]);
    }

    /// <summary>
    /// Reads a Vector2.
    /// </summary>
    /// <returns>The read Vector2.</returns>
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
    /// <returns>The read Vector3.</returns>
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
    /// <returns>The read Quaternion.</returns>
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
    /// <returns>The read float4.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    public float4 ReadFloat4()
    {
        Span<float> v = stackalloc float[4];
        ReadFloats(v);
        return new(v[0], v[1], v[2], v[3]);
    }

    /// <summary>
    /// Reads a char.
    /// </summary>
    /// <returns>The read char.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public char ReadChar() => (char)BinaryPrimitives.ReadUInt16LittleEndian(ReadRequest(2));

    /// <summary>
    /// Reads a Color32.
    /// </summary>
    /// <returns>The read Color32.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    public Color32 ReadColor32()
    {
        var span = ReadRequest(4);
        return new(span[0], span[1], span[2], span[3]);
    }

    /// <summary>
    /// Reads a Color.
    /// </summary>
    /// <returns>The read Color.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    public Color ReadColor()
    {
        Span<float> v = stackalloc float[4];
        ReadFloats(v);
        return new(v[0], v[1], v[2], v[3]);
    }

    /// <summary>
    /// Reads a DateTime.
    /// </summary>
    /// <returns>The read DateTime.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    public DateTime ReadDateTime() => new(ReadLong());

    /// <summary>
    /// Reads a TimeSpan.
    /// </summary>
    /// <returns>The read TimeSpan.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    public TimeSpan ReadTimeSpan() => new(ReadLong());

    /// <summary>
    /// Reads a Guid.
    /// </summary>
    /// <returns>The read Guid.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    public Guid ReadGuid() => new(ReadRequest(16));

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
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

    /// <summary>
    /// Reads a string prefixed by its length.
    /// </summary>
    /// <returns>The read string.</returns>
    /// <inheritdoc cref="ReadStringOfSize"/>
    /// <inheritdoc cref="ReadCount"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string? ReadString(CountType countType = CountType.UShort, bool returnNullOnZero = false)
        => ReadStringOfSize(ReadCount(countType), returnNullOnZero);

    /// <summary>
    /// Reads a pooled string prefixed by its length.
    /// </summary>
    /// <returns>The read pooled string.</returns>
    /// <inheritdoc cref="ReadStringOfSize"/>
    /// <inheritdoc cref="ReadCount"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string? ReadPooledString(CountType countType = CountType.UShort, bool returnNullOnZero = false)
        => ReadPooledStringOfSize(ReadCount(countType), returnNullOnZero);

    /// <summary>
    /// Reads a string of the specified byte length.
    /// </summary>
    /// <inheritdoc cref="ReadStringInternal"/>
    public string? ReadStringOfSize(int len, bool returnNullOnZero = false)
        => ReadStringInternal(len, returnNullOnZero, false);

    /// <summary>
    /// Reads a pooled string of the specified byte length.
    /// </summary>
    /// <inheritdoc cref="ReadStringInternal"/>
    public string? ReadPooledStringOfSize(int len, bool returnNullOnZero = false)
        => ReadStringInternal(len, returnNullOnZero, true);

    /// <summary>
    /// Reads a string (pooled or not based on the flag) of the specified byte length.
    /// </summary>
    /// <param name="len">The length of the string in bytes.</param>
    /// <param name="returnNullOnZero">Indicates whether the method should return a null if the length given is zero.</param>
    /// <returns>The read string, or null if the length is negative.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    private string? ReadStringInternal(int len, bool returnNullOnZero, bool isPooled)
    {
        switch (len)
        {
            case < 0:
                return null;
            case 0:
                return returnNullOnZero ? null : string.Empty;
        }

        var stringBytes = ReadRequest(len);
        return isPooled
            ? NetworkStringPool.GetOrAdd(stringBytes)
            : Encoding.UTF8.GetString(stringBytes);
    }

    /// <summary>
    /// Reads a collection from the buffer.
    /// </summary>
    /// <param name="countType">The type of the count value that was serialised.</param>
    /// <inheritdoc cref="ReadCollectionOfSize"/>
    private TCollection? ReadCollection<TCollection, TItem>(Func<int, TCollection> factory, Action<TCollection, TItem> add, ReadDel<TItem> reader, CountType countType, bool returnNullOnZero)
        => ReadCollectionOfSize(ReadCount(countType), factory, add, reader, returnNullOnZero);

    /// <summary>
    /// Reads a collection using an externally provided count.
    /// </summary>
    /// <param name="count">The number of items to read.</param>
    /// <param name="factory">The creation delegate for the collection.</param>
    /// <param name="add">The delegate that maps to the collection's Add method.</param>
    /// <param name="reader">The delegate that reads the value.</param>
    /// <param name="returnNullOnZero">Indicates whether the method should return a null if the length given is zero.</param>
    /// <typeparam name="TCollection">The type of the collection.</typeparam>
    /// <typeparam name="TItem">The type of the collection's elements.</typeparam>
    /// <returns>A collection of data deserialised from the buffer.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    private TCollection? ReadCollectionOfSize<TCollection, TItem>(int count, Func<int, TCollection> factory, Action<TCollection, TItem> add, ReadDel<TItem> reader, bool returnNullOnZero)
    {
        switch (count)
        {
            case < 0:
                return default;
            case 0:
                return returnNullOnZero ? default : factory(0);
        }

        EnsureReadable(count); // A collection must take at least N bytes, once per item
        var collection = factory(count);

        for (var i = 0; i < count; i++)
            add(collection, reader(this));

        return collection;
    }

    /// <summary>
    /// Reads an array prefixed by its length.
    /// </summary>=
    /// <param name="countType">The header type used to store array length.</param>=
    /// <inheritdoc cref="ReadArrayOfSize"/>
    public T[]? ReadArray<T>(ReadDel<T> reader, CountType countType = CountType.UShort, bool returnNullOnZero = false)
        => ReadArrayOfSize(ReadCount(countType), reader, returnNullOnZero);

    /// <summary>
    /// Reads an array of the specified length.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>The read array.</returns>
    /// <inheritdoc cref="ReadCollectionOfSize"/>
    public T[]? ReadArrayOfSize<T>(int count, ReadDel<T> reader, bool returnNullOnZero = false)
    {
        switch (count)
        {
            case < 0:
                return null;
            case 0:
                return returnNullOnZero ? null : Array.Empty<T>();
        }

        EnsureReadable(count); // An array must take at least N bytes, once per item
        var array = new T[count];

        for (var i = 0; i < array.Length; i++)
            array[i] = reader(this);

        return array;
    }

    /// <summary>
    /// Reads a list prefixed by its length.
    /// </summary>
    /// <param name="countType">The header type used to store list length.</param>
    /// <inheritdoc cref="ReadListOfSize"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<T>? ReadList<T>(ReadDel<T> reader, CountType countType = CountType.UShort, bool returnNullOnZero = false)
        => ReadCollection(PacketReaderDels.ListDels<T>.Create, PacketReaderDels.ListDels<T>.Add, reader, countType, returnNullOnZero);

    /// <summary>
    /// Reads a list of the specified length.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>The read list.</returns>
    /// <inheritdoc cref="ReadCollectionOfSize"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<T>? ReadListOfSize<T>(int count, ReadDel<T> reader, bool returnNullOnZero = false)
        => ReadCollectionOfSize(count, PacketReaderDels.ListDels<T>.Create, PacketReaderDels.ListDels<T>.Add, reader, returnNullOnZero);

    /// <summary>
    /// Reads a hash set prefixed by its length.
    /// </summary>
    /// <param name="countType">The header type used to store set length.</param>
    /// <inheritdoc cref="ReadHashSetOfSize"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HashSet<T>? ReadHashSet<T>(ReadDel<T> reader, CountType countType = CountType.UShort, bool returnNullOnZero = false)
        => ReadCollection(PacketReaderDels.HashSetDels<T>.Create, PacketReaderDels.HashSetDels<T>.Add, reader, countType, returnNullOnZero);

    /// <summary>
    /// Reads a hash set of the specified length.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>The read hash set.</returns>
    /// <inheritdoc cref="ReadCollectionOfSize"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HashSet<T>? ReadHashSetOfSize<T>(int count, ReadDel<T> reader, bool returnNullOnZero = false)
        => ReadCollectionOfSize(count, PacketReaderDels.HashSetDels<T>.Create, PacketReaderDels.HashSetDels<T>.Add, reader, returnNullOnZero);

    /// <summary>
    /// Reads an il2cpp hash set prefixed by its length.
    /// </summary>
    /// <param name="countType">The header type used to store set length.</param>
    /// <inheritdoc cref="ReadHashCppSetOfSize"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CppCollections.HashSet<T>? ReadCppHashSet<T>(ReadDel<T> reader, CountType countType = CountType.UShort, bool returnNullOnZero = false)
        => ReadCollection(PacketReaderDels.CppHashSetDels<T>.Create, PacketReaderDels.CppHashSetDels<T>.Add, reader, countType, returnNullOnZero);

    /// <summary>
    /// Reads an il2cpp hash set of the specified length.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>The read il2cpp hash set.</returns>
    /// <inheritdoc cref="ReadCollectionOfSize"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CppCollections.HashSet<T>? ReadHashCppSetOfSize<T>(int count, ReadDel<T> reader, bool returnNullOnZero = false)
        => ReadCollectionOfSize(count, PacketReaderDels.CppHashSetDels<T>.Create, PacketReaderDels.CppHashSetDels<T>.Add, reader, returnNullOnZero);

    /// <summary>
    /// Reads a dictionary prefixed by its length.
    /// </summary>
    /// <param name="countType">The header type used to store set length.</param>
    /// <inheritdoc cref="ReadDictionaryWithSize"/>
    public Dictionary<TKey, TValue>? ReadDictionary<TKey, TValue>(ReadDel<TKey> keyReader, ReadDel<TValue> valueReader, CountType countType = CountType.UShort, bool returnNullOnZero = false) where TKey : notnull
        => ReadDictionaryWithSize(ReadCount(countType),  keyReader, valueReader, returnNullOnZero);

    /// <summary>
    /// Reads dictionary of the specified length.
    /// </summary>
    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <param name="count">The number of entries to read.</param>
    /// <param name="keyReader">The function to read individual keys.</param>
    /// <param name="valueReader">The function to read individual values.</param>
    /// <param name="returnNullOnZero">Indicates whether the method should return a null if the length given is zero.</param>
    /// <returns>The read dictionary.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    public Dictionary<TKey, TValue>? ReadDictionaryWithSize<TKey, TValue>(int count, ReadDel<TKey> keyReader, ReadDel<TValue> valueReader, bool returnNullOnZero = false) where TKey : notnull
    {
        switch (count)
        {
            case < 0:
                return null;
            case 0:
                return returnNullOnZero ? null : new(0);
        }

        EnsureReadable(count * 2); // A dictionary must take at least 2N bytes, one for the key and the other for the value
        var dict = new Dictionary<TKey, TValue>(count);

        for (var i = 0; i < count; i++)
            dict[keyReader(this)] = valueReader(this);

        return dict;
    }

    // ReSharper enable InvalidXmlDocComment
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

    /// <summary>
    /// Reads an object that implements <see cref="INetObject"/>.
    /// </summary>
    /// <typeparam name="T">The type of the net object.</typeparam>
    /// <returns>The deserialized net object.</returns>
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
    /// <returns>The deserialised packet.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadCustomPacket<T>() where T : ICustomPacket, new()
    {
        EnsureReadable(1);
        position++; // The byte header is already read, so we skip this byte
        return ReadNetObject<T>();
    }

    /// <summary>
    /// Reads a packed boolean value.
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

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void EndPackingBools() => currentBitIndex = 8;

    /// <summary>
    /// Reads a tuple value.
    /// </summary>
    /// <typeparam name="T">The tuple to read.</typeparam>
    /// <returns>The read tuple.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadTuple<T>() where T : struct, ITuple => PacketReaderDels.Tuple<T>.Reader(this);

    /// <summary>
    /// Reads a generic value.
    /// </summary>
    /// <typeparam name="T">The type to read.</typeparam>
    /// <returns>The read value.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadObject<T>() => PacketReaderDels.Object<T>.Reader(this);

    /// <summary>
    /// Reads an optional value.
    /// </summary>
    /// <typeparam name="T">The value type to read.</typeparam>
    /// <returns>The read value, or null.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? ReadNullable<T>() => ReadPackedBool() ? ReadObject<T>() : default;

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

    /// <inheritdoc/>
    public override void MoveForward(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        EnsureReadable(count);
        position += count;
    }

    /// <inheritdoc/>
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

    private void SetBuffer(byte[] data, int size = -1, bool rented = false)
    {
        buffer = data;
        dataSize = size >= 0 && size <= data.Length ? size : data.Length;
        isRented = rented;
        Clear();
    }

    /// <inheritdoc/>
    protected override void OnRecycle()
    {
        if (isRented && buffer != null)
            ArrayPool<byte>.Shared.Return(buffer);
    }

    private ulong ReadVarInt(int maxShift)
    {
        if (IsRecycled)
            throw new InvalidOperationException("PacketReader is already recycled!");

        if (buffer == null)
            throw new InvalidOperationException("The buffer is somehow not set!");

        EndPackingBools();

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

    /// <inheritdoc/>
    public override void Dispose() => Return(this);

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
        CountType.VarUInt => (int)ReadPackedUInt(),
        _ => throw new ArgumentOutOfRangeException(nameof(countType), countType, "Unsupported count type.")
    };

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
}

/// <summary>
/// Reusable cached delegates to improve performance, add more for data types as needed to avoid excess GC overhead.
/// </summary>
[PublicApi]
public static class PacketReaderDels
{
    /// <summary>
    /// A delegate to read a byte.
    /// </summary>
    public static readonly ReadDel<byte> Byte = reader => reader.ReadByte();

    /// <summary>
    /// A delegate to read an sbyte.
    /// </summary>
    public static readonly ReadDel<sbyte> SByte = reader => reader.ReadSByte();

    /// <summary>
    /// A delegate to read a string.
    /// </summary>
    public static readonly ReadDel<string?> String = reader => reader.ReadString();

    /// <summary>
    /// A delegate to read a ushort.
    /// </summary>
    public static readonly ReadDel<ushort> UShort = reader => reader.ReadUShort();

    /// <summary>
    /// A delegate to read an int.
    /// </summary>
    public static readonly ReadDel<int> Int = reader => reader.ReadInt();

    /// <summary>
    /// A delegate to read a packed int.
    /// </summary>
    public static readonly ReadDel<int> PackedInt = reader => reader.ReadPackedInt();

    /// <summary>
    /// A delegate to read a boolean.
    /// </summary>
    public static readonly ReadDel<bool> Bool = reader => reader.ReadBool();

    /// <summary>
    /// A delegate to read a short.
    /// </summary>
    public static readonly ReadDel<short> Short = reader => reader.ReadShort();

    /// <summary>
    /// A delegate to read a uint.
    /// </summary>
    public static readonly ReadDel<uint> UInt = reader => reader.ReadUInt();

    /// <summary>
    /// A delegate to read a long.
    /// </summary>
    public static readonly ReadDel<long> Long = reader => reader.ReadLong();

    /// <summary>
    /// A delegate to read a ulong.
    /// </summary>
    public static readonly ReadDel<ulong> ULong = reader => reader.ReadULong();

    /// <summary>
    /// A delegate to read a float.
    /// </summary>
    public static readonly ReadDel<float> Float = reader => reader.ReadFloat();

    /// <summary>
    /// A delegate to read a double.
    /// </summary>
    public static readonly ReadDel<double> Double = reader => reader.ReadDouble();

    /// <summary>
    /// A delegate to read a packed uint.
    /// </summary>
    public static readonly ReadDel<uint> PackedUInt = reader => reader.ReadPackedUInt();

    /// <summary>
    /// A delegate to read a packed long.
    /// </summary>
    public static readonly ReadDel<long> PackedLong = reader => reader.ReadPackedLong();

    /// <summary>
    /// A delegate to read a packed ulong.
    /// </summary>
    public static readonly ReadDel<ulong> PackedULong = reader => reader.ReadPackedULong();

    /// <summary>
    /// A delegate to read a packed bool.
    /// </summary>
    public static readonly ReadDel<bool> PackedBool = reader => reader.ReadPackedBool();

    /// <summary>
    /// A delegate to read a Vector2.
    /// </summary>
    public static readonly ReadDel<Vector2> Vector2 = reader => reader.ReadVector2();

    /// <summary>
    /// A delegate to read a Vector3.
    /// </summary>
    public static readonly ReadDel<Vector3> Vector3 = reader => reader.ReadVector3();

    /// <summary>
    /// A delegate to read a Quaternion.
    /// </summary>
    public static readonly ReadDel<Quaternion> Quaternion = reader => reader.ReadQuaternion();

    /// <summary>
    /// A delegate to read a float4.
    /// </summary>
    public static readonly ReadDel<float4> Float4 = reader => reader.ReadFloat4();

    /// <summary>
    /// A delegate to read a Half.
    /// </summary>
    public static readonly ReadDel<Half> Half = reader => reader.ReadHalf();

    /// <summary>
    /// A delegate to read a decimal.
    /// </summary>
    public static readonly ReadDel<decimal> Decimal = reader => reader.ReadDecimal();

    /// <summary>
    /// A delegate to read a Color.
    /// </summary>
    public static readonly ReadDel<Color> Color = reader => reader.ReadColor();

    /// <summary>
    /// A delegate to read a Color32.
    /// </summary>
    public static readonly ReadDel<Color32> Color32 = reader => reader.ReadColor32();

    /// <summary>
    /// A delegate to read a DateTime.
    /// </summary>
    public static readonly ReadDel<DateTime> DateTime = reader => reader.ReadDateTime();

    /// <summary>
    /// A delegate to read a TimeSpan.
    /// </summary>
    public static readonly ReadDel<TimeSpan> TimeSpan = reader => reader.ReadTimeSpan();

    /// <summary>
    /// A delegate to read a Guid.
    /// </summary>
    public static readonly ReadDel<Guid> Guid = reader => reader.ReadGuid();

    /// <summary>
    /// A delegate to read a char.
    /// </summary>
    public static readonly ReadDel<char> Char = reader => reader.ReadChar();

    /// <summary>
    /// Caches a reading delegate for types implementing INetObject.
    /// </summary>
    /// <typeparam name="T">The net object type.</typeparam>
    public static class NetObject<T> where T : INetObject, new()
    {
        /// <summary>
        /// A delegate to read an INetObject.
        /// </summary>
        public static readonly ReadDel<T> Reader = reader => reader.ReadNetObject<T>();
    }

    /// <summary>
    /// Caches a reading delegate for optional values (nullables).
    /// </summary>
    /// <typeparam name="T">The object type.</typeparam>
    public static class Nullable<T>
    {
        /// <summary>
        /// A delegate to read an optional value.
        /// </summary>
        public static readonly ReadDel<T?> Reader = reader => reader.ReadNullable<T>();
    }

    /// <summary>
    /// Caches a reading delegate for value <see cref="Tuple"/>s.
    /// </summary>
    /// <typeparam name="T">The tuple type.</typeparam>
    /// <remarks>If you are using tuples of elements greater than 7 values...why? Just use an <see cref="INetObject"/> at that point.</remarks>
    public static class Tuple<T> where T : struct, ITuple
    {
        /// <summary>
        /// A delegate to read a <see cref="Tuple"/>.
        /// </summary>
        public static readonly ReadDel<T> Reader = CreateTupleReader();

        private static ReadDel<T> CreateTupleReader()
        {
            var readerParam = Expression.Parameter(typeof(PacketReader), "reader");
            var componentTypes = typeof(T).GetGenericArguments();
            var readCalls = new Expression[componentTypes.Length];
            var readObjectMethodDef = Method(nameof(PacketReader.ReadObject));

            var elemCount = Mathf.Min(componentTypes.Length, 7);

            for (var i = 0; i < elemCount; i++)
            {
                var genericRead = readObjectMethodDef.MakeGenericMethod(componentTypes[i]);
                readCalls[i] = Expression.Call(readerParam, genericRead);
            }

            if (componentTypes.Length == 8)
            {
                var genericRead = Method(nameof(PacketReader.ReadTuple)).MakeGenericMethod(componentTypes[7]);
                readCalls[7] = Expression.Call(readerParam, genericRead);
            }

            var constructor = typeof(T).GetConstructor(componentTypes) ?? throw new InvalidOperationException($"Could not find constructor for tuple {typeof(T)}");
            var newTuple = Expression.New(constructor, readCalls);
            return Expression.Lambda<ReadDel<T>>(newTuple, readerParam).Compile();
        }
    }

    /// <summary>
    /// Caches a reading delegate for custom objects that aren't natively supported.
    /// </summary>
    /// <typeparam name="T">The object type.</typeparam>
    public static class Object<T>
    {
        internal static volatile ReadDel<T> _reader = CreateReader();

        /// <summary>
        /// A delegate to read a value.
        /// </summary>
        public static ReadDel<T> Reader => _reader;

        private static ReadDel<T> CreateReader()
        {
            try
            {
                return (ReadDel<T>)Delegate.CreateDelegate(typeof(ReadDel<T>), GetReadExpression(typeof(T)));
            }
            catch
            {
                return _ => throw new NotSupportedException($"Type {typeof(T).Name} is not supported natively. Did you forget to register it?");
            }
        }
    }

    /// <summary>
    /// Caches a reading delegate for <see cref="Enum"/> types.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    public static class Enum<T> where T : struct, Enum
    {
        /// <summary>
        /// A delegate to read an enum value.
        /// </summary>
        public static readonly ReadDel<T> Reader = CreateReader();

        private static ReadDel<T> CreateReader()
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
    /// Caches a reading delegate for <see cref="Enum"/> types using a packed format.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    public static class PackedEnum<T> where T : struct, Enum
    {
        /// <summary>
        /// A delegate to read a packed enum value.
        /// </summary>
        public static readonly ReadDel<T> Reader = CreateReader();

        private static ReadDel<T> CreateReader()
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

    private static readonly ReadOnlyDictionary<Type, string> ReadMethodMap = new(new ConcurrentDictionary<Type, string>()
    {
        [typeof(int)] = nameof(PacketReader.ReadInt),
        [typeof(byte)] = nameof(PacketReader.ReadByte),
        [typeof(bool)] = nameof(PacketReader.ReadBool),
        [typeof(uint)] = nameof(PacketReader.ReadUInt),
        [typeof(long)] = nameof(PacketReader.ReadLong),
        [typeof(Half)] = nameof(PacketReader.ReadHalf),
        [typeof(Guid)] = nameof(PacketReader.ReadGuid),
        [typeof(char)] = nameof(PacketReader.ReadChar),
        [typeof(Color)] = nameof(PacketReader.ReadColor),
        [typeof(sbyte)] = nameof(PacketReader.ReadSByte),
        [typeof(short)] = nameof(PacketReader.ReadShort),
        [typeof(ulong)] = nameof(PacketReader.ReadULong),
        [typeof(float)] = nameof(PacketReader.ReadFloat),
        [typeof(ushort)] = nameof(PacketReader.ReadUShort),
        [typeof(double)] = nameof(PacketReader.ReadDouble),
        [typeof(string)] = nameof(PacketReader.ReadString),
        [typeof(float4)] = nameof(PacketReader.ReadFloat4),
        [typeof(decimal)] = nameof(PacketReader.ReadDecimal),
        [typeof(Vector2)] = nameof(PacketReader.ReadVector2),
        [typeof(Vector3)] = nameof(PacketReader.ReadVector3),
        [typeof(Color32)] = nameof(PacketReader.ReadColor32),
        [typeof(DateTime)] = nameof(PacketReader.ReadDateTime),
        [typeof(TimeSpan)] = nameof(PacketReader.ReadTimeSpan),
        [typeof(Quaternion)] = nameof(PacketReader.ReadQuaternion),
    });

    internal static class NetObjectFactory<T> where T : INetObject, new()
    {
        public static readonly Func<T> Factory = CreateFactory();

        private static Func<T> CreateFactory()
        {
            var newExp = Expression.New(typeof(T));
            var lambda = Expression.Lambda<Func<T>>(newExp);
            return lambda.Compile();
        }
    }

    internal static class ListDels<T>
    {
        public static readonly Func<int, List<T>> Create = count => new List<T>(count);
        public static readonly Action<List<T>, T> Add = (list, item) => list.Add(item);
    }

    internal static class HashSetDels<T>
    {
        public static readonly Func<int, HashSet<T>> Create = count => new HashSet<T>(count);
        public static readonly Action<HashSet<T>, T> Add = (set, item) => set.Add(item);
    }

    internal static class CppHashSetDels<T>
    {
        public static readonly Func<int, CppCollections.HashSet<T>> Create = _ => new CppCollections.HashSet<T>();
        public static readonly Action<CppCollections.HashSet<T>, T> Add = (set, item) => set.Add(item);
    }

    private static MethodInfo GetReadExpression(Type type)
    {
        if (TypeReadCache.TryGetValue(type, out var method))
            return method;

        if (ReadMethodMap.TryGetValue(type, out var methodName))
            method = Method(methodName);
        else if (type.IsEnum)
            method = Method(nameof(PacketReader.ReadEnum)).MakeGenericMethod(type);
        else if (typeof(ITuple).IsAssignableFrom(type) && type.IsValueType)
            method = Method(nameof(PacketReader.ReadTuple)).MakeGenericMethod(type);
        else if (typeof(IPacket).IsAssignableFrom(type))
            method = Method(nameof(PacketReader.ReadPacket)).MakeGenericMethod(type);
        else if (typeof(ICustomPacket).IsAssignableFrom(type))
            method = Method(nameof(PacketReader.ReadCustomPacket)).MakeGenericMethod(type);
        else if (typeof(INetObject).IsAssignableFrom(type))
            method = Method(nameof(PacketReader.ReadNetObject)).MakeGenericMethod(type);

        if (method == null)
            throw new NotSupportedException($"Type {type.Name} is not supported in automatic deserialization.");

        TypeReadCache.GetOrAdd(type, method);
        return method;
    }

    private static MethodInfo Method(string name) =>
        typeof(PacketReader).GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new MissingMethodException($"PacketReader missing method: {name}");
}