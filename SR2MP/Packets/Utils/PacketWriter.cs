using System.Text;

namespace SR2MP.Packets.Utils;

public class PacketWriter : IDisposable
{
    private readonly MemoryStream stream;
    private readonly BinaryWriter writer;

    public PacketWriter()
    {
        stream = new MemoryStream();
        writer = new BinaryWriter(stream, Encoding.UTF8);
    }

    public void WriteByte(byte value) => writer.Write(value);

    public void WriteInt(int value) => writer.Write(value);
    public void WriteLong(long value) => writer.Write(value);

    public void WriteFloat(float value) => writer.Write(value);
    
    public void WriteDouble(double value) => writer.Write(value);

    public void WriteString(string value) => writer.Write(value);

    public void WriteBool(bool value) => writer.Write(value);

    public void WritePacket(IPacket value) => value.Serialise(this);

    public void WriteEnum(Enum value) => Write(Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType())));

    public void WriteVector3(Vector3 value)
    {
        WriteFloat(value.x);
        WriteFloat(value.y);
        WriteFloat(value.z);
    }

    public void WriteQuaternion(Quaternion value)
    {
        WriteFloat(value.x);
        WriteFloat(value.y);
        WriteFloat(value.z);
        WriteFloat(value.w);
    }

    public void WriteArray<T>(T[] array, Action<PacketWriter, T> writer)
    {
        WriteInt(array.Length);

        for (var i = 0; i < array.Length; i++)
            writer(this, array[i]);
    }

    public void WriteList<T>(List<T> list, Action<PacketWriter, T> writer)
    {
        WriteInt(list.Count);

        for (var i = 0; i < list.Count; i++)
            writer(this, list[i]);
    }

    public void WriteDictionary<TKey, TValue>(Dictionary<TKey, TValue> dict, Action<PacketWriter, TKey> keyWriter, Action<PacketWriter, TValue> valueWriter) where TKey : notnull
    {
        WriteInt(dict.Count);

        foreach (var (key, value) in dict)
        {
            keyWriter(this, key);
            valueWriter(this, value);
        }
    }

    // Do NOT use this to serialise values! Use concrete methods above!
    private void Write(object value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
        else if (value is byte @byte)
            WriteByte(@byte);
        else if (value is int @int)
            WriteInt(@int);
        else if (value is long @long)
            WriteLong(@long);
        else if (value is float @float)
            WriteFloat(@float);
        else if (value is double @double)
            WriteDouble(@double);
        else if (value is string @string)
            WriteString(@string);
        else if (value is bool @bool)
            WriteBool(@bool);
        else if (value is IPacket packet)
            WritePacket(packet);
        else if (value is Enum @enum)
            WriteEnum(@enum);
        else if (value is Vector3 vector3)
            WriteVector3(vector3);
        else if (value is Quaternion quaternion)
            WriteQuaternion(quaternion);
    }

    public byte[] ToArray() => stream.ToArray();

    public void Dispose()
    {
        writer?.Dispose();
        stream?.Dispose();
        GC.SuppressFinalize(this);
    }
}