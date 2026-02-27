using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using SR2MP.Shared.Utils;
using Unity.Mathematics;

namespace SR2MP.Packets.Utils;

public sealed class PacketReader : PacketBuffer
{
    public int BytesRemaining => DataSize - position;

    public override int DataSize => dataSize;

    private bool isRented;
    private int dataSize;

    public PacketReader() : base(8) { }

    private void EnsureReadable(int bytesToRead)
    {
        if (IsRecycled)
            throw new InvalidOperationException("PacketReader is already recycled!");

        if (position + bytesToRead > DataSize)
            throw new EndOfStreamException($"Attempted to read {bytesToRead} bytes, but only {BytesRemaining} remain.");

        EndPackingBools();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
        EnsureReadable(1);
        return buffer[position++];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool() => ReadByte() != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte() => (sbyte)ReadByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShort() => BinaryPrimitives.ReadInt16LittleEndian(ReadRequest(2));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort() => BinaryPrimitives.ReadUInt16LittleEndian(ReadRequest(2));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt() => BinaryPrimitives.ReadInt32LittleEndian(ReadRequest(4));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt() => BinaryPrimitives.ReadUInt32LittleEndian(ReadRequest(4));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLong() => BinaryPrimitives.ReadInt64LittleEndian(ReadRequest(8));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadULong() => BinaryPrimitives.ReadUInt64LittleEndian(ReadRequest(8));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble() => BinaryPrimitives.ReadDoubleLittleEndian(ReadRequest(8));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat() => BinaryPrimitives.ReadSingleLittleEndian(ReadRequest(4));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadPackedInt()
    {
        var val = ReadPackedUInt();
        return (int)(val >> 1) ^ -(int)(val & 1);
    }

    public uint ReadPackedUInt()
    {
        EnsureReadable(1);

        var result = 0u;
        var shift = 0;

        while (true)
        {
            if (position >= DataSize)
                throw new EndOfStreamException("Unexpected end of stream while reading VarInt.");

            var b = buffer[position++];
            result |= (uint)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
                break;

            shift += 7;

            if (shift >= 35)
                throw new InvalidDataException("VarInt too long");
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadPackedLong()
    {
        var val = ReadPackedULong();
        return (long)(val >> 1) ^ -(long)(val & 1);
    }

    public ulong ReadPackedULong()
    {
        EnsureReadable(1);

        var result = 0ul;
        var shift = 0;

        while (true)
        {
            if (position >= DataSize)
                throw new EndOfStreamException("Unexpected end of stream while reading VarInt.");

            var b = buffer[position++];
            result |= (ulong)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
                break;

            shift += 7;

            if (shift >= 70)
                throw new InvalidDataException("VarInt too long");
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 ReadVector2()
    {
        var span = ReadRequest(8);
        return new(BinaryPrimitives.ReadSingleLittleEndian(span),
                   BinaryPrimitives.ReadSingleLittleEndian(span[4..]));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 ReadVector3()
    {
        var span = ReadRequest(12);
        return new(BinaryPrimitives.ReadSingleLittleEndian(span),
                   BinaryPrimitives.ReadSingleLittleEndian(span[4..]),
                   BinaryPrimitives.ReadSingleLittleEndian(span[8..]));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quaternion ReadQuaternion()
    {
        var span = ReadRequest(16);
        return new(BinaryPrimitives.ReadSingleLittleEndian(span),
                   BinaryPrimitives.ReadSingleLittleEndian(span[4..]),
                   BinaryPrimitives.ReadSingleLittleEndian(span[8..]),
                   BinaryPrimitives.ReadSingleLittleEndian(span[12..]));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float4 ReadFloat4()
    {
        var span = ReadRequest(16);
        return new(BinaryPrimitives.ReadSingleLittleEndian(span),
                   BinaryPrimitives.ReadSingleLittleEndian(span[4..]),
                   BinaryPrimitives.ReadSingleLittleEndian(span[8..]),
                   BinaryPrimitives.ReadSingleLittleEndian(span[12..]));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString() => ReadStringWithSize(ReadUShort())!;

    public string? ReadStringWithSize(int len)
    {
        if (len < 0)
            return null;

        if (len == 0)
            return string.Empty;

        EnsureReadable(len);
        var s = Encoding.UTF8.GetString(buffer.AsSpan(position, len));
        position += len;
        return s;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadEnum<T>() where T : struct, Enum => PacketReaderDels.Enum<T>.Func(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadEnumFromString<T>() where T : struct, Enum => Enum.Parse<T>(ReadString());

    public T[] ReadArray<T>(Func<PacketReader, T> reader)
    {
        var array = new T[ReadUShort()];

        for (var i = 0; i < array.Length; i++)
            array[i] = reader(this);

        return array;
    }

    public List<T> ReadList<T>(Func<PacketReader, T> reader)
    {
        var count = ReadUShort();
        var list = new List<T>(count);

        for (var i = 0; i < count; i++)
            list.Add(reader(this));

        return list;
    }

    public HashSet<T> ReadSet<T>(Func<PacketReader, T> reader)
    {
        var count = ReadUShort();
        var list = new HashSet<T>(count);

        for (var i = 0; i < count; i++)
            list.Add(reader(this));

        return list;
    }

    // public CppCollections.List<T> ReadCppList<T>(Func<PacketReader, T> reader)
    // {
    //     var count = ReadUShort();
    //     var list = new CppCollections.List<T>(count);

    //     for (var i = 0; i < count; i++)
    //         list.Add(reader(this));

    //     return list;
    // }

    public CppCollections.HashSet<T> ReadCppSet<T>(Func<PacketReader, T> reader)
    {
        var count = ReadUShort();
        var list = new CppCollections.HashSet<T>();

        for (var i = 0; i < count; i++)
            list.Add(reader(this));

        return list;
    }

    public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(Func<PacketReader, TKey> keyReader, Func<PacketReader, TValue> valueReader) where TKey : notnull
    {
        var count = ReadUShort();
        var dict = new Dictionary<TKey, TValue>(count);

        for (var i = 0; i < count; i++)
            dict[keyReader(this)] = valueReader(this);

        return dict;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadNetObject<T>() where T : INetObject, new()
    {
        var result = PacketReaderDels.NetObjectFactory<T>.Func();
        result.Deserialise(this);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadPacket<T>() where T : IPacket, new()
    {
        EnsureReadable(1);
        position++;
        return ReadNetObject<T>();
    }

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

    public override void EndPackingBools() => currentBitIndex = 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T ReadStruct<T>() where T : struct => PacketReaderDels.Struct<T>.Reader(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? ReadNullable<T>() where T : struct => ReadBool() ? ReadStruct<T>() : null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadPackedEnum<T>() where T : struct, Enum => PacketReaderDels.PackedEnum<T>.Func(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadToSpan(Span<byte> destination)
    {
        EnsureReadable(destination.Length);
        buffer.AsSpan(position, destination.Length).CopyTo(destination);
        position += destination.Length;
    }

    protected override void EnsureBounds(int count) => EnsureReadable(count);

    public override void MoveForward(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        EnsureReadable(count);
        position += count;
    }

    public override void MoveBack(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        if (position < count)
            throw new InvalidOperationException("Cannot return to a position before the start of the stream!");

        EndPackingBools();
        position -= count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> ReadRequest(int size)
    {
        EnsureReadable(size);
        var span = buffer.AsSpan(position, size);
        position += size;
        return span;
    }

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

    public static PacketReader Borrow(byte[] data, int size = -1, bool rented = false)
    {
        var reader = RecyclePool<PacketReader>.Borrow();
        reader.SetBuffer(data, size, rented);
        return reader;
    }

    public static void Return(PacketReader reader) => RecyclePool<PacketReader>.Return(reader);
}

/// <summary>
/// Reusable cached delegates to improve performance, add more for data types as needed to avoid excess GC overhead
/// </summary>
public static class PacketReaderDels
{
    public static readonly Func<PacketReader, byte> Byte = reader => reader.ReadByte();
    public static readonly Func<PacketReader, sbyte> SByte = reader => reader.ReadSByte();
    public static readonly Func<PacketReader, string> String = reader => reader.ReadString();
    public static readonly Func<PacketReader, ushort> UShort = reader => reader.ReadUShort();

    public static class NetObject<T> where T : INetObject, new()
    {
        public static readonly Func<PacketReader, T> Func = reader => reader.ReadNetObject<T>();
    }

    public static class Tuple<T1, T2>
    {
        public static readonly Func<PacketReader, (T1, T2)> Func = CreateTupleReader<(T1, T2)>(typeof(T1), typeof(T2));
    }

    public static class Struct<T> where T : struct
    {
        public static readonly Func<PacketReader, T> Reader = (Func<PacketReader, T>)Delegate.CreateDelegate(typeof(Func<PacketReader, T>), GetReadExpression(typeof(T)));
    }

    public static class Enum<T> where T : struct, Enum
    {
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

    public static class PackedEnum<T> where T : struct, Enum
    {
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

    private static Func<PacketReader, TTuple> CreateTupleReader<TTuple>(params Type[] componentTypes)
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
        else if (typeof(INetObject).IsAssignableFrom(type))
            method = Method(nameof(PacketReader.ReadNetObject)).MakeGenericMethod(type);

        if (method == null)
            throw new NotSupportedException($"Type {type.Name} is not supported in automatic deserialization.");

        TypeReadCache[type] = method;
        return method;
    }

    private static MethodInfo Method(string name) =>
        typeof(PacketReader).GetMethod(name, BindingFlags.Instance | BindingFlags.Public)
        ?? throw new MissingMethodException($"PacketReader missing method: {name}");

    public static class NetObjectFactory<T> where T : INetObject, new()
    {
        public static readonly Func<T> Func = CreateFactory();

        private static Func<T> CreateFactory()
        {
            var newExp = Expression.New(typeof(T));
            var lambda = Expression.Lambda<Func<T>>(newExp);
            return lambda.Compile();
        }
    }
}