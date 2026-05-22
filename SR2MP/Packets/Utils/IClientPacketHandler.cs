namespace SR2MP.Packets.Utils;

/// <summary>
/// An interface that represents a client packet handler.
/// </summary>
public interface IClientPacketHandler : IPacketHandler
{
    /// <summary>
    /// Handles a packet from the server.
    /// </summary>
    /// <param name="reader">The reader containing the packet data.</param>
    void Handle(PacketReader reader);
}