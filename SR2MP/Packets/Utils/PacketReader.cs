using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Mathematics;

namespace SR2MP.Packets.Utils;

public sealed class PacketReader : IDisposable
{
    private readonly MemoryStream _stream;
    private readonly BinaryReader _reader;

    private byte _currentPackedByte;
    private int _currentBitIndex = 8;

    public PacketReader(byte[] data)
    {
        _stream = new MemoryStream(data);
        _reader = new BinaryReader(_stream, Encoding.UTF8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte() => _reader.ReadByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte() => _reader.ReadSByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt() => _reader.ReadInt32();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLong() => _reader.ReadInt64();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat() => _reader.ReadSingle();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble() => _reader.ReadDouble();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShort() => _reader.ReadInt16();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort() => _reader.ReadUInt16();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt() => _reader.ReadUInt32();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadULong() => _reader.ReadUInt64();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString() => _reader.ReadString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool() => _reader.ReadBoolean();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadEnum<T>() where T : struct, Enum => PacketReaderDels.Enum<T>.Func(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 ReadVector3() => new(_reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quaternion ReadQuaternion() => new(_reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float4 ReadFloat4() => new(_reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle());

    public T[] ReadArray<T>(Func<PacketReader, T> reader)
    {
        var array = new T[_reader.ReadInt32()];

        for (var i = 0; i < array.Length; i++)
            array[i] = reader(this);

        return array;
    }

    public List<T> ReadList<T>(Func<PacketReader, T> reader)
    {
        var count = _reader.ReadInt32();
        var list = new List<T>(count);

        for (var i = 0; i < count; i++)
            list.Add(reader(this));

        return list;
    }

    public HashSet<T> ReadSet<T>(Func<PacketReader, T> reader)
    {
        var count = _reader.ReadInt32();
        var list = new HashSet<T>(count);

        for (var i = 0; i < count; i++)
            list.Add(reader(this));

        return list;
    }

    public CppCollections.List<T> ReadCppList<T>(Func<PacketReader, T> reader)
    {
        var count = _reader.ReadInt32();
        var list = new CppCollections.List<T>(count);

        for (var i = 0; i < count; i++)
            list.Add(reader(this));

        return list;
    }

    public CppCollections.HashSet<T> ReadCppSet<T>(Func<PacketReader, T> reader)
    {
        var count = _reader.ReadInt32();
        var list = new CppCollections.HashSet<T>();

        for (var i = 0; i < count; i++)
            list.Add(reader(this));

        return list;
    }

    public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(Func<PacketReader, TKey> keyReader, Func<PacketReader, TValue> valueReader) where TKey : notnull
    {
        var count = _reader.ReadInt32();
        var dict = new Dictionary<TKey, TValue>(count);

        for (var i = 0; i < count; i++)
            dict[keyReader(this)] = valueReader(this);

        return dict;
    }

    public T ReadNetObject<T>() where T : INetObject, new()
    {
        var result = new T();
        result.Deserialise(this);
        return result;
    }

    public T ReadPacket<T>() where T : IPacket, new()
    {
        _stream.Position++;
        return ReadNetObject<T>();
    }

    public bool ReadPackedBool()
    {
        if (_currentBitIndex >= 8)
        {
            _currentPackedByte = _reader.ReadByte();
            _currentBitIndex = 0;
        }

        var value = (_currentPackedByte & (1 << _currentBitIndex)) != 0;
        _currentBitIndex++;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndPackingBools() => _currentBitIndex = 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Skip(int count) => _stream.Position += count;

    public void Dispose()
    {
        _reader.Dispose();
        _stream.Dispose();
        // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor
        GC.SuppressFinalize(this);
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
    public static readonly Func<PacketReader, float> Float = reader => reader.ReadFloat();

    public static class Packet<T> where T : IPacket, new()
    {
        public static readonly Func<PacketReader, T> Func = reader => reader.ReadPacket<T>();
    }

    public static class NetObject<T> where T : INetObject, new()
    {
        public static readonly Func<PacketReader, T> Func = reader => reader.ReadNetObject<T>();
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

    public static class Tuple<T1, T2>
    {
        public static readonly Func<PacketReader, (T1, T2)> Func = CreateReader();

        private static Func<PacketReader, (T1, T2)> CreateReader() => TupleResolver.CreateTupleReader<(T1, T2)>(typeof(T1), typeof(T2));
    }

    private static class TupleResolver
    {
        private static readonly Dictionary<Type, MethodInfo> TypeReadCache = new();

        // Stack overflow my beloved
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
                throw new NotSupportedException($"Type {type.Name} is not supported in automatic Tuple deserialization.");

            TypeReadCache[type] = method;
            return method;
        }

        private static MethodInfo Method(string name) =>
            typeof(PacketReader).GetMethod(name, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new MissingMethodException($"PacketReader missing method: {name}");
    }
}