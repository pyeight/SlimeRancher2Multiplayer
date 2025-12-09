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

    public void WriteFloat(float value) => writer.Write(value);

    public void WriteString(string value) => writer.Write(value);

    public void WriteBool(bool value) => writer.Write(value);

    public void WritePacket(IPacket value) => value.Serialise(this);

    public void WriteEnum(Enum value) => Write(Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType())));

    // Do NOT use this to serialise values! Use concrete methods above!
    private void Write(object value)
    {
        if (value is byte @byte)
            WriteByte(@byte);
        else if (value is int @int)
            WriteInt(@int);
        else if (value is float @float)
            WriteFloat(@float);
        else if (value is string @string)
            WriteString(@string);
        else if (value is bool @bool)
            WriteBool(@bool);
        else if (value is IPacket packet)
            WritePacket(packet);
        else if (value is Enum @enum)
            WriteEnum(@enum);
    }

    public byte[] ToArray() => stream.ToArray();

    public void Dispose()
    {
        writer?.Dispose();
        stream?.Dispose();
        GC.SuppressFinalize(this);
    }
}