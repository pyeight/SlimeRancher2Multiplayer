using System.Buffers;
using System.Buffers.Binary;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Mathematics;

namespace SR2MP.Packets.Utils;

public sealed class PacketWriter : PacketBase
{
    public PacketWriter() : base(256, 0) {}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int bytesToAdd)
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(PacketWriter));

        EndPackingBools();

        if (position + bytesToAdd > buffer.Length)
            ResizeBuffer(bytesToAdd);
    }

    private void ResizeBuffer(int bytesToAdd)
    {
        var newSize = Mathf.Max(buffer.Length * 2, position + bytesToAdd);
        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        Buffer.BlockCopy(buffer, 0, newBuffer, 0, position);
        ArrayPool<byte>.Shared.Return(buffer);
        buffer = newBuffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value)
    {
        EnsureCapacity(1);
        buffer[position++] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value) => WriteByte((byte)(value ? 1 : 0));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSByte(sbyte value) => WriteByte((byte)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteShort(short value) => WriteUShort((ushort)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUShort(ushort value)
    {
        EnsureCapacity(2);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(position), value);
        position += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt(int value) => WriteUInt((uint)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt(uint value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(position), value);
        position += 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLong(long value) => WriteULong((ulong)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteULong(ulong value)
    {
        EnsureCapacity(8);
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(position), value);
        position += 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat(float value) => WriteInt(BitConverter.SingleToInt32Bits(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDouble(double value) => WriteLong(BitConverter.DoubleToInt64Bits(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVector2(Vector2 value)
    {
        EnsureCapacity(8);

        var span = buffer.AsSpan(position);
        BinaryPrimitives.WriteInt32LittleEndian(span, BitConverter.SingleToInt32Bits(value.x));
        BinaryPrimitives.WriteInt32LittleEndian(span[4..], BitConverter.SingleToInt32Bits(value.y));

        position += 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVector3(Vector3 value)
    {
        EnsureCapacity(12);

        var span = buffer.AsSpan(position);
        BinaryPrimitives.WriteInt32LittleEndian(span, BitConverter.SingleToInt32Bits(value.x));
        BinaryPrimitives.WriteInt32LittleEndian(span[4..], BitConverter.SingleToInt32Bits(value.y));
        BinaryPrimitives.WriteInt32LittleEndian(span[8..], BitConverter.SingleToInt32Bits(value.z));

        position += 12;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteQuaternion(Quaternion value)
    {
        EnsureCapacity(16);

        var span = buffer.AsSpan(position);
        BinaryPrimitives.WriteInt32LittleEndian(span, BitConverter.SingleToInt32Bits(value.x));
        BinaryPrimitives.WriteInt32LittleEndian(span[4..], BitConverter.SingleToInt32Bits(value.y));
        BinaryPrimitives.WriteInt32LittleEndian(span[8..], BitConverter.SingleToInt32Bits(value.z));
        BinaryPrimitives.WriteInt32LittleEndian(span[12..], BitConverter.SingleToInt32Bits(value.w));

        position += 16;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat4(float4 value)
    {
        EnsureCapacity(16);

        var span = buffer.AsSpan(position);
        BinaryPrimitives.WriteInt32LittleEndian(span, BitConverter.SingleToInt32Bits(value.x));
        BinaryPrimitives.WriteInt32LittleEndian(span[4..], BitConverter.SingleToInt32Bits(value.y));
        BinaryPrimitives.WriteInt32LittleEndian(span[8..], BitConverter.SingleToInt32Bits(value.z));
        BinaryPrimitives.WriteInt32LittleEndian(span[12..], BitConverter.SingleToInt32Bits(value.w));

        position += 16;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteEnum<T>(T value) where T : struct, Enum => PacketWriterDels.Enum<T>.Func(this, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteNetObject<T>(T value) where T : INetObject => value.Serialise(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePacket<T>(T value) where T : IPacket
    {
        WriteByte((byte)value.Type);
        value.Serialise(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePackedInt(int value) => WritePackedUInt((uint)((value << 1) ^ (value >> 31)));

    public void WritePackedUInt(uint value)
    {
        EnsureCapacity(5);

        while (value >= 0x80)
        {
            buffer[position++] = (byte)(value | 0x80);
            value >>= 7;
        }

        buffer[position++] = (byte)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePackedLong(long value) => WritePackedULong((ulong)((value << 1) ^ (value >> 63)));

    public void WritePackedULong(ulong value)
    {
        EnsureCapacity(10);

        while (value >= 0x80)
        {
            buffer[position++] = (byte)(value | 0x80);
            value >>= 7;
        }

        buffer[position++] = (byte)value;
    }

    public void WriteString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            WriteUShort(0);
            return;
        }

        var byteCount = Encoding.UTF8.GetByteCount(value);
        EnsureCapacity(2 + byteCount);

        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(position), (ushort)byteCount);
        position += 2;

        Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, position);
        position += byteCount;
    }

    public void WriteArray<T>(T[]? array, Action<PacketWriter, T> writer)
    {
        if (array == null)
        {
            WriteInt(0);
            return;
        }

        WriteInt(array.Length);

        foreach (var item in array)
            writer(this, item);
    }

    public void WriteList<T>(List<T>? list, Action<PacketWriter, T> writer)
    {
        if (list == null)
        {
            WriteInt(0);
            return;
        }

        WriteInt(list.Count);

        foreach (var item in list)
            writer(this, item);
    }

    public void WriteSet<T>(HashSet<T>? set, Action<PacketWriter, T> writer)
    {
        if (set == null)
        {
            WriteInt(0);
            return;
        }

        WriteInt(set.Count);

        foreach (var item in set)
            writer(this, item);
    }

    public void WriteCppList<T>(CppCollections.List<T>? list, Action<PacketWriter, T> writer)
    {
        if (list == null)
        {
            WriteInt(0);
            return;
        }

        WriteInt(list.Count);

        foreach (var item in list)
            writer(this, item);
    }

    public void WriteCppSet<T>(CppCollections.HashSet<T>? set, Action<PacketWriter, T> writer)
    {
        if (set == null)
        {
            WriteInt(0);
            return;
        }

        WriteInt(set.Count);

        foreach (var item in set)
            writer(this, item);
    }

    public void WriteDictionary<TKey, TValue>(Dictionary<TKey, TValue>? dict, Action<PacketWriter, TKey> keyWriter, Action<PacketWriter, TValue> valueWriter) where TKey : notnull
    {
        if (dict == null)
        {
            WriteInt(0);
            return;
        }

        WriteInt(dict.Count);

        foreach (var (key, value) in dict)
        {
            keyWriter(this, key);
            valueWriter(this, value);
        }
    }

    public void WritePackedBool(bool value)
    {
        if (value)
            currentPackedByte |= (byte)(1 << currentBitIndex);

        currentBitIndex++;

        if (currentBitIndex == 8)
            ResetPackingBools();
    }

    public void EndPackingBools()
    {
        if (currentBitIndex > 0)
            ResetPackingBools();
    }

    private void ResetPackingBools()
    {
        currentBitIndex = 0;
        WriteByte(currentPackedByte);
        currentPackedByte = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] ToArray() => buffer.AsSpan(0, position).ToArray();
}

/// <summary>
/// Reusable cached delegates to improve performance, add more for data types as needed to avoid excess GC overhead
/// </summary>
public static class PacketWriterDels
{
    public static readonly Action<PacketWriter, byte> Byte = (writer, value) => writer.WriteByte(value);
    public static readonly Action<PacketWriter, sbyte> SByte = (writer, value) => writer.WriteSByte(value);
    public static readonly Action<PacketWriter, string> String = (writer, value) => writer.WriteString(value);

    public static class NetObject<T> where T : INetObject
    {
        public static readonly Action<PacketWriter, T> Func = (writer, value) => value.Serialise(writer);
    }

    public static class Tuple<T1, T2>
    {
        public static readonly Action<PacketWriter, (T1, T2)> Func = CreateTupleWriter<(T1, T2)>(typeof(T1), typeof(T2));
    }

    public static class Enum<T> where T : struct, Enum
    {
        public static readonly Action<PacketWriter, T> Func = CreateWriter();

        private static Action<PacketWriter, T> CreateWriter()
        {
            var size = Unsafe.SizeOf<T>();
            return size switch
            {
                1 => (writer, value) => writer.WriteByte(Unsafe.As<T, byte>(ref value)),
                2 => (writer, value) => writer.WriteUShort(Unsafe.As<T, ushort>(ref value)),
                4 => (writer, value) => writer.WriteUInt(Unsafe.As<T, uint>(ref value)),
                8 => (writer, value) => writer.WriteULong(Unsafe.As<T, ulong>(ref value)),
                _ => throw new ArgumentException($"Enum size {size} not supported")
            };
        }
    }

    // See PacketReaderDels for the comments, they're pretty much the same
    private static readonly Dictionary<Type, MethodInfo> TypeWriteCache = new();

    private static Action<PacketWriter, TTuple> CreateTupleWriter<TTuple>(params Type[] componentTypes)
    {
        var writerParam = Expression.Parameter(typeof(PacketWriter), "writer");
        var tupleParam = Expression.Parameter(typeof(TTuple), "value");

        var writeCalls = new Expression[componentTypes.Length];

        for (var i = 0; i < componentTypes.Length; i++)
            writeCalls[i] = Expression.Call(writerParam, GetWriteExpression(componentTypes[i]), Expression.Field(tupleParam, $"Item{i + 1}"));

        var block = Expression.Block(writeCalls);
        return Expression.Lambda<Action<PacketWriter, TTuple>>(block, writerParam, tupleParam).Compile();
    }

    private static MethodInfo GetWriteExpression(Type type)
    {
        if (TypeWriteCache.TryGetValue(type, out var method))
            return method;

        if (type == typeof(byte)) method = Method(nameof(PacketWriter.WriteByte));
        else if (type == typeof(int)) method = Method(nameof(PacketWriter.WriteInt));
        else if (type == typeof(uint)) method = Method(nameof(PacketWriter.WriteUInt));
        else if (type == typeof(long)) method = Method(nameof(PacketWriter.WriteLong));
        else if (type == typeof(bool)) method = Method(nameof(PacketWriter.WriteBool));
        else if (type == typeof(short)) method = Method(nameof(PacketWriter.WriteShort));
        else if (type == typeof(ulong)) method = Method(nameof(PacketWriter.WriteULong));
        else if (type == typeof(sbyte)) method = Method(nameof(PacketWriter.WriteSByte));
        else if (type == typeof(float)) method = Method(nameof(PacketWriter.WriteFloat));
        else if (type == typeof(ushort)) method = Method(nameof(PacketWriter.WriteUShort));
        else if (type == typeof(double)) method = Method(nameof(PacketWriter.WriteDouble));
        else if (type == typeof(string)) method = Method(nameof(PacketWriter.WriteString));
        else if (type == typeof(float4)) method = Method(nameof(PacketWriter.WriteFloat4));
        else if (type == typeof(Vector3)) method = Method(nameof(PacketWriter.WriteVector3));
        else if (type == typeof(Quaternion)) method = Method(nameof(PacketWriter.WriteQuaternion));
        else if (type.IsEnum) method = Method(nameof(PacketWriter.WriteEnum)).MakeGenericMethod(type);
        else if (typeof(IPacket).IsAssignableFrom(type)) method = Method(nameof(PacketWriter.WritePacket)).MakeGenericMethod(type);
        else if (typeof(INetObject).IsAssignableFrom(type)) method = Method(nameof(PacketWriter.WriteNetObject)).MakeGenericMethod(type);

        if (method == null)
            throw new NotSupportedException($"Type {type.Name} is not supported in automatic Tuple serialization.");

        TypeWriteCache[type] = method;
        return method;
    }

    private static MethodInfo Method(string name) =>
        typeof(PacketWriter).GetMethod(name, BindingFlags.Instance | BindingFlags.Public)
        ?? throw new MissingMethodException($"PacketWriter missing method: {name}");
}