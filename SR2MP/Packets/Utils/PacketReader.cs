using System.Runtime.CompilerServices;
using System.Text;
using Unity.Mathematics;

namespace SR2MP.Packets.Utils;

public sealed class PacketReader : IDisposable
{
    private readonly MemoryStream _stream;
    private readonly BinaryReader _reader;

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
    public Vector3 ReadVector3() => new Vector3(_reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quaternion ReadQuaternion() => new Quaternion(_reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float4 ReadFloat4() => new float4(_reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle());

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

    public T ReadPacket<T>() where T : IPacket, new()
    {
        var result = new T();
        result.Deserialise(this);
        return result;
    }

    public void Skip(int count) => _stream.Position += count;

    public void Dispose()
    {
        _reader.Dispose();
        _stream.Dispose();
        // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor
        GC.SuppressFinalize(this);
    }
}

public static class PacketReaderDels
{
    public static readonly Func<PacketReader, byte> Byte = reader => reader.ReadByte();
    public static readonly Func<PacketReader, sbyte> SByte = reader => reader.ReadSByte();
    public static readonly Func<PacketReader, string> String = reader => reader.ReadString();

    public static class Packet<T> where T : IPacket, new()
    {
        public static readonly Func<PacketReader, T> Func = reader => reader.ReadPacket<T>();
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
}