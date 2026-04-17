using JetBrains.Annotations;

namespace SR2MP.Packets.Utils;

/// <summary>
/// An interface that represents a packet handler.
/// </summary>
[PublicApi]
public interface IPacketHandler
{
    /// <summary>
    /// Sets a value indicating whether this handler is currently executing on the server side.
    /// </summary>
    bool IsServerSide { set; }
}