using UnityEngine;
using System.Text;

namespace SR2MP.Packets.Utils;

public sealed class PacketReader : IDisposable
{
    private readonly MemoryStream stream;
    private readonly BinaryReader reader;

    public PacketReader(byte[] data)
    {
        stream = new MemoryStream(data);
        reader = new BinaryReader(stream, Encoding.UTF8);
    }

    public byte ReadByte() => reader.ReadByte();

    public int ReadInt() => reader.ReadInt32();
    public long ReadLong() => reader.ReadInt64();

    public float ReadFloat() => reader.ReadSingle();
    
    public double ReadDouble() => reader.ReadDouble();

    public string ReadString() => reader.ReadString();

    public bool ReadBool() => reader.ReadBoolean();

    public Vector3 ReadVector3() => new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

    public Quaternion ReadQuaternion() => new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

    public T[] ReadArray<T>(Func<PacketReader, T> reader)
    {
        var array = new T[this.reader.ReadInt32()];

        for (var i = 0; i < array.Length; i++)
            array[i] = reader(this);

        return array;
    }

    public List<T> ReadList<T>(Func<PacketReader, T> reader)
    {
        var list = new List<T>(this.reader.ReadInt32());

        for (var i = 0; i < list.Count; i++)
            list[i] = reader(this);

        return list;
    }

    public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(Func<PacketReader, TKey> keyReader, Func<PacketReader, TValue> valueReader) where TKey : notnull
    {
        var dict = new Dictionary<TKey, TValue>(reader.ReadInt32());

        for (var i = 0; i < dict.Count; i++)
            dict[keyReader(this)] = valueReader(this);

        return dict;
    }

    // All packet types MUST have a parameter-less constructor! Either make the type a struct (which always has a parameterless constructor), or at least declare a parameterless constructor for classes!
    public T ReadPacket<T>() where T : IPacket, new()
    {
        var result = new T();
        result.Deserialise(this);
        return result;
    }

    public T ReadEnum<T>() => (T)ReadEnum(typeof(T)); // Special case to avoid the extra overhead of two switch cases happening at once

    public T Read<T>() => (T)Read(typeof(T));

    // This should NOT be used in actual RPCs! This is just a backing system! Use the concrete methods above!
    // What this basically does is that it's a non-generic read method that returns an object based on the type you feed it.
    private object Read(Type type) => type switch
    {
        null => throw new NullReferenceException(nameof(type)),
        _ when type == typeof(byte) => ReadByte(),
        _ when type == typeof(bool) => ReadBool(),
        _ when type == typeof(int) => ReadInt(),
        _ when type == typeof(long) => ReadLong(),
        _ when type == typeof(float) => ReadFloat(),
        _ when type == typeof(double) => ReadDouble(),
        _ when type == typeof(string) => ReadString(),
        _ when type == typeof(Vector3) => ReadVector3(),
        _ when type == typeof(Quaternion) => ReadQuaternion(),
        _ when type.IsAssignableTo(typeof(Enum)) => ReadEnum(type),
        _ when type.IsAssignableTo(typeof(IPacket)) => ReadPacket(type),
        _ => throw new NotSupportedException($"Unable to read a value associated with {type.Name}!")
    };

    // Non-generic variant of the ReadPacket<T> method, for the above non-generic catch-all read method.
    private object ReadPacket(Type type)
    {
        var result = (IPacket)Activator.CreateInstance(type)!;
        result.Deserialise(this);
        return result;
    }

    private object ReadEnum(Type type) => Enum.ToObject(type, Read(Enum.GetUnderlyingType(type)));

    public void Skip(int count) => stream.Position += count;

    public void Dispose()
    {
        reader?.Dispose();
        stream?.Dispose();
        GC.SuppressFinalize(this);
    }
}