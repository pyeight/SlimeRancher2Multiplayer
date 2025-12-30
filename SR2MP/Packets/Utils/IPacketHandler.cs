namespace SR2MP.Packets.Utils;

public interface IPacketHandler
{
    void Handle(byte[] data, string clientIdentifier);
}