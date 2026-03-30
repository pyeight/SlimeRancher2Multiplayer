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
/// A reusable reader for extracting primitive types, structs, and objects from a packet buffer.
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
    public override int DataSize => dataSize;

    private bool isRented;
    private int dataSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="PacketReader"/> class.
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
    /// Reads a single byte from the buffer.
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
    /// Reads a boolean value from the buffer.
    /// </summary>
    /// <returns>True if the byte is non-zero, otherwise false.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool() => ReadByte() != 0;

    /// <summary>
    /// Reads a signed byte from the buffer.
    /// </summary>
    /// <returns>The read signed byte.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte() => (sbyte)ReadByte();

    /// <summary>
    /// Reads a 16-bit signed integer using little-endian encoding.
    /// </summary>
    /// <returns>The read short.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShort() => BinaryPrimitives.ReadInt16LittleEndian(ReadRequest(2));

    /// <summary>
    /// Reads a 16-bit unsigned integer using little-endian encoding.
    /// </summary>
    /// <returns>The read unsigned short.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort() => BinaryPrimitives.ReadUInt16LittleEndian(ReadRequest(2));

    /// <summary>
    /// Reads a 32-bit signed integer using little-endian encoding.
    /// </summary>
    /// <returns>The read integer.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt() => BinaryPrimitives.ReadInt32LittleEndian(ReadRequest(4));

    /// <summary>
    /// Reads a 32-bit unsigned integer using little-endian encoding.
    /// </summary>
    /// <returns>The read unsigned integer.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt() => BinaryPrimitives.ReadUInt32LittleEndian(ReadRequest(4));

    /// <summary>
    /// Reads a 64-bit signed integer using little-endian encoding.
    /// </summary>
    /// <returns>The read long.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLong() => BinaryPrimitives.ReadInt64LittleEndian(ReadRequest(8));

    /// <summary>
    /// Reads a 64-bit unsigned integer using little-endian encoding.
    /// </summary>
    /// <returns>The read unsigned long.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadULong() => BinaryPrimitives.ReadUInt64LittleEndian(ReadRequest(8));

    /// <summary>
    /// Reads a double-precision floating-point number using little-endian encoding.
    /// </summary>
    /// <returns>The read double.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble() => BinaryPrimitives.ReadDoubleLittleEndian(ReadRequest(8));

    /// <summary>
    /// Reads a single-precision floating-point number using little-endian encoding.
    /// </summary>
    /// <returns>The read float.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat() => BinaryPrimitives.ReadSingleLittleEndian(ReadRequest(4));

    /// <summary>
    /// Reads a variable-length 32-bit signed integer (ZigZag encoded).
    /// </summary>
    /// <returns>The unpacked integer.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadPackedInt()
    {
        var val = ReadPackedUInt();
        return (int)(val >> 1) ^ -(int)(val & 1);
    }

    /// <summary>
    /// Reads a variable-length 32-bit unsigned integer.
    /// </summary>
    /// <returns>The unpacked unsigned integer.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    /// <exception cref="InvalidDataException">Thrown if the varint exceeds the maximum shift size.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadPackedUInt() => (uint)ReadVarInt(35);

    /// <summary>
    /// Reads a variable-length 64-bit signed integer (ZigZag encoded).
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
    /// Reads a variable-length 64-bit unsigned integer.
    /// </summary>
    /// <returns>The unpacked unsigned long.</returns>
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
    /// Reads a Vector2 structure composed of 2 floats.
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
    /// Reads a Vector3 structure composed of 3 floats.
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
    /// Reads a Quaternion structure composed of 4 floats.
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
    /// Reads a float4 structure composed of 4 floats.
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
    /// Reads a UTF-8 encoded string prefixed with its length as a ushort.
    /// </summary>
    /// <param name="returnNullOnZero">Indicates whether the method should return a null if the length given is zero.</param>
    /// <returns>The read string.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string? ReadString(bool returnNullOnZero = false) => ReadStringWithSize(ReadUShort(), returnNullOnZero);

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
    /// Reads an enum value of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the enum.</typeparam>
    /// <returns>The read enum value.</returns>
    /// <exception cref="NotSupportedException">Thrown if the enum size is not supported.</exception>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadEnum<T>() where T : struct, Enum => PacketReaderDels.Enum<T>.Func(this);

    /// <summary>
    /// Reads an enum by parsing a string representation.
    /// </summary>
    /// <typeparam name="T">The type of the enum.</typeparam>
    /// <returns>The parsed enum value.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadEnumFromString<T>() where T : struct, Enum => Enum.Parse<T>(ReadString()!);

    private TCollection ReadCollection<TCollection, T>(Func<int, TCollection> factory, Action<TCollection, T> add, Func<PacketReader, T> reader)
    {
        var count = ReadUShort();
        var collection = factory(count);

        for (var i = 0; i < count; i++)
            add(collection, reader(this));

        return collection;
    }

    /// <summary>
    /// Reads an array of the specified type, prefixed with its length as a ushort.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <param name="reader">The function to read individual elements.</param>
    /// <returns>The read array.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    public T[] ReadArray<T>(Func<PacketReader, T> reader)
    {
        var array = new T[ReadUShort()];

        for (var i = 0; i < array.Length; i++)
            array[i] = reader(this);

        return array;
    }

    /// <summary>
    /// Reads a List of the specified type, prefixed with its length as a ushort.
    /// </summary>
    /// <typeparam name="T">The list element type.</typeparam>
    /// <param name="reader">The function to read individual elements.</param>
    /// <returns>The read list.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<T> ReadList<T>(Func<PacketReader, T> reader)
        => ReadCollection(PacketReaderDels.ListFactory<T>.Func, PacketReaderDels.ListAdd<T>.Func, reader);

    /// <summary>
    /// Reads a HashSet of the specified type, prefixed with its length as a ushort.
    /// </summary>
    /// <typeparam name="T">The HashSet element type.</typeparam>
    /// <param name="reader">The function to read individual elements.</param>
    /// <returns>The read HashSet.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HashSet<T> ReadSet<T>(Func<PacketReader, T> reader)
        => ReadCollection(PacketReaderDels.HashSetFactory<T>.Func, PacketReaderDels.HashSetAdd<T>.Func, reader);

    /// <summary>
    /// Reads a custom CppCollections HashSet of the specified type, prefixed with its length as a ushort.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="reader">The function to read individual elements.</param>
    /// <returns>The read HashSet.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CppCollections.HashSet<T> ReadCppSet<T>(Func<PacketReader, T> reader)
        => ReadCollection(PacketReaderDels.CppHashSetFactory<T>.Func, PacketReaderDels.CppHashSetAdd<T>.Func, reader);

    /// <summary>
    /// Reads a Dictionary, prefixed with its entry count as a ushort.
    /// </summary>
    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <param name="keyReader">The function to read individual keys.</param>
    /// <param name="valueReader">The function to read individual values.</param>
    /// <returns>The read Dictionary.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(Func<PacketReader, TKey> keyReader, Func<PacketReader, TValue> valueReader) where TKey : notnull
    {
        var count = ReadUShort();
        var dict = new Dictionary<TKey, TValue>(count);

        for (var i = 0; i < count; i++)
            dict[keyReader(this)] = valueReader(this);

        return dict;
    }

    /// <summary>
    /// Reads an object that implements <see cref="INetObject"/> by invoking its Deserialise method.
    /// </summary>
    /// <typeparam name="T">The type of the net object.</typeparam>
    /// <returns>The deserialized object.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadNetObject<T>() where T : INetObject, new()
    {
        var result = PacketReaderDels.NetObjectFactory<T>.Func();
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
    /// Reads a custom implemented packet.
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
    /// Finalizes reading packed booleans and realigns the reader for byte-level operations.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void EndPackingBools() => currentBitIndex = 8;

    /// <summary>
    /// Reads a struct value.
    /// </summary>
    /// <typeparam name="T">The struct type to read.</typeparam>
    /// <returns>The read struct.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadStruct<T>() where T : struct => PacketReaderDels.Struct<T>.Reader(this);

    /// <summary>
    /// Reads an optional struct value, returning null if the boolean flag is false.
    /// </summary>
    /// <typeparam name="T">The struct type to read.</typeparam>
    /// <returns>The read struct, or null.</returns>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? ReadNullable<T>() where T : struct => ReadBool() ? ReadStruct<T>() : null;

    /// <summary>
    /// Reads an enum value optimized with varint packing for its underlying type.
    /// </summary>
    /// <typeparam name="T">The type of the enum.</typeparam>
    /// <returns>The read enum value.</returns>
    /// <exception cref="NotSupportedException">Thrown if the enum size is not supported.</exception>
    /// <inheritdoc cref="EnsureReadable"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadPackedEnum<T>() where T : struct, Enum => PacketReaderDels.PackedEnum<T>.Func(this);

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
        dataSize = size >= 0 ? size : data.Length;
        isRented = rented;
        Clear();
    }

    protected override void OnRecycle()
    {
        if (isRented && buffer != null)
            ArrayPool<byte>.Shared.Return(buffer);
    }

    /// <summary>
    /// Borrows a <see cref="PacketReader"/> instance from the recycle pool and initializes it with the specified data.
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

    /// <summary>
    /// Reads a variable-length numeric value.
    /// </summary>
    /// <param name="maxShift">The maximum number of shifts that the packing can encode.</param>
    /// <returns>A variable-length number.</returns>
    /// <exception cref="EndOfStreamException">Thrown if there are not enough bytes left.</exception>
    /// <exception cref="InvalidDataException">Thrown if the varint exceeds the maximum shift size.</exception>
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
        public static readonly Func<PacketReader, T> Func = reader => reader.ReadNetObject<T>();
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
        public static readonly Func<PacketReader, (T1, T2)> Func = CreateTupleReader<(T1, T2)>(typeof(T1), typeof(T2));
    }

    /// <summary>
    /// Caches a dynamically generated reading delegate for custom structs.
    /// </summary>
    /// <typeparam name="T">The struct type.</typeparam>
    public static class Struct<T> where T : struct
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
        public static readonly Func<PacketReader, T> Func = CreateReader();

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
        public static readonly Func<PacketReader, T> Func = CreateReader();

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
        public static readonly Func<T> Func = CreateFactory();

        private static Func<T> CreateFactory()
        {
            var newExp = Expression.New(typeof(T));
            var lambda = Expression.Lambda<Func<T>>(newExp);
            return lambda.Compile();
        }
    }

    /// <summary>
    /// Factory delegate cache for constructing generic Lists.
    /// </summary>
    /// <typeparam name="T">The list element type.</typeparam>
    public static class ListFactory<T>
    {
        /// <summary>
        /// A delegate that instantiates a List of size count.
        /// </summary>
        public static readonly Func<int, List<T>> Func = count => new List<T>(count);
    }

    /// <summary>
    /// Add method delegate cache for generic Lists.
    /// </summary>
    /// <typeparam name="T">The list element type.</typeparam>
    public static class ListAdd<T>
    {
        /// <summary>
        /// A delegate that adds an item to a list.
        /// </summary>
        public static readonly Action<List<T>, T> Func = (list, item) => list.Add(item);
    }

    /// <summary>
    /// Factory delegate cache for constructing HashSets.
    /// </summary>
    /// <typeparam name="T">The set element type.</typeparam>
    public static class HashSetFactory<T>
    {
        /// <summary>
        /// A delegate that instantiates a HashSet with capacity.
        /// </summary>
        public static readonly Func<int, HashSet<T>> Func = count => new HashSet<T>(count);
    }

    /// <summary>
    /// Add method delegate cache for HashSets.
    /// </summary>
    /// <typeparam name="T">The set element type.</typeparam>
    public static class HashSetAdd<T>
    {
        /// <summary>
        /// A delegate that adds an item to a HashSet.
        /// </summary>
        public static readonly Action<HashSet<T>, T> Func = (set, item) => set.Add(item);
    }

    /// <summary>
    /// Factory delegate cache for constructing custom CppCollections HashSets.
    /// </summary>
    /// <typeparam name="T">The set element type.</typeparam>
    public static class CppHashSetFactory<T>
    {
        /// <summary>
        /// A delegate that instantiates a CppCollections HashSet.
        /// </summary>
        public static readonly Func<int, CppCollections.HashSet<T>> Func = _ => new CppCollections.HashSet<T>();
    }

    /// <summary>
    /// Add method delegate cache for custom CppCollections HashSets.
    /// </summary>
    /// <typeparam name="T">The set element type.</typeparam>
    public static class CppHashSetAdd<T>
    {
        /// <summary>
        /// A delegate that adds an item to a CppCollections HashSet.
        /// </summary>
        public static readonly Action<CppCollections.HashSet<T>, T> Func = (set, item) => set.Add(item);
    }
}