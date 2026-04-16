using JetBrains.Annotations;

namespace SR2MP.Packets.Utils;

/// <summary>
/// An interface that represents a custom packet.
/// </summary>
[PublicApi]
public interface ICustomPacket : IReliabilityNetObject
{
    /// <summary>
    /// Gets the packet header.
    /// </summary>
    byte PacketHeader { get; }
}