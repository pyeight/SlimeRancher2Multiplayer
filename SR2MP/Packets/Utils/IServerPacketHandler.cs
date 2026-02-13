using System.Net;

namespace SR2MP.Packets.Utils;

public interface IServerPacketHandler
{
    void Handle(byte[] data, IPEndPoint clientEp);
}