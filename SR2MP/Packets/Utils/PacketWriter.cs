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

public sealed class PacketWriter : PacketBuffer
{
    private int size;

    public override int DataSize => size;

    public PacketWriter() : base(0) { }

    private void EnsureCapacity(int bytesToAdd)
    {
        if (IsRecycled)
            throw new InvalidOperationException("PacketWriter is already recycled!");

        if (buffer == null)
            throw new InvalidOperationException("The buffer has been detached and is no longer available.");

        if (currentBitIndex > 0)
            FlushPackedByte();

        if (position + bytesToAdd > buffer.Length)
            ResizeBuffer(bytesToAdd);
    }

    private void ResizeBuffer(int bytesToAdd)
    {
        var newSize = Math.Max(position + bytesToAdd, buffer!.Length * 2);
        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        buffer.AsSpan(0, position).CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(buffer);
        buffer = newBuffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value) => WriteAlloc(1)[0] = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value) => WriteByte((byte)(value ? 1 : 0));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSByte(sbyte value) => WriteByte((byte)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteShort(short value) => BinaryPrimitives.WriteInt16LittleEndian(WriteAlloc(2), value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUShort(ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(WriteAlloc(2), value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt(int value) => BinaryPrimitives.WriteInt32LittleEndian(WriteAlloc(4), value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt(uint value) => BinaryPrimitives.WriteUInt32LittleEndian(WriteAlloc(4), value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat(float value) => BinaryPrimitives.WriteSingleLittleEndian(WriteAlloc(4), value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLong(long value) => BinaryPrimitives.WriteInt64LittleEndian(WriteAlloc(8), value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteULong(ulong value) => BinaryPrimitives.WriteUInt64LittleEndian(WriteAlloc(8), value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDouble(double value) => BinaryPrimitives.WriteDoubleLittleEndian(WriteAlloc(8), value);

    private void WriteFloats(ReadOnlySpan<float> values)
    {
        var span = WriteAlloc(values.Length * 4);

        for (var i = 0; i < values.Length; i++)
            BinaryPrimitives.WriteSingleLittleEndian(span[(i * 4)..], values[i]);
    }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public void WriteVector2(Vector2 value) => WriteFloats(stackalloc float[2] { value.x, value.y });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVector3(Vector3 value) => WriteFloats(stackalloc float[3] { value.x, value.y, value.z });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteQuaternion(Quaternion value) => WriteFloats(stackalloc float[4] { value.x, value.y, value.z, value.w });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat4(float4 value) => WriteFloats(stackalloc float[4] { value.x, value.y, value.z, value.w });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteEnum<T>(T value) where T : struct, Enum => PacketWriterDels.Enum<T>.Func(this, value);

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public void WriteEnumAsString<T>(T value) where T : struct, Enum => WriteString(value.ToString());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteNetObject<T>(T value) where T : INetObject => value.Serialise(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WritePacket<T>(T value) where T : IPacket
    {
        WriteByte((byte)value.Type);
        value.Serialise(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteCustomPacket<T>(T value) where T : ICustomPacket
    {
        WriteByte(value.PacketHeader);
        value.Serialise(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePackedInt(int value) => WritePackedUInt((uint)((value << 1) ^ (value >> 31)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePackedUInt(uint value) => WriteVarInt(value, 5);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePackedLong(long value) => WritePackedULong((ulong)((value << 1) ^ (value >> 63)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePackedULong(ulong value) => WriteVarInt(value, 10);

    public void WriteString(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            WriteUShort(0);
            return;
        }

        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        EnsureCapacity(2 + maxByteCount);

        var lengthIndex = position;
        Advance(2);

        var actualCount = Encoding.UTF8.GetBytes(value.AsSpan(), buffer.AsSpan(position));
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(lengthIndex), (ushort)actualCount);

        Advance(actualCount);
    }

    public void WriteStringWithoutSize(string? value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("Value cannot be null or empty");

        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        EnsureCapacity(maxByteCount);
        Advance(Encoding.UTF8.GetBytes(value.AsSpan(), buffer.AsSpan(position)));
    }

    private void WriteCollection<T>(int count, IEnumerable<T> items, Action<PacketWriter, T> writer)
    {
        WriteUShort((ushort)count);

        foreach (var item in items)
            writer(this, item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteArray<T>(T[]? array, Action<PacketWriter, T> writer)
        => WriteCollection(array?.Length ?? 0, array ?? Enumerable.Empty<T>(), writer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteList<T>(List<T>? list, Action<PacketWriter, T> writer)
        => WriteCollection(list?.Count ?? 0, list ?? Enumerable.Empty<T>(), writer);

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public void WriteSet<T>(HashSet<T>? set, Action<PacketWriter, T> writer)
    //     => WriteCollection(set?.Count ?? 0, set ?? Enumerable.Empty<T>(), writer);

    public void WriteCppSet<T>(CppCollections.HashSet<T>? set, Action<PacketWriter, T> writer)
    {
        if (set == null)
        {
            WriteUShort(0);
            return;
        }

        WriteUShort((ushort)set.Count);

        foreach (var item in set)
            writer(this, item);
    }

    public void WriteDictionary<TKey, TValue>(Dictionary<TKey, TValue>? dict, Action<PacketWriter, TKey> keyWriter, Action<PacketWriter, TValue> valueWriter) where TKey : notnull
    {
        WriteUShort((ushort)(dict?.Count ?? 0));

        if (dict == null)
            return;

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
            EnsureCapacity(0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void EndPackingBools() => EnsureCapacity(0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FlushPackedByte()
    {
        var packed = currentPackedByte;
        currentPackedByte = 0;
        currentBitIndex = 0;

        if (position + 1 > buffer!.Length)
            ResizeBuffer(1);

        buffer[position] = packed;
        Advance(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSpan(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return;

        EnsureCapacity(data.Length);
        data.CopyTo(buffer.AsSpan(position));
        Advance(data.Length);
    }

    // public void WriteNullable<T>(T? value) where T : struct
    // {
    //     var hasValue = value.HasValue;
    //     WriteBool(hasValue);

    //     if (hasValue)
    //         WriteStruct(value!.Value);
    // }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // private void WriteStruct<T>(T value) where T : struct => PacketWriterDels.Struct<T>.Writer(this, value);

    public override void MoveForward(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        EnsureCapacity(count);
        buffer.AsSpan(position, count).Clear();
        Advance(count);
    }

    public override void MoveBack(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        EnsureCapacity(0);

        if (position < count)
            throw new InvalidOperationException("New position cannot be negative.");

        position -= count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePackedEnum<T>(T value) where T : struct, Enum => PacketWriterDels.PackedEnum<T>.Func(this, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<byte> WriteAlloc(int count)
    {
        EnsureCapacity(count);
        var span = buffer.AsSpan(position, count);
        Advance(count);
        return span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ToSpan()
    {
        EnsureCapacity(0);
        return buffer.AsSpan(0, size);
    }

    protected override void OnRecycle()
    {
        if (buffer != null)
            ArrayPool<byte>.Shared.Return(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void EnsureBounds(int count) => EnsureCapacity(count);

    public void Reset(int initialCapacity = 256)
    {
        buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        Clear();
    }

    public byte[] DetachBuffer(out int length)
    {
        EnsureCapacity(0);
        length = position;

        var detachedBuffer = buffer;

        buffer = null!;
        Clear();

        return detachedBuffer!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Advance(int count)
    {
        position += count;

        if (position > size)
            size = position;
    }

    public override void Clear()
    {
        base.Clear();
        size = 0;
    }

    public static PacketWriter Borrow(int initialCapacity = 256)
    {
        var writer = RecyclePool<PacketWriter>.Borrow();
        writer.Reset(initialCapacity);
        return writer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(PacketWriter writer) => RecyclePool<PacketWriter>.Return(writer);

    private void WriteVarInt(ulong value, int maxSize)
    {
        EnsureCapacity(maxSize);

        while (value >= 0x80)
        {
            buffer![position++] = (byte)(value | 0x80);
            value >>= 7;
        }

        buffer![position++] = (byte)value;

        if (position > size)
            size = position;
    }
}

/// <summary>
/// Reusable cached delegates to improve performance, add more for data types as needed to avoid excess GC overhead
/// </summary>
public static class PacketWriterDels
{
    public static readonly Action<PacketWriter, byte> Byte = (writer, value) => writer.WriteByte(value);
    public static readonly Action<PacketWriter, sbyte> SByte = (writer, value) => writer.WriteSByte(value);
    public static readonly Action<PacketWriter, string> String = (writer, value) => writer.WriteString(value);
    public static readonly Action<PacketWriter, ushort> UShort = (writer, value) => writer.WriteUShort(value);
    public static readonly Action<PacketWriter, int> Int32 = (writer, value) => writer.WriteInt(value);
    public static readonly Action<PacketWriter, int> PackedInt32 = (writer, value) => writer.WritePackedInt(value);

    public static class NetObject<T> where T : INetObject
    {
        public static readonly Action<PacketWriter, T> Func = (writer, value) => value.Serialise(writer);
    }

    public static class Tuple<T1, T2>
    {
        public static readonly Action<PacketWriter, (T1, T2)> Func = CreateTupleWriter<(T1, T2)>(typeof(T1), typeof(T2));
    }

    public static class Struct<T> where T : struct
    {
        public static readonly Action<PacketWriter, T> Writer = (Action<PacketWriter, T>)Delegate.CreateDelegate(typeof(Action<PacketWriter, T>), GetWriteExpression(typeof(T)));
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

    public static class PackedEnum<T> where T : struct, Enum
    {
        public static readonly Action<PacketWriter, T> Func = CreateWriter();

        private static Action<PacketWriter, T> CreateWriter()
        {
            var size = Unsafe.SizeOf<T>();
            var underlying = Enum.GetUnderlyingType(typeof(T));
            return size switch
            {
                1 => (writer, value) => writer.WriteByte(Unsafe.As<T, byte>(ref value)),
                2 => (writer, value) => writer.WriteUShort(Unsafe.As<T, ushort>(ref value)),
                4 when underlying == typeof(int) => (writer, value) => writer.WritePackedInt(Unsafe.As<T, int>(ref value)),
                4 => (writer, value) => writer.WritePackedUInt(Unsafe.As<T, uint>(ref value)),
                8 when underlying == typeof(long) => (writer, value) => writer.WritePackedLong(Unsafe.As<T, long>(ref value)),
                8 => (writer, value) => writer.WritePackedULong(Unsafe.As<T, ulong>(ref value)),
                _ => throw new ArgumentException($"Enum size {size} not supported")
            };
        }
    }

    private static readonly ConcurrentDictionary<Type, MethodInfo> TypeWriteCache = new();

    private static readonly ReadOnlyDictionary<Type, string> WriteMethodMap = new(new Dictionary<Type, string>()
    {
        [typeof(byte)] = nameof(PacketWriter.WriteByte),
        [typeof(int)] = nameof(PacketWriter.WriteInt),
        [typeof(uint)] = nameof(PacketWriter.WriteUInt),
        [typeof(long)] = nameof(PacketWriter.WriteLong),
        [typeof(bool)] = nameof(PacketWriter.WriteBool),
        [typeof(short)] = nameof(PacketWriter.WriteShort),
        [typeof(ulong)] = nameof(PacketWriter.WriteULong),
        [typeof(sbyte)] = nameof(PacketWriter.WriteSByte),
        [typeof(float)] = nameof(PacketWriter.WriteFloat),
        [typeof(ushort)] = nameof(PacketWriter.WriteUShort),
        [typeof(double)] = nameof(PacketWriter.WriteDouble),
        [typeof(string)] = nameof(PacketWriter.WriteString),
        [typeof(float4)] = nameof(PacketWriter.WriteFloat4),
        [typeof(Vector3)] = nameof(PacketWriter.WriteVector3),
        [typeof(Quaternion)] = nameof(PacketWriter.WriteQuaternion),
    });

    public static Action<PacketWriter, TTuple> CreateTupleWriter<TTuple>(params Type[] componentTypes)
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

        if (WriteMethodMap.TryGetValue(type, out var methodName))
            method = Method(methodName);
        else if (type.IsEnum)
            method = Method(nameof(PacketWriter.WriteEnum)).MakeGenericMethod(type);
        else if (typeof(IPacket).IsAssignableFrom(type))
            method = Method(nameof(PacketWriter.WritePacket)).MakeGenericMethod(type);
        else if (typeof(ICustomPacket).IsAssignableFrom(type))
            method = Method(nameof(PacketWriter.WriteCustomPacket)).MakeGenericMethod(type);
        else if (typeof(INetObject).IsAssignableFrom(type))
            method = Method(nameof(PacketWriter.WriteNetObject)).MakeGenericMethod(type);

        if (method == null)
            throw new NotSupportedException($"Type {type.Name} is not supported in automatic serialization.");

        TypeWriteCache[type] = method;
        return method;
    }

    private static MethodInfo Method(string name) =>
        typeof(PacketWriter).GetMethod(name, BindingFlags.Instance | BindingFlags.Public)
        ?? throw new MissingMethodException($"PacketWriter missing method: {name}");
}