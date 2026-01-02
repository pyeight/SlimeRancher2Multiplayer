using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Mathematics;

namespace SR2MP.Packets.Utils;

public sealed class PacketWriter : IDisposable
{
    private readonly MemoryStream _stream;
    private readonly BinaryWriter _writer;

    private byte _currentPackingByte;
    private int _currentBitIndex;

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
    public void WriteShort(short value) => _writer.Write(value);

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
            _writer.Write(_currentPackingByte);
            ResetPackingBools();
        }
    }

    public void EndPackingBools()
    {
        if (_currentBitIndex > 0)
            _writer.Write(_currentPackingByte);

        ResetPackingBools();
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
            var tupleParam = Expression.Parameter(typeof(TTuple), "tuple");

            var writeCalls = new Expression[componentTypes.Length];

            for (int i = 0; i < componentTypes.Length; i++)
            {
                var itemField = Expression.Field(tupleParam, $"Item{i + 1}");
                writeCalls[i] = GetWriteExpression(writerParam, itemField, componentTypes[i]);
            }

            var block = Expression.Block(writeCalls);
            return Expression.Lambda<Action<PacketWriter, TTuple>>(block, writerParam, tupleParam).Compile();
        }

        private static Expression GetWriteExpression(ParameterExpression writerParam, MemberExpression valueAccess, Type type)
        {
            if (TypeWriteCache.TryGetValue(type, out var method))
                return Expression.Call(writerParam, method, valueAccess);

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

            if (method == null)
                throw new NotSupportedException($"Type {type.Name} is not supported in automatic Tuple serialization.");

            TypeWriteCache[type] = method;
            return Expression.Call(writerParam, method, valueAccess);
        }

        private static MethodInfo Method(string name) =>
            typeof(PacketWriter).GetMethod(name, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new MissingMethodException($"PacketWriter missing method: {name}");
    }
}