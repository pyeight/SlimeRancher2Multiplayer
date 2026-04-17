using JetBrains.Annotations;

namespace SR2MP.Packets.Utils;

/// <summary>
/// An interface that represents a network object with reliability.
/// </summary>
[PublicApi]
public interface IReliabilityNetObject : INetObject
{
    /// <summary>
    /// Gets the reliability of the network object.
    /// </summary>
    PacketReliability Reliability { get; }

    /// <summary>
    /// Gets the channel the network object is sent on.
    /// </summary>
    NetworkChannel Channel { get; }
}