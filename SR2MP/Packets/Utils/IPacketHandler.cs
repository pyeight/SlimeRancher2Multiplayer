using System.Net;

namespace SR2MP.Packets.Utils;

public interface IServerPacketHandler
{
    void HandleServer(byte[] data, IPEndPoint clientEp);
}