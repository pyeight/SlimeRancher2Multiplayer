using JetBrains.Annotations;

namespace SR2MP.Packets.Utils;

/// <summary>
/// An enum that denotes the reliability of a packet.
/// </summary>
[PublicApi]
public enum PacketReliability : byte
{
    /// <summary>
    /// The packet is unreliable.
    /// </summary>
    Unreliable = 0,

    /// <summary>
    /// The packet is unreliable but ordered.
    /// </summary>
    UnreliableOrdered = 1,

    /// <summary>
    /// The packet is reliable.
    /// </summary>
    Reliable = 2,

    /// <summary>
    /// The packet is reliable and ordered.
    /// </summary>
    ReliableOrdered = 3
}