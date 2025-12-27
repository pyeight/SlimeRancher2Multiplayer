namespace SR2MP.Packets.Utils;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class PacketHandlerAttribute : Attribute
{
    public byte PacketType { get; }

    public PacketHandlerAttribute(byte packetType)
    {
        PacketType = packetType;
    }
}