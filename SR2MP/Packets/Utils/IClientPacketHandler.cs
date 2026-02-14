namespace SR2MP.Packets.Utils;

public interface IClientPacketHandler : IPacketHandler
{
    void Handle(byte[] data);
}