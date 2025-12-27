using System.Runtime.CompilerServices;
using System.Text;
using Unity.Mathematics;

namespace SR2MP.Packets.Utils;

// There is a TON of copy paste at the moment
// TODO: Figure out a way to reduce the level of copy paste here
public sealed class PacketWriter : IDisposable
{
    private readonly MemoryStream _stream;
    private readonly BinaryWriter _writer;

    public PacketWriter()
    {
        _stream = new MemoryStream();
        _writer = new BinaryWriter(_stream, Encoding.UTF8);
    }

    public void WriteByte(byte value) => _writer.Write(value);

    public void WriteSByte(sbyte value) => _writer.Write(value);

    public void WriteInt(int value) => _writer.Write(value);

    public void WriteLong(long value) => _writer.Write(value);

    public void WriteFloat(float value) => _writer.Write(value);

    public void WriteDouble(double value) => _writer.Write(value);

    public void WriteString(string? value) => _writer.Write(value ?? string.Empty);

    public void WriteBool(bool value) => _writer.Write(value);

    public void WritePacket<T>(T value) where T : IPacket => value.Serialise(this);

    public void WriteVector3(Vector3 value)
    {
        _writer.Write(value.x);
        _writer.Write(value.y);
        _writer.Write(value.z);
    }

    public void WriteQuaternion(Quaternion value)
    {
        _writer.Write(value.x);
        _writer.Write(value.y);
        _writer.Write(value.z);
        _writer.Write(value.w);
    }

    public void WriteFloat4(float4 value)
    {
        _writer.Write(value.x);
        _writer.Write(value.y);
        _writer.Write(value.z);
        _writer.Write(value.w);
    }

    public void WriteArray<T>(T[] array, Action<PacketWriter, T> writer)
    {
        _writer.Write(array.Length);

        foreach (var item in array)
            writer(this, item);
    }

    public void WriteList<T>(List<T> list, Action<PacketWriter, T> writer)
    {
        _writer.Write(list.Count);

        foreach (var item in list)
            writer(this, item);
    }

    public void WriteEnumList<T>(List<T> list) where T : struct, Enum
    {
        _writer.Write(list.Count);

        var size = Unsafe.SizeOf<T>();

        switch (size)
        {
            case 1:
            {
                foreach (var item in list)
                    _writer.Write(Unsafe.As<T, byte>(ref Unsafe.AsRef(in item)));

                break;
            }
            case 2:
            {
                foreach (var item in list)
                    _writer.Write(Unsafe.As<T, ushort>(ref Unsafe.AsRef(in item)));

                break;
            }
            case 4:
            {
                foreach (var item in list)
                    _writer.Write(Unsafe.As<T, uint>(ref Unsafe.AsRef(in item)));

                break;
            }
            case 8:
            {
                foreach (var item in list)
                    _writer.Write(Unsafe.As<T, ulong>(ref Unsafe.AsRef(in item)));

                break;
            }
            default:
                throw new ArgumentException($"Enum size {size} not supported");
        }
    }

    public void WriteEnumSet<T>(HashSet<T> set) where T : struct, Enum
    {
        _writer.Write(set.Count);

        var size = Unsafe.SizeOf<T>();

        switch (size)
        {
            case 1:
            {
                foreach (var item in set)
                    _writer.Write(Unsafe.As<T, byte>(ref Unsafe.AsRef(in item)));

                break;
            }
            case 2:
            {
                foreach (var item in set)
                    _writer.Write(Unsafe.As<T, ushort>(ref Unsafe.AsRef(in item)));

                break;
            }
            case 4:
            {
                foreach (var item in set)
                    _writer.Write(Unsafe.As<T, uint>(ref Unsafe.AsRef(in item)));

                break;
            }
            case 8:
            {
                foreach (var item in set)
                    _writer.Write(Unsafe.As<T, ulong>(ref Unsafe.AsRef(in item)));

                break;
            }
            default:
                throw new ArgumentException($"Enum size {size} not supported");
        }
    }

    public void WriteSet<T>(HashSet<T> set, Action<PacketWriter, T> writer)
    {
        _writer.Write(set.Count);

        foreach (var item in set)
            writer(this, item);
    }

    public void WriteCppList<T>(CppCollections.List<T> list, Action<PacketWriter, T> writer)
    {
        _writer.Write(list.Count);

        foreach (var item in list)
            writer(this, item);
    }

    public void WriteCppEnumList<T>(CppCollections.List<T> list) where T : struct, Enum
    {
        _writer.Write(list.Count);

        var size = Unsafe.SizeOf<T>();

        switch (size)
        {
            case 1:
            {
                foreach (var item in list)
                    _writer.Write(Unsafe.As<T, byte>(ref Unsafe.AsRef(in item)));

                break;
            }
            case 2:
            {
                foreach (var item in list)
                    _writer.Write(Unsafe.As<T, ushort>(ref Unsafe.AsRef(in item)));

                break;
            }
            case 4:
            {
                foreach (var item in list)
                    _writer.Write(Unsafe.As<T, uint>(ref Unsafe.AsRef(in item)));

                break;
            }
            case 8:
            {
                foreach (var item in list)
                    _writer.Write(Unsafe.As<T, ulong>(ref Unsafe.AsRef(in item)));

                break;
            }
            default:
                throw new ArgumentException($"Enum size {size} not supported");
        }
    }

    public void WriteCppSet<T>(CppCollections.HashSet<T> set, Action<PacketWriter, T> writer)
    {
        _writer.Write(set.Count);

        foreach (var item in set)
            writer(this, item);
    }

    public void WriteCppEnumSet<T>(CppCollections.HashSet<T> set) where T : struct, Enum
    {
        _writer.Write(set.Count);

        var size = Unsafe.SizeOf<T>();

        switch (size)
        {
            case 1:
            {
                foreach (var item in set)
                    _writer.Write(Unsafe.As<T, byte>(ref Unsafe.AsRef(in item)));

                break;
            }
            case 2:
            {
                foreach (var item in set)
                    _writer.Write(Unsafe.As<T, ushort>(ref Unsafe.AsRef(in item)));

                break;
            }
            case 4:
            {
                foreach (var item in set)
                    _writer.Write(Unsafe.As<T, uint>(ref Unsafe.AsRef(in item)));

                break;
            }
            case 8:
            {
                foreach (var item in set)
                    _writer.Write(Unsafe.As<T, ulong>(ref Unsafe.AsRef(in item)));

                break;
            }
            default:
                throw new ArgumentException($"Enum size {size} not supported");
        }
    }

    public void WriteDictionary<TKey, TValue>(Dictionary<TKey, TValue> dict, Action<PacketWriter, TKey> keyWriter, Action<PacketWriter, TValue> valueWriter) where TKey : notnull
    {
        _writer.Write(dict.Count);

        foreach (var (key, value) in dict)
        {
            keyWriter(this, key);
            valueWriter(this, value);
        }
    }

    public void WriteEnum<T>(T value) where T : struct, Enum
    {
        var size = Unsafe.SizeOf<T>();

        switch (size)
        {
            case 1:
                _writer.Write(Unsafe.As<T, byte>(ref value));
                break;
            case 2:
                _writer.Write(Unsafe.As<T, ushort>(ref value));
                break;
            case 4:
                _writer.Write(Unsafe.As<T, uint>(ref value));
                break;
            case 8:
                _writer.Write(Unsafe.As<T, ulong>(ref value));
                break;
            default:
                throw new ArgumentException($"Enum size {size} not supported");
        }
    }

    public byte[] ToArray() => _stream.ToArray();

    public void Dispose()
    {
        _writer.Dispose();
        _stream.Dispose();
        // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor
        GC.SuppressFinalize(this);
    }
}

public static class PacketWriterDels
{
    public static readonly Action<PacketWriter, byte> Byte = (writer, value) => writer.WriteByte(value);
    public static readonly Action<PacketWriter, sbyte> SByte = (writer, value) => writer.WriteSByte(value);
    public static readonly Action<PacketWriter, string> String = (writer, value) => writer.WriteString(value);

    public static class Packet<T> where T : IPacket
    {
        public static readonly Action<PacketWriter, T> Func = (writer, value) => value.Serialise(writer);
    }
}