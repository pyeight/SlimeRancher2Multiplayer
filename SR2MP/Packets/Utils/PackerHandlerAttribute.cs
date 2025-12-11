namespace SR2MP.Packets.Utils;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PacketHandlerAttribute(byte packetType) : Attribute
{
    public byte PacketType { get; } = packetType;
}