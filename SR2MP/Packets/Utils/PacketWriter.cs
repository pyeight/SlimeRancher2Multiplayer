using System.Runtime.CompilerServices;
using System.Text;
using Unity.Mathematics;

namespace SR2MP.Packets.Utils;

public sealed class PacketWriter : IDisposable
{
    private readonly MemoryStream _stream;
    private readonly BinaryWriter _writer;

    public PacketWriter()
    {
        _stream = new MemoryStream();
        _writer = new BinaryWriter(_stream, Encoding.UTF8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value) => _writer.Write(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSByte(sbyte value) => _writer.Write(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value) => _writer.Write(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUShort(ushort value) => _writer.Write(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt(int value) => _writer.Write(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt(uint value) => _writer.Write(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLong(long value) => _writer.Write(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteULong(ulong value) => _writer.Write(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat(float value) => _writer.Write(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDouble(double value) => _writer.Write(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(string? value) => _writer.Write(value ?? string.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteEnum<T>(T value) where T : struct, Enum => PacketWriterDels.Enum<T>.Func(this, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePacket<T>(T value) where T : IPacket => value.Serialise(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVector3(Vector3 value)
    {
        _writer.Write(value.x);
        _writer.Write(value.y);
        _writer.Write(value.z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteQuaternion(Quaternion value)
    {
        _writer.Write(value.x);
        _writer.Write(value.y);
        _writer.Write(value.z);
        _writer.Write(value.w);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    public static readonly Action<PacketWriter, float> Float = (writer, value) => writer.WriteFloat(value);

    public static class Packet<T> where T : IPacket
    {
        public static readonly Action<PacketWriter, T> Func = (writer, value) => value.Serialise(writer);
    }

    public static class Enum<T> where T : struct, Enum
    {
        public static readonly Action<PacketWriter, T> Func = CreateWriter();

        private static Action<PacketWriter, T> CreateWriter()
        {
            var size = Unsafe.SizeOf<T>();
            return size switch
            {
                1 => (w, v) => w.WriteByte(Unsafe.As<T, byte>(ref v)),
                2 => (w, v) => w.WriteUShort(Unsafe.As<T, ushort>(ref v)),
                4 => (w, v) => w.WriteUInt(Unsafe.As<T, uint>(ref v)),
                8 => (w, v) => w.WriteULong(Unsafe.As<T, ulong>(ref v)),
                _ => throw new ArgumentException($"Enum size {size} not supported")
            };
        }
    }
}