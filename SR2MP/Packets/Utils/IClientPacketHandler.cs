namespace SR2MP.Packets.Utils;

public interface IClientPacketHandler
{
    void HandleClient(byte[] data);
}