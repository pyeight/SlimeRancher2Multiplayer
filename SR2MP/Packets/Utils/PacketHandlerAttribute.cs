using JetBrains.Annotations;

namespace SR2MP.Packets.Utils;

/// <summary>
/// An attribute that denotes a packet handler.
/// </summary>
[AttributeUsage(AttributeTargets.Class), MeansImplicitUse(ImplicitUseTargetFlags.WithMembers), PublicAPI]
public sealed class PacketHandlerAttribute : Attribute
{
    /// <summary>
    /// Gets the packet type.
    /// </summary>
    public byte PacketType { get; }

    /// <summary>
    /// Gets the handler type.
    /// </summary>
    public HandlerType HandlerType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PacketHandlerAttribute"/> class.
    /// </summary>
    /// <param name="packetType">The packet type.</param>
    /// <param name="handlerType">The handler type.</param>
    public PacketHandlerAttribute(byte packetType, HandlerType handlerType = HandlerType.Both)
    {
        PacketType = packetType;
        HandlerType = handlerType;
    }
}

/// <summary>
/// An enum that denotes the type of handler.
/// </summary>
[PublicAPI]
public enum HandlerType : byte
{
    /// <summary>
    /// The handler operates on both the client's and the host's machines.
    /// </summary>
    Both = 0,

    /// <summary>
    /// The handler operates only on the client's machine.
    /// </summary>
    Client = 1,

    /// <summary>
    /// The handler operates only on the host's machine.
    /// </summary>
    Server = 2
}