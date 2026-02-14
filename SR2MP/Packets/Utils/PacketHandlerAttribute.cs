namespace SR2MP.Packets.Utils;

[AttributeUsage(AttributeTargets.Class)]
public sealed class PacketHandlerAttribute : Attribute
{
    public byte PacketType { get; }
    public HandlerType HandlerType { get; }

    public PacketHandlerAttribute(byte packetType, HandlerType handlerType = 0)
    {
        PacketType = packetType;
        HandlerType = handlerType;
    }
}

public enum HandlerType : byte
{
    Both = 0,
    Client = 1,
    Server = 2
}