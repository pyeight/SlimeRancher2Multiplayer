using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Mathematics;

namespace SR2MP.Packets.Utils;

public sealed class PacketReader : PacketBuffer
{
    private int BytesRemaining => DataSize - position;

    public override int DataSize => buffer.Length;

    public PacketReader(byte[] data) : base(data, 8, BufferType.Reader) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureReadable(int bytesToRead)
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(PacketReader));

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
    public short ReadShort() => (short)ReadUShort();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort()
    {
        EnsureReadable(2);
        var value = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(position));
        position += 2;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt() => (int)ReadUInt();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt()
    {
        EnsureReadable(4);
        var value = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(position));
        position += 4;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLong() => (long)ReadULong();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadULong()
    {
        EnsureReadable(8);
        var value = BinaryPrimitives.ReadUInt64LittleEndian(buffer.AsSpan(position));
        position += 8;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat()
    {
        EnsureReadable(4);
        var value = BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(position));
        position += 4;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble()
    {
        EnsureReadable(8);
        var value = BinaryPrimitives.ReadDoubleLittleEndian(buffer.AsSpan(position));
        position += 8;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadPackedInt()
    {
        var val = ReadPackedUInt();
        return (int)(val >> 1) ^ -(int)(val & 1);
    }

    public uint ReadPackedUInt()
    {
        var result = 0u;
        var shift = 0;

        while (true)
        {
            var b = ReadByte();
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
        var result = 0ul;
        var shift = 0;

        while (true)
        {
            var b = ReadByte();
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
        EnsureReadable(8);

        var span = buffer.AsSpan(position);
        var x = BinaryPrimitives.ReadSingleLittleEndian(span);
        var y = BinaryPrimitives.ReadSingleLittleEndian(span[4..]);

        position += 8;
        return new(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 ReadVector3()
    {
        EnsureReadable(12);

        var span = buffer.AsSpan(position);
        var x = BinaryPrimitives.ReadSingleLittleEndian(span);
        var y = BinaryPrimitives.ReadSingleLittleEndian(span[4..]);
        var z = BinaryPrimitives.ReadSingleLittleEndian(span[8..]);

        position += 12;
        return new(x, y, z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quaternion ReadQuaternion()
    {
        EnsureReadable(16);

        var span = buffer.AsSpan(position);
        var x = BinaryPrimitives.ReadSingleLittleEndian(span);
        var y = BinaryPrimitives.ReadSingleLittleEndian(span[4..]);
        var z = BinaryPrimitives.ReadSingleLittleEndian(span[8..]);
        var w = BinaryPrimitives.ReadSingleLittleEndian(span[12..]);

        position += 16;
        return new(x, y, z, w);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float4 ReadFloat4()
    {
        EnsureReadable(16);

        var span = buffer.AsSpan(position);
        var x = BinaryPrimitives.ReadSingleLittleEndian(span);
        var y = BinaryPrimitives.ReadSingleLittleEndian(span[4..]);
        var z = BinaryPrimitives.ReadSingleLittleEndian(span[8..]);
        var w = BinaryPrimitives.ReadSingleLittleEndian(span[12..]);

        position += 16;
        return new(x, y, z, w);
    }

    public string ReadString()
    {
        var len = ReadUShort();

        if (len == 0)
            return string.Empty;

        EnsureReadable(len);
        var s = Encoding.UTF8.GetString(buffer, position, len);
        position += len;
        return s;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadEnum<T>() where T : struct, Enum => PacketReaderDels.Enum<T>.Func(this);

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

    // Stack overflow my beloved
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

        // Possibly the only time I'll ever use single line if statements; I'd rather DIE than do this again lmao
        if (type == typeof(byte)) method = Method(nameof(PacketReader.ReadByte));
        else if (type == typeof(int)) method = Method(nameof(PacketReader.ReadInt));
        else if (type == typeof(bool)) method = Method(nameof(PacketReader.ReadBool));
        else if (type == typeof(uint)) method = Method(nameof(PacketReader.ReadUInt));
        else if (type == typeof(long)) method = Method(nameof(PacketReader.ReadLong));
        else if (type == typeof(sbyte)) method = Method(nameof(PacketReader.ReadSByte));
        else if (type == typeof(short)) method = Method(nameof(PacketReader.ReadShort));
        else if (type == typeof(ulong)) method = Method(nameof(PacketReader.ReadULong));
        else if (type == typeof(float)) method = Method(nameof(PacketReader.ReadFloat));
        else if (type == typeof(ushort)) method = Method(nameof(PacketReader.ReadUShort));
        else if (type == typeof(double)) method = Method(nameof(PacketReader.ReadDouble));
        else if (type == typeof(string)) method = Method(nameof(PacketReader.ReadString));
        else if (type == typeof(float4)) method = Method(nameof(PacketReader.ReadFloat4));
        else if (type == typeof(Vector3)) method = Method(nameof(PacketReader.ReadVector3));
        else if (type == typeof(Quaternion)) method = Method(nameof(PacketReader.ReadQuaternion));
        else if (type.IsEnum) method = Method(nameof(PacketReader.ReadEnum)).MakeGenericMethod(type);
        else if (typeof(IPacket).IsAssignableFrom(type)) method = Method(nameof(PacketReader.ReadPacket)).MakeGenericMethod(type);
        else if (typeof(INetObject).IsAssignableFrom(type)) method = Method(nameof(PacketReader.ReadNetObject)).MakeGenericMethod(type);

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