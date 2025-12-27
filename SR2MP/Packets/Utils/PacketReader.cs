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

    public byte ReadByte() => _reader.ReadByte();

    public sbyte ReadSByte() => _reader.ReadSByte();

    public int ReadInt() => _reader.ReadInt32();

    public long ReadLong() => _reader.ReadInt64();

    public float ReadFloat() => _reader.ReadSingle();

    public double ReadDouble() => _reader.ReadDouble();

    public string ReadString() => _reader.ReadString();

    public bool ReadBool() => _reader.ReadBoolean();

    public Vector3 ReadVector3() => new Vector3(_reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle());

    public Quaternion ReadQuaternion() => new Quaternion(_reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle());

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

    public List<T> ReadEnumList<T>() where T : struct, Enum
    {
        var count = _reader.ReadInt32();
        var list = new List<T>(count);

        var size = Unsafe.SizeOf<T>();

        switch (size)
        {
            case 1:
            {
                for (var j = 0; j < count; j++)
                {
                    var b = _reader.ReadByte();
                    list.Add(Unsafe.As<byte, T>(ref b));
                }
                break;
            }
            case 2:
            {
                for (var j = 0; j < count; j++)
                {
                    var s = _reader.ReadUInt16();
                    list.Add(Unsafe.As<ushort, T>(ref s));
                }
                break;
            }
            case 4:
            {
                for (var j = 0; j < count; j++)
                {
                    var i = _reader.ReadUInt32();
                    list.Add(Unsafe.As<uint, T>(ref i));
                }
                break;
            }
            case 8:
            {
                for (var j = 0; j < count; j++)
                {
                    var l = _reader.ReadUInt64();
                    list.Add(Unsafe.As<ulong, T>(ref l));
                }
                break;
            }
            default:
                throw new NotSupportedException($"Enum size {size} not supported");
        }

        return list;
    }

    public HashSet<T> ReadEnumSet<T>() where T : struct, Enum
    {
        var count = _reader.ReadInt32();
        var set = new HashSet<T>(count);

        var size = Unsafe.SizeOf<T>();

        switch (size)
        {
            case 1:
            {
                for (var j = 0; j < count; j++)
                {
                    var b = _reader.ReadByte();
                    set.Add(Unsafe.As<byte, T>(ref b));
                }
                break;
            }
            case 2:
            {
                for (var j = 0; j < count; j++)
                {
                    var s = _reader.ReadUInt16();
                    set.Add(Unsafe.As<ushort, T>(ref s));
                }
                break;
            }
            case 4:
            {
                for (var j = 0; j < count; j++)
                {
                    var i = _reader.ReadUInt32();
                    set.Add(Unsafe.As<uint, T>(ref i));
                }
                break;
            }
            case 8:
            {
                for (var j = 0; j < count; j++)
                {
                    var l = _reader.ReadUInt64();
                    set.Add(Unsafe.As<ulong, T>(ref l));
                }
                break;
            }
            default:
                throw new NotSupportedException($"Enum size {size} not supported");
        }

        return set;
    }

    public CppCollections.List<T> ReadCppEnumList<T>() where T : struct, Enum
    {
        var count = _reader.ReadInt32();
        var list = new CppCollections.List<T>(count);

        var size = Unsafe.SizeOf<T>();

        switch (size)
        {
            case 1:
            {
                for (var j = 0; j < count; j++)
                {
                    var b = _reader.ReadByte();
                    list.Add(Unsafe.As<byte, T>(ref b));
                }
                break;
            }
            case 2:
            {
                for (var j = 0; j < count; j++)
                {
                    var s = _reader.ReadUInt16();
                    list.Add(Unsafe.As<ushort, T>(ref s));
                }
                break;
            }
            case 4:
            {
                for (var j = 0; j < count; j++)
                {
                    var i = _reader.ReadUInt32();
                    list.Add(Unsafe.As<uint, T>(ref i));
                }
                break;
            }
            case 8:
            {
                for (var j = 0; j < count; j++)
                {
                    var l = _reader.ReadUInt64();
                    list.Add(Unsafe.As<ulong, T>(ref l));
                }
                break;
            }
            default:
                throw new NotSupportedException($"Enum size {size} not supported");
        }

        return list;
    }

    public CppCollections.HashSet<T> ReadCppEnumSet<T>() where T : struct, Enum
    {
        var count = _reader.ReadInt32();
        var list = new CppCollections.HashSet<T>();

        var size = Unsafe.SizeOf<T>();

        switch (size)
        {
            case 1:
            {
                for (var j = 0; j < count; j++)
                {
                    var b = _reader.ReadByte();
                    list.Add(Unsafe.As<byte, T>(ref b));
                }
                break;
            }
            case 2:
            {
                for (var j = 0; j < count; j++)
                {
                    var s = _reader.ReadUInt16();
                    list.Add(Unsafe.As<ushort, T>(ref s));
                }
                break;
            }
            case 4:
            {
                for (var j = 0; j < count; j++)
                {
                    var i = _reader.ReadUInt32();
                    list.Add(Unsafe.As<uint, T>(ref i));
                }
                break;
            }
            case 8:
            {
                for (var j = 0; j < count; j++)
                {
                    var l = _reader.ReadUInt64();
                    list.Add(Unsafe.As<ulong, T>(ref l));
                }
                break;
            }
            default:
                throw new NotSupportedException($"Enum size {size} not supported");
        }

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

    public T ReadEnum<T>()
    {
        var size = Unsafe.SizeOf<T>();

        switch (size)
        {
            case 1:
                var b = _reader.ReadByte();
                return Unsafe.As<byte, T>(ref b);
            case 2:
                var s = _reader.ReadUInt16();
                return Unsafe.As<ushort, T>(ref s);
            case 4:
                var i = _reader.ReadUInt32();
                return Unsafe.As<uint, T>(ref i);
            case 8:
                var l = _reader.ReadUInt64();
                return Unsafe.As<ulong, T>(ref l);
            default:
                throw new NotSupportedException($"Enum size {size} not supported");
        }
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
}