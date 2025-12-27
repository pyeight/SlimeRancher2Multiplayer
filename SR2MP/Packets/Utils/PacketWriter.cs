using System.Runtime.CompilerServices;
using System.Text;
using Unity.Mathematics;

namespace SR2MP.Packets.Utils;

public sealed class PacketWriter : IDisposable
{
    private readonly MemoryStream stream;
    private readonly BinaryWriter _writer;

    public PacketWriter()
    {
        stream = new MemoryStream();
        _writer = new BinaryWriter(stream, Encoding.UTF8);
    }

    public void WriteByte(byte value) => _writer.Write(value);

    public void WriteSByte(sbyte value) => _writer.Write(value);

    public void WriteInt(int value) => _writer.Write(value);

    public void WriteLong(long value) => _writer.Write(value);

    public void WriteFloat(float value) => _writer.Write(value);

    public void WriteDouble(double value) => _writer.Write(value);

    public void WriteString(string value) => _writer.Write(value ?? string.Empty);

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

        for (var i = 0; i < array.Length; i++)
            writer(this, array[i]);
    }

    public void WriteList<T>(List<T> list, Action<PacketWriter, T> writer)
    {
        _writer.Write(list.Count);

        for (var i = 0; i < list.Count; i++)
            writer(this, list[i]);
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

        for (var i = 0; i < list.Count; i++)
            writer(this, list[i]);
    }

    public void WriteCppSet<T>(CppCollections.HashSet<T> set, Action<PacketWriter, T> writer)
    {
        _writer.Write(set.Count);

        foreach (var item in set)
            writer(this, item);
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

    public byte[] ToArray() => stream.ToArray();

    public void Dispose()
    {
        _writer?.Dispose();
        stream?.Dispose();
        GC.SuppressFinalize(this);
    }
}