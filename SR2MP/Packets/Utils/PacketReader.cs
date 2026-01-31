using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using LiteNetLib.Utils;
using Unity.Mathematics;

namespace SR2MP.Packets.Utils;

public sealed class PacketReader : IDisposable
{
    private readonly NetDataReader _reader;

    private byte _currentPackedByte;
    private int _currentBitIndex = 8;

    public PacketReader(byte[] data)
    {
        _reader = new NetDataReader(data);
    }

    public PacketReader(NetDataReader reader)
    {
        _reader = reader;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte() => _reader.GetByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte() => _reader.GetSByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt() => _reader.GetInt();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLong() => _reader.GetLong();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat() => _reader.GetFloat();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble() => _reader.GetDouble();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShort() => _reader.GetShort();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort() => _reader.GetUShort();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt() => _reader.GetUInt();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadULong() => _reader.GetULong();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString() => _reader.GetString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool() => _reader.GetBool();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadEnum<T>() where T : struct, Enum => PacketReaderDels.Enum<T>.Func(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 ReadVector3() => new(_reader.GetFloat(), _reader.GetFloat(), _reader.GetFloat());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quaternion ReadQuaternion() => new(_reader.GetFloat(), _reader.GetFloat(), _reader.GetFloat(), _reader.GetFloat());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float4 ReadFloat4() => new(_reader.GetFloat(), _reader.GetFloat(), _reader.GetFloat(), _reader.GetFloat());

    public T[] ReadArray<T>(Func<PacketReader, T> reader)
    {
        var array = new T[_reader.GetInt()];

        for (var i = 0; i < array.Length; i++)
            array[i] = reader(this);

        return array;
    }

    public List<T> ReadList<T>(Func<PacketReader, T> reader)
    {
        var count = _reader.GetInt();
        var list = new List<T>(count);

        for (var i = 0; i < count; i++)
            list.Add(reader(this));

        return list;
    }

    public HashSet<T> ReadSet<T>(Func<PacketReader, T> reader)
    {
        var count = _reader.GetInt();
        var list = new HashSet<T>(count);

        for (var i = 0; i < count; i++)
            list.Add(reader(this));

        return list;
    }

    public CppCollections.List<T> ReadCppList<T>(Func<PacketReader, T> reader)
    {
        var count = _reader.GetInt();
        var list = new CppCollections.List<T>(count);

        for (var i = 0; i < count; i++)
            list.Add(reader(this));

        return list;
    }

    public CppCollections.HashSet<T> ReadCppSet<T>(Func<PacketReader, T> reader)
    {
        var count = _reader.GetInt();
        var list = new CppCollections.HashSet<T>();

        for (var i = 0; i < count; i++)
            list.Add(reader(this));

        return list;
    }

    public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(Func<PacketReader, TKey> keyReader, Func<PacketReader, TValue> valueReader) where TKey : notnull
    {
        var count = _reader.GetInt();
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

    public T ReadPacket<T>() where T : PacketBase, new()
    {
        _reader.GetByte();
        return ReadNetObject<T>();
    }

    public bool ReadPackedBool()
    {
        if (_currentBitIndex >= 8)
        {
            _currentPackedByte = _reader.GetByte();
            _currentBitIndex = 0;
        }

        var value = (_currentPackedByte & (1 << _currentBitIndex)) != 0;
        _currentBitIndex++;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndPackingBools() => _currentBitIndex = 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Skip(int count) => _reader.SkipBytes(count);

    public void Dispose()
    {
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

    public static class Packet<T> where T : PacketBase, new()
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
                1 => reader => Unsafe.As<byte, T>(ref Unsafe.AsRef(reader.ReadByte())),
                2 => reader => Unsafe.As<ushort, T>(ref Unsafe.AsRef(reader.ReadUShort())),
                4 => reader => Unsafe.As<uint, T>(ref Unsafe.AsRef(reader.ReadUInt())),
                8 => reader => Unsafe.As<ulong, T>(ref Unsafe.AsRef(reader.ReadULong())),
                _ => throw new ArgumentException($"Enum size {size} not supported")
            };
        }
    }

    public static class Tuple<T1, T2>
    {
        public static readonly Func<PacketReader, (T1, T2)> Func = CreateReader();

        private static Func<PacketReader, (T1, T2)> CreateReader() => TupleResolver.CreateTupleReader<(T1, T2)>(typeof(T1), typeof(T2));
    }

    // See PacketWriterDels for the comments, they're pretty much the same
    private static class TupleResolver
    {
        private static readonly Dictionary<Type, MethodInfo> TypeReadCache = new();

        public static Func<PacketReader, TTuple> CreateTupleReader<TTuple>(params Type[] componentTypes)
        {
            var readerParam = Expression.Parameter(typeof(PacketReader), "reader");

            var readCalls = new Expression[componentTypes.Length];
            for (var i = 0; i < componentTypes.Length; i++)
                readCalls[i] = Expression.Call(readerParam, GetReadExpression(componentTypes[i]));

            var block = Expression.New(typeof(TTuple).GetConstructor(componentTypes) ?? throw new MissingMethodException("Tuple constructor not found"), readCalls);

            return Expression.Lambda<Func<PacketReader, TTuple>>(block, readerParam).Compile();
        }

        private static MethodInfo GetReadExpression(Type type)
        {
            if (TypeReadCache.TryGetValue(type, out var method))
                return method;

            if (type == typeof(byte)) method = Method(nameof(PacketReader.ReadByte));
            else if (type == typeof(int)) method = Method(nameof(PacketReader.ReadInt));
            else if (type == typeof(uint)) method = Method(nameof(PacketReader.ReadUInt));
            else if (type == typeof(long)) method = Method(nameof(PacketReader.ReadLong));
            else if (type == typeof(bool)) method = Method(nameof(PacketReader.ReadBool));
            else if (type == typeof(short)) method = Method(nameof(PacketReader.ReadShort));
            else if (type == typeof(ulong)) method = Method(nameof(PacketReader.ReadULong));
            else if (type == typeof(sbyte)) method = Method(nameof(PacketReader.ReadSByte));
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
