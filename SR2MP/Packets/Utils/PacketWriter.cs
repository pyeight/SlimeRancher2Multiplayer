using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using LiteNetLib.Utils;
using Unity.Mathematics;

namespace SR2MP.Packets.Utils;

public sealed class PacketWriter : IDisposable
{
    private readonly NetDataWriter _writer;

    private byte _currentPackingByte;
    private int _currentBitIndex;

    public PacketWriter()
    {
        _writer = new NetDataWriter();
    }

    public PacketWriter(NetDataWriter writer)
    {
        _writer = writer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value) => _writer.Put(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSByte(sbyte value) => _writer.Put(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value) => _writer.Put(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteShort(short value) => _writer.Put(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUShort(ushort value) => _writer.Put(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt(int value) => _writer.Put(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt(uint value) => _writer.Put(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLong(long value) => _writer.Put(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteULong(ulong value) => _writer.Put(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat(float value) => _writer.Put(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDouble(double value) => _writer.Put(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(string? value) => _writer.Put(value ?? string.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteEnum<T>(T value) where T : struct, Enum => PacketWriterDels.Enum<T>.Func(this, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteNetObject<T>(T value) where T : INetObject => value.Serialise(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePacket<T>(T value) where T : PacketBase
    {
        _writer.Put((byte)value.Type);
        value.Serialise(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVector3(Vector3 value)
    {
        _writer.Put(value.x);
        _writer.Put(value.y);
        _writer.Put(value.z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteQuaternion(Quaternion value)
    {
        _writer.Put(value.x);
        _writer.Put(value.y);
        _writer.Put(value.z);
        _writer.Put(value.w);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat4(float4 value)
    {
        _writer.Put(value.x);
        _writer.Put(value.y);
        _writer.Put(value.z);
        _writer.Put(value.w);
    }

    public void WriteArray<T>(T[] array, Action<PacketWriter, T> writer)
    {
        if (array == null)
        {
            _writer.Put(0);
            return;
        }

        _writer.Put(array.Length);

        foreach (var item in array)
            writer(this, item);
    }

    public void WriteList<T>(List<T> list, Action<PacketWriter, T> writer)
    {
        if (list == null)
        {
            _writer.Put(0);
            return;
        }

        _writer.Put(list.Count);

        foreach (var item in list)
            writer(this, item);
    }

    public void WriteSet<T>(HashSet<T> set, Action<PacketWriter, T> writer)
    {
        if (set == null)
        {
            _writer.Put(0);
            return;
        }

        _writer.Put(set.Count);

        foreach (var item in set)
            writer(this, item);
    }

    public void WriteCppList<T>(CppCollections.List<T> list, Action<PacketWriter, T> writer)
    {
        if (list == null)
        {
            _writer.Put(0);
            return;
        }

        _writer.Put(list.Count);

        foreach (var item in list)
            writer(this, item);
    }

    public void WriteCppSet<T>(CppCollections.HashSet<T> set, Action<PacketWriter, T> writer)
    {
        if (set == null)
        {
            _writer.Put(0);
            return;
        }

        _writer.Put(set.Count);

        foreach (var item in set)
            writer(this, item);
    }

    public void WriteDictionary<TKey, TValue>(Dictionary<TKey, TValue> dict, Action<PacketWriter, TKey> keyWriter, Action<PacketWriter, TValue> valueWriter) where TKey : notnull
    {
        if (dict == null)
        {
            _writer.Put(0);
            return;
        }

        _writer.Put(dict.Count);

        foreach (var (key, value) in dict)
        {
            keyWriter(this, key);
            valueWriter(this, value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetPackingBools()
    {
        _currentPackingByte = 0;
        _currentBitIndex = 0;
    }

    public void WritePackedBool(bool value)
    {
        if (value)
            _currentPackingByte |= (byte)(1 << _currentBitIndex);

        _currentBitIndex++;

        if (_currentBitIndex == 8)
        {
            _writer.Put(_currentPackingByte);
            ResetPackingBools();
        }
    }

    public void EndPackingBools()
    {
        if (_currentBitIndex > 0)
            _writer.Put(_currentPackingByte);

        ResetPackingBools();
    }

    public byte[] ToArray() => _writer.CopyData();

    public void Dispose()
    {
        // NetDataWriter doesn't need disposal.
        GC.SuppressFinalize(this);
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
    public static readonly Action<PacketWriter, float> Float = (writer, value) => writer.WriteFloat(value);

    public static class Packet<T> where T : PacketBase
    {
        public static readonly Action<PacketWriter, T> Func = (writer, value) => writer.WritePacket(value);
    }

    public static class NetObject<T> where T : INetObject
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
                1 => (writer, value) => writer.WriteByte(Unsafe.As<T, byte>(ref value)),
                2 => (writer, value) => writer.WriteUShort(Unsafe.As<T, ushort>(ref value)),
                4 => (writer, value) => writer.WriteUInt(Unsafe.As<T, uint>(ref value)),
                8 => (writer, value) => writer.WriteULong(Unsafe.As<T, ulong>(ref value)),
                _ => throw new ArgumentException($"Enum size {size} not supported")
            };
        }
    }

    public static class Tuple<T1, T2>
    {
        public static readonly Action<PacketWriter, (T1, T2)> Func = CreateWriter();

        private static Action<PacketWriter, (T1, T2)> CreateWriter() => TupleResolver.CreateTupleWriter<(T1, T2)>(typeof(T1), typeof(T2));
    }

    // See PacketReaderDels for the comments, they're pretty much the same
    private static class TupleResolver
    {
        private static readonly Dictionary<Type, MethodInfo> TypeWriteCache = new();

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
}
