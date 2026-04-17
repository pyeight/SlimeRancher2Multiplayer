using JetBrains.Annotations;

namespace SR2MP.Packets.Utils;

/// <summary>
/// An enum that denotes the reliability of a packet.
/// </summary>
[PublicApi, Flags]
public enum PacketReliability : byte
{
    /// <summary>
    /// The packet is unreliable.
    /// </summary>
#pragma warning disable S2346 // Flags enumerations zero-value members should be named "None"
    Unreliable = 0,
#pragma warning restore S2346 // Flags enumerations zero-value members should be named "None"

    /// <summary>
    /// The packet is reliable.
    /// </summary>
    Reliable = 1 << 0,

    /// <summary>
    /// The packet is ordered.
    /// </summary>
    Ordered = 1 << 1,

    /// <summary>
    /// The packet is reliable and ordered.
    /// </summary>
    ReliableOrdered = Reliable | Ordered
}