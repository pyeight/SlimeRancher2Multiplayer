using System.Net;
using JetBrains.Annotations;

namespace SR2MP.Packets.Utils;

/// <summary>
/// An interface that represents a server packet handler.
/// </summary>
[PublicApi]
public interface IServerPacketHandler : IPacketHandler
{
    /// <summary>
    /// Handles a packet from a client.
    /// </summary>
    /// <param name="reader">The reader containing the packet data.</param>
    /// <param name="clientEp">The endpoint of the client that sent the packet.</param>
    void Handle(PacketReader reader, IPEndPoint? clientEp);
}