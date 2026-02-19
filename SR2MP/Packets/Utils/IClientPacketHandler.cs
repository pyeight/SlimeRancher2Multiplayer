namespace SR2MP.Packets.Utils;

public interface IClientPacketHandler : IPacketHandler
{
    void Handle(PacketReader reader);
}